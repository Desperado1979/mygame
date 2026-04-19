using UnityEngine;

/// <summary>D2-1: One non-basic skill (default Q) — MP cost + cooldown + AoE hit.</summary>
public class PlayerSkillBurstSimple : MonoBehaviour
{
    public PlayerMpSimple mp;
    public PlayerSkillMasterySimple mastery;
    public PlayerSkillUnlockSimple unlocks;
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
        mastery = GetComponent<PlayerSkillMasterySimple>();
        unlocks = GetComponent<PlayerSkillUnlockSimple>();
    }

    void Update()
    {
        if (!Input.GetKeyDown(skillKey))
            return;

        if (unlocks == null)
            unlocks = GetComponent<PlayerSkillUnlockSimple>();

        if (Time.time < cooldownEndTime)
        {
            ServerAuditLogSimple.Push(
                ServerAuditLogSimple.CategorySrvValIllegalOperation,
                $"skillId={skillId}&reason=cooldown&remainSec={CooldownRemaining:F2}");
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
            ServerAuditLogSimple.Push(
                ServerAuditLogSimple.CategorySrvValIllegalOperation,
                $"skillId={skillId}&reason=mp_insufficient&needMp={mpCost}&haveMp={mp.CurrentMpRounded}");
            Debug.Log("Not enough MP");
            return;
        }

        cooldownEndTime = Time.time + cooldownSeconds;

        if (mastery == null)
            mastery = GetComponent<PlayerSkillMasterySimple>();
        if (mastery != null)
            mastery.RegisterBurstCast();

        int tier = unlocks != null ? unlocks.burstTier : 1;
        float tierDamageMul = tier >= 2 ? 1.35f : 1f;
        float tierRangeMul = tier >= 2 ? 1.15f : 1f;
        float dmgMult = mastery != null ? mastery.BurstDamageMultiplier : 1f;
        int rolledDamage = Mathf.Max(1, Mathf.RoundToInt(damagePerEnemy * dmgMult * tierDamageMul));

        Collider[] hits = Physics.OverlapSphere(transform.position, skillRadius * tierRangeMul, enemyLayer);
        int damaged = 0;
        for (int i = 0; i < hits.Length; i++)
        {
            EnemyHealthSimple enemy = hits[i].GetComponent<EnemyHealthSimple>();
            if (enemy == null) continue;
            enemy.TakeHit(rolledDamage, transform.position);
            damaged++;

            MonsterP1A1Mark p1 = hits[i].GetComponent<MonsterP1A1Mark>();
            if (p1 != null)
                p1.RegisterBurstHit();

            EnemyStatusEffectsSimple status = hits[i].GetComponent<EnemyStatusEffectsSimple>();
            if (status != null && burnDurationSeconds > 0f)
                status.ApplyBurn(burnDurationSeconds);
        }

        Debug.Log($"{skillId} T{tier} cast — hits {hits.Length}, damaged {damaged}");
    }

    public float CooldownRemaining => Mathf.Max(0f, cooldownEndTime - Time.time);

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, skillRadius);
    }
}
