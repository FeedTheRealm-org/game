using System.Collections.Generic;
using System.Threading.Tasks;
using FTR.Core.Server.Config;
using FTR.Core.Server.Persistence.Schemas;
using MongoDB.Driver;

namespace FTR.Core.Server.Persistence;

/// <summary>
/// Repository for managing player data in MongoDB.
/// </summary>
public class PlayersRepository
{
    private readonly ServerConfig serverConfig;
    private readonly Logging.Logger logger;
    private readonly Database db;

    private IMongoCollection<PlayerModel> collection;

    public PlayersRepository(Database db, ServerConfig serverConfig, Logging.Logger logger)
    {
        this.serverConfig = serverConfig;
        this.logger = logger;
        this.db = db;
    }

    /// <summary>
    /// Initializes the MongoDB collection for player data.
    /// </summary>
    public async Task Connect(Database db)
    {
        if (!serverConfig.PersistToDatabase)
            return;
        this.collection = db.GetCollection<PlayerModel>("players");
        this.logger.Log("Players collection initialized");
    }

    /// <summary>
    /// Saves or updates a player's data in MongoDB.
    /// </summary>
    public async Task SavePlayerAsync(PlayerModel player)
    {
        if (!serverConfig.PersistToDatabase)
            return;
        var filter = Builders<PlayerModel>.Filter.Eq(p => p.PlayerId, player.PlayerId);
        var options = new ReplaceOptions { IsUpsert = true };
        await this.collection.ReplaceOneAsync(filter, player, options);
        this.logger.Log($"Player {player.PlayerId} saved to MongoDB");
    }

    /// <summary>
    /// Retrieves a player's data from MongoDB by player ID.
    /// </summary>
    public async Task<PlayerModel> GetPlayerAsync(string playerId)
    {
        if (!serverConfig.PersistToDatabase)
            return null;
        var filter = Builders<PlayerModel>.Filter.Eq(p => p.PlayerId, playerId);
        return await this.collection.Find(filter).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Saves a player's inventory and gold to MongoDB.
    /// </summary>
    public async Task SaveInventoryAsync(string playerId, List<InventoryItemModel> inventory)
    {
        if (!serverConfig.PersistToDatabase)
            return;
        var filter = Builders<PlayerModel>.Filter.Eq(p => p.PlayerId, playerId);
        var update = Builders<PlayerModel>.Update.Set(p => p.Inventory, inventory);
        await this.collection.UpdateOneAsync(filter, update);
    }

    /// <summary>
    /// Saves a player's fast access inventory to MongoDB.
    /// </summary>
    public async Task SaveFastAccessAsync(string playerId, List<InventoryItemModel> fastAccess)
    {
        if (!serverConfig.PersistToDatabase)
            return;
        var filter = Builders<PlayerModel>.Filter.Eq(p => p.PlayerId, playerId);
        var update = Builders<PlayerModel>.Update.Set(p => p.FastAccessInventory, fastAccess);
        await this.collection.UpdateOneAsync(filter, update);
    }

    /// <summary>
    /// Saves a player's gold amount to MongoDB.
    /// </summary>
    public async Task SaveGoldAsync(string playerId, int goldAmount)
    {
        if (!serverConfig.PersistToDatabase)
            return;
        var filter = Builders<PlayerModel>.Filter.Eq(p => p.PlayerId, playerId);
        var update = Builders<PlayerModel>.Update.Set(p => p.Gold, goldAmount);
        await this.collection.UpdateOneAsync(filter, update);
    }

    /// <summary>
    /// Saves a player's active and completed quests to MongoDB.
    /// </summary>
    public async Task SaveQuestAsync(
        string playerId,
        string effectiveQuestId,
        int progress,
        bool completed
    )
    {
        if (!serverConfig.PersistToDatabase)
            return;

        if (completed)
        {
            var filter = Builders<PlayerModel>.Filter.Eq(p => p.PlayerId, playerId);
            var update = Builders<PlayerModel>
                .Update.PullFilter(p => p.ActiveQuests, q => q.EffectiveQuestId == effectiveQuestId)
                .AddToSet(p => p.CompletedQuests, effectiveQuestId);

            await this.collection.UpdateOneAsync(filter, update);
        }
        else
        {
            var filter = Builders<PlayerModel>.Filter.And(
                Builders<PlayerModel>.Filter.Eq(p => p.PlayerId, playerId),
                Builders<PlayerModel>.Filter.ElemMatch(
                    p => p.ActiveQuests,
                    q => q.EffectiveQuestId == effectiveQuestId
                )
            );

            var update = Builders<PlayerModel>.Update.Set("active_quests.$.progress", progress);

            await this.collection.UpdateOneAsync(filter, update);
        }
    }

    /// <summary>
    /// Saves a player's last known position to MongoDB.
    /// </summary>
    public async Task SavePositionAsync(string playerId, PositionModel position)
    {
        if (!serverConfig.PersistToDatabase)
            return;
        var filter = Builders<PlayerModel>.Filter.Eq(p => p.PlayerId, playerId);
        var update = Builders<PlayerModel>.Update.Set(p => p.LastPosition, position);
        await this.collection.UpdateOneAsync(filter, update);
    }
}
