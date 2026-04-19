using System.Collections.Generic;
using UnityEngine;

/// <summary>D5 kickoff: client-side audit queue placeholder for future server validation.</summary>
public static class ServerAuditLogSimple
{
    /// <summary>与存档规约一致：客户端拒绝执行的操作（如 CD/MP），供 persist_sync 审计链排练；<b>非</b>服务端已判定事件。</summary>
    public const string CategorySrvValIllegalOperation = "SrvVal_IllegalOperation";

    /// <summary>拾取/入包：超重等（<c>TryAddPickup</c> 失败）。</summary>
    public const string CategorySrvValInventoryFull = "SrvVal_InventoryFull";

    /// <summary>拾取：归属保护等，物品存在但不可拾。</summary>
    public const string CategorySrvValPickupDenied = "SrvVal_PickupDenied";

    /// <summary>仓库：存取药/金失败（数量不足、背包满等）。</summary>
    public const string CategorySrvValBankReject = "SrvVal_BankReject";

    /// <summary>金币不足（强化、商店扣款等统一口径）。</summary>
    public const string CategorySrvValWalletReject = "SrvVal_WalletReject";

    /// <summary>技能阶/解锁：SP 不足等。</summary>
    public const string CategorySrvValUnlockReject = "SrvVal_UnlockReject";

    /// <summary>经验池：U/I 投入不足等。</summary>
    public const string CategorySrvValProgressReject = "SrvVal_ProgressReject";

    /// <summary>使用红蓝药：数量 0、治疗/回蓝未生效等。</summary>
    public const string CategorySrvValItemUseReject = "SrvVal_ItemUseReject";

    /// <summary>卖药：无货可卖等。</summary>
    public const string CategorySrvValTradeReject = "SrvVal_TradeReject";

    /// <summary>普攻：范围内无带血敌人（未命中）。</summary>
    public const string CategorySrvValCombatMiss = "SrvVal_CombatMiss";

    /// <summary>读档：无存档键等。</summary>
    public const string CategorySrvValLoadReject = "SrvVal_LoadReject";

    /// <summary>背包扣除：数量不足等（<c>RemoveItemById</c>）。</summary>
    public const string CategorySrvValStorageReject = "SrvVal_StorageReject";

    /// <summary>安全区：PvP 开关为 ON 但角色仍在安全区内（观测；<c>CanFightNow</c> 仍为 false）。</summary>
    public const string CategorySrvValZoneReject = "SrvVal_ZoneReject";

    /// <summary>传送：范围内按 F 但无有效落点（<c>targetPoint</c> 与配置均为空）。</summary>
    public const string CategorySrvValPortalReject = "SrvVal_PortalReject";

    /// <summary>存档：清档（F11）等破坏性本地操作（客户端自述意图，非服务端裁决）。</summary>
    public const string CategorySrvValStateReject = "SrvVal_StateReject";

    /// <summary>聊天占位：发送空串等。</summary>
    public const string CategorySrvValChatReject = "SrvVal_ChatReject";

    /// <summary>队伍占位：满员仍加人、单人下限仍踢人等。</summary>
    public const string CategorySrvValPartyReject = "SrvVal_PartyReject";
    public struct AuditEntry
    {
        public long seq;
        public string tsUtc;
        public string category;
        public string payload;
    }

    static readonly Queue<string> queue = new Queue<string>();
    static readonly Queue<AuditEntry> structured = new Queue<AuditEntry>();
    const int MaxItems = 256;
    static long seqCounter = 0;

    public static void Push(string category, string payload)
    {
        string ts = System.DateTime.UtcNow.ToString("O");
        long seq = ++seqCounter;
        string msg = $"{seq}|{ts}|{category}|{payload}";
        queue.Enqueue(msg);
        structured.Enqueue(new AuditEntry { seq = seq, tsUtc = ts, category = category, payload = payload });
        while (queue.Count > MaxItems)
            queue.Dequeue();
        while (structured.Count > MaxItems)
            structured.Dequeue();
        Debug.Log($"[AUDIT] #{seq} {category} {payload}");
    }

    public static string[] Snapshot()
    {
        return queue.ToArray();
    }

    public static AuditEntry[] SnapshotStructured()
    {
        return structured.ToArray();
    }
}
