using Unity.Netcode;
using UnityEngine;

/// <summary>B2·Day4：Enter=房间（联网时服端广播），` = 系统行（仅 Host/服进程）；单机仍只打 Log。</summary>
public class ChatPlaceholderSimple : MonoBehaviour
{
    public KeyCode sendLocalKey = KeyCode.Return;
    public KeyCode sendSystemKey = KeyCode.BackQuote;
    public string localMessage = "Hello room";
    public string systemMessage = "System notice";
    [TextArea] public string lastLocalSent = "";
    [TextArea] public string lastSystemSent = "";

    void Update()
    {
        MultiplayerPlayerSimple net = GetComponent<MultiplayerPlayerSimple>();
        if (net != null && net.IsSpawned && !net.IsOwner)
            return;
        if (!Application.isFocused)
            return;

        if (Input.GetKeyDown(sendLocalKey))
        {
            if (string.IsNullOrEmpty(localMessage))
            {
                ServerAuditLogSimple.Push(
                    ServerAuditLogSimple.CategorySrvValChatReject,
                    "channel=room&reason=empty");
                return;
            }
            if (IsNetworkSession)
            {
                if (net == null || !net.IsOwner)
                    return;
                net.OwnerSendRoomChat(localMessage);
                lastLocalSent = localMessage;
            }
            else
            {
                lastLocalSent = localMessage;
                Debug.Log("[Room/offline] " + localMessage);
            }
        }
        if (Input.GetKeyDown(sendSystemKey))
        {
            if (string.IsNullOrEmpty(systemMessage))
            {
                ServerAuditLogSimple.Push(
                    ServerAuditLogSimple.CategorySrvValChatReject,
                    "channel=system&reason=empty");
                return;
            }
            if (IsNetworkSession)
            {
                if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
                {
                    ServerAuditLogSimple.Push(
                        ServerAuditLogSimple.CategorySrvValChatReject,
                        "channel=system&reason=not_server");
                    return;
                }
                ChatRoomStateSimple st = ChatRoomStateSimple.Instance;
                if (st == null)
                {
                    ServerAuditLogSimple.Push(
                        ServerAuditLogSimple.CategorySrvValChatReject,
                        "channel=system&reason=no_room_state");
                    return;
                }
                st.ServerPostSystemLine(systemMessage);
                lastSystemSent = systemMessage;
            }
            else
            {
                lastSystemSent = systemMessage;
                Debug.Log("[System/offline] " + systemMessage);
            }
        }
    }

    static bool IsNetworkSession =>
        NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
}
