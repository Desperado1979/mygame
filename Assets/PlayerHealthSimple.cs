using UnityEngine;
using System.Collections.Generic;

/// <summary>玩家生命占位：接收伤害，并应用强化减伤。</summary>
public class PlayerHealthSimple : MonoBehaviour
{
    public static PlayerHealthSimple Instance { get; private set; }
    static readonly List<PlayerHealthSimple> s_players = new List<PlayerHealthSimple>();
    public static IReadOnlyList<PlayerHealthSimple> Players => s_players;
    const float ReengageRadiusBase = 7f;
    const float ReengageRadiusMultiplier = 2f;
    const float ReengageTargetLockSeconds = 0.8f;
    bool isNetPlayer;

    public int maxHp = 260;
    [SerializeField] int currentHp;
    [Header("Death & respawn placeholder")]
    public Transform respawnPoint;
    public float respawnDelaySeconds = 1.5f;
    [Min(0f)] public float respawnInvulnerableSeconds = 3f;
    [Min(0f)] public float respawnNoAggroSeconds = 3f;
    [Min(0f)] public float respawnEnemyRetreatSeconds = 3f;
    [Min(0.5f)] public float respawnEnemyRetreatRadius = 7f;
    [Range(0, 100)] public int goldDropPercentOnDeath = 20;

    public int CurrentHp => currentHp;
    public bool IsDead => currentHp <= 0;
    public bool IsRespawnNoAggroActive => Time.time < noAggroUntilTime;
    /// <summary>头顶血条填充 0~1。</summary>
    public float HpFill01 => maxHp <= 0 ? 0f : Mathf.Clamp01((float)currentHp / maxHp);

    PlayerEnhanceSimple enhance;
    bool respawning;
    float invulnerableUntilTime;
    float noAggroUntilTime;
    bool wasNoAggroLastFrame;
    Renderer[] cachedRenderers;
    Rigidbody cachedRigidbody;

