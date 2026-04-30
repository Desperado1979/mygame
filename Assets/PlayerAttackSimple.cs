using UnityEngine;

public class PlayerAttackSimple : MonoBehaviour
{
    public float attackRange = 2.0f;
    public LayerMask enemyLayer;
    public int damagePerHit = 1;
    PlayerStatsSimple _stats;
    float nextAttackAt;

    void Awake()
    {
        D3GrowthBalanceData d = D3GrowthBalance.Load();
        attackRange = Mathf.Max(0.1f, d.playerMeleeAttackRangeSolo);
        _stats = GetComponent<PlayerStatsSimple>();
        int str = _stats != null ? _stats.strength : d.startingStr;
        damagePerHit = D3GrowthBalance.ComputeMeleePhysicalDamage(d, str);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (Time.time < nextAttackAt)
                return;

            D3GrowthBalanceData d = D3GrowthBalance.Load();
            if (_stats == null)
                _stats = GetComponent<PlayerStatsSimple>();
            int agiGate = _stats != null ? _stats.agility : d.startingAgi;
            nextAttackAt = Time.time + D3GrowthBalance.ComputeMeleeAttackInterval(d, agiGate);

            Collider[] hits = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);
            if (hits.Length > 0)
            {
                EnemyHealthSimple enemy = hits[0].GetComponent<EnemyHealthSimple>();
                if (enemy != null)
                {
                    int str = _stats != null ? _stats.strength : d.startingStr;
                    int agi = _stats != null ? _stats.agility : d.startingAgi;
                    int raw = D3GrowthBalance.ComputeMeleePhysicalDamage(d, str);
                    int afterArmor = D3GrowthBalance.ApplyPhysicalDefenseToDamage(raw, enemy.PhysicalDefense);
                    int final = D3GrowthBalance.ApplyMeleeCritAfterPhysicalArmor(d, agi, afterArmor);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    float critP = D3GrowthBalance.MeleeCritProbability(d, agi);
                    bool crit = final > afterArmor;
                    Debug.Log($"[MeleeHit][Solo] str={str} agi={agi} raw={raw} def={enemy.PhysicalDefense} base={afterArmor} final={final} crit={(crit ? 1 : 0)} p={critP:P1}");
#endif
                    damagePerHit = final;
                    enemy.TakeHit(final, transform.position, gameObject);
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