using UnityEngine;

/// <summary>D4 kickoff: PvP flag and red-name placeholder.</summary>
public class PlayerPvpSimple : MonoBehaviour
{
    public bool pvpEnabled;
    public int pvpKills;
    public KeyCode togglePvpKey = KeyCode.K;
    PlayerHotkeysSimple hotkeys;

    public bool IsRedName => pvpKills >= 3;
    public bool CanFightNow => pvpEnabled && !SafeZoneSimple.IsInside(transform.position);

    void Update()
    {
        if (hotkeys == null)
            hotkeys = GetComponent<PlayerHotkeysSimple>();
        KeyCode k = hotkeys != null ? hotkeys.togglePvp : togglePvpKey;
        if (Input.GetKeyDown(k))
        {
            pvpEnabled = !pvpEnabled;
            Debug.Log($"PvP {(pvpEnabled ? "Enabled" : "Disabled")}");
            if (pvpEnabled && SafeZoneSimple.IsInside(transform.position))
                ServerAuditLogSimple.Push(ServerAuditLogSimple.CategorySrvValZoneReject, "op=pvp_on_in_safe_zone");
        }
    }
}
