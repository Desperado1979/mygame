using UnityEngine;

/// <summary>D5：本地导出（F12/F4/F3）+ 排练 POST /sync 与观测 GET。</summary>
public partial class PlayerStateExportSimple : MonoBehaviour
{
    const string SyncBaseUrlPrefsKey = "EOD_SYNC_BASE_URL";

    public string playerId = "local_player_001";
    public string exportFileName = "player_state_export.json";
    public string auditExportFileName = "audit_events.ndjson";
    public string requestExportFileName = "client_sync_request.json";
    public string lastSyncSnapshotFileName = "last_sync_response.json";

    [Tooltip("POST /sync 与 GET 排练根地址；可在运行时由 PlayerPrefs EOD_SYNC_BASE_URL 覆盖。")]
    public string syncBaseUrl = "http://127.0.0.1:8787";

    [Tooltip("D17：与 persist_sync D16 对齐，若本地存有上次 ETag 则在 POST /sync 带 If-Match；关闭则从不发送（旧服调试）。")]
    public bool syncUseIfMatch = true;

    [Tooltip("D18：本地无 ETag 时，POST /sync 前先 GET /state 取 ETag 写入 PlayerPrefs，减少首包或清缓存后与服务器存档不一致。")]
    public bool syncPrefetchStateEtag = true;

    public bool networkSyncEnabled = true;

    public int LastHttpCode;
    public int LastSyncWarnLow;
    public int LastSyncWarnHigh;
    public string LastWarningsCodesPreview = "";
    public string LastSyncError = "";
    public bool LastNetAlertHigh;
    public string LastPostResponsePreview = "";
    public string LastMetricsAlertsPreview = "";
    public string LastMetricsDashboardPreview = "";
    public string LastMetricsAuditCategoriesPreview = "";
    public string LastHealthProbePreview = "";

    public string LastPostResponseFull { get; private set; } = "";

    /// <summary>最近一次成功解析的 <c>POST /sync</c> 响应里 <c>validation.ok</c>；未收到或非 2xx 时为 <c>null</c>。权威在服务端。</summary>
    public bool? LastSyncValidationOk;

    /// <summary><c>POST /sync</c> 响应中 <c>auditSummary.byCategory.SrvVal_IllegalOperation</c> 的 HUD 短摘要（无则为空）。</summary>
    public string LastAuditCategoryPreview = "";

    /// <summary>最近一次 <c>POST /sync</c> 的粗分类（如 <c>ok</c>、<c>maint</c>、<c>ratelimit</c>）；仅观测。</summary>
    public string LastSyncPostStatusTag = "";

    /// <summary>服务端响应头 <c>X-Sync-Duration-Ms</c>（毫秒）；未返回时为 <c>-1</c>。</summary>
    public int LastSyncDurationMs = -1;

    /// <summary><c>POST /sync</c> 因 **429** 重试的次数（仅统计已发生的等待重试）。</summary>
    public int LastSyncRetryCount;

    /// <summary>最近一次 <c>POST /sync</c> 的 HTTP 状态码；未发起过 F2 同步时为 <c>-1</c>（与 <see cref="LastHttpCode"/> 区分，后者会被 F1/<c>;</c> 等 GET 覆盖）。</summary>
    public int LastSyncPostHttpCode = -1;

    /// <summary>D17：最近一次从 <c>POST /sync</c> 响应头 <c>ETag</c> 解析出的强校验值（十六进制，无引号）；未收到则为空。</summary>
    public string LastServerETag = "";

    /// <summary>D17：本次 POST 是否已带 <c>If-Match</c>（仅观测）。</summary>
    public bool LastIfMatchWasSent;

    /// <summary>D18：本次 F2 流程是否在 POST 前执行了 <c>GET /state</c> 预取 ETag（仅观测）。</summary>
    public bool LastStateEtagPrefetchRan;

    /// <summary>最近一次 <c>POST /sync</c> 响应 JSON 中的 <c>error</c> 字段（无则空）；纯传输失败时为 <c>transport</c>。</summary>
    public string LastSyncResponseErrorCode = "";

    /// <summary><c>error</c> 配套 <c>detail</c> 的 HUD 截断预览（对象型 <c>detail</c> 为原始片段缩略）。</summary>
    public string LastSyncResponseDetailPreview = "";

    public PlayerProgressSimple progress;
    public PlayerWalletSimple wallet;
    public PlayerInventorySimple inventory;
    public PlayerSkillUnlockSimple unlocks;
    public PlayerSkillMasterySimple mastery;
    public PlayerBankSimple bank;
    public PlayerHealthSimple health;
    PlayerHotkeysSimple hotkeys;

    void Awake() => WireRefs();

    void Reset()
    {
        WireRefs();
    }

    void WireRefs()
    {
        progress = GetComponent<PlayerProgressSimple>();
        wallet = GetComponent<PlayerWalletSimple>();
        inventory = GetComponent<PlayerInventorySimple>();
        unlocks = GetComponent<PlayerSkillUnlockSimple>();
        mastery = GetComponent<PlayerSkillMasterySimple>();
        bank = GetComponent<PlayerBankSimple>();
        health = GetComponent<PlayerHealthSimple>();
    }
}
