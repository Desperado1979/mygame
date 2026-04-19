using System.Text;
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
    public PlayerStateExportSimple stateExport;
    public P1A1QuestState p1A1Quest;
    public WaveSpawnerSimple waveSpawner;
    [Header("Week1 split: runtime vs debug")]
    public bool showDebugDetails = true;

    void Start()
    {
        if (player != null)
        {
            if (stateExport == null)
                stateExport = player.GetComponent<PlayerStateExportSimple>();
            if (p1A1Quest == null)
                p1A1Quest = player.GetComponent<P1A1QuestState>();
            if (progress == null)
                progress = player.GetComponent<PlayerProgressSimple>();
        }
        EnsureWaveSpawnerRef();
    }

    void EnsureWaveSpawnerRef()
    {
        if (waveSpawner != null)
            return;
        // true = 含未激活物体；否则场景里关了 P1A_WaveSpawner 就永远找不到
        waveSpawner = FindObjectOfType<WaveSpawnerSimple>(true);
    }

    void Update()
    {
        if (hudText == null) return;
        EnsureWaveSpawnerRef();

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

        var line = new StringBuilder(512);
        if (flow.Length > 0)
            line.Append($"{flow}  ");
        line.Append($"E:{enemyCount}  P:{pos}");

        if (mp != null) line.Append($"  MP:{mp.CurrentMpRounded}/{mp.MaxMp}");
        if (health != null) line.Append($"  HP:{health.CurrentHp}/{health.maxHp}");
        // P1 / 波次 提前，便于左上角 HUD 首屏可见
        if (p1A1Quest != null)
        {
            if (p1A1Quest.IsComplete)
                line.Append("  P1:OK");
            else
            {
                line.Append($"  P1:{p1A1Quest.compliantKills}/{p1A1Quest.targetKills}");
                if (p1A1Quest.showOrderHintInHud)
                    line.Append($" tip:{p1A1Quest.OrderHintShort}");
            }
        }
        if (waveSpawner != null && !waveSpawner.allWavesComplete)
            line.Append($"  Wv:{waveSpawner.currentWaveIndex + 1}/{Mathf.Max(1, waveSpawner.TotalWaveCount)}");
        else if (waveSpawner != null && waveSpawner.allWavesComplete)
            line.Append("  Wv:Done");
        else
            line.Append("  Wv:--");

        line.Append('\n');

        if (areaState != null) line.Append($"A:{areaState.currentArea}  ");
        if (pvp != null) line.Append($"  PvP:{(pvp.pvpEnabled ? "On" : "Off")}{(pvp.IsRedName ? "(Red)" : "")}");
        if (party != null) line.Append($"  Party:{party.partySize}/{party.maxPartySize}");
        if (party != null) line.Append(party.shareDropWithParty ? "  Drop:Share" : "  Drop:Solo");

        if (burstSkill != null)
        {
            float cd = burstSkill.CooldownRemaining;
            line.Append(cd > 0.01f ? $"  Q:{cd:F1}s" : "  Q:rdy");
        }

        if (frostSkill != null)
        {
            float cd = frostSkill.CooldownRemaining;
            line.Append(cd > 0.01f ? $"  R:{cd:F1}s" : "  R:rdy");
        }

        if (showDebugDetails && player != null)
        {
            EnemyStatusEffectsSimple near = FindNearestEnemyStatus(player.position, 18f);
            string st = near == null ? "" : near.GetHudSummary();
            if (st.Length > 0) line.Append($"  [{st}]");
        }

        if (showDebugDetails && equipDebug != null && equipDebug.testArmor != null)
        {
            EquipmentDataSimple a = equipDebug.testArmor;
            int ps = equipDebug.playerStrengthForTest;
            bool ok = equipDebug.CanEquipTestArmor();
            line.Append($"  Eq:{ps}/{a.requiredStrength}{(ok ? "+" : "X")}");
        }

        if (progress != null)
        {
            int need = progress.XpNeededThisLevel();
            int remain = Mathf.Max(0, need - progress.xpIntoCurrentLevel);
            line.Append($"  Lv{progress.level} {progress.xpIntoCurrentLevel}/{need} 差{remain}  SP:{progress.skillUnlockPoints}");
            line.Append($"  XP池:{progress.xpBank}");
        }

        if (inventory != null)
        {
            line.Append($"  W:{Mathf.CeilToInt(inventory.CurrentWeight)}/{Mathf.CeilToInt(inventory.maxCarryWeight)}");
            line.Append($"  H:{inventory.HpPotionCount}");
            line.Append($"  M:{inventory.MpPotionCount}");
        }

        if (showDebugDetails && burstMastery != null)
        {
            line.Append($"  q{burstMastery.burstSkillLevel}");
            line.Append($"  r{burstMastery.frostSkillLevel}");
        }
        if (showDebugDetails && unlocks != null)
            line.Append($"  Qt{unlocks.burstTier} Rt{unlocks.frostTier}");

        if (wallet != null) line.Append($"  G:{wallet.Gold}");
        if (bank != null) line.Append($"  BG:{bank.bankGold} BH:{bank.bankHpPotion} BM:{bank.bankMpPotion}");
        if (showDebugDetails && enhance != null) line.Append($"  +{enhance.enhanceStep} DR:{enhance.DamageReductionFraction * 100f:F0}%");
        if (showDebugDetails && progress != null) line.Append("  U升 I点 O解Q2 P解R2 1红 2蓝 B买红 N买蓝 V卖药");
        if (showDebugDetails && bank != null) line.Append("  F5存金 F6取金 F7存药 F8取药");
        if (showDebugDetails && party != null) line.Append("  小键盘+/-队伍");
        if (showDebugDetails && chat != null) line.Append("  Enter本地 `系统");
        if (stateExport != null)
        {
            line.Append($"  SrvVal:L{stateExport.LastSyncWarnLow}H{stateExport.LastSyncWarnHigh}");
            if (stateExport.LastSyncValidationOk.HasValue)
                line.Append(stateExport.LastSyncValidationOk.Value ? " val:ok" : " val:no");
            if (!string.IsNullOrEmpty(stateExport.LastAuditCategoryPreview))
                line.Append($" {stateExport.LastAuditCategoryPreview}");
            if (!string.IsNullOrEmpty(stateExport.LastWarningsCodesPreview))
                line.Append($" codes:{stateExport.LastWarningsCodesPreview}");
            if (stateExport.LastNetAlertHigh)
                line.Append(" NET:!");
            if (!string.IsNullOrEmpty(stateExport.LastHealthProbePreview))
                line.Append($" HLT:{stateExport.LastHealthProbePreview}");
            if (!string.IsNullOrEmpty(stateExport.LastMetricsAuditCategoriesPreview))
                line.Append($" AudC:{stateExport.LastMetricsAuditCategoriesPreview}");
            if (!string.IsNullOrEmpty(stateExport.LastSyncPostStatusTag))
                line.Append($" syn:{stateExport.LastSyncPostStatusTag}");
            if (stateExport.LastSyncDurationMs >= 0)
                line.Append($" d:{stateExport.LastSyncDurationMs}ms");
            if (stateExport.LastSyncRetryCount > 0)
                line.Append($" r:{stateExport.LastSyncRetryCount}");
        }
        if (showDebugDetails) line.Append("  F1健康 ;审计类 F4审计 F3整包 F2POST F12状态 F9存 F10读 F11清");
        if (showDebugDetails && health != null) line.Append("  死亡掉金并复活");

        hudText.text = line.ToString();
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
