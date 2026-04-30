using Unity.Netcode;
using UnityEngine;

/// <summary>
/// B2·Day4：同会话「房间」聊天广播 + 与系统行分离；不替代商业聊天服。
/// 权威在服；展示在各端 <see cref="LastRoomLineHud"/> / <see cref="LastSystemLineHud"/>。
/// </summary>
public class ChatRoomStateSimple : NetworkBehaviour
{
    public static ChatRoomStateSimple Instance { get; private set; }

    /// <summary>与 <see cref="D3GrowthBalanceData.chatRoomMaxPayloadChars"/> 同步；服端截断/净化用。</summary>
    public static int MaxPayloadChars => Mathf.Clamp(D3GrowthBalance.Load().chatRoomMaxPayloadChars, 16, 1024);

    const int KHistoryBufferCap = 10;
    static int ActiveHistoryLineCount => Mathf.Clamp(D3GrowthBalance.Load().chatRoomHudHistoryLines, 1, KHistoryBufferCap);

    public static string LastRoomLineHud { get; private set; } = string.Empty;
    public static string LastSystemLineHud { get; private set; } = string.Empty;
    public static string RoomHistoryHud => JoinHistory(_roomHistory);
    public static string SystemHistoryHud => JoinHistory(_systemHistory);
    public static ulong RoomBroadcastSeq { get; private set; }
    public static ulong SystemBroadcastSeq { get; private set; }
    static readonly string[] _roomHistory = new string[KHistoryBufferCap];
    static readonly string[] _systemHistory = new string[KHistoryBufferCap];

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        base.OnDestroy();
    }

    /// <summary>仅 <see cref="NetworkManager.IsServer"/> 进程可发系统行（如 Host 按 `）。</summary>
    public void ServerPostSystemLine(string text)
    {
        if (!IsServer)
            return;
        string line = SanitizeForChat(text);
        if (line.Length == 0)
            return;
        string full = "[SYS] " + line;
        if (full.Length > MaxPayloadChars + 6)
            full = full.Substring(0, MaxPayloadChars + 6);
        PushSystemToAllClientRpc(full);
    }

    /// <summary>由 <see cref="MultiplayerPlayerSimple"/> 的 ServerRpc 在服端调用。</summary>
    public void ServerAppendRoomFromPlayer(ulong senderClientId, string text)
    {
        if (!IsServer)
            return;
        string body = SanitizeForChat(text);
        if (body.Length == 0)
            return;
        string head = "[R" + senderClientId + "] ";
        int room = MaxPayloadChars - head.Length;
        if (room < 1)
            return;
        if (body.Length > room)
            body = body.Substring(0, room);
        string full = head + body;
        PushRoomToAllClientRpc(full);
    }

    [ClientRpc]
    void PushRoomToAllClientRpc(string line)
    {
        if (line == null)
            line = string.Empty;
        LastRoomLineHud = line;
        PushHistory(_roomHistory, line);
        RoomBroadcastSeq++;
        Debug.Log(line);
    }

    [ClientRpc]
    void PushSystemToAllClientRpc(string line)
    {
        if (line == null)
            line = string.Empty;
        LastSystemLineHud = line;
        PushHistory(_systemHistory, line);
        SystemBroadcastSeq++;
        Debug.Log(line);
    }

    static void PushHistory(string[] target, string line)
    {
        if (target == null || target.Length == 0)
            return;
        int n = Mathf.Min(ActiveHistoryLineCount, target.Length);
        if (n < 1)
            return;
        for (int i = n - 1; i >= 1; i--)
            target[i] = target[i - 1];
        target[0] = line;
    }

    static string JoinHistory(string[] target)
    {
        if (target == null || target.Length == 0)
            return string.Empty;
        int n = Mathf.Min(ActiveHistoryLineCount, target.Length);
        if (n < 1)
            return string.Empty;
        System.Text.StringBuilder sb = new System.Text.StringBuilder(220);
        for (int i = 0; i < n; i++)
        {
            string s = target[i];
            if (string.IsNullOrEmpty(s))
                continue;
            if (sb.Length > 0)
                sb.Append(" | ");
            sb.Append(s);
        }
        return sb.ToString();
    }

    static string SanitizeForChat(string t)
    {
        if (string.IsNullOrEmpty(t))
            return string.Empty;
        t = t.Replace('\r', ' ').Replace('\n', ' ');
        t = t.Trim();
        if (t.Length > MaxPayloadChars)
            t = t.Substring(0, MaxPayloadChars);
        return t;
    }
}
