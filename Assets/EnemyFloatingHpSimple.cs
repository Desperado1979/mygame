using UnityEngine;
using UnityEngine.UI;

/// <summary>敌人头顶血条（世界空间 UGUI + 内置白贴图）。</summary>
public class EnemyFloatingHpSimple : MonoBehaviour
{
    public Vector3 offset = new Vector3(0f, 1.35f, 0f);
    [Header("Bar layout")]
    public Vector3 barScale = new Vector3(0.01f, 0.01f, 0.01f);
    public Vector2 barSize = new Vector2(120f, 10f);
    public Color bgColor = new Color(0.12f, 0.12f, 0.12f, 0.9f);
    public Color fillColor = new Color(0.9f, 0.22f, 0.22f, 1f);
    [Header("Low HP warning")]
    [Range(0.05f, 0.95f)] public float lowHpThreshold = 0.3f;
    public Color lowHpFlashColor = new Color(1f, 0.92f, 0.25f, 1f);
    [Min(0.1f)] public float lowHpFlashSpeed = 8f;
    [Header("Performance")]
    [Min(0.02f)] public float uiRefreshInterval = 0.08f;
    public int canvasSortingOrder = 80;

    Transform _barRoot;
    Image _fill;
    EnemyHealthSimple _hp;
    float _nextUiRefreshAt;

    void Start()
    {
        _hp = GetComponent<EnemyHealthSimple>();
        CleanupLegacyLabel();
        EnsureBar();
    }

    void CleanupLegacyLabel()
    {
        Transform legacy = transform.Find("EnemyHpLabel");
        if (legacy != null)
            Destroy(legacy.gameObject);
    }

    void EnsureBar()
    {
        if (_hp == null)
            return;

        _fill = WorldSpaceUiBarUtil.CreateHorizontalBar(transform, "EnemyHpBar", offset, barScale, barSize, bgColor,
            fillColor, canvasSortingOrder);
        _barRoot = _fill.transform.parent;
    }

    void LateUpdate()
    {
        if (_fill == null || _hp == null)
            return;

        Camera cam = Camera.main;
        if (cam != null && _barRoot != null)
            _barRoot.forward = cam.transform.forward;

        if (Time.unscaledTime < _nextUiRefreshAt)
            return;
        _nextUiRefreshAt = Time.unscaledTime + uiRefreshInterval;

        float t = _hp.MaxHp <= 0 ? 0f : Mathf.Clamp01((float)_hp.CurrentHp / _hp.MaxHp);
        _fill.fillAmount = t;
        _fill.color = EvaluateHpColor(t);
    }

    Color EvaluateHpColor(float hp01)
    {
        if (hp01 > lowHpThreshold)
            return fillColor;
        float blink = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * lowHpFlashSpeed);
        return Color.Lerp(fillColor, lowHpFlashColor, blink);
    }
}
