using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor personalizado para LootTable que agrega botones de utilidad
/// </summary>
[CustomEditor(typeof(LootTable))]
public class LootTableEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Dibuja el inspector normal
        DrawDefaultInspector();
        
        EditorGUILayout.Space(10);
        
        LootTable lootTable = (LootTable)target;
        
        // Botones de utilidad
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("🎲 Test Roll", GUILayout.Height(30)))
        {
            TestRollLoot(lootTable);
        }
        
        if (GUILayout.Button("✓ Validate", GUILayout.Height(30)))
        {
            ValidateTable(lootTable);
        }
        
        EditorGUILayout.EndHorizontal();
        
        if (GUILayout.Button("📊 Print Stats", GUILayout.Height(30)))
        {
            lootTable.PrintStats();
        }
        
        // Información rápida
        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox(GetQuickInfo(lootTable), MessageType.Info);
    }
    
    private void TestRollLoot(LootTable lootTable)
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[LootTableEditor] Test Roll solo funciona en Play Mode!");
            return;
        }
        
        Debug.Log($"═══════════════════════════════════════════════════");
        Debug.Log($"[LootTableEditor] Testing Roll for: {lootTable.tableName}");
        Debug.Log($"═══════════════════════════════════════════════════");
        
        var items = lootTable.RollLoot();
        
        if (items.Count == 0)
        {
            Debug.Log("[LootTableEditor] ❌ No items dropped!");
        }
        else
        {
            Debug.Log($"[LootTableEditor] ✅ Dropped {items.Count} items:");
            foreach (var item in items)
            {
                Debug.Log($"  - {item.displayName}");
            }
        }
        
        Debug.Log($"═══════════════════════════════════════════════════");
    }
    
    private void ValidateTable(LootTable lootTable)
    {
        Debug.Log($"═══════════════════════════════════════════════════");
        Debug.Log($"[LootTableEditor] Validating: {lootTable.tableName}");
        Debug.Log($"═══════════════════════════════════════════════════");
        
        bool isValid = lootTable.Validate();
        
        if (isValid)
        {
            Debug.Log($"[LootTableEditor] ✅ Table is VALID!");
        }
        else
        {
            Debug.LogError($"[LootTableEditor] ❌ Table has ERRORS! Check logs above.");
        }
        
        Debug.Log($"═══════════════════════════════════════════════════");
    }
    
    private string GetQuickInfo(LootTable lootTable)
    {
        if (lootTable.lootEntries == null || lootTable.lootEntries.Count == 0)
        {
            return "⚠️ No hay entries configuradas";
        }
        
        int validEntries = 0;
        foreach (var entry in lootTable.lootEntries)
        {
            if (entry != null && entry.IsValid())
            {
                validEntries++;
            }
        }
        
        return $"📦 {validEntries}/{lootTable.lootEntries.Count} entries válidas | " +
               $"🎯 Drop chance: {lootTable.overallDropChance}% | " +
               $"📊 Items: {lootTable.minItemsDropped}-{lootTable.maxItemsDropped}";
    }
}
