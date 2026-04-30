using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>Simple enemy chase: detect player, chase, then leash back to spawn.</summary>
public class EnemyChaseSimple : MonoBehaviour
{
    static readonly List<EnemyChaseSimple> s_instances = new List<EnemyChaseSimple>();
    public static IReadOnlyList<EnemyChaseSimple> Instances => s_instances;

    [Header("Chase tuning（DefaultD3Growth 在 Awake 写默认）")]
    public float moveSpeed = 3.4f;
    public float detectRange = 16f;
    public float stopRange = 1.05f;
    public float leashRangeFromSpawn = 26f;
    [Tooltip("索敌刷新间隔，由 D3 初始化")]
    float retargetInterval = 0.35f;
    float targetLockSeconds = 0.8f;
    float detectExitPadding = 0.75f;
    float leashExitPadding = 1f;
    float noPushBuffer = 0.08f;
    float keepDistanceDeadband = 0.02f;

    Vector3 spawnPos;
    Transform player;
    EnemyStatusEffectsSimple _fx;
    float _retargetAt;
    float _forcedRetreatUntil;
    float _targetLockUntil;
    float _lastKeepDistance;
    bool _isChasingCurrentTarget;
    public Transform CurrentTarget => player;
    public bool IsForcedRetreating => Time.time < _forcedRetreatUntil;
    public float CurrentEngageKeepDistance => Mathf.Max(stopRange, _lastKeepDistance);
    public bool IsChasingCurrentTarget => _isChasingCurrentTarget;

    void Awake()
    {
        D3GrowthBalanceData d = D3GrowthBalance.Load();
        moveSpeed = Mathf.Max(0.1f, d.enemyChaseMoveSpeed);
        detectRange = Mathf.Max(0.5f, d.enemyChaseDetectRange);
        stopRange = Mathf.Max(0.1f, d.enemyChaseStopRange);
        leashRangeFromSpawn = Mathf.Max(0.5f, d.enemyChaseLeashRange);
        retargetInterval = Mathf.Max(0.05f, d.enemyChaseRetargetIntervalSec);
        targetLockSeconds = Mathf.Max(0f, d.enemyChaseTargetLockSec);
        detectExitPadding = Mathf.Max(0f, d.enemyChaseDetectExitPadding);
        leashExitPadding = Mathf.Max(0f, d.enemyChaseLeashExitPadding);
        noPushBuffer = Mathf.Max(0f, d.enemyChaseNoPushBuffer);
        keepDistanceDeadband = Mathf.Max(0f, d.enemyChaseKeepDistanceDeadband);
    }

    void Start()
    {
        spawnPos = transform.position;
        _fx = GetComponent<EnemyStatusEffectsSimple>();
        _lastKeepDistance = Mathf.Max(0.1f, stopRange);
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
            MoveToward(spawnPos, 0f);
            return;
        }

        if (player != null)
        {
            PlayerHealthSimple pNow = player.GetComponent<PlayerHealthSimple>();
            if (pNow == null || pNow.IsDead || pNow.IsRespawnNoAggroActive)
            {
                player = null;
                _isChasingCurrentTarget = false;
                _targetLockUntil = Mathf.Min(_targetLockUntil, Time.time);
            }
        }

        bool lockActive = player != null && Time.time < _targetLockUntil;
        if (!lockActive && (player == null || Time.time >= _retargetAt))
        {
            Transform newTarget = FindNearestAlivePlayer();
            bool changedTarget = newTarget != player;
            player = newTarget;
            _retargetAt = Time.time + retargetInterval;
            if (changedTarget && player != null)
                _targetLockUntil = Time.time + targetLockSeconds;
            if (player == null)
            {
                _isChasingCurrentTarget = false;
                _targetLockUntil = Mathf.Min(_targetLockUntil, Time.time);
            }
        }
        if (player == null)
            return;

        if (_fx == null)
            _fx = GetComponent<EnemyStatusEffectsSimple>();
        if (_fx != null && _fx.IsFrozen)
            return;

        float distToPlayer = Vector3.Distance(transform.position, player.position);
        float distSpawnToPlayer = Vector3.Distance(spawnPos, player.position);

