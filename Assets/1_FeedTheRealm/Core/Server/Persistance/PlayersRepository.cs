using System.Collections.Generic;
using System.Threading.Tasks;
using FTR.Core.Server.Config;
using FTR.Core.Server.Persistance.Schemas;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace FTR.Core.Server.Persistance;

public class PlayersRepository
{
    private readonly Database db;
    private readonly IMongoCollection<PlayerDocument> collection;

    public PlayersRepository(FTR.Core.Common.Config.Config config, ServerConfig serverConfig)
    {
        this.db = new Database(serverConfig.MongoConnectionString, "world1", "1"); // TODO: get world id and zone id from config
        this.collection = db.GetCollection<PlayerDocument>("players");
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
        SavePlayerAsync(testPlayer).Wait();
    }

    public async Task SavePlayerAsync(PlayerDocument player)
    {
        var filter = Builders<PlayerDocument>.Filter.Eq(p => p.PlayerId, player.PlayerId);
        var options = new ReplaceOptions { IsUpsert = true };

        await this.collection.ReplaceOneAsync(filter, player, options);
    }

    public async Task<PlayerDocument> GetPlayerAsync(string playerId)
    {
        var filter = Builders<PlayerDocument>.Filter.Eq(p => p.PlayerId, playerId);
        return await this.collection.Find(filter).FirstOrDefaultAsync();
    }
}
