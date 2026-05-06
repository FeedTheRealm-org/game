using System.Collections.Generic;
using UnityEngine;

namespace FTR.Gameplay.Client.Registry
{
    /// <summary>
    /// Centralized bank of AudioClips for sound effects, with access by string ID.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SoundFXRegistry",
        menuName = "Scriptable Objects/Audio/SoundFX Registry"
    )]
    public class ClientSoundFXRegistry : ScriptableObject
    {
        // -------------------------------------------------------------------------
        // Public IDs — use these constants instead of literal strings
        // -------------------------------------------------------------------------
        public static class SoundFXIds
        {
            public const string Attack = "attack";
            public const string Hit = "hit";
            public const string Death = "death";
            public const string Pickup = "pickup";
            public const string Dialog = "dialog";
            public const string QuestComplete = "quest_complete";
            public const string QuestAccept = "quest_accept";
            public const string Purchase = "purchase";
            public const string Walking = "walking";
            public const string Dash = "dash";
            public const string OpenUI = "open_ui";
            public const string CloseUI = "close_ui";
            public const string SettingsOpen = "settings_open";
            public const string EnteredTeleport = "entered_teleport";
            public const string ChestOpen = "chest_open";
            public const string ChestClose = "chest_close";
            public const string Spawn = "spawn";
        }

        [SerializeField]
        private List<SoundFXEntry> entries = new List<SoundFXEntry>();

        private Dictionary<string, SoundFXEntry> entryById;

        private void BuildMapIfNeeded()
        {
            if (entryById != null)
                return;

            entryById = new Dictionary<string, SoundFXEntry>();
            foreach (var entry in entries)
            {
                if (string.IsNullOrEmpty(entry.Id))
                {
                    Debug.LogWarning("[ClientSoundFXRegistry] Empty ID found.");
                    continue;
                }
                if (entryById.ContainsKey(entry.Id))
                {
                    Debug.LogWarning(
                        $"[ClientSoundFXRegistry] Duplicated ID '{entry.Id}', using first."
                    );
                    continue;
                }
                entryById[entry.Id] = entry;
            }
        }

        private void OnEnable() => entryById = null;

        public AudioClip GetSoundFXById(string id)
        {
            BuildMapIfNeeded();
            if (string.IsNullOrEmpty(id))
                return null;

            entryById.TryGetValue(id, out var entry);
            return entry?.Clip;
        }

        public SoundFXEntry GetEntryById(string id)
        {
            BuildMapIfNeeded();
            if (string.IsNullOrEmpty(id))
                return null;

            entryById.TryGetValue(id, out var entry);
            return entry;
        }

        public void RegisterSoundFX(string id, AudioClip clip)
        {
            BuildMapIfNeeded();
            if (string.IsNullOrEmpty(id) || clip == null)
            {
                Debug.LogWarning($"[ClientSoundFXRegistry] Id or invalid clip. Id: {id}");
                return;
            }
            Debug.LogWarning(
                "[ClientSoundFXRegistry] RegisterSoundFX at runtime does not persist delay/volume settings."
            );
        }
    }
}
