using System;
using FTR.Core.Server.Utils;

namespace FTR.Core.Server.Config;

public class ServerSecretsConfig
{
    public string MongoConnectionString { get; private set; }
    public string ServerFixedToken { get; private set; }
    public string DDAgentHost { get; private set; }

    /// <summary>
    /// Loads environment variables from ENV or from the specified .env file (if enabled) and sets the fields.
    /// </summary>
    public void LoadEnvironmentVariables(string envFilePath, bool loadFromEnvFile)
    {
        if (loadFromEnvFile)
            EnvironmentVariablesUtils.LoadFromEnvFile(envFilePath);

        // Set fields from env vars
        this.MongoConnectionString = Environment.GetEnvironmentVariable("MONGO_CONNECTION_STRING");
        this.ServerFixedToken = Environment.GetEnvironmentVariable("SERVER_FIXED_TOKEN");
        this.DDAgentHost = Environment.GetEnvironmentVariable("DD_AGENT_HOST");

        // Validations
        if (string.IsNullOrEmpty(MongoConnectionString))
            throw new InvalidOperationException(
                "MONGO_CONNECTION_STRING environment variable is not set."
            );

        if (string.IsNullOrEmpty(ServerFixedToken))
            throw new InvalidOperationException(
                "SERVER_FIXED_TOKEN environment variable is not set."
            );

        if (string.IsNullOrEmpty(DDAgentHost))
            throw new InvalidOperationException("DD_AGENT_HOST environment variable is not set.");
    }
}
