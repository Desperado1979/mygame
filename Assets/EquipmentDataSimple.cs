using UnityEngine;

/// <summary>D2-3: 占位装备数据 — 四耐性（缩短对应状态持续时间，百分点占位）+ 力量需求（README §3.4–3.5）。</summary>
[CreateAssetMenu(fileName = "NewEquipment", menuName = "EpochOfDawn/Equipment Data (Simple)", order = 0)]
public class EquipmentDataSimple : ScriptableObject
{
    public string displayName = "Unnamed";

    [Tooltip("穿戴所需力量")]
    public int requiredStrength = 10;

    [Header("README §5.2 — 装备对最大 HP/MP 的加算（件内白字，多件时由穿戴入口汇总）")]
    [Min(0)] public int bonusMaxHp;
    [Min(0)] public int bonusMaxMp;

    [Header("四耐性（% 占位：将来用于缩短对应状态持续时间）")]
    [Range(0, 100)] public int resistFreezePercent;
    [Range(0, 100)] public int resistBurnPercent;
    [Range(0, 100)] public int resistPoisonPercent;
    [Range(0, 100)] public int resistBloodlustPercent;
}
