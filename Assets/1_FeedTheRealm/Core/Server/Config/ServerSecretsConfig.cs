using System;
using System.IO;

namespace FTR.Core.Server.Config;

public class ServerSecretsConfig
{
    public string MongoConnectionString { get; private set; }

    /// <summary>
    /// Loads environment variables from ENV or from the specified .env file (if enabled) and sets the fields.
    /// </summary>
    public void LoadEnvironmentVariables(string envFilePath, bool loadFromEnvFile)
    {
        if (loadFromEnvFile)
            LoadFromEnvFile(envFilePath);

        // Set fields from env vars
        MongoConnectionString = Environment.GetEnvironmentVariable("MONGO_CONNECTION_STRING");

        // Validations
        if (string.IsNullOrEmpty(MongoConnectionString))
            throw new InvalidOperationException(
                "MONGO_CONNECTION_STRING environment variable is not set."
            );
    }

    private void LoadFromEnvFile(string filePath)
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
