using UnityEngine;

public class PlayerPickupSimple : MonoBehaviour
{
    public float pickupRange = 2f;
    public LayerMask dropLayer;

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.E)) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, pickupRange, dropLayer);
        if (hits.Length == 0)
        {
            Debug.Log("No drop in range");
            return;
        }

        Collider closest = hits[0];
        float bestDist = Vector3.Distance(transform.position, closest.transform.position);

        for (int i = 1; i < hits.Length; i++)
        {
            float dist = Vector3.Distance(transform.position, hits[i].transform.position);
            if (dist < bestDist)
            {
                closest = hits[i];
                bestDist = dist;
            }
        }

        DropItemSimple drop = closest.GetComponent<DropItemSimple>();
        float w = drop != null ? drop.pickupWeight : 1f;
        string id = drop != null ? drop.pickupId : GameItemIdsSimple.GenericDrop;
        int count = drop != null ? drop.pickupCount : 1;
        if (drop != null && !drop.CanBePickedBy(transform))
        {
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

        Debug.Log("Picked: " + closest.name);
        Destroy(closest.gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}
