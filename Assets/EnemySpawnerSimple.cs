using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

public class EnemySpawnerSimple : MonoBehaviour
{
    public GameObject enemyPrefab;
    public Transform spawnPoint;
    public float respawnDelay = 3f;

    private GameObject currentEnemy;
    private float timer = 0f;

    void Start()
    {
        if (IsClientOnlyNetworkRuntime())
            return;
        SpawnNow();
    }

    void Update()
    {
        if (IsClientOnlyNetworkRuntime())
            return;
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
        TrySpawnAsNetworkEnemy(currentEnemy);
        timer = 0f;
    }

    static bool IsClientOnlyNetworkRuntime()
    {
        return NetworkManager.Singleton != null &&
               NetworkManager.Singleton.IsListening &&
               !NetworkManager.Singleton.IsServer;
    }

    static void TrySpawnAsNetworkEnemy(GameObject go)
    {
        if (go == null)
            return;
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || !NetworkManager.Singleton.IsServer)
            return;
        if (go.GetComponent<NetworkObject>() == null)
            go.AddComponent<NetworkObject>();
        if (go.GetComponent<NetworkTransform>() == null)
            go.AddComponent<NetworkTransform>();
        if (go.GetComponent<MultiplayerEnemyAuthoritySimple>() == null)
            go.AddComponent<MultiplayerEnemyAuthoritySimple>();
        NetworkObject no = go.GetComponent<NetworkObject>();
        if (!no.IsSpawned)
            no.Spawn();
    }
}
