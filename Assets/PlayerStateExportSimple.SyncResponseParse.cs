using System;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// 解析 <c>persist_sync</c> <c>POST /sync</c> 响应中的校验摘要（<c>validation.warningSummary</c>、<c>warningsByCode</c>）。
/// <para><b>里程碑原则（服务端可信）：</b>是否通过同步、是否阻断，以服务端校验与配置为准；
/// 此处仅将响应中的摘要解析到客户端 HUD 观测字段，并配合 <see cref="PlayerStateExportSimple.Network"/> 落盘快照，<b>不作</b>权威判定。</para>
/// </summary>
public partial class PlayerStateExportSimple
{
    /// <summary>从 <c>POST /sync</c>（及同类 JSON）体解析 <c>error</c> / <c>detail</c>，供 HUD 与自检。</summary>
    public static void ParseSyncErrorEnvelope(string json, out string errorCode, out string detailPreview)
    {
        errorCode = "";
        detailPreview = "";
        if (string.IsNullOrEmpty(json)) return;

        Match me = Regex.Match(json, "\"error\"\\s*:\\s*\"([^\"]*)\"");
        if (me.Success) errorCode = me.Groups[1].Value;

        Match md = Regex.Match(json, "\"detail\"\\s*:\\s*\"([^\"]*)\"");
        if (md.Success)
        {
            detailPreview = CollapseWs(Truncate(md.Groups[1].Value, 120));
            return;
        }

        if (json.IndexOf("\"detail\"", StringComparison.Ordinal) < 0 || string.IsNullOrEmpty(errorCode))
            return;

        int d = json.IndexOf("\"detail\"", StringComparison.Ordinal);
        int colon = json.IndexOf(':', d);
        if (colon < 0) return;
        int start = colon + 1;
        while (start < json.Length && char.IsWhiteSpace(json[start])) start++;
        if (start >= json.Length) return;
        int span = Math.Min(160, json.Length - start);
        string slice = json.Substring(start, span).Trim();
        detailPreview = CollapseWs(Truncate(slice, 120));
    }

    /// <summary>在每次 <c>POST /sync</c> 完成后刷新；<paramref name="transportErr"/> 为 Unity 传输错误且 JSON 无 <c>error</c> 时用 <c>transport</c>。</summary>
    void ApplySyncErrorEnvelopeFromBody(string json, string transportErr)
    {
        ParseSyncErrorEnvelope(json, out LastSyncResponseErrorCode, out LastSyncResponseDetailPreview);
        if (!string.IsNullOrEmpty(LastSyncResponseErrorCode) || !string.IsNullOrEmpty(LastSyncResponseDetailPreview))
            return;
        if (string.IsNullOrEmpty(transportErr)) return;
        LastSyncResponseErrorCode = "transport";
        LastSyncResponseDetailPreview = CollapseWs(Truncate(transportErr, 96));
    }

    static string CollapseWs(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return Regex.Replace(s.Trim(), "\\s+", " ");
    }

    void ParseAndApplyServerSyncResponse(string json)
    {
        ParseWarningSummarySlice(json, out int low, out int high);
        LastSyncWarnLow = low;
        LastSyncWarnHigh = high;
        LastWarningsCodesPreview = BuildWarningsByCodePreview(json);
        LastNetAlertHigh = LastSyncWarnHigh > 0;
        LastSyncValidationOk = TryParseValidationOk(json);
        LastAuditCategoryPreview = BuildAuditSummaryHudPreview(json);
    }

