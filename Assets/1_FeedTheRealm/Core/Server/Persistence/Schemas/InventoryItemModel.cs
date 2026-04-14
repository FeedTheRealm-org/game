using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FTR.Core.Server.Persistence.Schemas;

public class InventoryItemModel
{
    [BsonElement("item_id")]
    public string ItemId { get; set; }

    [BsonElement("quantity")]
    public int Quantity { get; set; }

    [BsonElement("slot")]
    public int Slot { get; set; }
}
