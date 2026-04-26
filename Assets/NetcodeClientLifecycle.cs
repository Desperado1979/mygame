using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// <b>Client-only</b> (not Host: <c>IsClient &amp;&amp; !IsServer</c>) post-connect setup.
/// Waits until NGO exposes a local <see cref="NetworkObject"/> player, then performs the minimum scene fixes.
/// Does <b>not</b> disable, destroy, or SetActive on any object that has a <see cref="NetworkObject"/> — those are
/// server / replication owned. This replaces ad-hoc "purge" logic that raced spawn and bricked movement and visibility.
/// </summary>
[DefaultExecutionOrder(-9990)]
public class NetcodeClientLifecycle : MonoBehaviour
{
    const float WaitLocalPlayerTimeoutSec = 12f;

    public void Begin()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            return;
        if (NetworkManager.Singleton.IsServer)
            return;
        StopAllCoroutines();
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        float waited = 0f;
        while (waited < WaitLocalPlayerTimeoutSec)
        {
            var nm = NetworkManager.Singleton;
            if (nm == null || !nm.IsListening)
                yield break;
            if (nm.LocalClient != null && nm.LocalClient.PlayerObject != null)
                break;
            waited += Time.deltaTime;
            yield return null;
        }

        if (NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClient?.PlayerObject == null)
        {
            Debug.LogError(
                "[MP][Client] Local player object never appeared; scene cleanup skipped. Check PlayerPrefab and NGO logs.");
            yield break;
        }

        DisableSpawnersOnClient();
        CullOfflineEnemyDummiesWithoutNetworkObject();
    }

    static void DisableSpawnersOnClient()
    {
        var waveSpawners = Object.FindObjectsByType<WaveSpawnerSimple>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < waveSpawners.Length; i++)
        {
            if (waveSpawners[i] != null)
                waveSpawners[i].enabled = false;
        }

        var enemySpawners = Object.FindObjectsByType<EnemySpawnerSimple>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < enemySpawners.Length; i++)
        {
            if (enemySpawners[i] != null)
                enemySpawners[i].enabled = false;
        }
    }

    static void CullOfflineEnemyDummiesWithoutNetworkObject()
    {
        var enemies = Object.FindObjectsByType<EnemyHealthSimple>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < enemies.Length; i++)
        {
            var e = enemies[i];
            if (e == null) continue;
            if (e.GetComponent<NetworkObject>() != null)
                continue;
            Object.Destroy(e.gameObject);
        }
    }
}
