using Cysharp.Threading.Tasks;
using FTRShared.Runtime.Models;
using UnityEngine;
using VContainer;

namespace FTR.Core.Common.Loaders;

public class WorldLoaderManager
{
    [Inject]
    ILoader loader;

    private WorldData loadedWorldData;

    public UniTask<WorldData> Load()
    {
        throw new System.NotImplementedException();
    }

    public void LoadPlayerState(string playerId)
    {
        throw new System.NotImplementedException();
    }

    private Vector3 GetRandomSpawnPoint()
    {
        // Vector2 randomCircle = Random.insideUnitCircle * radius;
        // Vector3 spawnPosition =
        //     transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
        // return spawnPosition;
        throw new System.NotImplementedException();
    }
}
