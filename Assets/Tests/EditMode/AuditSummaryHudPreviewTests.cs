using NUnit.Framework;

/// <summary>D10：契约测试门禁 — 与 <c>persist_sync</c> 返回的 <c>auditSummary</c> 聚合字段缩写一致。</summary>
public class AuditSummaryHudPreviewTests
{
    [Test]
    public void BuildPreview_TotAndAbbreviations()
    {
        const string json =
            "{\"auditSummary\":{\"total\":3,\"SrvVal_IllegalOperation\":1,\"SrvVal_InventoryFull\":2}}";
        string p = PlayerStateExportSimple.AuditSummaryHudPreviewFromJson(json);
        StringAssert.Contains("tot:3", p);
        StringAssert.Contains("SV:1", p);
        StringAssert.Contains("Inv:2", p);
    }

    [Test]
    public void BuildPreview_EmptyWhenNoAuditSummary()
    {
        Assert.AreEqual("", PlayerStateExportSimple.AuditSummaryHudPreviewFromJson("{}"));
    }
}