        bool canChase = CanChaseByHysteresis(distToPlayer, distSpawnToPlayer);
        if (canChase)
        {
            float keepDistance = Mathf.Max(stopRange, ComputeNoPushDistance(player));
            _lastKeepDistance = keepDistance;
            if (distToPlayer < keepDistance - keepDistanceDeadband)
                MoveAwayFrom(player.position, keepDistance);
            else if (distToPlayer > keepDistance)
                MoveToward(player.position, keepDistance);
            return;
        }

        MoveToward(spawnPos, 0f);
    }

    bool CanChaseByHysteresis(float distToPlayer, float distSpawnToPlayer)
    {
        if (_isChasingCurrentTarget)
        {
            bool stillInDetect = distToPlayer <= detectRange + detectExitPadding;
            bool stillInLeash = distSpawnToPlayer <= leashRangeFromSpawn + leashExitPadding;
            _isChasingCurrentTarget = stillInDetect && stillInLeash;
            return _isChasingCurrentTarget;
        }

        bool canEnter = distToPlayer <= detectRange && distSpawnToPlayer <= leashRangeFromSpawn;
        _isChasingCurrentTarget = canEnter;
        return canEnter;
    }

    /// <summary>HUD 用：返回当前追击状态简码（C=追击，R=回撤，F=强制回撤）。</summary>
    public string BuildHudChaseStateTag()
    {
        if (IsForcedRetreating)
            return "F";
        return _isChasingCurrentTarget ? "C" : "R";
    }

    /// <summary>HUD 用：追击状态 + 目标平面距离/边界（例：C d1.8/k1.6）。</summary>
    public string BuildHudChaseDebugSummary()
    {
        string state = BuildHudChaseStateTag();
        if (player == null)
            return state;

        Vector3 flat = player.position - transform.position;
        flat.y = 0f;
        float dist = flat.magnitude;
        float keep = CurrentEngageKeepDistance;
        return state + " d" + dist.ToString("F1") + "/k" + keep.ToString("F1");
    }

    /// <summary>短时强制回撤到出生点（用于玩家复活防连死）。</summary>
    public void ForceRetreat(float seconds)
    {
        _forcedRetreatUntil = Mathf.Max(_forcedRetreatUntil, Time.time + Mathf.Max(0f, seconds));
        player = null;
        _retargetAt = Time.time + retargetInterval;
        _targetLockUntil = Mathf.Min(_targetLockUntil, Time.time);
    }

    /// <summary>强制将目标设为指定玩家，用于复活免仇恨结束后的重拉仇恨。</summary>
    public void ForceTarget(Transform target, float lockSeconds = 0.6f)
    {
        if (target == null)
            return;
        player = target;
        _forcedRetreatUntil = Mathf.Min(_forcedRetreatUntil, Time.time);
        _retargetAt = Time.time + Mathf.Max(0f, lockSeconds);
        _targetLockUntil = Time.time + Mathf.Max(0f, lockSeconds);
    }

    void MoveToward(Vector3 target, float keepDistance)
    {
        Vector3 flat = target - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.001f)
            return;

        float dist = flat.magnitude;
        float allowed = Mathf.Max(0f, dist - Mathf.Max(0f, keepDistance));
        if (allowed <= 0.0001f)
            return;

        Vector3 step = flat.normalized * moveSpeed * Time.deltaTime;
        float stepLen = step.magnitude;
        if (stepLen > allowed)
            step = flat.normalized * allowed;

        transform.position += step;
        transform.forward = flat.normalized;
    }

    void MoveAwayFrom(Vector3 danger, float keepDistance)
    {
        Vector3 flat = transform.position - danger;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.0001f)
            return;

        float dist = flat.magnitude;
        float need = Mathf.Max(0f, keepDistance - dist);
        if (need <= 0.0001f)
            return;

        float stepLen = Mathf.Min(need, moveSpeed * Time.deltaTime);
        Vector3 step = flat.normalized * stepLen;
        transform.position += step;
        transform.forward = -flat.normalized;
    }

    float ComputeNoPushDistance(Transform target)
    {
        float enemyR = ApproxRadiusXZ(transform);
        float targetR = ApproxRadiusXZ(target);
        // Slight buffer to avoid penetration-resolve pushing.
        return enemyR + targetR + noPushBuffer;
    }

    static float ApproxRadiusXZ(Transform t)
    {
        if (t == null)
            return 0.5f;
        Collider c = t.GetComponent<Collider>();
        if (c == null)
            return 0.5f;
        Bounds b = c.bounds;
        return Mathf.Max(0.1f, Mathf.Max(b.extents.x, b.extents.z));
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
