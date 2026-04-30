using UnityEngine;
using System.Collections.Generic;

/// <summary>玩家生命占位：接收伤害，并应用强化减伤。</summary>
public class PlayerHealthSimple : MonoBehaviour
{
    public static PlayerHealthSimple Instance { get; private set; }
    static readonly List<PlayerHealthSimple> s_players = new List<PlayerHealthSimple>();
    public static IReadOnlyList<PlayerHealthSimple> Players => s_players;
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
    float nextHpRegenAt;
    float hpRegenCarry;

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
        ApplyD3DeathEconomyFromBalance();
    }

    void ApplyD3DeathEconomyFromBalance()
    {
        D3GrowthBalanceData d = D3GrowthBalance.Load();
        goldDropPercentOnDeath = Mathf.Clamp(d.playerDeathDropGoldPercent, 0, 100);
        respawnDelaySeconds = Mathf.Max(0.1f, d.playerRespawnDelaySec);
        respawnInvulnerableSeconds = Mathf.Max(0f, d.playerRespawnInvulnerableSec);
        respawnNoAggroSeconds = Mathf.Max(0f, d.playerRespawnNoAggroSec);
        respawnEnemyRetreatSeconds = Mathf.Max(0f, d.playerRespawnEnemyRetreatSec);
        respawnEnemyRetreatRadius = Mathf.Max(0.5f, d.playerRespawnEnemyRetreatRadius);
    }

    void Start()
    {
        CacheBodyParts();
        EnsureFloatingBars();
        wasNoAggroLastFrame = IsRespawnNoAggroActive;
    }

    void Update()
    {
        TryPassiveHpRegen();
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

    void TryPassiveHpRegen()
    {
        if (IsDead || currentHp >= maxHp)
            return;

        // 联机时仅服务器执行回血，客户端只吃同步镜像，避免双端各自回血。
        MultiplayerPlayerSimple net = GetComponent<MultiplayerPlayerSimple>();
        if (net != null && net.IsSpawned && !net.IsServer)
            return;

        if (Time.time < nextHpRegenAt)
            return;

        D3GrowthBalanceData d = D3GrowthBalance.Load();
        float tickSec = Mathf.Max(0.1f, d.hpRegenTickSec);
        nextHpRegenAt = Time.time + tickSec;

        float perVit = Mathf.Max(0f, d.hpRegenPercentPer10SecPerVit);
        if (perVit <= 0f)
            return;

        PlayerStatsSimple st = GetComponent<PlayerStatsSimple>();
        int vit = st != null ? st.vitality : d.startingVit;
        if (vit <= 0)
            return;

        float pctPer10Sec = vit * perVit;
        float healFloat = maxHp * (pctPer10Sec / 100f) * (tickSec / 10f);
        hpRegenCarry += healFloat;
        int heal = Mathf.FloorToInt(hpRegenCarry);
        if (heal <= 0)
            return;
        hpRegenCarry -= heal;
        currentHp = Mathf.Min(maxHp, currentHp + heal);
    }

    public void TakeHit(int rawDamage, string source = "enemy")
    {
        if (rawDamage <= 0)
            return;
        if (Time.time < invulnerableUntilTime)
            return;

        D3GrowthBalanceData d = D3GrowthBalance.Load();
        PlayerStatsSimple st = GetComponent<PlayerStatsSimple>();
        int agi = st != null ? st.agility : d.startingAgi;
        float dodgeP = D3GrowthBalance.ComputePlayerDodgeProbability(d, agi);
        if (dodgeP > 0f && UnityEngine.Random.value < dodgeP)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Dodge] agi={agi} p={dodgeP:P1} source={source} hp={currentHp}/{maxHp}");
#endif
            return;
        }

        if (enhance == null)
            enhance = GetComponent<PlayerEnhanceSimple>();

        PlayerStatsSimple stDef = st != null ? st : GetComponent<PlayerStatsSimple>();
        int strStat = stDef != null ? stDef.strength : d.startingStr;
        int vit = stDef != null ? stDef.vitality : d.startingVit;
        int pDef = D3GrowthBalance.ComputePlayerPhysicalDefense(d, strStat, vit);
        int reduced = D3GrowthBalance.ApplyFlatMitigationStep(rawDamage, pDef);
        float mul = enhance != null ? enhance.DamageTakenMultiplier : 1f;
        int finalDamage = Mathf.Max(1, Mathf.RoundToInt(reduced * mul));

        currentHp = Mathf.Max(0, currentHp - finalDamage);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[PlayerHit] src={source} raw={rawDamage} pDef={pDef} reduced={reduced} mul={mul:F2} final={finalDamage} hp={currentHp}/{maxHp}");
