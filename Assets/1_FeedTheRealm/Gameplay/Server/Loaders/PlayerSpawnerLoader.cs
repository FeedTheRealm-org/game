using Cysharp.Threading.Tasks;
using FTR.Core.Common.Loaders;
using FTRShared.Runtime.Models;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Server.Loaders
{
    public class PlayerSpawnerLoader : MonoBehaviour, ILoader
    {
        [Inject]
        PlayerSpawnpointManager playerSpawnpointManager;

        public async UniTask Load(string worldId, ZoneData zoneData, CreatablesData creatablesData)
        {
            var spawnpoints = zoneData.playerSpawnAreas;
            playerSpawnpointManager.SetSpawnpoints(spawnpoints);
        }
    }
}
