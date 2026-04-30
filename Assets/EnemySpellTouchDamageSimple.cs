using System.Collections.Generic;
using UnityEngine;

/// <summary>敌人法术环伤：比近身物伤略大一圈，走 <see cref="PlayerHealthSimple.TakeSpellHit"/>；<c>enemySpellTouchDamage=0</c> 时自关。</summary>
public class EnemySpellTouchDamageSimple : MonoBehaviour
{
    public int spellDamage = 6;
    public float damageInterval = 1.35f;
    public float touchRange = 2.2f;

    /// <summary>表 <c>enemySpellTouchDamage≤0</c> 时关闭，供 <see cref="MultiplayerEnemyAuthoritySimple"/> 避免在 Server 上误开。</summary>
    public bool InactiveByBalance { get; private set; }

    float nextDamageTime;
    EnemyChaseSimple chase;

    void Awake()
    {
        D3GrowthBalanceData d = D3GrowthBalance.Load();
        spellDamage = Mathf.Max(0, d.enemySpellTouchDamage);
        if (spellDamage <= 0)
        {
            InactiveByBalance = true;
            enabled = false;
            return;
        }

        damageInterval = Mathf.Max(0.05f, d.enemySpellTouchIntervalSec);
        touchRange = Mathf.Max(0.1f, d.enemySpellTouchRange);
    }

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
        player.TakeSpellHit(spellDamage, name);
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
