using Mirror;
using Models;
using UnityEngine;

/// <summary>
///  Serializer for StructureData over the network
/// </summary>
public static class StructureDataSerializer
{
    public static void WriteStructureData(this NetworkWriter writer, StructureData data)
    {
        writer.WriteString(data.id);
        writer.WriteString(data.structureName);
        writer.WriteVector3(data.size);
        writer.WriteVector3(data.rotation);
        writer.WriteVector3(data.offset);
        writer.WriteVector3(data.position);
        writer.WriteBool(data.isShop);
        writer.WriteVector3(data.colliderSize);
        writer.WriteVector3(data.colliderCenter);
    }

    public static StructureData ReadStructureData(this NetworkReader reader)
    {
        return new StructureData(
            reader.ReadString(),
            reader.ReadString(),
            reader.ReadVector3(),
            reader.ReadVector3(),
            reader.ReadVector3(),
            reader.ReadVector3(),
            reader.ReadBool(),
            reader.ReadVector3(),
            reader.ReadVector3()
        );
    }
}
