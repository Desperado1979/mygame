using UnityEngine;

/// <summary>Simple hit feedback: flash + tiny knockback.</summary>
public class EnemyHitFeedbackSimple : MonoBehaviour
{
    public float flashDuration = 0.08f;
    public float knockbackDistance = 0.35f;
    public Color flashColor = Color.white;

    MeshRenderer meshRenderer;
    Color baseColor = Color.white;
    float flashEndTime;
    bool hasBase;

    void Awake()
    {
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        if (meshRenderer != null && meshRenderer.material != null)
        {
            baseColor = meshRenderer.material.color;
            hasBase = true;
        }
    }

    void LateUpdate()
    {
        if (!hasBase || meshRenderer == null)
            return;
        if (Time.time >= flashEndTime)
            return;

        float t = Mathf.Clamp01((flashEndTime - Time.time) / Mathf.Max(0.01f, flashDuration));
        meshRenderer.material.color = Color.Lerp(baseColor, flashColor, t);
    }

    public void Play(Vector3 hitFromWorldPos)
    {
        flashEndTime = Time.time + Mathf.Max(0.01f, flashDuration);

        Vector3 dir = transform.position - hitFromWorldPos;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
        {
            dir.Normalize();
            transform.position += dir * knockbackDistance;
        }
    }
}
