using MongoDB.Driver;

namespace FTR.Core.Server.Persistance;

public class Database
{
    private readonly IMongoDatabase _db;

    public Database(string connectionString, string serverId, string zoneId)
    {
        var settings = MongoClientSettings.FromConnectionString(connectionString);
        var client = new MongoClient(settings);

        _db = client.GetDatabase($"world-{serverId}_zone-{zoneId}");
    }

    public IMongoCollection<T> GetCollection<T>(string name)
    {
        return _db.GetCollection<T>(name);
    }
}
