using System.Collections.Generic;
using UnityEngine;

/// <summary>D5 kickoff: client-side audit queue placeholder for future server validation.</summary>
public static class ServerAuditLogSimple
{
    public struct AuditEntry
    {
        public long seq;
        public string tsUtc;
        public string category;
        public string payload;
    }

    static readonly Queue<string> queue = new Queue<string>();
    static readonly Queue<AuditEntry> structured = new Queue<AuditEntry>();
    const int MaxItems = 120;
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
