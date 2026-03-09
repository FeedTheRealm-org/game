using FTR.Core.Common.Utils;
using UnityEngine;

namespace FTR.Core.Server.Config
{
    [CreateAssetMenu(menuName = "Scriptable Objects/Config/ServerConfig")]
    public class ServerConfig : ScriptableObject
    {
        [Header("Game Config")]
        [SerializeField]
        private float playerSpeed = 5f;
        public float PlayerSpeed => playerSpeed;

        [SerializeField]
        private uint itemDespawnTime = 120; // this is in seconds
        public uint ItemDespawnTime => itemDespawnTime;
    }
}
