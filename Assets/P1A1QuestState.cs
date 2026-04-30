using UnityEngine;

/// <summary>P1-A-1：统计「死前已冰+已 Q」的合规击杀数（每只怪至多计一次）。</summary>
public class P1A1QuestState : MonoBehaviour
{
    public static P1A1QuestState Instance { get; private set; }

    public int targetKills = 5;
    public int compliantKills;

    [Tooltip("若赋值，则 Start 时覆盖 targetKills（数据驱动）。")]
    public P1AContentConfig contentConfig;

    [Tooltip("HUD 一行提示：推荐先 R 冰冻再 Q 火球（与 README P1-A-1 一致）。")]
    public bool showOrderHintInHud = true;

    public bool IsComplete => compliantKills >= targetKills;

    /// <summary>与验收说明一致的简短操作提示（优先读 <see cref="P1AContentConfig.p1OrderHintShort"/>）。</summary>
    public string OrderHintShort
    {
        get
        {
            P1AContentConfig cfg = contentConfig != null ? contentConfig : P1AContentConfig.TryLoadDefault();
            if (cfg != null && !string.IsNullOrWhiteSpace(cfg.p1OrderHintShort))
                return cfg.p1OrderHintShort.Trim();
            return "R冰→Q火";
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (contentConfig == null)
            contentConfig = P1AContentConfig.TryLoadDefault();
        if (contentConfig != null)
        {
            targetKills = Mathf.Max(1, contentConfig.p1TargetKills);
            showOrderHintInHud = contentConfig.p1ShowOrderHintInHud;
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void RegisterKillFromMark(MonsterP1A1Mark mark)
    {
        if (mark == null || !mark.IsCompliantForKill)
            return;
        if (compliantKills < targetKills)
            compliantKills++;
    }
}

/// <summary>P1-A 关卡表（ScriptableObject）。Create / EpochOfDawn / P1-A Content Config。与本脚本同文件，避免独立 .cs 未编入程序集时出现 CS0246。</summary>
[CreateAssetMenu(fileName = "P1AContentConfig", menuName = "EpochOfDawn/P1-A Content Config")]
public class P1AContentConfig : ScriptableObject
{
    public const string DefaultResourcePath = "P1A/DefaultP1AContent";

    static P1AContentConfig _cachedDefault;

    public static P1AContentConfig TryLoadDefault()
    {
        if (_cachedDefault == null)
            _cachedDefault = Resources.Load<P1AContentConfig>(DefaultResourcePath);
        return _cachedDefault;
    }

    /// <summary>清静态缓存；进入 Play 前调用可避免「快速进 Play」仍持旧版 Resources 资产（如刚修改 hudBaiBuLines）。</summary>
    public static void ClearDefaultCache() => _cachedDefault = null;

    [Header("P1-A-1 验收")]
    [Min(1)] public int p1TargetKills = 5;
    [Tooltip("有 contentConfig 时由表覆盖；用于 HUD 的 R 冰 / Q 火顺序提示行。")]
    public bool p1ShowOrderHintInHud = true;
    [Tooltip("HUD 中 `tip:` 后展示的短句；可改语系或键位说明。")]
    public string p1OrderHintShort = "R冰→Q火";

    [Header("P1-A HUD（DebugHud 首行，可选）")]
    [Tooltip("为 false 时首行不显示 Wv: 波次进度（场景里仍可刷波）。")]
    public bool p1ShowWaveInHud = true;
    [Tooltip("任务 HUD 主标签，如 P1，显示为 标签:进度 或 标签:完成尾字。")]
    public string p1HudTag = "P1";
    [Tooltip("完成时 标签 后的部分，如 OK、完成。")]
    public string p1HudCompleteSuffix = "OK";
    [Tooltip("顺序提示前标签，如 tip，显示为 空格+tip+:+提示句。")]
    public string p1HudTipLabel = "tip";
    [Tooltip("波次段主标签，如 Wv。")]
    public string p1WaveHudTag = "Wv";
    [Tooltip("波次全清时的尾字，如 Done。")]
    public string p1WaveHudDone = "Done";
    [Tooltip("无波次组件时的尾字，如 --。")]
    public string p1WaveHudNone = "--";

    [Header("B2 / Day1 — 公共目标（HUD、勿与 p1HudTag 任务行混用）")]
    [Tooltip("公共目标行主标签。如 公目")] public string publicObjectiveHudTag = "公目";
    [Tooltip("参与态键。如 态")] public string publicObjectiveParticipationKey = "态";
    [Tooltip("波次表尚未全清。如 进行中")] public string publicObjectiveInProgress = "进行中";
    [Tooltip("波次表已全清且未循环。如 本段已清场")] public string publicObjectiveSegmentDone = "本段已清场";
    [Tooltip("无 WaveSpawner 时。如 无")] public string publicObjectiveNoSpawner = "无";
    [Tooltip("在参与圈内且于野外。如 参与")] public string publicObjectiveParticipating = "参与";
    [Tooltip("于野外但不在参与圈内。如 观战")] public string publicObjectiveObserving = "观战";
    [Tooltip("在城镇/安全区（不计入参与圈收益态展示）。如 城内")] public string publicObjectiveInTown = "城内";
    [Tooltip("行内 波次 段标签，与 Party/坐标 区分。如 波 或 Wv")] public string publicObjectiveWaveKey = "波";
    [Tooltip("精英 小标题。如 精")] public string publicObjectiveEliteTag = "精";
    [Tooltip("公共精英已刷新。如 在场")] public string publicObjectiveEliteActive = "在场";
    [Tooltip("累计已击败>0 且当前无在场精英。观战/远处也能看出本段已清。如 已清")] public string publicObjectiveEliteCleared = "已清";
    [Tooltip("从未产生过本段公共精英事件（计为 0）且无在场。如 暂无")] public string publicObjectiveEliteDown = "暂无";
    [Tooltip("精英事件 波序号。如 序")] public string publicObjectiveEliteWaveKey = "序";
    [Tooltip("累计击败数。如 计")] public string publicObjectiveEliteKillKey = "计";
    [Tooltip("B2·Day2：公共精英被击败时全端短提示。{0}=波序号，{1}=累计击败数。")]
    public string publicObjectiveEliteDefeatToast = "公目:精英已击败 序{0} 计{1}";

    [Header("B2 / Day1 — 公共目标世界提示（场景刷怪中心上方 · [Client-Side Expression]）")]
    [Tooltip("为 true 时由 WaveSpawner 自动挂 PublicObjectiveWorldHintSimple；纯表现。")]
    public bool publicObjectiveWorldHintEnabled = true;
    public float publicObjectiveWorldHintCenterYOffset = 3.5f;
    public Vector3 publicObjectiveWorldHintScale = new Vector3(0.014f, 0.014f, 0.014f);
    [Range(18, 96)] public int publicObjectiveWorldHintFontSize = 44;

    [Header("DebugHud 行首标签（十项，可本地化）")]
    [Tooltip("① 敌人数前缀，如 E")] public string hudTagEnemy = "E";
    [Tooltip("② 坐标前缀，如 P")] public string hudTagPos = "P";
    [Tooltip("③ MP 前缀")] public string hudTagMp = "MP";
    [Tooltip("④ HP 前缀")] public string hudTagHp = "HP";
    [Tooltip("⑤ D4 流程主标签，如 Flow")] public string hudTagFlow = "Flow";
    [Tooltip("⑥ 流程完成尾字，如 OK")] public string hudFlowCompleteSuffix = "OK";
    [Tooltip("⑦ 流程步进前缀，如 S（与步号拼成 S0）")] public string hudFlowStepPrefix = "S";
    [Tooltip("⑧ 区域前缀，如 A")] public string hudTagArea = "A";
    [Tooltip("⑨ PvP 前缀，如 PvP")] public string hudTagPvp = "PvP";
    [Tooltip("⑩ 队伍前缀，如 Party")] public string hudTagParty = "Party";
    [Tooltip("B2·Day4 房间聊天摘要前缀，如 ch")] public string hudTagChatRoom = "ch";
    [Tooltip("B2·Day4 系统消息摘要前缀，如 sys")] public string hudTagChatSys = "sys";

    [Header("DebugHud 第二组（十项 · Q/R、Drop、PvP 值）")]
    [Tooltip("⑪ 技能 Q 行标签")] public string hudTagSkillQ = "Q";
    [Tooltip("⑫ 技能 R 行标签")] public string hudTagSkillR = "R";
    [Tooltip("⑬ CD 已好时的尾字，如 rdy")] public string hudSkillRdyText = "rdy";
    [Tooltip("⑭ CD 秒数后单位，如 s → 1.2s")] public string hudSkillSecondsSuffix = "s";
    [Tooltip("⑮ 掉落模式主标签，如 Drop")] public string hudTagDrop = "Drop";
    [Tooltip("⑯ 队伍均分尾字，如 Share")] public string hudDropShareSuffix = "Share";
    [Tooltip("⑰ 各自拾取尾字，如 Solo")] public string hudDropSoloSuffix = "Solo";
    [Tooltip("⑱ PvP 开状态字，如 On")] public string hudPvpValueOn = "On";
    [Tooltip("⑲ PvP 关状态字，如 Off")] public string hudPvpValueOff = "Off";
    [Tooltip("⑳ 红名括注，如 Red → (Red)")] public string hudPvpRedLabel = "Red";
    [Tooltip("Day5：在安全区/城内时 Off 的附注，如 Safe")] public string hudPvpValueSafeBlocked = "Safe";

    [Header("DebugHud 第三组（十项 · 装备/成长/背包/金·BG）")]
    [Tooltip("㉑ 装等测试，如 Eq")] public string hudTagEq = "Eq";
    [Tooltip("㉒ 等级前缀，如 Lv（接数字无冒号）")] public string hudTagLv = "Lv";
    [Tooltip("㉓ 经验缺口前缀，如 差")] public string hudTagDiff = "差";
    [Tooltip("㉔ 技能点，如 SP")] public string hudTagSp = "SP";
    [Tooltip("㉕ 经验池，如 XP池")] public string hudTagXpPool = "XP池";
    [Tooltip("㉖ 负重，如 W")] public string hudTagWeight = "W";
    [Tooltip("㉗ 红药瓶数，如 H")] public string hudTagInvHp = "H";
    [Tooltip("㉘ 蓝药瓶数，如 M")] public string hudTagInvMp = "M";
    [Tooltip("㉙ 金币，如 G")] public string hudTagGold = "G";
    [Tooltip("㉚ 金库存金，如 BG（BH/BM 见第四组）")] public string hudTagBankGold = "BG";

    [Header("DebugHud 第四组（十项 · 金库尾/熟练/阶/强化/D5 摘要）")]
    [Tooltip("㉛ 金库红药，如 BH")] public string hudTagBankHp = "BH";
    [Tooltip("㉜ 金库蓝药，如 BM")] public string hudTagBankMp = "BM";
    [Tooltip("㉝ 火球熟练小写，如 q")] public string hudTagMasteryQ = "q";
    [Tooltip("㉞ 冰熟练小写，如 r")] public string hudTagMasteryR = "r";
    [Tooltip("㉟ 火球阶，如 Qt")] public string hudTagUnlockQt = "Qt";
    [Tooltip("㊱ 冰阶，如 Rt")] public string hudTagUnlockRt = "Rt";
    [Tooltip("㊲ 强化阶前导，如 +")] public string hudTagEnhancePlus = "+";
    [Tooltip("㊳ 减伤，如 DR")] public string hudTagDr = "DR";
    [Tooltip("㊴ D5 行头，如 D5")] public string hudTagD5Banner = "D5";
    [Tooltip("㊵ 校验 val，如 val")] public string hudTagVal = "val";

    [Header("DebugHud 第五组（十项 · D5 行短标签，不含 e/dt/ifm）")]
    [Tooltip("㊶ 警告聚合 SrvVal")] public string hudTagSrvVal = "SrvVal";
    [Tooltip("㊷ codes 前缀")] public string hudTagCodes = "codes";
    [Tooltip("㊸ NET 告警")] public string hudTagNet = "NET";
    [Tooltip("㊹ HLT 健康探针")] public string hudTagHlt = "HLT";
    [Tooltip("㊺ AudC 审计类")] public string hudTagAudC = "AudC";
    [Tooltip("㊻ syn POST 状态")] public string hudTagSyn = "syn";
    [Tooltip("㊼ f2 末次 HTTP 码")] public string hudTagF2 = "f2";
    [Tooltip("D5 一键验收结论（pass/warn/retry/fail/idle）")] public string hudTagF2Verdict = "f2v";
    [Tooltip("㊽ etg 预取")] public string hudTagEtg = "etg";
    [Tooltip("㊾ d 耗时 ms")] public string hudTagD5Dur = "d";
    [Tooltip("㊿ r 重试次数")] public string hudTagD5Retry = "r";

    [Header("DebugHud 第六组（十项 · e/dt/ifm、val 尾字、装等、NET 尾、etg 值、ms）")]
    [Tooltip("🄀 错误码 e:")] public string hudTagErr = "e";
    [Tooltip("🄁 详情 dt:")] public string hudTagDetail = "dt";
    [Tooltip("🄂 已发 If-Match 仅词 ifm，无冒号")] public string hudTagIfm = "ifm";
    [Tooltip("🄃 etg 后段，如 pre")] public string hudTagEtgPre = "pre";
    [Tooltip("🄄 d: 后时长单位，如 ms")] public string hudTagD5DurationUnit = "ms";
    [Tooltip("🄅 val 成功，如 ok")] public string hudTagValOk = "ok";
    [Tooltip("🄆 val 失败，如 no")] public string hudTagValNo = "no";
    [Tooltip("🄇 装等可装")] public string hudTagEqOk = "+";
    [Tooltip("🄈 装等不可装")] public string hudTagEqBad = "X";
    [Tooltip("🄉 NET: 后仅告警符，如 !")] public string hudTagNetAlertSuffix = "!";

    [Header("DebugHud 第七组（十项 · 帮助整段 / 无坐标 / SrvVal 内外围字 / 百分号）")]
    [TextArea(1, 3)] [Tooltip("A11-① 升级/购药 提示整段（showDebugDetails）")] public string hudHelpTextProgress = "  U升 I点 O解Q2 P解R2 1红 2蓝 B买红 N买蓝 V卖药";
    [TextArea(1, 3)] [Tooltip("A11-② 金库 F5～F8 整段")] public string hudHelpTextBank = "  F5存金 F6取金 F7存药 F8取药";
    [TextArea(1, 2)] [Tooltip("A11-③ 队伍小键盘 整段（Day3：* 切换掉落共享）")] public string hudHelpTextParty = "  小键盘+/-队伍 *切换掉落共享";
    [TextArea(1, 2)] [Tooltip("A11-④ 回车=` 房间；` = 系统（仅 Host）")] public string hudHelpTextChat = "  Enter房间 `系统(H)";
    [TextArea(1, 4)] [Tooltip("A11-⑤ 热键 Keys 行（前换行在代码中）")] public string hudHelpTextKeys = "Keys Esc退出 F1健康 ;审计类 F4审计 F3整包 F2POST F12状态 F9存 F10读 F11清";
    [TextArea(1, 2)] [Tooltip("A11-⑥ 死亡提示 整段")] public string hudHelpTextDeath = "  死亡掉金并复活";
    [Tooltip("A11-⑦ 无玩家时 P: 后占位")] public string hudTagPosNA = "N/A";
    [Tooltip("A11-⑧ SrvVal 后低告警字母")] public string hudTagSrvValLowMark = "L";
    [Tooltip("A11-⑨ SrvVal 后高告警字母")] public string hudTagSrvValHighMark = "H";
    [Tooltip("A11-⑩ 减伤 DR: 后百分号")] public string hudTagPercent = "%";

    [Header("DebugHud 第八组（十项 · 标点/括注/冒号/间距）")]
    [Tooltip("A12-① 近敌摘要左括")] public string hudPunctNearEnemyL = "[";
    [Tooltip("A12-② 近敌摘要右括")] public string hudPunctNearEnemyR = "]";
    [Tooltip("A12-③ MP/HP/P1/波 分子分母 分隔")] public string hudPunctStatSlash = "/";
    [Tooltip("A12-④ e、dt 等截断尾")] public string hudPunctEllipsis = "…";
    [Tooltip("A12-⑤ Flow: 后或 E: 与 P: 之间宽空白")] public string hudPunctDebugWideSpace = "  ";
    [Tooltip("A12-⑥ D5 行头与后续段之间的单空")] public string hudPunctD5BannerSpace = " ";
    [Tooltip("A12-⑦ PvP 红名左括")] public string hudPunctPvpParenOpen = "(";
    [Tooltip("A12-⑧ PvP 红名右括")] public string hudPunctPvpParenClose = ")";
    [Tooltip("A12-⑨ 各标签:值 的冒号（E、P、MP 等通用于 DebugHud 首屏）")] public string hudPunctLabelColon = ":";
    [Tooltip("A12-⑩ 首行 P1 的 tip: 中 tip 后冒号，若置空回退 A12-⑨")] public string hudPunctTipColon = ":";

    [Header("DebugHud 第九组（十项 · 多开/LAN 联机一行）")]
    [Tooltip("A13-① 多开行头，如 MP")] public string hudMultiTag = "MP";
    [Tooltip("A13-② 未起服/断连时 尾缀（如空格+省略号）")] public string hudMultiDisconnectedTail = " …";
    [Tooltip("A13-③ 角色 host")] public string hudMultiRoleHost = "host";
    [Tooltip("A13-④ 角色 cli")] public string hudMultiRoleCli = "cli";
    [Tooltip("A13-⑤ 角色 srv")] public string hudMultiRoleSrv = "srv";
    [Tooltip("A13-⑥ 角色 auto")] public string hudMultiRoleAuto = "auto";
    [Tooltip("A13-⑦ 关/断：off 等小写态")] public string hudMultiStateOff = "off";
    [Tooltip("A13-⑧ 已连时 连接数 标签 cc")] public string hudMultiCcKey = "cc";
    [Tooltip("A13-⑨ MP 段与「cc:」块之间的单空")] public string hudMultiBlockGap = " ";
    [Tooltip("A13-⑩ 地址与端口 之间的冒号（127.0.0.1:port）")] public string hudMultiAddrPortSep = ":";

    [Header("DebugHud 第十组（十项 · 近敌燃/冰/秒 + 联调 e·dt 截断宽）")]
    [Tooltip("A14-① 近敌 HUD 燃烧前缀")] public string hudVfxBurnLabel = "燃";
    [Tooltip("A14-② 近敌 HUD 冰冻前缀")] public string hudVfxFrostLabel = "冰";
    [Tooltip("A14-③ 与秒数 F1 连写的单位")] public string hudVfxTimeUnitS = "s";
    [Tooltip("A14-④ 燃+冰 双显时 中间空白")] public string hudVfxDualMidSpace = " ";
    [Min(1)] [Tooltip("A14-⑤ e: 全宽超过则头截断，长度阈值")] public int hudTruncErrCodeMax = 36;
    [Min(1)] [Tooltip("A14-⑥ e: 头保留字符数 + A12-④")] public int hudTruncErrCodeKeep = 33;
    [Min(1)] [Tooltip("A14-⑦ dt: 全宽超阈值则头截断")] public int hudTruncDetailMax = 56;
    [Min(1)] [Tooltip("A14-⑧ dt: 头保留 + A12-④")] public int hudTruncDetailKeep = 53;
    [Tooltip("A14-⑨ 多开行 label:value 的冒号（与 A12-⑨ 可独立）")] public string hudMultiKeyColon = ":";
    [Tooltip("A14-⑩ 已连时 cc:n 后、地址 前的单空")] public string hudMultiCcToAddrSpace = " ";

    [Header("DebugHud 第十一组（十项 · 刷新节流 / 近敌半径 / 数值显示精度）")]
    [Min(0.05f)] [Tooltip("A13-① E: 计数刷新间隔（秒）")] public float hudEnemyCountIntervalSec = 0.33f;
    [Min(0.05f)] [Tooltip("A13-② 近敌状态刷新间隔（秒）")] public float hudNearEnemyStatusIntervalSec = 0.28f;
    [Min(1f)] [Tooltip("A13-③ 近敌扫描半径")] public float hudNearEnemyScanRadius = 18f;
    [Range(0, 3)] [Tooltip("A13-④ 坐标 x/z 小数位")] public int hudPosDecimals = 1;
    [Range(0, 3)] [Tooltip("A13-⑤ Q/R CD 小数位")] public int hudCdDecimals = 1;
    [Range(0, 2)] [Tooltip("A13-⑥ DR 百分比小数位")] public int hudDrPercentDecimals = 0;
    [Range(0, 3)] [Tooltip("A13-⑦ 近敌燃/冰剩余秒小数位（EnemyStatusEffects）")] public int hudNearEnemySecondsDecimals = 1;
    [Range(0, 4)] [Tooltip("A13-⑧ 经验进度 x/y 小数位（一般为 0）")] public int hudXpProgressDecimals = 0;
    [Range(0, 4)] [Tooltip("A13-⑨ 负重显示小数位（CeilToInt 前预留）")] public int hudWeightDecimals = 0;
    [Range(0, 4)] [Tooltip("A13-⑩ D5 时长 d: 数值小数位")] public int hudD5DurationDecimals = 0;

    [Header("A-12 百步：可选行（hudBaiBuLines[100]，空则跳过，showDebugDetails 时顺序追加在 HUD 末尾）")]
    [Tooltip("索引 0～99；每非空行追加一换行后输出；可本地化/附言/自测用。")]
    public string[] hudBaiBuLines = new string[100];

    [Header("A-14 续百步：第二组可选行（hudBaiBuLines2[100]）")]
    [Tooltip("索引 100～199 的扩展槽位；读取时紧接第一组后追加。")]
    public string[] hudBaiBuLines2 = new string[100];

    [Header("P1-A-2 波次")]
    public int[] waveEnemyCounts = { 2, 3, 5 };
    [Min(0f)] public float delayBetweenWaves = 2f;
    [Min(0.1f)] public float waveSpawnRingRadius = 8f;
    [Tooltip("清完全部波后是否从第一波循环；`WaveSpawnerSimple.loopWaves` 有配置时从表拉取。")]
    public bool loopWaves = false;

    [Header("P1-A-1 野外分散刷怪（WildSpawner）")]
    [Min(1)] public int wildEnemyCount = 5;
    [Min(0.1f)] public float wildRingRadius = 8f;

    [Header("通用（生成位置）")]
    [Tooltip("敌人生成时的 Y；`WaveSpawnerSimple.spawnY` 与 `P1A1WildSpawner` 圆周布怪共用。")]
    [Min(-50f)] public float enemySpawnHeightY = 0.5f;

    [Header("P1-A-3 小 Boss 默认")]
    [Min(1f)] public float miniBossHpMultiplier = 3f;
    [Range(0.05f, 0.95f)] public float bossPhaseHpFraction = 0.5f;
    [Min(1f)] public float bossPhaseChaseSpeedMul = 1.25f;
}
