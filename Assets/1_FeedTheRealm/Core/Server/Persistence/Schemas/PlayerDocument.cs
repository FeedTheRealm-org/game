using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FTR.Core.Server.Persistence.Schemas;

public class PlayerModel
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string PlayerId { get; set; }

    [BsonElement("gold")]
    public int Gold { get; set; }

    [BsonElement("last_position")]
    public PositionModel LastPosition { get; set; }

    [BsonElement("inventory")]
    public List<InventoryItemModel> Inventory { get; set; } = new();

    [BsonElement("active_quests")]
    public List<QuestModel> ActiveQuests { get; set; } = new();

    [BsonElement("completed_quests")]
    public List<string> CompletedQuests { get; set; } = new();
}
