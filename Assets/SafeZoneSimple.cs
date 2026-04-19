using UnityEngine;

/// <summary>D4 kickoff: safe-zone placeholder. Inside zone, PvP/hostile damage is blocked.</summary>
public class SafeZoneSimple : MonoBehaviour
{
    public static SafeZoneSimple Instance { get; private set; }
    public float radius = 8f;
    public WorldZoneConfigSimple zoneConfig;

    void Awake()
    {
        Instance = this;
        SyncFromConfig();
    }

    void Update()
    {
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
        if (zoneConfig == null)
            return;
        if (zoneConfig.cityCenter != null)
            transform.position = zoneConfig.cityCenter.position;
        radius = Mathf.Max(0.5f, zoneConfig.cityRadius);
    }
}
