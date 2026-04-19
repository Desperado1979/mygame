using System;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// F12/F4/F3 导出与 <c>BuildSyncRequest</c> 形状。**契约真源**：仓库内 JSON Schema（与 <c>npm run validate</c> 一致）。
/// <list type="bullet">
/// <item><description><c>EpochOfDawn/server/schemas/client_sync_request.schema.json</c> — 整包 <see cref="ClientSyncRequestV1"/>（标题 ClientSyncRequestPayloadV1）。</description></item>
/// <item><description><c>EpochOfDawn/server/schemas/player_state.schema.json</c> — 嵌套 <see cref="PlayerStateDto"/>（标题 PlayerStateExportV1）。</description></item>
/// <item><description><c>EpochOfDawn/server/schemas/audit_event.schema.json</c> — <see cref="AuditSyncItem"/> 数组元素（标题 AuditEventV1）。</description></item>
/// </list>
/// 字段名须与 Schema 一致（Unity <c>JsonUtility</c> 区分大小写）。扩列时先改 Schema 与 persist_sync，再改此处。
/// </summary>
public partial class PlayerStateExportSimple
{
    /// <summary>与 <c>client_sync_request.schema.json</c> 顶层 <c>schemaVersion</c> const <c>1</c> 一致。</summary>
    public const int ClientSyncRequestSchemaVersion = 1;

    /// <summary>与 <c>player_state.schema.json</c> 中 <c>version</c> 一致（当前约定）。</summary>
    public const int PlayerStateExportSchemaVersion = 1;

    /// <summary>与 <c>client_sync_request.schema.json</c> → <c>meta.metaSchemaVersion</c> minimum 1 对齐。</summary>
    public const int ClientSyncMetaSchemaVersion = 1;

    /// <summary>与 <c>meta.saveSchemaVersion</c> minimum 1 对齐。</summary>
    public const int SaveSnapshotSchemaVersion = 1;

    [Serializable]
    class XpBlock
    {
        public int xpIntoLevel;
        public int xpBank;
        public int skillPoints;
    }

    [Serializable]
    class InventoryBlock
    {
        public int hpPotion;
        public int mpPotion;
        public float weight;
    }

    [Serializable]
    class SkillsBlock
    {
        public int qTier;
        public int rTier;
        public int qLevel;
        public int rLevel;
    }

    [Serializable]
    class BankBlock
    {
        public int gold;
        public int hpPotion;
        public int mpPotion;
    }

    /// <summary>对应 <c>player_state.schema.json</c>（PlayerStateExportV1）。</summary>
    [Serializable]
    class PlayerStateDto
    {
        public string playerId;
        public int version;
        public int level;
        public XpBlock xp;
        public int gold;
        public InventoryBlock inventory;
        public SkillsBlock skills;
        public BankBlock bank;
        public int hpNow;
        public int hpMax;
        public string exportedAtUtc;
    }

    /// <summary>对应 <c>client_sync_request.schema.json</c> → <c>meta</c>。</summary>
    [Serializable]
    class ClientSyncMetaV1
    {
        public int metaSchemaVersion;
        public string unityVersion;
        public int saveSchemaVersion;
        public string sessionRunId;
        public string productName;
        public string signaturePlaceholder;
    }

    /// <summary>对应 <c>audit_event.schema.json</c>（单条审计）。数组项 <c>seq</c> 在 Schema 中为 <c>minimum: 1</c>；运行时以服务端校验为准。</summary>
    [Serializable]
    class AuditSyncItem
    {
        public long seq;
        public string ts;
        public string category;
        public string payload;
    }

    /// <summary>对应 <c>client_sync_request.schema.json</c> 根对象（ClientSyncRequestPayloadV1）。</summary>
    [Serializable]
    class ClientSyncRequestV1
    {
        public int schemaVersion;
        public string mode;
        public string issuedAtUtc;
        public string playerId;
        public ClientSyncMetaV1 meta;
        public PlayerStateDto state;
        public AuditSyncItem[] audit;
    }

    const string SessionRunIdPrefsKey = "EOD_SESSION_RUN_ID";

    public void ExportNow()
    {
        PlayerStateDto dto = BuildDto();
        string json = JsonUtility.ToJson(dto, true);
        string path = Path.Combine(Application.persistentDataPath, exportFileName);
        File.WriteAllText(path, json);
        Debug.Log($"State exported: {path}");
    }

