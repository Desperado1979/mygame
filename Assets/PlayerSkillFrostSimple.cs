using UnityEngine;

/// <summary>D2-2: R 技能 — 范围法伤直伤（<see cref="TakeSpellHit"/>）+ 冻结；数据由 <c>DefaultD3Growth</c> 覆盖。</summary>
public class PlayerSkillFrostSimple : MonoBehaviour
{
    public PlayerMpSimple mp;
    public PlayerSkillMasterySimple mastery;
    public PlayerSkillUnlockSimple unlocks;
    public LayerMask enemyLayer;

    public KeyCode skillKey = KeyCode.R;
    public string skillId = "FrostPulse";
    public int mpCost = 12;
    public float cooldownSeconds = 3.5f;
    public float skillRadius = 2.8f;
    public float freezeDurationSeconds = 2.2f;
    [Tooltip("法伤/冰伤直伤（TakeSpellHit）；由 DefaultD3Growth.skillFrostDamagePerHit 覆盖")]
    public int frostDamagePerEnemy;

    float cooldownEndTime;

    void Awake()
    {
        ApplyD3FrostSkillFromBalance();
    }

    void ApplyD3FrostSkillFromBalance()
    {
        D3GrowthBalanceData d = D3GrowthBalance.Load();
        mpCost = Mathf.Max(1, d.skillFrostMpCost);
        cooldownSeconds = Mathf.Max(0.1f, d.skillFrostCooldownSec);
        skillRadius = Mathf.Max(0.1f, d.skillFrostRadius);
        freezeDurationSeconds = Mathf.Max(0f, d.skillFrostFreezeSec);
        frostDamagePerEnemy = Mathf.Max(0, d.skillFrostDamagePerHit);
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
            Debug.LogWarning($"{nameof(PlayerSkillFrostSimple)}: assign PlayerMpSimple.");
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
            mastery.RegisterFrostCast();

        int tier = unlocks != null ? unlocks.frostTier : 1;
        D3GrowthBalanceData d3f = D3GrowthBalance.Load();
        float freezeSec = D3GrowthBalance.ComputeFrostFreezeDurationSeconds(
            d3f,
            freezeDurationSeconds,
            tier,
            mastery != null ? mastery.FrostFreezeDurationMultiplier : 1f);

        PlayerStatsSimple stFrost = GetComponent<PlayerStatsSimple>();
        int intelFrost = stFrost != null ? stFrost.intellect : d3f.startingInt;
        int rolledDamage = D3GrowthBalance.ComputeFrostRolledDamage(
            d3f, frostDamagePerEnemy, intelFrost, tier);

        float frostR = D3GrowthBalance.ComputeFrostOverlapRadius(d3f, skillRadius, tier);
        Collider[] hits = Physics.OverlapSphere(transform.position, frostR, enemyLayer);
        int damaged = 0;
        int applied = 0;
        for (int i = 0; i < hits.Length; i++)
        {
            EnemyHealthSimple eh = hits[i].GetComponentInParent<EnemyHealthSimple>();
            if (eh != null && rolledDamage > 0)
            {
                eh.TakeSpellHit(rolledDamage, transform.position, gameObject);
                damaged++;
            }

            EnemyStatusEffectsSimple status = hits[i].GetComponentInParent<EnemyStatusEffectsSimple>();
            if (status != null)
            {
                status.ApplyFreeze(freezeSec);
                MonsterP1A1Mark p1 = hits[i].GetComponentInParent<MonsterP1A1Mark>();
                if (p1 != null)
                    p1.RegisterFreeze();
                applied++;
            }
        }

        Debug.Log($"{skillId} T{tier} — spellHits {damaged}, frozen {applied} / colliders {hits.Length}");
    }

    public float CooldownRemaining => Mathf.Max(0f, cooldownEndTime - Time.time);

    void OnDrawGizmosSelected()
    {
        if (unlocks == null)
            unlocks = GetComponent<PlayerSkillUnlockSimple>();
        int tier = unlocks != null ? unlocks.frostTier : 1;
        D3GrowthBalanceData d = D3GrowthBalance.Load();
        float r = D3GrowthBalance.ComputeFrostOverlapRadius(d, skillRadius, tier);
        Gizmos.color = new Color(0.4f, 0.85f, 1f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, r);
    }
}
