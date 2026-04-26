using System;
using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

/// <summary>
/// Minimal multiplayer bootstrap for 5-player internal playtests.
/// Keeps single-player content intact and only starts networking when explicitly enabled.
/// </summary>
[DefaultExecutionOrder(-10000)]
public class MultiplayerBootstrapSimple : MonoBehaviour
{
    public static MultiplayerBootstrapSimple Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreateBootstrap()
    {
        MultiplayerBootstrapSimple existing = FindAnyObjectByType<MultiplayerBootstrapSimple>();
        if (existing != null)
            return;
        GameObject go = new GameObject("MultiplayerBootstrapRuntime");
        DontDestroyOnLoad(go);
        go.AddComponent<MultiplayerBootstrapSimple>();
    }

    public enum StartupMode
    {
        Disabled = 0,
        AutoFromCommandLine = 1,
        Host = 2,
        Client = 3,
        DedicatedServer = 4
    }

    [Header("Startup")]
    public StartupMode startupMode = StartupMode.AutoFromCommandLine;
    public bool autoStartOnPlay = true;
    public bool disableScenePlayersWhenNetworkStarts = true;
    public MultiplayerConnectionConfigSimple connectionConfig;

    [Header("Transport")]
    public string connectAddress = "127.0.0.1";
    public ushort connectPort = 7777;

    [Header("Runtime")]
    public bool started;
    public StartupMode effectiveMode = StartupMode.Disabled;

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

    void Start()
    {
        ApplyConfigOverrides();
        if (!autoStartOnPlay || started)
            return;
        StartNetworking();
    }

    void ApplyConfigOverrides()
    {
        if (connectionConfig == null)
            return;
        startupMode = connectionConfig.startupMode;
        autoStartOnPlay = connectionConfig.autoStartOnPlay;
        disableScenePlayersWhenNetworkStarts = connectionConfig.disableScenePlayersWhenNetworkStarts;
        if (!string.IsNullOrWhiteSpace(connectionConfig.connectAddress))
            connectAddress = connectionConfig.connectAddress.Trim();
        connectPort = connectionConfig.connectPort;
    }

    [ContextMenu("Start Networking Now")]
    public void StartNetworking()
    {
        if (started)
            return;
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            started = true;
            return;
        }

        effectiveMode = ResolveStartupMode();
        if (effectiveMode == StartupMode.Disabled)
            return;

        NetworkManager nm = EnsureNetworkManager();
        EnsureNetworkConfigWithTransport(nm);
        ConfigureTransport(nm, effectiveMode);
        // Same prefab list on host + all clients before listen, or late-join clients cannot spawn enemy visuals.
        MultiplayerNetPrefabsRegister.RegisterFromSceneSpawners(nm);
        EnsureRuntimePlayerPrefab(nm);
        MultiplayerNetPrefabsRegister.RegisterPlayerPrefab(nm);

        // 纯 Client：先保留场景里单机人物；等本地网络玩家生成后再关，避免「未连上 Host 却先隐藏自己」。
        bool deferDisableSceneForClient = disableScenePlayersWhenNetworkStarts
            && effectiveMode == StartupMode.Client;
        if (disableScenePlayersWhenNetworkStarts && !deferDisableSceneForClient)
            DisableScenePlayers();

