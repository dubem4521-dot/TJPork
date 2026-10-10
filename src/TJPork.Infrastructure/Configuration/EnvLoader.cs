using System;
using System.Collections.Generic;
using System.IO;

namespace TJPork.Infrastructure.Configuration
{
    public static class EnvLoader
    {
        public static void Load(string? explicitPath = null)
        {
            var searchPaths = new List<string>();
            if (!string.IsNullOrEmpty(explicitPath))
            {
                searchPaths.Add(explicitPath);
            }

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var currentDir = Directory.GetCurrentDirectory();

            searchPaths.Add(Path.Combine(currentDir, ".env"));
            searchPaths.Add(Path.Combine(currentDir, "..", ".env"));
            searchPaths.Add(Path.Combine(currentDir, "..", "..", ".env"));
            searchPaths.Add(Path.Combine(baseDir, ".env"));
            searchPaths.Add(Path.Combine(baseDir, "..", "..", "..", "..", ".env"));

            foreach (var path in searchPaths)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        var lines = File.ReadAllLines(path);
                        foreach (var rawLine in lines)
                        {
                            var line = rawLine.Trim();
                            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                            {
                                continue;
                            }

                            var separatorIndex = line.IndexOf('=');
                            if (separatorIndex > 0)
                            {
                                var key = line.Substring(0, separatorIndex).Trim();
                                var val = line.Substring(separatorIndex + 1).Trim();

                                // Strip surrounding single or double quotes
                                if ((val.StartsWith("\"") && val.EndsWith("\"")) ||
                                    (val.StartsWith("'") && val.EndsWith("'")))
                                {
                                    val = val.Substring(1, val.Length - 2);
                                }

                                if (!string.IsNullOrEmpty(key))
                                {
                                    Environment.SetEnvironmentVariable(key, val);
                                }
                            }
                        }
                        break;
                    }
                }
                catch
                {
                    // Ignore disk read errors gracefully
                }
            }
        }
    }
}
