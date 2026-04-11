using System.Collections.Generic;
using System.Threading.Tasks;
using FTR.Core.Server.Persistance.Schemas;
using MongoDB.Driver;

namespace FTR.Core.Server.Persistance;

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
