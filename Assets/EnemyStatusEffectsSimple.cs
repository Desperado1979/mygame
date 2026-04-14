using UnityEngine;

/// <summary>D2-2: Burn (DOT) + Freeze (root placeholder, no DOT) — aligned with README §11.1.</summary>
[RequireComponent(typeof(EnemyHealthSimple))]
public class EnemyStatusEffectsSimple : MonoBehaviour
{
    [Header("Burn (DOT, independent of res in full design)")]
    public int burnDamagePerTick = 1;
    public float burnTickInterval = 0.55f;

    [Header("Runtime")]
    float burnEndTime;
    float freezeEndTime;
    float nextBurnTickTime;

    MeshRenderer meshRenderer;
    Color originalColor;
    bool cachedColor;

    Rigidbody body;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        if (meshRenderer != null && meshRenderer.material != null)
        {
            originalColor = meshRenderer.material.color;
            cachedColor = true;
        }
    }

    void Update()
    {
        float t = Time.time;
        TickBurn(t);
        ClampVelocityIfFrozen(t);
        UpdateTint(t);
    }

    void TickBurn(float t)
    {
        if (t >= burnEndTime)
            return;

        if (t < nextBurnTickTime)
            return;

        EnemyHealthSimple health = GetComponent<EnemyHealthSimple>();
        if (health != null)
            health.TakeHit(burnDamagePerTick);

        nextBurnTickTime = t + burnTickInterval;
    }

    void ClampVelocityIfFrozen(float t)
    {
        if (body == null || body.isKinematic)
            return;
        if (t >= freezeEndTime)
            return;

        body.velocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
    }

    void UpdateTint(float t)
    {
        if (!cachedColor || meshRenderer == null)
            return;

        bool burning = t < burnEndTime;
        bool frozen = t < freezeEndTime;
        Color c = originalColor;

        if (burning && frozen)
            c = Color.Lerp(Color.red, Color.cyan, 0.45f);
        else if (frozen)
            c = Color.Lerp(originalColor, Color.cyan, 0.38f);
        else if (burning)
            c = Color.Lerp(originalColor, new Color(1f, 0.35f, 0.08f), 0.42f);

        meshRenderer.material.color = c;
    }

    /// <summary>Refresh or extend burn duration; DOT ticks use burnTickInterval.</summary>
    public void ApplyBurn(float durationSeconds)
    {
        float t = Time.time;
        burnEndTime = Mathf.Max(burnEndTime, t + Mathf.Max(0.01f, durationSeconds));
        nextBurnTickTime = Mathf.Min(nextBurnTickTime, t + 0.02f);
    }

    public void ApplyFreeze(float durationSeconds)
    {
        float t = Time.time;
        freezeEndTime = Mathf.Max(freezeEndTime, t + Mathf.Max(0.01f, durationSeconds));
    }

    public bool IsFrozen => Time.time < freezeEndTime;
    public bool IsBurning => Time.time < burnEndTime;

    /// <summary>For future movers: 0 while frozen.</summary>
    public float MoveSpeedMultiplier => IsFrozen ? 0f : 1f;

    public string GetHudSummary()
    {
        float t = Time.time;
        if (t >= burnEndTime && t >= freezeEndTime)
            return "";

        if (t < burnEndTime && t < freezeEndTime)
            return $"燃{burnEndTime - t:F1}s 冰{freezeEndTime - t:F1}s";
        if (t < burnEndTime)
            return $"燃{burnEndTime - t:F1}s";
        return $"冰{freezeEndTime - t:F1}s";
    }
}
