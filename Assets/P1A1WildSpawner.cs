using UnityEngine;

/// <summary>P1-A-1：在野外点按圆周分散生成若干敌人（占位预制体）。</summary>
public class P1A1WildSpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public int count = 5;
    public float ringRadius = 8f;

    [Tooltip("若赋值，则覆盖 count / ringRadius（数据驱动）。")]
    public P1AContentConfig contentConfig;

    void Start()
    {
        if (contentConfig == null)
            contentConfig = P1AContentConfig.TryLoadDefault();
        float y = 0.5f;
        if (contentConfig != null)
        {
            count = Mathf.Max(1, contentConfig.wildEnemyCount);
            ringRadius = contentConfig.wildRingRadius;
            y = contentConfig.enemySpawnHeightY;
        }

        if (enemyPrefab == null || count <= 0)
            return;

        for (int i = 0; i < count; i++)
        {
            float ang = (i / (float)count) * Mathf.PI * 2f;
            Vector3 p = transform.position + new Vector3(Mathf.Cos(ang) * ringRadius, y, Mathf.Sin(ang) * ringRadius);
            Instantiate(enemyPrefab, p, Quaternion.identity);
        }
    }
}
