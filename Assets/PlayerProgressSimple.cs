using UnityEngine;
using Unity.Netcode;

/// <summary>D3: 经验二选一占位（升级 or 技能解锁点），对齐 README 第 4 节方向。</summary>
public class PlayerProgressSimple : MonoBehaviour
{
    public static PlayerProgressSimple Instance { get; private set; }

    [Min(1)] public int level = 1;
    public int xpIntoCurrentLevel;
    [Tooltip("可分配经验池：击杀进这里，再由你决定升等级还是换技能点")]
    public int xpBank;
    [Tooltip("经验兑换得到的技能解锁点（占位）")]
    public int skillUnlockPoints;
    PlayerHotkeysSimple hotkeys;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // 场景单机体先占 Instance；带 NGO 的玩家体后唤醒时会在 OnNetworkSpawn 里 SetPreferredInstance。
            // 此处不再误报黄字，但仍需挂推导链，避免网络体缺组件。
            bool thisNet = GetComponent<MultiplayerPlayerSimple>() != null;
            bool instNet = Instance.GetComponent<MultiplayerPlayerSimple>() != null;
            if (thisNet && !instNet)
            {
                EnsureDerivedStatsChain();
                return;
            }

            Debug.LogWarning("Multiple PlayerProgressSimple — keeping first instance.");
            return;
        }

        Instance = this;
        EnsureDerivedStatsChain();
    }

    void EnsureDerivedStatsChain()
    {
        if (GetComponent<PlayerDerivedStatsSimple>() == null)
            gameObject.AddComponent<PlayerDerivedStatsSimple>();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        // Netcode: U/I 在联网时统一由 MultiplayerPlayerSimple 走 ServerRpc，避免纯 Client/Host 双处触发。
        MultiplayerPlayerSimple netPlayer = GetComponent<MultiplayerPlayerSimple>();
        if (netPlayer != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            return;

        if (hotkeys == null)
            hotkeys = GetComponent<PlayerHotkeysSimple>();

        // U: 把经验池投入等级进度
        KeyCode levelKey = hotkeys != null ? hotkeys.spendXpToLevel : KeyCode.U;
        if (Input.GetKeyDown(levelKey))
        {
            D3GrowthBalanceData db = D3GrowthBalance.Load();
            SpendXpForLevel(db.spendXpToLevelPerPress);
        }

        // I: 把经验池兑换为技能解锁点（占位）
        KeyCode spKey = hotkeys != null ? hotkeys.spendXpToSkillPoint : KeyCode.I;
        if (Input.GetKeyDown(spKey))
        {
            D3GrowthBalanceData db = D3GrowthBalance.Load();
            SpendXpForSkillUnlock(db.spendXpToSkillUnlockPerPress);
        }
    }

    public int XpNeededThisLevel()
    {
        D3GrowthBalanceData d = D3GrowthBalance.Load();
        return D3GrowthBalance.XpNeededForLevel(d, level);
    }

    public void AddXp(int amount)
    {
        if (amount <= 0) return;
        xpBank += amount;
    }

    public bool SpendXpForLevel(int amount)
    {
        D3GrowthBalanceData d0 = D3GrowthBalance.Load();
        int maxL = Mathf.Max(1, d0.maxLevel);
        if (level >= maxL)
        {
            if (amount > 0)
                ServerAuditLogSimple.Push(
                    ServerAuditLogSimple.CategorySrvValProgressReject,
                    "op=add_level_xp&reason=max_level");
            return false;
        }

        if (amount <= 0 || xpBank < amount)
        {
            if (amount > 0)
                ServerAuditLogSimple.Push(
                    ServerAuditLogSimple.CategorySrvValProgressReject,
                    $"op=add_level_xp&needXp={amount}&haveXp={xpBank}");
            return false;
        }

        int levelAtStart = level;
        xpBank -= amount;
        xpIntoCurrentLevel += amount;
        while (level < maxL && xpIntoCurrentLevel >= XpNeededThisLevel())
        {
            int need = XpNeededThisLevel();
            xpIntoCurrentLevel -= need;
            level++;
            Debug.Log($"Level Up → Lv.{level}");
        }

        int levelsGained = level - levelAtStart;
        if (levelsGained > 0)
        {
            D3GrowthBalanceData d3 = D3GrowthBalance.Load();
            int addPts = levelsGained * d3.statPointsPerLevel;
            if (addPts > 0)
                GrantD3StatPoints(addPts);
        }

        return true;
    }

    void GrantD3StatPoints(int n)
    {
        if (n <= 0) return;
        var mps = GetComponent<MultiplayerPlayerSimple>();
        if (mps != null && mps.IsSpawned && mps.IsServer)
            mps.ServerGrantD3StatPoints(n);
        else
        {
            PlayerStatsSimple st = GetComponent<PlayerStatsSimple>();
            if (st != null)
                st.GrantUnallocated(n);
        }
    }

    public bool SpendXpForSkillUnlock(int costPerPoint)
    {
        if (costPerPoint <= 0 || xpBank < costPerPoint)
        {
            if (costPerPoint > 0)
                ServerAuditLogSimple.Push(
                    ServerAuditLogSimple.CategorySrvValProgressReject,
                    $"op=xp_to_sp&needXp={costPerPoint}&haveXp={xpBank}");
            return false;
        }

        xpBank -= costPerPoint;
        skillUnlockPoints++;
        Debug.Log($"技能解锁点 +1（当前 {skillUnlockPoints}）");
        return true;
    }

    public void SetState(int newLevel, int newXpIntoLevel, int newXpBank, int newSkillPoints)
    {
        D3GrowthBalanceData d = D3GrowthBalance.Load();
        int maxL = Mathf.Max(1, d.maxLevel);
        level = Mathf.Clamp(newLevel, 1, maxL);
        xpIntoCurrentLevel = Mathf.Max(0, newXpIntoLevel);
        xpBank = Mathf.Max(0, newXpBank);
        skillUnlockPoints = Mathf.Max(0, newSkillPoints);
    }

    public static void SetPreferredInstance(PlayerProgressSimple value)
    {
        if (value != null)
            Instance = value;
    }
}
