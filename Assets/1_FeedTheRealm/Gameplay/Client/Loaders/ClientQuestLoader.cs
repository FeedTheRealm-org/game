using Cysharp.Threading.Tasks;
using FTR.Core.Common.Loaders;
using FTR.Gameplay.Client.Environment.Quest;
using FTRShared.Runtime.Models;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Client.Loaders
{
    /// <summary>
    /// Client-side loader that populates ClientQuestRegistry with world quest data.
    /// Analogous to ClientNpcDialogLoader — injects the registry SO directly
    /// without depending on PlayerLinker.
    /// </summary>
    public class ClientQuestLoader : ILoader
    {
        [Inject]
        private readonly ClientQuestRegistry clientQuestRegistry;

        public async UniTask Load(WorldData worldData)
        {
            if (worldData?.quests == null)
            {
                Debug.LogWarning("[ClientQuestLoader] WorldData has no quests.");
                return;
            }

            clientQuestRegistry.Populate(worldData.quests);
            Debug.Log(
                $"[ClientQuestLoader] Registry populated with {worldData.quests.Count} quest(s)."
            );

            await UniTask.CompletedTask;
        }
    }
}
