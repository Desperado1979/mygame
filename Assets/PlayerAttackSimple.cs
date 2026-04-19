using UnityEngine;

public class PlayerAttackSimple : MonoBehaviour
{
    public float attackRange = 2.0f;
    public LayerMask enemyLayer;
    public int damagePerHit = 1;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);
            if (hits.Length > 0)
            {
                EnemyHealthSimple enemy = hits[0].GetComponent<EnemyHealthSimple>();
                if (enemy != null)
                {
                    enemy.TakeHit(damagePerHit, transform.position);
                }
                else
                {
                    ServerAuditLogSimple.Push(
                        ServerAuditLogSimple.CategorySrvValCombatMiss,
                        $"reason=no_enemy_health&hit={hits[0].name}");
                    Debug.Log("Hit Enemy: " + hits[0].name + " (no EnemyHealthSimple)");
                }
            }
            else
            {
                ServerAuditLogSimple.Push(
                    ServerAuditLogSimple.CategorySrvValCombatMiss,
                    $"range={attackRange:F1}");
                Debug.Log("Attack Miss");
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}