    /// <summary>从 <c>auditSummary</c> 拼 HUD 短串（<c>tot</c> + 各类 <c>SrvVal_*</c> 缩写），与 <c>persist_sync.cjs</c> 聚合字段一致。</summary>
    static string BuildAuditSummaryHudPreview(string json)
    {
        int i = json.IndexOf("\"auditSummary\"", StringComparison.Ordinal);
        if (i < 0) return "";
        int len = Math.Min(2200, json.Length - i);
        if (len <= 0) return "";
        string slice = json.Substring(i, len);
        var sb = new StringBuilder();
        Match mt = Regex.Match(slice, "\"total\"\\s*:\\s*(\\d+)");
        if (mt.Success)
            sb.Append("tot:").Append(mt.Groups[1].Value);

        void Cat(string key, string abbrev)
        {
            Match m = Regex.Match(slice, "\"" + Regex.Escape(key) + "\"\\s*:\\s*(\\d+)");
            if (!m.Success) return;
            if (sb.Length > 0) sb.Append(' ');
            sb.Append(abbrev).Append(':').Append(m.Groups[1].Value);
        }

        Cat("SrvVal_IllegalOperation", "SV");
        Cat("SrvVal_InventoryFull", "Inv");
        Cat("SrvVal_PickupDenied", "Pk");
        Cat("SrvVal_BankReject", "Bn");
        Cat("SrvVal_WalletReject", "W");
        Cat("SrvVal_UnlockReject", "Un");
        Cat("SrvVal_ProgressReject", "Pr");
        Cat("SrvVal_ItemUseReject", "It");
        Cat("SrvVal_TradeReject", "Tr");
        Cat("SrvVal_CombatMiss", "Cm");
        Cat("SrvVal_LoadReject", "Ld");
        Cat("SrvVal_StorageReject", "St");
        Cat("SrvVal_ZoneReject", "Zo");
        Cat("SrvVal_PortalReject", "Pt");
        Cat("SrvVal_StateReject", "Sa");
        Cat("SrvVal_ChatReject", "Ch");
        Cat("SrvVal_PartyReject", "Pa");
        return sb.ToString();
    }

    /// <summary>供编辑器自检菜单与调试：与 <see cref="BuildAuditSummaryHudPreview"/> 相同，从整段同步响应 JSON 生成 HUD 审计摘要短串。</summary>
    public static string AuditSummaryHudPreviewFromJson(string json) => BuildAuditSummaryHudPreview(json);

    /// <summary>从响应 JSON 中取 <c>validation.ok</c>（见 <c>persist_sync.cjs</c> 200/400 体）。解析失败则 <c>null</c>。</summary>
    static bool? TryParseValidationOk(string json)
    {
        int i = json.IndexOf("\"validation\"", StringComparison.Ordinal);
        if (i < 0) return null;
        int len = Math.Min(1500, json.Length - i);
        if (len <= 0) return null;
        string slice = json.Substring(i, len);
        Match m = Regex.Match(slice, "\"ok\"\\s*:\\s*(true|false)");
        if (!m.Success) return null;
        return m.Groups[1].Value == "true";
    }

    static void ParseWarningSummarySlice(string json, out int low, out int high)
    {
        low = high = 0;
        int i = json.IndexOf("\"warningSummary\"", StringComparison.Ordinal);
        if (i < 0) return;
        int len = Math.Min(500, json.Length - i);
        if (len <= 0) return;
        string slice = json.Substring(i, len);
        Match ml = Regex.Match(slice, "\"low\"\\s*:\\s*(\\d+)");
        Match mh = Regex.Match(slice, "\"high\"\\s*:\\s*(\\d+)");
        if (ml.Success) int.TryParse(ml.Groups[1].Value, out low);
        if (mh.Success) int.TryParse(mh.Groups[1].Value, out high);
    }

    static string BuildWarningsByCodePreview(string json, int maxPairs = 4)
    {
        Match m = Regex.Match(json, "\"warningsByCode\"\\s*:\\s*\\{([^}]*)\\}");
        if (!m.Success) return "";
        string inner = m.Groups[1].Value;
        MatchCollection pairs = Regex.Matches(inner, "\"([^\"]+)\"\\s*:\\s*(\\d+)");
        StringBuilder sb = new StringBuilder();
        int n = 0;
        foreach (Match x in pairs)
        {
            if (n > 0) sb.Append(',');
            sb.Append(x.Groups[1].Value).Append(':').Append(x.Groups[2].Value);
            n++;
            if (n >= maxPairs) break;
        }
        string s = sb.ToString();
        return s.Length > 48 ? s.Substring(0, 48) + "…" : s;
    }

    static string Truncate(string s, int max)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Length <= max ? s : s.Substring(0, max) + "…";
    }

    /// <summary>非 2xx 响应若仍含校验/审计片段，则解析到 HUD（如 400 <c>high_warning_block</c> 带 <c>auditSummary</c>）。</summary>
    public static bool SyncResponseBodyMayHaveHudFields(string body)
    {
        if (string.IsNullOrEmpty(body)) return false;
        return body.IndexOf("\"auditSummary\"", StringComparison.Ordinal) >= 0
               || body.IndexOf("\"warningSummary\"", StringComparison.Ordinal) >= 0
               || body.IndexOf("\"warningsByCode\"", StringComparison.Ordinal) >= 0;
    }
}
