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
    /// </summary>
    public class ClientQuestLoader : ILoader
    {
        [Inject]
        private readonly ClientQuestRegistry clientQuestRegistry;

        public async UniTask Load(string world_id, ZoneData zoneData, CreatablesData creatablesData)
        {
            if (creatablesData?.quests == null)
            {
                Debug.LogWarning("[ClientQuestLoader] CreatablesData has no quests.");
                return;
            }

            clientQuestRegistry.Populate(creatablesData.quests);
            Debug.Log(
                $"[ClientQuestLoader] Registry populated with {creatablesData.quests.Count} quest(s)."
            );

            await UniTask.CompletedTask;
        }
    }
}
