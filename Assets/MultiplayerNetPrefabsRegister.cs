using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Registers scene-referenced enemy prefabs on both host and every client <b>before</b> the session
/// starts. NGO does not sync runtime Prefabs list — both sides must call the same registration
/// with prefabs that include a <see cref="NetworkObject"/> on the asset, or clients will never
/// replicate server-spawned enemies (they get purged locally or never appear).
/// <para>
/// <see cref="NetworkConfig.PlayerPrefab"/> must also be present in that list: spawn uses
/// <c>GetNetworkObjectToSpawn(hash)</c>, which looks up <see cref="NetworkPrefabs.NetworkPrefabOverrideLinks"/>,
/// not the PlayerPrefab field alone. Missing registration yields "Failed to create object locally" and no
/// <see cref="NetworkManager.LocalClient.PlayerObject"/> on the client.
/// </para>
/// </summary>
public static class MultiplayerNetPrefabsRegister
{
    /// <summary>Call after <c>PlayerPrefab</c> is set (e.g. from Resources). Safe if already listed.</summary>
    public static void RegisterPlayerPrefab(NetworkManager nm)
    {
        if (nm == null || nm.NetworkConfig == null)
            return;
        GameObject p = nm.NetworkConfig.PlayerPrefab;
        if (p == null)
            return;
        if (nm.NetworkConfig.Prefabs.Contains(p))
            return;
        if (p.GetComponent<NetworkObject>() == null)
        {
            Debug.LogError(
                "[MP] PlayerPrefab has no NetworkObject; clients cannot resolve spawn by hash. Fix the prefab asset.");
            return;
        }

        nm.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = p });
        Debug.Log("[MP] Registered NetworkConfig.PlayerPrefab in NetworkConfig.Prefabs: " + p.name);
    }

    public static void RegisterFromSceneSpawners(NetworkManager nm)
    {
        if (nm == null || nm.NetworkConfig == null || nm.NetworkConfig.Prefabs == null)
            return;

        void TryAdd(GameObject prefab)
        {
            if (prefab == null) return;
            if (nm.NetworkConfig.Prefabs.Contains(prefab)) return;
            if (prefab.GetComponent<NetworkObject>() == null)
            {
                Debug.LogWarning(
                    "[MP] Cannot register as network prefab (add NetworkObject to the prefab asset): " +
                    prefab.name);
                return;
            }

            nm.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = prefab });
        }

        var waves = Object.FindObjectsByType<WaveSpawnerSimple>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < waves.Length; i++)
        {
            if (waves[i] != null)
            {
                TryAdd(waves[i].enemyPrefab);
                if (waves[i].enemyPrefab != null)
                {
                    var eh = waves[i].enemyPrefab.GetComponent<EnemyHealthSimple>();
                    if (eh != null && eh.dropPrefab != null)
                        TryAdd(eh.dropPrefab);
                }
            }
        }

        var spawners = Object.FindObjectsByType<EnemySpawnerSimple>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < spawners.Length; i++)
        {
            if (spawners[i] != null)
            {
                TryAdd(spawners[i].enemyPrefab);
                if (spawners[i].enemyPrefab != null)
                {
                    var eh = spawners[i].enemyPrefab.GetComponent<EnemyHealthSimple>();
                    if (eh != null && eh.dropPrefab != null)
                        TryAdd(eh.dropPrefab);
                }
            }
        }

        // If scene spawners are absent, still register a known path (may be same asset as dropPrefab in Editor).
        TryAdd(Resources.Load<GameObject>("Drops/Drop_Coin"));
    }
}
