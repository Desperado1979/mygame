using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Runs only on <see cref="NetworkManager.IsServer"/> (Host or dedicated server) after the session is listening.
/// Strips <b>offline</b> scene duplicates; never touches GOs that already have a <see cref="NetworkObject"/>.
/// </summary>
public static class NetcodeServerScenePolicy
{
    public static void Apply()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || !NetworkManager.Singleton.IsServer)
            return;

        CullLegacyPlayerBodiesWithoutNetworkObject();
        CullOfflineEnemyDummiesWithoutNetworkObject();
    }

    static void CullLegacyPlayerBodiesWithoutNetworkObject()
    {
        var players = Object.FindObjectsByType<PlayerHealthSimple>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < players.Length; i++)
        {
            var p = players[i];
            if (p == null) continue;
            if (p.GetComponent<MultiplayerPlayerSimple>() != null)
                continue;
            if (p.GetComponent<NetworkObject>() != null)
                continue;
            p.gameObject.SetActive(false);
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
