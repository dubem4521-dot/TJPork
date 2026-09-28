using System;

namespace TJPork.Infrastructure.Data
{
    public static class DbConnectionHelper
    {
        public static bool IsPostgreSql(string? connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) return false;
            var trimmed = connectionString.Trim();
            var lower = trimmed.ToLowerInvariant();

            return lower.StartsWith("postgres://") ||
                   lower.StartsWith("postgresql://") ||
                   lower.Contains("host=") ||
                   lower.Contains("server=") ||
                   lower.Contains("user id=") ||
                   lower.Contains("username=") ||
                   lower.Contains(".supabase.co") ||
                   lower.Contains(".pooler.supabase.com");
        }

        public static string FormatPostgreSqlConnectionString(string raw)
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            if (string.IsNullOrWhiteSpace(raw)) return raw;
            var trimmed = raw.Trim().Trim('"', '\'');

            if (trimmed.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            {
                var uri = new Uri(trimmed);
                var userInfo = uri.UserInfo.Split(':');
                var username = Uri.UnescapeDataString(userInfo[0]);
                var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
                var host = uri.Host;
                var port = uri.Port > 0 ? uri.Port : 5432;
                var database = uri.AbsolutePath.TrimStart('/');
                if (string.IsNullOrEmpty(database)) database = "postgres";

                return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
            }

            if (!trimmed.Contains("SSL Mode=", StringComparison.OrdinalIgnoreCase))
            {
                trimmed += ";SSL Mode=Require;Trust Server Certificate=true";
            }

            return trimmed;
        }
    }
}
