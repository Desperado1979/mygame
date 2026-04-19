using UnityEngine;

/// <summary>P1-A / README 命名：聚合敌人冰冻状态（底层为 <see cref="EnemyStatusEffectsSimple"/>）。</summary>
public class MonsterCombatHost : MonoBehaviour
{
    public bool IsFrozen
    {
        get
        {
            EnemyStatusEffectsSimple s = GetComponent<EnemyStatusEffectsSimple>();
            return s != null && s.IsFrozen;
        }
    }
}
