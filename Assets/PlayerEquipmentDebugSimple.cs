using UnityEngine;

/// <summary>D2-3 / D3：测试穿戴与装备 MaxHP/MaxMP 加总入口；多部位后在此汇总。</summary>
public class PlayerEquipmentDebugSimple : MonoBehaviour
{
    public const string DefaultTestArmorResourcesPath = "Equipment/TestIronChest";

    public EquipmentDataSimple testArmor;
    [Tooltip("为 true 时把 testArmor 的白字加入 <see cref=\"PlayerDerivedStatsSimple\"/> 的 HP/MP 上限。")]
    public bool treatTestArmorAsEquipped = true;

    [Tooltip("无 PlayerStats 时回退用；有 <see cref=\"PlayerStatsSimple\"/> 时以力量为准。")]
    public int playerStrengthForTest = 8;

    void Awake()
    {
        if (testArmor == null && !string.IsNullOrEmpty(DefaultTestArmorResourcesPath))
        {
            testArmor = Resources.Load<EquipmentDataSimple>(DefaultTestArmorResourcesPath);
            if (testArmor == null)
                Debug.LogWarning(
                    "[PlayerEquipmentDebug] 未在 Inspector 指定 testArmor，且 Resources 未找到："
                    + DefaultTestArmorResourcesPath);
        }
    }

    public bool CanEquipTestArmor()
    {
        if (testArmor == null) return false;
        return ResolveStrengthForRequirement() >= testArmor.requiredStrength;
    }

    public int ResolveStrengthForRequirement()
    {
        PlayerStatsSimple st = GetComponent<PlayerStatsSimple>();
        if (st != null)
            return st.strength;
        return playerStrengthForTest;
    }

    public int AggregateEquipHpBonus()
    {
        if (!treatTestArmorAsEquipped || testArmor == null)
            return 0;
        return Mathf.Max(0, testArmor.bonusMaxHp);
    }

    public int AggregateEquipMpBonus()
    {
        if (!treatTestArmorAsEquipped || testArmor == null)
            return 0;
        return Mathf.Max(0, testArmor.bonusMaxMp);
    }
}
