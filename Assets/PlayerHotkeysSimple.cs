using UnityEngine;

/// <summary>Week1: unified hotkey registry for progression/economy/save actions.</summary>
public class PlayerHotkeysSimple : MonoBehaviour
{
    [Header("Progression")]
    public KeyCode spendXpToLevel = KeyCode.U;
    public KeyCode spendXpToSkillPoint = KeyCode.I;
    public KeyCode unlockQ2 = KeyCode.O;
    public KeyCode unlockR2 = KeyCode.P;

    [Header("Economy / Inventory")]
    public KeyCode useHpPotion = KeyCode.Alpha1;
    public KeyCode useMpPotion = KeyCode.Alpha2;
    public KeyCode buyHpPotion = KeyCode.B;
    public KeyCode buyMpPotion = KeyCode.N;
    public KeyCode sellPotion = KeyCode.V;
    [Tooltip("快速清背包：优先丢材料(shard)，其次蓝药(mana)。")]
    public KeyCode discardJunk = KeyCode.C;
    public KeyCode enhance = KeyCode.T;

    [Header("Bank")]
    public KeyCode bankDepositGold = KeyCode.F5;
    public KeyCode bankWithdrawGold = KeyCode.F6;
    public KeyCode bankDepositPotion = KeyCode.F7;
    public KeyCode bankWithdrawPotion = KeyCode.F8;

    [Header("Social / PvP")]
    public KeyCode togglePvp = KeyCode.K;

    [Header("Save")]
    public KeyCode save = KeyCode.F9;
    public KeyCode load = KeyCode.F10;
    public KeyCode clearSave = KeyCode.F11;
    public KeyCode exportStateJson = KeyCode.F12;
    public KeyCode exportAuditNdjson = KeyCode.F4;
    public KeyCode exportRequestPayloadJson = KeyCode.F3;

    [Header("D5 rehearsal — POST /sync + metrics GET（默认 [ ] 避免与队伍 =/- 冲突）")]
    [Tooltip("POST /sync；须 PlayerStateExportSimple.networkSyncEnabled 且排练服已启动（见 docs/getting-started §5）。")]
    public KeyCode postSyncToServer = KeyCode.F2;
    [Tooltip("GET /metrics/alerts/players（须 networkSyncEnabled）。")]
    public KeyCode metricsAlertsPlayers = KeyCode.LeftBracket;
    [Tooltip("GET /metrics/dashboard（须 networkSyncEnabled）。")]
    public KeyCode metricsDashboard = KeyCode.RightBracket;
    [Tooltip("GET /metrics/audit-categories?days=7")]
    public KeyCode metricsAuditCategories = KeyCode.Semicolon;
    [Tooltip("GET /health（须 networkSyncEnabled）。")]
    public KeyCode probeServerHealth = KeyCode.F1;

    [Header("D3 属性点 — 小键盘1~4=STR/AGI/INT/VIT（可改；无小键盘时自行绑定）")]
    public KeyCode d3AddStr = KeyCode.Keypad1;
    public KeyCode d3AddAgi = KeyCode.Keypad2;
    public KeyCode d3AddInt = KeyCode.Keypad3;
    public KeyCode d3AddVit = KeyCode.Keypad4;

    [Header("D5 备用热键（与上行并存；设为 None 可关闭）")]
    [Tooltip("等同 [：GET /metrics/alerts/players")]
    public KeyCode metricsAlertsPlayersAlt = KeyCode.J;
    [Tooltip("等同 ]：GET /metrics/dashboard")]
    public KeyCode metricsDashboardAlt = KeyCode.L;
    [Tooltip("等同 F1：GET /health")]
    public KeyCode probeServerHealthAlt = KeyCode.Alpha0;
    [Tooltip("等同 F2：POST /sync")]
    public KeyCode postSyncToServerAlt = KeyCode.Minus;
    [Tooltip("等同 ;：GET /metrics/audit-categories")]
    public KeyCode metricsAuditCategoriesAlt = KeyCode.None;

    [Header("System")]
    public KeyCode quitGame = KeyCode.Escape;
}
