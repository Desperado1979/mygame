using UnityEngine;

/// <summary>D4 kickoff: local party placeholder (fake members).</summary>
public class PartyPlaceholderSimple : MonoBehaviour
{
    public int partySize = 1;
    public int maxPartySize = 5;
    public bool shareDropWithParty = true;
    public KeyCode addMemberKey = KeyCode.KeypadPlus;
    public KeyCode removeMemberKey = KeyCode.KeypadMinus;

    void Update()
    {
        SyncFromRuntimeState();
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
            partySize++;
            return true;
        }

        if (st.MemberCount >= maxPartySize)
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
            partySize = Mathf.Clamp(st.MemberCount, 1, Mathf.Max(1, maxPartySize));
    }
}
