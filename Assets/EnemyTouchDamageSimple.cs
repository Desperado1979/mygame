using UnityEngine;
using System.Collections.Generic;

/// <summary>敌人近身接触伤害占位：离玩家足够近时按间隔扣血。</summary>
public class EnemyTouchDamageSimple : MonoBehaviour
{
    public int touchDamage = 8;
    public float damageInterval = 1.1f;
    public float touchRange = 1.25f;

    float nextDamageTime;
    EnemyChaseSimple chase;

    float GetEffectiveTouchRange()
    {
        float r = touchRange;
        if (chase != null)
            r = Mathf.Max(r, chase.stopRange + 0.75f);
        return r;
    }

    void Update()
    {
        if (chase == null)
            chase = GetComponent<EnemyChaseSimple>();
        if (chase != null && chase.IsForcedRetreating)
            return;

        PlayerHealthSimple player = ResolveDamageTargetInRange();
        if (player == null)
            return;

        float effectiveTouchRange = GetEffectiveTouchRange();

        Vector3 d = player.transform.position - transform.position;
        d.y = 0f;
        float sq = d.sqrMagnitude;
        if (sq > effectiveTouchRange * effectiveTouchRange)
            return;

        if (Time.time < nextDamageTime)
            return;

        nextDamageTime = Time.time + damageInterval;
        player.TakeHit(touchDamage, name);
    }

    bool CanDamage(PlayerHealthSimple p)
    {
        if (p == null || p.IsDead || p.IsRespawnNoAggroActive)
            return false;
        return true;
    }

    PlayerHealthSimple ResolveDamageTargetInRange()
    {
        float effectiveTouchRange = GetEffectiveTouchRange();
        float maxSq = effectiveTouchRange * effectiveTouchRange;

        // 单机/本机优先：避免多人残留对象导致“贴脸但不打本机”。
        PlayerHealthSimple local = PlayerHealthSimple.Instance;
        if (local != null && CanDamage(local))
        {
            Vector3 dl = local.transform.position - transform.position;
            dl.y = 0f;
            if (dl.sqrMagnitude <= maxSq)
                return local;
        }

        IReadOnlyList<PlayerHealthSimple> players = PlayerHealthSimple.Players;
        if (players == null || players.Count == 0)
            return null;

        PlayerHealthSimple best = null;
        float bestSq = float.MaxValue;
        for (int i = 0; i < players.Count; i++)
        {
            PlayerHealthSimple p = players[i];
            if (!CanDamage(p))
                continue;
            Vector3 d = p.transform.position - transform.position;
            d.y = 0f;
            float sq = d.sqrMagnitude;
            if (sq > maxSq)
                continue;
            if (sq < bestSq)
            {
                bestSq = sq;
                best = p;
            }
        }
        return best;
    }
}
