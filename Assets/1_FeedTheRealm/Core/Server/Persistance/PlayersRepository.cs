using System.Collections.Generic;
using System.Threading.Tasks;
using FTR.Core.Server.Config;
using FTR.Core.Server.Persistance.Schemas;
using MongoDB.Driver;

namespace FTR.Core.Server.Persistance;

public class PlayersRepository
{
    private readonly Database db;
    private readonly IMongoCollection<PlayerDocument> collection;
    private readonly Logging.Logger logger;

    public PlayersRepository(
        FTR.Core.Common.Config.Config config,
        ServerConfig serverConfig,
        Logging.Logger logger
    )
    {
        this.logger = logger;

        string worldId = "world1";
        string zoneId = "1";
        this.logger.Log(
            $"Initializing PlayersRepository with worldId: {worldId}, zoneId: {zoneId}"
        );

        this.db = new Database(serverConfig.MongoConnectionString, worldId, zoneId); // TODO: get world id and zone id from config
        this.logger.Log($"Connected to {worldId}_{zoneId} Mongo database");

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
        _ = SavePlayerAsync(testPlayer);
        this.logger.Log("Test player document saved to MongoDB");
    }

    public async Task SavePlayerAsync(PlayerDocument player)
    {
        this.logger.Log($"Saving player {player.PlayerId} to MongoDB");
        var filter = Builders<PlayerDocument>.Filter.Eq(p => p.PlayerId, player.PlayerId);
        this.logger.Log($"Filter for player {player.PlayerId} created");
        var options = new ReplaceOptions { IsUpsert = true };
        this.logger.Log(
            $"ReplaceOptions for player {player.PlayerId} created with IsUpsert: {options.IsUpsert}"
        );

        await this.collection.ReplaceOneAsync(filter, player, options);
        this.logger.Log($"Player {player.PlayerId} saved to MongoDB");
    }

    public async Task<PlayerDocument> GetPlayerAsync(string playerId)
    {
        var filter = Builders<PlayerDocument>.Filter.Eq(p => p.PlayerId, playerId);
        return await this.collection.Find(filter).FirstOrDefaultAsync();
    }
}
