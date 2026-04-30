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

    /// <summary>与 <see cref="WaveSpawnerSimple"/> 的 currentWaveIndex 一致（0 起算）；由服务端在刷波时写入，供纯 Client 的 HUD 用。</summary>
    readonly NetworkVariable<int> publicWaveIndex0 = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);
    /// <summary>与波次表长度一致；0 表示尚未在服端推过步进（Client 可暂用场景 Spawner 配置）。</summary>
    readonly NetworkVariable<int> publicTotalWaves = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);
    readonly NetworkVariable<bool> publicAllWavesComplete = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public int EliteWave => eliteWave.Value;
    public int EliteDefeatCount => eliteDefeatCount.Value;
    public bool EliteActive => eliteActive.Value;

    public int PublicWaveIndex0 => publicWaveIndex0.Value;
    public int PublicTotalWaves => publicTotalWaves.Value;
    public bool PublicAllWavesComplete => publicAllWavesComplete.Value;

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

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        eliteDefeatCount.OnValueChanged += OnEliteDefeatCountChanged;
    }

    public override void OnNetworkDespawn()
    {
        eliteDefeatCount.OnValueChanged -= OnEliteDefeatCountChanged;
        base.OnNetworkDespawn();
    }

    void OnEliteDefeatCountChanged(int previousValue, int newValue)
    {
        if (newValue <= previousValue)
            return;
        int wave = Mathf.Max(0, eliteWave.Value);
        ApplyEliteDefeatToast(wave, newValue);
    }

    /// <summary>与 NetworkVariable 复制同路径，保证纯 Client 与 Host 同显（ClientRpc 在部分联机拓扑下不可靠）。单机见 <see cref="PublicObjectiveLocalStateSimple.MarkEliteDefeated"/>。</summary>
    public static void ApplyEliteDefeatToast(int wave, int totalKills)
    {
        P1AContentConfig c = P1AContentConfig.TryLoadDefault();
        string fmt = (c != null && !string.IsNullOrWhiteSpace(c.publicObjectiveEliteDefeatToast))
            ? c.publicObjectiveEliteDefeatToast
            : "公目:精英已击败 序{0} 计{1}";
        try
        {
            PublicObjectiveLastToast.Set(string.Format(fmt, wave, totalKills));
        }
        catch (System.FormatException)
        {
            PublicObjectiveLastToast.Set($"公目:精英已击败 序{wave} 计{totalKills}");
        }
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
        {
            int wave = Mathf.Max(0, eliteWave.Value);
            eliteDefeatCount.Value += 1;
            int total = eliteDefeatCount.Value;
            eliteActive.Value = false;
            // 全端同显：与 NetworkVariable.OnValueChanged 双通道；纯 Client/观战 上 RPC 到齐更稳。
            NotifyEliteDefeatedToClientsClientRpc(wave, total);
        }
        else
        {
            eliteActive.Value = false;
        }
    }

    [ClientRpc]
    void NotifyEliteDefeatedToClientsClientRpc(int wave, int totalKills)
    {
        ApplyEliteDefeatToast(wave, totalKills);
    }

    /// <summary>仅服务端：<see cref="WaveSpawnerSimple"/> 推进/结束时调用，将波次显式同步到各 Client（Spawner 在纯 Client 上不跑 Update，本地字段不增长）。</summary>
    public void ServerSetPublicWaveState(int currentWaveIndex0, int totalWaves, bool allWavesComplete)
    {
        if (!IsServer)
            return;
        publicWaveIndex0.Value = Mathf.Max(0, currentWaveIndex0);
        publicTotalWaves.Value = Mathf.Max(0, totalWaves);
        publicAllWavesComplete.Value = allWavesComplete;
    }
}

/// <summary>
/// 未起多机 Host 时（单机默认），与 <see cref="PublicObjectiveEventStateSimple"/> 二选一；供 HUD 与精英计次，不与 NGO 的 NetworkVariable 混写。
/// </summary>
public sealed class PublicObjectiveLocalStateSimple : MonoBehaviour
{
    public static PublicObjectiveLocalStateSimple Instance { get; private set; }

    int _eliteWave;
    int _defeatCount;
    bool _eliteActive;

    public int EliteWave => _eliteWave;
    public int EliteDefeatCount => _defeatCount;
    public bool EliteActive => _eliteActive;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static PublicObjectiveLocalStateSimple Ensure()
    {
        if (PublicObjectiveEventStateSimple.Instance != null)
            return null;
        if (Instance != null)
            return Instance;
        GameObject go = new GameObject("PublicObjectiveLocalStateRuntime");
        DontDestroyOnLoad(go);
        return go.AddComponent<PublicObjectiveLocalStateSimple>();
    }

    public void MarkEliteSpawned(int waveNumber)
    {
        _eliteWave = Mathf.Max(1, waveNumber);
        _eliteActive = true;
    }

    public void MarkEliteDefeated()
    {
        if (_eliteActive)
        {
            _defeatCount += 1;
            int w = _eliteWave;
            int t = _defeatCount;
            _eliteActive = false;
            PublicObjectiveEventStateSimple.ApplyEliteDefeatToast(w, t);
        }
        else
        {
            _eliteActive = false;
        }
    }
}
