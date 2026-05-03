using System;
using System.IO;

namespace FTR.Core.Server.Utils;

public static class EnvironmentVariablesUtils
{
    public static void LoadFromEnvFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($".env file not found at path: {filePath}");

        foreach (var line in File.ReadAllLines(filePath))
        {
            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
                continue;

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
                continue;

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();

            Environment.SetEnvironmentVariable(key, value);
        }
    }
}
