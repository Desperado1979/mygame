using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Minimal shared party state for playtests.
/// Works in NGO sessions and falls back gracefully in single-player.
/// </summary>
public class PartyRuntimeStateSimple : NetworkBehaviour
{
    public static PartyRuntimeStateSimple Instance { get; private set; }

    readonly NetworkVariable<int> memberCount = new NetworkVariable<int>(1);

    public int MemberCount => Mathf.Max(1, memberCount.Value);

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

    public void RequestAddMember()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            return;
        if (IsServer)
            ApplyAddMember();
        else
            AddMemberServerRpc();
    }

    public void RequestRemoveMember()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            return;
        if (IsServer)
            ApplyRemoveMember();
        else
            RemoveMemberServerRpc();
    }

    public void ServerSetMemberCount(int count)
    {
        if (!IsServer)
            return;
        memberCount.Value = Mathf.Max(1, count);
    }

    [ServerRpc(RequireOwnership = false)]
    void AddMemberServerRpc()
    {
        ApplyAddMember();
    }

    [ServerRpc(RequireOwnership = false)]
    void RemoveMemberServerRpc()
    {
        ApplyRemoveMember();
    }

    void ApplyAddMember()
    {
        memberCount.Value = Mathf.Max(1, memberCount.Value + 1);
    }

    void ApplyRemoveMember()
    {
        memberCount.Value = Mathf.Max(1, memberCount.Value - 1);
    }
}
