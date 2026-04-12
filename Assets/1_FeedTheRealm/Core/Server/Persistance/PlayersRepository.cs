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

        // save basic player document to test
        var testPlayer = new PlayerDocument
        {
            PlayerId = "player1",
            Gold = 100,
            LastPosition = new Vec3
            {
                X = 0,
                Y = 0,
                Z = 0,
            },
            Inventory = new List<InventoryItem>
            {
                new InventoryItem
                {
                    ItemId = "sword",
                    Quantity = 1,
                    Slot = 0,
                },
                new InventoryItem
                {
                    ItemId = "potion",
                    Quantity = 5,
                    Slot = 1,
                },
            },
            ActiveQuests = new List<ActiveQuest>
            {
                new ActiveQuest { QuestId = "quest1", Progress = 50 },
            },
            CompletedQuests = new List<string> { "quest0" },
        };

        await SavePlayerAsync(testPlayer);
        testPlayer.Gold += 50;
        await SaveInventoryAsync(testPlayer.PlayerId, testPlayer.Inventory, testPlayer.Gold);
        testPlayer.LastPosition = new Vec3
        {
            X = 10,
            Y = 0,
            Z = 5,
        };
        await SavePositionAsync(testPlayer.PlayerId, testPlayer.LastPosition);
        testPlayer.ActiveQuests.Add(new ActiveQuest { QuestId = "quest2", Progress = 0 });
        await SaveQuestsAsync(
            testPlayer.PlayerId,
            testPlayer.ActiveQuests,
            testPlayer.CompletedQuests
        );

        var retrievedPlayer = await GetPlayerAsync(testPlayer.PlayerId);
        if (retrievedPlayer == null)
        {
            this.logger.Log("Failed to retrieve test player from MongoDB");
            return;
        }

        this.logger.Log(
            $"Test player document saved to MongoDB: {retrievedPlayer.PlayerId}, Gold: {retrievedPlayer.Gold}, Position: ({retrievedPlayer.LastPosition.X}, {retrievedPlayer.LastPosition.Y}, {retrievedPlayer.LastPosition.Z}), Inventory Count: {retrievedPlayer.Inventory.Count}, Active Quests Count: {retrievedPlayer.ActiveQuests.Count}, Completed Quests Count: {retrievedPlayer.CompletedQuests.Count}"
        );
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
