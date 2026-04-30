using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// B2 / Day1：在波次刷怪中心上方显示公共目标短文案（波次、参与态），与 <see cref="DebugHudSimple"/> 的公目行同源配置。
/// <para><b>[Client-Side Expression]</b>：仅表现与可读性，权威仍以服端 NetworkVariable / 单机 LocalState 为准。</para>
/// </summary>
[DisallowMultipleComponent]
public class PublicObjectiveWorldHintSimple : MonoBehaviour
{
    WaveSpawnerSimple _spawner;
    P1AContentConfig _cfg;
    Canvas _canvas;
    Text _text;
    Transform _billboard;
    float _nextRefresh;
    float _refreshIntervalSec = 0.2f;

    void Awake()
    {
        _refreshIntervalSec = Mathf.Max(0.05f, D3GrowthBalance.Load().publicObjectiveWorldHintRefreshSec);
        _spawner = GetComponent<WaveSpawnerSimple>();
        _cfg = P1AContentConfig.TryLoadDefault();
        if (_cfg != null && !_cfg.publicObjectiveWorldHintEnabled)
        {
            enabled = false;
            return;
        }
    }

    void Start()
    {
        if (!enabled || _spawner == null)
            return;
        BuildUiIfNeeded();
    }

    void LateUpdate()
    {
        if (!enabled || _spawner == null || _text == null || _billboard == null)
            return;
        if (Time.unscaledTime < _nextRefresh)
            return;
        _nextRefresh = Time.unscaledTime + _refreshIntervalSec;

        if (_cfg != null && !_cfg.publicObjectiveWorldHintEnabled)
        {
            _canvas.enabled = false;
            return;
        }

        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 toCam = cam.transform.position - _billboard.position;
            if (toCam.sqrMagnitude > 0.0001f)
                _billboard.rotation = Quaternion.LookRotation(toCam) * Quaternion.Euler(0f, 180f, 0f);
        }

        string line = BuildLine();
        bool show = !string.IsNullOrEmpty(line);
        _canvas.enabled = show;
        if (show)
            _text.text = line;
    }

    void BuildUiIfNeeded()
    {
        if (_canvas != null)
            return;

        Transform anchor = _spawner.center != null ? _spawner.center : _spawner.transform;
        float yOff = _cfg != null ? _cfg.publicObjectiveWorldHintCenterYOffset : 3.5f;
        Vector3 scale = _cfg != null ? _cfg.publicObjectiveWorldHintScale : new Vector3(0.014f, 0.014f, 0.014f);

        _billboard = new GameObject("PublicObjectiveWorldHint").transform;
        _billboard.SetParent(anchor, false);
        _billboard.localPosition = new Vector3(0f, yOff, 0f);
        _billboard.localRotation = Quaternion.identity;
        _billboard.localScale = Vector3.one;

        GameObject canvasGo = new GameObject("Canvas");
        canvasGo.transform.SetParent(_billboard, false);
        canvasGo.transform.localPosition = Vector3.zero;
        canvasGo.transform.localRotation = Quaternion.identity;
        canvasGo.transform.localScale = scale;

        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.worldCamera = Camera.main;
        _canvas.sortingOrder = 80;

        RectTransform crt = canvasGo.GetComponent<RectTransform>();
        crt.sizeDelta = new Vector2(900f, 220f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(canvasGo.transform, false);
        _text = textGo.AddComponent<Text>();
        _text.font = font;
        _text.fontSize = _cfg != null ? Mathf.Clamp(_cfg.publicObjectiveWorldHintFontSize, 18, 96) : 44;
        _text.color = new Color(1f, 0.96f, 0.85f, 1f);
        _text.alignment = TextAnchor.MiddleCenter;
        _text.horizontalOverflow = HorizontalWrapMode.Wrap;
        _text.verticalOverflow = VerticalWrapMode.Truncate;
        _text.raycastTarget = false;

        RectTransform trt = _text.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
    }

    string BuildLine()
    {
        if (_cfg == null)
            _cfg = P1AContentConfig.TryLoadDefault();
        if (_cfg == null)
            return string.Empty;

        PublicObjectiveWaveDisplayUtil.GetWaveDisplay(_spawner, out int w1, out int wtot, out bool allDone);
        if (allDone)
            return $"{_cfg.publicObjectiveHudTag} {_cfg.publicObjectiveSegmentDone}";

        Transform player = ResolveLocalPlayer();
        PlayerAreaStateSimple area = player != null ? player.GetComponent<PlayerAreaStateSimple>() : null;
        bool participating = IsParticipating(player, area);

        string part = participating ? _cfg.publicObjectiveParticipating : _cfg.publicObjectiveObserving;
        if (area != null && area.IsInCity)
            part = _cfg.publicObjectiveInTown;

        return $"{_cfg.publicObjectiveHudTag} {_cfg.publicObjectiveInProgress} · {_cfg.publicObjectiveWaveKey}{w1}/{wtot} · {_cfg.publicObjectiveParticipationKey}{part}";
    }

    static Transform ResolveLocalPlayer()
    {
        MultiplayerPlayerSimple[] arr = FindObjectsByType<MultiplayerPlayerSimple>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < arr.Length; i++)
        {
            if (arr[i] != null && arr[i].IsOwner)
                return arr[i].transform;
        }
        if (PlayerHealthSimple.Instance != null)
            return PlayerHealthSimple.Instance.transform;
        return null;
    }

    bool IsParticipating(Transform player, PlayerAreaStateSimple area)
    {
        if (_spawner == null || player == null)
            return false;
        PublicObjectiveWaveDisplayUtil.GetWaveDisplay(_spawner, out _, out _, out bool wAllDone);
        if (wAllDone)
            return false;
        Vector3 c = _spawner.center != null ? _spawner.center.position : _spawner.transform.position;
        float joinRadius = PublicObjectiveWaveDisplayUtil.ParticipationJoinRadius(_spawner);
        bool inJoinRange = Vector3.Distance(player.position, c) <= joinRadius;
        bool inField = area == null || !area.IsInCity;
        return inJoinRange && inField;
    }
}
