using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FTR.Core.Common.Loaders;
using FTR.Core.Server;
using FTR.Gameplay.Common.Environment.Dialogs;
using FTR.Gameplay.Server.Environment.Quest;
using FTRShared.Runtime.Models;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Server.Loaders
{
    public class FriendlyNpcSpawnerLoader : ILoader
    {
        [Inject]
        private readonly NpcDialogRegistry npcDialogRegistry;

        [Inject]
        private readonly ServerQuestRegistry serverQuestRegistry;

        private readonly GameObject spawnerPrefab;

        private readonly IObjectResolver resolver;

        public FriendlyNpcSpawnerLoader(
            ServerPrefabProvider prefabProvider,
            IObjectResolver resolver
        )
        {
            spawnerPrefab = prefabProvider.FriendlyNpcSpawnerComponent;
            this.resolver = resolver;
        }

        public async UniTask Load(string worldId, ZoneData zoneData, CreatablesData creatablesData)
        {
            npcDialogRegistry.Populate(creatablesData.npcs, creatablesData.dialogs);
            serverQuestRegistry.Populate(creatablesData.quests);

            var npcById = BuildNpcLookup(creatablesData.npcs);
            var dialogById = BuildDialogLookup(creatablesData.dialogs);

            foreach (var spawnData in zoneData.npcSpawnAreas)
            {
                if (string.IsNullOrEmpty(spawnData.NpcId))
                {
                    Debug.LogWarning("NPCSpawnerData has no NpcId, skipping.");
                    continue;
                }

                if (!npcById.TryGetValue(spawnData.NpcId, out var npcData))
                {
                    Debug.LogWarning($"No NPCData found for NpcId '{spawnData.NpcId}', skipping.");
                    continue;
                }

                DialogData dialogData = null;
                if (
                    npcData.dialogProgression != null
                    && npcData.dialogProgression.Count > 0
                    && npcData.dialogProgression[0] != null
                    && !string.IsNullOrEmpty(npcData.dialogProgression[0].dialogId)
                )
                {
                    var dialogId = npcData.dialogProgression[0].dialogId;
                    if (!dialogById.TryGetValue(dialogId, out dialogData))
                    {
                        Debug.LogWarning(
                            $"DialogId '{dialogId}' not found for NPC '{npcData.id}'."
                        );
                    }
                }

                var instance = resolver.Instantiate(
                    spawnerPrefab,
                    new Vector3(spawnData.Position.x, spawnData.Position.y, spawnData.Position.z),
                    Quaternion.identity
                );
                instance.name = $"NPCSpawner_{npcData.id}";

                var npcSpawns = instance.GetComponent<NPCSpawns>();
                npcSpawns.Initialize(spawnData, npcData, dialogData);
            }
        }

        private static Dictionary<string, NPCData> BuildNpcLookup(List<NPCData> npcs)
        {
            var dict = new Dictionary<string, NPCData>();
            foreach (var npc in npcs)
            {
                if (npc == null || string.IsNullOrEmpty(npc.id))
                    continue;
                if (dict.ContainsKey(npc.id))
                {
                    Debug.LogWarning($"Duplicate NPCData id '{npc.id}', skipping.");
                    continue;
                }
                dict[npc.id] = npc;
            }
            return dict;
        }

        private static Dictionary<string, DialogData> BuildDialogLookup(List<DialogData> dialogs)
        {
            var dict = new Dictionary<string, DialogData>();
            foreach (var dialog in dialogs)
            {
                if (dialog == null || string.IsNullOrEmpty(dialog.id))
                    continue;
                if (dict.ContainsKey(dialog.id))
                {
                    Debug.LogWarning($"Duplicate DialogData id '{dialog.id}', skipping.");
                    continue;
                }
                dict[dialog.id] = dialog;
            }
            return dict;
        }
    }
}
