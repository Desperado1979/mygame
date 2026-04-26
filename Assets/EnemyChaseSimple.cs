using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>Simple enemy chase: detect player, chase, then leash back to spawn.</summary>
public class EnemyChaseSimple : MonoBehaviour
{
    static readonly List<EnemyChaseSimple> s_instances = new List<EnemyChaseSimple>();
    public static IReadOnlyList<EnemyChaseSimple> Instances => s_instances;
    const float RetargetInterval = 0.35f;

    [Header("Chase tuning")]
    public float moveSpeed = 3.4f;
    public float detectRange = 16f;
    public float stopRange = 1.05f;
    public float leashRangeFromSpawn = 26f;

    Vector3 spawnPos;
    Transform player;
    EnemyStatusEffectsSimple _fx;
    float _retargetAt;
    float _forcedRetreatUntil;
    public Transform CurrentTarget => player;
    public bool IsForcedRetreating => Time.time < _forcedRetreatUntil;

    void Start()
    {
        spawnPos = transform.position;
        _fx = GetComponent<EnemyStatusEffectsSimple>();
    }

    void OnEnable()
    {
        if (!s_instances.Contains(this))
            s_instances.Add(this);
    }

    void OnDisable()
    {
        s_instances.Remove(this);
    }

    void Update()
    {
        if (IsForcedRetreating)
        {
            MoveToward(spawnPos);
            return;
        }

        if (player != null)
        {
            PlayerHealthSimple pNow = player.GetComponent<PlayerHealthSimple>();
            if (pNow == null || pNow.IsDead || pNow.IsRespawnNoAggroActive)
                player = null;
        }

        if (player == null || Time.time >= _retargetAt)
        {
            player = FindNearestAlivePlayer();
            _retargetAt = Time.time + RetargetInterval;
        }
        if (player == null)
            return;

        if (_fx == null)
            _fx = GetComponent<EnemyStatusEffectsSimple>();
        if (_fx != null && _fx.IsFrozen)
            return;

        float distToPlayer = Vector3.Distance(transform.position, player.position);
        float distSpawnToPlayer = Vector3.Distance(spawnPos, player.position);

        bool canChase = distToPlayer <= detectRange && distSpawnToPlayer <= leashRangeFromSpawn;
        if (canChase)
        {
            if (distToPlayer > stopRange)
                MoveToward(player.position);
            return;
        }

        MoveToward(spawnPos);
    }

    /// <summary>短时强制回撤到出生点（用于玩家复活防连死）。</summary>
    public void ForceRetreat(float seconds)
    {
        _forcedRetreatUntil = Mathf.Max(_forcedRetreatUntil, Time.time + Mathf.Max(0f, seconds));
        player = null;
        _retargetAt = Time.time + RetargetInterval;
    }

    /// <summary>强制将目标设为指定玩家，用于复活免仇恨结束后的重拉仇恨。</summary>
    public void ForceTarget(Transform target, float lockSeconds = 0.6f)
    {
        if (target == null)
            return;
        player = target;
        _forcedRetreatUntil = Mathf.Min(_forcedRetreatUntil, Time.time);
        _retargetAt = Time.time + Mathf.Max(0f, lockSeconds);
    }

    void MoveToward(Vector3 target)
    {
        Vector3 flat = target - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.001f)
            return;

        Vector3 step = flat.normalized * moveSpeed * Time.deltaTime;
        if (step.sqrMagnitude > flat.sqrMagnitude)
            step = flat;

        transform.position += step;
        transform.forward = flat.normalized;
    }

    Transform FindNearestAlivePlayer()
    {
        IReadOnlyList<PlayerHealthSimple> players = PlayerHealthSimple.Players;
        // 联机时服务器上有多名玩家；Instance 往往指向 Host，会锁死只追 Host。单机仍优先本机 Instance。
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            PlayerHealthSimple local = PlayerHealthSimple.Instance;
            if (local != null && !local.IsDead && !local.IsRespawnNoAggroActive)
                return local.transform;
        }
        if (players == null || players.Count == 0)
            return null;
        Transform best = null;
        float bestSq = float.MaxValue;
        for (int i = 0; i < players.Count; i++)
        {
            PlayerHealthSimple p = players[i];
            if (p == null || p.IsDead || p.IsRespawnNoAggroActive)
                continue;
            float sq = (p.transform.position - transform.position).sqrMagnitude;
            if (sq < bestSq)
            {
                bestSq = sq;
                best = p.transform;
            }
        }
        return best;
    }
}
