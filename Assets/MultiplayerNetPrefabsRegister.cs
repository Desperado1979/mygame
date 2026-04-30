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

    /// <summary>与 PlayerPrefab 同理：服端 Spawn 的运行时对象必须在双方 Prefabs 表里，否则纯 Client 无法生成本地副本、<see cref="PublicObjectiveEventStateSimple"/> 的 NetworkVariable 不会到端上。</summary>
    public const string PublicObjectiveStateResourcesPath = "Netcode/PublicObjectiveStateNet";
    public const string PartyRuntimeStateResourcesPath = "Netcode/PartyRuntimeStateNet";
    public const string ChatRoomStateResourcesPath = "Netcode/ChatRoomStateNet";

    public static void RegisterChatRoomStatePrefab(NetworkManager nm)
    {
        if (nm == null || nm.NetworkConfig == null)
            return;
        GameObject p = Resources.Load<GameObject>(ChatRoomStateResourcesPath);
        if (p == null)
        {
            Debug.LogWarning(
                "[MP] Missing Resources/" + ChatRoomStateResourcesPath +
                ".prefab — room chat / system HUD may not work on pure clients.");
            return;
        }
        if (p.GetComponent<NetworkObject>() == null || p.GetComponent<ChatRoomStateSimple>() == null)
        {
            Debug.LogError(
                "[MP] ChatRoomState prefab must have NetworkObject + ChatRoomStateSimple.");
            return;
        }
        if (nm.NetworkConfig.Prefabs.Contains(p))
            return;
        nm.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = p });
        Debug.Log("[MP] Registered chat room state prefab: " + ChatRoomStateResourcesPath);
    }

    public static void RegisterPublicObjectiveStatePrefab(NetworkManager nm)
    {
        if (nm == null || nm.NetworkConfig == null)
            return;
        GameObject p = Resources.Load<GameObject>(PublicObjectiveStateResourcesPath);
        if (p == null)
        {
            Debug.LogWarning(
                "[MP] Missing Resources/" + PublicObjectiveStateResourcesPath +
                ".prefab — public wave / elite sync may not work on pure clients.");
            return;
        }
        if (p.GetComponent<NetworkObject>() == null || p.GetComponent<PublicObjectiveEventStateSimple>() == null)
        {
            Debug.LogError(
                "[MP] PublicObjectiveState prefab must have NetworkObject + PublicObjectiveEventStateSimple.");
            return;
        }
        if (nm.NetworkConfig.Prefabs.Contains(p))
            return;
        nm.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = p });
        Debug.Log("[MP] Registered public objective state prefab: " + PublicObjectiveStateResourcesPath);
    }

    /// <summary>队伍运行态同样走服务端 Spawn；若未提前注册 prefab，纯 Client 端可能拿不到 PartyRuntimeState 的 NetworkVariable。</summary>
    public static void RegisterPartyRuntimeStatePrefab(NetworkManager nm)
    {
        if (nm == null || nm.NetworkConfig == null)
            return;
        GameObject p = Resources.Load<GameObject>(PartyRuntimeStateResourcesPath);
        if (p == null)
        {
            Debug.LogWarning(
                "[MP] Missing Resources/" + PartyRuntimeStateResourcesPath +
                ".prefab — pure clients may not replicate party runtime state.");
            return;
        }
        if (p.GetComponent<NetworkObject>() == null || p.GetComponent<PartyRuntimeStateSimple>() == null)
        {
            Debug.LogError(
                "[MP] PartyRuntimeState prefab must have NetworkObject + PartyRuntimeStateSimple.");
            return;
        }
        if (nm.NetworkConfig.Prefabs.Contains(p))
            return;
        nm.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = p });
        Debug.Log("[MP] Registered party runtime state prefab: " + PartyRuntimeStateResourcesPath);
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
