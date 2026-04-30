using UnityEditor;
using UnityEngine;

/// <summary>
/// D3：保存 <c>DefaultD3Growth.json</c> 后清除 <see cref="D3GrowthBalance"/> 静态缓存。
/// 配合「Enter Play Mode Without Reloading Domain」时仍能在下次 <see cref="D3GrowthBalance.Load"/> 读到新 JSON。
/// </summary>
class DefaultD3GrowthImportProcessor : AssetPostprocessor
{
    const string JsonAssetPath = "Assets/Resources/Balance/DefaultD3Growth.json";

    static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedTo,
        string[] movedFrom)
    {
        bool touched = false;
        for (int i = 0; i < importedAssets.Length; i++)
        {
            if (Normalize(importedAssets[i]) == JsonAssetPath)
            {
                touched = true;
                break;
            }
        }

        if (!touched)
        {
            for (int i = 0; i < deletedAssets.Length; i++)
            {
                if (Normalize(deletedAssets[i]) == JsonAssetPath)
                {
                    touched = true;
                    break;
                }
            }
        }

        if (!touched)
            return;

        D3GrowthBalance.ClearLoadCache();
        Debug.Log("[D3] DefaultD3Growth.json changed — cleared D3GrowthBalance load cache.");
    }

    static string Normalize(string path)
    {
        if (string.IsNullOrEmpty(path))
            return "";
        return path.Replace('\\', '/');
    }
}
