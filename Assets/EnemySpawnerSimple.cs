using UnityEngine;

public class EnemySpawnerSimple : MonoBehaviour
{
    public GameObject enemyPrefab;
    public Transform spawnPoint;
    public float respawnDelay = 3f;

    private GameObject currentEnemy;
    private float timer = 0f;

    void Start()
    {
        SpawnNow();
    }

    void Update()
    {
        if (currentEnemy != null) return;

        timer += Time.deltaTime;
        if (timer >= respawnDelay)
        {
            SpawnNow();
        }
    }

    void SpawnNow()
    {
        if (enemyPrefab == null || spawnPoint == null) return;
        currentEnemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
        timer = 0f;
    }
}
