#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Reflection;

namespace FTR.Gameplay.Client.Registry
{
    [CustomEditor(typeof(ClientSoundFXRegistry))]
    public class ClientSoundFXRegistryEditor : UnityEditor.Editor
    {
        private const float BUTTON_HEIGHT = 30f;
        private const float SPACING = 10f;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space(SPACING);
            EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);

            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (
                GUILayout.Button(
                    "🔧 Auto-generate Entries from Code IDs",
                    GUILayout.Height(BUTTON_HEIGHT)
                )
            )
                AutoGenerateEntries();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(5f);

            GUI.backgroundColor = new Color(0.9f, 0.7f, 0.3f);
            if (GUILayout.Button("🔍 Validate Entries", GUILayout.Height(BUTTON_HEIGHT)))
                ValidateEntries();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(5f);

            GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f);
            if (GUILayout.Button("🧹 Clear Empty Entries", GUILayout.Height(BUTTON_HEIGHT)))
                ClearEmptyEntries();
            GUI.backgroundColor = Color.white;
        }

        // The SerializedProperty names must match exactly with
        // the private fields of SoundFXEntry: "id", "clip", "delay", "volume"
        private const string PROP_ID = "id";
        private const string PROP_CLIP = "clip";
        private const string PROP_DELAY = "delay";
        private const string PROP_VOLUME = "volume";

        private static System.Collections.Generic.List<string> GetCodeIds() =>
            typeof(ClientSoundFXRegistry.SoundFXIds)
                .GetFields(
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy
                )
                .Where(f => f.IsLiteral && !f.IsInitOnly)
                .Select(f => (string)f.GetValue(null))
                .ToList();

        private void AutoGenerateEntries()
        {
            var registry = (ClientSoundFXRegistry)target;
            var so = new SerializedObject(registry);
            var entriesProp = so.FindProperty("entries");

            var idConstants = GetCodeIds();
            if (idConstants.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    "No code constants found in ClientSoundFXRegistry.SoundFXIds.",
                    "OK"
                );
                return;
            }

            var existing = new System.Collections.Generic.Dictionary<
                string,
                (AudioClip clip, float delay, float volume)
            >();

            for (int i = 0; i < entriesProp.arraySize; i++)
            {
                var ep = entriesProp.GetArrayElementAtIndex(i);
                var existingId = ep.FindPropertyRelative(PROP_ID).stringValue;
                if (string.IsNullOrEmpty(existingId))
                    continue;

                existing[existingId] = (
                    (AudioClip)ep.FindPropertyRelative(PROP_CLIP).objectReferenceValue,
                    ep.FindPropertyRelative(PROP_DELAY).floatValue,
                    ep.FindPropertyRelative(PROP_VOLUME).floatValue
                );
            }

            entriesProp.ClearArray();

            foreach (var id in idConstants)
            {
                entriesProp.InsertArrayElementAtIndex(entriesProp.arraySize);
                var ep = entriesProp.GetArrayElementAtIndex(entriesProp.arraySize - 1);

                ep.FindPropertyRelative(PROP_ID).stringValue = id;

                if (existing.TryGetValue(id, out var prev))
                {
                    ep.FindPropertyRelative(PROP_CLIP).objectReferenceValue = prev.clip;
                    ep.FindPropertyRelative(PROP_DELAY).floatValue = prev.delay;
                    ep.FindPropertyRelative(PROP_VOLUME).floatValue = prev.volume;
                }
                else
                {
                    ep.FindPropertyRelative(PROP_CLIP).objectReferenceValue = null;
                    ep.FindPropertyRelative(PROP_DELAY).floatValue = 0f;
                    ep.FindPropertyRelative(PROP_VOLUME).floatValue = 1f;
                }
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog(
                "Success",
                $"Generated {idConstants.Count} entries.\n\nIDs:\n{string.Join(", ", idConstants)}",
                "OK"
            );
        }

        private void ValidateEntries()
        {
            var registry = (ClientSoundFXRegistry)target;
            var so = new SerializedObject(registry);
            var entriesProp = so.FindProperty("entries");

            var foundIds = new System.Collections.Generic.HashSet<string>();
            var errors = new System.Collections.Generic.List<string>();
            var warnings = new System.Collections.Generic.List<string>();

            for (int i = 0; i < entriesProp.arraySize; i++)
            {
                var ep = entriesProp.GetArrayElementAtIndex(i);
                var id = ep.FindPropertyRelative(PROP_ID).stringValue;
                var clip = ep.FindPropertyRelative(PROP_CLIP).objectReferenceValue;

                if (string.IsNullOrEmpty(id))
                {
                    errors.Add($"Entry #{i}: ID is empty");
                    continue;
                }
                if (!foundIds.Add(id))
                    errors.Add($"Entry #{i}: Duplicate ID '{id}'");
                if (clip == null)
                    warnings.Add($"Entry '{id}': no entries assigned");
            }

            foreach (var codeId in GetCodeIds())
            {
                if (!foundIds.Contains(codeId))
                    errors.Add($"ID '{codeId}' defined in code but not in entries");
            }

            if (errors.Count == 0 && warnings.Count == 0)
            {
                EditorUtility.DisplayDialog("Validation", "✅ All good!", "OK");
                return;
            }

            var msg = "";
            if (errors.Count > 0)
                msg += $"❌ Errors ({errors.Count}):\n{string.Join("\n", errors)}\n\n";
            if (warnings.Count > 0)
                msg += $"⚠️ Warnings ({warnings.Count}):\n{string.Join("\n", warnings)}";

            EditorUtility.DisplayDialog("Validation", msg, "OK");
        }

        private void ClearEmptyEntries()
        {
            var registry = (ClientSoundFXRegistry)target;
            var so = new SerializedObject(registry);
            var entriesProp = so.FindProperty("entries");

            int removed = 0;
            for (int i = entriesProp.arraySize - 1; i >= 0; i--)
            {
                var ep = entriesProp.GetArrayElementAtIndex(i);
                var id = ep.FindPropertyRelative(PROP_ID).stringValue;
                var clip = ep.FindPropertyRelative(PROP_CLIP).objectReferenceValue;

                if (string.IsNullOrEmpty(id) && clip == null)
                {
                    entriesProp.DeleteArrayElementAtIndex(i);
                    removed++;
                }
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog(
                "Clear Empty Entries",
                $"Empty entries removed: {removed}.",
                "OK"
            );
        }
    }
}
#endif
