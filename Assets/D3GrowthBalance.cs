using System;
using UnityEngine;

/// <summary>D3：成长/经济/探索与客户端表现默认（含 HUD/镜头/联机客户端节拍等），对齐 README 数值段落。唯一数据源：<c>Resources/Balance/DefaultD3Growth</c>；现场调参改 JSON，不在场景组件上「硬顶」第二套权威。</summary>
[System.Serializable]
public class D3GrowthBalanceData
{
    [Header("README §5.2 — MaxHP = hpBase + VIT*hpPerVit + Level*hpPerLevel + equipHpBonus")]
    public int hpBase = 100;
    public int hpPerVit = 15;
    public int hpPerLevel = 20;
    public int equipHpBonus;
    [Tooltip("README §5.1：每 1 VIT 提供的 HP 回复百分比（每 10 秒）")]
    [Min(0f)] public float hpRegenPercentPer10SecPerVit = 0.3f;
    [Tooltip("HP 自然回复结算间隔秒（PlayerHealthSimple）")]
    [Min(0.1f)] public float hpRegenTickSec = 1f;
    [Tooltip("README §5.1：每 1 STR 提供的玩家物理防御（近身伤先减防）")]
    [Min(0)] public int playerPhysicalDefensePerStr = 2;
    [Tooltip("README §5.1：每 1 VIT 提供的玩家物理防御（近身伤先减防）")]
    [Min(0)] public int playerPhysicalDefensePerVit = 1;
    [Tooltip("README §5.1：每 1 VIT 提供的玩家法术/元素平面防（法伤即时段先减防；完整抗性表另里程碑）")]
    [Min(0)] public int playerSpellDefensePerVit = 1;
    [Tooltip("README §7：每 1 INT 提供的玩家法术平面防（与 VIT 项叠加；0=仅 VIT 项）")]
    [Min(0)] public int playerSpellDefensePerInt = 0;
    [Tooltip("README §5.1：每 1 AGI 提供的闪避率（命中前判定）")]
    [Min(0f)] public float playerDodgeChancePerAgi = 0.01f;
    [Tooltip("闪避率上限")]
    [Range(0f, 1f)] public float playerDodgeChanceCap = 0.6f;

    [Header("README §5.2 — MaxMP = mpBase + INT*mpPerInt + Level*mpPerLevel + equipMpBonus")]
    public int mpBase = 50;
    public int mpPerInt = 8;
    public int mpPerLevel = 5;
    public int equipMpBonus;

    [Header("README §9 — Weight_max = carryBase + STR*carryPerStr")]
    public float carryBase = 100f;
    public float carryPerStr = 5f;

    [Header("初始四维（未开自由加点前；后续可从存档/服端下沉）")]
    public int startingStr = 10;
    public int startingAgi = 10;
    public int startingInt = 10;
    public int startingVit = 10;

    [Header("README §3.5 — 强化金币曲线（与 PlayerEnhanceSimple 对齐）")]
    public int enhanceGoldBase = 10;
    public int enhanceGoldPerStep = 12;

    [Header("README §2.1 — 每级自由属性点")]
    [Min(0)] public int statPointsPerLevel = 5;

    [Header("README §2.2 / D3 — 满级、升级经验曲线、U/I 单次经验消耗")]
    [Min(1)] public int maxLevel = 50;
    public int xpBasePerLevel = 40;
    public int xpExtraPerLevelStep = 14;
    [Min(1)] public int spendXpToLevelPerPress = 10;
    [Min(1)] public int spendXpToSkillUnlockPerPress = 15;

    [Header("D3 — 红蓝药买/卖/量（与 PlayerInventorySimple 对齐，单件重量同 potionUnitWeight）")]
    [Min(1)] public int hpPotionHealAmount = 45;
    [Min(1)] public int mpPotionRestoreAmount = 35;
    [Min(1)] public int buyPotionGoldCost = 20;
    [Min(1)] public int buyManaGoldCost = 24;
    [Min(1)] public int buyPotionCount = 1;
    [Min(0.01f)] public float potionUnitWeight = 1f;
    [Min(1)] public int sellPotionGold = 8;
    [Min(1)] public int sellManaGold = 10;

    [Header("D3 — 掉落实例（与 DropItemSimple 对齐；重量与背包/药水一致）")]
    [Min(0)] public int dropRollWeightHp = 40;
    [Min(0)] public int dropRollWeightMp = 35;
    [Min(0)] public int dropRollWeightShard = 25;
    [Min(0.01f)] public float shardUnitWeight = 1f;
    [Min(0.01f)] public float lootDefaultUnitWeight = 1f;

