using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Minimal shared party state for playtests (B2·Day3：全端同显人数与掉落共享开关)。
/// Works in NGO sessions and falls back gracefully in single-player.
/// </summary>
public class PartyRuntimeStateSimple : NetworkBehaviour
{
    public static PartyRuntimeStateSimple Instance { get; private set; }

    /// <summary>与 <c>DefaultD3Growth.partyMaxMembers</c> 对齐；服端硬上限。</summary>
    public static int DefaultMaxPartyMembers => Mathf.Max(1, D3GrowthBalance.Load().partyMaxMembers);

    readonly NetworkVariable<int> memberCount = new NetworkVariable<int>(1);
    readonly NetworkVariable<bool> shareDropWithParty = new NetworkVariable<bool>(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public int MemberCount => Mathf.Max(1, memberCount.Value);

    public bool ShareDropWithParty => shareDropWithParty.Value;

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
        memberCount.Value = Mathf.Clamp(count, 1, DefaultMaxPartyMembers);
    }

    public void RequestToggleShareDrop()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            return;
        if (IsServer)
            shareDropWithParty.Value = !shareDropWithParty.Value;
        else
            ToggleShareDropServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    void ToggleShareDropServerRpc()
    {
        shareDropWithParty.Value = !shareDropWithParty.Value;
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
        if (memberCount.Value >= DefaultMaxPartyMembers)
            return;
        memberCount.Value = memberCount.Value + 1;
    }

    void ApplyRemoveMember()
    {
        memberCount.Value = Mathf.Max(1, memberCount.Value - 1);
    }
}
