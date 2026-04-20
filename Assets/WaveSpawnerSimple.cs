using System.Collections.Generic;
using UnityEngine;

/// <summary>P1-A-2：按「波次表」刷怪；清场后经间隔再刷下一波。可与 P1-A-1 共用预制体。</summary>
public class WaveSpawnerSimple : MonoBehaviour
{
    public GameObject enemyPrefab;
    [Tooltip("若赋值，则 Start 时从配置覆盖下列字段（数据驱动）。")]
    public P1AContentConfig contentConfig;
    [Tooltip("当 contentConfig 为空时，是否自动加载 Resources/P1A/DefaultP1AContent 并覆盖场景参数。")]
    public bool useDefaultConfigWhenMissing = true;

    [Tooltip("每波敌人数，例如 2,3,5")]
    public int[] waveEnemyCounts = { 2, 3, 5 };
    [Tooltip("开启后，使用本组件的 waveEnemyCounts 覆盖 contentConfig 里的波次人数。")]
    public bool overrideWaveEnemyCountsFromInspector = false;

    public float delayBetweenWaves = 2f;
    public float spawnRingRadius = 8f;
    public float spawnY = 0.5f;
    public bool loopWaves;
    public Transform center;

    [Header("Runtime (read-only)")]
    public int currentWaveIndex;
    public bool allWavesComplete;

    public int TotalWaveCount => waveEnemyCounts != null ? waveEnemyCounts.Length : 0;

    readonly List<GameObject> _alive = new List<GameObject>();
    int _waveCursor;
    float _spawnAfterTime = -1f;
    bool _waitingToSpawn = true;

    void Start()
    {
        int[] inspectorCounts = waveEnemyCounts != null ? (int[])waveEnemyCounts.Clone() : null;
        if (contentConfig == null && useDefaultConfigWhenMissing)
            contentConfig = P1AContentConfig.TryLoadDefault();
        if (contentConfig != null)
        {
            delayBetweenWaves = contentConfig.delayBetweenWaves;
            spawnRingRadius = contentConfig.waveSpawnRingRadius;
            // 仅当配置里明确有多段波次时才覆盖，避免 Resources 表缺字段时被反序列化成 null/空，把场景里的 2,3,5 冲掉
            if (contentConfig.waveEnemyCounts != null && contentConfig.waveEnemyCounts.Length > 0)
                waveEnemyCounts = (int[])contentConfig.waveEnemyCounts.Clone();
        }

        if (overrideWaveEnemyCountsFromInspector && inspectorCounts != null && inspectorCounts.Length > 0)
            waveEnemyCounts = inspectorCounts;

        if (waveEnemyCounts == null || waveEnemyCounts.Length == 0)
            waveEnemyCounts = new[] { 2, 3, 5 };

        if (enemyPrefab == null)
            return;

        _waveCursor = 0;
        _spawnAfterTime = Time.time;
    }

    void Update()
    {
        if (enemyPrefab == null || allWavesComplete)
            return;

        PruneDead();

        if (_waitingToSpawn && Time.time >= _spawnAfterTime)
        {
            SpawnCurrentWave();
            _waitingToSpawn = false;
        }

        if (!_waitingToSpawn && _alive.Count == 0)
        {
            _waveCursor++;
            if (_waveCursor >= waveEnemyCounts.Length)
            {
                if (!loopWaves)
                {
                    allWavesComplete = true;
                    return;
                }
                _waveCursor = 0;
            }

            _spawnAfterTime = Time.time + delayBetweenWaves;
            _waitingToSpawn = true;
        }
    }

    void PruneDead()
    {
        for (int i = _alive.Count - 1; i >= 0; i--)
        {
            if (_alive[i] == null)
                _alive.RemoveAt(i);
        }
    }

    void SpawnCurrentWave()
    {
        int n = Mathf.Max(1, waveEnemyCounts[_waveCursor]);
        currentWaveIndex = _waveCursor;
        Vector3 c = center != null ? center.position : transform.position;

        for (int i = 0; i < n; i++)
        {
            float ang = (i / (float)n) * Mathf.PI * 2f;
            Vector3 p = c + new Vector3(Mathf.Cos(ang) * spawnRingRadius, spawnY, Mathf.Sin(ang) * spawnRingRadius);
            GameObject go = Instantiate(enemyPrefab, p, Quaternion.identity);
            _alive.Add(go);
        }
    }
}
