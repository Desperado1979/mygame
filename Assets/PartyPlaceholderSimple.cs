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
        if (Input.GetKeyDown(addMemberKey))
        {
            if (partySize >= maxPartySize)
                ServerAuditLogSimple.Push(ServerAuditLogSimple.CategorySrvValPartyReject, "op=add&reason=party_full");
            else
                partySize++;
        }
        if (Input.GetKeyDown(removeMemberKey))
        {
            if (partySize <= 1)
                ServerAuditLogSimple.Push(ServerAuditLogSimple.CategorySrvValPartyReject, "op=remove&reason=solo_floor");
            else
                partySize--;
        }
    }
}