    [Header("D3 — 击杀奖励全局倍率（Enemy prefab 基底 × 倍率；精英平加在其后）")]
    [Min(0f)] public float killXpMultiplier = 1f;
    [Min(0f)] public float killGoldMultiplier = 1f;

    [Header("D3 — 玩家钱包/死亡经济（与 PlayerWalletSimple / PlayerHealth 对齐；无存档时起始金用表）")]
    [Min(0)] public int startingGold;
    [Range(0, 100)] public int playerDeathDropGoldPercent = 20;

    [Header("D3 — 仓库 F5~F8 步长（PlayerBankSimple）")]
    [Min(1)] public int bankGoldStep = 50;
    [Min(1)] public int bankPotionStep = 1;

    [Header("D3 — 二阶技能 SP 消耗（O/P，PlayerSkillUnlockSimple）")]
    [Min(1)] public int burstTier2SpCost = 2;
    [Min(1)] public int frostTier2SpCost = 2;

    [Header("D3 — MP 自然回复/秒（PlayerMpSimple；上限仍由公式）")]
    [Min(0f)] public float mpRegenPerSecond = 1.2f;

    [Header("D3 — Q 技能 PlayerSkillBurst")]
    [Min(1)] public int skillBurstMpCost = 20;
    [Min(0.1f)] public float skillBurstCooldownSec = 2.5f;
    [Min(0.1f)] public float skillBurstRadius = 3.2f;
    [Min(1)] public int skillBurstDamagePerHit = 2;
    [Min(0f)] public float skillBurstBurnSec = 3.2f;
    [Tooltip("Burst 施加燃烧后，DOT 每跳伤害（仍走 TakeSpellHit / 法防）")]
    [Min(1)] public int skillBurstBurnDamagePerTick = 1;
    [Tooltip("燃烧 DOT 间隔秒")]
    [Min(0.05f)] public float skillBurstBurnTickIntervalSec = 0.55f;

    [Header("D3 — R 技能 PlayerSkillFrost")]
    [Min(1)] public int skillFrostMpCost = 12;
    [Min(0.1f)] public float skillFrostCooldownSec = 3.5f;
    [Min(0.1f)] public float skillFrostRadius = 2.8f;
    [Min(0f)] public float skillFrostFreezeSec = 2.2f;
    [Tooltip("冰冻脉冲法伤基底，走 SpellDefense；0 = 纯控制")]
    [Min(0)] public int skillFrostDamagePerHit = 2;

    [Header("D3 — 熟练度 PlayerSkillMastery（施放升档 + 每级倍率边率）")]
    [Min(1)] public int masteryBurstCastsBase = 5;
    [Min(0)] public int masteryBurstCastsPerLevel = 3;
    [Min(1)] public int masteryFrostCastsBase = 5;
    [Min(0)] public int masteryFrostCastsPerLevel = 3;
    [Min(0f)] public float masteryBurstDmgPerSkillLevel = 0.08f;
    [Min(0f)] public float masteryFrostFreezePerSkillLevel = 0.06f;

    [Header("D3 — 技能二阶相对一阶倍率（Q 伤害/范围、R 冻时/范围/伤害）")]
    [Min(1f)] public float skillTier2BurstDamageMul = 1.35f;
    [Min(1f)] public float skillTier2BurstRangeMul = 1.15f;
    [Min(1f)] public float skillTier2FrostFreezeMul = 1.35f;
    [Min(1f)] public float skillTier2FrostRangeMul = 1.12f;
    [Min(1f)] public float skillTier2FrostDamageMul = 1.25f;

    [Header("D3 — 公共精英平加/缩放（PublicObjectiveElite）")]
    [Min(1)] public int eliteBonusHp = 10;
    [Min(0)] public int eliteBonusXp = 18;
    [Min(0)] public int eliteBonusGold = 30;
    [Min(1.01f)] public float eliteScaleMul = 1.14f;

