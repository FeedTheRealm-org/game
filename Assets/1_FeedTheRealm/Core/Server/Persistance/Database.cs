using System;
using MongoDB.Driver;

namespace FTR.Core.Server.Persistance;

public class Database
{
    private IMongoDatabase _db;
    private readonly Logging.Logger logger;

    public Database(Logging.Logger logger)
    {
        this.logger = logger;
    }

    public void Connect(string connectionString, string worldId, string zoneId)
    {
        this.logger.Log($"Connected to {worldId}_{zoneId} Mongo database");
        var settings = MongoClientSettings.FromConnectionString(connectionString);
        var client = new MongoClient(settings);

        _db = client.GetDatabase($"world-{worldId}_zone-{zoneId}");
    }

    public IMongoCollection<T> GetCollection<T>(string name)
    {
        if (_db == null)
            throw new InvalidOperationException(
                "Database connection is not established. Call Connect() first."
            );
        return _db.GetCollection<T>(name);
    }
}
