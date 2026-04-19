using UnityEngine;

/// <summary>P1-A-1：统计「死前已冰+已 Q」的合规击杀数（每只怪至多计一次）。</summary>
public class P1A1QuestState : MonoBehaviour
{
    public static P1A1QuestState Instance { get; private set; }

    public int targetKills = 5;
    public int compliantKills;

    [Tooltip("若赋值，则 Start 时覆盖 targetKills（数据驱动）。")]
    public P1AContentConfig contentConfig;

    [Tooltip("HUD 一行提示：推荐先 R 冰冻再 Q 火球（与 README P1-A-1 一致）。")]
    public bool showOrderHintInHud = true;

    public bool IsComplete => compliantKills >= targetKills;

    /// <summary>与验收说明一致的简短操作提示。</summary>
    public string OrderHintShort => "R冰→Q火";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (contentConfig == null)
            contentConfig = P1AContentConfig.TryLoadDefault();
        if (contentConfig != null)
            targetKills = Mathf.Max(1, contentConfig.p1TargetKills);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void RegisterKillFromMark(MonsterP1A1Mark mark)
    {
        if (mark == null || !mark.IsCompliantForKill)
            return;
        if (compliantKills < targetKills)
            compliantKills++;
    }
}

/// <summary>P1-A 关卡表（ScriptableObject）。Create / EpochOfDawn / P1-A Content Config。与本脚本同文件，避免独立 .cs 未编入程序集时出现 CS0246。</summary>
[CreateAssetMenu(fileName = "P1AContentConfig", menuName = "EpochOfDawn/P1-A Content Config")]
public class P1AContentConfig : ScriptableObject
{
    public const string DefaultResourcePath = "P1A/DefaultP1AContent";

    static P1AContentConfig _cachedDefault;

    public static P1AContentConfig TryLoadDefault()
    {
        if (_cachedDefault == null)
            _cachedDefault = Resources.Load<P1AContentConfig>(DefaultResourcePath);
        return _cachedDefault;
    }

    [Header("P1-A-1 验收")]
    [Min(1)] public int p1TargetKills = 5;

    [Header("P1-A-2 波次")]
    public int[] waveEnemyCounts = { 2, 3, 5 };
    [Min(0f)] public float delayBetweenWaves = 2f;
    [Min(0.1f)] public float waveSpawnRingRadius = 8f;

    [Header("P1-A-1 野外分散刷怪（WildSpawner）")]
    [Min(1)] public int wildEnemyCount = 5;
    [Min(0.1f)] public float wildRingRadius = 8f;

    [Header("P1-A-3 小 Boss 默认")]
    [Min(1f)] public float miniBossHpMultiplier = 3f;
    [Range(0.05f, 0.95f)] public float bossPhaseHpFraction = 0.5f;
    [Min(1f)] public float bossPhaseChaseSpeedMul = 1.25f;
}