    [Header("D3 — 探索/战斗基线（拾取/联机/单机各组件）")]
    [Min(0.1f)] public float interactionPickupRange = 2f;
    [Min(0.1f)] public float playerNetMoveSpeed = 6f;
    [Min(0.1f)] public float playerSoloMoveSpeed = 5f;
    [Min(0.1f)] public float playerMeleeAttackRangeNet = 2.4f;
    [Min(0.1f)] public float playerMeleeAttackRangeSolo = 2f;
    [Tooltip("旧基底：未接入 STR 缩放前的占位；现役普攻见 meleePhyDamagePerStr")]
    [Min(1)] public int playerMeleeDamagePerHit = 1;
    [Tooltip("README §5.2：Atk_phy = STR×该系数（+Buff 后续里程碑）；普攻伤害=max(1, STR×系数)")]
    [Min(1)] public int meleePhyDamagePerStr = 3;
    [Tooltip("普攻基准间隔秒（攻速=1 时）")]
    [Min(0.05f)] public float meleeBaseAttackIntervalSec = 0.5f;
    [Tooltip("README §5.2 攻速边率：间隔 = base/(1+AGI×k)")]
    [Min(0f)] public float meleeAttackSpeedPerAgi = 0.015f;
    [Tooltip("普攻最短间隔秒（防极端 AGI 下过快）")]
    [Min(0.03f)] public float meleeMinAttackIntervalSec = 0.08f;

    [Tooltip("普攻暴率：min(上限, AGI×本系数)；0=关闭暴击")]
    [Min(0f)] public float meleeCritChancePerAgi = 0.005f;
    [Tooltip("暴击时对「已扣物防」伤害的倍率（README 默认 150%）")]
    [Min(1f)] public float meleeCritDamageMul = 1.5f;
    [Tooltip("普攻暴率上限")]
    [Range(0f, 1f)] public float meleeCritChanceCap = 0.75f;

    [Header("D3 — 敌人受击（README §7 物系即时段：攻击−防御后再合成倍率；普攻链优先落地）")]
    [Min(0)] public int enemyDefaultPhysicalDefense = 8;

    [Header("D3 — 敌人法术/元素技能直击与 DOT（减免另列；与物防可分调）")]
    [Min(0)] public int enemyDefaultSpellDefense = 6;

    [Header("D3 — 技能直伤智力平加（Q/R 法伤链；0=关闭；近似 §5.2 元素养成）")]
    [Min(0)] public int spellDirectDamagePerInt = 0;

    [Header("D3 — 单机冲刺（PlayerMoveSimple）")]
    [Min(1f)] public float playerSoloDashMultiplier = 2.2f;
    [Min(0.02f)] public float playerSoloDashDuration = 0.18f;
    [Min(0.02f)] public float playerSoloDashCooldown = 0.85f;

    [Header("D3 — 敌人近身伤默认（EnemyTouchDamageSimple）")]
    [Min(1)] public int enemyTouchDamage = 8;
    [Min(0.05f)] public float enemyTouchDamageInterval = 1.1f;
    [Min(0.1f)] public float enemyTouchRange = 1.25f;

    [Header("D3 — 敌人法术环伤（EnemySpellTouchDamageSimple → PlayerHealthSimple.TakeSpellHit；0=关闭）")]
    [Tooltip("每跳原始法伤；0 则组件 Awake 后关闭")]
    [Min(0)] public int enemySpellTouchDamage = 6;
    [Min(0.05f)] public float enemySpellTouchIntervalSec = 1.35f;
    [Min(0.1f)] public float enemySpellTouchRange = 2.2f;

    [Header("D3 — 玩家复活/再索敌（PlayerHealthSimple）")]
    [Min(0.1f)] public float playerRespawnDelaySec = 1.5f;
    [Min(0f)] public float playerRespawnInvulnerableSec = 3f;
    [Min(0f)] public float playerRespawnNoAggroSec = 3f;
    [Min(0f)] public float playerRespawnEnemyRetreatSec = 3f;
    [Min(0.5f)] public float playerRespawnEnemyRetreatRadius = 7f;
    [Min(0.5f)] public float playerReengageRadiusBase = 7f;
    [Min(1f)] public float playerReengageRadiusMultiplier = 2f;
    [Min(0.05f)] public float playerReengageTargetLockSec = 0.8f;

    [Header("D3 — 联机 Client 位姿合并发送间隔（MultiplayerPlayerSimple）")]
    [Min(0.01f)] public float netClientMoveSendIntervalSec = 0.04f;
    [Min(0f)] public float netScenePlayerDisableGuardWindowSec = 6f;
    [Min(0.05f)] public float netScenePlayerDisableGuardIntervalSec = 0.5f;
    [Min(0.1f)] public float netScenePlayerDuplicateHealIntervalSec = 1.5f;

