using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TJPork.Core.Interfaces;

namespace TJPork.Infrastructure.Services
{
    public class SupabaseFileStorageService : IFileStorageService
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<SupabaseFileStorageService> _logger;
        private readonly HttpClient _httpClient;

        public SupabaseFileStorageService(
            IConfiguration config,
            IWebHostEnvironment env,
            ILogger<SupabaseFileStorageService> logger,
            HttpClient? httpClient = null)
        {
            _config = config;
            _env = env;
            _logger = logger;
            _httpClient = httpClient ?? new HttpClient();
        }

        public async Task<string> SaveFileAsync(Stream contentStream, string originalFileName, string contentType, string folder)
        {
            var supabaseUrl = (_config["Supabase:Url"] ?? _config["SUPABASE_URL"])?.Trim().TrimEnd('/');
            var supabaseKey = _config["Supabase:Key"] ?? _config["SUPABASE_KEY"] ?? _config["SUPABASE_ANON_KEY"] ?? _config["SUPABASE_SERVICE_ROLE_KEY"];
            var bucket = _config["Supabase:Bucket"] ?? _config["SUPABASE_BUCKET"] ?? "tjpork-images";

            var ext = Path.GetExtension(originalFileName).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";
            var uniqueFileName = $"{Guid.NewGuid():N}{ext}";
            var sanitizedFolder = folder.Trim('/', '\\').Replace('\\', '/');

            // 1. If Supabase credentials are configured, upload to Supabase Storage
            if (!string.IsNullOrWhiteSpace(supabaseUrl) && !string.IsNullOrWhiteSpace(supabaseKey))
            {
                try
                {
                    var objectPath = string.IsNullOrWhiteSpace(sanitizedFolder) ? uniqueFileName : $"{sanitizedFolder}/{uniqueFileName}";
                    var requestUrl = $"{supabaseUrl}/storage/v1/object/{bucket}/{objectPath}";

                    if (contentStream.CanSeek) contentStream.Position = 0;
                    using var content = new StreamContent(contentStream);
                    content.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);

                    using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
                    {
                        Content = content
                    };
                    request.Headers.Add("apikey", supabaseKey);
                    request.Headers.Add("Authorization", $"Bearer {supabaseKey}");
                    request.Headers.Add("x-upsert", "true");

                    var response = await _httpClient.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        var publicUrl = $"{supabaseUrl}/storage/v1/object/public/{bucket}/{objectPath}";
                        _logger.LogInformation("Successfully uploaded image to Supabase Storage: {Url}", publicUrl);
                        return publicUrl;
                    }

                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Supabase Storage upload returned {StatusCode}: {Error}. Falling back to local storage.", response.StatusCode, errorBody);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception uploading image to Supabase Storage. Falling back to local disk.");
                }
            }

            // 2. Local fallback storage (wwwroot/images/{folder})
            try
            {
                var localFolderPath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "images", sanitizedFolder);
                Directory.CreateDirectory(localFolderPath);

                var localFilePath = Path.Combine(localFolderPath, uniqueFileName);
                if (contentStream.CanSeek) contentStream.Position = 0;

                using (var fileStream = new FileStream(localFilePath, FileMode.Create))
                {
                    await contentStream.CopyToAsync(fileStream);
                }

                var relativeUrl = $"/images/{sanitizedFolder}/{uniqueFileName}";
                _logger.LogInformation("Saved image locally to: {Path}", relativeUrl);
                return relativeUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save file to local storage.");
                throw;
            }
        }

        public async Task<bool> DeleteFileAsync(string fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl)) return false;

            var supabaseUrl = (_config["Supabase:Url"] ?? _config["SUPABASE_URL"])?.Trim().TrimEnd('/');
            var supabaseKey = _config["Supabase:Key"] ?? _config["SUPABASE_KEY"] ?? _config["SUPABASE_ANON_KEY"] ?? _config["SUPABASE_SERVICE_ROLE_KEY"];
            var bucket = _config["Supabase:Bucket"] ?? _config["SUPABASE_BUCKET"] ?? "tjpork-images";

            // If Supabase URL
            if (!string.IsNullOrWhiteSpace(supabaseUrl) && fileUrl.StartsWith(supabaseUrl, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var prefix = $"{supabaseUrl}/storage/v1/object/public/{bucket}/";
                    if (fileUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        var objectPath = fileUrl[prefix.Length..];
                        var deleteUrl = $"{supabaseUrl}/storage/v1/object/{bucket}/{objectPath}";

                        using var request = new HttpRequestMessage(HttpMethod.Delete, deleteUrl);
                        request.Headers.Add("apikey", supabaseKey);
                        request.Headers.Add("Authorization", $"Bearer {supabaseKey}");

                        var response = await _httpClient.SendAsync(request);
                        return response.IsSuccessStatusCode;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete file from Supabase Storage: {Url}", fileUrl);
                    return false;
                }
            }

            // If local URL
            if (fileUrl.StartsWith("/images/", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var relative = fileUrl.TrimStart('/');
                    var localPath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), relative);
                    if (File.Exists(localPath))
                    {
                        File.Delete(localPath);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete local file: {Path}", fileUrl);
                }
            }

            return false;
        }
    }

    public static class FileStorageServiceExtensions
    {
        public static async Task<string> SaveFileAsync(this IFileStorageService storageService, IFormFile file, string folder)
        {
            using var stream = file.OpenReadStream();
            return await storageService.SaveFileAsync(stream, file.FileName, file.ContentType, folder);
        }
    }
}
