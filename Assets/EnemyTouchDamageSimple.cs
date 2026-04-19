using UnityEngine;

/// <summary>敌人近身接触伤害占位：离玩家足够近时按间隔扣血。</summary>
public class EnemyTouchDamageSimple : MonoBehaviour
{
    public int touchDamage = 8;
    public float damageInterval = 1.1f;
    public float touchRange = 1.25f;

    float nextDamageTime;

    void Update()
    {
        PlayerHealthSimple player = PlayerHealthSimple.Instance;
        if (player == null || player.IsDead)
            return;
        if (SafeZoneSimple.IsInside(player.transform.position))
            return;

        float sq = (player.transform.position - transform.position).sqrMagnitude;
        if (sq > touchRange * touchRange)
            return;

        if (Time.time < nextDamageTime)
            return;

        nextDamageTime = Time.time + damageInterval;
        player.TakeHit(touchDamage, name);
    }
}
