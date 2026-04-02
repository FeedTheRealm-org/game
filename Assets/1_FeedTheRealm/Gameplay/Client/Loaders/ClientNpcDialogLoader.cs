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

        public async UniTask Load(string worldId, ZoneData zoneData, CreatablesData creatablesData)
        {
            if (creatablesData.npcs == null || creatablesData.dialogs == null)
            {
                Debug.LogWarning(
                    "[ClientNpcDialogLoader] CreatablesData is missing npcs or dialogs, registry will be empty."
                );
                return;
            }

            npcDialogRegistry.Populate(creatablesData.npcs, creatablesData.dialogs);
            Debug.Log(
                $"[ClientNpcDialogLoader] Registry populated with {creatablesData.npcs.Count} NPC(s) and {creatablesData.dialogs.Count} dialog(s)."
            );

            await UniTask.CompletedTask;
        }
    }
}