    [Header("D3 — 敌人追击默认（EnemyChaseSimple）")]
    [Min(0.1f)] public float enemyChaseMoveSpeed = 3.4f;
    [Min(0.5f)] public float enemyChaseDetectRange = 16f;
    [Min(0.1f)] public float enemyChaseStopRange = 1.05f;
    [Min(0.5f)] public float enemyChaseLeashRange = 26f;
    [Min(0.05f)] public float enemyChaseRetargetIntervalSec = 0.35f;
    [Min(0f)] public float enemyChaseTargetLockSec = 0.8f;
    [Min(0f)] public float enemyChaseDetectExitPadding = 0.75f;
    [Min(0f)] public float enemyChaseLeashExitPadding = 1f;
    [Min(0f)] public float enemyChaseNoPushBuffer = 0.08f;
    [Min(0f)] public float enemyChaseKeepDistanceDeadband = 0.02f;
    [Min(0f)] public float enemyTouchKeepDistanceExtraRange = 0.2f;

    [Header("D3 — PvP 展示阈值（PlayerPvpSimple，规则仍以区域为准）")]
    [Min(1)] public int pvpRedNameKillThreshold = 3;
    [Min(0.5f)] public float pvpHudHintSeconds = 2.5f;

    [Header("D3 — 队伍人数上限（PartyRuntimeState / PartyPlaceholder）")]
    [Min(1)] public int partyMaxMembers = 5;

    [Header("D3 — 城内圆半径 / 安全区兜底（WorldZoneConfig / SafeZone / PlayerAreaState）")]
    [Min(0.5f)] public float worldCityRadiusDefault = 16f;
    [Min(0.5f)] public float safeZoneDefaultRadius = 8f;
    [Min(0.2f)] public float playerAreaZoneConfigPollSec = 1f;

    [Header("D3 — 传送门交互默认值（AreaPortalSimple Awake）")]
    [Min(0.1f)] public float portalUseRangeDefault = 2f;
    [Min(0.5f)] public float portalYToleranceDefault = 4f;

    [Header("D3 — 强化减伤（PlayerEnhance；与 enhanceGold* 同文件）")]
    [Range(0f, 0.2f)] public float enhanceDamageReductionPerStep = 0.02f;
    [Range(0f, 0.9f)] public float enhanceMaxDamageReduction = 0.6f;

    [Header("D3 — HUD W1 闭环观测窗口秒（DebugHudSimple）")]
    [Min(1f)] public float w1ClosureWindowSeconds = 300f;

    [Header("D5 — POST /sync 尝试上限（PlayerStateExportSimple.Network）")]
    [Min(1)] public int d5SyncPostMaxAttempts = 2;
    [Min(1f)] public float d5SyncPost429DefaultWaitSec = 2f;

    [Header("D5 — 客户端审计队列长度帽（ServerAuditLogSimple）")]
    [Min(16)] public int auditQueueMaxItems = 256;

    [Header("D3 — 联机纯 Client 重连（MultiplayerBootstrapSimple）")]
    [Min(1)] public int netClientReconnectMaxAttempts = 3;
    [Min(1f)] public float netClientReconnectWindowSec = 60f;
    [Min(0.5f)] public float netClientReconnectRetryDelaySec = 5f;
    [Min(1f)] public float netClientListEmptyStopPollingSec = 25f;
    [Min(0.05f)] public float netClientListEmptyPollStepSec = 0.4f;
    [Min(0.05f)] public float netClientReconnectArmDelaySec = 0.8f;
    [Min(1f)] public float netClientSpawnPlayerWaitSec = 20f;

    [Header("D3 — 纯 Client 生命周期等待本地玩家（NetcodeClientLifecycle）")]
    [Min(1f)] public float netClientWaitLocalPlayerLifecycleSec = 12f;

    [Header("D3 — 传送门服端判定松弛倍率（AreaPortalSimple.IsInUseRangeForPortal serverSlack）")]
    [Min(1f)] public float portalServerSideRangeSlackMul = 2.5f;

    [Header("D3 — B2 公共目标 HUD/提示（PublicObjectiveLastToast / WorldHint / WaveDisplay）")]
    [Min(0.5f)] public float publicObjectiveToastVisibleSec = 4.5f;
    [Min(0.05f)] public float publicObjectiveWorldHintRefreshSec = 0.2f;
    [Min(0.1f)] public float publicObjectiveJoinRadiusScale = 2.2f;
    [Min(0.1f)] public float publicObjectiveJoinRadiusMin = 4f;

