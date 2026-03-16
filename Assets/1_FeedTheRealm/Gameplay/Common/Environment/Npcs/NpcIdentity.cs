using FTRShared.Runtime.Models;
using UnityEngine;

namespace FTR.Gameplay.Common.Environment.Npcs
{
    /// <summary>
    /// Common component that marks a GameObject as an NPC and exposes its unique identifier.
    /// </summary>
    public class NpcIdentity : MonoBehaviour
    {
        [SerializeField]
        private string npcId;

        public string NpcId => npcId;

        /// <summary>
        /// Called by NPCSpawns after instantiation to bind the live NPCData to this NPC.
        /// Overrides any value set in the Inspector.
        /// </summary>
        public void Initialize(NPCData data)
        {
            npcId = data.id;
            gameObject.name = $"NPC_{data.id}";
        }
    }
}
