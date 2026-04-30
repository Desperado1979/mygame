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

    [Header("D2-2 — Burn on hit (README §11.1) — DefaultD3Growth 覆盖")]
    public float burnDurationSeconds = 3.2f;

    float cooldownEndTime;

    void Awake()
    {
        ApplyD3BurstSkillFromBalance();
    }

    void ApplyD3BurstSkillFromBalance()
    {
        D3GrowthBalanceData d = D3GrowthBalance.Load();
        mpCost = Mathf.Max(1, d.skillBurstMpCost);
        cooldownSeconds = Mathf.Max(0.1f, d.skillBurstCooldownSec);
        skillRadius = Mathf.Max(0.1f, d.skillBurstRadius);
        damagePerEnemy = Mathf.Max(1, d.skillBurstDamagePerHit);
        burnDurationSeconds = Mathf.Max(0f, d.skillBurstBurnSec);
    }

    void Reset()
    {
        mp = GetComponent<PlayerMpSimple>();
        mastery = GetComponent<PlayerSkillMasterySimple>();
        unlocks = GetComponent<PlayerSkillUnlockSimple>();
    }

    void Update()
    {
        MultiplayerPlayerSimple net = GetComponent<MultiplayerPlayerSimple>();
        if (net != null)
            return; // Multiplayer path is handled by MultiplayerPlayerSimple server RPC.

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
        D3GrowthBalanceData d3b = D3GrowthBalance.Load();
        float dmgMult = mastery != null ? mastery.BurstDamageMultiplier : 1f;
        PlayerStatsSimple stBurst = GetComponent<PlayerStatsSimple>();
        int intelBurst = stBurst != null ? stBurst.intellect : d3b.startingInt;
        int rolledDamage = D3GrowthBalance.ComputeBurstRolledDamage(
            d3b, damagePerEnemy, intelBurst, dmgMult, tier);

        float overlapR = D3GrowthBalance.ComputeBurstOverlapRadius(d3b, skillRadius, tier);
        Collider[] hits = Physics.OverlapSphere(transform.position, overlapR, enemyLayer);
        int damaged = 0;
        for (int i = 0; i < hits.Length; i++)
        {
            EnemyHealthSimple enemy = hits[i].GetComponent<EnemyHealthSimple>();
            if (enemy == null) continue;
            enemy.TakeSpellHit(rolledDamage, transform.position, gameObject);
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
        if (unlocks == null)
            unlocks = GetComponent<PlayerSkillUnlockSimple>();
        int tier = unlocks != null ? unlocks.burstTier : 1;
        D3GrowthBalanceData d = D3GrowthBalance.Load();
        float r = D3GrowthBalance.ComputeBurstOverlapRadius(d, skillRadius, tier);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, r);
    }
}
