#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

/// <summary>D10：与 <c>persist_sync</c> 的 <c>auditSummary</c> HUD 短串一致；放在 Editor 默认程序集，避免独立测试 asmdef 对 <c>Assembly-CSharp</c> 引用异常。</summary>
public static class AuditSummaryHudPreviewSelfTest
{
    [MenuItem("EpochOfDawn/Tests/Run Audit HUD Preview Self-Test")]
    public static void RunMenu()
    {
        try
        {
            RunCore();
            Debug.Log("[AuditSummaryHudPreviewSelfTest] OK");
        }
        catch (Exception ex)
        {
            Debug.LogError("[AuditSummaryHudPreviewSelfTest] FAILED: " + ex.Message);
            throw;
        }
    }

    static void RunCore()
    {
        const string json =
            "{\"auditSummary\":{\"total\":3,\"SrvVal_IllegalOperation\":1,\"SrvVal_InventoryFull\":2}}";
        string p = PlayerStateExportSimple.AuditSummaryHudPreviewFromJson(json);
        if (p.IndexOf("tot:3", StringComparison.Ordinal) < 0)
            throw new Exception("expected tot:3 in: " + p);
        if (p.IndexOf("SV:1", StringComparison.Ordinal) < 0)
            throw new Exception("expected SV:1 in: " + p);
        if (p.IndexOf("Inv:2", StringComparison.Ordinal) < 0)
            throw new Exception("expected Inv:2 in: " + p);

        string empty = PlayerStateExportSimple.AuditSummaryHudPreviewFromJson("{}");
        if (empty != "")
            throw new Exception("expected empty string, got: " + empty);

        const string rejectJson =
            "{\"ok\":false,\"error\":\"srvval_audit_block\",\"auditSummary\":{\"total\":2,\"SrvVal_LoadReject\":2},\"validation\":{\"ok\":false,\"warningSummary\":{\"low\":0,\"high\":0}}}";
        string rej = PlayerStateExportSimple.AuditSummaryHudPreviewFromJson(rejectJson);
        if (rej.IndexOf("Ld:2", StringComparison.Ordinal) < 0)
            throw new Exception("expected Ld:2 in: " + rej);

        if (!PlayerStateExportSimple.SyncResponseBodyMayHaveHudFields(rejectJson))
            throw new Exception("SyncResponseBodyMayHaveHudFields expected true for rejectJson");
        if (PlayerStateExportSimple.SyncResponseBodyMayHaveHudFields("{\"ok\":false,\"error\":\"invalid_json\"}"))
            throw new Exception("SyncResponseBodyMayHaveHudFields expected false for bare error");

        PlayerStateExportSimple.ParseSyncErrorEnvelope(
            "{\"ok\":false,\"error\":\"precondition_failed\",\"detail\":\"if_match_mismatch\"}",
            out string ec,
            out string dp);
        if (ec != "precondition_failed")
            throw new Exception("ParseSyncErrorEnvelope error code mismatch: " + ec);
        if (dp.IndexOf("if_match", StringComparison.Ordinal) < 0)
            throw new Exception("ParseSyncErrorEnvelope detail mismatch: " + dp);
    }
}
#endif
