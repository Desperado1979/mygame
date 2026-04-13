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
                    enemy.TakeHit(damagePerHit);
                }
                else
                {
                    Debug.Log("Hit Enemy: " + hits[0].name + " (no EnemyHealthSimple)");
                }
            }
            else
            {
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