    [Header("D3 — DebugHud 轮询间隔（P1A 表未覆盖时的回落）")]
    [Min(0.05f)] public float hudEnemyCountPollIntervalSec = 0.33f;
    [Min(0.05f)] public float hudNearEnemyStatusPollIntervalSec = 0.28f;
    [Min(0.5f)] public float hudNearEnemyScanRadiusDefault = 18f;

    [Header("D3 — DebugHud 文本截断（P1A 未配置字段时的回落；与 P1AContentConfig 同名语义）")]
    [Min(4)] public int hudTruncErrCodeMaxDefault = 36;
    [Min(4)] public int hudTruncErrCodeKeepDefault = 33;
    [Min(8)] public int hudTruncDetailMaxDefault = 56;
    [Min(8)] public int hudTruncDetailKeepDefault = 53;

    [Header("D3 — B2 房间聊天（ChatRoomStateSimple；权威在服）")]
    [Min(16)] public int chatRoomMaxPayloadChars = 200;
    [Min(1)] public int chatRoomHudHistoryLines = 3;

    [Header("D3 — DebugHud 聊天行截断（防 HUD 爆行）")]
    [Min(8)] public int hudChatRoomDebugTruncateChars = 96;
    [Min(8)] public int hudChatSystemDebugTruncateChars = 72;

    [Header("D3 — 跟随镜头默认（CameraFollowSimple Awake；[Client-Side Expression]）")]
    [Min(0.01f)] public float cameraFollowSmoothDefault = 8f;
    public float cameraFollowOffsetX;
    [Min(0.01f)] public float cameraFollowOffsetY = 8f;
    public float cameraFollowOffsetZ = -10f;
    [Min(0.1f)] public float cameraFollowMinDistanceDefault = 5f;
    [Min(0.1f)] public float cameraFollowMaxDistanceDefault = 18f;
    [Min(0.1f)] public float cameraFollowOrbitSensitivityDefault = 3f;
    [Min(0.1f)] public float cameraFollowZoomSensitivityDefault = 3f;
    public float cameraFollowMinPitchDefault = -15f;
    public float cameraFollowMaxPitchDefault = 75f;
    [Min(0.05f)] public float cameraFollowLookAtYOffsetDefault = 1.2f;

    [Header("D3 — 单点刷怪默认复生延迟（EnemySpawnerSimple Awake）")]
    [Min(0.1f)] public float enemySpawnerRespawnDelayDefault = 3f;
}

/// <summary>加载并缓存 <see cref="D3GrowthBalanceData"/>；缺失或解析失败时用代码默认值。</summary>
public static class D3GrowthBalance
{
    public const string DefaultResourcePath = "Balance/DefaultD3Growth";

    static D3GrowthBalanceData _cached;

    /// <summary>
    /// JsonUtility 遇非法 JSON 往往不抛异常；缺字段时数值型为 0，可能产出 maxLevel=0 等哑数据。
    /// 台账「D3 现场验收」第 1 条：不因表损坏刷红错 —— 仅 Development 下 Warning，回落类内默认值。
    /// </summary>
    static bool ParsedGrowthLooksValid(D3GrowthBalanceData d)
    {
        if (d == null) return false;
        if (d.maxLevel < 1) return false;
        if (d.spendXpToLevelPerPress < 1 || d.spendXpToSkillUnlockPerPress < 1) return false;
        if (d.statPointsPerLevel < 1) return false;
        return true;
    }

    public static D3GrowthBalanceData Load()
    {
        if (_cached != null)
            return _cached;

        TextAsset ta = Resources.Load<TextAsset>(DefaultResourcePath);
        D3GrowthBalanceData parsed = null;

        if (ta != null && !string.IsNullOrWhiteSpace(ta.text))
        {
            try
            {
                parsed = JsonUtility.FromJson<D3GrowthBalanceData>(ta.text);
            }
            catch (Exception e)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("[D3] DefaultD3Growth JsonUtility exception — using code defaults. Resources/" + DefaultResourcePath + " msg=" + e.Message);
#endif
                parsed = null;
            }
        }

