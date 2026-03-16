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

        public async UniTask Load(WorldData worldData)
        {
            var spawnpoints = worldData.playerSpawnAreas;
            playerSpawnpointManager.SetSpawnpoints(spawnpoints);
        }
    }
}
