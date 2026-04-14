using UnityEngine;

/// <summary>D4 kickoff: local party placeholder (fake members).</summary>
public class PartyPlaceholderSimple : MonoBehaviour
{
    public int partySize = 1;
    public int maxPartySize = 5;
    public bool shareDropWithParty = true;
    public KeyCode addMemberKey = KeyCode.Equals;
    public KeyCode removeMemberKey = KeyCode.Minus;

    void Update()
    {
        if (Input.GetKeyDown(addMemberKey))
            partySize = Mathf.Clamp(partySize + 1, 1, maxPartySize);
        if (Input.GetKeyDown(removeMemberKey))
            partySize = Mathf.Clamp(partySize - 1, 1, maxPartySize);
    }
}
