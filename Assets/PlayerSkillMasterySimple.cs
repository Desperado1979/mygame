using UnityEngine;

/// <summary>D3: Q 技能靠施放次数升档 Lv.1~10，伤害乘数占位（README §4.3）。</summary>
public class PlayerSkillMasterySimple : MonoBehaviour
{
    [Range(1, 10)] public int burstSkillLevel = 1;
    public int burstCastsThisLevel;

    [Tooltip("从当前 burstSkillLevel 升到下一级所需施放次数（DefaultD3Growth 覆盖）")]
    public int burstCastsBase = 5;
    public int burstCastsPerLevel = 3;

    public int CastsNeededForNextLevel()
    {
        if (burstSkillLevel >= 10)
            return int.MaxValue;
        return Mathf.Max(1, burstCastsBase + (burstSkillLevel - 1) * burstCastsPerLevel);
    }

    /// <summary>在 Q 成功扣蓝进 CD 后调用。</summary>
    public void RegisterBurstCast()
    {
        if (burstSkillLevel >= 10)
            return;

        burstCastsThisLevel++;
        int need = CastsNeededForNextLevel();
        if (burstCastsThisLevel < need)
            return;

        burstCastsThisLevel = 0;
        burstSkillLevel++;
        Debug.Log($"Burst skill Lv.{burstSkillLevel}");
    }

    /// <summary>Damage = Base × (1 + (Lv-1) × Scale)（Scale 由 DefaultD3Growth 写 <c>burstDamageScalePerLevel</c>）。</summary>
    public float BurstDamageMultiplier => 1f + (burstSkillLevel - 1) * burstDamageScalePerLevel;

    [Header("Frost (R) — cast mastery（DefaultD3Growth 覆盖）")]
    [Range(1, 10)] public int frostSkillLevel = 1;
    public int frostCastsThisLevel;
    public int frostCastsBase = 5;
    public int frostCastsPerLevel = 3;

    [Tooltip("README §4.3：每 Q 技能等级对伤害乘子边率")]
    public float burstDamageScalePerLevel = 0.08f;
    [Tooltip("每 R 技能等级对冰冻时长乘子边率")]
    public float frostFreezeScalePerLevel = 0.06f;

    void Awake()
    {
        ApplyD3MasteryFromBalance();
    }

    void ApplyD3MasteryFromBalance()
    {
        D3GrowthBalanceData d = D3GrowthBalance.Load();
        burstCastsBase = Mathf.Max(1, d.masteryBurstCastsBase);
        burstCastsPerLevel = Mathf.Max(0, d.masteryBurstCastsPerLevel);
        frostCastsBase = Mathf.Max(1, d.masteryFrostCastsBase);
        frostCastsPerLevel = Mathf.Max(0, d.masteryFrostCastsPerLevel);
        burstDamageScalePerLevel = Mathf.Max(0f, d.masteryBurstDmgPerSkillLevel);
        frostFreezeScalePerLevel = Mathf.Max(0f, d.masteryFrostFreezePerSkillLevel);
    }

    public int FrostCastsNeededForNextLevel()
    {
        if (frostSkillLevel >= 10)
            return int.MaxValue;
        return Mathf.Max(1, frostCastsBase + (frostSkillLevel - 1) * frostCastsPerLevel);
    }

    /// <summary>R 成功扣蓝进 CD 后调用。</summary>
    public void RegisterFrostCast()
    {
        if (frostSkillLevel >= 10)
            return;

        frostCastsThisLevel++;
        int need = FrostCastsNeededForNextLevel();
        if (frostCastsThisLevel < need)
            return;

        frostCastsThisLevel = 0;
        frostSkillLevel++;
        Debug.Log($"Frost skill Lv.{frostSkillLevel}");
    }

    public float FrostFreezeDurationMultiplier => 1f + (frostSkillLevel - 1) * frostFreezeScalePerLevel;
}
