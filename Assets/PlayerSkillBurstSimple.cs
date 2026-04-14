using UnityEngine;

/// <summary>D2-1: One non-basic skill (default Q) — MP cost + cooldown + AoE hit.</summary>
public class PlayerSkillBurstSimple : MonoBehaviour
{
    public PlayerMpSimple mp;
    public LayerMask enemyLayer;

    public KeyCode skillKey = KeyCode.Q;
    public string skillId = "Burst";
    public int mpCost = 20;
    public float cooldownSeconds = 2.5f;
    public float skillRadius = 3.2f;
    public int damagePerEnemy = 2;

    [Header("D2-2 — Burn on hit (README §11.1)")]
    public float burnDurationSeconds = 3.2f;

    float cooldownEndTime;

    void Reset()
    {
        mp = GetComponent<PlayerMpSimple>();
    }

    void Update()
    {
        if (!Input.GetKeyDown(skillKey))
            return;

        if (Time.time < cooldownEndTime)
        {
            Debug.Log($"{skillId} on cooldown ({CooldownRemaining:F1}s)");
            return;
        }

        if (mp == null)
        {
            Debug.LogWarning($"{nameof(PlayerSkillBurstSimple)}: assign PlayerMpSimple.");
            return;
        }

        if (!mp.TrySpend(mpCost))
        {
            Debug.Log("Not enough MP");
            return;
        }

        cooldownEndTime = Time.time + cooldownSeconds;

        Collider[] hits = Physics.OverlapSphere(transform.position, skillRadius, enemyLayer);
        int damaged = 0;
        for (int i = 0; i < hits.Length; i++)
        {
            EnemyHealthSimple enemy = hits[i].GetComponent<EnemyHealthSimple>();
            if (enemy == null) continue;
            enemy.TakeHit(damagePerEnemy);
            damaged++;

            EnemyStatusEffectsSimple status = hits[i].GetComponent<EnemyStatusEffectsSimple>();
            if (status != null && burnDurationSeconds > 0f)
                status.ApplyBurn(burnDurationSeconds);
        }

        Debug.Log($"{skillId} cast — hits {hits.Length}, damaged {damaged}");
    }

    public float CooldownRemaining => Mathf.Max(0f, cooldownEndTime - Time.time);

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, skillRadius);
    }
}
