using UnityEngine;

/// <summary>D2-1: MP pool with optional regen (placeholder for INT-driven MP later).</summary>
public class PlayerMpSimple : MonoBehaviour
{
    public int maxMp = 100;
    [Tooltip("MP per second while below max.")]
    public float mpRegenPerSecond = 8f;

    float mp;

    void Start()
    {
        mp = maxMp;
    }

    void Update()
    {
        if (mpRegenPerSecond > 0f && mp < maxMp)
            mp = Mathf.Min(maxMp, mp + mpRegenPerSecond * Time.deltaTime);
    }

    public bool TrySpend(int amount)
    {
        if (amount <= 0) return true;
        if (mp < amount) return false;
        mp -= amount;
        return true;
    }

    public int CurrentMpRounded => Mathf.FloorToInt(mp);
    public int MaxMp => maxMp;
    public float Mp01 => maxMp <= 0 ? 0f : Mathf.Clamp01(mp / maxMp);
}
