using UnityEngine;
using UnityEngine.UI;

public class DebugHudSimple : MonoBehaviour
{
    public Transform player;
    public Text hudText;

    [Header("D2-1 optional — skill bar placeholder")]
    public PlayerMpSimple mp;
    public PlayerSkillBurstSimple burstSkill;

    [Header("D2-2 optional — frost + status readout")]
    public PlayerSkillFrostSimple frostSkill;

    [Header("D2-3 optional — equipment placeholder")]
    public PlayerEquipmentDebugSimple equipDebug;

    void Update()
    {
        if (hudText == null) return;

        int enemyCount = 0;
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        for (int i = 0; i < allObjects.Length; i++)
        {
            GameObject obj = allObjects[i];
            if (!obj.activeInHierarchy) continue;
            if (obj.CompareTag("Enemy") || obj.name.Contains("Enemy"))
            {
                enemyCount++;
            }
        }

        string pos = player == null
            ? "N/A"
            : $"{player.position.x:F1},{player.position.z:F1}";

        string line = $"E:{enemyCount}  P:{pos}";

        if (mp != null)
            line += $"  MP:{mp.CurrentMpRounded}/{mp.MaxMp}";

        if (burstSkill != null)
        {
            float cd = burstSkill.CooldownRemaining;
            line += cd > 0.01f
                ? $"  Q:{cd:F1}s"
                : "  Q:rdy";
        }

        if (frostSkill != null)
        {
            float cd = frostSkill.CooldownRemaining;
            line += cd > 0.01f
                ? $"  R:{cd:F1}s"
                : "  R:rdy";
        }

        if (player != null)
        {
            EnemyStatusEffectsSimple near = FindNearestEnemyStatus(player.position, 18f);
            string st = near == null ? "" : near.GetHudSummary();
            if (st.Length > 0)
                line += $"  [{st}]";
        }

        if (equipDebug != null && equipDebug.testArmor != null)
        {
            EquipmentDataSimple a = equipDebug.testArmor;
            int ps = equipDebug.playerStrengthForTest;
            bool ok = equipDebug.CanEquipTestArmor();
            line += $"  Eq:{ps}/{a.requiredStrength}{(ok ? "+" : "X")}";
        }

        hudText.text = line;
    }

    static EnemyStatusEffectsSimple FindNearestEnemyStatus(Vector3 from, float maxDist)
    {
        EnemyStatusEffectsSimple[] all = FindObjectsOfType<EnemyStatusEffectsSimple>();
        EnemyStatusEffectsSimple best = null;
        float bestSq = maxDist * maxDist;
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null || !all[i].gameObject.activeInHierarchy) continue;
            float sq = (all[i].transform.position - from).sqrMagnitude;
            if (sq < bestSq)
            {
                bestSq = sq;
                best = all[i];
            }
        }

        return best;
    }
}
