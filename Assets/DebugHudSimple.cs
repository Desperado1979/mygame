using System;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

// 战斗 HUD 预览字段语义（tgt*/ply*/tgt:def）：仓库根 docs/getting-started.md §6.5；局内结算仍以 Server 为准。
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
    public PlayerStatsSimple playerStats;
    public P1A1QuestState p1A1Quest;
    public WaveSpawnerSimple waveSpawner;
    [Header("Week1 split: runtime vs debug")]
    public bool showDebugDetails = true;

    [Tooltip("制作人单机验收：HUD 最顶一行白话提示（表 hudBaiBuLines[0]；空则用内置句）。与 showDebugDetails 无关。")]
    public bool showDirectorPlainSmokeTip = true;

    const string DirectorPlainSmokeTipFallback =
        "单机玩法自检：打怪后金币和经验应有变化 | U升级 | I技能点 | 小键盘1234加四维 | 控制台勿大片红错";

    /** 玩测：避免每帧全场景遍历；约 3Hz 刷新 E: 计数（回落见 D3GrowthBalance）。 */
    float _hudEnemyCountNextAt;
    int _hudEnemyCountCache;
    float _hudNearEnemyNextAt;
    EnemyStatusEffectsSimple _hudNearEnemyCache;
    readonly StringBuilder _hudLine = new StringBuilder(1024);
    P1AContentConfig _p1Table;
    float _w1StartUnscaled = float.NegativeInfinity;
    bool _w1CoopReady;
    bool _w1ObjectiveReady;
    bool _w1RewardReady;
    bool _w1BaselineReady;
    int _w1BaseGold;
    int _w1BaseLevel;
    int _w1BaseXpIntoLevel;
    int _w1BaseXpBank;
    int _w1BaseEliteDefeatCount;
    ulong _w1BaseRoomSeq;
    Transform _w1TrackedPlayer;

    void Awake()
    {
        P1AContentConfig.ClearDefaultCache();
        _p1Table = P1AContentConfig.TryLoadDefault();
    }

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
        if (_p1Table == null)
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

    void GetWaveDisplayForHud(out int waveOneBased, out int totalWaves, out bool allWavesCompleteOut)
    {
        PublicObjectiveWaveDisplayUtil.GetWaveDisplay(waveSpawner, out waveOneBased, out totalWaves, out allWavesCompleteOut);
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
        if (playerStats == null || playerStats.transform != player)
            playerStats = player.GetComponent<PlayerStatsSimple>();
        if (equipDebug == null || equipDebug.transform != player)
            equipDebug = player.GetComponent<PlayerEquipmentDebugSimple>();
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

    static string TruncateHud(string s, int maxChars)
    {
        if (string.IsNullOrEmpty(s))
            return string.Empty;
        if (maxChars < 4)
            maxChars = 4;
        if (s.Length <= maxChars)
            return s;
        return s.Substring(0, maxChars - 1) + "…";
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

    static void AppendHudLines(StringBuilder line, string[] arr, int limit, int startIndex = 0)
    {
        if (line == null || arr == null || limit <= 0) return;
        startIndex = Mathf.Max(0, startIndex);
        for (int i = startIndex; i < limit && i < arr.Length; i++)
        {
            string s = arr[i];
            if (string.IsNullOrWhiteSpace(s)) continue;
            line.Append('\n').Append(s);
        }
    }

    static string FormatMasteryProgress(int castsThisLevel, int castsNeed)
    {
        if (castsNeed == int.MaxValue)
            return "MAX";
        return castsThisLevel + "/" + castsNeed;
    }

    /// <summary>推导漂移标记：<c>hp≠/mp≠/mpr≠/xp≠/w≠</c>；<c>ΔHP/ΔMP/Δmpr/Δxp/ΔW</c> 仅在 Editor / Development Build。</summary>
    static void AppendDerivedDriftIndicators(
        StringBuilder line,
        PlayerHealthSimple health,
        PlayerMpSimple mp,
        PlayerInventorySimple inventory,
        PlayerProgressSimple progress,
        D3GrowthBalanceData bd,
        int xHp,
        int xMp,
        float wCap)
    {
        bool driftHp = health != null && health.maxHp != xHp;
        bool driftMp = mp != null && mp.MaxMp != xMp;
        float mpRegTable = bd != null ? Mathf.Max(0f, bd.mpRegenPerSecond) : 0f;
        bool driftMpr = mp != null && bd != null && Mathf.Abs(mp.mpRegenPerSecond - mpRegTable) > 0.0001f;
        bool driftW = inventory != null && Mathf.Abs(inventory.maxCarryWeight - wCap) > 0.01f;
        bool driftXp = false;
        if (progress != null && bd != null)
        {
            int maxL = Mathf.Max(1, bd.maxLevel);
            int needXp = D3GrowthBalance.XpNeededForLevel(bd, progress.level);
            if (progress.xpIntoCurrentLevel < 0)
                driftXp = true;
            else if (progress.level < maxL && progress.xpIntoCurrentLevel > needXp)
                driftXp = true;
        }

        if (!driftHp && !driftMp && !driftMpr && !driftXp && !driftW)
            return;

        line.Append(' ');
        bool needSep = false;
        if (driftHp)
        {
            line.Append("hp≠");
            needSep = true;
        }

        if (driftMp)
        {
            if (needSep)
                line.Append(' ');
            line.Append("mp≠");
            needSep = true;
        }

        if (driftMpr)
        {
            if (needSep)
                line.Append(' ');
            line.Append("mpr≠");
            needSep = true;
        }

        if (driftXp)
        {
            if (needSep)
                line.Append(' ');
            line.Append("xp≠");
            needSep = true;
        }

        if (driftW)
        {
            if (needSep)
                line.Append(' ');
            line.Append("w≠");
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        line.Append(' ');
        needSep = false;
        if (driftHp)
        {
            line.Append("ΔHP").Append(health.maxHp - xHp);
            needSep = true;
        }

        if (driftMp)
        {
            if (needSep)
                line.Append(' ');
            line.Append("ΔMP").Append(mp.MaxMp - xMp);
            needSep = true;
        }

        if (driftMpr && mp != null && bd != null)
        {
            if (needSep)
                line.Append(' ');
            line.Append("Δmpr").Append((mp.mpRegenPerSecond - mpRegTable).ToString("F4"));
            needSep = true;
        }

        if (driftXp && progress != null && bd != null)
        {
            if (needSep)
                line.Append(' ');
            int needXp = D3GrowthBalance.XpNeededForLevel(bd, progress.level);
            line.Append("Δxp").Append(progress.xpIntoCurrentLevel - needXp);
            needSep = true;
        }

        if (driftW)
        {
            if (needSep)
                line.Append(' ');
            line.Append("ΔW").Append((inventory.maxCarryWeight - wCap).ToString("F1"));
        }
#endif
    }

    void ResetW1ForPlayer(Transform owner)
    {
        _w1TrackedPlayer = owner;
        _w1StartUnscaled = Time.unscaledTime;
        _w1CoopReady = false;
        _w1ObjectiveReady = false;
        _w1RewardReady = false;
        _w1BaselineReady = false;
        _w1BaseGold = 0;
        _w1BaseLevel = 0;
        _w1BaseXpIntoLevel = 0;
        _w1BaseXpBank = 0;
        _w1BaseEliteDefeatCount = CurrentEliteDefeatCount();
        _w1BaseRoomSeq = ChatRoomStateSimple.RoomBroadcastSeq;
    }

    void UpdateW1Signals()
    {
        if (player == null)
            return;
        if (_w1TrackedPlayer != player)
            ResetW1ForPlayer(player);

        if (party != null && party.partySize >= 2)
            _w1CoopReady = true;
        if (ChatRoomStateSimple.RoomBroadcastSeq > _w1BaseRoomSeq)
            _w1CoopReady = true;

        if (IsPublicObjectiveParticipatingNow())
            _w1ObjectiveReady = true;
        if (CurrentEliteDefeatCount() > _w1BaseEliteDefeatCount)
            _w1ObjectiveReady = true;

        if (!_w1BaselineReady && progress != null && wallet != null)
        {
            _w1BaseGold = wallet.Gold;
            _w1BaseLevel = progress.level;
            _w1BaseXpIntoLevel = progress.xpIntoCurrentLevel;
            _w1BaseXpBank = progress.xpBank;
            _w1BaselineReady = true;
        }

        if (_w1BaselineReady && progress != null && wallet != null)
        {
            if (wallet.Gold > _w1BaseGold
                || progress.level > _w1BaseLevel
                || progress.xpIntoCurrentLevel > _w1BaseXpIntoLevel
                || progress.xpBank > _w1BaseXpBank)
                _w1RewardReady = true;
        }
    }

    bool IsPublicObjectiveParticipatingNow()
    {
        if (waveSpawner == null || player == null)
            return false;
        GetWaveDisplayForHud(out _, out _, out bool wAllDone);
        if (wAllDone)
            return false;
        Vector3 c = waveSpawner.center != null ? waveSpawner.center.position : waveSpawner.transform.position;
        float joinRadius = PublicObjectiveWaveDisplayUtil.ParticipationJoinRadius(waveSpawner);
        bool inJoinRange = Vector3.Distance(player.position, c) <= joinRadius;
        bool inField = areaState == null || !areaState.IsInCity;
        return inJoinRange && inField;
    }

    int CurrentEliteDefeatCount()
    {
        if (PublicObjectiveEventStateSimple.Instance != null)
            return PublicObjectiveEventStateSimple.Instance.EliteDefeatCount;
        if (PublicObjectiveLocalStateSimple.Instance != null)
            return PublicObjectiveLocalStateSimple.Instance.EliteDefeatCount;
        return 0;
    }

    void AppendW1ClosureHud(StringBuilder line, string kv, string slash)
    {
        if (_w1TrackedPlayer == null)
            return;
        string w1Tag = P1aHudString(_p1Table, t => t.p1HudTag, "W1");
        float elapsed = Mathf.Max(0f, Time.unscaledTime - _w1StartUnscaled);
        int done = (_w1CoopReady ? 1 : 0) + (_w1ObjectiveReady ? 1 : 0) + (_w1RewardReady ? 1 : 0);
        float w1Win = Mathf.Max(1f, D3GrowthBalance.Load().w1ClosureWindowSeconds);
        bool inWindow = elapsed <= w1Win;
        string t = FormatW1Duration(elapsed);
        string c = _w1CoopReady ? "C1" : "C0";
        string o = _w1ObjectiveReady ? "O1" : "O0";
        string r = _w1RewardReady ? "R1" : "R0";
        line.Append("  ")
            .Append(w1Tag)
            .Append(kv)
            .Append(done)
            .Append(slash)
            .Append(3)
            .Append(' ')
            .Append("t")
            .Append(kv)
            .Append(t)
            .Append(inWindow ? "" : "!")
            .Append(' ')
            .Append(c)
            .Append(' ')
            .Append(o)
            .Append(' ')
            .Append(r);
    }

    static string FormatW1Duration(float sec)
    {
        int total = Mathf.Max(0, Mathf.FloorToInt(sec));
        int m = total / 60;
        int s = total % 60;
        return m.ToString("00") + ":" + s.ToString("00");
    }

    static string BuildD5SyncVerdict(PlayerStateExportSimple st)
    {
        if (st == null)
            return "idle";
        // [Client-Side Expression] 总开关关：与「从未 F2」的 idle 区分，避免误以为可 POST。
        if (!st.networkSyncEnabled)
            return "off";
        if (st.LastSyncPostHttpCode < 0)
            return "idle";
        bool ok2xx = st.LastSyncPostHttpCode >= 200 && st.LastSyncPostHttpCode < 300;
        bool valOk = st.LastSyncValidationOk.HasValue && st.LastSyncValidationOk.Value;
        if (ok2xx && valOk)
            return "pass";
        if (ok2xx)
            return "warn";
        if (st.LastSyncPostHttpCode == 429 || st.LastSyncPostHttpCode == 503 || st.LastSyncPostHttpCode <= 0)
            return "retry";
        return "fail";
    }

    void Update()
    {
        if (hudText == null) return;
        EnsureWaveSpawnerRef();
        EnsurePlayerRefs();
        UpdateW1Signals();

        D3GrowthBalanceData d3 = D3GrowthBalance.Load();
        float enemyCountItv = (_p1Table != null && _p1Table.hudEnemyCountIntervalSec > 0.01f)
            ? _p1Table.hudEnemyCountIntervalSec
            : Mathf.Max(0.05f, d3.hudEnemyCountPollIntervalSec);
        float nearEnemyItv = (_p1Table != null && _p1Table.hudNearEnemyStatusIntervalSec > 0.01f)
            ? _p1Table.hudNearEnemyStatusIntervalSec
            : Mathf.Max(0.05f, d3.hudNearEnemyStatusPollIntervalSec);
        float nearEnemyRadius = (_p1Table != null && _p1Table.hudNearEnemyScanRadius > 0.5f)
            ? _p1Table.hudNearEnemyScanRadius
            : Mathf.Max(0.5f, d3.hudNearEnemyScanRadiusDefault);
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
        int errMax = (_p1Table != null && _p1Table.hudTruncErrCodeMax > 0)
            ? _p1Table.hudTruncErrCodeMax
            : Mathf.Max(4, d3.hudTruncErrCodeMaxDefault);
        int errKeep = (_p1Table != null && _p1Table.hudTruncErrCodeKeep > 0)
            ? _p1Table.hudTruncErrCodeKeep
            : Mathf.Max(4, d3.hudTruncErrCodeKeepDefault);
        int dtlMax = (_p1Table != null && _p1Table.hudTruncDetailMax > 0)
            ? _p1Table.hudTruncDetailMax
            : Mathf.Max(8, d3.hudTruncDetailMaxDefault);
        int dtlKeep = (_p1Table != null && _p1Table.hudTruncDetailKeep > 0)
            ? _p1Table.hudTruncDetailKeep
            : Mathf.Max(8, d3.hudTruncDetailKeepDefault);

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
        if (showDirectorPlainSmokeTip)
        {
            string tip0 = null;
            if (_p1Table != null && _p1Table.hudBaiBuLines != null && _p1Table.hudBaiBuLines.Length > 0)
                tip0 = _p1Table.hudBaiBuLines[0];
            if (string.IsNullOrWhiteSpace(tip0))
                tip0 = DirectorPlainSmokeTipFallback;
            line.Append(tip0.Trim()).Append('\n');
        }
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
            if (waveSpawner == null)
            {
                line.Append("  ").Append(wv).Append(kv).Append(wvNone);
            }
            else
            {
                GetWaveDisplayForHud(out int w1, out int wtot, out bool wAllDone);
                if (!wAllDone)
                {
                    line.Append("  ")
                        .Append(wv)
                        .Append(kv)
                        .Append(w1)
                        .Append(sl)
                        .Append(wtot);
                }
                else
                    line.Append("  ").Append(wv).Append(kv).Append(wvDone);
            }
        }
        AppendPublicObjectiveHud(line, kv, sl, showDebugDetails);
        AppendW1ClosureHud(line, kv, sl);

        line.Append('\n');
        if (PublicObjectiveLastToast.IsActive)
            line.Append(PublicObjectiveLastToast.Message).Append('\n');

        string aTag = P1aHudString(_p1Table, t => t.hudTagArea, "A");
        string pvpT = P1aHudString(_p1Table, t => t.hudTagPvp, "PvP");
        string prTag = P1aHudString(_p1Table, t => t.hudTagParty, "Party");
        if (areaState != null) line.Append(aTag).Append(kv).Append(areaState.currentArea).Append("  ");
        if (pvp != null)
        {
            string on = P1aHudString(_p1Table, t => t.hudPvpValueOn, "On");
            string off = P1aHudString(_p1Table, t => t.hudPvpValueOff, "Off");
            string red = P1aHudString(_p1Table, t => t.hudPvpRedLabel, "Red");
            string safe = P1aHudString(_p1Table, t => t.hudPvpValueSafeBlocked, "Safe");
            line.Append("  ")
                .Append(pvpT)
                .Append(kv)
                .Append(pvp.pvpEnabled ? on : off)
                .Append(pvp.IsRedName ? pvpPo + red + pvpPc : "")
                .Append(!pvp.pvpEnabled && pvp.IsInSafeArea ? pvpPo + safe + pvpPc : "")
                .Append(pvp.HasHint ? pvpPo + pvp.LastHint + pvpPc : "");
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
            int ps = equipDebug.ResolveStrengthForRequirement();
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
            if (a.bonusMaxHp > 0 || a.bonusMaxMp > 0)
            {
                line.Append(" +H")
                    .Append(a.bonusMaxHp)
                    .Append("+M")
                    .Append(a.bonusMaxMp);
            }
        }

        if (progress != null)
        {
            int need = progress.XpNeededThisLevel();
            int remain = Mathf.Max(0, need - progress.xpIntoCurrentLevel);
            int capLv = D3GrowthBalance.Load().maxLevel;
            string lvT = P1aHudString(_p1Table, t => t.hudTagLv, "Lv");
            string diffT = P1aHudString(_p1Table, t => t.hudTagDiff, "差");
            string spT = P1aHudString(_p1Table, t => t.hudTagSp, "SP");
            string xpP = P1aHudString(_p1Table, t => t.hudTagXpPool, "XP池");
            line.Append("  ")
                .Append(lvT)
                .Append(progress.level)
                .Append('/')
                .Append(capLv)
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

        if (playerStats != null)
        {
            line.Append("  d3 S")
                .Append(playerStats.strength)
                .Append( "/")
                .Append(playerStats.agility)
                .Append( "/")
                .Append(playerStats.intellect)
                .Append( "/")
                .Append(playerStats.vitality);
            line.Append(" U").Append(kv).Append(playerStats.unallocatedStatPoints);
            if (showDebugDetails)
            {
                // [Client-Side Expression] 有「近敌」时用其物/法防做预览，无则回落表默认；局内真实结算仍以 Server 为准。
                D3GrowthBalanceData bd = D3GrowthBalance.Load();
                EnemyHealthSimple nearEnemyHealth = null;
                if (_hudNearEnemyCache != null)
                    nearEnemyHealth = _hudNearEnemyCache.GetComponent<EnemyHealthSimple>();
                int phyDefPreview = nearEnemyHealth != null
                    ? nearEnemyHealth.PhysicalDefense
                    : bd.enemyDefaultPhysicalDefense;
                int spellDefPreview = nearEnemyHealth != null
                    ? nearEnemyHealth.SpellDefense
                    : bd.enemyDefaultSpellDefense;
                int mAtk = D3GrowthBalance.ComputeMeleePhysicalDamage(bd, playerStats.strength);
                int vsDef = D3GrowthBalance.ApplyPhysicalDefenseToDamage(mAtk, phyDefPreview);
                float critP = D3GrowthBalance.MeleeCritProbability(bd, playerStats.agility);
                float dodgeP = D3GrowthBalance.ComputePlayerDodgeProbability(bd, playerStats.agility);
                float exp = D3GrowthBalance.ExpectedMeleeDamageAfterArmor(bd, playerStats.agility, vsDef);
                float qMul = burstMastery != null ? burstMastery.BurstDamageMultiplier : 1f;
                int qTier = unlocks != null ? unlocks.burstTier : 1;
                float qRg = D3GrowthBalance.ComputeBurstOverlapRadius(
                    bd,
                    burstSkill != null ? burstSkill.skillRadius : bd.skillBurstRadius,
                    qTier);
                int qBaseHit = burstSkill != null ? burstSkill.damagePerEnemy : bd.skillBurstDamagePerHit;
                int qRaw = D3GrowthBalance.ComputeBurstRolledDamage(
                    bd, qBaseHit, playerStats.intellect, qMul, qTier);
                int vsSp = D3GrowthBalance.ApplySpellDefenseToDamage(qRaw, spellDefPreview);
                int bTk = Mathf.Max(1, bd.skillBurstBurnDamagePerTick);
                int bSp = D3GrowthBalance.ApplySpellDefenseToDamage(bTk, spellDefPreview);
                float bIv = Mathf.Max(0.05f, bd.skillBurstBurnTickIntervalSec);
                int rTier = unlocks != null ? unlocks.frostTier : 1;
                float rRg = D3GrowthBalance.ComputeFrostOverlapRadius(
                    bd,
                    frostSkill != null ? frostSkill.skillRadius : bd.skillFrostRadius,
                    rTier);
                int rBaseHit = frostSkill != null ? frostSkill.frostDamagePerEnemy : bd.skillFrostDamagePerHit;
                int rRaw = D3GrowthBalance.ComputeFrostRolledDamage(
                    bd, rBaseHit, playerStats.intellect, rTier);
                int rSp = rRaw <= 0 ? 0 : D3GrowthBalance.ApplySpellDefenseToDamage(rRaw, spellDefPreview);
                float frzS = D3GrowthBalance.ComputeFrostFreezeDurationSeconds(
                    bd,
                    frostSkill != null ? frostSkill.freezeDurationSeconds : bd.skillFrostFreezeSec,
                    rTier,
                    burstMastery != null ? burstMastery.FrostFreezeDurationMultiplier : 1f);
                float atkItv = D3GrowthBalance.ComputeMeleeAttackInterval(bd, playerStats.agility);
                int pDef = D3GrowthBalance.ComputePlayerPhysicalDefense(bd, playerStats.strength, playerStats.vitality);
                int sDef = D3GrowthBalance.ComputePlayerSpellDefense(bd, playerStats.intellect, playerStats.vitality);
                int lvlHud = progress != null ? Mathf.Max(1, progress.level) : 1;
                int exHpHud = equipDebug != null ? equipDebug.AggregateEquipHpBonus() : 0;
                int exMpHud = equipDebug != null ? equipDebug.AggregateEquipMpBonus() : 0;
                int xHp = D3GrowthBalance.ComputeMaxHp(bd, playerStats.vitality, lvlHud, exHpHud);
                int xMp = D3GrowthBalance.ComputeMaxMp(bd, playerStats.intellect, lvlHud, exMpHud);

                float hpRegenPerSec = 0f;
                float pctHpPer10Sec = Mathf.Max(0f, bd.hpRegenPercentPer10SecPerVit) * Mathf.Max(0, playerStats.vitality);
                if (pctHpPer10Sec > 0f)
                    hpRegenPerSec = xHp * (pctHpPer10Sec / 100f) / 10f;

                float mpRegenPerSec = mp != null ? mp.mpRegenPerSecond : bd.mpRegenPerSecond;
                float mRg = D3GrowthBalance.GetMeleeAttackRangeForHud(
                    bd,
                    player != null ? player.gameObject : null);
                float mSp = D3GrowthBalance.GetPlayerMoveSpeedForHud(
                    bd,
                    player != null ? player.gameObject : null);
                float pckR = D3GrowthBalance.GetInteractionPickupRangeForHud(bd);
                float wCap = D3GrowthBalance.ComputeCarryWeight(bd, playerStats.strength);
                line.Append("  c%")
                    .Append(kv)
                    .Append((critP * 100f).ToString("F1"))
                    .Append(" d%")
                    .Append(kv)
                    .Append((dodgeP * 100f).ToString("F1"))
                    .Append("  mAtk")
                    .Append(kv)
                    .Append(mAtk)
                    .Append(" vsDef")
                    .Append(kv)
                    .Append(vsDef)
                    .Append(" E~")
                    .Append(kv)
                    .Append(exp.ToString("F1"));
                if (nearEnemyHealth != null)
                {
                    // tgt* = 近敌身上用于预览的防（非玩家）；与下行 ply* 区分。
                    line.Append(" tgtPD")
                        .Append(kv)
                        .Append(phyDefPreview)
                        .Append(" tgtSD")
                        .Append(kv)
                        .Append(spellDefPreview);
                }
                else
                {
                    // 与有近敌时 tgtPD/tgtSD 对称：明示 vsDef/vsSp 等用的是表默认敌防预览。
                    line.Append(" tgt:def");
                }

                line.Append(" qR")
                    .Append(kv)
                    .Append(qRaw)
                    .Append(" vsSp")
                    .Append(kv)
                    .Append(vsSp)
                    .Append(" bTk")
                    .Append(kv)
                    .Append(bTk)
                    .Append(" bSp")
                    .Append(kv)
                    .Append(bSp)
                    .Append(" bIv")
                    .Append(kv)
                    .Append(bIv.ToString("F2"))
                    .Append(" qRg")
                    .Append(kv)
                    .Append(qRg.ToString("F2"))
                    .Append(" rR")
                    .Append(kv)
                    .Append(rRaw)
                    .Append(" rSp")
                    .Append(kv)
                    .Append(rSp)
                    .Append(" frzS")
                    .Append(kv)
                    .Append(frzS.ToString("F2"))
                    .Append(" rRg")
                    .Append(kv)
                    .Append(rRg.ToString("F2"))
                    .Append(" mRg")
                    .Append(kv)
                    .Append(mRg.ToString("F2"))
                    .Append(" mSp")
                    .Append(kv)
                    .Append(mSp.ToString("F2"))
                    .Append(" pckR")
                    .Append(kv)
                    .Append(pckR.ToString("F2"))
                    .Append(" wCap")
                    .Append(kv)
                    .Append(wCap.ToString(wFmt))
                    .Append(" xHp")
                    .Append(kv)
                    .Append(xHp)
                    .Append(" xMp")
                    .Append(kv)
                    .Append(xMp)
                    .Append(" atkItv")
                    .Append(kv)
                    .Append(atkItv.ToString("F2"))
                    .Append("s")
                    .Append(" hpRegen/s")
                    .Append(kv)
                    .Append(hpRegenPerSec.ToString("F2"))
                    .Append(" plyPD")
                    .Append(kv)
                    .Append(pDef)
                    .Append(" plySD")
                    .Append(kv)
                    .Append(sDef)
                    .Append(" mpReg/s")
                    .Append(kv)
                    .Append(mpRegenPerSec.ToString("F2"));
                AppendDerivedDriftIndicators(line, health, mp, inventory, progress, bd, xHp, xMp, wCap);
            }
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
            string qProg = FormatMasteryProgress(burstMastery.burstCastsThisLevel, burstMastery.CastsNeededForNextLevel());
            string rProg = FormatMasteryProgress(burstMastery.frostCastsThisLevel, burstMastery.FrostCastsNeededForNextLevel());
            line.Append($"  {mq}{burstMastery.burstSkillLevel}({qProg})");
            line.Append($"  {mr}{burstMastery.frostSkillLevel}({rProg})");
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
                .Append(pctS)
                .Append("  T$")
                .Append(enhance.GoldCostNext().ToString());
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
            line.Append(P1aHudStringKeepWhitespace(_p1Table, t => t.hudHelpTextParty, "  小键盘+/-队伍 *切换掉落共享"));
        }
        if (showDebugDetails && chat != null)
        {
            line.Append(P1aHudStringKeepWhitespace(_p1Table, t => t.hudHelpTextChat, "  Enter房间 `系统(H)"));
            if (ChatRoomStateSimple.RoomBroadcastSeq > 0
                || !string.IsNullOrEmpty(ChatRoomStateSimple.LastRoomLineHud)
                || ChatRoomStateSimple.SystemBroadcastSeq > 0
                || !string.IsNullOrEmpty(ChatRoomStateSimple.LastSystemLineHud))
            {
                line.Append('\n');
                string chT = P1aHudString(_p1Table, t => t.hudTagChatRoom, "ch");
                string syT = P1aHudString(_p1Table, t => t.hudTagChatSys, "sys");
                int trRoom = Mathf.Max(8, d3.hudChatRoomDebugTruncateChars);
                int trSys = Mathf.Max(8, d3.hudChatSystemDebugTruncateChars);
                line.Append(chT)
                    .Append(kv)
                    .Append(TruncateHud(ChatRoomStateSimple.RoomHistoryHud, trRoom))
                    .Append("  ")
                    .Append(syT)
                    .Append(kv)
                    .Append(TruncateHud(ChatRoomStateSimple.SystemHistoryHud, trSys));
            }
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
            string f2vT = P1aHudString(_p1Table, t => t.hudTagF2Verdict, "f2v");
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
            line.Append(' ').Append(f2vT).Append(kv).Append(BuildD5SyncVerdict(stateExport));
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
            int skipBaiBu0 = (_p1Table.hudBaiBuLines != null && _p1Table.hudBaiBuLines.Length > 0
                              && !string.IsNullOrWhiteSpace(_p1Table.hudBaiBuLines[0]))
                ? 1
                : 0;
            AppendHudLines(line, _p1Table.hudBaiBuLines, 100, skipBaiBu0);
            AppendHudLines(line, _p1Table.hudBaiBuLines2, 100);
        }

        hudText.text = line.ToString();
    }

    void AppendPublicObjectiveHud(StringBuilder line, string kv, string slash, bool debugDetails)
    {
        string objTag = P1aHudString(_p1Table, t => t.publicObjectiveHudTag, "公目");
        string stKey = P1aHudString(_p1Table, t => t.publicObjectiveParticipationKey, "态");
        string waveKey = P1aHudString(_p1Table, t => t.publicObjectiveWaveKey, "波");
        string eliteTag = P1aHudString(_p1Table, t => t.publicObjectiveEliteTag, "精");
        string inProg = P1aHudString(_p1Table, t => t.publicObjectiveInProgress, "进行中");
        string segDone = P1aHudString(_p1Table, t => t.publicObjectiveSegmentDone, "本段已清场");
        string noSp = P1aHudString(_p1Table, t => t.publicObjectiveNoSpawner, "无");
        string partOk = P1aHudString(_p1Table, t => t.publicObjectiveParticipating, "参与");
        string watch = P1aHudString(_p1Table, t => t.publicObjectiveObserving, "观战");
        string inTown = P1aHudString(_p1Table, t => t.publicObjectiveInTown, "城内");
        string eOn = P1aHudString(_p1Table, t => t.publicObjectiveEliteActive, "在场");
        string eCl = P1aHudString(_p1Table, t => t.publicObjectiveEliteCleared, "已清");
        string eOff = P1aHudString(_p1Table, t => t.publicObjectiveEliteDown, "暂无");
        string eWk = P1aHudString(_p1Table, t => t.publicObjectiveEliteWaveKey, "序");
        string eKk = P1aHudString(_p1Table, t => t.publicObjectiveEliteKillKey, "计");

        if (waveSpawner == null)
        {
            line.Append("  ").Append(objTag).Append(kv).Append(noSp);
            return;
        }

        GetWaveDisplayForHud(out int w1, out int wtot, out bool wAllDone);
        bool objectiveActive = !wAllDone;
        if (!objectiveActive)
        {
            line.Append("  ").Append(objTag).Append(kv).Append(segDone);
            return;
        }

        Vector3 c = waveSpawner.center != null ? waveSpawner.center.position : waveSpawner.transform.position;
        float joinRadius = PublicObjectiveWaveDisplayUtil.ParticipationJoinRadius(waveSpawner);
        bool inJoinRange = player != null && Vector3.Distance(player.position, c) <= joinRadius;
        bool inField = areaState == null || !areaState.IsInCity;
        bool participating = inJoinRange && inField;
        string participation;
        if (player == null)
            participation = watch;
        else if (participating)
            participation = partOk;
        else if (areaState != null && areaState.IsInCity)
            participation = inTown;
        else
            participation = watch;

        line.Append("  ")
            .Append(objTag)
            .Append(kv)
            .Append(inProg)
            .Append(' ')
            .Append(stKey)
            .Append(kv)
            .Append(participation)
            .Append(' ')
            .Append(waveKey)
            .Append(kv)
            .Append(w1)
            .Append(slash)
            .Append(Mathf.Max(1, wtot));

        // [Client-Side Expression] B2：单机核对「参与半径」与站位（与 PublicObjectiveWaveDisplayUtil.ParticipationJoinRadius 一致）。
        if (debugDetails && player != null)
        {
            float dx = player.position.x - c.x;
            float dz = player.position.z - c.z;
            float distXZ = Mathf.Sqrt(dx * dx + dz * dz);
            line.Append(' ')
                .Append("jr")
                .Append(kv)
                .Append(joinRadius.ToString("F1"))
                .Append(' ')
                .Append("dst")
                .Append(kv)
                .Append(distXZ.ToString("F1"));
        }

        int ew;
        int ek;
        bool ea;
        if (PublicObjectiveEventStateSimple.Instance != null)
        {
            var eventState = PublicObjectiveEventStateSimple.Instance;
            ew = eventState.EliteWave;
            ek = eventState.EliteDefeatCount;
            ea = eventState.EliteActive;
        }
        else if (PublicObjectiveLocalStateSimple.Instance != null)
        {
            var loc = PublicObjectiveLocalStateSimple.Instance;
            ew = loc.EliteWave;
            ek = loc.EliteDefeatCount;
            ea = loc.EliteActive;
        }
        else
        {
            return;
        }

        string eliteState = ea ? eOn : (ek > 0 ? eCl : eOff);
        line.Append(' ')
            .Append(eliteTag)
            .Append(kv)
            .Append(eliteState)
            .Append(' ')
            .Append(eWk)
            .Append(kv)
            .Append(ew)
            .Append(' ')
            .Append(eKk)
            .Append(kv)
            .Append(ek);
    }
}