        if (parsed != null && ParsedGrowthLooksValid(parsed))
            _cached = parsed;
        else
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (ta != null && !string.IsNullOrWhiteSpace(ta.text))
                Debug.LogWarning("[D3] DefaultD3Growth JSON missing fields or failed sanity check — using code defaults. Resources/" + DefaultResourcePath);
#endif
            _cached = new D3GrowthBalanceData();
        }

        return _cached;
    }

    /// <summary>Editor / 单测：强制下次 <see cref="Load"/> 重新读 Resources。</summary>
    public static void ClearLoadCache()
    {
        _cached = null;
    }

    public static int ComputeMaxHp(D3GrowthBalanceData d, int vit, int level, int equipExtra = 0)
    {
        level = Mathf.Max(1, level);
        vit = Mathf.Max(0, vit);
        return Mathf.Max(1, d.hpBase + vit * d.hpPerVit + level * d.hpPerLevel + equipExtra + d.equipHpBonus);
    }

    public static int ComputeMaxMp(D3GrowthBalanceData d, int intellect, int level, int equipExtra = 0)
    {
        level = Mathf.Max(1, level);
        intellect = Mathf.Max(0, intellect);
        return Mathf.Max(1, d.mpBase + intellect * d.mpPerInt + level * d.mpPerLevel + equipExtra + d.equipMpBonus);
    }

    public static float ComputeCarryWeight(D3GrowthBalanceData d, int str)
    {
        str = Mathf.Max(0, str);
        return Mathf.Max(1f, d.carryBase + str * d.carryPerStr);
    }

    /// <summary>README §5.2 物理普攻基底（防御阶跃另列）；敌人无防时即等价扣血。</summary>
    public static int ComputeMeleePhysicalDamage(D3GrowthBalanceData d, int strength)
    {
        strength = Mathf.Max(0, strength);
        int k = Mathf.Max(1, d.meleePhyDamagePerStr);
        return Mathf.Max(1, strength * k);
    }

    /// <summary>普攻间隔（秒）：base / (1 + AGI×k)，并受最短间隔钳制。</summary>
    public static float ComputeMeleeAttackInterval(D3GrowthBalanceData d, int agility)
    {
        agility = Mathf.Max(0, agility);
        float baseSec = Mathf.Max(0.05f, d.meleeBaseAttackIntervalSec);
        float k = Mathf.Max(0f, d.meleeAttackSpeedPerAgi);
        float speedMul = 1f + agility * k;
        float sec = baseSec / Mathf.Max(0.01f, speedMul);
        return Mathf.Max(0.03f, d.meleeMinAttackIntervalSec, sec);
    }

    /// <summary>玩家受击前闪避率：min(cap, AGI×k)。</summary>
    public static float ComputePlayerDodgeProbability(D3GrowthBalanceData d, int agility)
    {
        agility = Mathf.Max(0, agility);
        float perAgi = Mathf.Max(0f, d.playerDodgeChancePerAgi);
        float cap = Mathf.Clamp01(d.playerDodgeChanceCap);
        return Mathf.Min(cap, agility * perAgi);
    }

    /// <summary>玩家物理防御：README §5.1 — STR 与 VIT 双项平加，供近身伤链先减防。</summary>
    public static int ComputePlayerPhysicalDefense(D3GrowthBalanceData d, int strength, int vitality)
    {
        strength = Mathf.Max(0, strength);
        vitality = Mathf.Max(0, vitality);
        int perStr = Mathf.Max(0, d.playerPhysicalDefensePerStr);
        int perVit = Mathf.Max(0, d.playerPhysicalDefensePerVit);
        return Mathf.Max(0, strength * perStr + vitality * perVit);
    }

    /// <summary>玩家法术平面防：VIT/INT 双项平加，供敌人侧法伤直伤链先减防（与 <see cref="EnemyHealthSimple.TakeSpellHit"/> 对称）。</summary>
    public static int ComputePlayerSpellDefense(D3GrowthBalanceData d, int intellect, int vitality)
    {
        intellect = Mathf.Max(0, intellect);
        vitality = Mathf.Max(0, vitality);
        int perInt = Mathf.Max(0, d.playerSpellDefensePerInt);
        int perVit = Mathf.Max(0, d.playerSpellDefensePerVit);
        return Mathf.Max(0, intellect * perInt + vitality * perVit);
    }

    /// <summary>README 战斗漏斗第一步（即时段）：max(1, 原始伤害 − 平面防御)。物/法两条防御独立。</summary>
    public static int ApplyFlatMitigationStep(int rawDamage, int flatDefense)
    {
        rawDamage = Mathf.Max(0, rawDamage);
        flatDefense = Mathf.Max(0, flatDefense);
        return Mathf.Max(1, rawDamage - flatDefense);
    }

    /// <summary>物系近战普攻链使用的平面物防。</summary>
    public static int ApplyPhysicalDefenseToDamage(int rawPhysicalDamage, int physicalDefense) =>
        ApplyFlatMitigationStep(rawPhysicalDamage, physicalDefense);

    /// <summary>技能直伤 / 燃烧 DOT 等走的平面「法术」减免（完整元素抗性表另里程碑）。</summary>
    public static int ApplySpellDefenseToDamage(int rawSpellDamage, int spellDefense) =>
        ApplyFlatMitigationStep(rawSpellDamage, spellDefense);

    /// <summary>智力对 Q/R 直伤的每点加成；表为 0 时不贡献。</summary>
    public static int SpellIntellectFlatBonus(D3GrowthBalanceData d, int intellect)
    {
        int k = d.spellDirectDamagePerInt;
        if (k <= 0)
            return 0;
        intellect = Mathf.Max(0, intellect);
        return intellect * k;
    }

    /// <summary>Q Burst 单目标滚动伤害（单机 <see cref="PlayerSkillBurstSimple"/> 与联机服端 Burst 同式）。</summary>
    public static int ComputeBurstRolledDamage(
        D3GrowthBalanceData d,
        int damagePerHit,
        int intellect,
        float burstMasteryDamageMultiplier,
        int burstTier)
    {
        damagePerHit = Mathf.Max(1, damagePerHit);
        intellect = Mathf.Max(0, intellect);
        float dmgMult = burstMasteryDamageMultiplier > 0f ? burstMasteryDamageMultiplier : 1f;
        int tier = Mathf.Max(1, burstTier);
        float tierMul = tier >= 2 ? Mathf.Max(1f, d.skillTier2BurstDamageMul) : 1f;
        return Mathf.Max(
            1,
            Mathf.RoundToInt(damagePerHit * dmgMult * tierMul) + SpellIntellectFlatBonus(d, intellect));
    }

    /// <summary>R Frost 单目标滚动伤害（可为 0；与 <see cref="PlayerSkillFrostSimple"/> / 联机服端 Frost 同式）。</summary>
    public static int ComputeFrostRolledDamage(D3GrowthBalanceData d, int frostDamagePerHit, int intellect, int frostTier)
    {
        intellect = Mathf.Max(0, intellect);
        int tier = Mathf.Max(1, frostTier);
        float tierMul = tier >= 2 ? Mathf.Max(1f, d.skillTier2FrostDamageMul) : 1f;
        float scaledBase = Mathf.Max(0, frostDamagePerHit) * tierMul;
        return Mathf.Max(
            0,
            Mathf.RoundToInt(scaledBase) + SpellIntellectFlatBonus(d, intellect));
    }

    /// <summary>R 冰冻持续秒数（<see cref="PlayerSkillFrostSimple"/> / 联机服端 Frost 同式；熟练只拉伸冻结，不进入 <see cref="ComputeFrostRolledDamage"/>）。</summary>
    public static float ComputeFrostFreezeDurationSeconds(
        D3GrowthBalanceData d,
        float freezeDurationFromSkill,
        int frostTier,
        float frostMasteryFreezeDurationMultiplier)
    {
        int tier = Mathf.Max(1, frostTier);
        float tierFreezeMul = tier >= 2 ? Mathf.Max(1f, d.skillTier2FrostFreezeMul) : 1f;
        float m = frostMasteryFreezeDurationMultiplier > 0f ? frostMasteryFreezeDurationMultiplier : 1f;
        return Mathf.Max(0f, freezeDurationFromSkill * tierFreezeMul * m);
    }

    /// <summary>Q Burst <c>OverlapSphere</c> 半径（二阶 × <c>skillTier2BurstRangeMul</c>；单机/联机同式）。</summary>
    public static float ComputeBurstOverlapRadius(D3GrowthBalanceData d, float skillRadius, int burstTier)
    {
        int tier = Mathf.Max(1, burstTier);
        float mul = tier >= 2 ? Mathf.Max(1f, d.skillTier2BurstRangeMul) : 1f;
        return Mathf.Max(0.1f, skillRadius) * mul;
    }

    /// <summary>R Frost <c>OverlapSphere</c> 半径（二阶 × <c>skillTier2FrostRangeMul</c>）。</summary>
    public static float ComputeFrostOverlapRadius(D3GrowthBalanceData d, float skillRadius, int frostTier)
    {
        int tier = Mathf.Max(1, frostTier);
        float mul = tier >= 2 ? Mathf.Max(1f, d.skillTier2FrostRangeMul) : 1f;
        return Mathf.Max(0.1f, skillRadius) * mul;
    }

    /// <summary>
    /// Debug HUD：近战普攻 <c>OverlapSphere</c> 半径（表 <c>playerMeleeAttackRangeNet</c> / <c>playerMeleeAttackRangeSolo</c>；
    /// 与挂 <see cref="MultiplayerPlayerSimple"/> 时用联机键一致）。
    /// </summary>
    public static float GetMeleeAttackRangeForHud(D3GrowthBalanceData d, GameObject playerRoot)
    {
        if (playerRoot != null && playerRoot.GetComponent<MultiplayerPlayerSimple>() != null)
            return Mathf.Max(0.1f, d.playerMeleeAttackRangeNet);
        return Mathf.Max(0.1f, d.playerMeleeAttackRangeSolo);
    }

    /// <summary>
    /// Debug HUD：与 <see cref="PlayerMoveSimple"/> / <see cref="MultiplayerPlayerSimple"/> 覆写口径一致（表 <c>playerSoloMoveSpeed</c> / <c>playerNetMoveSpeed</c>）。
    /// </summary>
    public static float GetPlayerMoveSpeedForHud(D3GrowthBalanceData d, GameObject playerRoot)
    {
        if (playerRoot != null && playerRoot.GetComponent<MultiplayerPlayerSimple>() != null)
            return Mathf.Max(0.1f, d.playerNetMoveSpeed);
        return Mathf.Max(0.1f, d.playerSoloMoveSpeed);
    }

    /// <summary>Debug HUD：与 <see cref="PlayerPickupSimple"/> <c>Awake</c> 的 <c>interactionPickupRange</c> 一致（单机/联机同表）。</summary>
    public static float GetInteractionPickupRangeForHud(D3GrowthBalanceData d) =>
        Mathf.Max(0.1f, d.interactionPickupRange);

    /// <summary>单次普攻骰暴的命中概率（表 <c>meleeCritChancePerAgi=0</c> 时为 0）。</summary>
    public static float MeleeCritProbability(D3GrowthBalanceData d, int agility)
    {
        float perAgi = d.meleeCritChancePerAgi;
        if (perAgi <= 0f)
            return 0f;
        agility = Mathf.Max(0, agility);
        return Mathf.Min(Mathf.Clamp01(d.meleeCritChanceCap), agility * perAgi);
    }

    /// <summary>
    /// 漏斗第 3 步（近战普攻）：在 <see cref="ApplyPhysicalDefenseToDamage"/> 之后对即时段骰暴击。
    /// <paramref name="meleeCritChancePerAgi"/> 为 0 时关闭；仅物理普攻链，非法术技能。
    /// </summary>
    public static int ApplyMeleeCritAfterPhysicalArmor(D3GrowthBalanceData d, int agility, int damageAfterPhysicalArmor)
    {
        float p = MeleeCritProbability(d, agility);
        if (p <= 0f)
            return damageAfterPhysicalArmor;

        float mul = Mathf.Max(1f, d.meleeCritDamageMul);
        if (UnityEngine.Random.value < p)
            return Mathf.Max(1, Mathf.RoundToInt(damageAfterPhysicalArmor * mul));
        return damageAfterPhysicalArmor;
    }

    /// <summary>
    /// HUD 期望伤害：物防减法后对暴率取期望（相对 <see cref="enemyDefaultPhysicalDefense"/> 的预览；**非权威**）。
    /// </summary>
    public static float ExpectedMeleeDamageAfterArmor(D3GrowthBalanceData d, int agility, int damageAfterPhysicalArmor)
    {
        float p = MeleeCritProbability(d, agility);
        float mul = Mathf.Max(1f, d.meleeCritDamageMul);
        return damageAfterPhysicalArmor * (1f + p * (mul - 1f));
    }

    /// <summary>当前等级槽内，升到下一整级所需总经验（README：每级 = 基础 + (L-1)×斜率）。</summary>
    public static int XpNeededForLevel(D3GrowthBalanceData d, int currentLevel)
    {
        currentLevel = Mathf.Max(1, currentLevel);
        return Mathf.Max(1, d.xpBasePerLevel + (currentLevel - 1) * d.xpExtraPerLevelStep);
    }
}
