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
    [Tooltip("本等级升级所需经验 = 基础 + (等级-1)*斜率")]
    public int xpBasePerLevel = 40;
    public int xpExtraPerLevelStep = 14;
    PlayerHotkeysSimple hotkeys;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple PlayerProgressSimple — keeping first instance.");
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        // Netcode: in multiplayer, progression input must go through MultiplayerPlayerSimple ServerRpc.
        MultiplayerPlayerSimple netPlayer = GetComponent<MultiplayerPlayerSimple>();
        if (netPlayer != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.IsServer)
            return;

        if (hotkeys == null)
            hotkeys = GetComponent<PlayerHotkeysSimple>();

        // U: 把经验池投入等级进度
        KeyCode levelKey = hotkeys != null ? hotkeys.spendXpToLevel : KeyCode.U;
        if (Input.GetKeyDown(levelKey))
            SpendXpForLevel(10);

        // I: 把经验池兑换为技能解锁点（占位）
        KeyCode spKey = hotkeys != null ? hotkeys.spendXpToSkillPoint : KeyCode.I;
        if (Input.GetKeyDown(spKey))
            SpendXpForSkillUnlock(15);
    }

    public int XpNeededThisLevel()
    {
        return Mathf.Max(1, xpBasePerLevel + (level - 1) * xpExtraPerLevelStep);
    }

    public void AddXp(int amount)
    {
        if (amount <= 0) return;
        xpBank += amount;
    }

    public bool SpendXpForLevel(int amount)
    {
        if (amount <= 0 || xpBank < amount)
        {
            if (amount > 0)
                ServerAuditLogSimple.Push(
                    ServerAuditLogSimple.CategorySrvValProgressReject,
                    $"op=add_level_xp&needXp={amount}&haveXp={xpBank}");
            return false;
        }

        xpBank -= amount;
        xpIntoCurrentLevel += amount;
        int cap = 200;
        while (level < cap && xpIntoCurrentLevel >= XpNeededThisLevel())
        {
            xpIntoCurrentLevel -= XpNeededThisLevel();
            level++;
            Debug.Log($"Level Up → Lv.{level}");
        }

        return true;
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
        level = Mathf.Max(1, newLevel);
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