        started = StartByMode(nm, effectiveMode);
        if (started && nm != null && nm.IsServer)
        {
            EnsurePublicObjectiveStateObject();
            EnsurePartyRuntimeStateObject();
        }
        if (started)
            StartCoroutine(CoAfterNetworkListen());
        if (started && deferDisableSceneForClient)
            StartCoroutine(CoDisableSceneWhenClientPlayerSpawnedOrTimeout());
        Debug.Log($"[MP] startup={effectiveMode} started={started} address={connectAddress}:{connectPort}");
    }

    /// <summary>纯 Client 连上后才隐藏场景里旧 Player；连不上则关掉网络并保留单机可玩性。</summary>
    IEnumerator CoDisableSceneWhenClientPlayerSpawnedOrTimeout()
    {
        const float waitSec = 20f;
        float t = 0f;
        while (t < waitSec)
        {
            var nm = NetworkManager.Singleton;
            if (nm == null)
                yield break;
            if (nm.LocalClient != null && nm.LocalClient.PlayerObject != null)
            {
                DisableScenePlayers();
                yield break;
            }
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        var nmc = NetworkManager.Singleton;
        if (nmc == null)
            yield break;
        if (nmc.LocalClient != null && nmc.LocalClient.PlayerObject != null)
        {
            DisableScenePlayers();
            yield break;
        }

        Debug.LogWarning(
            "[MP] Client: no network player in time; shutting down network so the scene single-player can stay visible.");
        nmc.Shutdown();
        started = false;
    }

    /// <summary>One frame for NGO spawn, then <see cref="NetcodeServerScenePolicy"/> on any server; <see cref="NetcodeClientLifecycle"/> only when pure client.</summary>
    IEnumerator CoAfterNetworkListen()
    {
        yield return null;
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsListening)
            yield break;
        if (nm.IsServer)
            NetcodeServerScenePolicy.Apply();
        if (nm.IsClient && !nm.IsServer)
        {
            NetcodeClientLifecycle life = GetComponent<NetcodeClientLifecycle>();
            if (life == null) life = gameObject.AddComponent<NetcodeClientLifecycle>();
            life.Begin();
        }
    }

    StartupMode ResolveStartupMode()
    {
        string[] args = Environment.GetCommandLineArgs();
        ApplyCommandLineNetworkOverrides(args);

        if (startupMode != StartupMode.AutoFromCommandLine)
            return startupMode;

        if (Application.isBatchMode)
            return StartupMode.DedicatedServer;

        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i] ?? "";
            if (a.Equals("-dedicated", StringComparison.OrdinalIgnoreCase) ||
                a.Equals("-server", StringComparison.OrdinalIgnoreCase))
                return StartupMode.DedicatedServer;
            if (a.Equals("-host", StringComparison.OrdinalIgnoreCase))
                return StartupMode.Host;
            if (a.Equals("-client", StringComparison.OrdinalIgnoreCase))
                return StartupMode.Client;
            if (a.Equals("-mp-off", StringComparison.OrdinalIgnoreCase))
                return StartupMode.Disabled;
        }
#if UNITY_EDITOR
        // Editor Play defaults to single-player unless explicitly -host/-client/-server.
        return StartupMode.Disabled;
#else
        return StartupMode.Host;
