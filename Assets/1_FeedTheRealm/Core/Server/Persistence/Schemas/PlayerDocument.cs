using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FTR.Core.Server.Persistence.Schemas;

public class PlayerDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string PlayerId { get; set; }

    [BsonElement("gold")]
    public int Gold { get; set; }

    [BsonElement("last_position")]
    public Vec3 LastPosition { get; set; }

    [BsonElement("inventory")]
    public List<InventoryItem> Inventory { get; set; } = new();

    [BsonElement("active_quests")]
    public List<ActiveQuest> ActiveQuests { get; set; } = new();

    [BsonElement("completed_quests")]
    public List<string> CompletedQuests { get; set; } = new();
}

public class Vec3
{
    [BsonElement("x")]
    public float X { get; set; }

    [BsonElement("y")]
    public float Y { get; set; }

    [BsonElement("z")]
    public float Z { get; set; }
}

public class InventoryItem
{
    [BsonElement("item_id")]
    public string ItemId { get; set; }

    [BsonElement("quantity")]
    public int Quantity { get; set; }

    [BsonElement("slot")]
    public int Slot { get; set; }
}

public class ActiveQuest
{
    [BsonElement("quest_id")]
    public string QuestId { get; set; }

    [BsonElement("progress")]
    public int Progress { get; set; }
}
