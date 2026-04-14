using System;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>D5 step: export current player state JSON for server integration rehearsal.</summary>
public class PlayerStateExportSimple : MonoBehaviour
{
    [Serializable]
    class XpBlock { public int xpIntoLevel; public int xpBank; public int skillPoints; }
    [Serializable]
    class InventoryBlock { public int hpPotion; public int mpPotion; public float weight; }
    [Serializable]
    class SkillsBlock { public int qTier; public int rTier; public int qLevel; public int rLevel; }
    [Serializable]
    class BankBlock { public int gold; public int hpPotion; public int mpPotion; }
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

    public string playerId = "local_player_001";
    public string exportFileName = "player_state_export.json";
    public string auditExportFileName = "audit_events.ndjson";

    public PlayerProgressSimple progress;
    public PlayerWalletSimple wallet;
    public PlayerInventorySimple inventory;
    public PlayerSkillUnlockSimple unlocks;
    public PlayerSkillMasterySimple mastery;
    public PlayerBankSimple bank;
    public PlayerHealthSimple health;
    PlayerHotkeysSimple hotkeys;

    void Reset()
    {
        progress = GetComponent<PlayerProgressSimple>();
        wallet = GetComponent<PlayerWalletSimple>();
        inventory = GetComponent<PlayerInventorySimple>();
        unlocks = GetComponent<PlayerSkillUnlockSimple>();
        mastery = GetComponent<PlayerSkillMasterySimple>();
        bank = GetComponent<PlayerBankSimple>();
        health = GetComponent<PlayerHealthSimple>();
    }

    void Update()
    {
        if (hotkeys == null)
            hotkeys = GetComponent<PlayerHotkeysSimple>();
        KeyCode stateKey = hotkeys != null ? hotkeys.exportStateJson : KeyCode.F12;
        KeyCode auditKey = hotkeys != null ? hotkeys.exportAuditNdjson : KeyCode.F4;
        if (Input.GetKeyDown(stateKey))
            ExportNow();
        if (Input.GetKeyDown(auditKey))
            ExportAuditNow();
    }

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

    PlayerStateDto BuildDto()
    {
        return new PlayerStateDto
        {
            playerId = string.IsNullOrWhiteSpace(playerId) ? "local_player_001" : playerId.Trim(),
            version = 1,
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
