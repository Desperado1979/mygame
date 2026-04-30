using System;
using System.Collections;
using System.Collections.Generic;
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

    readonly List<GameObject> _sceneRootDisabledForNet = new List<GameObject>(2);
    bool _netClientHooksRegistered;
    bool _clientReconnectArmed;
    int _clientReconnectAttempt;
    float _clientReconnectWindowStartUnscaled = float.NegativeInfinity;
    float _clientReconnectNextAtUnscaled = float.NegativeInfinity;
    string _clientReconnectReason = "";
    float _sessionStartedAtUnscaled = float.NegativeInfinity;
    int _clientDisconnectEvents;
    int _clientRecoverCount;
    bool _fatalBlocker;
    string _fatalReason = "";
    int _reconnectMaxAttempts = 3;
    float _reconnectWindowSec = 60f;
    float _reconnectRetryDelaySec = 5f;
    float _listEmptyStopPollingSec = 25f;
    float _listEmptyPollStepSec = 0.4f;
    float _reconnectArmDelaySec = 0.8f;
    float _sceneDisableGuardWindowSec = 6f;
    float _sceneDisableGuardIntervalSec = 0.5f;
    float _sceneDuplicateHealIntervalSec = 1.5f;
    float _sceneDuplicateHealNextAtUnscaled = float.NegativeInfinity;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        D3GrowthBalanceData d = D3GrowthBalance.Load();
        _reconnectMaxAttempts = Mathf.Max(1, d.netClientReconnectMaxAttempts);
        _reconnectWindowSec = Mathf.Max(1f, d.netClientReconnectWindowSec);
        _reconnectRetryDelaySec = Mathf.Max(0.5f, d.netClientReconnectRetryDelaySec);
        _listEmptyStopPollingSec = Mathf.Max(1f, d.netClientListEmptyStopPollingSec);
        _listEmptyPollStepSec = Mathf.Max(0.05f, d.netClientListEmptyPollStepSec);
        _reconnectArmDelaySec = Mathf.Max(0.05f, d.netClientReconnectArmDelaySec);
        _sceneDisableGuardWindowSec = Mathf.Max(0f, d.netScenePlayerDisableGuardWindowSec);
        _sceneDisableGuardIntervalSec = Mathf.Max(0.05f, d.netScenePlayerDisableGuardIntervalSec);
        _sceneDuplicateHealIntervalSec = Mathf.Max(0.1f, d.netScenePlayerDuplicateHealIntervalSec);
    }

    void OnDestroy()
    {
        UnregisterNetClientCallbacks();
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

    void Update()
    {
        TickClientReconnect();
        TickScenePlayerDuplicateHeal();
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
        MultiplayerNetPrefabsRegister.RegisterPublicObjectiveStatePrefab(nm);
        MultiplayerNetPrefabsRegister.RegisterPartyRuntimeStatePrefab(nm);
        MultiplayerNetPrefabsRegister.RegisterChatRoomStatePrefab(nm);

        // 纯 Client：先保留场景里单机人物；等本地网络玩家生成后再关，避免「未连上 Host 却先隐藏自己」。
        bool deferDisableSceneForClient = disableScenePlayersWhenNetworkStarts
            && effectiveMode == StartupMode.Client;
        if (disableScenePlayersWhenNetworkStarts && !deferDisableSceneForClient)
            DisableScenePlayers();

        started = StartByMode(nm, effectiveMode);
        if (started)
        {
            // 首次进会话才重置计时；重连窗口内不重置，避免 up 反复回到 00:00 造成“闪变”。
            if (!_clientReconnectArmed || _sessionStartedAtUnscaled <= 0f)
                _sessionStartedAtUnscaled = Time.unscaledTime;
            if (!_clientReconnectArmed)
            {
                _clientDisconnectEvents = 0;
                _clientRecoverCount = 0;
                _fatalBlocker = false;
                _fatalReason = "";
            }
        }
        if (started && nm != null && nm.IsServer)
        {
            EnsurePublicObjectiveStateObject();
            EnsurePartyRuntimeStateObject();
            EnsureChatRoomStateObject();
        }
        if (started)
            StartCoroutine(CoAfterNetworkListen());
        if (started && disableScenePlayersWhenNetworkStarts)
            StartCoroutine(CoReassertScenePlayersDisabledForNetWindow());
        if (started && deferDisableSceneForClient)
            StartCoroutine(CoDisableSceneWhenClientPlayerSpawnedOrTimeout());
        if (started && effectiveMode == StartupMode.Client)
        {
            RegisterNetClientCallbacks();
            StartCoroutine(CoWatchClientConnectionThenRestoreIfStuck());
        }
        Debug.Log($"[MP] startup={effectiveMode} started={started} address={connectAddress}:{connectPort}");
    }

    /// <summary>纯 Client 连上后才隐藏场景里旧 Player；连不上则关掉网络并保留单机可玩性。</summary>
    IEnumerator CoDisableSceneWhenClientPlayerSpawnedOrTimeout()
    {
        float waitSec = Mathf.Max(1f, D3GrowthBalance.Load().netClientSpawnPlayerWaitSec);
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
        ClientRecoverAfterConnectionLost("no_network_player_timeout");
    }

    IEnumerator CoReassertScenePlayersDisabledForNetWindow()
    {
        if (_sceneDisableGuardWindowSec <= 0f)
            yield break;
        float elapsed = 0f;
        while (elapsed < _sceneDisableGuardWindowSec)
        {
            var nm = NetworkManager.Singleton;
            if (!started || nm == null || !nm.IsListening)
                yield break;
            if (effectiveMode == StartupMode.Client
                && (nm.LocalClient == null || nm.LocalClient.PlayerObject == null))
            {
                // Pure client waits for network player spawn; avoid hiding scene fallback too early.
            }
            else
            {
                DisableScenePlayers();
            }
            yield return new WaitForSecondsRealtime(_sceneDisableGuardIntervalSec);
            elapsed += _sceneDisableGuardIntervalSec;
        }
    }

    void TickScenePlayerDuplicateHeal()
    {
        if (!started || !disableScenePlayersWhenNetworkStarts)
            return;
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsListening)
            return;
        if (Time.unscaledTime < _sceneDuplicateHealNextAtUnscaled)
            return;
        _sceneDuplicateHealNextAtUnscaled = Time.unscaledTime + _sceneDuplicateHealIntervalSec;

        int dup = CountActiveScenePlayerRootsForNetRisk();
        if (dup <= 0)
            return;
        if (effectiveMode == StartupMode.Client
            && (nm.LocalClient == null || nm.LocalClient.PlayerObject == null))
            return;

        DisableScenePlayers();
        Debug.LogWarning("[MP] Duplicate scene-player roots detected and disabled: " + dup);
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

    void RegisterNetClientCallbacks()
    {
        if (_netClientHooksRegistered)
            return;
        if (NetworkManager.Singleton == null)
            return;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnNetcodeClientDisconnect;
        _netClientHooksRegistered = true;
    }

    void UnregisterNetClientCallbacks()
    {
        if (!_netClientHooksRegistered)
            return;
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnNetcodeClientDisconnect;
        _netClientHooksRegistered = false;
    }

    void OnNetcodeClientDisconnect(ulong clientId)
    {
        if (Instance != this)
            return;
        if (NetworkManager.Singleton == null)
            return;
        if (NetworkManager.Singleton.IsServer)
            return;
        _clientDisconnectEvents++;
        // 纯 Client 被服踢下或断线后：如已关过场景体，必须还原，否则无输入
        ClientRecoverAfterConnectionLost($"OnClientDisconnect({clientId})");
    }

    /// <summary>
    /// UTP 在 Host 已关或端口冲突时，Client 上 IsListening 可能已假，而 NetworkPlayer 已生成、场景体已关，导致无角色移动。
    /// 若 <see cref="_sceneRootDisabledForNet"/> 一直为空，仅短轮询等待 <see cref="CoDisableSceneWhenClientPlayerSpawnedOrTimeout"/> 可能关场景，避免 while(true)+continue 空转。
    /// </summary>
    IEnumerator CoWatchClientConnectionThenRestoreIfStuck()
    {
        float waitedListEmpty = 0f;
        for (;;)
        {
            yield return new WaitForSecondsRealtime(_listEmptyPollStepSec);
            if (!started || effectiveMode != StartupMode.Client)
                yield break;
            if (this == null)
                yield break;
            var nm = NetworkManager.Singleton;
            if (nm == null)
                yield break;
            if (nm.IsServer)
                yield break;
            if (!nm.IsClient)
                yield break;
            if (!nm.IsListening)
            {
                if (_sceneRootDisabledForNet.Count > 0)
                    ClientRecoverAfterConnectionLost("transport_not_listening");
                else if (started)
                {
                    ClientRecoverAfterConnectionLost("transport_not_listening_no_scene_restore_list");
                }
                UnregisterNetClientCallbacks();
                yield break;
            }

            if (_sceneRootDisabledForNet.Count == 0)
            {
                waitedListEmpty += _listEmptyPollStepSec;
                if (waitedListEmpty >= _listEmptyStopPollingSec)
                    yield break;
                continue;
            }

            if (nm.LocalClient == null || nm.LocalClient.PlayerObject == null)
            {
                ClientRecoverAfterConnectionLost("local_network_player_lost");
                yield break;
            }
        }
    }

    void ClientRecoverAfterConnectionLost(string reason)
    {
        for (int i = 0; i < _sceneRootDisabledForNet.Count; i++)
        {
            GameObject go = _sceneRootDisabledForNet[i];
            if (go != null && !go.activeSelf)
                go.SetActive(true);
        }
        _sceneRootDisabledForNet.Clear();
        var nm = NetworkManager.Singleton;
        if (nm != null && nm.IsListening)
            nm.Shutdown();
        started = false;
        UnregisterNetClientCallbacks();
        // Day6：无论之前是否记录到 _sceneRootDisabledForNet，都尝试恢复一套可操控的本地玩家与相机绑定。
        TryRestoreOfflinePlayerAndCamera();
        _clientRecoverCount++;
        ArmClientReconnect(reason);
        Debug.LogWarning("[MP] Client: recovered from failed session — " + reason);
    }

    void TryRestoreOfflinePlayerAndCamera()
    {
        Transform chosen = null;
        PlayerMoveSimple[] moves = UnityEngine.Object.FindObjectsByType<PlayerMoveSimple>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int i = 0; i < moves.Length; i++)
        {
            PlayerMoveSimple mv = moves[i];
            if (mv == null)
                continue;
            NetworkObject no = mv.GetComponent<NetworkObject>();
            if (no != null)
                continue;
            Transform root = mv.transform.root;
            if (root != null && !root.gameObject.activeSelf)
                root.gameObject.SetActive(true);
            chosen = mv.transform;
            break;
        }

        if (chosen == null)
        {
            PlayerHealthSimple[] hs = UnityEngine.Object.FindObjectsByType<PlayerHealthSimple>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < hs.Length; i++)
            {
                PlayerHealthSimple h = hs[i];
                if (h == null)
                    continue;
                NetworkObject no = h.GetComponent<NetworkObject>();
                if (no != null)
                    continue;
                Transform root = h.transform.root;
                if (root != null && !root.gameObject.activeSelf)
                    root.gameObject.SetActive(true);
                chosen = h.transform;
                break;
            }
        }

        if (chosen == null)
            return;

        Camera cam = Camera.main;
        if (cam != null)
        {
            CameraFollowSimple follow = cam.GetComponent<CameraFollowSimple>();
            if (follow != null)
                follow.target = chosen;
        }

        DebugHudSimple hud = FindAnyObjectByType<DebugHudSimple>();
        if (hud != null)
        {
            hud.player = chosen;
            hud.health = chosen.GetComponent<PlayerHealthSimple>();
            hud.mp = chosen.GetComponent<PlayerMpSimple>();
            if (hud.stateExport == null)
                hud.stateExport = chosen.GetComponent<PlayerStateExportSimple>();
            if (hud.playerStats == null)
                hud.playerStats = chosen.GetComponent<PlayerStatsSimple>();
            if (hud.equipDebug == null)
                hud.equipDebug = chosen.GetComponent<PlayerEquipmentDebugSimple>();
        }

        PlayerHealthSimple.SetPreferredInstance(chosen.GetComponent<PlayerHealthSimple>());
        PlayerProgressSimple.SetPreferredInstance(chosen.GetComponent<PlayerProgressSimple>());
        PlayerWalletSimple.SetPreferredInstance(chosen.GetComponent<PlayerWalletSimple>());
    }

    void ArmClientReconnect(string reason)
    {
        if (effectiveMode != StartupMode.Client)
            return;
        string why = string.IsNullOrWhiteSpace(reason) ? "lost" : reason.Trim();
        float now = Time.unscaledTime;
        bool keepExistingWindow = _clientReconnectArmed
            && _clientReconnectWindowStartUnscaled > 0f
            && (now - _clientReconnectWindowStartUnscaled) <= _reconnectWindowSec;
        if (!keepExistingWindow)
        {
            _clientReconnectArmed = true;
            _clientReconnectAttempt = 0;
            _clientReconnectWindowStartUnscaled = now;
        }
        _clientReconnectNextAtUnscaled = now + _reconnectArmDelaySec;
        _clientReconnectReason = why;
    }

    void ClearClientReconnectState()
    {
        _clientReconnectArmed = false;
        _clientReconnectAttempt = 0;
        _clientReconnectWindowStartUnscaled = float.NegativeInfinity;
        _clientReconnectNextAtUnscaled = float.NegativeInfinity;
        _clientReconnectReason = "";
    }

    void TickClientReconnect()
    {
        var nmNow = NetworkManager.Singleton;
        // Day6 兜底：若未走到 Recover 分支，但会话已跑过且当前已断开，也要进入 rc:x/3 以便可观测与重连。
        if (!_clientReconnectArmed
            && effectiveMode == StartupMode.Client
            && _sessionStartedAtUnscaled > 0f)
        {
            if (nmNow == null || !nmNow.IsListening)
                ArmClientReconnect("auto_detect_not_listening");
        }

        if (!_clientReconnectArmed)
            return;
        if (effectiveMode != StartupMode.Client)
        {
            ClearClientReconnectState();
            return;
        }
        // 仅在「真正回到已连接且已生成本地网络玩家」后清空 rc；
        // StartClient()/IsConnectedClient 过早为 true 时不可作为成功标准。
        if (started
            && nmNow != null
            && nmNow.IsListening
            && nmNow.IsClient
            && !nmNow.IsServer
            && nmNow.IsConnectedClient
            && nmNow.LocalClient != null
            && nmNow.LocalClient.PlayerObject != null)
        {
            ClearClientReconnectState();
            return;
        }

        float elapsed = Time.unscaledTime - _clientReconnectWindowStartUnscaled;
        if (_clientReconnectAttempt >= _reconnectMaxAttempts || elapsed > _reconnectWindowSec)
        {
            Debug.LogWarning(
                $"[MP] Client: reconnect exhausted ({_clientReconnectAttempt}/{_reconnectMaxAttempts}) reason={_clientReconnectReason}");
            _fatalBlocker = true;
            _fatalReason = string.IsNullOrWhiteSpace(_clientReconnectReason) ? "reconnect_exhausted" : _clientReconnectReason;
            ClearClientReconnectState();
            return;
        }
        if (Time.unscaledTime < _clientReconnectNextAtUnscaled)
            return;

        if (nmNow != null && nmNow.IsListening)
        {
            nmNow.Shutdown();
            started = false;
            UnregisterNetClientCallbacks();
        }

        _clientReconnectAttempt++;
        Debug.Log(
            $"[MP] Client: reconnect attempt {_clientReconnectAttempt}/{_reconnectMaxAttempts} reason={_clientReconnectReason}");
        StartNetworking();
        _clientReconnectNextAtUnscaled = Time.unscaledTime + _reconnectRetryDelaySec;
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
                RecordAndDisableForNetSession(go);
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
                RecordAndDisableForNetSession(go);
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
                RecordAndDisableForNetSession(go);
        }
    }

    void RecordAndDisableForNetSession(GameObject go)
    {
        if (go == null)
            return;
        if (go.activeSelf)
        {
            if (!_sceneRootDisabledForNet.Contains(go))
                _sceneRootDisabledForNet.Add(go);
        }
        go.SetActive(false);
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

        GameObject prefab = Resources.Load<GameObject>(MultiplayerNetPrefabsRegister.PublicObjectiveStateResourcesPath);
        GameObject go;
        if (prefab != null)
        {
            go = Instantiate(prefab);
            go.name = "PublicObjectiveStateRuntime";
        }
        else
        {
            Debug.LogWarning(
                "[MP] Falling back to runtime-built PublicObjectiveState (no Resources prefab). Pure clients may not replicate wave NetworkVariables.");
            go = new GameObject("PublicObjectiveStateRuntime");
            go.AddComponent<NetworkObject>();
            go.AddComponent<PublicObjectiveEventStateSimple>();
        }

        DontDestroyOnLoad(go);
        NetworkObject no = go.GetComponent<NetworkObject>();
        if (!no.IsSpawned)
            no.Spawn();
    }

    void EnsureChatRoomStateObject()
    {
        if (ChatRoomStateSimple.Instance != null)
        {
            NetworkObject existingNo = ChatRoomStateSimple.Instance.GetComponent<NetworkObject>();
            if (existingNo != null && !existingNo.IsSpawned)
                existingNo.Spawn();
            return;
        }

        GameObject prefab = Resources.Load<GameObject>(MultiplayerNetPrefabsRegister.ChatRoomStateResourcesPath);
        GameObject go;
        if (prefab != null)
        {
            go = Instantiate(prefab);
            go.name = "ChatRoomStateRuntime";
        }
        else
        {
            Debug.LogWarning(
                "[MP] Falling back to runtime-built ChatRoomState (no Resources prefab). Pure clients may not replicate room chat.");
            go = new GameObject("ChatRoomStateRuntime");
            go.AddComponent<NetworkObject>();
            go.AddComponent<ChatRoomStateSimple>();
        }

        DontDestroyOnLoad(go);
        NetworkObject no = go.GetComponent<NetworkObject>();
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

        GameObject prefab = Resources.Load<GameObject>(MultiplayerNetPrefabsRegister.PartyRuntimeStateResourcesPath);
        GameObject go;
        if (prefab != null)
        {
            go = Instantiate(prefab);
            go.name = "PartyRuntimeStateRuntime";
        }
        else
        {
            Debug.LogWarning(
                "[MP] Falling back to runtime-built PartyRuntimeState (no Resources prefab). Pure clients may not replicate party NetworkVariables.");
            go = new GameObject("PartyRuntimeStateRuntime");
            go.AddComponent<NetworkObject>();
            go.AddComponent<PartyRuntimeStateSimple>();
        }

        DontDestroyOnLoad(go);
        NetworkObject no = go.GetComponent<NetworkObject>();
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
        if (effectiveMode == StartupMode.Disabled)
            return mTag + kC + offN;
        NetworkManager nm = NetworkManager.Singleton;
        string hS = MultiplayerHudPick(t, c => c.hudMultiRoleHost, "host");
        string cS = MultiplayerHudPick(t, c => c.hudMultiRoleCli, "cli");
        string sS = MultiplayerHudPick(t, c => c.hudMultiRoleSrv, "srv");
        string aS = MultiplayerHudPick(t, c => c.hudMultiRoleAuto, "auto");
        string role = effectiveMode switch
        {
            StartupMode.Host => hS,
            StartupMode.Client => cS,
            StartupMode.DedicatedServer => sS,
            StartupMode.AutoFromCommandLine => aS,
            _ => offN
        };
        if (nm != null && nm.IsListening)
        {
            if (nm.IsHost) role = hS;
            else if (nm.IsServer) role = sS;
            else role = cS;
        }

        bool listening = nm != null && nm.IsListening;
        bool connectedClient = listening
            && nm != null
            && nm.IsClient
            && !nm.IsServer
            && nm.IsConnectedClient
            && nm.LocalClient != null
            && nm.LocalClient.PlayerObject != null;
        int n = listening && nm != null && nm.ConnectedClientsIds != null ? nm.ConnectedClientsIds.Count : 0;
        string ccK = MultiplayerHudPick(t, c => c.hudMultiCcKey, "cc");
        string g1 = MultiplayerHudKeep(t, c => c.hudMultiBlockGap, " ");
        string aSep = MultiplayerHudPick(t, c => c.hudMultiAddrPortSep, ":");
        string line = mTag + kC + role + g1 + ccK + kC + n + g1 + connectAddress + aSep + connectPort;

        string st = connectedClient ? "up" : (_clientReconnectArmed ? "reconnect" : (listening ? "listen" : "down"));
        line += g1 + "st:" + st;
        int dup = listening ? CountActiveScenePlayerRootsForNetRisk() : 0;
        if (listening)
            line += g1 + "dup:" + dup;
        if (nm != null && nm.IsHost)
        {
            bool hostGreen = IsHostEntryGreen(nm, dup);
            line += g1 + "hg:" + (hostGreen ? "1" : "0");
            if (!hostGreen)
                line += g1 + "hgr:" + BuildHostEntryFailReason(nm, dup);
        }
        if (_clientReconnectArmed && effectiveMode == StartupMode.Client)
            line += g1 + "rc:" + _clientReconnectAttempt + "/" + _reconnectMaxAttempts;
        if (_fatalBlocker)
        {
            line += g1 + "fatal:1";
            if (!string.IsNullOrWhiteSpace(_fatalReason))
                line += g1 + "why:" + _fatalReason;
        }
        else
        {
            line += g1 + "fatal:0";
        }
        if (_sessionStartedAtUnscaled > 0f)
            line += g1 + "up:" + FormatDurationHms(Mathf.Max(0f, Time.unscaledTime - _sessionStartedAtUnscaled));
        return line;
    }

    bool IsHostEntryGreen(NetworkManager nm, int dup)
    {
        if (nm == null || !nm.IsHost || !nm.IsListening)
            return false;
        if (dup > 0)
            return false;
        if (nm.LocalClient == null || nm.LocalClient.PlayerObject == null)
            return false;
        MultiplayerPlayerSimple mps = nm.LocalClient.PlayerObject.GetComponent<MultiplayerPlayerSimple>();
        return mps != null;
    }

    string BuildHostEntryFailReason(NetworkManager nm, int dup)
    {
        if (nm == null || !nm.IsHost || !nm.IsListening)
            return "not_host";
        if (dup > 0)
            return "dup";
        if (nm.LocalClient == null || nm.LocalClient.PlayerObject == null)
            return "no_player";
        MultiplayerPlayerSimple mps = nm.LocalClient.PlayerObject.GetComponent<MultiplayerPlayerSimple>();
        if (mps == null)
            return "no_mps";
        return "unknown";
    }

    int CountActiveScenePlayerRootsForNetRisk()
    {
        HashSet<GameObject> roots = new HashSet<GameObject>();
        var movers = FindObjectsByType<PlayerMoveSimple>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < movers.Length; i++)
        {
            PlayerMoveSimple mv = movers[i];
            if (mv == null || mv.GetComponent<MultiplayerPlayerSimple>() != null)
                continue;
            roots.Add(mv.transform.root.gameObject);
        }

        var healths = FindObjectsByType<PlayerHealthSimple>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < healths.Length; i++)
        {
            PlayerHealthSimple hp = healths[i];
            if (hp == null || hp.GetComponent<MultiplayerPlayerSimple>() != null)
                continue;
            roots.Add(hp.transform.root.gameObject);
        }
        return roots.Count;
    }

    static string FormatDurationHms(float sec)
    {
        int total = Mathf.Max(0, Mathf.FloorToInt(sec));
        int h = total / 3600;
        int m = (total % 3600) / 60;
        int s = total % 60;
        if (h > 0)
            return h.ToString("00") + ":" + m.ToString("00") + ":" + s.ToString("00");
        return m.ToString("00") + ":" + s.ToString("00");
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
