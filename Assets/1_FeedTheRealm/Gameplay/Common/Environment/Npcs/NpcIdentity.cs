using FTRShared.Runtime.Models;
using Mirror;

namespace FTR.Gameplay.Common.Environment.Npcs
{
    /// <summary>
    /// Common component that marks a GameObject as an NPC and exposes its unique identifier.
    /// </summary>
    public class NpcIdentity : NetworkBehaviour, INpcIdentity
    {
        [SyncVar]
        private string npcId;

        public string NpcId => npcId;

        /// <summary>
        /// Called by NPCSpawns on the server after instantiation to bind the live NPCData.
        /// </summary>
        [Server]
        public void Initialize(NPCData data)
        {
            npcId = data?.id;
            gameObject.name = $"NPC_{data?.id}";
        }
    }
}
