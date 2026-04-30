using Unity.Netcode;
using UnityEngine;

/// <summary>Week3: trigger portal for city/field switching.</summary>
public class AreaPortalSimple : MonoBehaviour
{
    public WorldZoneConfigSimple zoneConfig;
    public Transform targetPoint;
    public KeyCode useKey = KeyCode.F;
    public string prompt = "Press F to travel";
    public float useRange = 2f;
    [Tooltip("与竖直门心（如 y=2）+ 地面角色（y≈0～1）同一套距离：用 XZ 水平距离 + 竖直容差。纯 3D 球会导致「去野外」点够不着 2m。")]
    [Min(0.5f)] public float yTolerance = 4f;
    public bool toCity = false;

    void Awake()
    {
        D3GrowthBalanceData d = D3GrowthBalance.Load();
        useRange = Mathf.Max(0.1f, d.portalUseRangeDefault);
        yTolerance = Mathf.Max(0.5f, d.portalYToleranceDefault);
    }

    /// <summary>
    /// 纯 Client 会晚于连上才关掉场景单机体；<see cref="PlayerHealthSimple.Instance"/> 可能先被单机占住。
    /// 若此处只缓存一次 <c>player</c>，会永远对着「无 MultiplayerPlayerSimple 的旧 Capsule」发 F，Host 正常、Client 不传。
    /// </summary>
    static bool TryResolveLocalActingPlayerTransform(out Transform playerTransform)
    {
        playerTransform = null;
        NetworkManager nm = NetworkManager.Singleton;
        if (nm != null && nm.IsListening && nm.LocalClient != null && nm.LocalClient.PlayerObject != null)
        {
            NetworkObject po = nm.LocalClient.PlayerObject;
            PlayerHealthSimple ph = po.GetComponent<PlayerHealthSimple>();
            if (ph == null)
                ph = po.GetComponentInChildren<PlayerHealthSimple>();
            playerTransform = ph != null ? ph.transform : po.transform;
            return true;
        }

        PlayerHealthSimple p = PlayerHealthSimple.Instance;
        if (p == null)
            return false;
        playerTransform = p.transform;
        return true;
    }

    void Update()
    {
        if (!TryResolveLocalActingPlayerTransform(out Transform player))
            return;
        if (zoneConfig != null)
        {
            Transform cfg = toCity ? zoneConfig.citySpawnPoint : zoneConfig.fieldSpawnPoint;
            if (cfg != null) targetPoint = cfg;
        }

        bool inRange = IsInUseRangeForPortal(player.position, transform.position, useRange, yTolerance, serverSlack: false);
        if (targetPoint == null)
        {
            if (inRange && Input.GetKeyDown(useKey))
                ServerAuditLogSimple.Push(ServerAuditLogSimple.CategorySrvValPortalReject, "reason=no_target");
            return;
        }

        if (!inRange)
            return;
        if (!Input.GetKeyDown(useKey))
            return;

        MultiplayerPlayerSimple net = player.GetComponent<MultiplayerPlayerSimple>();
        if (net != null)
        {
            if (net.IsOwner)
                net.TeleportViaAreaPortalServerRpc(transform.position, useRange, targetPoint.position, yTolerance);
            return;
        }

        player.position = targetPoint.position;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 c = transform.position;
        Gizmos.DrawLine(c + new Vector3(-useRange, 0f, 0f), c + new Vector3(useRange, 0f, 0f));
        Gizmos.DrawLine(c + new Vector3(0f, 0f, -useRange), c + new Vector3(0f, 0f, useRange));
    }

    /// <summary>水平 (XZ) 在 <paramref name="useRange"/> 内，且与传送点 Y 差不超过 <paramref name="yTolerance"/>。</summary>
    public static bool IsInUseRangeForPortal(
        Vector3 playerPos, Vector3 portalPos, float useRange, float yTolerance,
        bool serverSlack)
    {
        if (yTolerance < 0.5f)
            yTolerance = Mathf.Max(0.5f, D3GrowthBalance.Load().portalYToleranceDefault);
        // 纯 Client 时权威位在服端，NetworkTransform 下常与本地观感差一截；Host 本机同进程反而容易「刚好在圈内」
        float serverSideSlackMul = Mathf.Max(1f, D3GrowthBalance.Load().portalServerSideRangeSlackMul);
        float r = Mathf.Max(0.1f, useRange) * (serverSlack ? serverSideSlackMul : 1f);
        float dx = playerPos.x - portalPos.x;
        float dz = playerPos.z - portalPos.z;
        if (dx * dx + dz * dz > r * r)
            return false;
        return Mathf.Abs(playerPos.y - portalPos.y) <= yTolerance;
    }
}
