using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FTR.Core.Server.Persistence;

public class Database
{
    private IMongoDatabase _db;
    private readonly Logging.Logger logger;

    private const int connectionTimeoutSeconds = 3;

    public Database(Logging.Logger logger)
    {
        this.logger = logger;
    }

    public async Task Connect(
        string connectionString,
        string worldId,
        string zoneId,
        CancellationToken cancellationToken = default
    )
    {
        var settings = MongoClientSettings.FromConnectionString(connectionString);
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(connectionTimeoutSeconds);
        settings.ConnectTimeout = TimeSpan.FromSeconds(connectionTimeoutSeconds);
        settings.SocketTimeout = TimeSpan.FromSeconds(connectionTimeoutSeconds);

        var client = new MongoClient(settings);
        var databaseName = $"world-{worldId}_zone-{zoneId}";
        var database = client.GetDatabase(databaseName);

        // Ping to check connection
        await database.RunCommandAsync<BsonDocument>(
            new BsonDocument("ping", 1),
            cancellationToken: cancellationToken
        );

        _db = database;
        this.logger.Log($"Connected to {worldId}_{zoneId} Mongo database");
    }

    public IMongoCollection<T> GetCollection<T>(string name)
    {
        if (_db == null)
            throw new InvalidOperationException("Database connection is not established.");
        return _db.GetCollection<T>(name);
    }
}
