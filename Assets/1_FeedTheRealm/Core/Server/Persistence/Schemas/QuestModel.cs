using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FTR.Core.Server.Persistence.Schemas;

public class QuestModel
{
    [BsonElement("effective_quest_id")]
    public string EffectiveQuestId { get; set; }

    [BsonElement("progress")]
    public int Progress { get; set; }
}
