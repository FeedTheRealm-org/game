using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FTR.Core.Common.Loaders;
using FTR.Gameplay.Common.Environment.Dialogs;
using FTRShared.Runtime.Models;
using UnityEngine;

namespace FTR.Gameplay.Server.Loaders
{
    public class FriendlyNpcSpawnerLoader : MonoBehaviour, ILoader
    {
        [SerializeField]
        private GameObject spawnerPrefab;

        [SerializeField]
        private NpcDialogRegistry npcDialogRegistry;

        public async UniTask Load(WorldData worldData)
        {
            npcDialogRegistry.Populate(worldData.npcs, worldData.dialogs);

            var npcById = BuildNpcLookup(worldData.npcs);
            var dialogById = BuildDialogLookup(worldData.dialogs);

            foreach (var spawnData in worldData.npcSpawnAreas)
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
                if (npcData.npcDialog != null && !string.IsNullOrEmpty(npcData.npcDialog.dialogId))
                {
                    if (!dialogById.TryGetValue(npcData.npcDialog.dialogId, out dialogData))
                    {
                        Debug.LogWarning(
                            $"DialogId '{npcData.npcDialog.dialogId}' not found for NPC '{npcData.id}'."
                        );
                    }
                }

                var instance = Instantiate(
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
