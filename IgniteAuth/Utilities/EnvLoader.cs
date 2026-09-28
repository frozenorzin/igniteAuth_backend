using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IgniteAuth.Utilities;

/// <summary>
/// Simple helper class to load and parse .env files
/// </summary>
public static class EnvLoader
{
    /// <summary>
    /// Loads environment variables from a .env file at the solution root
    /// </summary>
    public static void LoadFromEnvFile()
    {
        // Try to find .env file at solution root (typically parent of bin/Debug or bin/Release)
        string? envFilePath = FindEnvFile();

        if (!string.IsNullOrEmpty(envFilePath) && File.Exists(envFilePath))
        {
            try
            {
                var lines = File.ReadAllLines(envFilePath);
                foreach (var line in lines)
                {
                    // Skip empty lines and comments
                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                        continue;

                    // Parse KEY=VALUE format
                    var parts = line.Split('=', 2);
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Trim();
                        var value = parts[1].Trim();

                        // Only set if not already set by actual environment variables
                        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                        {
                            Environment.SetEnvironmentVariable(key, value);
                        }
                    }
                }
                Console.WriteLine($"✓ Loaded environment variables from: {envFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Warning: Failed to load .env file from {envFilePath}: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("⚠ Warning: .env file not found. Using environment variables or defaults.");
        }
    }

    /// <summary>
    /// Tries to find the .env file by walking up from the current directory
    /// </summary>
    private static string? FindEnvFile()
    {
        // Start from the current directory and walk up
        var currentDir = new DirectoryInfo(AppContext.BaseDirectory);

        while (currentDir != null)
        {
            var envFile = Path.Combine(currentDir.FullName, ".env");
            if (File.Exists(envFile))
            {
                return envFile;
            }

            // Go up one directory
            currentDir = currentDir.Parent;

            // Prevent infinite loop - stop at root or after reasonable depth
            if (currentDir?.FullName == currentDir?.Parent?.FullName)
                break;
        }

        return null;
    }
}