    void Awake()
    {
        isNetPlayer = GetComponent<MultiplayerPlayerSimple>() != null;
        if (!isNetPlayer)
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Multiple PlayerHealthSimple — keeping first.");
                enabled = false;
                return;
            }
            Instance = this;
        }
        currentHp = maxHp;
        enhance = GetComponent<PlayerEnhanceSimple>();
        if (!isNetPlayer && GetComponent<P1A1QuestState>() == null)
            gameObject.AddComponent<P1A1QuestState>();
    }

    void Start()
    {
        CacheBodyParts();
        EnsureFloatingBars();
        wasNoAggroLastFrame = IsRespawnNoAggroActive;
    }

    void Update()
    {
        // no-aggro 窗口结束那一帧主动重拉附近敌人，避免需要玩家先移动/攻击才触发。
        bool nowNoAggro = IsRespawnNoAggroActive;
        if (wasNoAggroLastFrame && !nowNoAggro && !IsDead)
            ReengageNearbyEnemiesAfterNoAggro();
        wasNoAggroLastFrame = nowNoAggro;
    }

    void OnEnable()
    {
        if (!s_players.Contains(this))
            s_players.Add(this);
        EnsureFloatingBars();
    }

    void OnDisable()
    {
        s_players.Remove(this);
    }

    void EnsureFloatingBars()
    {
        // 无论 Instance 如何，当前玩家对象都兜底确保头顶条存在。
        if (GetComponent<PlayerFloatingBarsSimple>() == null)
        {
            gameObject.AddComponent<PlayerFloatingBarsSimple>();
        }
    }

    void CacheBodyParts()
    {
        if (cachedRenderers == null || cachedRenderers.Length == 0)
            cachedRenderers = GetComponentsInChildren<Renderer>(true);
        if (cachedRigidbody == null)
            cachedRigidbody = GetComponent<Rigidbody>();
    }

    void OnDestroy()
    {
        s_players.Remove(this);
        if (!isNetPlayer && Instance == this)
            Instance = null;
    }

    public void TakeHit(int rawDamage, string source = "enemy")
    {
        if (rawDamage <= 0)
            return;
        if (Time.time < invulnerableUntilTime)
            return;

        if (enhance == null)
            enhance = GetComponent<PlayerEnhanceSimple>();

        float mul = enhance != null ? enhance.DamageTakenMultiplier : 1f;
        int finalDamage = Mathf.Max(1, Mathf.RoundToInt(rawDamage * mul));

        currentHp = Mathf.Max(0, currentHp - finalDamage);
        Debug.Log($"Player hit by {source}: -{finalDamage} HP ({currentHp}/{maxHp})");

        if (currentHp <= 0)
            OnDeath();
    }

    public bool Heal(int amount, string source = "item")
    {
        if (amount <= 0 || currentHp <= 0 || currentHp >= maxHp)
            return false;

        int before = currentHp;
        currentHp = Mathf.Min(maxHp, currentHp + amount);
        Debug.Log($"Player healed by {source}: +{currentHp - before} HP ({currentHp}/{maxHp})");
        return true;
    }

    public void SetCurrentHp(int value)
    {
        currentHp = Mathf.Clamp(value, 0, maxHp);
    }

    public static void SetPreferredInstance(PlayerHealthSimple value)
    {
        if (value != null)
            Instance = value;
    }

    void OnDeath()
    {
        if (respawning)
            return;

        int lostGold = 0;
        PlayerWalletSimple wallet = GetComponent<PlayerWalletSimple>();
        if (wallet != null)
            lostGold = wallet.LosePercent(goldDropPercentOnDeath);

        Debug.Log($"Player Down: lost {lostGold} gold, respawn in {respawnDelaySeconds:F1}s");
        respawning = true;
        SetBodyVisible(false);
        Invoke(nameof(RespawnNow), Mathf.Max(0f, respawnDelaySeconds));
    }

    void RespawnNow()
    {
        currentHp = maxHp;
        if (respawnPoint != null)
            transform.position = respawnPoint.position;
        invulnerableUntilTime = Time.time + Mathf.Max(0f, respawnInvulnerableSeconds);
        noAggroUntilTime = Time.time + Mathf.Max(0f, respawnNoAggroSeconds);
        ForceNearbyEnemiesRetreat();
        SetBodyVisible(true);
        respawning = false;
        wasNoAggroLastFrame = IsRespawnNoAggroActive;
        Debug.Log($"Player Respawned (invul {Mathf.Max(0f, respawnInvulnerableSeconds):F1}s, no-aggro {Mathf.Max(0f, respawnNoAggroSeconds):F1}s)");
    }

    void ForceNearbyEnemiesRetreat()
    {
        if (respawnEnemyRetreatSeconds <= 0f || respawnEnemyRetreatRadius <= 0f)
            return;

        IReadOnlyList<EnemyChaseSimple> enemies = EnemyChaseSimple.Instances;
        if (enemies == null || enemies.Count == 0)
            return;
        float sqR = respawnEnemyRetreatRadius * respawnEnemyRetreatRadius;
        Vector3 me = transform.position;
        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyChaseSimple e = enemies[i];
            if (e == null) continue;
            Vector3 d = e.transform.position - me;
            d.y = 0f;
            if (d.sqrMagnitude > sqR) continue;
            e.ForceRetreat(respawnEnemyRetreatSeconds);
        }
    }

    void ReengageNearbyEnemiesAfterNoAggro()
    {
        IReadOnlyList<EnemyChaseSimple> enemies = EnemyChaseSimple.Instances;
        if (enemies == null || enemies.Count == 0)
            return;
        float r = Mathf.Max(respawnEnemyRetreatRadius, ReengageRadiusBase) * ReengageRadiusMultiplier;
        float sqR = r * r;
        Vector3 me = transform.position;
        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyChaseSimple e = enemies[i];
            if (e == null) continue;
            Vector3 d = e.transform.position - me;
            d.y = 0f;
            if (d.sqrMagnitude > sqR) continue;
            e.ForceTarget(transform, ReengageTargetLockSeconds);
        }
    }

    void SetBodyVisible(bool visible)
    {
        CacheBodyParts();
        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            Renderer r = cachedRenderers[i];
            if (r == null) continue;
            if (r.GetComponent<PlayerFloatingBarsSimple>() != null) continue;
            r.enabled = visible;
        }
        if (cachedRigidbody != null)
            cachedRigidbody.isKinematic = !visible;
    }
}
