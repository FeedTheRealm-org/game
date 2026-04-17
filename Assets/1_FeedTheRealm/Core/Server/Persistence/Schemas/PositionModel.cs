using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FTR.Core.Server.Persistence.Schemas;

public class PositionModel
{
    [BsonElement("x")]
    public float X { get; set; }

    [BsonElement("y")]
    public float Y { get; set; }

    [BsonElement("z")]
    public float Z { get; set; }
}
