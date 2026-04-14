using UnityEngine;

/// <summary>Simple floating HP text above enemy.</summary>
public class EnemyFloatingHpSimple : MonoBehaviour
{
    public Vector3 offset = new Vector3(0f, 1.35f, 0f);
    [Header("Readability")]
    public int fontSize = 48;
    public float baseCharacterSize = 0.08f;
    public float distanceScaleFactor = 0.018f;
    public float minCharacterSize = 0.06f;
    public float maxCharacterSize = 0.18f;
    TextMesh label;
    EnemyHealthSimple hp;

    void Start()
    {
        hp = GetComponent<EnemyHealthSimple>();
        EnsureLabel();
        Refresh();
    }

    void LateUpdate()
    {
        if (label == null || hp == null)
            return;
        Camera cam = Camera.main;
        if (cam != null)
        {
            label.transform.forward = cam.transform.forward;
            float dist = Vector3.Distance(cam.transform.position, label.transform.position);
            float scaled = baseCharacterSize + dist * distanceScaleFactor;
            label.characterSize = Mathf.Clamp(scaled, minCharacterSize, maxCharacterSize);
        }
        label.text = $"HP {Mathf.Max(0, hp.CurrentHp)}/{hp.MaxHp}";
    }

    void EnsureLabel()
    {
        Transform t = transform.Find("EnemyHpLabel");
        if (t == null)
        {
            GameObject go = new GameObject("EnemyHpLabel");
            go.transform.SetParent(transform);
            go.transform.localPosition = offset;
            label = go.AddComponent<TextMesh>();
            label.fontSize = fontSize;
            label.characterSize = baseCharacterSize;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = new Color(1f, 0.95f, 0.95f);
        }
        else
        {
            label = t.GetComponent<TextMesh>();
            if (label == null)
                label = t.gameObject.AddComponent<TextMesh>();
        }

        label.fontSize = fontSize;
        label.characterSize = baseCharacterSize;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
    }

    void Refresh()
    {
        if (label != null && hp != null)
            label.text = $"HP {Mathf.Max(0, hp.CurrentHp)}/{hp.MaxHp}";
    }
}
