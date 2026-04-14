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

    [Header("D3 optional — XP / bag / Q&R mastery / gold / enhance")]
    public PlayerProgressSimple progress;
    public PlayerInventorySimple inventory;
    public PlayerSkillMasterySimple burstMastery;
    public PlayerSkillUnlockSimple unlocks;
    public PlayerWalletSimple wallet;
    public PlayerEnhanceSimple enhance;
    public PlayerHealthSimple health;
    public PlayerBankSimple bank;
    public PlayerAreaStateSimple areaState;
    public PlayerPvpSimple pvp;
    public D4FlowValidatorSimple d4Flow;
    public PartyPlaceholderSimple party;
    public ChatPlaceholderSimple chat;
    [Header("Week1 split: runtime vs debug")]
    public bool showDebugDetails = true;

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
                enemyCount++;
        }

        string pos = player == null ? "N/A" : $"{player.position.x:F1},{player.position.z:F1}";
        string flow = d4Flow == null
            ? ""
            : (d4Flow.Completed ? "Flow:OK" : $"Flow:S{d4Flow.CurrentStep}");

        string line = flow.Length > 0
            ? $"{flow}  E:{enemyCount}  P:{pos}"
            : $"E:{enemyCount}  P:{pos}";

        if (mp != null) line += $"  MP:{mp.CurrentMpRounded}/{mp.MaxMp}";
        if (health != null) line += $"  HP:{health.CurrentHp}/{health.maxHp}";
        if (areaState != null) line += $"  A:{areaState.currentArea}";
        if (pvp != null) line += $"  PvP:{(pvp.pvpEnabled ? "On" : "Off")}{(pvp.IsRedName ? "(Red)" : "")}";
        if (party != null) line += $"  Party:{party.partySize}/{party.maxPartySize}";
        if (party != null) line += party.shareDropWithParty ? "  Drop:Share" : "  Drop:Solo";

        if (burstSkill != null)
        {
            float cd = burstSkill.CooldownRemaining;
            line += cd > 0.01f ? $"  Q:{cd:F1}s" : "  Q:rdy";
        }

        if (frostSkill != null)
        {
            float cd = frostSkill.CooldownRemaining;
            line += cd > 0.01f ? $"  R:{cd:F1}s" : "  R:rdy";
        }

        if (showDebugDetails && player != null)
        {
            EnemyStatusEffectsSimple near = FindNearestEnemyStatus(player.position, 18f);
            string st = near == null ? "" : near.GetHudSummary();
            if (st.Length > 0) line += $"  [{st}]";
        }

        if (showDebugDetails && equipDebug != null && equipDebug.testArmor != null)
        {
            EquipmentDataSimple a = equipDebug.testArmor;
            int ps = equipDebug.playerStrengthForTest;
            bool ok = equipDebug.CanEquipTestArmor();
            line += $"  Eq:{ps}/{a.requiredStrength}{(ok ? "+" : "X")}";
        }

        if (progress != null)
        {
            int need = progress.XpNeededThisLevel();
            line += $"  Lv{progress.level} {progress.xpIntoCurrentLevel}/{need}";
            line += $"  XP:{progress.xpBank}";
            line += $"  SP:{progress.skillUnlockPoints}";
        }

        if (inventory != null)
        {
            line += $"  W:{Mathf.CeilToInt(inventory.CurrentWeight)}/{Mathf.CeilToInt(inventory.maxCarryWeight)}";
            line += $"  H:{inventory.HpPotionCount}";
            line += $"  M:{inventory.MpPotionCount}";
        }

        if (showDebugDetails && burstMastery != null)
        {
            line += $"  q{burstMastery.burstSkillLevel}";
            line += $"  r{burstMastery.frostSkillLevel}";
        }
        if (showDebugDetails && unlocks != null)
            line += $"  Qt{unlocks.burstTier} Rt{unlocks.frostTier}";

        if (wallet != null) line += $"  G:{wallet.Gold}";
        if (bank != null) line += $"  BG:{bank.bankGold} BH:{bank.bankHpPotion} BM:{bank.bankMpPotion}";
        if (showDebugDetails && enhance != null) line += $"  +{enhance.enhanceStep} DR:{enhance.DamageReductionFraction * 100f:F0}%";
        if (showDebugDetails && progress != null) line += "  U升 I点 O解Q2 P解R2 1红 2蓝 B买红 N买蓝 V卖药";
        if (showDebugDetails && bank != null) line += "  F5存金 F6取金 F7存药 F8取药";
        if (showDebugDetails && party != null) line += "  =加队 -减队";
        if (showDebugDetails && chat != null) line += "  Enter本地 `系统";
        if (showDebugDetails) line += "  F4导审计 F9存档 F10读档 F11清档 F12导JSON";
        if (showDebugDetails && health != null) line += "  死亡掉金并复活";

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
