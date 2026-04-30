using UnityEngine;

/// <summary>
/// D3：四维 + 未分配点（README §2.1）。单机本地权威；联机时由 <see cref="MultiplayerPlayerSimple"/> 的 NetworkVariable 镜射。
/// </summary>
public class PlayerStatsSimple : MonoBehaviour
{
    [Tooltip("力量 — 负重等，见 README §9")]
    public int strength = 10;
    [Tooltip("敏捷")]
    public int agility = 10;
    [Tooltip("智力 — MP 等，见 §5.2")]
    public int intellect = 10;
    [Tooltip("体力 — HP 等，见 §5.2")]
    public int vitality = 10;
    [Tooltip("可加到四维上的未分配点")]
    public int unallocatedStatPoints;

    void Awake()
    {
        ApplyStartingStatsFromBalance();
    }

    void ApplyStartingStatsFromBalance()
    {
        D3GrowthBalanceData d = D3GrowthBalance.Load();
        strength = Mathf.Max(0, d.startingStr);
        agility = Mathf.Max(0, d.startingAgi);
        intellect = Mathf.Max(0, d.startingInt);
        vitality = Mathf.Max(0, d.startingVit);
        unallocatedStatPoints = 0;
    }

    public void ApplyStatsMirror(int s, int a, int i, int v, int u)
    {
        strength = Mathf.Max(0, s);
        agility = Mathf.Max(0, a);
        intellect = Mathf.Max(0, i);
        vitality = Mathf.Max(0, v);
        unallocatedStatPoints = Mathf.Max(0, u);
    }

    public void GrantUnallocated(int n)
    {
        if (n <= 0)
            return;
        unallocatedStatPoints = Mathf.Max(0, unallocatedStatPoints) + n;
    }

    /// <summary>单机/非 NGO 时本地加 STR；联机用 <see cref="MultiplayerPlayerSimple"/> RPC。</summary>
    public bool TryAddStrengthLocal()
    {
        if (unallocatedStatPoints < 1) return false;
        unallocatedStatPoints--;
        strength++;
        return true;
    }

    public bool TryAddAgilityLocal()
    {
        if (unallocatedStatPoints < 1) return false;
        unallocatedStatPoints--;
        agility++;
        return true;
    }

    public bool TryAddIntellectLocal()
    {
        if (unallocatedStatPoints < 1) return false;
        unallocatedStatPoints--;
        intellect++;
        return true;
    }

    public bool TryAddVitalityLocal()
    {
        if (unallocatedStatPoints < 1) return false;
        unallocatedStatPoints--;
        vitality++;
        return true;
    }

    void Update()
    {
        MultiplayerPlayerSimple mps = GetComponent<MultiplayerPlayerSimple>();
        if (mps != null)
            return;
        if (hotkeys == null)
            hotkeys = GetComponent<PlayerHotkeysSimple>();
        if (hotkeys == null)
            return;
        if (Input.GetKeyDown(hotkeys.d3AddStr) && TryAddStrengthLocal())
            GetComponent<PlayerDerivedStatsSimple>()?.RequestRefresh();
        else if (Input.GetKeyDown(hotkeys.d3AddAgi) && TryAddAgilityLocal())
            GetComponent<PlayerDerivedStatsSimple>()?.RequestRefresh();
        else if (Input.GetKeyDown(hotkeys.d3AddInt) && TryAddIntellectLocal())
            GetComponent<PlayerDerivedStatsSimple>()?.RequestRefresh();
        else if (Input.GetKeyDown(hotkeys.d3AddVit) && TryAddVitalityLocal())
            GetComponent<PlayerDerivedStatsSimple>()?.RequestRefresh();
    }

    PlayerHotkeysSimple hotkeys;
}
