using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class FolderScriptIconAssigner
{
    private const string TargetFolder = "Assets/1_FeedTheRealm/";
    private const string IconPath = "Assets/Editor/icon.jpeg";

    static FolderScriptIconAssigner()
    {
        EditorApplication.delayCall += AssignIcons;
    }

    private static void AssignIcons()
    {
        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
        if (icon == null)
        {
            Debug.LogWarning("Icon not found at: " + IconPath);
            return;
        }

        var guids = AssetDatabase.FindAssets("t:MonoScript", new[] { TargetFolder });

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);

            if (script != null)
            {
                EditorGUIUtility.SetIconForObject(script, icon);
            }
        }

        Debug.Log("Icons assigned to scripts in " + TargetFolder);
    }
}
