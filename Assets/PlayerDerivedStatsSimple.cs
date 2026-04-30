using UnityEngine;

/// <summary>D3：按 README §5.2 / §9 从四维+等级推导 MaxHP / MaxMP / 负重上限；等级变化时刷新（单机与联机共用公式）。</summary>
[DefaultExecutionOrder(10)]
public sealed class PlayerDerivedStatsSimple : MonoBehaviour
{
    PlayerStatsSimple stats;
    PlayerProgressSimple progress;
    PlayerHealthSimple health;
    PlayerMpSimple mp;
    PlayerInventorySimple inventory;
    PlayerEquipmentDebugSimple equipDebug;

    int _lastAppliedLevel = int.MinValue;
    int _lastStr = int.MinValue, _lastAgi = int.MinValue, _lastInt = int.MinValue, _lastVit = int.MinValue;
    int _lastEqHp = int.MinValue;
    int _lastEqMp = int.MinValue;

    void Awake()
    {
        if (GetComponent<PlayerStatsSimple>() == null)
            gameObject.AddComponent<PlayerStatsSimple>();
        stats = GetComponent<PlayerStatsSimple>();
        progress = GetComponent<PlayerProgressSimple>();
        health = GetComponent<PlayerHealthSimple>();
        mp = GetComponent<PlayerMpSimple>();
        inventory = GetComponent<PlayerInventorySimple>();
        // PlayerEquipmentDebugSimple 可能由 NGO OnNetworkSpawn 晚于本 Awake 再 AddComponent；禁止在此处缓存 null。
    }

    PlayerEquipmentDebugSimple ResolveEquipDebug()
    {
        if (equipDebug == null)
            equipDebug = GetComponent<PlayerEquipmentDebugSimple>();
        return equipDebug;
    }

    void Start()
    {
        ApplyDerivedStats(true);
    }

    public void RequestRefresh() => ApplyDerivedStats(true);

    void LateUpdate()
    {
        if (progress == null)
            return;
        if (stats == null)
            return;
        PlayerEquipmentDebugSimple eq = ResolveEquipDebug();
        int eqHp = eq != null ? eq.AggregateEquipHpBonus() : 0;
        int eqMp = eq != null ? eq.AggregateEquipMpBonus() : 0;
        bool chg = progress.level != _lastAppliedLevel
                   || stats.strength != _lastStr || stats.agility != _lastAgi
                   || stats.intellect != _lastInt || stats.vitality != _lastVit
                   || eqHp != _lastEqHp || eqMp != _lastEqMp;
        if (chg)
            ApplyDerivedStats(true);
    }

    void ApplyDerivedStats(bool preserveHpMpRatio)
    {
        D3GrowthBalanceData d = D3GrowthBalance.Load();

        int level = progress != null ? Mathf.Max(1, progress.level) : 1;
        int str = stats != null ? stats.strength : d.startingStr;
        int mint = stats != null ? stats.intellect : d.startingInt;
        int vit = stats != null ? stats.vitality : d.startingVit;

        PlayerEquipmentDebugSimple eqComp = ResolveEquipDebug();
        int exHp = eqComp != null ? eqComp.AggregateEquipHpBonus() : 0;
        int exMp = eqComp != null ? eqComp.AggregateEquipMpBonus() : 0;

        int maxHp = D3GrowthBalance.ComputeMaxHp(d, vit, level, exHp);
        int maxMp = D3GrowthBalance.ComputeMaxMp(d, mint, level, exMp);
        _lastEqHp = exHp;
        _lastEqMp = exMp;
        float carry = D3GrowthBalance.ComputeCarryWeight(d, str);

        if (health != null)
            health.ApplyMaxHpFromDerived(maxHp, preserveHpMpRatio);

        if (mp != null)
            mp.ApplyMaxMpFromDerived(maxMp, preserveHpMpRatio);

        if (inventory != null)
            inventory.ApplyMaxCarryWeightFromDerived(carry);

        _lastAppliedLevel = level;
        if (stats != null)
        {
            _lastStr = stats.strength;
            _lastAgi = stats.agility;
            _lastInt = stats.intellect;
            _lastVit = stats.vitality;
        }
    }
}
