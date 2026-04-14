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
            Debug.Log($"[Local] {localMessage}");
            lastLocalSent = localMessage;
        }
        if (Input.GetKeyDown(sendSystemKey))
        {
            Debug.Log($"[System] {systemMessage}");
            lastSystemSent = systemMessage;
        }
    }
}
