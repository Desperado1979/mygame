using UnityEngine;

/// <summary>
/// Shared connection settings for internal multiplayer sessions.
/// Create via: Assets/Create/EpochOfDawn/Multiplayer Connection Config
/// </summary>
[CreateAssetMenu(menuName = "EpochOfDawn/Multiplayer Connection Config", fileName = "MultiplayerConnectionConfig")]
public class MultiplayerConnectionConfigSimple : ScriptableObject
{
    public MultiplayerBootstrapSimple.StartupMode startupMode = MultiplayerBootstrapSimple.StartupMode.AutoFromCommandLine;
    public string connectAddress = "127.0.0.1";
    public ushort connectPort = 7777;
    public bool autoStartOnPlay = true;
    public bool disableScenePlayersWhenNetworkStarts = true;
}
