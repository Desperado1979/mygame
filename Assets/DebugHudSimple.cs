using System;
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

    /** 玩测：避免每帧全场景遍历；约 3Hz 刷新 E: 计数。 */
    const float HudEnemyCountInterval = 0.33f;
    float _hudEnemyCountNextAt;
    int _hudEnemyCountCache;
    const float HudNearEnemyStatusInterval = 0.28f;
    float _hudNearEnemyNextAt;
    EnemyStatusEffectsSimple _hudNearEnemyCache;
    const float PublicObjectiveJoinRadiusScale = 2.2f;
    readonly StringBuilder _hudLine = new StringBuilder(1024);
    P1AContentConfig _p1Table;

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
        _p1Table = P1AContentConfig.TryLoadDefault();
        EnsureWaveSpawnerRef();
    }

    void EnsureWaveSpawnerRef()
    {
        if (waveSpawner != null)
            return;
        // true = 含未激活物体；否则场景里关了 P1A_WaveSpawner 就永远找不到
        waveSpawner = FindObjectOfType<WaveSpawnerSimple>(true);
    }

    void EnsurePlayerRefs()
    {
        if (player == null)
        {
            MultiplayerPlayerSimple owner = FindOwnerPlayer();
            if (owner != null)
                player = owner.transform;
            else if (PlayerHealthSimple.Instance != null)
                player = PlayerHealthSimple.Instance.transform;
        }

        if (player == null)
            return;

        if (health == null || health.transform != player)
            health = player.GetComponent<PlayerHealthSimple>();
        if (mp == null || mp.transform != player)
            mp = player.GetComponent<PlayerMpSimple>();
        if (stateExport == null || stateExport.transform != player)
            stateExport = player.GetComponent<PlayerStateExportSimple>();
        if (progress == null || progress.transform != player)
            progress = player.GetComponent<PlayerProgressSimple>();
        if (inventory == null || inventory.transform != player)
            inventory = player.GetComponent<PlayerInventorySimple>();
        if (wallet == null || wallet.transform != player)
            wallet = player.GetComponent<PlayerWalletSimple>();
        if (enhance == null || enhance.transform != player)
            enhance = player.GetComponent<PlayerEnhanceSimple>();
        if (bank == null || bank.transform != player)
            bank = player.GetComponent<PlayerBankSimple>();
        if (unlocks == null || unlocks.transform != player)
            unlocks = player.GetComponent<PlayerSkillUnlockSimple>();
        if (burstMastery == null || burstMastery.transform != player)
            burstMastery = player.GetComponent<PlayerSkillMasterySimple>();
        if (p1A1Quest == null || p1A1Quest.transform != player)
            p1A1Quest = player.GetComponent<P1A1QuestState>();
        if (areaState == null || areaState.transform != player)
            areaState = player.GetComponent<PlayerAreaStateSimple>();
        if (pvp == null || pvp.transform != player)
            pvp = player.GetComponent<PlayerPvpSimple>();
        if (party == null || party.transform != player)
            party = player.GetComponent<PartyPlaceholderSimple>();
        if (chat == null || chat.transform != player)
            chat = player.GetComponent<ChatPlaceholderSimple>();
        if (burstSkill == null || burstSkill.transform != player)
            burstSkill = player.GetComponent<PlayerSkillBurstSimple>();
        if (frostSkill == null || frostSkill.transform != player)
            frostSkill = player.GetComponent<PlayerSkillFrostSimple>();
    }

    static MultiplayerPlayerSimple FindOwnerPlayer()
    {
        MultiplayerPlayerSimple[] arr = FindObjectsByType<MultiplayerPlayerSimple>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < arr.Length; i++)
        {
            if (arr[i] != null && arr[i].IsOwner)
                return arr[i];
        }
        return null;
    }

    static string P1aHudString(P1AContentConfig t, Func<P1AContentConfig, string> pick, string fallback)
    {
        if (t == null) return fallback;
        string s = pick(t);
        if (string.IsNullOrWhiteSpace(s)) return fallback;
        return s.Trim();
    }

    /// <summary>整段 HUD 文本（可含前导空格），仅空则回退，不做 Trim。</summary>
    static string P1aHudStringKeepWhitespace(P1AContentConfig t, Func<P1AContentConfig, string> pick, string fallback)
    {
        if (t == null) return fallback;
        string s = pick(t);
        if (string.IsNullOrWhiteSpace(s)) return fallback;
        return s;
    }

    static void AppendHudLines(StringBuilder line, string[] arr, int limit)
    {
        if (line == null || arr == null || limit <= 0) return;
        for (int i = 0; i < limit && i < arr.Length; i++)
        {
            string s = arr[i];
            if (string.IsNullOrWhiteSpace(s)) continue;
            line.Append('\n').Append(s);
        }
    }

    void Update()
    {
        if (hudText == null) return;
        EnsureWaveSpawnerRef();
        EnsurePlayerRefs();

        float enemyCountItv = (_p1Table != null && _p1Table.hudEnemyCountIntervalSec > 0.01f)
            ? _p1Table.hudEnemyCountIntervalSec
            : HudEnemyCountInterval;
        float nearEnemyItv = (_p1Table != null && _p1Table.hudNearEnemyStatusIntervalSec > 0.01f)
            ? _p1Table.hudNearEnemyStatusIntervalSec
            : HudNearEnemyStatusInterval;
        float nearEnemyRadius = (_p1Table != null && _p1Table.hudNearEnemyScanRadius > 0.5f)
            ? _p1Table.hudNearEnemyScanRadius
            : 18f;
        int posDp = _p1Table != null ? Mathf.Clamp(_p1Table.hudPosDecimals, 0, 3) : 1;
        int cdDp = _p1Table != null ? Mathf.Clamp(_p1Table.hudCdDecimals, 0, 3) : 1;
        int drDp = _p1Table != null ? Mathf.Clamp(_p1Table.hudDrPercentDecimals, 0, 2) : 0;
        int xpDp = _p1Table != null ? Mathf.Clamp(_p1Table.hudXpProgressDecimals, 0, 4) : 0;
        int wDp = _p1Table != null ? Mathf.Clamp(_p1Table.hudWeightDecimals, 0, 4) : 0;
        int d5Dp = _p1Table != null ? Mathf.Clamp(_p1Table.hudD5DurationDecimals, 0, 4) : 0;
        string posFmt = "F" + posDp;
        string cdFmt = "F" + cdDp;
        string drFmt = "F" + drDp;
        string xpFmt = "F" + xpDp;
        string wFmt = "F" + wDp;
        string d5Fmt = "F" + d5Dp;

        if (Time.unscaledTime >= _hudEnemyCountNextAt)
        {
            _hudEnemyCountNextAt = Time.unscaledTime + enemyCountItv;
            _hudEnemyCountCache = EnemyStatusEffectsSimple.HudLivingApproxCount;
        }

        string posNa = P1aHudString(_p1Table, t => t.hudTagPosNA, "N/A");
        string pos = player == null
            ? posNa
            : player.position.x.ToString(posFmt) + "," + player.position.z.ToString(posFmt);

        string kv = P1aHudString(_p1Table, t => t.hudPunctLabelColon, ":");
        string wide = P1aHudStringKeepWhitespace(_p1Table, t => t.hudPunctDebugWideSpace, "  ");
        string d5sp = P1aHudStringKeepWhitespace(_p1Table, t => t.hudPunctD5BannerSpace, " ");
        string sl = P1aHudString(_p1Table, t => t.hudPunctStatSlash, "/");
        string ellip = P1aHudString(_p1Table, t => t.hudPunctEllipsis, "…");
        string pvpPo = P1aHudString(_p1Table, t => t.hudPunctPvpParenOpen, "(");
        string pvpPc = P1aHudString(_p1Table, t => t.hudPunctPvpParenClose, ")");
        string nel = P1aHudString(_p1Table, t => t.hudPunctNearEnemyL, "[");
        string ner = P1aHudString(_p1Table, t => t.hudPunctNearEnemyR, "]");
        string tipC = P1aHudString(_p1Table, t => t.hudPunctTipColon, kv);
        int errMax = (_p1Table != null && _p1Table.hudTruncErrCodeMax > 0) ? _p1Table.hudTruncErrCodeMax : 36;
        int errKeep = (_p1Table != null && _p1Table.hudTruncErrCodeKeep > 0) ? _p1Table.hudTruncErrCodeKeep : 33;
        int dtlMax = (_p1Table != null && _p1Table.hudTruncDetailMax > 0) ? _p1Table.hudTruncDetailMax : 56;
        int dtlKeep = (_p1Table != null && _p1Table.hudTruncDetailKeep > 0) ? _p1Table.hudTruncDetailKeep : 53;

        string fTag = P1aHudString(_p1Table, t => t.hudTagFlow, "Flow");
        string fOk = P1aHudString(_p1Table, t => t.hudFlowCompleteSuffix, "OK");
        string fSp = P1aHudString(_p1Table, t => t.hudFlowStepPrefix, "S");
        string flow = d4Flow == null
            ? ""
            : (d4Flow.Completed ? $"{fTag}:{fOk}" : $"{fTag}:{fSp}{d4Flow.CurrentStep}");

        string eTag = P1aHudString(_p1Table, t => t.hudTagEnemy, "E");
        string pTag = P1aHudString(_p1Table, t => t.hudTagPos, "P");
        StringBuilder line = _hudLine;
        line.Clear();
        if (flow.Length > 0)
            line.Append(flow).Append(wide);
        line.Append(eTag).Append(kv).Append(_hudEnemyCountCache).Append(wide).Append(pTag).Append(kv).Append(pos);

        string mpTag = P1aHudString(_p1Table, t => t.hudTagMp, "MP");
        string hpTag = P1aHudString(_p1Table, t => t.hudTagHp, "HP");
        if (mp != null) line.Append("  ").Append(mpTag).Append(kv).Append(mp.CurrentMpRounded).Append(sl).Append(mp.MaxMp);
        if (health != null) line.Append("  ").Append(hpTag).Append(kv).Append(health.CurrentHp).Append(sl).Append(health.maxHp);
        // P1 / 波次 提前，便于左上角 HUD 首屏可见
        if (p1A1Quest != null)
        {
            string p1t = P1aHudString(_p1Table, t => t.p1HudTag, "P1");
            string p1ok = P1aHudString(_p1Table, t => t.p1HudCompleteSuffix, "OK");
            string tipl = P1aHudString(_p1Table, t => t.p1HudTipLabel, "tip");
            if (p1A1Quest.IsComplete)
                line.Append("  ").Append(p1t).Append(kv).Append(p1ok);
            else
            {
                line.Append("  ").Append(p1t).Append(kv).Append(p1A1Quest.compliantKills).Append(sl).Append(p1A1Quest.targetKills);
                if (p1A1Quest.showOrderHintInHud)
                    line.Append(' ').Append(tipl).Append(tipC).Append(p1A1Quest.OrderHintShort);
            }
        }
        if (_p1Table == null || _p1Table.p1ShowWaveInHud)
        {
            string wv = P1aHudString(_p1Table, t => t.p1WaveHudTag, "Wv");
            string wvDone = P1aHudString(_p1Table, t => t.p1WaveHudDone, "Done");
            string wvNone = P1aHudString(_p1Table, t => t.p1WaveHudNone, "--");
            if (waveSpawner != null && !waveSpawner.allWavesComplete)
                line.Append("  ")
                    .Append(wv)
                    .Append(kv)
                    .Append(waveSpawner.currentWaveIndex + 1)
                    .Append(sl)
                    .Append(Mathf.Max(1, waveSpawner.TotalWaveCount));
            else if (waveSpawner != null && waveSpawner.allWavesComplete)
                line.Append("  ").Append(wv).Append(kv).Append(wvDone);
            else
                line.Append("  ").Append(wv).Append(kv).Append(wvNone);
        }
        AppendPublicObjectiveHud(line, kv, sl);

        line.Append('\n');

        string aTag = P1aHudString(_p1Table, t => t.hudTagArea, "A");
        string pvpT = P1aHudString(_p1Table, t => t.hudTagPvp, "PvP");
        string prTag = P1aHudString(_p1Table, t => t.hudTagParty, "Party");
        if (areaState != null) line.Append(aTag).Append(kv).Append(areaState.currentArea).Append("  ");
        if (pvp != null)
        {
            string on = P1aHudString(_p1Table, t => t.hudPvpValueOn, "On");
            string off = P1aHudString(_p1Table, t => t.hudPvpValueOff, "Off");
            string red = P1aHudString(_p1Table, t => t.hudPvpRedLabel, "Red");
            line.Append("  ")
                .Append(pvpT)
                .Append(kv)
                .Append(pvp.pvpEnabled ? on : off)
                .Append(pvp.IsRedName ? pvpPo + red + pvpPc : "");
        }
        if (party != null) line.Append("  ").Append(prTag).Append(kv).Append(party.partySize).Append(sl).Append(party.maxPartySize);
        if (party != null)
        {
            string dTag = P1aHudString(_p1Table, t => t.hudTagDrop, "Drop");
            string dSh = P1aHudString(_p1Table, t => t.hudDropShareSuffix, "Share");
            string dSo = P1aHudString(_p1Table, t => t.hudDropSoloSuffix, "Solo");
            line.Append("  ")
                .Append(dTag)
                .Append(kv)
                .Append(party.shareDropWithParty ? dSh : dSo);
        }

        string qT = P1aHudString(_p1Table, t => t.hudTagSkillQ, "Q");
        string rT = P1aHudString(_p1Table, t => t.hudTagSkillR, "R");
        string rdy = P1aHudString(_p1Table, t => t.hudSkillRdyText, "rdy");
        string secSuf = P1aHudString(_p1Table, t => t.hudSkillSecondsSuffix, "s");
        if (burstSkill != null)
        {
            float cd = burstSkill.CooldownRemaining;
            if (cd > 0.01f) line.Append("  ").Append(qT).Append(kv).Append(cd.ToString(cdFmt)).Append(secSuf);
            else line.Append("  ").Append(qT).Append(kv).Append(rdy);
        }

        if (frostSkill != null)
        {
            float cd = frostSkill.CooldownRemaining;
            if (cd > 0.01f) line.Append("  ").Append(rT).Append(kv).Append(cd.ToString(cdFmt)).Append(secSuf);
            else line.Append("  ").Append(rT).Append(kv).Append(rdy);
        }

        if (showDebugDetails && player != null)
        {
            if (Time.unscaledTime >= _hudNearEnemyNextAt)
            {
                _hudNearEnemyNextAt = Time.unscaledTime + nearEnemyItv;
                _hudNearEnemyCache = EnemyStatusEffectsSimple.FindNearestForHud(player.position, nearEnemyRadius);
            }
            string st = _hudNearEnemyCache == null ? "" : _hudNearEnemyCache.GetHudSummary();
            if (st.Length > 0) line.Append("  ").Append(nel).Append(st).Append(ner);
        }

        if (showDebugDetails && equipDebug != null && equipDebug.testArmor != null)
        {
            EquipmentDataSimple a = equipDebug.testArmor;
            int ps = equipDebug.playerStrengthForTest;
            bool ok = equipDebug.CanEquipTestArmor();
            string eqT = P1aHudString(_p1Table, t => t.hudTagEq, "Eq");
            string okCh = P1aHudString(_p1Table, t => t.hudTagEqOk, "+");
            string badCh = P1aHudString(_p1Table, t => t.hudTagEqBad, "X");
            line.Append("  ")
                .Append(eqT)
                .Append(kv)
                .Append(ps)
                .Append(sl)
                .Append(a.requiredStrength)
                .Append(ok ? okCh : badCh);
        }

        if (progress != null)
        {
            int need = progress.XpNeededThisLevel();
            int remain = Mathf.Max(0, need - progress.xpIntoCurrentLevel);
            string lvT = P1aHudString(_p1Table, t => t.hudTagLv, "Lv");
            string diffT = P1aHudString(_p1Table, t => t.hudTagDiff, "差");
            string spT = P1aHudString(_p1Table, t => t.hudTagSp, "SP");
            string xpP = P1aHudString(_p1Table, t => t.hudTagXpPool, "XP池");
            line.Append("  ")
                .Append(lvT)
                .Append(progress.level)
                .Append(' ')
                .Append(progress.xpIntoCurrentLevel.ToString(xpFmt))
                .Append(sl)
                .Append(need.ToString(xpFmt))
                .Append("  ")
                .Append(diffT)
                .Append(remain)
                .Append("  ")
                .Append(spT)
                .Append(kv)
                .Append(progress.skillUnlockPoints);
            line.Append("  ").Append(xpP).Append(kv).Append(progress.xpBank);
        }

        if (inventory != null)
        {
            string wT = P1aHudString(_p1Table, t => t.hudTagWeight, "W");
            string hT = P1aHudString(_p1Table, t => t.hudTagInvHp, "H");
            string mT = P1aHudString(_p1Table, t => t.hudTagInvMp, "M");
            line.Append("  ")
                .Append(wT)
                .Append(kv)
                .Append(inventory.CurrentWeight.ToString(wFmt))
                .Append(sl)
                .Append(inventory.maxCarryWeight.ToString(wFmt));
            line.Append("  ").Append(hT).Append(kv).Append(inventory.HpPotionCount);
            line.Append("  ").Append(mT).Append(kv).Append(inventory.MpPotionCount);
        }

        if (showDebugDetails && burstMastery != null)
        {
            string mq = P1aHudString(_p1Table, t => t.hudTagMasteryQ, "q");
            string mr = P1aHudString(_p1Table, t => t.hudTagMasteryR, "r");
            line.Append($"  {mq}{burstMastery.burstSkillLevel}");
            line.Append($"  {mr}{burstMastery.frostSkillLevel}");
        }
        if (showDebugDetails && unlocks != null)
        {
            string qt = P1aHudString(_p1Table, t => t.hudTagUnlockQt, "Qt");
            string rtu = P1aHudString(_p1Table, t => t.hudTagUnlockRt, "Rt");
            line.Append($"  {qt}{unlocks.burstTier} {rtu}{unlocks.frostTier}");
        }

        if (wallet != null)
        {
            string gT = P1aHudString(_p1Table, t => t.hudTagGold, "G");
            line.Append("  ").Append(gT).Append(kv).Append(wallet.Gold);
        }

        if (bank != null)
        {
            string bgT = P1aHudString(_p1Table, t => t.hudTagBankGold, "BG");
            string bhT = P1aHudString(_p1Table, t => t.hudTagBankHp, "BH");
            string bmT = P1aHudString(_p1Table, t => t.hudTagBankMp, "BM");
            line.Append("  ")
                .Append(bgT)
                .Append(kv)
                .Append(bank.bankGold)
                .Append(' ')
                .Append(bhT)
                .Append(kv)
                .Append(bank.bankHpPotion)
                .Append(' ')
                .Append(bmT)
                .Append(kv)
                .Append(bank.bankMpPotion);
        }
        if (showDebugDetails && enhance != null)
        {
            string pl = P1aHudString(_p1Table, t => t.hudTagEnhancePlus, "+");
            string drT = P1aHudString(_p1Table, t => t.hudTagDr, "DR");
            string pctS = P1aHudString(_p1Table, t => t.hudTagPercent, "%");
            line.Append("  ")
                .Append(pl)
                .Append(enhance.enhanceStep)
                .Append(' ')
                .Append(drT)
                .Append(kv)
                .Append((enhance.DamageReductionFraction * 100f).ToString(drFmt))
                .Append(pctS);
        }
        if (showDebugDetails && progress != null)
        {
            line.Append(P1aHudStringKeepWhitespace(_p1Table, t => t.hudHelpTextProgress,
                "  U升 I点 O解Q2 P解R2 1红 2蓝 B买红 N买蓝 V卖药"));
        }
        if (showDebugDetails && bank != null)
        {
            line.Append(P1aHudStringKeepWhitespace(_p1Table, t => t.hudHelpTextBank,
                "  F5存金 F6取金 F7存药 F8取药"));
        }
        if (showDebugDetails && party != null)
        {
            line.Append(P1aHudStringKeepWhitespace(_p1Table, t => t.hudHelpTextParty, "  小键盘+/-队伍"));
        }
        if (showDebugDetails && chat != null)
        {
            line.Append(P1aHudStringKeepWhitespace(_p1Table, t => t.hudHelpTextChat, "  Enter本地 `系统"));
        }
        if (stateExport != null)
        {
            line.Append('\n');
            string d5B = P1aHudString(_p1Table, t => t.hudTagD5Banner, "D5");
            string srvT = P1aHudString(_p1Table, t => t.hudTagSrvVal, "SrvVal");
            string lMk = P1aHudString(_p1Table, t => t.hudTagSrvValLowMark, "L");
            string hMk = P1aHudString(_p1Table, t => t.hudTagSrvValHighMark, "H");
            string codesT = P1aHudString(_p1Table, t => t.hudTagCodes, "codes");
            string netT = P1aHudString(_p1Table, t => t.hudTagNet, "NET");
            string hltT = P1aHudString(_p1Table, t => t.hudTagHlt, "HLT");
            string audT = P1aHudString(_p1Table, t => t.hudTagAudC, "AudC");
            string synT = P1aHudString(_p1Table, t => t.hudTagSyn, "syn");
            string f2T = P1aHudString(_p1Table, t => t.hudTagF2, "f2");
            string etgT = P1aHudString(_p1Table, t => t.hudTagEtg, "etg");
            string dMsT = P1aHudString(_p1Table, t => t.hudTagD5Dur, "d");
            string d5RY = P1aHudString(_p1Table, t => t.hudTagD5Retry, "r");
            string d5Unit = P1aHudString(_p1Table, t => t.hudTagD5DurationUnit, "ms");
            string etgPreV = P1aHudString(_p1Table, t => t.hudTagEtgPre, "pre");
            string netSuf = P1aHudString(_p1Table, t => t.hudTagNetAlertSuffix, "!");
            string vOk = P1aHudString(_p1Table, t => t.hudTagValOk, "ok");
            string vNo = P1aHudString(_p1Table, t => t.hudTagValNo, "no");
            string errL = P1aHudString(_p1Table, t => t.hudTagErr, "e");
            string detL = P1aHudString(_p1Table, t => t.hudTagDetail, "dt");
            string ifmW = P1aHudString(_p1Table, t => t.hudTagIfm, "ifm");
            line.Append(d5B).Append(d5sp);
            line.Append(srvT).Append(kv).Append(lMk).Append(stateExport.LastSyncWarnLow).Append(hMk).Append(stateExport.LastSyncWarnHigh);
            if (stateExport.LastSyncValidationOk.HasValue)
            {
                string valL = P1aHudString(_p1Table, t => t.hudTagVal, "val");
                line.Append(' ')
                    .Append(valL)
                    .Append(kv)
                    .Append(stateExport.LastSyncValidationOk.Value ? vOk : vNo);
            }
            if (!string.IsNullOrEmpty(stateExport.LastAuditCategoryPreview))
                line.Append(' ').Append(stateExport.LastAuditCategoryPreview);
            if (!string.IsNullOrEmpty(stateExport.LastWarningsCodesPreview))
            {
                line.Append(' ').Append(codesT).Append(kv).Append(stateExport.LastWarningsCodesPreview);
            }
            if (stateExport.LastNetAlertHigh) line.Append(' ').Append(netT).Append(kv).Append(netSuf);
            if (!string.IsNullOrEmpty(stateExport.LastHealthProbePreview))
                line.Append(' ').Append(hltT).Append(kv).Append(stateExport.LastHealthProbePreview);
            if (!string.IsNullOrEmpty(stateExport.LastMetricsAuditCategoriesPreview))
                line.Append(' ').Append(audT).Append(kv).Append(stateExport.LastMetricsAuditCategoriesPreview);
            if (!string.IsNullOrEmpty(stateExport.LastSyncPostStatusTag))
                line.Append(' ').Append(synT).Append(kv).Append(stateExport.LastSyncPostStatusTag);
            if (stateExport.LastSyncPostHttpCode >= 0)
                line.Append(' ').Append(f2T).Append(kv).Append(stateExport.LastSyncPostHttpCode);
            if (stateExport.LastIfMatchWasSent) line.Append(' ').Append(ifmW);
            if (stateExport.LastStateEtagPrefetchRan) line.Append(' ').Append(etgT).Append(kv).Append(etgPreV);
            if (stateExport.LastSyncDurationMs >= 0)
                line.Append(' ')
                    .Append(dMsT)
                    .Append(kv)
                    .Append(stateExport.LastSyncDurationMs.ToString(d5Fmt))
                    .Append(d5Unit);
            if (stateExport.LastSyncRetryCount > 0)
                line.Append(' ').Append(d5RY).Append(kv).Append(stateExport.LastSyncRetryCount);
            if (!string.IsNullOrEmpty(stateExport.LastSyncResponseErrorCode))
            {
                string e = stateExport.LastSyncResponseErrorCode;
                if (e.Length > errMax) e = e.Substring(0, errKeep) + ellip;
                line.Append(' ').Append(errL).Append(kv).Append(e);
            }
            if (!string.IsNullOrEmpty(stateExport.LastSyncResponseDetailPreview))
            {
                string dtp = stateExport.LastSyncResponseDetailPreview;
                if (dtp.Length > dtlMax) dtp = dtp.Substring(0, dtlKeep) + ellip;
                line.Append(' ').Append(detL).Append(kv).Append(dtp);
            }
        }
        if (showDebugDetails && MultiplayerBootstrapSimple.Instance != null)
        {
            line.Append('\n');
            line.Append(MultiplayerBootstrapSimple.Instance.BuildMultiplayerHudLine());
        }
        if (showDebugDetails)
        {
            line.Append('\n');
            line.Append(P1aHudString(_p1Table, t => t.hudHelpTextKeys,
                "Keys Esc退出 F1健康 ;审计类 F4审计 F3整包 F2POST F12状态 F9存 F10读 F11清"));
        }
        if (showDebugDetails && health != null)
        {
            line.Append(P1aHudStringKeepWhitespace(_p1Table, t => t.hudHelpTextDeath, "  死亡掉金并复活"));
        }

        if (showDebugDetails && _p1Table != null)
        {
            AppendHudLines(line, _p1Table.hudBaiBuLines, 100);
            AppendHudLines(line, _p1Table.hudBaiBuLines2, 100);
        }

        hudText.text = line.ToString();
    }

    void AppendPublicObjectiveHud(StringBuilder line, string kv, string slash)
    {
        string objTag = P1aHudString(_p1Table, t => t.p1HudTag, "Obj");
        string stTag = "st";
        string pTag = P1aHudString(_p1Table, t => t.hudTagParty, "P");
        string eliteTag = "Elite";
        string on = P1aHudString(_p1Table, t => t.hudPvpValueOn, "on");
        string off = P1aHudString(_p1Table, t => t.hudPvpValueOff, "off");
        string join = "join";
        string watching = "watch";
        string active = "active";
        string done = "done";

        if (waveSpawner == null)
        {
            line.Append("  ").Append(objTag).Append(kv).Append(off);
            return;
        }

        bool objectiveActive = !waveSpawner.allWavesComplete;
        if (!objectiveActive)
        {
            line.Append("  ").Append(objTag).Append(kv).Append(done);
            return;
        }

        Vector3 c = waveSpawner.center != null ? waveSpawner.center.position : waveSpawner.transform.position;
        float joinRadius = Mathf.Max(4f, waveSpawner.spawnRingRadius * PublicObjectiveJoinRadiusScale);
        bool inJoinRange = player != null && Vector3.Distance(player.position, c) <= joinRadius;
        bool inField = areaState == null || !areaState.IsInCity;
        bool participating = inJoinRange && inField;

        line.Append("  ")
            .Append(objTag)
            .Append(kv)
            .Append(active)
            .Append(' ')
            .Append(stTag)
            .Append(kv)
            .Append(participating ? join : watching)
            .Append(' ')
            .Append(pTag)
            .Append(kv)
            .Append(waveSpawner.currentWaveIndex + 1)
            .Append(slash)
            .Append(Mathf.Max(1, waveSpawner.TotalWaveCount));

        PublicObjectiveEventStateSimple eventState = PublicObjectiveEventStateSimple.Instance;
        if (eventState != null)
        {
            line.Append(' ')
                .Append(eliteTag)
                .Append(kv)
                .Append(eventState.EliteActive ? on : done)
                .Append(' ')
                .Append("W")
                .Append(kv)
                .Append(eventState.EliteWave)
                .Append(' ')
                .Append("K")
                .Append(kv)
                .Append(eventState.EliteDefeatCount);
        }
    }
}
