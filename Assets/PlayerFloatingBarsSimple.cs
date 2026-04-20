using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>玩家头顶血条 + 蓝条（MP），世界空间 UGUI，内置白贴图。</summary>
[RequireComponent(typeof(PlayerHealthSimple))]
[DefaultExecutionOrder(10)]
public class PlayerFloatingBarsSimple : MonoBehaviour
{
    [Header("Layout")]
    public Vector3 offset = new Vector3(0f, 1.35f, 0f);
    public Vector3 barScale = new Vector3(0.02f, 0.02f, 0.02f);
    public Vector2 barSize = new Vector2(130f, 10f);
    [Tooltip("世界坐标间距（米），不是像素。")]
    public float hpMpSpacingWorld = 0.16f;

    [Header("Colors")]
    public Color hpBg = new Color(0.12f, 0.12f, 0.12f, 0.9f);
    public Color hpFill = new Color(0.25f, 0.85f, 0.28f, 1f);
    public Color mpBg = new Color(0.12f, 0.12f, 0.18f, 0.9f);
    public Color mpFill = new Color(0.25f, 0.55f, 1f, 1f);
    [Header("Low HP warning")]
    [Range(0.05f, 0.95f)] public float lowHpThreshold = 0.3f;
    public Color lowHpFlashColor = new Color(1f, 0.92f, 0.25f, 1f);
    [Min(0.1f)] public float lowHpFlashSpeed = 8f;
    [Header("Performance")]
    [Min(0.02f)] public float uiRefreshInterval = 0.08f;

    public int canvasSortingOrder = 120;

    Transform _hpRoot;
    Transform _mpRoot;
    Image _hpFill;
    Image _mpFill;
    PlayerHealthSimple _health;
    PlayerMpSimple _mp;
    float _nextUiRefreshAt;

    void Awake()
    {
        _health = GetComponent<PlayerHealthSimple>();
        _mp = GetComponent<PlayerMpSimple>();
    }

    IEnumerator Start()
    {
        yield return null;
        if (_health == null)
            _health = GetComponent<PlayerHealthSimple>();
        if (_mp == null)
            _mp = GetComponent<PlayerMpSimple>();

        float half = hpMpSpacingWorld * 0.5f;
        _hpFill = WorldSpaceUiBarUtil.CreateHorizontalBar(transform, "PlayerHpBar",
            offset + new Vector3(0f, half, 0f), barScale, barSize, hpBg, hpFill, canvasSortingOrder);
        _hpRoot = _hpFill.transform.parent;

        _mpFill = WorldSpaceUiBarUtil.CreateHorizontalBar(transform, "PlayerMpBar",
            offset + new Vector3(0f, -half, 0f), barScale, barSize, mpBg, mpFill, canvasSortingOrder + 1);
        _mpRoot = _mpFill.transform.parent;
    }

    void LateUpdate()
    {
        if (_health == null || _hpFill == null)
            return;

        Camera cam = Camera.main;
        if (_hpRoot != null)
        {
            Canvas c = _hpRoot.GetComponent<Canvas>();
            if (c != null)
                c.worldCamera = cam;
        }

        if (_mpRoot != null)
        {
            Canvas c = _mpRoot.GetComponent<Canvas>();
            if (c != null)
                c.worldCamera = cam;
        }

        if (cam != null)
        {
            if (_hpRoot != null)
                FaceCamera(_hpRoot, cam);
            if (_mpRoot != null)
                FaceCamera(_mpRoot, cam);
        }

        if (Time.unscaledTime < _nextUiRefreshAt)
            return;
        _nextUiRefreshAt = Time.unscaledTime + uiRefreshInterval;

        float hp01 = _health.HpFill01;
        _hpFill.fillAmount = hp01;
        _hpFill.color = EvaluateHpColor(hp01);
        if (_mp != null && _mpFill != null)
            _mpFill.fillAmount = _mp.Mp01;
        else if (_mpFill != null)
            _mpFill.fillAmount = 0f;
    }

    static void FaceCamera(Transform barRoot, Camera cam)
    {
        barRoot.LookAt(
            barRoot.position + cam.transform.rotation * Vector3.forward,
            cam.transform.rotation * Vector3.up
        );
    }

    Color EvaluateHpColor(float hp01)
    {
        if (hp01 > lowHpThreshold)
            return hpFill;
        float blink = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * lowHpFlashSpeed);
        return Color.Lerp(hpFill, lowHpFlashColor, blink);
    }
}
