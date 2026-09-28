using System.IO;
using System.Threading.Tasks;

namespace TJPork.Core.Interfaces
{
    public interface IFileStorageService
    {
        /// <summary>
        /// Saves a file stream to storage (Supabase Storage or local fallback) and returns the public URL.
        /// </summary>
        Task<string> SaveFileAsync(Stream contentStream, string originalFileName, string contentType, string folder);

        /// <summary>
        /// Deletes a file from storage by its URL, if applicable.
        /// </summary>
        Task<bool> DeleteFileAsync(string fileUrl);
    }
}
