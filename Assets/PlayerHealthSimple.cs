using UnityEngine;

/// <summary>玩家生命占位：接收伤害，并应用强化减伤。</summary>
public class PlayerHealthSimple : MonoBehaviour
{
    public static PlayerHealthSimple Instance { get; private set; }

    public int maxHp = 260;
    [SerializeField] int currentHp;
    [Header("Death & respawn placeholder")]
    public Transform respawnPoint;
    public float respawnDelaySeconds = 1.5f;
    [Range(0, 100)] public int goldDropPercentOnDeath = 20;

    public int CurrentHp => currentHp;
    public bool IsDead => currentHp <= 0;
    /// <summary>头顶血条填充 0~1。</summary>
    public float HpFill01 => maxHp <= 0 ? 0f : Mathf.Clamp01((float)currentHp / maxHp);

    PlayerEnhanceSimple enhance;
    bool respawning;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple PlayerHealthSimple — keeping first.");
            return;
        }

        Instance = this;
        currentHp = maxHp;
        enhance = GetComponent<PlayerEnhanceSimple>();
        if (GetComponent<P1A1QuestState>() == null)
            gameObject.AddComponent<P1A1QuestState>();
    }

    void Start()
    {
        EnsureFloatingBars();
    }

    void OnEnable()
    {
        EnsureFloatingBars();
    }

    void EnsureFloatingBars()
    {
        // 无论 Instance 如何，当前玩家对象都兜底确保头顶条存在。
        if (GetComponent<PlayerFloatingBarsSimple>() == null)
        {
            gameObject.AddComponent<PlayerFloatingBarsSimple>();
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void TakeHit(int rawDamage, string source = "enemy")
    {
        if (rawDamage <= 0)
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
        Invoke(nameof(RespawnNow), Mathf.Max(0f, respawnDelaySeconds));
    }

    void RespawnNow()
    {
        currentHp = maxHp;
        if (respawnPoint != null)
            transform.position = respawnPoint.position;
        respawning = false;
        Debug.Log("Player Respawned");
    }
}
