#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Bakes <see cref="NetPlayerHostFactory"/> output to a real prefab under Resources (preferred for NGO PlayerPrefab).</summary>
public static class NetPlayerHostPrefabBaker
{
    public const string PrefabAssetPath = "Assets/Resources/Netcode/NetPlayerHost.prefab";

    [MenuItem("Tools/EpochOfDawn/Netcode/Bake NetPlayerHost Prefab", priority = 100)]
    public static void BakeNetPlayerHostPrefab()
    {
        GameObject instance = NetPlayerHostFactory.CreateRuntimePlayerTemplate(
            Vector3.zero,
            parent: null,
            hideInHierarchy: false,
            objectName: "NetPlayerHost");

        string dir = Path.GetDirectoryName(PrefabAssetPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, PrefabAssetPath);
        Object.DestroyImmediate(instance);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        Debug.Log("[MP] Baked Net Player prefab: " + PrefabAssetPath + " (Resources load path: " + NetPlayerHostFactory.ResourcesPath + ")");
    }

    public static bool EnsureBakedPrefabReady(bool autoBakeIfMissingOrInvalid)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabAssetPath);
        bool ready = IsValidNetPlayerHostPrefab(prefab);
        if (ready || !autoBakeIfMissingOrInvalid)
            return ready;

        BakeNetPlayerHostPrefab();
        prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabAssetPath);
        return IsValidNetPlayerHostPrefab(prefab);
    }

    static bool IsValidNetPlayerHostPrefab(GameObject prefab)
    {
        if (prefab == null)
            return false;
        if (prefab.GetComponent<Unity.Netcode.NetworkObject>() == null)
            return false;
        if (prefab.GetComponent<MultiplayerPlayerSimple>() == null)
            return false;
        return true;
    }
}
#endif
