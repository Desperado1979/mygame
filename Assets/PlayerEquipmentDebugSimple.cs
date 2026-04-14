using UnityEngine;

/// <summary>D2-3: 在玩家上挂一件测试装占位，供 HUD / 以后穿戴校验接 STR。</summary>
public class PlayerEquipmentDebugSimple : MonoBehaviour
{
    public EquipmentDataSimple testArmor;

    [Tooltip("占位：人物力量，将来接属性系统")]
    public int playerStrengthForTest = 8;

    public bool CanEquipTestArmor()
    {
        if (testArmor == null) return false;
        return playerStrengthForTest >= testArmor.requiredStrength;
    }
}
