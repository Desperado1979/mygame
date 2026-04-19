using UnityEngine;

/// <summary>D3: Q 技能靠施放次数升档 Lv.1~10，伤害乘数占位（README §4.3）。</summary>
public class PlayerSkillMasterySimple : MonoBehaviour
{
    [Range(1, 10)] public int burstSkillLevel = 1;
    public int burstCastsThisLevel;

    [Tooltip("从当前 burstSkillLevel 升到下一级所需施放次数")]
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

    /// <summary>Damage = Base × (1 + (Lv-1) × Scale)，Scale 占位。</summary>
    public float BurstDamageMultiplier => 1f + (burstSkillLevel - 1) * 0.08f;

    [Header("Frost (R) — cast mastery")]
    [Range(1, 10)] public int frostSkillLevel = 1;
    public int frostCastsThisLevel;
    public int frostCastsBase = 5;
    public int frostCastsPerLevel = 3;

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

    public float FrostFreezeDurationMultiplier => 1f + (frostSkillLevel - 1) * 0.06f;
}
