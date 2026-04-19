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
        if (zoneConfig != null)
        {
            Transform cfg = toCity ? zoneConfig.citySpawnPoint : zoneConfig.fieldSpawnPoint;
            if (cfg != null) targetPoint = cfg;
        }

        float sq = (player.position - transform.position).sqrMagnitude;
        bool inRange = sq <= useRange * useRange;
        if (targetPoint == null)
        {
            if (inRange && Input.GetKeyDown(useKey))
                ServerAuditLogSimple.Push(ServerAuditLogSimple.CategorySrvValPortalReject, "reason=no_target");
            return;
        }

        if (!inRange)
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
