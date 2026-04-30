#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>Play 前守门：确保 NetPlayerHost prefab 已烘焙且可用于 NGO PlayerPrefab。</summary>
[InitializeOnLoad]
public static class NetPlayerHostPlayModeGuard
{
    const bool AutoBakeOnPlay = true;

    static NetPlayerHostPlayModeGuard()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode)
            return;

        bool ready = NetPlayerHostPrefabBaker.EnsureBakedPrefabReady(AutoBakeOnPlay);
        if (ready)
            return;

        EditorApplication.isPlaying = false;
        Debug.LogError(
            "[MP] NetPlayerHost prefab is missing or invalid. Play was cancelled. " +
            "Run: Tools/EpochOfDawn/Netcode/Bake NetPlayerHost Prefab.");
    }
}
#endif
