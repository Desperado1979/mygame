using UnityEngine;

/// <summary>P1-A-3：小 Boss 占位「阶段」— 血量低于阈值时强化追击（不改动画）。</summary>
public class P1BossPhaseSimple : MonoBehaviour
{
    [Tooltip("若赋值，则 Awake 时从配置覆盖下列字段（数据驱动）。")]
    public P1AContentConfig contentConfig;

    [Range(0.05f, 0.95f)] public float hpFractionForPhase2 = 0.5f;
    [Min(1f)] public float chaseSpeedMultiplierPhase2 = 1.25f;

    bool _enteredPhase2;

    void Awake()
    {
        if (contentConfig == null)
            contentConfig = P1AContentConfig.TryLoadDefault();
        if (contentConfig == null)
            return;
        hpFractionForPhase2 = contentConfig.bossPhaseHpFraction;
        chaseSpeedMultiplierPhase2 = contentConfig.bossPhaseChaseSpeedMul;
    }

    void Update()
    {
        if (_enteredPhase2)
            return;

        EnemyHealthSimple h = GetComponent<EnemyHealthSimple>();
        if (h == null || h.CurrentHp <= 0)
            return;

        if (h.CurrentHp > h.MaxHp * hpFractionForPhase2)
            return;

        _enteredPhase2 = true;
        EnemyChaseSimple chase = GetComponent<EnemyChaseSimple>();
        if (chase != null)
            chase.moveSpeed *= chaseSpeedMultiplierPhase2;
    }
}
