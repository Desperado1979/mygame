using UnityEngine;

/// <summary>
/// 运行时热键入口：与 <see cref="PlayerStateExportSimple.Export"/> / <see cref="PlayerStateExportSimple.Network"/> 分离，便于对照 D5 排练路径。
/// </summary>
public partial class PlayerStateExportSimple
{
    void Update()
    {
        if (hotkeys == null)
            hotkeys = GetComponent<PlayerHotkeysSimple>();

        KeyCode stateKey = hotkeys != null ? hotkeys.exportStateJson : KeyCode.F12;
        KeyCode auditKey = hotkeys != null ? hotkeys.exportAuditNdjson : KeyCode.F4;
        KeyCode requestKey = hotkeys != null ? hotkeys.exportRequestPayloadJson : KeyCode.F3;
        KeyCode postKey = hotkeys != null ? hotkeys.postSyncToServer : KeyCode.F2;
        KeyCode mapKey = hotkeys != null ? hotkeys.metricsAlertsPlayers : KeyCode.LeftBracket;
        KeyCode mdbKey = hotkeys != null ? hotkeys.metricsDashboard : KeyCode.RightBracket;
        KeyCode macKey = hotkeys != null ? hotkeys.metricsAuditCategories : KeyCode.Semicolon;
        KeyCode healthKey = hotkeys != null ? hotkeys.probeServerHealth : KeyCode.F1;
        KeyCode postAlt = hotkeys != null ? hotkeys.postSyncToServerAlt : KeyCode.None;
        KeyCode mapAlt = hotkeys != null ? hotkeys.metricsAlertsPlayersAlt : KeyCode.None;
        KeyCode mdbAlt = hotkeys != null ? hotkeys.metricsDashboardAlt : KeyCode.None;
        KeyCode macAlt = hotkeys != null ? hotkeys.metricsAuditCategoriesAlt : KeyCode.None;
        KeyCode healthAlt = hotkeys != null ? hotkeys.probeServerHealthAlt : KeyCode.None;

        if (Input.GetKeyDown(stateKey))
            ExportNow();
        if (Input.GetKeyDown(auditKey))
            ExportAuditNow();
        if (Input.GetKeyDown(requestKey))
            ExportRequestPayloadNow();

        if (Input.GetKeyDown(healthKey) || (healthAlt != KeyCode.None && Input.GetKeyDown(healthAlt)))
            StartCoroutine(GetHealthProbeRoutine());
        if (Input.GetKeyDown(macKey) || (macAlt != KeyCode.None && Input.GetKeyDown(macAlt)))
            StartCoroutine(GetMetricsAuditCategoriesRoutine());

        if (!networkSyncEnabled)
            return;

        if (Input.GetKeyDown(postKey) || (postAlt != KeyCode.None && Input.GetKeyDown(postAlt)))
            StartCoroutine(PostSyncRoutine());
        if (Input.GetKeyDown(mapKey) || (mapAlt != KeyCode.None && Input.GetKeyDown(mapAlt)))
            StartCoroutine(GetMetricsAlertsPlayersRoutine());
        if (Input.GetKeyDown(mdbKey) || (mdbAlt != KeyCode.None && Input.GetKeyDown(mdbAlt)))
            StartCoroutine(GetMetricsDashboardRoutine());
    }
}