    public void ExportAuditNow()
    {
        ServerAuditLogSimple.AuditEntry[] events = ServerAuditLogSimple.SnapshotStructured();
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < events.Length; i++)
        {
            string line = $"{{\"seq\":{events[i].seq},\"ts\":\"{Escape(events[i].tsUtc)}\",\"category\":\"{Escape(events[i].category)}\",\"payload\":\"{Escape(events[i].payload)}\"}}";
            sb.AppendLine(line);
        }

        string path = Path.Combine(Application.persistentDataPath, auditExportFileName);
        File.WriteAllText(path, sb.ToString());
        Debug.Log($"Audit exported: {path} ({events.Length} events)");
    }

    public void ExportRequestPayloadNow()
    {
        ClientSyncRequestV1 req = BuildSyncRequest(false);
        string json = JsonUtility.ToJson(req, true);
        string path = Path.Combine(Application.persistentDataPath, requestExportFileName);
        File.WriteAllText(path, json);
        Debug.Log($"Sync request payload exported: {path}");
    }

    ClientSyncRequestV1 BuildSyncRequest(bool forNetworkPost)
    {
        ServerAuditLogSimple.AuditEntry[] events = ServerAuditLogSimple.SnapshotStructured();
        var audit = new AuditSyncItem[events.Length];
        for (int i = 0; i < events.Length; i++)
        {
            audit[i] = new AuditSyncItem
            {
                seq = events[i].seq,
                ts = events[i].tsUtc,
                category = events[i].category,
                payload = events[i].payload
            };
        }

        string pid = string.IsNullOrWhiteSpace(playerId) ? "local_player_001" : playerId.Trim();
        return new ClientSyncRequestV1
        {
            schemaVersion = ClientSyncRequestSchemaVersion,
            mode = forNetworkPost ? "sync_rehearsal" : "dry_run_no_network",
            issuedAtUtc = DateTime.UtcNow.ToString("o"),
            playerId = pid,
            meta = new ClientSyncMetaV1
            {
                metaSchemaVersion = ClientSyncMetaSchemaVersion,
                unityVersion = Application.unityVersion,
                saveSchemaVersion = SaveSnapshotSchemaVersion,
                sessionRunId = GetOrCreateSessionRunId(),
                productName = Application.productName,
                signaturePlaceholder = ""
            },
            state = BuildDto(),
            audit = audit
        };
    }

    static string GetOrCreateSessionRunId()
    {
        string id = PlayerPrefs.GetString(SessionRunIdPrefsKey, "");
        if (string.IsNullOrEmpty(id))
        {
            id = Guid.NewGuid().ToString("N");
            PlayerPrefs.SetString(SessionRunIdPrefsKey, id);
            PlayerPrefs.Save();
        }
        return id;
    }

    PlayerStateDto BuildDto()
    {
        return new PlayerStateDto
        {
            playerId = string.IsNullOrWhiteSpace(playerId) ? "local_player_001" : playerId.Trim(),
            version = PlayerStateExportSchemaVersion,
            level = progress != null ? progress.level : 1,
            xp = new XpBlock
            {
                xpIntoLevel = progress != null ? progress.xpIntoCurrentLevel : 0,
                xpBank = progress != null ? progress.xpBank : 0,
                skillPoints = progress != null ? progress.skillUnlockPoints : 0
            },
            gold = wallet != null ? wallet.Gold : 0,
            inventory = new InventoryBlock
            {
                hpPotion = inventory != null ? inventory.HpPotionCount : 0,
                mpPotion = inventory != null ? inventory.MpPotionCount : 0,
                weight = inventory != null ? inventory.CurrentWeight : 0f
            },
            skills = new SkillsBlock
            {
                qTier = unlocks != null ? unlocks.burstTier : 1,
                rTier = unlocks != null ? unlocks.frostTier : 1,
                qLevel = mastery != null ? mastery.burstSkillLevel : 1,
                rLevel = mastery != null ? mastery.frostSkillLevel : 1
            },
            bank = new BankBlock
            {
                gold = bank != null ? bank.bankGold : 0,
                hpPotion = bank != null ? bank.bankHpPotion : 0,
                mpPotion = bank != null ? bank.bankMpPotion : 0
            },
            hpNow = health != null ? health.CurrentHp : 0,
            hpMax = health != null ? health.maxHp : 0,
            exportedAtUtc = DateTime.UtcNow.ToString("o")
        };
    }

    static string Escape(string s)
    {
        if (s == null) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