#endif

        if (currentHp <= 0)
            OnDeath();
    }

    /// <summary>法伤/元素即时段：先扣法术平面防（INT/VIT），再强化减伤倍率；闪避与近身伤共用一套骰点。</summary>
    public void TakeSpellHit(int rawDamage, string source = "enemy_spell")
    {
        if (rawDamage <= 0)
            return;
        if (Time.time < invulnerableUntilTime)
            return;

        D3GrowthBalanceData d = D3GrowthBalance.Load();
        PlayerStatsSimple st = GetComponent<PlayerStatsSimple>();
        int agi = st != null ? st.agility : d.startingAgi;
        float dodgeP = D3GrowthBalance.ComputePlayerDodgeProbability(d, agi);
        if (dodgeP > 0f && UnityEngine.Random.value < dodgeP)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Dodge][spell] agi={agi} p={dodgeP:P1} source={source} hp={currentHp}/{maxHp}");
#endif
            return;
        }

        if (enhance == null)
            enhance = GetComponent<PlayerEnhanceSimple>();

        PlayerStatsSimple stDef = st != null ? st : GetComponent<PlayerStatsSimple>();
        int intel = stDef != null ? stDef.intellect : d.startingInt;
        int vit = stDef != null ? stDef.vitality : d.startingVit;
        int sDef = D3GrowthBalance.ComputePlayerSpellDefense(d, intel, vit);
        int reduced = D3GrowthBalance.ApplySpellDefenseToDamage(rawDamage, sDef);
        float mul = enhance != null ? enhance.DamageTakenMultiplier : 1f;
        int finalDamage = Mathf.Max(1, Mathf.RoundToInt(reduced * mul));

        currentHp = Mathf.Max(0, currentHp - finalDamage);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log(
            $"[PlayerSpellHit] src={source} raw={rawDamage} sDef={sDef} reduced={reduced} mul={mul:F2} final={finalDamage} hp={currentHp}/{maxHp}");
#endif

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

    /// <summary>D3：由 <see cref="PlayerDerivedStatsSimple"/> 应用 README 公式 MaxHP；升级时默认保留血量比例。</summary>
    public void ApplyMaxHpFromDerived(int newMaxHp, bool preserveFillRatio)
    {
        newMaxHp = Mathf.Max(1, newMaxHp);
        if (newMaxHp == maxHp && preserveFillRatio)
            return;

        float r = maxHp <= 0 ? 1f : HpFill01;
        maxHp = newMaxHp;
        if (preserveFillRatio)
        {
            if (currentHp <= 0)
                currentHp = 0;
            else
                currentHp = Mathf.Clamp(Mathf.RoundToInt(r * maxHp), 1, maxHp);
        }
        else
            currentHp = Mathf.Clamp(currentHp, 0, maxHp);
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
        D3GrowthBalanceData d = D3GrowthBalance.Load();
        float baseR = Mathf.Max(0.5f, d.playerReengageRadiusBase);
        float mul = Mathf.Max(1f, d.playerReengageRadiusMultiplier);
        float lockSec = Mathf.Max(0.05f, d.playerReengageTargetLockSec);
        float r = Mathf.Max(respawnEnemyRetreatRadius, baseR) * mul;
        float sqR = r * r;
        Vector3 me = transform.position;
        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyChaseSimple e = enemies[i];
            if (e == null) continue;
            Vector3 dist = e.transform.position - me;
            dist.y = 0f;
            if (dist.sqrMagnitude > sqR) continue;
            e.ForceTarget(transform, lockSec);
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
