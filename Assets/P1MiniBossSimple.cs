using UnityEngine;

/// <summary>P1-A-3：小 Boss 占位 — 在 <see cref="EnemyHealthSimple"/> 初始化血量前放大 maxHp。</summary>
public class P1MiniBossSimple : MonoBehaviour
{
    [Min(1f)] public float hpMultiplier = 3f;

    [Tooltip("若赋值，则优先用配置里的倍率（数据驱动）。")]
    public P1AContentConfig contentConfig;

    public void ApplyToEnemyHealth()
    {
        if (contentConfig == null)
            contentConfig = P1AContentConfig.TryLoadDefault();
        EnemyHealthSimple h = GetComponent<EnemyHealthSimple>();
        if (h == null)
            return;
        float mul = contentConfig != null ? contentConfig.miniBossHpMultiplier : hpMultiplier;
        h.maxHp = Mathf.Max(1, Mathf.RoundToInt(h.maxHp * mul));
    }
}
