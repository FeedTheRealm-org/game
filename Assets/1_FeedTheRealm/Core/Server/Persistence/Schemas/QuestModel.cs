using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FTR.Core.Server.Persistence.Schemas;

public class QuestModel
{
    [BsonElement("quest_id")]
    public string QuestId { get; set; }

    [BsonElement("progress")]
    public int Progress { get; set; }
}
