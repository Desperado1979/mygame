using UnityEngine;

/// <summary>D4 / B2·Day3：队伍占位 — 人数与掉落共享与 <see cref="PartyRuntimeStateSimple"/> 对齐（ NGO 全端一致）。</summary>
public class PartyPlaceholderSimple : MonoBehaviour
{
    public int partySize = 1;
    public int maxPartySize = 5;
    public bool shareDropWithParty = true;
    public KeyCode addMemberKey = KeyCode.KeypadPlus;
    public KeyCode removeMemberKey = KeyCode.KeypadMinus;
    [Tooltip("切换 Drop Share/Solo（仅本地操控体；联网时写服端 NetworkVariable）")]
    public KeyCode toggleShareDropKey = KeyCode.KeypadMultiply;

    void Awake()
    {
        maxPartySize = Mathf.Max(1, D3GrowthBalance.Load().partyMaxMembers);
    }

    void Update()
    {
        SyncFromRuntimeState();
        // Host 上会同时存在「本地玩家」与「远端玩家的代理体」；两者都挂本组件时会各读一次键盘 → +2/-2。仅本地操控体处理按键。
        MultiplayerPlayerSimple net = GetComponent<MultiplayerPlayerSimple>();
        if (net != null && net.IsSpawned && !net.IsOwner)
            return;
        // 本机双开两 exe 且都可能收到输入时，仅前台窗口处理（与 IsOwner 叠加）。
        if (!Application.isFocused)
            return;
        if (Input.GetKeyDown(toggleShareDropKey))
        {
            PartyRuntimeStateSimple stShare = PartyRuntimeStateSimple.Instance;
            if (stShare != null)
                stShare.RequestToggleShareDrop();
            else
                shareDropWithParty = !shareDropWithParty;
        }
        if (Input.GetKeyDown(addMemberKey))
        {
            if (partySize >= maxPartySize)
                ServerAuditLogSimple.Push(ServerAuditLogSimple.CategorySrvValPartyReject, "op=add&reason=party_full");
            else if (!TryAddMember())
                ServerAuditLogSimple.Push(ServerAuditLogSimple.CategorySrvValPartyReject, "op=add&reason=runtime_reject");
        }
        if (Input.GetKeyDown(removeMemberKey))
        {
            if (partySize <= 1)
                ServerAuditLogSimple.Push(ServerAuditLogSimple.CategorySrvValPartyReject, "op=remove&reason=solo_floor");
            else if (!TryRemoveMember())
                ServerAuditLogSimple.Push(ServerAuditLogSimple.CategorySrvValPartyReject, "op=remove&reason=runtime_reject");
        }
    }

    bool TryAddMember()
    {
        PartyRuntimeStateSimple st = PartyRuntimeStateSimple.Instance;
        if (st == null)
        {
            if (partySize >= maxPartySize)
                return false;
            partySize++;
            return true;
        }

        int cap = Mathf.Min(maxPartySize, PartyRuntimeStateSimple.DefaultMaxPartyMembers);
        if (st.MemberCount >= cap)
            return false;

        st.RequestAddMember();
        return true;
    }

    bool TryRemoveMember()
    {
        PartyRuntimeStateSimple st = PartyRuntimeStateSimple.Instance;
        if (st == null)
        {
            partySize--;
            return true;
        }

        if (st.MemberCount <= 1)
            return false;

        st.RequestRemoveMember();
        return true;
    }

    void SyncFromRuntimeState()
    {
        PartyRuntimeStateSimple st = PartyRuntimeStateSimple.Instance;
        if (st != null)
        {
            partySize = Mathf.Clamp(
                st.MemberCount,
                1,
                Mathf.Min(maxPartySize, PartyRuntimeStateSimple.DefaultMaxPartyMembers));
            shareDropWithParty = st.ShareDropWithParty;
        }
    }
}
