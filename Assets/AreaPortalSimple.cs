using UnityEngine;

/// <summary>Week3: trigger portal for city/field switching.</summary>
public class AreaPortalSimple : MonoBehaviour
{
    public WorldZoneConfigSimple zoneConfig;
    public Transform targetPoint;
    public KeyCode useKey = KeyCode.F;
    public string prompt = "Press F to travel";
    public float useRange = 2f;
    public bool toCity = false;

    Transform player;

    void Start()
    {
        PlayerHealthSimple p = PlayerHealthSimple.Instance;
        if (p != null) player = p.transform;
    }

    void Update()
    {
        if (player == null)
        {
            PlayerHealthSimple p = PlayerHealthSimple.Instance;
            if (p != null) player = p.transform;
            return;
        }
        if (targetPoint == null) return;
        if (zoneConfig != null)
        {
            Transform cfg = toCity ? zoneConfig.citySpawnPoint : zoneConfig.fieldSpawnPoint;
            if (cfg != null) targetPoint = cfg;
        }

        if ((player.position - transform.position).sqrMagnitude > useRange * useRange)
            return;
        if (Input.GetKeyDown(useKey))
            player.position = targetPoint.position;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, useRange);
    }
}
