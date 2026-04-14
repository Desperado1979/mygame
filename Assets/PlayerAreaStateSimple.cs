using UnityEngine;

/// <summary>Week3: city/field area state with simple radius split.</summary>
public class PlayerAreaStateSimple : MonoBehaviour
{
    public WorldZoneConfigSimple zoneConfig;
    public Transform cityCenter;
    public float cityRadius = 16f;
    public string currentArea = "Field";

    public bool IsInCity => currentArea == "City";

    void Update()
    {
        if (zoneConfig != null)
        {
            cityCenter = zoneConfig.cityCenter;
            cityRadius = zoneConfig.cityRadius;
        }
        if (cityCenter == null)
            return;
        float sq = (transform.position - cityCenter.position).sqrMagnitude;
        currentArea = sq <= cityRadius * cityRadius ? "City" : "Field";
    }
}
