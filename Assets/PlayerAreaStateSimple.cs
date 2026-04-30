using UnityEngine;

/// <summary>Week3: city/field area state with simple radius split.</summary>
public class PlayerAreaStateSimple : MonoBehaviour
{
    public WorldZoneConfigSimple zoneConfig;
    public Transform cityCenter;
    public float cityRadius = 16f;
    public string currentArea = "Field";
    float _nextAutoBindAt;
    float _zonePollSec = 1f;

    public bool IsInCity => currentArea == "City";

    void Awake()
    {
        D3GrowthBalanceData d = D3GrowthBalance.Load();
        cityRadius = Mathf.Max(0.5f, d.worldCityRadiusDefault);
        _zonePollSec = Mathf.Max(0.2f, d.playerAreaZoneConfigPollSec);
    }

    void Update()
    {
        if (zoneConfig == null && Time.unscaledTime >= _nextAutoBindAt)
        {
            _nextAutoBindAt = Time.unscaledTime + _zonePollSec;
            zoneConfig = FindObjectOfType<WorldZoneConfigSimple>(true);
        }
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
