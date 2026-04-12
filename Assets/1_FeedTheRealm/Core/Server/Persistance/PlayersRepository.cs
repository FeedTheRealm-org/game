using System.Collections.Generic;
using System.Threading.Tasks;
using FTR.Core.Server.Persistance.Schemas;
using MongoDB.Driver;

namespace FTR.Core.Server.Persistance;

/// <summary>
/// Repository for managing player data in MongoDB.
/// </summary>
public class PlayersRepository
{
    private readonly Logging.Logger logger;
    private readonly Database db;

    private IMongoCollection<PlayerDocument> collection;

    public PlayersRepository(Database db, Logging.Logger logger)
    {
        this.logger = logger;
        this.db = db;
    }

    /// <summary>
    /// Initializes the MongoDB collection for player data.
    /// </summary>
    public async Task Connect(Database db)
    {
        this.collection = db.GetCollection<PlayerDocument>("players");
        this.logger.Log("Players collection initialized");
    }

    /// <summary>
    /// Saves or updates a player's data in MongoDB.
    /// </summary>
    public async Task SavePlayerAsync(PlayerDocument player)
    {
        var filter = Builders<PlayerDocument>.Filter.Eq(p => p.PlayerId, player.PlayerId);
        var options = new ReplaceOptions { IsUpsert = true };
        await this.collection.ReplaceOneAsync(filter, player, options);
        this.logger.Log($"Player {player.PlayerId} saved to MongoDB");
    }

    /// <summary>
    /// Retrieves a player's data from MongoDB by player ID.
    /// </summary>
    public async Task<PlayerDocument> GetPlayerAsync(string playerId)
    {
        var filter = Builders<PlayerDocument>.Filter.Eq(p => p.PlayerId, playerId);
        return await this.collection.Find(filter).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Saves a player's inventory and gold to MongoDB.
    /// </summary>
    public async Task SaveInventoryAsync(string playerId, List<InventoryItem> inventory, int gold)
    {
        var filter = Builders<PlayerDocument>.Filter.Eq(p => p.PlayerId, playerId);
        var update = Builders<PlayerDocument>
            .Update.Set(p => p.Inventory, inventory)
            .Set(p => p.Gold, gold);
        await this.collection.UpdateOneAsync(filter, update);
    }

    /// <summary>
    /// Saves a player's active and completed quests to MongoDB.
    /// </summary>
    public async Task SaveQuestsAsync(
        string playerId,
        List<ActiveQuest> active,
        List<string> completed
    )
    {
        var filter = Builders<PlayerDocument>.Filter.Eq(p => p.PlayerId, playerId);
        var update = Builders<PlayerDocument>
            .Update.Set(p => p.ActiveQuests, active)
            .Set(p => p.CompletedQuests, completed);
        await this.collection.UpdateOneAsync(filter, update);
    }

    /// <summary>
    /// Saves a player's last known position to MongoDB.
    /// </summary>
    public async Task SavePositionAsync(string playerId, Vec3 position)
    {
        var filter = Builders<PlayerDocument>.Filter.Eq(p => p.PlayerId, playerId);
        var update = Builders<PlayerDocument>.Update.Set(p => p.LastPosition, position);
        await this.collection.UpdateOneAsync(filter, update);
    }
}
