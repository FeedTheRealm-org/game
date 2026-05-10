using System;
using System.Threading;
using System.Threading.Tasks;
using FTR.Core.Server.Config;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FTR.Core.Server.Persistence;

public class Database
{
    private IMongoDatabase _db;
    private readonly ServerConfig serverConfig;
    private readonly Logging.Logger logger;

    private const int connectionTimeoutSeconds = 3;

    public Database(ServerConfig serverConfig, Logging.Logger logger)
    {
        this.serverConfig = serverConfig;
        this.logger = logger;
    }

    public async Task Connect(
        string connectionString,
        string worldId,
        CancellationToken cancellationToken = default
    )
    {
        if (!serverConfig.PersistToDatabase)
            return;
        System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
        var settings = MongoClientSettings.FromConnectionString(connectionString);
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(connectionTimeoutSeconds);
        settings.ConnectTimeout = TimeSpan.FromSeconds(connectionTimeoutSeconds);
        settings.SocketTimeout = TimeSpan.FromSeconds(connectionTimeoutSeconds);

        var client = new MongoClient(settings);
        var databaseName = $"world_{worldId}";
        var database = client.GetDatabase(databaseName);

        // Ping to check connection
        await database.RunCommandAsync<BsonDocument>(
            new BsonDocument("ping", 1),
            cancellationToken: cancellationToken
        );

        _db = database;
        this.logger.Log($"Connected to {databaseName} Mongo database");
    }

    public IMongoCollection<T> GetCollection<T>(string name)
    {
        if (!serverConfig.PersistToDatabase)
            return null;
        if (_db == null)
            throw new InvalidOperationException("Database connection is not established.");
        return _db.GetCollection<T>(name);
    }
}
