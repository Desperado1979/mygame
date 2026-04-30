using UnityEngine;

/// <summary>D4/Day5: PvP flag with safe-zone hard validation and clear HUD hint.</summary>
public class PlayerPvpSimple : MonoBehaviour
{
    public bool pvpEnabled;
    public int pvpKills;
    public KeyCode togglePvpKey = KeyCode.K;
    PlayerHotkeysSimple hotkeys;
    PlayerAreaStateSimple areaState;
    float _hintUntil = float.NegativeInfinity;
    string _lastHint;
    int _redNameKillThreshold = 3;
    float _pvpHintSeconds = 2.5f;

    public bool IsRedName => pvpKills >= _redNameKillThreshold;
    public bool IsInSafeArea => IsInCityArea() || SafeZoneSimple.IsInside(transform.position);
    public bool CanFightNow => pvpEnabled && !IsInSafeArea;
    public bool HasHint => !string.IsNullOrEmpty(_lastHint) && Time.unscaledTime < _hintUntil;
    public string LastHint => HasHint ? _lastHint : string.Empty;

    void Awake()
    {
        D3GrowthBalanceData d = D3GrowthBalance.Load();
        _redNameKillThreshold = Mathf.Max(1, d.pvpRedNameKillThreshold);
        _pvpHintSeconds = Mathf.Max(0.5f, d.pvpHudHintSeconds);
    }

    void Update()
    {
        MultiplayerPlayerSimple net = GetComponent<MultiplayerPlayerSimple>();
        if (net != null && net.IsSpawned && !net.IsOwner)
            return;
        if (!Application.isFocused)
            return;
        if (hotkeys == null)
            hotkeys = GetComponent<PlayerHotkeysSimple>();
        if (areaState == null)
            areaState = GetComponent<PlayerAreaStateSimple>();

        // Day5 硬校验：在安全区/城内强制维持 PvP 关闭，防止文案与可战状态脱节。
        if (pvpEnabled && IsInSafeArea)
        {
            pvpEnabled = false;
            SetHint("禁战区");
            ServerAuditLogSimple.Push(ServerAuditLogSimple.CategorySrvValZoneReject, "op=pvp_force_off_in_safe_zone");
            Debug.Log("PvP Disabled (safe zone)");
        }

        KeyCode k = hotkeys != null ? hotkeys.togglePvp : togglePvpKey;
        if (Input.GetKeyDown(k))
        {
            if (pvpEnabled)
            {
                pvpEnabled = false;
                SetHint("PvP关");
                Debug.Log("PvP Disabled");
                return;
            }

            if (IsInSafeArea)
            {
                pvpEnabled = false;
                SetHint("禁战区");
                ServerAuditLogSimple.Push(ServerAuditLogSimple.CategorySrvValZoneReject, "op=pvp_on_blocked_safe_zone");
                Debug.Log("PvP blocked in safe zone");
                return;
            }

            pvpEnabled = true;
            SetHint("PvP开");
            Debug.Log("PvP Enabled");
        }
    }

    bool IsInCityArea()
    {
        return areaState != null && areaState.IsInCity;
    }

    void SetHint(string text, float seconds = -1f)
    {
        if (seconds < 0f)
            seconds = _pvpHintSeconds;
        _lastHint = text;
        _hintUntil = Time.unscaledTime + Mathf.Max(0.5f, seconds);
    }
}
