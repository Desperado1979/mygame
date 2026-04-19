using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public partial class PlayerStateExportSimple
{
    const int MaxSyncPostAttempts = 2;
    const string EtagPrefsPrefix = "EOD_SYNC_ETAG_";

    static string PrefsKeyForPlayerEtag(string playerId)
    {
        if (string.IsNullOrEmpty(playerId)) playerId = "anonymous";
        var sb = new StringBuilder(EtagPrefsPrefix, EtagPrefsPrefix.Length + playerId.Length);
        foreach (char c in playerId)
        {
            if (char.IsLetterOrDigit(c) || c == '_' || c == '-') sb.Append(c);
            else sb.Append('_');
        }
        string k = sb.ToString();
        return k.Length > 200 ? k.Substring(0, 200) : k;
    }

    static string NormalizeEtagHeader(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";
        string s = raw.Trim();
        if (s.Length >= 2 && s[0] == '"' && s[s.Length - 1] == '"')
            s = s.Substring(1, s.Length - 2);
        return s.Trim();
    }

    string LoadSavedEtag(string playerId) => PlayerPrefs.GetString(PrefsKeyForPlayerEtag(playerId), "");

    void SaveSavedEtag(string playerId, string etagHexNoQuotes)
    {
        if (string.IsNullOrEmpty(etagHexNoQuotes)) return;
        PlayerPrefs.SetString(PrefsKeyForPlayerEtag(playerId), etagHexNoQuotes);
        PlayerPrefs.Save();
    }

    public IEnumerator PostSyncRoutine()
    {
        if (!networkSyncEnabled)
            yield break;

        string baseUrl = ResolveBaseUrl();
        string url = baseUrl.TrimEnd('/') + "/sync";
        ClientSyncRequestV1 body = BuildSyncRequest(true);
        string json = JsonUtility.ToJson(body, true);
        byte[] raw = Encoding.UTF8.GetBytes(json);
        LastSyncRetryCount = 0;
        string pidForEtag = body != null && !string.IsNullOrEmpty(body.playerId) ? body.playerId : playerId;

        for (int attempt = 1; attempt <= MaxSyncPostAttempts; attempt++)
        {
            using (UnityWebRequest req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                req.uploadHandler = new UploadHandlerRaw(raw);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                LastSyncDurationMs = -1;
                LastIfMatchWasSent = false;
                if (syncUseIfMatch)
                {
                    string saved = LoadSavedEtag(pidForEtag);
                    if (!string.IsNullOrEmpty(saved))
                    {
                        req.SetRequestHeader("If-Match", "\"" + saved + "\"");
                        LastIfMatchWasSent = true;
                    }
                }
                yield return req.SendWebRequest();

                LastHttpCode = (int)req.responseCode;
                string text = req.downloadHandler != null ? req.downloadHandler.text : "";
                LastPostResponseFull = text;
                LastPostResponsePreview = Truncate(text, 400);
                LastSyncError = req.result != UnityWebRequest.Result.Success ? req.error : "";
                LastSyncPostStatusTag = ClassifySyncPostStatus(LastHttpCode, text);
                string durHdr = req.GetResponseHeader("X-Sync-Duration-Ms");
                LastSyncDurationMs = int.TryParse(durHdr, out int dms) ? dms : -1;

                string etagHdr = req.GetResponseHeader("ETag");
                if (string.IsNullOrEmpty(etagHdr)) etagHdr = req.GetResponseHeader("etag");
                string etagNorm = NormalizeEtagHeader(etagHdr);
                LastServerETag = etagNorm;
                if (!string.IsNullOrEmpty(etagNorm) && (LastHttpCode == 200 || LastHttpCode == 412))
                    SaveSavedEtag(pidForEtag, etagNorm);

                if (LastHttpCode == 429 && attempt < MaxSyncPostAttempts)
                {
                    LastSyncRetryCount = attempt;
                    string ra = req.GetResponseHeader("Retry-After");
                    float waitSec = 2f;
                    if (int.TryParse(ra, out int sec)) waitSec = Mathf.Clamp(sec, 1, 60);
                    Debug.LogWarning($"POST /sync 429, retry after {waitSec}s (attempt {attempt}/{MaxSyncPostAttempts})");
                    yield return new WaitForSeconds(waitSec);
                    continue;
                }

                if (req.result == UnityWebRequest.Result.Success && LastHttpCode >= 200 && LastHttpCode < 300)
                    ParseAndApplyServerSyncResponse(text);
                else
                {
                    LastSyncWarnLow = LastSyncWarnHigh = 0;
                    LastWarningsCodesPreview = "";
                    LastNetAlertHigh = false;
                    LastSyncValidationOk = null;
                    LastAuditCategoryPreview = "";
                    LastMetricsAuditCategoriesPreview = "";
                }

                TryWriteSyncSnapshotFile();
                yield break;
            }
        }
    }

    public IEnumerator GetMetricsAlertsPlayersRoutine()
    {
        string url = ResolveBaseUrl().TrimEnd('/') + "/metrics/alerts/players?days=7&top=10&minAcceptRate=95";
        yield return SimpleGet(url, (code, body) =>
        {
            LastMetricsAlertsPreview = Truncate(body, 240);
            LastHttpCode = code;
        });
    }

    public IEnumerator GetMetricsDashboardRoutine()
    {
        string url = ResolveBaseUrl().TrimEnd('/') + "/metrics/dashboard?days=7&top=5&hours=24&compareHours=24";
        yield return SimpleGet(url, (code, body) =>
        {
            LastMetricsDashboardPreview = Truncate(body, 240);
            LastHttpCode = code;
        });
    }

    public IEnumerator GetMetricsAuditCategoriesRoutine()
    {
        string url = ResolveBaseUrl().TrimEnd('/') + "/metrics/audit-categories?days=7";
        yield return SimpleGet(url, (code, body) =>
        {
            LastMetricsAuditCategoriesPreview = Truncate(body, 280);
            LastHttpCode = code;
        });
    }

    public IEnumerator GetHealthProbeRoutine()
    {
        string url = ResolveBaseUrl().TrimEnd('/') + "/health";
        yield return SimpleGet(url, (code, body) =>
        {
            LastHealthProbePreview = code >= 200 && code < 300 ? Truncate(body, 120) : $"err:{code}";
            LastHttpCode = code;
        });
    }

    IEnumerator SimpleGet(string url, Action<int, string> done)
    {
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();
            int code = (int)req.responseCode;
            string text = req.downloadHandler != null ? req.downloadHandler.text : "";
            done(code, text);
        }
    }

    void TryWriteSyncSnapshotFile()
    {
        try
        {
            string path = Path.Combine(Application.persistentDataPath, lastSyncSnapshotFileName);
            File.WriteAllText(path, LastPostResponseFull ?? "");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"sync snapshot write failed: {e.Message}");
        }
    }

    string ResolveBaseUrl()
    {
        string prefs = PlayerPrefs.GetString(SyncBaseUrlPrefsKey, "");
        if (!string.IsNullOrWhiteSpace(prefs))
            return prefs.Trim();
        return string.IsNullOrWhiteSpace(syncBaseUrl) ? "http://127.0.0.1:8787" : syncBaseUrl.Trim();
    }

    static string ClassifySyncPostStatus(int httpCode, string body)
    {
        if (httpCode >= 200 && httpCode < 300) return "ok";
        switch (httpCode)
        {
            case 429: return "ratelimit";
            case 412: return "precond";
            case 503: return "maint";
            case 401: return "hmac";
            case 403: return "staging";
            case 400:
                if (!string.IsNullOrEmpty(body) && body.IndexOf("srvval_", StringComparison.OrdinalIgnoreCase) >= 0)
                    return "srvval";
                if (!string.IsNullOrEmpty(body) && body.IndexOf("issued_at", StringComparison.OrdinalIgnoreCase) >= 0)
                    return "time";
                return "badreq";
            default:
                return httpCode > 0 ? "http" + httpCode : "err";
        }
    }
}
