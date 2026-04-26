using UnityEngine;
using Unity.Netcode;

/// <summary>D3: 用 SP 解锁技能二阶占位。O=解锁Q2，P=解锁R2。</summary>
public class PlayerSkillUnlockSimple : MonoBehaviour
{
    public PlayerProgressSimple progress;
    PlayerHotkeysSimple hotkeys;

    public int burstTier = 1;
    public int frostTier = 1;

    public int burstTier2Cost = 2;
    public int frostTier2Cost = 2;

    void Reset()
    {
        progress = GetComponent<PlayerProgressSimple>();
    }

    void Update()
    {
        // Netcode: in multiplayer, unlock input must be validated and executed on server.
        MultiplayerPlayerSimple netPlayer = GetComponent<MultiplayerPlayerSimple>();
        if (netPlayer != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.IsServer)
            return;

        if (hotkeys == null)
            hotkeys = GetComponent<PlayerHotkeysSimple>();

        KeyCode q2 = hotkeys != null ? hotkeys.unlockQ2 : KeyCode.O;
        KeyCode r2 = hotkeys != null ? hotkeys.unlockR2 : KeyCode.P;

        if (Input.GetKeyDown(q2))
            UnlockBurstTier2();
        if (Input.GetKeyDown(r2))
            UnlockFrostTier2();
    }

    public bool UnlockBurstTier2()
    {
        if (burstTier >= 2) return false;
        if (progress == null) progress = GetComponent<PlayerProgressSimple>();
        if (progress == null)
        {
            ServerAuditLogSimple.Push(
                ServerAuditLogSimple.CategorySrvValUnlockReject,
                "skill=burst_tier2&reason=no_progress");
            return false;
        }
        if (progress.skillUnlockPoints < burstTier2Cost)
        {
            ServerAuditLogSimple.Push(
                ServerAuditLogSimple.CategorySrvValUnlockReject,
                $"skill=burst_tier2&needSp={burstTier2Cost}&haveSp={progress.skillUnlockPoints}");
            return false;
        }

        progress.skillUnlockPoints -= burstTier2Cost;
        burstTier = 2;
        Debug.Log("Q 二阶已解锁");
        return true;
    }

    public bool UnlockFrostTier2()
    {
        if (frostTier >= 2) return false;
        if (progress == null) progress = GetComponent<PlayerProgressSimple>();
        if (progress == null)
        {
            ServerAuditLogSimple.Push(
                ServerAuditLogSimple.CategorySrvValUnlockReject,
                "skill=frost_tier2&reason=no_progress");
            return false;
        }
        if (progress.skillUnlockPoints < frostTier2Cost)
        {
            ServerAuditLogSimple.Push(
                ServerAuditLogSimple.CategorySrvValUnlockReject,
                $"skill=frost_tier2&needSp={frostTier2Cost}&haveSp={progress.skillUnlockPoints}");
            return false;
        }

        progress.skillUnlockPoints -= frostTier2Cost;
        frostTier = 2;
        Debug.Log("R 二阶已解锁");
        return true;
    }
}
