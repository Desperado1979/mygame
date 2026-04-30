using UnityEngine;

/// <summary>D4: shared city/field config for area + portal + safezone consistency.</summary>
public class WorldZoneConfigSimple : MonoBehaviour
{
    public Transform cityCenter;
    public float cityRadius = 16f;
    public Transform citySpawnPoint;
    public Transform fieldSpawnPoint;

    void Awake()
    {
        cityRadius = Mathf.Max(0.5f, D3GrowthBalance.Load().worldCityRadiusDefault);
    }
}
