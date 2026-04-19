using UnityEngine;

/// <summary>D4 kickoff: local chat placeholder (prints to console).</summary>
public class ChatPlaceholderSimple : MonoBehaviour
{
    public KeyCode sendLocalKey = KeyCode.Return;
    public KeyCode sendSystemKey = KeyCode.BackQuote;
    public string localMessage = "Hello local channel";
    public string systemMessage = "System: maintenance placeholder";
    [TextArea] public string lastLocalSent = "";
    [TextArea] public string lastSystemSent = "";

    void Update()
    {
        if (Input.GetKeyDown(sendLocalKey))
        {
            if (string.IsNullOrEmpty(localMessage))
                ServerAuditLogSimple.Push(ServerAuditLogSimple.CategorySrvValChatReject, "channel=local&reason=empty");
            else
            {
                Debug.Log($"[Local] {localMessage}");
                lastLocalSent = localMessage;
            }
        }
        if (Input.GetKeyDown(sendSystemKey))
        {
            if (string.IsNullOrEmpty(systemMessage))
                ServerAuditLogSimple.Push(ServerAuditLogSimple.CategorySrvValChatReject, "channel=system&reason=empty");
            else
            {
                Debug.Log($"[System] {systemMessage}");
                lastSystemSent = systemMessage;
            }
        }
    }
}