#endif
    }

    void ApplyCommandLineNetworkOverrides(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i] ?? "";
            if (a.Equals("-mp-off", StringComparison.OrdinalIgnoreCase))
            {
                startupMode = StartupMode.Disabled;
                continue;
            }
            if (a.Equals("-host", StringComparison.OrdinalIgnoreCase)) startupMode = StartupMode.Host;
            if (a.Equals("-client", StringComparison.OrdinalIgnoreCase)) startupMode = StartupMode.Client;
            if (a.Equals("-server", StringComparison.OrdinalIgnoreCase) ||
                a.Equals("-dedicated", StringComparison.OrdinalIgnoreCase))
                startupMode = StartupMode.DedicatedServer;
            if (a.Equals("-address", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                connectAddress = args[i + 1];
            if (a.Equals("-port", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length && ushort.TryParse(args[i + 1], out ushort p))
                connectPort = p;
        }
    }

    NetworkManager EnsureNetworkManager()
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (nm != null)
            return nm;

        GameObject go = new GameObject("NetworkManagerRuntime");
        DontDestroyOnLoad(go);
        nm = go.AddComponent<NetworkManager>();
        go.AddComponent<UnityTransport>();
        return nm;
    }

    /// <summary>运行时创建的 <see cref="NetworkManager"/> 没有序列化过的 <see cref="NetworkManager.NetworkConfig"/>，必须先实例化并绑定 Transport。</summary>
    void EnsureNetworkConfigWithTransport(NetworkManager nm)
    {
        if (nm.NetworkConfig == null)
            nm.NetworkConfig = new NetworkConfig();

        // P1A 单场景 + 同 prefab 多实例（波次怪）：开集成场景管理时，客户端在 Sync 会 PopulateScenePlacedObjects。
        // 同场景内多份相同 GlobalObjectIdHash 且 IsSceneObject 为 true/null 时会抛
        // "ScenePlacedObjects which already contains the same GlobalObjectIdHash"。
        // 关集成场景管理走 NGO 的 prefab/已生成对象同步，与本项目用法一致（未使用 NGO LoadScene 切场景）。
        nm.NetworkConfig.EnableSceneManagement = false;

        UnityTransport transport = nm.GetComponent<UnityTransport>();
        if (transport == null)
            transport = nm.gameObject.AddComponent<UnityTransport>();

        if (nm.NetworkConfig.NetworkTransport == null)
            nm.NetworkConfig.NetworkTransport = transport;
    }

    /// <summary>Host/专服在 <c>0.0.0.0</c> 上侦听，客户端再连 <see cref="connectAddress"/>；避免仅绑定 loopback 时在本机双开/部分网卡上 UDP 异常。</summary>
    void ConfigureTransport(NetworkManager nm, StartupMode mode)
    {
        UnityTransport transport = nm.GetComponent<UnityTransport>();
        if (transport == null)
            transport = nm.gameObject.AddComponent<UnityTransport>();

        if (mode == StartupMode.Client)
        {
            transport.SetConnectionData(connectAddress, connectPort);
        }
        else
        {
            // Third arg = server bind address. Clients still use connectAddress:port to reach the host.
            transport.SetConnectionData(connectAddress, connectPort, "0.0.0.0");
        }
    }

    void EnsureRuntimePlayerPrefab(NetworkManager nm)
    {
        if (nm == null || nm.NetworkConfig == null)
            return;
        if (nm.NetworkConfig.PlayerPrefab != null)
            return;

        GameObject resourcesPrefab = Resources.Load<GameObject>(NetPlayerHostFactory.ResourcesPath);
        if (resourcesPrefab != null)
        {
            nm.NetworkConfig.PlayerPrefab = resourcesPrefab;
            Debug.Log("[MP] PlayerPrefab from Resources: " + NetPlayerHostFactory.ResourcesPath);
            return;
        }

        var holder = new GameObject("NetPlayerHostTemplateHolder");
        DontDestroyOnLoad(holder);
        GameObject built = NetPlayerHostFactory.CreateRuntimePlayerTemplate(
            NetPlayerHostFactory.RuntimeTemplatePosition,
            holder.transform,
            hideInHierarchy: true,
            objectName: "NetPlayerRuntimeTemplate");
        nm.NetworkConfig.PlayerPrefab = built;
        Debug.LogWarning(
            "[MP] No Resources prefab at '" + NetPlayerHostFactory.ResourcesPath + "'. Using DDOL template at " +
            NetPlayerHostFactory.RuntimeTemplatePosition +
            ". Bake: Tools / EpochOfDawn / Netcode / Bake NetPlayerHost Prefab (writes Assets/Resources/...).");
    }

    void DisableScenePlayers()
    {
        var movers = FindObjectsByType<PlayerMoveSimple>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < movers.Length; i++)
        {
            GameObject go = movers[i].gameObject;
            if (go != null && go.activeSelf && go.GetComponent<MultiplayerPlayerSimple>() == null)
                go.SetActive(false);
        }

        // Extra safety: old scene player might not have PlayerMoveSimple enabled but still has health/attack.
        var hpHolders = FindObjectsByType<PlayerHealthSimple>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < hpHolders.Length; i++)
        {
            GameObject go = hpHolders[i] != null ? hpHolders[i].gameObject : null;
            if (go == null)
                continue;
            if (go.GetComponent<MultiplayerPlayerSimple>() != null)
                continue;
            if (go.activeSelf)
                go.SetActive(false);
        }

        var attacks = FindObjectsByType<PlayerAttackSimple>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < attacks.Length; i++)
        {
            GameObject go = attacks[i] != null ? attacks[i].gameObject : null;
            if (go == null)
                continue;
            if (go.GetComponent<MultiplayerPlayerSimple>() != null)
                continue;
            if (go.activeSelf)
                go.SetActive(false);
        }
    }

    bool StartByMode(NetworkManager nm, StartupMode mode)
    {
        switch (mode)
        {
            case StartupMode.Host:
                return nm.StartHost();
            case StartupMode.Client:
                return nm.StartClient();
            case StartupMode.DedicatedServer:
                return nm.StartServer();
            default:
                return false;
        }
    }

    void EnsurePublicObjectiveStateObject()
    {
        if (PublicObjectiveEventStateSimple.Instance != null)
        {
            NetworkObject existingNo = PublicObjectiveEventStateSimple.Instance.GetComponent<NetworkObject>();
            if (existingNo != null && !existingNo.IsSpawned)
                existingNo.Spawn();
            return;
        }

        GameObject go = new GameObject("PublicObjectiveStateRuntime");
        DontDestroyOnLoad(go);
        NetworkObject no = go.AddComponent<NetworkObject>();
        go.AddComponent<PublicObjectiveEventStateSimple>();
        if (!no.IsSpawned)
            no.Spawn();
    }

    void EnsurePartyRuntimeStateObject()
    {
        if (PartyRuntimeStateSimple.Instance != null)
        {
            NetworkObject existingNo = PartyRuntimeStateSimple.Instance.GetComponent<NetworkObject>();
            if (existingNo != null && !existingNo.IsSpawned)
                existingNo.Spawn();
            return;
        }

        GameObject go = new GameObject("PartyRuntimeStateRuntime");
        DontDestroyOnLoad(go);
        NetworkObject no = go.AddComponent<NetworkObject>();
        go.AddComponent<PartyRuntimeStateSimple>();
        if (!no.IsSpawned)
            no.Spawn();
    }

    /// <summary>供 <see cref="DebugHudSimple"/> 一行观测本机多开 / LAN。文案优先 <see cref="P1AContentConfig"/>。</summary>
    public string BuildMultiplayerHudLine()
    {
        P1AContentConfig t = P1AContentConfig.TryLoadDefault();
        string mTag = MultiplayerHudPick(t, c => c.hudMultiTag, "MP");
        string kC = MultiplayerHudPick(t, c => c.hudMultiKeyColon, ":");
        string offN = MultiplayerHudPick(t, c => c.hudMultiStateOff, "off");
        if (!started || effectiveMode == StartupMode.Disabled)
            return mTag + kC + offN;

        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsListening)
        {
            string em = effectiveMode switch
            {
                StartupMode.Host => MultiplayerHudPick(t, c => c.hudMultiRoleHost, "host"),
                StartupMode.Client => MultiplayerHudPick(t, c => c.hudMultiRoleCli, "cli"),
                StartupMode.DedicatedServer => MultiplayerHudPick(t, c => c.hudMultiRoleSrv, "srv"),
                StartupMode.AutoFromCommandLine => MultiplayerHudPick(t, c => c.hudMultiRoleAuto, "auto"),
                _ => offN
            };
            string tail = MultiplayerHudKeep(t, c => c.hudMultiDisconnectedTail, " …");
            return mTag + kC + em + tail;
        }

        string hS = MultiplayerHudPick(t, c => c.hudMultiRoleHost, "host");
        string cS = MultiplayerHudPick(t, c => c.hudMultiRoleCli, "cli");
        string sS = MultiplayerHudPick(t, c => c.hudMultiRoleSrv, "srv");
        string role;
        if (nm.IsHost) role = hS;
        else if (nm.IsServer) role = sS;
        else role = cS;

        // ConnectedClients is server-only; ConnectedClientsIds is available on client for HUD.
        int n = nm.ConnectedClientsIds != null ? nm.ConnectedClientsIds.Count : 0;
        string ccK = MultiplayerHudPick(t, c => c.hudMultiCcKey, "cc");
        string g1 = MultiplayerHudKeep(t, c => c.hudMultiBlockGap, " ");
        string g2 = MultiplayerHudKeep(t, c => c.hudMultiCcToAddrSpace, " ");
        string aSep = MultiplayerHudPick(t, c => c.hudMultiAddrPortSep, ":");
        return mTag + kC + role + g1 + ccK + kC + n + g2 + connectAddress + aSep + connectPort;
    }

    static string MultiplayerHudPick(P1AContentConfig t, Func<P1AContentConfig, string> f, string def)
    {
        if (t == null) return def;
        string s = f(t);
        if (string.IsNullOrWhiteSpace(s)) return def;
        return s.Trim();
    }

    static string MultiplayerHudKeep(P1AContentConfig t, Func<P1AContentConfig, string> f, string def)
    {
        if (t == null) return def;
        string s = f(t);
        if (string.IsNullOrWhiteSpace(s)) return def;
        return s;
    }
}
