using Cysharp.Threading.Tasks;
using FTR.Core.Common.Loaders;
using FTR.Gameplay.Common.Environment.Dialogs;
using FTRShared.Runtime.Models;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Client.Loaders
{
    /// <summary>
    /// Client-side loader that populates NpcDialogRegistry with world data.
    /// </summary>
    public class ClientNpcDialogLoader : ILoader
    {
        [Inject]
        private readonly NpcDialogRegistry npcDialogRegistry;

        public async UniTask Load(WorldData worldData)
        {
            if (worldData.npcs == null || worldData.dialogs == null)
            {
                Debug.LogWarning(
                    "[ClientNpcDialogLoader] WorldData is missing npcs or dialogs, registry will be empty."
                );
                return;
            }

            npcDialogRegistry.Populate(worldData.npcs, worldData.dialogs);
            Debug.Log(
                $"[ClientNpcDialogLoader] Registry populated with {worldData.npcs.Count} NPC(s) and {worldData.dialogs.Count} dialog(s)."
            );

            await UniTask.CompletedTask;
        }
    }
}
