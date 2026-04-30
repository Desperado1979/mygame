using Unity.Netcode;
using UnityEngine;

public class PlayerPickupSimple : MonoBehaviour
{
    public float pickupRange = 2f;
    public LayerMask dropLayer;

    void Awake()
    {
        D3GrowthBalanceData d = D3GrowthBalance.Load();
        pickupRange = Mathf.Max(0.1f, d.interactionPickupRange);
        // Inspector default (0) means "no layers". Also include project "Drop" layer — many drop prefabs use it
        // (see Resources/Drops/Drop_Coin); mask Default-only would never OverlapSphere them.
        if (dropLayer.value == 0)
            dropLayer = BuildDefaultDropLayerMask();
    }

    static LayerMask BuildDefaultDropLayerMask()
    {
        int mask = 1 << 0; // Default (and anything else sharing the player layer for edge cases)
        int drop = LayerMask.NameToLayer("Drop");
        if (drop >= 0)
            mask |= 1 << drop;
        return mask;
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.E)) return;

        NetworkObject selfNet = GetComponent<NetworkObject>();
        if (selfNet != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                if (!selfNet.IsOwner)
                    return;
                var mp = GetComponent<MultiplayerPlayerSimple>();
                if (mp != null)
                    mp.RequestPickupE_ServerRpc();
                return;
            }
        }

        RunServerSidePickup();
    }

    /// <summary>Server / offline: overlap, grant inventory, despawn or destroy drop.</summary>
    public void RunServerSidePickup()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, pickupRange, dropLayer);
        if (hits.Length == 0)
        {
            Debug.Log("No drop in range");
            return;
        }

        // Default layer can include this player's own capsule; closest would be "self" (dist ~0) and
        // the old code would Destroy(this) after adding loot — despawn / hard freeze. Only consider hits that carry a drop.
        Collider closest = null;
        float bestSqr = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            Collider c = hits[i];
            if (c == null) continue;
            if (IsSelfCharacterCollider(c)) continue;
            DropItemSimple d = c.GetComponent<DropItemSimple>() ?? c.GetComponentInParent<DropItemSimple>();
            if (d == null) continue;
            float sqr = (c.bounds.center - transform.position).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                closest = c;
            }
        }

        if (closest == null)
        {
            Debug.Log("No drop in range (layer mask may be wrong or nothing with DropItemSimple)");
            return;
        }

        DropItemSimple drop = closest.GetComponent<DropItemSimple>() ?? closest.GetComponentInParent<DropItemSimple>();
        float w = drop.pickupWeight;
        string id = drop.pickupId;
        int count = drop.pickupCount;
        if (!drop.CanBePickedBy(transform))
        {
            ServerAuditLogSimple.Push(
                ServerAuditLogSimple.CategorySrvValPickupDenied,
                $"id={id}&reason=owner_protect&remainSec={Mathf.Max(0f, drop.ownerProtectUntil - Time.time):F1}");
            Debug.Log($"该掉落暂时受归属保护 ({Mathf.Max(0f, drop.ownerProtectUntil - Time.time):F1}s)");
            return;
        }

        PlayerInventorySimple inv = GetComponent<PlayerInventorySimple>();
        if (inv != null)
        {
            if (!inv.TryAddPickup(w, id, count))
            {
                Debug.Log("背包已满（超重），无法拾取");
                return;
            }
        }

        MultiplayerPlayerSimple mp = GetComponent<MultiplayerPlayerSimple>();
        if (mp != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            mp.MirrorInventoryAddClientRpc(w, id, count);

        Debug.Log("Picked: " + drop.name);
        NetworkObject dno = drop.GetComponent<NetworkObject>();
        if (dno != null && dno.IsSpawned)
            dno.Despawn(true);
        else
            Destroy(drop.gameObject);
    }

    bool IsSelfCharacterCollider(Collider c)
    {
        if (c == null) return true;
        return c.transform == transform || c.transform.IsChildOf(transform);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}
