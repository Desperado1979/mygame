using UnityEngine;

/// <summary>D4 kickoff: safe-zone placeholder. Inside zone, PvP/hostile damage is blocked.</summary>
public class SafeZoneSimple : MonoBehaviour
{
    public static SafeZoneSimple Instance { get; private set; }
    public float radius = 8f;
    public WorldZoneConfigSimple zoneConfig;
    [Tooltip("开启后，每帧从 WorldZoneConfig 覆盖中心与半径；关闭后可在当前对象直接手调。")]
    public bool syncFromZoneConfig = true;
    [Tooltip("仅在 Play 模式生效：开启时运行中持续与 WorldZoneConfig 同步；关闭时只在启动时对齐一次，之后可手调。")]
    public bool keepSyncInPlayMode = false;

    void Awake()
    {
        Instance = this;
        SyncFromConfig();
    }

    void Update()
    {
        if (syncFromZoneConfig && (!Application.isPlaying || keepSyncInPlayMode))
            SyncFromConfig();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public bool Contains(Vector3 position)
    {
        return (position - transform.position).sqrMagnitude <= radius * radius;
    }

    public static bool IsInside(Vector3 position)
    {
        return Instance != null && Instance.Contains(position);
    }

    void SyncFromConfig()
    {
        if (!syncFromZoneConfig)
            return;
        if (zoneConfig == null)
            return;
        if (zoneConfig.cityCenter != null)
            transform.position = zoneConfig.cityCenter.position;
        radius = Mathf.Max(0.5f, zoneConfig.cityRadius);
    }
}
