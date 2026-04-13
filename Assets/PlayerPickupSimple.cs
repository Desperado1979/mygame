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

        Debug.Log("Picked: " + closest.name);
        Destroy(closest.gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}
