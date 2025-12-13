using UnityEngine;
using UnityEditor;
using System.Linq;

public static class ItemRegistryBuilder
{
    [MenuItem("Tools/Build Item Registry from Assets/items")] 
    public static void BuildRegistry()
    {
        string searchFolder = "Assets/items";
        string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { searchFolder });

        var items = guids.Select(g => AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(g)))
                         .Where(x => x != null)
                         .ToArray();

        string resourcesFolder = "Assets/Resources";
        if (!AssetDatabase.IsValidFolder(resourcesFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        string outPath = resourcesFolder + "/ItemRegistry.asset";
        ItemRegistryData registry = AssetDatabase.LoadAssetAtPath<ItemRegistryData>(outPath);
        if (registry == null)
        {
            registry = ScriptableObject.CreateInstance<ItemRegistryData>();
            AssetDatabase.CreateAsset(registry, outPath);
        }

        registry.items = items;
        EditorUtility.SetDirty(registry);
        AssetDatabase.SaveAssets();

        Debug.Log($"ItemRegistry built: {items.Length} items (from {searchFolder}) -> {outPath}");
    }

    [InitializeOnLoadMethod]
    private static void AutoBuildOnCompile()
    {
        // Build registry automatically when scripts compile in the Editor to avoid manual step.
        BuildRegistry();
    }
}
