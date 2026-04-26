using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Shared mini-event state for public objective elite.
/// Synced to all clients in NGO sessions; local fallback works in single-player.
/// </summary>
public class PublicObjectiveEventStateSimple : NetworkBehaviour
{
    public static PublicObjectiveEventStateSimple Instance { get; private set; }

    readonly NetworkVariable<int> eliteWave = new NetworkVariable<int>(0);
    readonly NetworkVariable<int> eliteDefeatCount = new NetworkVariable<int>(0);
    readonly NetworkVariable<bool> eliteActive = new NetworkVariable<bool>(false);

    public int EliteWave => eliteWave.Value;
    public int EliteDefeatCount => eliteDefeatCount.Value;
    public bool EliteActive => eliteActive.Value;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        base.OnDestroy();
    }

    public void ServerMarkEliteSpawned(int waveNumber)
    {
        if (!IsServer)
            return;
        eliteWave.Value = Mathf.Max(1, waveNumber);
        eliteActive.Value = true;
    }

    public void ServerMarkEliteDefeated()
    {
        if (!IsServer)
            return;
        if (eliteActive.Value)
            eliteDefeatCount.Value += 1;
        eliteActive.Value = false;
    }
}
