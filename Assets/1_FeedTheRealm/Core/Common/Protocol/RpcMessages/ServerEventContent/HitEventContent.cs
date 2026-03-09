using System.IO;

namespace FTR.Core.Common.Protocol.RpcMessages
{
    /// <summary>
    /// Serializable content for a HitEvent sent from the server to all clients.
    /// Contains the target entity's netId and updated health values so every client
    /// can update the floating health bar.
    /// </summary>
    public class HitEventContent
    {
        public uint TargetNetId { get; set; }
        public float CurrentHealth { get; set; }
        public float MaxHealth { get; set; }

        public byte[] ToBytes()
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write(TargetNetId);
            bw.Write(CurrentHealth);
            bw.Write(MaxHealth);
            return ms.ToArray();
        }

        public static HitEventContent FromBytes(byte[] bytes)
        {
            using var ms = new MemoryStream(bytes);
            using var br = new BinaryReader(ms);
            return new HitEventContent
            {
                TargetNetId = br.ReadUInt32(),
                CurrentHealth = br.ReadSingle(),
                MaxHealth = br.ReadSingle(),
            };
        }
    }
}
