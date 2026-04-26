using UnityEngine;

/// <summary>D2-2: Applies Freeze in radius (no direct hit damage) — separate from Q burst.</summary>
public class PlayerSkillFrostSimple : MonoBehaviour
{
    public PlayerMpSimple mp;
    public PlayerSkillMasterySimple mastery;
    public PlayerSkillUnlockSimple unlocks;
    public LayerMask enemyLayer;

    public KeyCode skillKey = KeyCode.R;
    public string skillId = "FrostPulse";
    public int mpCost = 12;
    public float cooldownSeconds = 3.5f;
    public float skillRadius = 2.8f;
    public float freezeDurationSeconds = 2.2f;

    float cooldownEndTime;

    void Reset()
    {
        mp = GetComponent<PlayerMpSimple>();
        mastery = GetComponent<PlayerSkillMasterySimple>();
        unlocks = GetComponent<PlayerSkillUnlockSimple>();
    }

    void Update()
    {
        MultiplayerPlayerSimple net = GetComponent<MultiplayerPlayerSimple>();
        if (net != null)
            return; // Multiplayer path is handled by MultiplayerPlayerSimple server RPC.

        if (!Input.GetKeyDown(skillKey))
            return;

        if (unlocks == null)
            unlocks = GetComponent<PlayerSkillUnlockSimple>();

        if (Time.time < cooldownEndTime)
        {
            ServerAuditLogSimple.Push(
                ServerAuditLogSimple.CategorySrvValIllegalOperation,
                $"skillId={skillId}&reason=cooldown&remainSec={CooldownRemaining:F2}");
            Debug.Log($"{skillId} on cooldown ({CooldownRemaining:F1}s)");
            return;
        }

        if (mp == null)
        {
            Debug.LogWarning($"{nameof(PlayerSkillFrostSimple)}: assign PlayerMpSimple.");
            return;
        }

        if (!mp.TrySpend(mpCost))
        {
            ServerAuditLogSimple.Push(
                ServerAuditLogSimple.CategorySrvValIllegalOperation,
                $"skillId={skillId}&reason=mp_insufficient&needMp={mpCost}&haveMp={mp.CurrentMpRounded}");
            Debug.Log("Not enough MP");
            return;
        }

        cooldownEndTime = Time.time + cooldownSeconds;

        if (mastery == null)
            mastery = GetComponent<PlayerSkillMasterySimple>();
        if (mastery != null)
            mastery.RegisterFrostCast();

        int tier = unlocks != null ? unlocks.frostTier : 1;
        float tierFreezeMul = tier >= 2 ? 1.35f : 1f;
        float tierRadiusMul = tier >= 2 ? 1.12f : 1f;
        float freezeSec = freezeDurationSeconds * tierFreezeMul;
        if (mastery != null)
            freezeSec *= mastery.FrostFreezeDurationMultiplier;

        Collider[] hits = Physics.OverlapSphere(transform.position, skillRadius * tierRadiusMul, enemyLayer);
        int applied = 0;
        for (int i = 0; i < hits.Length; i++)
        {
            EnemyStatusEffectsSimple status = hits[i].GetComponent<EnemyStatusEffectsSimple>();
            if (status == null) continue;
            status.ApplyFreeze(freezeSec);
            MonsterP1A1Mark p1 = hits[i].GetComponent<MonsterP1A1Mark>();
            if (p1 != null)
                p1.RegisterFreeze();
            applied++;
        }

        Debug.Log($"{skillId} T{tier} — frozen {applied} / colliders {hits.Length}");
    }

    public float CooldownRemaining => Mathf.Max(0f, cooldownEndTime - Time.time);

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.85f, 1f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, skillRadius);
    }
}
