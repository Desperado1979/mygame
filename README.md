# 破晓纪元

> **PC 端大型多人在线角色扮演（MMORPG）** — 无缝大世界、刷装养成、自由 PK。

## 产品规约（PC 客户端）

**工程与开发计划：** 本仓库根目录即 Unity 客户端工程（2022 LTS + URP；若从上级 monorepo 查看，路径常写作 **`EpochOfDawn/`**）。**`README.MD`** 为设计规约与进度总账。Git 忽略规则见本仓库根目录 **`.gitignore`**。

## 当前开发进度（AI 代记录）

- **记录规则：** 你每次汇报“做到哪一步”，我就更新本区，不丢历史。
- **状态定义：** `未开始` / `进行中` / `已完成` / `阻塞`。
- **补漏执行口径（AI / 续写代码）：** **只补**本 README **「当前开发进度」主表、D0/D1/D2 子任务表、P1-A 子任务表中**状态列为 `已完成`**，而 **Unity 工程 / `EpochOfDawn/server` / `EpochOfDawn/docs` 中仍缺失** 的实现或文档。**不**因「今日开发日志」长文、附录规约、或 **`进行中` / `未开始`** 的条目，自行新增系统或大改架构；若需推进未完成任务，须先由负责人**把对应行改为 `已完成` 或单独指令**后再动代码。
- **里程碑原则（服务端可信）：** 每个里程碑在**收尾或复盘**时，**回答**「**这一步离服务端可信更近一步了吗？**」——若更近：说明是靠 **审计链、同步协议、校验规则、权威边界**（何者由服裁定）等之一；若**未更近**（例如纯手感、纯美术、纯单机验收）：**允许**，但须在备注或日志里**写清**，避免误以为已具备服务端保障。与 **`.cursorrules` 权威后端原则**及 **D5 三件套（State / Audit / Request）** 长期对齐。
- **当前里程碑：** **P3**（**P3-0 / P3-1 / P3-2** 已完成：**审计写入 + persist_sync 聚合 + 可选 `SrvVal_*` 策略拒收/告警**）。**P2-D5**、**P1-A** 已收口。**P1-A** 已于 **2026-04-21** 标 **已完成**。D2 及 D3–D5 历史能力见下表与各节日志。

| 日期 | 阶段 | 目标 | 状态 | 结果/备注 |
|------|------|------|------|-----------|
| 2026-04-21 | **D16 续十六** | **`ETag`** / **`If-Match`**：**`GET /state`**、**`POST /sync`**、**`POST /rehearsal/apply-patch`** 乐观并发；**`GET /rehearsal/etag-concurrency`**；**`SYNC_ETAG_DISABLED`** | **已完成** | 见 **「本次续写 · D16 续十六」**；**Redis** 仍 **【未】** |
| 2026-04-21 | **D15 续十五** | **`REHEARSAL_PATCH_WRITE=1`**：**`POST /rehearsal/apply-patch`** 写 **`state`**；**`GET /rehearsal/apply-patch`** 说明；与 **`POST /sync`** 同校验链 | **已完成** | 见 **「本次续写 · D15 续十五」**；**生产 PATCH** 仍非目标 |
| 2026-04-21 | **D14 续十四** | **`SYNC_AUDIT_STATE_STRICT`**：**audit 重放尾** vs **`state`** 不一致时 **400**；**`GET /rehearsal/audit-state-strict`**；**`audit_validate.cjs`** | **已完成** | 见 **「本次续写 · D14 续十四」**；**深度交叉** 收口一档 |
| 2026-04-21 | **D13 续十三** | **`POST /rehearsal/validate-patch`**（白名单 **JSON Patch** 排练）；**`patch_validate_rehearsal.cjs`**；**`npm run validate-patch-rehearsal`** CI；**`GET /rehearsal/patch-strategy`** 增补规则 | **已完成** | 见 **「本次续写 · D13 续十三」**；**真 PATCH 写盘** 仍 **【未】** |
| 2026-04-21 | **D12 续十二** | **`SYNC_IDEMPOTENCY_PERSIST`**：**`Idempotency-Key`** **200** 缓存落盘 **`data/idempotency-cache.json`**；**`GET /rehearsal/idempotency-persist`**；**`GET /health`** 暴露路径 | **已完成** | 见 **「本次续写 · D12 续十二」**；多实例仍建议 **Redis** |
| 2026-04-21 | **D11 续十一** | **`SYNC_WARNING_SRVVAL_BRIDGE`** / **`WARNING_CODE_TO_SRVVAL_JSON`** → **`auditSummary.srvValFromWarnings`**；**`GET /rehearsal/warning-srvval-bridge`**；**Grafana** 示例 **`examples/grafana-persist-sync-minimal.json`** | **已完成** | 见 **「本次续写 · D11 续十一」**；roadmap **7/8** 再收口 **【未】** |
| 2026-04-21 | **D10 续十** | **合规 bundle** **`/rehearsal/compliance-bundle`**；**`LOG_REDACT_PII`**；**`audit-export-bundle`**（S3 式 manifest）；**`check-schema-snapshot`** CI；Unity **EditMode** **`AuditSummaryHudPreview`** | **已完成** | 见 **「本次续写 · D10 续十」**；**10×20** 波 **10** 收口 |
| 2026-04-21 | **D9 续九** | **`SYNC_ISSUED_AT_MAX_SKEW_SEC`**；**`replayObservation`**；**`/rehearsal/patch-strategy`**；**`check-schema-version`** CI；**`metrics-archive`**；Unity **429 重试** **`r:`** | **已完成** | 见 **「本次续写 · D9 续九」**（断网后已补文档） |
| 2026-04-21 | **D8 续八** | **`durationMs`/`httpStatus` 全路径 metrics**；**`X-Sync-Duration-Ms`**；**`sync-summary.latencyMs`**；**`state_version_vs_schema_mismatch`**；**`p3-d5-waves-10x20.md`** | **已完成** | 见 **「本次续写 · D8 续八」**；**10×20** 为滚动计划，非单次写完 |
| 2026-04-21 | **D7 续七** | **`/metrics/sync-summary`**、**`/rehearsal/mock-sync-200`**、**`audit_replay_cli`**、**`.env.example`**；Unity **`syn:`** 标签 | **已完成** | 见 **「本次续写 · D7 续七」**；roadmap **7/8/9** 组再收口若干 **【未】** |
| 2026-04-21 | **D6 续六** | **`persist_sync`**：维护/限流/幂等/HMAC/Staging；**`docker-compose`**；**`merge_metrics_ndjson`**；**`k6-smoke.js`** | **已完成** | 见 **「本次续写 · D6 续六」**；roadmap 第 **7** 组再收口若干 **【未】** |
| 2026-04-21 | **D5 续五** | **`audit_validate`** 重复 **`seq`** + **`SrvVal_*`** 低告警；**`/metrics/prometheus`**；**`d5-validate.yml`**；**`SrvVal_PartyReject`** | **已完成** | 见 **「本次续写 · D5 续五」**；roadmap 第 **7/8/9** 组部分 **【已】** |
| 2026-04-21 | **P3-2** | **`persist_sync`**：可选 **`SrvVal_*`** 拒收 / 阈值 / 仅告警；拒收**不落盘** | **已完成** | **`REJECT_SRVVAL_AUDIT`** 等见 **`persist_sync`** 启动日志与 **`GET /health`**；与 **`REJECT_HIGH_WARNINGS`** 独立 |
| 2026-04-21 | **P3-1** | **`persist_sync`**：**`auditSummary`** + metrics；Unity HUD **`SrvVal:n`** | **已完成** | 按 **`audit[].category`** 计数；**不**单独因该类拒收（可观测性）；与 **P3-0** 成对 |
| 2026-04-21 | **P3-0** | **非法操作 → 审计**：Q/R 在 **CD / MP 不足** 时 **`SrvVal_IllegalOperation`** | **已完成** | **`ServerAuditLogSimple.CategorySrvValIllegalOperation`**；**`PlayerSkillBurstSimple` / `PlayerSkillFrostSimple`** 写入 **`F4/F3`** 包；服务端仍权威，客户端仅记录「未放行」 |
| 2026-04-21 | **P2-D5** | **D5 客户端**：可维护性 + **Schema 契约注释** + **校验响应观测** | **已完成** | **P2-D5-1 / P2-D5-2 均已完成**：`Export` 对齐 **`server/schemas/*.schema.json`** 与版本常量；**`validation.ok`** → **`LastSyncValidationOk`** + HUD **`val:ok/no`**；权威仍在服务端 |
| 2026-04-18 | **P1-A** | **内容/手感主线**（波次 · 小 Boss · 成长反馈） | **已完成** | 2026-04-21：子任务 P1-A-0～5 全部 **已完成**（见下表与「本次续写」）；**纯单机验收**，未新增服务端权威战斗 |
| 2026-04-13 | D0 | 建立新工程工作流与每日可追踪记录 | 已完成 | 已安装 Unity、创建工程、首次 Windows Build 成功 |
| 2026-04-14 | D1 | 单场景垂直切片验收 | 已完成 | 见下节「D1 验收结论」 |
| 2026-04-14 | D2-1 | 技能条占位（MP + CD + 非普攻 Q） | 已完成 | 你已确认 `D2-1DONE` |

### D0 子任务看板（已完成）

| 子任务ID | 任务 | 状态 | 备注 |
|----------|------|------|------|
| D0-1 | 安装 Unity Hub + Unity Editor（LTS / Supported） | 已完成 | Unity 2022 LTS 已安装可用 |
| D0-2 | 新建 3D URP 项目 `EpochOfDawn`（本地） | 已完成 | Unity 项目已创建并进入编辑器 |
| D0-3 | 打一版 Windows 本地可执行并运行成功 | 已完成 | Build 可启动；当前为空场景（仅天空与空地） |

### D1 子任务看板（已完成）

| 子任务ID | 任务 | 状态 | 备注 |
|----------|------|------|------|
| D1-1 | 放置基础场景（Plane + Capsule + 俯视相机） | 已完成 | 可见地面与角色替身 |
| D1-2 | 角色 WASD 移动（脚本挂载 + Build 验证） | 已完成 | 你已确认 `D1-2 DONE` |
| D1-3 | 相机跟随角色（平滑） | 已完成 | 你已确认 `D1-3DONE` |
| D1-4 | 怪物替身与基础碰撞（Capsule 敌人） | 已完成 | 玩家与敌人碰撞有效，不再穿模 |
| D1-5 | 普攻按键触发（近距离命中判定） | 已完成 | 空格触发命中与未命中判定已验证 |
| D1-6 | 敌人血量与死亡销毁（最小战斗闭环） | 已完成 | 你已确认 `D1-6完成` |
| D1-7 | 敌人自动重生（固定点位） | 已完成 | 你已确认 `D1-7完成` |
| D1-8 | 最小 HUD（位置与敌人数量） | 已完成 | 你已确认 `D1-8 完成` |
| D1-9 | 最小掉落（敌人死亡后生成拾取物） | 已完成 | 你已确认 `D1-9 done` |
| D1-10 | 掉落拾取（按 E 拾取） | 已完成 | 你已确认 `d1-10 done` |
| D1-11 | D1 阶段打包验收（Build & Run） | 已完成 | 全链路可在 Windows 包体中复现 |

### D1 验收结论

- **目标达成：** 在单场景内形成 **「移动 → 普攻 → 击杀 → 掉落 → 拾取 → 敌人重生」** 的最小可玩闭环，满足附录 D 对 **D1 垂直切片** 的意图（先玩法手感，再扩系统）。
- **工程形态：** 使用 **Unity 2022 LTS + URP**，工程目录 **`EpochOfDawn/`**；已添加 **`EpochOfDawn/.gitignore`**，避免将 `Library/` 等大目录提交 GitHub。
- **刻意未做（留 D2+）：** 正文中的 **分系攻击−防御公式、服务端权威战斗、技能 CD/MP、冰冻/燃烧/毒/嗜血全量状态、装备随机与力量门槛表** 等——避免在切片阶段过度耦合，便于后续按规约替换实现。

### D2 计划（下一里程碑）

**目标：** 在 D1 闭环之上，引入 **更接近正文** 的战斗与状态层：至少 **1 个带 CD/MP 的技能占位**；**2 种状态效果**（建议优先 **燃烧 + 冰冻** 或 **毒 debuff + 冰冻**）；**装备耐性 / 力量需求** 的数据结构（可先 UI 或 Debug 面板展示，不做完整掉落表）。

| 子任务ID | 任务 | 状态 | 备注 |
|----------|------|------|------|
| D2-1 | 技能条占位：CD + MP + 至少 1 个非普攻技能 | 已完成 | 你已确认 `D2-1DONE`；`PlayerMpSimple` + `PlayerSkillBurstSimple`（Q）+ `DebugHudSimple` 可选引用 |
| D2-2 | 状态最小子集：实现 2 种（与第 11 节对齐方向） | 已完成 | 已实现燃烧 DOT + 冰冻控制；Q/R 进入 HUD 与战斗循环 |
| D2-3 | 装备数据占位：四耐性 + 力量需求字段 + 1 件测试装 | 已完成 | `EquipmentDataSimple` + `PlayerEquipmentDebugSimple` + 测试装 |
| D2-4 | GitHub 就绪：根仓库或子目录 `git init`、首提交、确认无 >100MB 单文件 | 已完成 | 已完成首轮提交流程与远端同步 |
| D2-5 | `docs/` 重建：`getting-started.md`（安装 Unity、打开工程、Build） | 已完成 | 文档已落到 `EpochOfDawn/docs/getting-started.md` |

### P2-D5 子任务看板（D5 边界显式化 + 客户端可维护性 · **已完成**）

| 子任务ID | 任务 | 状态 | 里程碑原则（服务端可信） |
|----------|------|------|--------------------------|
| P2-D5-1 | **`PlayerStateExportSimple`** 拆 **`Input`** / **`SyncResponseParse`** partial；**`Network`** 仅保留 HTTP 与快照落盘 | **已完成** | **更近一步**：把「服务端返回的校验摘要」从网络协程里拆出，并在 **`SyncResponseParse`** 文件头注明 **权威在服务端**、客户端只解析展示 |
| P2-D5-2 | **`Export`**：`PlayerStateExportSimple.Export.cs` 顶层 XML + **`ClientSyncRequestSchemaVersion`** 等常量；嵌套类型对应 **`client_sync_request` / `player_state` / `audit_event`** Schema；**`SyncResponseParse`** 增加 **`validation.ok`** → **`LastSyncValidationOk`**；**`DebugHudSimple`** 显示 **`val:ok` / `val:no`** | **已完成** | **更近一步**：请求形状与 Schema 可在代码侧交叉索引；响应 **`validation.ok`** 仅观测，与 **`persist_sync.cjs`** 200/400 体一致 |

### P3 子任务看板（服务端可信 · 当前执行）

| 子任务ID | 任务 | 状态 | 里程碑原则（服务端可信） |
|----------|------|------|--------------------------|
| P3-0 | **拒绝施法**（CD / MP）→ **`ServerAuditLogSimple`** 类别 **`SrvVal_IllegalOperation`** | **已完成** | **更近一步**：与 **`.cursorrules` §3** 对齐；审计进 **F3 整包**，便于服务端日后对比「客户端自认未放行」；**非**服务端已判决 |
| P3-1 | **`persist_sync`**：`POST /sync` 响应与 **metrics** 增加 **`auditSummary`**（按 **`audit[].category`** 计数）；Unity 解析 **`SrvVal_IllegalOperation`** → HUD **`SrvVal:n`** | **已完成** | **更近一步**：服务端对审计条目不做法务裁决，仅 **聚合观测**；与客户端 P3-0 成对 |
| P3-2 | **`REJECT_SRVVAL_AUDIT=1`**（拒收含任意 **`SrvVal_*`** 的 **`audit[]`**）、**`SRVVAL_REJECT_CATEGORIES`**（名单）、**`SRVVAL_REJECT_THRESHOLD_JSON`**（按类计数阈值）、**`SRVVAL_ALERT_THRESHOLD_JSON`**（仅告警）；**`POST /sync`** 在拒收路径**不**落盘（与 **`high_warning_block`** 一致） | **已完成** | **更近一步**：策略在 **`persist_sync`**；与 **`REJECT_HIGH_WARNINGS`** 独立；默认全关不影响本地排练 |

### P1-A 子任务看板（内容 / 手感 · **已完成** · 2026-04-21）

**路线说明：** 在现有 **移动 / 战斗 / 技能 / 状态 / 装备占位 / 存档** 之上，先做 **可演示的关卡目标与反馈**，暂不展开联机会话与服务器权威战斗。

| 子任务ID | 任务 | 状态 | 备注 |
|----------|------|------|------|
| P1-A-0 | 路线确认：P1 选 A（内容手感），不优先 B（联机会话） | 已完成 | 2026-04-18 |
| P1-A-1 | **验收标准已定稿**：野外 · **R 冰冻定身** + **Q 火球至少命中** · 击杀 5 只分散怪 · **死法随意** | **已完成** | 本地 Play 自测通过；HUD 见 **`P1:x/5` → `P1:OK`**（调试 HUD 首行） |
| P1-A-2 | **波次 / 敌潮**：复用现有敌人与重生逻辑，抽「波次表」或轻量 `WaveSpawner`（可调参） | **已完成** | **`SampleScene`** 已挂 **`P1A_WaveSpawner`**（`WaveSpawnerSimple` + **`Enemy-01`**）；默认 **`P1A_WildRing` 关闭**，避免与 P1-A-1 五只一次性刷重叠；验 P1-A-1 时再勾选 **`P1A_WildRing`** |
| P1-A-3 | **小 Boss**：较普通怪更高生存或一条阶段触发（技能与动画能复用则只调数值与 AI 状态） | **已完成** | **`Resources/P1A/Enemy-MiniBoss`** + **`P1MiniBossSceneBootstrap`**（野外生成）；**`P1MiniBossSimple`** + **`P1BossPhaseSimple`**；可调 **`P1AContentConfig`** |
| P1-A-4 | **成长可见**：与现有等级 / 装备 / 技能点联动，局内或本局结束有明确反馈（HUD 或 Debug 面板一行目标进度） | **已完成** | **`DebugHudSimple`**：首行 **P1** / **Wv**；次行 **`Lv x y/need 差remain` `XP池` `SP`**（**`PlayerProgressSimple`** 已引用或运行时从 **`player`** 解析） |
| P1-A-5 | **里程碑验收**：Windows **Build & Run** + 录屏或清单可复现 | **已完成** | **EpochOfDawn → Build Windows Player (P1-A-5)** → **`Build/P1-A5-Windows/EpochOfDawn.exe`**；用户确认 **可不关编辑器**、运行 **好用**（2026-04-21） |

#### 下一阶段工作计划（P1-A · 对齐设计哲学 §1「数据驱动」）

1. **数据表驱动 P1 关卡**：用 **`P1AContentConfig`**（ScriptableObject）集中维护「P1 目标击杀数、波次表、野外半径、小 Boss 倍率/阶段阈值」；**不**把数值写死在各脚本里，便于策划调参与后续热更方向。
2. **验收顺序（大纲）**：**P1-A-1**（五只合规击杀）→ **P1-A-2**（波次）→ **P1-A-3**（小 Boss 数值/阶段）→ **P1-A-4**（HUD 成长反馈）→ **P1-A-5**（Windows Build）。
3. **与「刷怪 / 技能为王」一致**：本阶段只验证 **R/Q 控制 + 击杀成长**；**不**扩展任务系统、**不**做服务端权威战斗（与 P1-A 路线说明一致）。
4. **并行 D5 排练**：继续用 **F12/F4/F3 + F2 POST** 做联调；**不**把 D5 观测面扩成与长文日志 1:1（除非单独开里程碑）。

**实施记录（2026-04-20）**：已新增 **`P1AContentConfig`**，并接入 **`P1A1QuestState`**、**`WaveSpawnerSimple`**、**`P1A1WildSpawner`**、**`P1MiniBossSimple`**、**`P1BossPhaseSimple`**；在 Project 中 **Create → EpochOfDawn → P1-A Content Config** 生成资源后，拖到上述组件的 **`contentConfig`** 槽即可。**（续）** 仓库内已带 **`Resources/P1A/DefaultP1AContent`**：槽位留空时运行时 **`P1AContentConfig.TryLoadDefault()`** 自动采用；**`SampleScene`** 已挂 **`P1A_WildRing`**（`P1A1WildSpawner`），原 **`EnemySpawner`** 根物体默认 **关闭** 以免与「五只分散」验收抢怪。

#### P1-A-1 验收说明（已定稿 · 修订：冰 + 火 + 死法随意）

- **玩法目标（用户口述，已采纳）**：在**野外刷怪点**，对**五只分散的怪**，每只须满足：**至少用冰冻技能使其进入冰冻（定身）一次** → **至少被火球类技能命中一次** → **之后死法随意**（普攻/燃烧/再补技能均可）。  
- **工程对应**：**冰冻** = `PlayerSkillFrostSimple`（默认 **R 键**，`skillId: FrostPulse`），对范围内敌人 `ApplyFreeze`；**火球** = `PlayerSkillBurstSimple`（默认 **Q 键**，`skillId: Burst`），范围伤害 + 可挂燃烧。文档/口头可写 **R 冰 / Q 火**。
- **「定身」与代码**：冰冻期间 `MonsterCombatHost.IsFrozen == true`；`MonsterChaseHost` 在冰冻时**不位移**，与「定身」观感一致。每只怪挂 `MonsterP1A1Mark`：`RegisterBurstHit` 由 Q 命中后写入，`RegisterFreeze` 在 `ApplyFreeze` 时写入；`P1A1QuestState` 统计「死前已冰+已 Q」的合规击杀数。
- **计数口径（建议写进任务脚本）**：以 **每只怪各计一次** 为准：该怪在死亡前曾 **Frozen 至少一次** 且曾 **被 Burst 伤害命中至少一次**；**不要求**最后一击来源。五只都满足即 P1-A-1 完成。
- **顺序**：未强制「先冰后火」；若先火后冰也可，但「先冰后火」更符合教学与控场直觉，HUD 提示可按推荐顺序写。
- **分散 / MP**：仍建议多重生点或间距下限；R+Q 均有 MP 与 CD，验收用存档建议蓝量与等级够用。

### 今日开发日志（D3→D5 过渡）

- 已形成 D3 可玩闭环：经验二选一（升级/技能点）、Q/R 熟练、技能二阶解锁、背包重量、买卖药、仓库、强化减伤、死亡掉金复活、本地存档。
- 已完成 D4 壳层主流程：主城/野外分区、安全区规则、PK 开关、队伍与聊天占位、流程验收器（`Flow:OK`）。
- 已完成 D5 首批对接能力：`F12`/`F4`/`F3` 三件套；`persist_sync` 前 Schema + `audit_validate`（金币链 + `inv_*`，并对药水库存尾值 vs `state.inventory` 给 warning）；warning 现为结构化分级（`low/high` + `code` + `message`），`/sync` 200 响应含 `validation.warningSummary` + `validation.warningsByCode`，严格模式 400 响应含 `blockedBy`；每次 `/sync` 会记一条 `server/data/metrics.ndjson` 便于按日期看风险趋势（`npm run metrics-report -- --days 7 --top 10`，或 `GET /metrics/report?playerId=...&groupBy=day|hour`，分桶含 `warnings.topCodes`）；支持 `REJECT_HIGH_WARNINGS=1`（例如 `player_id_mismatch`）与 CLI `--fail-on-high`；`npm run validate` / **`npm run compare-state`**（服务端 `state` vs F12 `player_state_export.json`）离线演练。
- 已完成 D5 首批对接能力：`F12`/`F4`/`F3` 三件套；`persist_sync` 前 Schema + `audit_validate`（金币链 + `inv_*`，并对药水库存尾值 vs `state.inventory` 给 warning）；warning 现为结构化分级（`low/high` + `code` + `message`），`/sync` 200 响应含 `validation.warningSummary` + `validation.warningsByCode`，严格模式 400 响应含 `blockedBy`；每次 `/sync` 会记一条 `server/data/metrics.ndjson` 便于按日期看风险趋势（`npm run metrics-report -- --days 7 --top 10`，或 `GET /metrics/report?playerId=...&groupBy=day|hour`，分桶含 `warnings.topCodes`）；并新增 `GET /health`、`GET /metrics/recent`、`/metrics/report` 的 CSV 与筛选参数（`since/until/rejectedOnly/minSeverity`）以及 `health-check`/`metrics-csv`/`daily-risk` 命令；支持 `REJECT_HIGH_WARNINGS=1`（例如 `player_id_mismatch`）与 CLI `--fail-on-high`；`npm run validate` / **`npm run compare-state`**（服务端 `state` vs F12 `player_state_export.json`）离线演练。
- 监控使用手册已单独整理：`EpochOfDawn/server/RISK_MONITORING_QUICKSTART.md`（按命令/网页直接观测风险，不需要理解代码）。
- 体验增强：敌人追击、受击闪白击退、随机掉落类型（红药/蓝药/材料）、掉落文字与颜色提示。
- 重要说明：掉落吸附默认已关闭（`enableMagnet=false`），保持“掉哪里就是哪里”。
- D5 客户端细化：`POST /sync` 以 **HTTP 2xx** 为成功；解析响应中 `validation` 与 `warningSummary(low/high)`，HUD 单行展示 `SrvVal:…` 并写入快照。
- D5 客户端细化：从响应 JSON 提取 **`warningsByCode`**，HUD 追加 `codes:…`（按次数取前几条 code，长度截断）。
- D5 客户端细化：快照写入 **末次 POST 响应截断正文** + **`warningsByCode_full`**；`LastServerWarnHigh>0` 时 **NET 黄灯** 且最近事件标为告警。

#### 本次续写（2026-04-16，防聊天丢失）

- D5 客户端观测面已补齐：新增并打通 `GET /metrics/alerts/players` 与 `GET /metrics/dashboard` 两条排练链路（DTO、协程、历史、HUD、快照、PlayerPrefs、ContextMenu、文件落盘）。
- 端点状态标签扩展：在原 `MAL` 基础上新增 `MAP`（alerts/players）与 `MDB`（dashboard），并纳入“最近成功时间”统计行与快照导出字段。
- 热键补充：`-` 触发 `GET /metrics/alerts/players`，`=` 触发 `GET /metrics/dashboard`；均已接入 `PlayerHotkeysSimple` 的 Awake 默认值与 PlayerPrefs 持久化。
- 404 根因已定位并处理：问题来自 8787 上运行的是旧/不完整服务进程；重启并切到当前 `persist_sync.cjs` 后端点恢复 200。另已加固客户端 origin 解析，避免 BaseUrl 误填路径时拼出错误路由。
- 运维启动便捷化：`EpochOfDawn/server/package.json` 已新增 `npm run persist`（启动 `tools/persist_sync.cjs`），减少“跑错脚本”概率。
- HUD 帮助面板可读性增强：`H` 面板新增“D5观测热键行”，按启用项动态显示（如 `HLT:; MRC:[ MAP:- MDB:=`），便于联调时快速对照。
- D4 表现层启动：新增 `EpochOfDawn/Assets/PlayerVisualAnimatorSimple.cs`，把现有输入/状态（移动、冲刺、普攻、Q/R、受击、死亡）映射到 Animator 参数，不改战斗逻辑，后续可直接替换人物模型与动画控制器继续推进。
- D4 表现层收口：确认 `Mori_motionAsset` 为 **Humanoid/Avatar 重定向** 用法，模型采用 `Eva_Body_Parts_Prefab`，动作资源借 `Justia`；`PlayerVisualAnimatorSimple` 已改为**纯代码直接播放 Justia 状态名**（不再依赖手工创建 Animator 参数/连线），可直接驱动 `swordIdleLoop / swordAttack01 / swordSkillAttack01 / swordSkillAttack01Fly / swordDamageLow / swordDeath / swordDeathLoop / swordDodge`。
- 已清理实验性脚本：删除 `EpochOfDawn/Assets/EpochPlayerController.cs` 与对应 `.meta`，避免与现有 `PlayerMoveSimple` / `PlayerAttackSimple` / `PlayerSkillBurstSimple` / `PlayerSkillFrostSimple` 冲突。
- 资源结论：`Mori_motionAsset` 当前**没有 Run/Walk 基础移动动作**，属于战斗动作包；因此当前可稳定接入的是待机、攻击、技能、受击、死亡、闪避。后续若要自然跑步，需要补一条额外 Humanoid locomotion 动作，再把 `PlayerVisualAnimatorSimple.moveStateName` 改成该状态名即可。
- D4 表现层继续：`PlayerVisualAnimatorSimple` 现已兼容两种控制器命名方式：若仍用手工 `Player_Runtime`，默认播放 `Idle/Run/AttackState/SkillQState...`；若切回 `Justia` 控制器，会自动切换为 `swordIdleLoop / swordAttack01 / swordSkillAttack01...` 命名。
- 已加自动挂武器逻辑：脚本会在启动时自动查找 `Eva` 骨骼中的 `Bip02 Rhand_Weapon / right_Wmount / Rhand_WeaponOpp / R Hand` 等挂点，并尝试实例化 `simple_sword` 到右手；找不到挂点时会静默跳过，不影响现有玩法。
- 动画播放链已最终收口为 **AnimationClip + PlayableGraph 直驱**：不再依赖 `Justia_animCtrl_ALL` 的自动状态跳转，解决了“开场持续重播动作”的问题；同时已修正 `Eva_Body_Parts Prefab` 的 Avatar 绑定错误，现可稳定播放。
- 为接后续新资源，`PlayerVisualAnimatorSimple` 已补 `Refresh Default Clips` 入口，并支持按常见命名自动搜索 `idle / stand / run / walk / attack / dodge / death` 等动画片段；你把新站立/跑步资源导入后，只要刷新或直接拖到 `idleClip / moveClip` 即可接入。
- 为减少后续调参成本，`PlayerVisualAnimatorSimple` 已支持“动作锁定时长优先读取片段长度”：攻击、技能、闪避、受击、死亡默认跟随 `AnimationClip.length`，减少更换动作资源后手感错位；Inspector 里也会显示当前播放片段名与长度，便于观察。
- 已接入 `CustomPackage` 的站立/跑步：`PlayerVisualAnimatorSimple` 在编辑器自动补默认动作时，会优先从 `Assets/CustomPackage/Animations/` 搜索 `Idle/Run/Walk`（如 `Male_Idle.anim / Male_Run.anim / Woman_run.anim / Male_Walk.anim`），用于替换原先的移动占位 `swordIdleLoop`。

**当前结论（D5 本地联调）**
- `persist_sync` 本地排练所需核心 GET 端点已覆盖：`/health`、`/players`、`/metrics/recent`、`/metrics/report`、`/metrics/players`、`/metrics/codes`、`/metrics/rejections`、`/metrics/anomalies`、`/metrics/alerts`、`/metrics/alerts/players`、`/metrics/dashboard`。
- 下一阶段建议转向：人物模型/表现层接入，或公网联机基础设施（鉴权、会话、分区、部署）收敛，不再在 D5 本地排练层做大改。

#### 本次续写（2026-04-17，进度核查与续写承诺）

- 已完成“根 README + 项目全量文件”交叉核查：`EpochOfDawn/Assets`、`EpochOfDawn/server`、`EpochOfDawn/docs` 的关键内容与里程碑记录基本一致，当前阶段判断为 **D4 稳定 + D5 本地联调可持续**。
- 核查确认：`D4FlowValidatorSimple` 的 `Flow:OK` 流程门可用；`PlayerStateExportSimple` + `ServerAuditLogSimple` + `PlayerHotkeysSimple` 已形成 `F12/F4/F3` 三件套链路；`persist_sync.cjs` 已覆盖 `/health`、`/players`、`/metrics/*`（含 `alerts/players`、`dashboard`）端点。
- 风险提示（已记录待收口）：README 历史段仍混有 Cocos 旧表述，后续 AI 执行以 Unity 现行链路为唯一准绳；`PlayerStateExportSimple` 体量偏大，后续优先做“行为不变”的拆分降风险。
- 下一步执行优先级（P1→P3）：P1 统一文档“唯一有效技术栈/流程入口”；P2 拆分 D5 客户端大脚本（导出/请求/响应/HUD 分层）；P3 开始补最小服务端权威校验闭环（先从伤害与非法操作审计事件入手）。
- 协作约定补充：后续每次推进后，均在本节追加“本次续写（日期）”，确保新对话可无缝续接，不丢进度。

#### 本次续写（2026-04-17，P1/P2 开工）

- 已开始执行 P1/P2：先做“口径统一 + 低风险拆分”。
- P1（文档口径）执行规则固定：所有实现、验收、续写以 `EpochOfDawn/`（Unity 2022.3 LTS + URP）和本节 D0~D5 日志为准；第十部分中的 Cocos/Colyseus 仅作历史归档，不作为当前开发指令来源。
- P2（代码拆分）第一刀已落地：`PlayerStateExportSimple` 已改为 `partial class`，并抽出 D5 观测热键与提示拼接方法到新文件 `EpochOfDawn/Assets/PlayerStateExportSimple.D5Hints.cs`，目标是 **行为不变、降低主文件复杂度**。
- P2（代码拆分）第二刀已落地：再将 HUD 文案与本地联调摘要相关方法（如 `BuildServerValidationSummary`、`BuildRecentEventLines*`、`BuildBeginnerSyncSummary`、`BuildQuickOpsChecklist`）迁移到 `EpochOfDawn/Assets/PlayerStateExportSimple.HudText.cs`，继续保持 **行为不变** 前提下降低主文件体量。
- P2（代码拆分）第三刀已落地：将导出与请求包组装相关方法（`ExportNow`、`ExportAuditNow`、`ExportRequestPayloadNow`、`BuildRequestMeta`）迁移到 `EpochOfDawn/Assets/PlayerStateExportSimple.Export.cs`，`F12/F4/F3` 行为保持不变。
- P2（代码拆分）第四刀已落地：将 D5 联调“网络入口方法”与 ContextMenu 入口（`PullStateFromServerRehearsalNow`、`ProbeServerHealthRehearsalNow`、`ListServerPlayersRehearsalNow`、`FetchServerMetrics*RehearsalNow`）迁移到 `EpochOfDawn/Assets/PlayerStateExportSimple.RehearsalEntrypoints.cs`；协程本体暂留主文件，行为保持不变。
- P2（代码拆分）第五刀：`BuildSyncHudSummary` 从主文件迁移到 `EpochOfDawn/Assets/PlayerStateExportSimple.HudText.cs`。
- P2（代码拆分）第六刀：队列/IO/事件核心方法迁移到 `EpochOfDawn/Assets/PlayerStateExportSimple.IOAndQueue.cs`（含 `SavePendingQueue`、`LoadPendingQueue`、`AppendClientHistoryLine`、`PushRecentEvent`、`ResolveServerOrigin`、`BuildDto`、`Escape`）。
- P2（代码拆分）第七刀：`ClearLastServerValidationState` 与 `ParseAndApplyServerSyncResponse` 迁移到 `EpochOfDawn/Assets/PlayerStateExportSimple.ServerValidation.cs`。
- P2（代码拆分）第八刀：风险与快照能力迁移到 `EpochOfDawn/Assets/PlayerStateExportSimple.SnapshotAndRisk.cs`（含 `TickAutoSnapshotOnRisk`、`WriteSyncSnapshotFile`、`BuildLocalRiskTag` 等）。
- P2（代码拆分）第九刀：调参与面板偏好持久化迁移到 `EpochOfDawn/Assets/PlayerStateExportSimple.Prefs.cs`（`Save/LoadTuningPreset`、`Save/LoadEventPanelPrefs`）。
- P2（代码拆分）第十刀：D5 客户端持久化项迁移到 `EpochOfDawn/Assets/PlayerStateExportSimple.D5Prefs.cs`（`LoadD5ClientPrefsFromPrefs`、`SaveD5ClientPrefsToPrefs`）。
- P2（代码拆分）第十一刀：`SummarizePulledPlayerStateJson` 迁移到 `EpochOfDawn/Assets/PlayerStateExportSimple.ParseLite.cs`。
- P2（代码拆分）第十二刀：`SummarizePostErrorJsonBody` 迁移到 `EpochOfDawn/Assets/PlayerStateExportSimple.ParseLite.cs`。
- P2（代码拆分）第十三刀：`SummarizePersistSyncHealthResponse` 迁移到 `EpochOfDawn/Assets/PlayerStateExportSimple.ParseLite.cs`。
- P2（代码拆分）第十四刀：`SummarizePersistSyncPlayersForHud` 迁移到 `EpochOfDawn/Assets/PlayerStateExportSimple.ParseLite.cs`。
- P2（代码拆分）本轮连续 10 步（不逐条回报）：协程与解析继续分拆，新增 `CoroutinePost`、`CoroutinePullBasic`、`CoroutineMetricsA/B/C`、`ParseMetrics`、`ParseHelpers`、`StatsReset`、`Tuning`，并将 `ExportSyncSnapshotNow` 并入 `SnapshotAndRisk`；`PlayerStateExportSimple.cs` 现仅保留生命周期主流程（`Awake/Reset/Update`）。
- 下个小步（保持行为不变）：继续把 `PlayerStateExportSimple` 按“导出序列化 / 网络请求 / HUD 文案”三块拆分到 partial 文件。

#### 本次续写（2026-04-19，丢失代码补齐 · D5 客户端 + P1-A 工程名对齐）

- **D5 客户端（与日志「已验收」能力对齐）**
  - `PlayerStateExportSimple` 已拆为 **partial**：`PlayerStateExportSimple.cs`（热键与字段）、`PlayerStateExportSimple.Export.cs`（F12/F4/F3、DTO、`BuildDto`/`BuildSyncRequest`）、`PlayerStateExportSimple.Network.cs`（`POST /sync`、解析 `validation.warningSummary`、落盘 **`last_sync_response.json`**、排练 **`GET /metrics/alerts/players`** / **`GET /metrics/dashboard`**）。
  - `PlayerHotkeysSimple` 增补：**F2 → POST /sync**，**`-` / `=` → 两条 metrics GET**（与历史日志 D5 观测热键一致）。
  - `DebugHudSimple` 增加可选引用 **`PlayerStateExportSimple`**、**`P1A1QuestState`**：单行展示 **`SrvVal:LxHy`**、**`codes:`** 截断、**`NET:!`**（高警时）；热键提示已更新。
  - `server/schemas/` 补全 **`player_state.schema.json`**、**`audit_event.schema.json`**，使 **`npm run validate`** 可走完整 **schema: OK**（不再因缺文件退回 `ajv_unavailable`）。
- **P1-A（README 命名与计数链）**
  - 新增 **`MonsterCombatHost`**（`IsFrozen` → `EnemyStatusEffectsSimple`）、**`MonsterChaseHost`**（与 **`EnemyChaseSimple`** 同体标记）、**`MonsterP1A1Mark`**、**`P1A1QuestState`**、**`P1A1WildSpawner`**（圆周分散生成）。
  - **`EnemyHealthSimple`** 自动挂 **`MonsterCombatHost` / `MonsterChaseHost` / `MonsterP1A1Mark`**；死亡时向 **`P1A1QuestState`** 上报合规击杀（死前曾 Q 命中且曾 R 冰冻）。
  - **`PlayerSkillBurstSimple` / `PlayerSkillFrostSimple`** 对命中目标调用 **`RegisterBurstHit` / `RegisterFreeze`**。
- **清理**：删除空壳 **`hp.cs`**。
- **备注**：P1-A-1 玩法验收仍依赖场景内 **`P1A1WildSpawner` / 敌人预制体 / HUD 引用** 的配置；脚本链已齐。

#### 本次续写（2026-04-19 · 续）— 自动绑定与热键去冲突

- **`DebugHudSimple.Start`**：若未在 Inspector 赋值，则从 **`player`** 自动解析 **`PlayerStateExportSimple`**、**`P1A1QuestState`**。
- **`PlayerHealthSimple.Awake`**：若玩家身上尚无 **`P1A1QuestState`**，则 **自动添加**（避免漏挂导致 P1 计数不工作）。
- **`PlayerStateExportSimple`**：新增 **`Awake`/`WireRefs`**，运行时补挂组件后也能正确 **`GetComponent`** 各依赖。
- **热键**：D5 metrics 默认由 **`=` / `-`** 改为 **`[` / `]`**；**`PartyPlaceholderSimple`** 默认改为 **小键盘 `+` / `-`** 调整队伍人数，避免与 D5、与 **`PlayerHotkeysSimple`** 冲突。
- **HUD 文案**：队伍提示改为 **「小键盘+/-队伍」**。

#### 本次续写（2026-04-19 · 续二）— P1-A-2/3/4 工程落地

- **`WaveSpawnerSimple`**：按 **`waveEnemyCounts`** 分波圆周刷敌，清场后 **`delayBetweenWaves`** 再刷下一波；可选 **`loopWaves`**；运行时 **`currentWaveIndex` / `allWavesComplete`** 供 HUD 使用。
- **`P1MiniBossSimple`**：由 **`EnemyHealthSimple.Awake`** 调用 **`ApplyToEnemyHealth`**（早于 **`Start`** 与浮动血条），按 **`hpMultiplier`** 放大 **`maxHp`**。
- **`P1A1QuestState`**：新增 **`IsComplete`**；**`DebugHudSimple`** 达标显示 **`P1:OK`**，并可选展示 **`WaveSpawnerSimple`** 的 **`Wv:`** 进度（**`FindObjectOfType`** 自动找，亦可 Inspector 指定 **`waveSpawner`**）。

#### 本次续写（2026-04-19 · 续三）— 小 Boss 血量时机 + P1-A-5 清单

- **`EnemyHealthSimple.Awake`**：优先应用 **`P1MiniBossSimple`**，避免 **`EnemyFloatingHpSimple`** 首帧读到未放大的 **`maxHp`**。
- **`WaveSpawnerSimple.TotalWaveCount`**：供 HUD **`Wv:`** 使用。
- **P1-A-5**：**`docs/getting-started.md`** 已增加 **Windows Build & Run 验收清单**；进度表 **P1-A-5** 标为 **进行中**（待你本地打包勾选）。

#### 本次续写（2026-04-19 · 续四）— 缺口收敛（代码 + 仍须人工）

**仍须你在编辑器/本机完成的（非脚本可替代）**
- **P1-A-1**：场景里挂 **`P1A1WildSpawner`** 或 **`WaveSpawnerSimple`** 并指定 **`Enemy-01`** 等预制体；否则只有脚本无怪可打。
- **P1-A-5**：按 **`docs/getting-started.md`**「P1-A-5」打 **Windows 包** 跑通一遍。

**本次已补**
- **P1-A-1**：**`P1A1QuestState`** 增加 **`OrderHintShort`**（默认 HUD **`tip:R冰→Q火`**，可关 **`showOrderHintInHud`**）。
- **P1-A-3**：新增 **`P1BossPhaseSimple`**（血量 ≤ 阈值比例时 **`EnemyChaseSimple.moveSpeed`** 乘算，占位「二阶段」）。
- **P1-A-4**：**`DebugHudSimple`** 合并 **等级进度** 为 **`Lv x y/need 差remain`** + **`XP池`** + **`SP`**。
- **D5**：**`GET /health`** 排练（默认热键 **F1**），HUD 摘要 **`HLT:`**（**`PlayerStateExportSimple.LastHealthProbePreview`**）。

#### 本次续写（2026-04-20 · 续五）— P1-A `P1AContentConfig` 数据驱动落地

- **表资源**：Project **Create → EpochOfDawn → P1-A Content Config**，同一 `.asset` 可拖到多个组件 **`contentConfig`**，便于一次调参驱动「目标击杀 / 波次 / 野外圈 / 小 Boss 与阶段追击」。
- **接入点**：**`P1A1QuestState`**、**`WaveSpawnerSimple`**、**`P1A1WildSpawner`**、**`P1MiniBossSimple`**、**`P1BossPhaseSimple`**（与 **「下一阶段工作计划」** 内 **实施记录（2026-04-20）**、`getting-started.md`「P1-A 数据驱动」一致）。
- **仍须本机**：场景中挂上 **Spawner** 与 **敌人预制体**，并完成 **P1-A-5** Windows 包验收（见 **`EpochOfDawn/docs/getting-started.md`**）。

#### 本次续写（2026-04-20 · 续六）— 默认表 + SampleScene P1-A-1 开箱

- **`P1AContentConfig`**：类型定义在 **`P1A1QuestState.cs`** 同文件（避免独立脚本未编入程序集时出现 **CS0246**）。**`DefaultP1AContent.asset`** 的 **`m_Script`** 指向 **`P1A1QuestState.cs.meta`**（GUID `4045dfe4ebef23447bc2c998888819f3`），**`m_EditorClassIdentifier`** 为 **`Assembly-CSharp::P1AContentConfig`** 以区分同文件内的 **`P1A1QuestState`**。
- **`Resources/P1A/DefaultP1AContent.asset`**：随包默认数值表；各消费端在 **`contentConfig == null`** 时调用 **`P1AContentConfig.TryLoadDefault()`**。
- **`SampleScene`**：新增 **`P1A_WildRing`**（`P1A1WildSpawner` + 与旧 **`EnemySpawner`** 相同的敌人预制体）；**`EnemySpawner`** 根物体 **`m_IsActive: 0`**，需要单点循环刷怪时再手动打开。

#### 本次续写（2026-04-20 · 续七）— P1-A-1 玩法自测通过（用户确认）

- **屏幕上应看到什么（调试 HUD）**：合规击杀进行中为 **`P1:合规数/目标数`**（默认 **`P1:0/5` … `P1:5/5`**）；**五只均合规**后显示 **`P1:OK`**。若始终 **`P1:0/5`** 不涨，多为该怪**死前未同时满足**「曾冰冻 + 曾被 Q 伤害命中」；与最后一击无关。
- **后续**：**P1-A-2** 已由工程接入 **`P1A_WaveSpawner`**（见 **续八**）；下一里程碑建议 **P1-A-5** 按 **`docs/getting-started.md`** 打 **Windows 包**。

#### 本次续写（2026-04-20 · 续八）— P1-A-2 场景接入（`P1:OK` 后继续）

- **`SampleScene`**：新增根物体 **`P1A_WaveSpawner`**（**`WaveSpawnerSimple`**，`waveEnemyCounts` 2→3→5，清场间隔与半径来自 **`P1AContentConfig`** / 默认值）；**`P1A_WildRing`** 默认 **未激活**，专做波次时不与一次性五只野外圈抢怪。
- **HUD**：调试 HUD 首行 **`Wv:`** 需 **`DebugHudSimple`** 能解析到 **`WaveSpawnerSimple`**（已改为 **含未激活物体** 的查找）；场上无引用时显示 **`Wv:--`**。三波清完后为 **`Wv:Done`**。
- **建议下一步**：**P1-A-5** Windows **Build & Run**（见 **`EpochOfDawn/docs/getting-started.md`**）；并行可做 **P1-A-3** 小 Boss 预制体验收。

#### 本次续写（2026-04-21）— P1-A 收口（进度写入 + 下一里程碑）

- **进度表更新（AI 写入）**：**P1-A-3 / P1-A-4 / P1-A-5** 标为 **已完成**；**P1-A** 主表行标为 **已完成**（子任务 **P1-A-0～5** 全部齐）。
- **P1-A-3**：**`Resources/P1A/Enemy-MiniBoss.prefab`**（**`P1MiniBossSimple`** + **`P1BossPhaseSimple`**）+ **`P1MiniBossSceneBootstrap`**（**`SampleScene`** 根物体 **`P1A_MiniBossBootstrap`**）；野外点生成，与城内安全区错开。
- **P1-A-5**：**`Assets/Editor/EpochOfDawnBuildP1A5.cs`** 菜单 **EpochOfDawn → Build Windows Player (P1-A-5)**；用户确认 **未关 Unity 编辑器** 直接出包、**EXE 好用**。
- **P1-A-4**：**`DebugHudSimple`** 已具备成长行；**补强**为若 **`progress` 未拖引用** 则从 **`player`** **`GetComponent<PlayerProgressSimple>`**（避免漏绑时不显示 **Lv/XP池/SP**）。
- **里程碑原则（服务端可信）**：本阶段 **未更近** 服务端权威——仍为 **客户端表现 + 本地验收**；后续 **P3** 或 **D5 深化**再收紧审计与校验边界。
- **建议下一步（工程）**：**P2** 继续 **`PlayerStateExportSimple` 等大块脚本** 分拆与可维护性（见上文「下个小步」历史记录）；或按产品优先级开 **新里程碑**（新对话续接会更新「当前要做」）。

#### 本次续写（2026-04-21 · P2-D5 首刀）— 里程碑原则下的阶段分配与落地

- **阶段命名**：**P2-D5** = **P2（可维护性）** ∩ **D5（persist_sync 排练）**；目标：大块脚本分层 + **校验/审计边界在源码可读**，避免与「单机手感」里程碑混淆。
- **P2-D5-1（已完成）**
  - 新增 **`PlayerStateExportSimple.Input.cs`**：仅 **`Update` 热键**（F12/F4/F3/F1/F2/`[`/`]`）。
  - 新增 **`PlayerStateExportSimple.SyncResponseParse.cs`**：`ParseAndApplyServerSyncResponse`、`warningSummary` / `warningsByCode` 解析、**`Truncate`**；**文件头 XML** 标明 **权威在服务端**。
  - **`PlayerStateExportSimple.cs`**：保留字段与 **`WireRefs`**；**`Network.cs`**：仅协程、**`TryWriteSyncSnapshotFile`**、**`ResolveBaseUrl`**。
  - **行为不变**：热键与 POST 响应 HUD 字段与拆分前一致。
#### 本次续写（2026-04-21 · P2-D5-2）— Schema 对齐 + `validation.ok` 观测

- **`PlayerStateExportSimple.Export.cs`**：文件头指向 **`server/schemas/client_sync_request.schema.json`**、**`player_state.schema.json`**、**`audit_event.schema.json`**；公开常量 **`ClientSyncRequestSchemaVersion`**、**`PlayerStateExportSchemaVersion`**、**`ClientSyncMetaSchemaVersion`**、**`SaveSnapshotSchemaVersion`**；嵌套类型补充 **XML** 对应关系。
- **`LastSyncValidationOk`**：解析 **`POST /sync`** 响应中 **`validation.ok`**（与 **`persist_sync.cjs`** 返回体一致）；HTTP 非成功时置 **`null`**。
- **`DebugHudSimple`**：在 **`SrvVal:`** 后追加 **`val:ok`** / **`val:no`**（有解析结果时）。
- **里程碑原则**：仍为 **服务端权威**；客户端不根据 **`val:ok`** 改写玩法状态，仅供排练 HUD / 快照对照。

#### 本次续写（2026-04-21 · P3-0）— `SrvVal_IllegalOperation` 审计占位

- **`.cursorrules` §3**：非法操作需进入审计链；现 **Q/R** 在 **CD 未好** 或 **MP 不足** 时 **`ServerAuditLogSimple.Push(CategorySrvValIllegalOperation, …)`**。
- **常量**：**`ServerAuditLogSimple.CategorySrvValIllegalOperation`**（字符串 **`SrvVal_IllegalOperation`**），与规约命名一致。
- **载荷**：`skillId=…&reason=cooldown|mp_insufficient&…`（便于 F4/F3 文本检索）；**不**改变本地玩法结果（仍不施法）。

#### 本次续写（2026-04-21 · P3-1）— `auditSummary` 与服务端聚合

- **`server/tools/persist_sync.cjs`**：`summarizeAuditByCategory(body.audit)` → **`auditSummary`: `{ total, byCategory }`**；写入 **`metrics.ndjson`**（经 **`metricsBase`**）；**200 / 400 high_warning_block** 响应均带 **`auditSummary`**。
- **Unity**：**`LastAuditCategoryPreview`**（如 **`SrvVal:3`**）；**`DebugHudSimple`** 在 **`val:`** 后展示；**HTTP 失败** 时清空。
- **原则**：服务端仍 **不** 因 `SrvVal_IllegalOperation` 单独拒收（除非并入既有 high-warning 策略）；本步为 **可观测性**。

#### 本次续写（2026-04-21 · P3 批量推进 · ~20 步）

1. **`ServerAuditLogSimple`**：新增类别常量 **`SrvVal_InventoryFull`**、**`SrvVal_PickupDenied`**、**`SrvVal_BankReject`**、**`SrvVal_WalletReject`**、**`SrvVal_UnlockReject`**（与 **`SrvVal_IllegalOperation`** 并列，便于 **`auditSummary.byCategory`** 分桶）。
2. **`PlayerPickupSimple`**：归属保护 → **`PickupDenied`**；超重拾取失败 → **`InventoryFull`**。
3. **`PlayerBankSimple`**：存取金/药各拒绝路径 → **`BankReject`**；取款时背包满 → **`InventoryFull`**。
4. **`PlayerEnhanceSimple`**：无钱包 / 金币不足 → **`WalletReject`**（带 **`needGold` / `haveGold`**）。
5. **`PlayerSkillUnlockSimple`**：无 **`PlayerProgressSimple`** 或 SP 不足 → **`UnlockReject`**。
6. **`PlayerInventorySimple.TryBuyHpPotion` / `TryBuyMpPotion`**：超重 / 金币不足 → **`InventoryFull`** / **`WalletReject`**。
7. **`PlayerStateExportSimple.SyncResponseParse`**：**`BuildAuditSummaryHudPreview`** 输出 **`tot:`** + **`SV/Inv/Pk/Bn/W/Un`** 缩写（与 **`persist_sync`** 的 **`byCategory` 键名**一致）。
8. **`persist_sync.cjs`**：新增 **`aggregateAuditCategoriesFromRows`**；**`GET /metrics/audit-categories?days=7&playerId=`** 返回 **`rowsWithSummary` / `total` / `byCategory`**；启动日志增加一行说明。
9. **`docs/getting-started.md`**：补充 **audit-categories** 观测说明。
10. **里程碑原则**：本批仍为 **客户端记录 + 服务端聚合观测**，**不**自动因新类别拒收 **`/sync`**（**P3-2** 策略开关仍 **未开始**）。
11. **联调建议**：狂按 **Q/R（CD）**、超重拾取、**F5–F8** 仓库、**T** 强化、**O/P** 解锁、**B/N** 买药 → **F2** → HUD **`tot:`** 与缩写计数应上涨；浏览器打开 **audit-categories** 可见累计。
12. **`WithdrawPotion`**：银行侧无药可取 → **`BankReject`**（`bank_empty`）；背包满导致无法放入 → **`InventoryFull`**。
13. **`DepositPotion`**：身上无足够红/蓝药可存 → **`BankReject`**（`no_potions`）。
14. **`SyncResponseParse`**：对 **`byCategory`** 键使用 **`Regex.Escape`**，避免键名特殊字符干扰正则。
15. **`/metrics/audit-categories`**：与 **`readMetricsRows`** 共用 **`days` / `playerId`** 过滤逻辑。
16. **`DebugHudSimple`**：仍只读 **`LastAuditCategoryPreview`** 一串，随 **`auditSummary`** 变长自动扩展信息量。
17. **`getting-started.md`**：观测说明放在 **D5** 节，避免文档碎片化。
18. **P3-2**（**未开始**）：若需对某类 **`SrvVal_*`** 单独 **拒收/告警**，与 **`REJECT_HIGH_WARNINGS`** 独立配置——见子任务表。
19. **兼容**：仅含 **`SrvVal_IllegalOperation`** 的旧包体仍显示 **`SV:`** 计数。
20. **本批收口**：**Unity 多脚本 + persist_sync 一端点 + docs 一句 + README 本条** 对齐为 **P3 批量可验收增量**（**不**改玩法成功路径数值）。

#### 本次续写（2026-04-21 · P3 续二）— 进度/用药/交易审计 + `;` 热键

1. **`SrvVal_ProgressReject`**：**`PlayerProgressSimple`** 中 **U/I** 经验不足拒绝。
2. **`SrvVal_ItemUseReject`**：**1/2** 喝药 — 无药、缺组件、**`Heal`/`Restore`** 拒绝。
3. **`SrvVal_TradeReject`**：**V** 卖药 — 无钱包、无药可卖。
4. **`TryAddPickup` 内统一超重审计**：拾取侧去掉重复 **`Push`**（仅 **`PickupDenied`** 仍独占）。
5. **`PlayerHotkeysSimple`**：**`;`** → **`GET /metrics/audit-categories`**；可选 **`metricsAuditCategoriesAlt`**。
6. **`PlayerStateExportSimple`**：**`LastMetricsAuditCategoriesPreview`** + **`GetMetricsAuditCategoriesRoutine`**（**`;`** 在 **`networkSyncEnabled` 关闭时仍可用**，与 **F1** 同级）。
7. **`DebugHudSimple`**：**`AudC:`** 摘要；帮助行 **`;审计类`**。
8. **`SyncResponseParse`**：HUD 缩写 **`Pr` / `It` / `Tr`**。
9. **`docs/getting-started.md`**：热键表增加 **`;`**。
10. **验收**：空经验按 **U**、无药按 **1**、无货按 **V** → **F4** 可见新类别 → **F2** → **`tot:`** 涨；**`;`** 看 **`AudC:`** 是否含 **`byCategory`** JSON 截断。
11–20. **与上批同节奏**：本续二为 **P3 审计面补全 + D5 观测一键**；**P3-2** 仍 **未开始**（策略拒收开关）。

#### 本次续写（2026-04-21 · P3 续三）— 战斗/存档/仓储 + npm 与日志开关 · ~20 步

1. **`SrvVal_CombatMiss`**：**空格**普攻 — 范围内 **无碰撞体**；或 **有碰撞体但无 `EnemyHealthSimple`**（`reason=no_enemy_health`）。
2. **`SrvVal_LoadReject`**：**F10** 读档 — 无存档键 **`EOD_SAVE_level`**。
3. **`SrvVal_StorageReject`**：**`RemoveItemById`** — 持有数量 **不足** 请求扣除量。
4. **`ServerAuditLogSimple`**：队列 **`MaxItems` 120→256**，容纳更多排练事件。
5. **`SyncResponseParse`**：HUD 缩写 **`Cm` / `Ld` / `St`**。
6. **`server/tools/fetch_audit_categories.cjs`**：CLI 拉取 **`/metrics/audit-categories`**（参数 **days**、可选 **playerId**；**`SYNC_BASE_URL`**）。
7. **`server/package.json`**：脚本 **`npm run audit-categories`**。
8. **`persist_sync.cjs`**：环境变量 **`LOG_AUDIT_SUMMARY=1`** 时在 **`POST /sync` 成功** 后打印 **`auditSummary`** 一行。
9. **`docs/getting-started.md`**：补充 **npm audit-categories** 与 **`LOG_AUDIT_SUMMARY`** 说明。
10. **验收**：空挥 **空格**、**F10** 无档、（调试）强制 **`RemoveItemById`** 超额 → **F4** 可见 **Cm/Ld/St** → **F2** 后 **`tot:`** 与 **`Cm:`** 等上涨。
11. **与 P3-0～续二兼容**：旧 **`SrvVal_*** 键** 仍进入 **`auditSummary`**；**P3-2** 仍 **未实现** 自动拒收。
12. **性能**：审计为 **O(1) Push**；**persist_sync** 聚合仍为 **同步扫描 metrics**（大文件时另议）。
13. **安全**：**`fetch_audit_categories.cjs`** 仅 **GET**，不写数据。
14. **键位**：**`;`** 与 **`LOG_AUDIT_SUMMARY`** 独立，互不覆盖。
15. **普攻**：**`CombatMiss`** 含 **误伤无血条碰撞体**（占位），后续可换 **Layer** 细化。
16. **读档**：仅 **无键** 记 **`LoadReject`**；版本不匹配仍 **尝试加载**（仅 **Log**）。
17. **仓储**：**`StorageReject`** 不替代 **`inv_remove` 成功** 日志类审计（仍为 **`inv_remove`**）。
18. **HUD**：**`tot:`** 行随 **服务端** 返回的 **`byCategory`** 键扩展；**Unity** 缩写表与 **persist_sync** 键名一致。
19. **npm**：需在 **`EpochOfDawn/server/`** 执行 **`npm install`**（若尚未安装依赖）。
20. **本批收口**：**客户端 3 脚本 + 审计常量 + 服务端 2 文件 + docs + README** = **P3 续三** 可交付增量。

#### 本次续写（2026-04-21 · P3 续四）— 分区/传送/存档/聊天 + 200 条规划清单 · ~20 步

1. **`SrvVal_ZoneReject`**：**K** 切 **PvP=ON** 且仍在 **安全区** 内（**不改变** `CanFightNow`，仅观测）。
2. **`SrvVal_PortalReject`**：**F** 传送但 **无落点**（`targetPoint` 与 **`WorldZoneConfig`** 均未解析到）。
3. **`AreaPortalSimple`**：先解析 **`zoneConfig` 落点**，再判空；修正原先 **`targetPoint==null` 提前 return** 导致配置永不生效的问题。
4. **`SrvVal_StateReject`**：**F11** **`ClearSave`** 清本地档（`op=clear_local_save`）。
5. **`SrvVal_ChatReject`**：**Enter** 发本地频道但 **`localMessage` 为空**。
6. **`SyncResponseParse`**：HUD 缩写 **`Zo` / `Pt` / `Sa` / `Ch`**。
7. **`EpochOfDawn/docs/p3-d5-roadmap-200.md`**：**10 组 ×20 条** 扩展清单（**【已】/【未】** 标注；与 **「10×20 条」** 节奏对齐，可分多对话落地）。
8. **验收**：城内 **K** 开 PvP、坏传送点按 **F**、**F11**、**Enter** 空发 → **F4** 见新类 → **F2** 后 **`tot:`** 与 **`Zo:`** 等上涨。
9. **P3-2**：见 **「本次续写 · P3-2」**（策略拒收/告警）。
10. **兼容**：**`summarizeAuditByCategory`** 仍按 **任意字符串 category** 计数；新键自动进 **`auditSummary`**。
11. **性能**：本批均为 **O(1) Push**；无新增协程。
12. **安全**：**`ChatReject`** 不记录消息正文；**`StateReject`** 仅操作类型字符串。
13. **传送门**：若仅 **`zoneConfig`** 一侧有落点，行为与修复后一致；**双空** 才 **`PortalReject`**。
14. **PvP**：仅 **开启** 且在 **安全区** 记 **`ZoneReject`**；**关闭** 不记。
15. **HUD**：**`LastAuditCategoryPreview`** 自动带出新缩写（正则键名已 **`Regex.Escape`**）。
16. **文档索引**：根 **README** 本节 + **`docs/p3-d5-roadmap-200.md`** 双链；**不**替代 **P3 子任务表**。
17. **里程碑原则**：**分区/清档/空聊** 仍为 **客户端自述**；服务端 **不** 据此单独定罪。
18. **后续波次**： roadmap 第 **7～10 组** 多为 **【未】**，供 **「续推进 10×20」** 选题池。
19. **与续三关系**：**Cm/Ld/St** 与本批 **Zo/Pt/Sa/Ch** 可同屏出现于 **`tot:`**。
20. **本批收口**：**5 脚本 + 审计常量 + 解析缩写 + 规划文档 + README**。

#### 本次续写（2026-04-21 · P3-2）— `SrvVal_*` 策略拒收 / 告警 + 协作约定

1. **协作约定**：**无固定优先级**——**`p3-d5-roadmap-200.md`** 中 **【未】** 项按会话分批落地即可，不必每次选题；**【未】** 中超出当前工程范围的保留为产品 backlog。
2. **`persist_sync.cjs`**：**`REJECT_SRVVAL_AUDIT=1`** → 若 **`audit[]`** 任一条 **`category`** 以 **`SrvVal_`** 开头则 **400**（**`error: srvval_audit_block`**），且**不写**落盘文件。
3. **`SRVVAL_REJECT_CATEGORIES`**：逗号分隔**精确**类别名（如 **`SrvVal_IllegalOperation,SrvVal_CombatMiss`**）→ **`error: srvval_category_block`**。
4. **`SRVVAL_REJECT_THRESHOLD_JSON`**：JSON，键为类别、值为**本包** `auditSummary.byCategory` 计数阈值（`>=` 即拒收）→ **`error: srvval_threshold_block`**。
5. **`SRVVAL_ALERT_THRESHOLD_JSON`**：同上形状，仅 **`console.warn`**（**`srvval_audit_alert`**），**不**拒收。
6. **修复**：**`high_warning_block`** 与 **`SrvVal_*` 拒收**均在 **`writeFileSync`** **之前**判定，避免 **400** 仍已落盘。
7. **metrics**：拒收时 **`accepted: false`**，并带 **`srvValRejectDetail`**（若有）。
8. **`GET /health`**：返回 **`rejectAnySrvValAudit`** 及 **`srvValCategoryRejectList`** 等摘要。
9. **验收**：`REJECT_SRVVAL_AUDIT=1 npm run persist`，游戏内 **F2** → 应 **400**（若 **`audit[]`** 非空且含 **`SrvVal_*`**）；去掉环境变量后恢复 **200**。
10. **默认**：不设上述环境变量时与 **P3-0/1** 行为一致，本地排练 **不受影响**。

#### 本次续写（2026-04-21 · D5 续五）— 校验交叉 / Prometheus / CI + **`SrvVal_PartyReject`** · ~20 步

1. **`audit_validate.cjs`**：**`audit[]` 按 `seq` 排序后相邻重复 `seq`** → **硬错误** **`audit_duplicate_seq`**（防交叉重放歧义）。
2. **同上**：批次内含任意 **`SrvVal_*`** → **低** 告警 **`audit_contains_srvval`**（与金币/背包链独立，**不**单独拒收）。
3. **`persist_sync.cjs`**：**`GET /metrics/prometheus?days=7&playerId=...`** → OpenMetrics 文本（**`persist_sync_audit_*`** 与按 **`category`** 标签的计数）。
4. **`GET /health`**：增加 **`metricsPrometheusPath`**。
5. **`server/tools/fetch_prometheus_metrics.cjs`** + **`npm run metrics-prometheus`**（**`SYNC_BASE_URL`**）。
6. **`.github/workflows/d5-validate.yml`**：**`npm ci`** + **`validate_sync_file`** 跑 **`examples/request_payload_example.json`**（**仅 server** 变更时触发）。
7. **`SrvVal_PartyReject`**：**`PartyPlaceholderSimple`** 满员仍 **+**、单人仍 **-**。
8. **`ChatPlaceholderSimple`**：**BackQuote** 系统频道空串 → **`ChatReject`**（**`channel=system`**）。
9. **`SyncResponseParse`**：HUD 缩写 **`Pa`**（**`PartyReject`**）。
10. **验收**：重复 `seq` 的 JSON → **`npm run validate`** **FAIL**；**`curl` /metrics/prometheus** 或 **`npm run metrics-prometheus`**（需先 **`npm run persist`**）；小键盘 **+** 在满员时 **F4** 见 **`Pa`**。
11. **roadmap**：第 **7 组** 交叉验证、第 **8 组** Prometheus、第 **9 组** CI 门禁项标 **【已】**（见 **`p3-d5-roadmap-200.md`**）。
12. **兼容**：`audit_contains_srvval` 为 **low**，**`--fail-on-high`** 默认不拦截。
13. **Grafana**：本端点仅为 **文本指标**；模板 **【未】** 仍由运维自选。
14. **性能**：Prometheus 端点 **O(行数)** 扫描 **`metrics.ndjson`**，与 **`audit-categories`** 同量级。
15. **安全**：Prometheus 不含 **PII**；**`category`** 标签经转义。
16. **Unity**：**`Party`** 为占位，**不**改 **`maxPartySize`** 默认玩法。
17. **CI**：不跑 Unity；若需 **headless 编译** 另开工作流。
18. **与 P3-2**：**`REJECT_SRVVAL_AUDIT`** 仍基于 **`audit[]`** 原文；**`audit_validate`** 的 `SrvVal` **warning** 不触发 **400**。
19. **本批收口**：**服务端 3 文件 + workflow + Unity 4 脚本 + README + docs**。
20. **下一批**：roadmap **第 7～9 组** 余 **【未】**（replay、幂等、k6、Docker 等）继续按批。

#### 本次续写（2026-04-21 · D6 续六）— 维护 / 限流 / 幂等 / HMAC / Staging + Docker / k6 / NDJSON 合并 · ~20 步

1. **`MAINTENANCE_MODE=1`**：**`POST /sync`** → **503** **`maintenance_mode`**（不写盘）。
2. **`SYNC_RATE_LIMIT_PER_MINUTE`**：按 **客户端 IP**（**`X-Forwarded-For`** 首选）滑动 **60s** 窗；超限 **429** **`rate_limited`** + **`Retry-After`**；**`0`** 关闭。
3. **`SYNC_IDEMPOTENCY_TTL_MS`**：**`Idempotency-Key`** + **请求体** 派生 **SHA-256**；**仅**缓存 **200** 成功体，TTL 内重放直接返回缓存（**先于**限流计数，避免重试刷爆配额）。
4. **`SYNC_HMAC_SECRET`**：要求请求头 **`X-Sync-Signature`** 为 **HMAC-SHA256(body, secret)** 的 **hex**（**`crypto.timingSafeEqual`**）；未设则**不**校验。
5. **`SYNC_REQUIRE_STAGING_HEADER=1`**：必须 **`X-Sync-Staging: 1`**，否则 **403** **`staging_required`**（灰度占位）。
6. **`GET /health`**：返回 **`maintenanceMode`**、**`syncRateLimitPerMinute`**、**`syncIdempotencyTtlMs`**、**`syncHmacRequired`**、**`syncRequireStagingHeader`**。
7. **`docker-compose.yml`**（**`EpochOfDawn/server/`**）：**`docker compose up`** 跑 **`persist_sync`**（**8787**）。
8. **`tools/merge_metrics_ndjson.cjs`** + **`npm run merge-metrics-ndjson`**：按 **`ts`** 排序 NDJSON 行（**stdin** 或文件路径）。
9. **`examples/k6-smoke.js`**：**`k6 run`** 打 **`/health`**（需本机已装 **k6**）。
10. **启动日志**：打印上述开关状态，便于对齐 **roadmap** 第 **7** 组。
11. **验收**：**`MAINTENANCE_MODE=1`** → **F2** **503**；限流 **→ 429**；**HMAC** 设密钥后无签名 **401**；幂等 **→** 同体 **`Idempotency-Key`** 第二次 **200** 且不重复 **`appendMetrics`**（第二次走缓存短路）。
12. **幂等缓存**：内存 Map，大时清理过期项；生产应换 **Redis**（仍 **【未】**）。
13. **与 P3-2**：**`SrvVal_*` 拒收** 在 **JSON 解析之后**；维护/限流/HMAC 在**更早**阶段。
14. **roadmap**：第 **7** 组 **9/10/12/13/14**、第 **8** 组 **k6**、第 **9** 组 **Docker / NDJSON 合并** 标 **【已】**（见 **`p3-d5-roadmap-200.md`**）。
15. **默认**：不设新变量时行为与 **续五** 一致。
16. **安全**：HMAC 为占位；**IP** 伪造依赖前置 **反向代理** 配置。
17. **429**：客户端（Unity）可后续读 **`Retry-After`**（**【未】** 显式 HUD）。
18. **compose**：镜像 **node:20-bookworm-slim**，卷挂载 **`server/`** 工作目录。
19. **本批收口**：**`persist_sync.cjs`** + **compose** + **tools** + **k6 示例** + **README + docs**。
20. **下一批**：**replay**、**Schema 协商**、**PATCH** 等仍 **【未】**。

#### 本次续写（2026-04-21 · D7 续七）— SLO 摘要 / 重放 CLI / Mock 200 / HUD **`syn:`** · ~20 步

1. **`GET /metrics/sync-summary?days=&playerId=`**：从 **`metrics.ndjson`** 聚合 **`accepted`/`rejected`**、**`acceptRatePercent`**、**`byError`**（与 **`appendMetrics`** 写入一致）。
2. **`GET /rehearsal/mock-sync-200`**：固定 **200** JSON（**`rehearsal: true`**），供客户端离线对齐响应形状。
3. **`tools/audit_replay_cli.cjs`** + **`npm run audit-replay`**：对 **`client_sync_request`** 形 JSON 重放 **金币链** / **药尾** 并与 **`state`** 对照打印。
4. **`audit_validate.cjs`**：导出 **`replayGoldTailForCli`**、**`replayPotionInventoryTail`** 供上项复用。
5. **`tools/fetch_sync_summary.cjs`** + **`npm run sync-summary`**（参数同 **`audit-categories`**）。
6. **`EpochOfDawn/server/.env.example`**：汇总 **`REJECT_*` / `SRVVAL_*` / `SYNC_*` / `MAINTENANCE_*`** 等（**不**自动加载，自行 **`export`** 或用进程管理器）。
7. **`PlayerStateExportSimple`**：**`LastSyncPostStatusTag`**（**`ok`/`maint`/`ratelimit`/`hmac`/`staging`/`srvval`/`badreq`/`httpNNN`**）。
8. **`DebugHudSimple`**：**`syn:`** 短标签（与 **`tot:`** 独立）。
9. **`GET /health`**：**`metricsSyncSummaryPath`**、**`rehearsalMockSyncPath`**。
10. **CI**：**`d5-validate.yml`** 增加 **`audit_replay_cli`** 一步（与 **`validate_sync_file`** 同例）。
11. **验收**：**`npm run sync-summary`**（需 **`persist`**）；**`curl` rehearsal**；Unity **F2** 在非 2xx 时见 **`syn:`**。
12. **SLO**：仅统计**已写入 metrics** 的 **`POST /sync`**；**429/503** 若未记 metrics 则**不在**率内（与 **续六** 行为一致）。
13. **roadmap**：**7 replay（CLI）**、**8 追踪/SLO**、**9 mock / 版本对齐占位** 部分标 **【已】**（见 **`p3-d5-roadmap-200.md`**）。
14. **P99 延迟**：仍 **【未】**（需在 metrics 行写入 **`durationMs`**）。
15. **Mock**：不替代真实 **`auditSummary`**；仅形状参考。
16. **`.env.example`**：不含密钥真值；生产用密钥管理。
17. **Unity**：**`StringComparison`** 用于 **400** 体里 **`srvval_`** 粗检。
18. **兼容**：未跑过 **F2** 时 **`syn:`** 可为空。
19. **本批收口**：**`persist_sync` + 3 tools + `.env.example` + CI + Unity 3 文件 + README + docs**。
20. **下一批**：**Schema 协商**、**PATCH**、**warnings→SrvVal** 映射等 **【未】**。

#### 本次续写（2026-04-21 · D8 续八）— 延迟全链路 + **10×20 滚动计划** · ~20 步

1. **「再走 10 个 20 步」**：= **10 波 × 每波 ~20 条**增量；**200 条**为规划上限，分多会话落地；总表见 **`EpochOfDawn/docs/p3-d5-waves-10x20.md`**。
2. **`POST /sync`**：自 **`end`** 回调 **`t0`** 起算 **`durationMs`**；**所有**结束分支 **`appendMetrics`**（含 **503/429/401/403/400** 与 **幂等命中**）。
3. **metrics 行**：**`httpStatus`**、**`durationMs`**；成功含 **`httpStatus:200`**；幂等重放 **`idempotentReplay:true`**。
4. **响应头**：**`X-Sync-Duration-Ms`**（**`sendJson`** 第四参扩展 + **429** 手写头）。
5. **`GET /metrics/sync-summary`**：增加 **`latencyMs`**（**`p50`/`p90`/`p99`/`mean`/`sampleCount`**）。
6. **`GET /metrics/prometheus`**：追加 **`persist_sync_post_duration_ms{quantile=...}`** 与 **`persist_sync_post_duration_samples`**（有样本时）。
7. **`audit_validate`**：**`state.version` ≠ `schemaVersion`** → **`low`** **`state_version_vs_schema_mismatch`**。
8. **Unity**：**`LastSyncDurationMs`**；HUD **`d:Nms`**。
9. **验收**：**F2** 见 **`d:`**；**`sync-summary`** 见 **`latencyMs`**（需新产生的 metrics 行带 **`durationMs`**）。
10. **SLO**：**`acceptRatePercent`** 仍基于 **`accepted`**；延迟与成功率独立展示。
11. **兼容**：历史 **metrics** 无 **`durationMs`** 时 **`latencyMs.sampleCount`** 可能为 **0**。
12. **与续七**：**`syn:`** 仍表示类别；**`d:`** 表示服务端处理耗时（毫秒）。
13. **波 9～10**：**波 9**（**D9 续九**）与 **波 10**（**D10 续十** · 合规/导出/快照 CI）已见 **`p3-d5-waves-10x20.md`**；**PATCH 实装** 等仍 **【未】**。
14. **CI**：**`validate_sync_file`** 仍过（示例 **`state.version`** 与 **`schemaVersion`** 一致）。
15. **Prometheus**：**`quantile`** 标签为**窗口内聚合**，非 Histogram 原生桶。
16. **安全**：**`durationMs`** 不写入玩家 PII。
17. **幂等**：重放请求仍记 **metrics**，**SLO 成功计数 +1**（与业务「未重复写盘」正交）。
18. **里程碑原则**：客户端 **`d:`** 仅观测；权威仍在服务端。
19. **本批收口**：**`persist_sync` + `audit_validate` + Unity 3 文件 + waves 文档 + README + roadmap + getting-started**。
20. **下一会话**：按 **`p3-d5-waves-10x20.md` 波 9** 或 **`p3-d5-roadmap-200.md`** 下一组 **【未】** 继续。

#### 本次续写（2026-04-21 · D9 续九）— replay 观测 / 时间窗 / PATCH 草案 / CI schema / 429 重试 · ~20 步

1. **`SYNC_ISSUED_AT_MAX_SKEW_SEC`**：**`issuedAtUtc`** 相对服务端时钟超出则 **400**（**`issued_at_skew`** 等）；**0** 关闭。
2. **200 响应**：**`replayObservation`**（**`goldMatch`/`hpPotionMatch`/`mpPotionMatch`**），由 **`audit[]` 重放尾** 与 **`state`** 对照（**观测**，非权威裁定）。
3. **`GET /rehearsal/patch-strategy`**：PATCH 策略**草案** JSON；**`GET /health`** 含 **`rehearsalPatchStrategyPath`**。
4. **CI**：**`npm run check-schema-version`** — **`client_sync_request.schema.json`** 的 **`schemaVersion.const`** 与 **`PlayerStateExportSimple.Export.cs`** **`ClientSyncRequestSchemaVersion`** 一致。
5. **`npm run metrics-archive`**：将早于 N 天的 **metrics** 行**追加**到 **`data/metrics-archive/part-YYYY-MM-DD.ndjson`**（**不**删原文件）。
6. **Unity**：**`POST /sync`** 遇 **429** 最多再试 **1 次**（读 **`Retry-After`**，**1～60s**）；**`LastSyncRetryCount`**；HUD **`r:`**。
7. **`syn:time`**：**400** 体含 **`issued_at`** 时粗分类（与 **`srvval`** 并列观测）。
8. **`.env.example`**：已列 **`SYNC_ISSUED_AT_MAX_SKEW_SEC`**。
9. **验收**：设 **`SYNC_ISSUED_AT_MAX_SKEW_SEC=1`** 时故意改 **`issuedAtUtc`** → **400**；**F2** 成功体含 **`replayObservation`**；**`npm run check-schema-version`** **OK**。
10. **与 D8**：**`durationMs`**/**`X-Sync-Duration-Ms`** 仍有效；本批不重复。
11. **roadmap / waves**：**`p3-d5-waves-10x20.md`** 波 **9** 已从占位改为可对照条目（见该文件）。
12. **PATCH**：当前仍为**全量替换**写盘；**`/rehearsal/patch-strategy`** 仅为路线说明。
13. **归档**：大文件请自行轮转或压缩；脚本只做**按日副本**。
14. **里程碑原则**：**replayObservation** 为排练对齐；**权威**仍在服务端 **`validate`** 与后续产品策略。
15. **兼容**：未开 **`SYNC_ISSUED_AT_MAX_SKEW_SEC`** 时与 **D8** 行为一致。
16. **断网说明**：若会话中断，以 **`README`** 本段 + 工程内脚本为准补读。
17. **package.json**：**`check-schema-version`**、**`metrics-archive`**。
18. **本批收口**：**`persist_sync` + tools + CI + Unity + 文档**。
19. **下一批**：**warnings→SrvVal** 自动映射已在 **D11** 以**观测桥**落地；**PATCH 实装**、**Redis 幂等** 等仍 **【未】**。
20. **续接**：继续 **`p3-d5-roadmap-200.md`** 下一组 **【未】**（**PATCH 实装**、**Redis 幂等** 等）。

#### 本次续写（2026-04-21 · D10 续十）— 合规 / 导出 / Schema 快照 CI / Unity EditMode · ~20 步

1. **`GET /rehearsal/compliance-bundle`**：留存**提示**、PII 日志说明、S3 兼容导出约定（**排练**；执法以部署/法务为准）。
2. **`GET /health`**：增加 **`rehearsalComplianceBundlePath`**、**`auditExportBucketPrefix`**、**`logRedactPii`**、**`complianceMetricsRetentionHintDays`**、**`compliancePlayerStateRetentionHintDays`**。
3. **`LOG_REDACT_PII=1`**：控制台部分行对邮箱、16 位连续数字、**IPv4 末段**脱敏；**`auditSummary` / validation 错误串**经 **`redactPiiForLog`**；**不改** **`metrics.ndjson`** 落盘。
4. **`COMPLIANCE_METRICS_RETENTION_HINT_DAYS`** / **`COMPLIANCE_PLAYER_STATE_RETENTION_HINT_DAYS`**：仅 **hint**（秒级留存策略仍由运维/对象存储生命周期实现）。
5. **`AUDIT_EXPORT_BUCKET_PREFIX`**：默认 **`persist-sync-local`**；导出 manifest 写入该前缀说明。
6. **`tools/audit_export_bundle.cjs`** + **`npm run audit-export-bundle`**：在 **`data/audit-export/export-<iso>/`** 写入 **`manifest.json`**（**`parts[].sha256`**）+ **`metrics-window.ndjson`** + **`players-index.json`**（文件名与大小，**不**整包拷贝玩家 JSON 内容）。
7. **`tools/check_schema_files_snapshot.cjs`** + **`npm run check-schema-snapshot`**：校验 **`schemas/*.schema.json`** 与 **`tools/schema_files_snapshot.json`** 一致；**`--write`** 更新快照。
8. **CI** **`.github/workflows/d5-validate.yml`**：增加 **`check-schema-snapshot`**、**`audit-export-bundle`**、**`check-locale-ci`**（无 **`Assets/Localization`** 时 **skip**）。
9. **`tools/check_locale_placeholder.cjs`** + **`npm run check-locale-ci`**：占位，后续接多语言资源校验。
10. **Unity**：**`PlayerStateExportSimple.AuditSummaryHudPreviewFromJson`**（与私有 **`BuildAuditSummaryHudPreview`** 等价）。
11. **`Assets/Tests/EditMode/`**：**`EpochOfDawn.Tests.EditMode.asmdef`** + **`AuditSummaryHudPreviewTests`**（**NUnit**）。
12. **`.env.example`**：已列 **D10** 变量。
13. **里程碑原则**：合规与导出仍为**排练/可观测**；**权威**仍在服务端 **`validate`** 与产品策略。
14. **与 D9**：**`replayObservation`**、**`SYNC_ISSUED_AT_MAX_SKEW_SEC`** 不变。
15. **文档**：**`p3-d5-waves-10x20.md`** 波 **10** 已落地；**`getting-started`**、**`p3-d5-roadmap-200.md`** 已交叉更新。
16. **验收**：**`npm run check-schema-snapshot`** **OK**；**`npm run audit-export-bundle`** 生成 **`manifest.json`**；Unity **Test Runner** 跑 **EditMode** 通过。
17. **S3**：脚本**不**调用云 API；上传路径见 **`manifest.uploadHint`**。
18. **PII**：生产环境应限制 **`data/`** 访问；**`metrics-archive`** 副本仍含 **`playerId`**。
19. **Schema 快照**：修改 **`schemas/*.schema.json`** 后需 **`node tools/check_schema_files_snapshot.cjs --write`** 并提交 **`schema_files_snapshot.json`**。
20. **下一批**：**PATCH 实装**、**Redis 幂等** 等仍 **【未】**（**warnings→SrvVal** 观测桥已在 **D11** 落地）。

#### 本次续写（2026-04-21 · D11 续十一）— warning code → `SrvVal_*` 桥 / Grafana 示例 · ~12 步

1. **`SYNC_WARNING_SRVVAL_BRIDGE=1`**：在未设置 **`WARNING_CODE_TO_SRVVAL_JSON`** 时启用**内置**映射（**`state_version_vs_schema_mismatch`→`SrvVal_StateReject`** 等）。
2. **`WARNING_CODE_TO_SRVVAL_JSON`**：非空 JSON **完全**作为映射表（覆盖内置）；**`""`** 或未设且未开 bridge → **不**生成 **`srvValFromWarnings`**。
3. **`auditSummary.srvValFromWarnings`**：**`{ total, byCategory }`**，由 **`validation.warnings`** 聚合的 **`warningsByCode`** 经映射得到（**观测**；与 **`audit[]`** 真实 **`SrvVal_*`** 条目不混为裁决）。
4. **`GET /metrics/audit-categories`** / **Prometheus** 聚合路径：将 **`srvValFromWarnings.byCategory`** 与 **`auditSummary.byCategory`** **加总**到同一 **`byCategory`** 视图（便于 **`;`** / 大盘看合成信号）。
5. **`GET /rehearsal/warning-srvval-bridge`**：说明 env、**`defaultMap`**、**`activeMap`**、已知 **warning code** 列表。
6. **`GET /health`**：**`rehearsalWarningSrvValBridgePath`**、**`syncWarningSrvValBridge`**、**`warningCodeToSrvValMapConfigured`**。
7. **`.env.example`**：已列 **D11** 变量。
8. **`examples/grafana-persist-sync-minimal.json`**：最小 **Grafana** 面板（**`persist_sync_up`**、**`persist_sync_audit_events_total`**），数据源 **`${DS_PROMETHEUS}`** 占位。
9. **里程碑原则**：桥接仅为**排练对齐 HUD 口径**；**拒收/权威**仍以 **`validate`**、**P3-2**、产品策略为准。
10. **验收**：设 **`SYNC_WARNING_SRVVAL_BRIDGE=1`**，制造 **`state.version`≠`schemaVersion`** → 200 体 **`auditSummary.srvValFromWarnings.byCategory.SrvVal_StateReject`≥1**（在 **`audit[]`** 无该类时亦可出现）。
11. **与 D10**：**`LOG_REDACT_PII`**、合规 bundle **不变**。
12. **下一批**：**PATCH 实装**、**Redis 幂等**（**D12** 已落地**单机落盘**幂等）、**深度 audit↔state 交叉** 仍 **【未】**。

#### 本次续写（2026-04-21 · D12 续十二）— 幂等缓存落盘（Redis 前奏）· ~8 步

1. **`SYNC_IDEMPOTENCY_PERSIST=1`**：在 **`SYNC_IDEMPOTENCY_TTL_MS`>0** 时，把 **`Idempotency-Key`** 命中的 **200** 响应缓存写入 **`data/idempotency-cache.json`**（**tmp + rename**）。
2. **启动**：**`idempotencyLoadFromDisk()`** 载入未过期项；随后 **`idempotencyPersistSnapshot()`** 剔除文件中已过期键。
3. **每次** **`idempotencyCacheSet`**：内存更新后 **同步** 重写落盘文件（排练量可接受）。
4. **`GET /rehearsal/idempotency-persist`**：说明依赖 env、**单进程**假设、多实例请 **Redis**。
5. **`GET /health`**：**`syncIdempotencyPersist`**、**`idempotencyPersistPath`**；**`rehearsalIdempotencyPersistPath`**。
6. **`.env.example`**：已列 **D12**。
7. **里程碑**：仍**仅缓存 200** 重放；**不**替代跨节点一致性。
8. **验收**：**TTL**+**PERSIST** 开启 → 同一 **`Idempotency-Key`** 先发 **200** → 重启 **`npm run persist`** → 再发应 **idempotent_cache_hit** 且体一致。

#### 本次续写（2026-04-21 · D13 续十三）— PATCH 排练（只读校验）· ~10 步

1. **`tools/patch_validate_rehearsal.cjs`**：**`validatePatchRehearsalRequest(body)`**，**`ops`** 为 **JSON Patch 风格**（**`replace` / `add` / `remove`**），**`path`** 以 **`/`** 起且限于 **`gold` / `level` / `version` / `playerId` / `inventory/hpPotion` / `inventory/mpPotion`**（相对 **`state`** 根）。
2. **`baseState`** 可选；缺省为内置最小 **`player_state`** 预览。
3. **`POST /rehearsal/validate-patch`**：**200** 返回 **`mergedStatePreview`**、**`mergedStateHashPreview`**、**`warnings`**（如预览金为负）；**400** 返回 **`errors`**；**不写盘**、**不经** **`POST /sync`**。
4. **`GET /health`**：**`rehearsalValidatePatchPath`**。
5. **`GET /rehearsal/patch-strategy`**：规则增 **POST validate-patch** 说明。
6. **`npm run validate-patch-rehearsal`** = **`node tools/patch_validate_rehearsal.cjs --smoke`**。
7. **CI** **`.github/workflows/d5-validate.yml`**：增加 **`patch_validate_rehearsal --smoke`**。
8. **里程碑原则**：仍为**排练**；**`POST /sync`** 继续**整包替换**写盘。
9. **验收**：**`curl -X POST http://127.0.0.1:8787/rehearsal/validate-patch -H "Content-Type: application/json" -d "{\"ops\":[{\"op\":\"replace\",\"path\":\"/gold\",\"value\":10}]}"`** → **200**。
10. **下一批**：**`state` 排练写盘** 见 **D15**；**ETag**、**Redis** 等仍 **【未】**。

#### 本次续写（2026-04-21 · D14 续十四）— audit↔state 严格交叉（排练门禁）· ~10 步

1. **`SYNC_AUDIT_STATE_STRICT=1`**：在 **`audit_validate.cjs`** 中将下列情况从 **warning** 升为 **`errors`**（**`POST /sync`** → **400** **`validation_failed`**）：**`schemaVersion`≠1**、**`body.playerId`≠`state.playerId`**、**`state.version`≠`schemaVersion`**、**金币链尾 ≠ `state.gold`**、**药尾 ≠ `state.inventory`**（hp/mp）。
2. **默认关闭**：与历史行为一致（尾不一致仍为 **low warning**，容忍环形容量截断）。
3. **`GET /rehearsal/audit-state-strict`**：列出升格项与说明。
4. **`GET /health`**：**`syncAuditStateStrict`**、**`rehearsalAuditStateStrictPath`**。
5. **导出**：**`audit_validate.cjs`** **`syncAuditStateStrict`** 供 **`persist_sync`** 打日志。
6. **`.env.example`**：已列 **D14**。
7. **里程碑原则**：更严的**排练门禁**，**非**已上线 MMORPG 全量战斗权威。
8. **与 D11**：**`srvValFromWarnings`** 观测桥独立。
9. **验收**：**`SYNC_AUDIT_STATE_STRICT=1`** + 故意改 **`state.gold`** 与审计链不一致 → **400** 体含 **`gold_tail_mismatch_vs_state`**（或对应 error 码）。
10. **下一批**：**`state` 排练写盘** 见 **D15**；**ETag** 见 **D16**；**Redis**、**seq 连续性强校验** 等仍 **【未】**。

#### 本次续写（2026-04-21 · D15 续十五）— PATCH `state` 写盘（排练闸）· ~10 步

1. **`REHEARSAL_PATCH_WRITE=1`**：开启 **`POST /rehearsal/apply-patch`**；**默认关闭**（**403** **`rehearsal_patch_write_disabled`**）。
2. **请求体**：**`{ "playerId", "ops" }`** — **`ops`** 与 **`/rehearsal/validate-patch`** 白名单一致；**读** **`data/<safePlayerId>.json`**，仅把 **`mergedStatePreview`** 写回 **`state`** 字段。
3. **校验链**：**`issuedAtUtc`** 时间窗（若开）、**`validateSyncPayloadSchemas`**、**`validateClientSyncPayload`**、**`high_warning_block`**、**P3-2** **`evaluateSrvValSyncReject`**；**维护/限流/Staging/HMAC** 与 **`POST /sync`** 同口径。
4. **响应**：**200** 含 **`rehearsalPatchApplied`**、**`mergedStateHashPreview`**、**`patchWarnings`**；**metrics** 含 **`rehearsalApplyPatch:true`**。
5. **`GET /rehearsal/apply-patch`**：文档 JSON（**`rehearsalPatchWrite`** 是否开启）。
6. **`GET /health`**：**`rehearsalPatchWrite`**、**`rehearsalApplyPatchPath`**。
7. **`GET /rehearsal/patch-strategy`**：规则已增 **D15** 行。
8. **`.env.example`**：已列 **D15**。
9. **里程碑**：**排练** 写 **`state`**；**审计/权威玩法** 仍以产品与真服为准。
10. **下一批**：**ETag** 见 **D16**；**Redis**、**Unity 调 apply-patch** 等仍 **【未】**。

#### 本次续写（2026-04-21 · D16 续十六）— ETag / If-Match 乐观并发 · ~10 步

1. **算法**：磁盘上 **`data/<safePlayerId>.json`** 的 **UTF-8 字节** 做 **SHA-256**，十六进制；响应头 **`ETag: "<hex>"`**（引号包裹）。
2. **`GET /state?playerId=`**：**200** 带 **`ETag`**（与文件字节一致）。
3. **`POST /sync`**：在 **`playerId`** 合法后、重校验前，若请求带 **`If-Match`**（非 **`*`**）且 **`SYNC_ETAG_DISABLED`** 未开：无存档或哈希与当前文件不一致 → **412** **`precondition_failed`**（体含 **`detail`**：**`if_match_but_no_save`** / **`if_match_mismatch`**）；成功 **200** 带 **`ETag`**（与本次写入 **`pretty`** 字节一致）。
4. **`POST /rehearsal/apply-patch`**：在确认存档存在后同样 **`If-Match`** 校验；**200** 带 **`ETag`**。
5. **`Idempotency-Key` 重放 200**：响应带 **`ETag`** = 对 **JSON 序列化后的 body** 的哈希（与磁盘规范化可能略有差异，以重放体为准）。
6. **`SYNC_ETAG_DISABLED=1`**：**跳过** **`If-Match`** 校验；仍可发 **`ETag`**。
7. **`GET /rehearsal/etag-concurrency`**：机器可读说明；**`GET /health`**：**`syncEtagIfMatchEnabled`**、**`rehearsalEtagConcurrencyPath`**。
8. **`GET /rehearsal/compliance-bundle`**：**`relatedPaths.etagConcurrency`**；**`GET /rehearsal/patch-strategy`** 规则已更新 **D16**。
9. **`.env.example`**：已列 **D16**。
10. **下一批**：**Redis**、**Unity 客户端 If-Match** 等仍 **【未】**。

#### 本次续写（2026-04-19 · 会话收尾）— Git 与文件状态

1. **GitHub `main`**：已与 **`origin/main`** 对齐并完成推送（合流使用 **`merge origin/main -X ours`**：同路径冲突时 **保留本机版本**；远程侧多出的 **`docs/weekly/`**、**`server/README.md`**、**`server/examples/*`** 等已并入）。
2. **`persist_sync`**：**D16** **`ETag`/`If-Match`** 与 **`npm run check-syntax`**（见 **`EpochOfDawn/server/package.json`**）已在工程内。
3. **双 README 说明**：完整规约与进度总账在 **`d:\mygame\README.MD`**；仓库内另有根目录 **`README.md`**（架构长文节选），续写以 **`README.MD`** + **`EpochOfDawn/docs/getting-started.md`** 为对照即可。
4. **下一会话**：直接复制下文「**续接口令**」开聊。

## 新对话续接（必读）

**下次开新对话时，把下面整段复制给 AI，或让 AI先读本 README 本节 +「当前开发进度」。**

- **仓库根路径：** `d:\mygame\`
- **设计规约与进度总账：** 根目录 `README.MD`（本文件）
- **Unity 客户端工程：** `d:\mygame\EpochOfDawn\`（Unity **2022.3 LTS**，**URP**，Windows Build）
- **协作方式：** 用户 **不写代码**、主要在 **Unity 编辑器里操作**；AI **直接改 `EpochOfDawn/Assets` 下脚本** 并说明用户要点哪里
- **Git：** `EpochOfDawn/.gitignore` 已配置（勿提交 `Library/` 等大目录）
- **已完成阶段：** **D0、D1、D2** 已完成；D3 主闭环已完成；D4 主流程验收已通过（`Flow:OK`）；**P1-A 内容/手感**（P1-A-0～5）**已完成**（2026-04-21 写入进度表）。
- **当前要做：** **P3** 子表 **P3-0～P3-2** 已收口；扩展清单 **`EpochOfDawn/docs/p3-d5-roadmap-200.md`** + **10×20 滚动计划** **`EpochOfDawn/docs/p3-d5-waves-10x20.md`**（**无固定优先级**，按批推进）；并行 **D5** 联调（按需）。
- **执行口径（2026-04-17 固定）：** 代码实现以 **Unity 工程 `EpochOfDawn/`** 为唯一真源；本 README 第十部分历史 Cocos/Colyseus 表述不作为当前实现依据。
- **主要脚本（均在 `Assets/`）：** `PlayerMoveSimple`、`PlayerAttackSimple`、`PlayerPickupSimple`、`MonsterCombatHost`、`MonsterChaseHost`、`MonsterP1A1Mark`、`P1A1QuestState`（含 **`P1AContentConfig`** 类型）、`P1A1WildSpawner`、`WaveSpawnerSimple`、`P1MiniBossSimple`、`P1BossPhaseSimple`、`P1MiniBossSceneBootstrap`、`DropItemSimple`、`DebugHudSimple`、`PlayerProgressSimple`、`PlayerInventorySimple`、`PlayerBankSimple`、`PlayerSaveSimple`、`PlayerStateExportSimple`（partial：`Export` / `Network` / **`Input`** / **`SyncResponseParse`**）、`ServerAuditLogSimple`、`PlayerAreaStateSimple`、`SafeZoneSimple`、`AreaPortalSimple`、`WorldZoneConfigSimple`、`PartyPlaceholderSimple`、`ChatPlaceholderSimple`；**Editor**：`EpochOfDawnBuildP1A5`（P1-A-5 一键 Windows 构建）（名称以工程为准）

**续接口令（复制即可）：**  
`继续《破晓纪元》开发。已读 d:\mygame\README.MD 里「新对话续接」与「今日开发日志」。当前状态：D4 Flow:OK；P1-A 完成；P2-D5 完成；**P3** **SrvVal_*** + **P3-2**；**persist_sync** **durationMs/httpStatus** + **`replayObservation`** + **可选 `SYNC_ISSUED_AT_MAX_SKEW_SEC`** + **`/rehearsal/patch-strategy`** + **`POST /rehearsal/validate-patch`** + **`POST /rehearsal/apply-patch`**（**`REHEARSAL_PATCH_WRITE=1`**）+ **`GET /state`/`POST /sync`/`apply-patch` 的 ETag/If-Match（D16）** + **`GET /rehearsal/etag-concurrency`** + **可选 `SYNC_ETAG_DISABLED`** + **`/rehearsal/audit-state-strict`** + **`/rehearsal/compliance-bundle`** + **`/rehearsal/warning-srvval-bridge`** + **`/rehearsal/idempotency-persist`** + **可选 `SYNC_WARNING_SRVVAL_BRIDGE`** / **`WARNING_CODE_TO_SRVVAL_JSON`** + **可选 `SYNC_AUDIT_STATE_STRICT`** + **可选 `SYNC_IDEMPOTENCY_PERSIST`** + **可选 `LOG_REDACT_PII`**；**`npm run check-schema-version`** / **`check-schema-snapshot`** / **`validate-patch-rehearsal`** / **`audit-export-bundle`** / **`metrics-archive`**；**Grafana** **`EpochOfDawn/server/examples/grafana-persist-sync-minimal.json`**；**10×20** **`EpochOfDawn/docs/p3-d5-waves-10x20.md`**；**GitHub Actions** **`d5-validate.yml`**；**F2** **`tot:`** **`syn:`** **`d:ms`** **`r:`** + **`;`** **`AudC:`**；**Unity EditMode** **`AuditSummaryHudPreviewTests`**；**`p3-d5-roadmap-200.md`**；D5 F12/F4/F3/F2/F1。你改 EpochOfDawn/Assets 脚本。`

---

# 第一部分：核心架构 (Architectural Core)

## 1. 设计哲学

本游戏采用 **"数据驱动"** 模式：角色属性、装备、技能、背包等 **全部由数据表驱动**，服务端权威校验，便于平衡与热更新。

**核心体验关键词：**

- **纯刷怪升级**——没有任务系统，玩家通过击杀怪物获取一切（经验、装备、材料）
- **技能为王**——**经验**只用于人物升级或解锁技能；**技能等级**靠施放次数养成；满级低阶技能仍可压制高阶低练技能（见第 4 节）
- **PK 即正义**——野外自由 PK，红名高风险高回报，死亡大爆装
- **慢即是美**——RPG 阶段是漫长的成长旅程，拒绝速成

### 1.1 整体闭环：无敌的梦、不可复制的极限、必有克星

- **级别不是终点**：人物等级只做弱修正（见第 8 节）；**战力与个性**主要来自 **加点选择、技能熟练度、装备随机与武器词条组合**。
- **无职业 + 装备多样性**：不设固定职业模板；**刀剑弓杖** 与 **随机四维、耐性、技能增强** 等，让生态里 **自然长出** 各种「像战像法像弓」的混合角色，而不是官方填好的几套职业。
- **允许极低概率下的「无敌人物」**：通过 **极小概率的随机组合**（装备、词条对齐、技能练度等）**理论上** 可以堆出某个维度上近乎无解的个体——这是给每个人的 **梦想上限**，不是日常强度基准。
- **不可复制**：同一套「神配」不应成为可批量攻略的固定路线；**随机、不可逆选择、经验与解锁的取舍** 等，让极限个案 **难以被所有人抄作业**。
- **每种无敌背后必有克星**：规则上保留 **多条独立胜负维度**——例如 **即时减伤 / 状态持续时间 / 无视防御的燃烧与嗜血 / 控制与距离 / 资源与箭矢** 等——使任何极端构筑在 **某类对手或某类局面** 下仍有破口；**不做全局唯一真神**。

> 以上原则指导各系统取舍：**宁可极端个案罕见，也不要人人同质的最优解。**

## 2. 玩家基础属性 (The Immutables)

### 2.1 四维属性

| 属性 | 缩写 | 定位 |
|------|------|------|
| 力量 | STR | 物理攻防核心，影响负重 |
| 敏捷 | AGI | 闪避/攻速/暴击，PVP 生存核心 |
| 智力 | INT | 元素攻防核心，影响 MP |
| 体力 | VIT | HP/防御/状态抗性，坦克属性 |

- **不可逆性：** 属性点一旦分配，永久无法重置
- **每级获得 5 点** 自由属性点

### 2.2 等级体系

- **满级：** 50
- **1-40 级：** 成长期
- **41-48 级：** 数值沉淀期（经验曲线陡增）
- **49-50 级：** 极限冲刺（极少数玩家可达）

## 3. 装备系统：四阶、防具零攻击 (Tier-Locked)

### 3.0 装备部位（7件套）

| 部位 | 说明 |
|------|------|
| 头盔 | 物防 / 元素抗等 |
| 盔甲 | 防御核心 |
| 护腿 | 防御向 |
| 鞋子 | 防御 + 机动 |
| 手套 | 防御向 |
| 武器 | **无攻击白字**；随机四维（穿戴生效）+ 词缀（见 3.6） |
| 盾牌 | 纯防御 |

- **除武器外**无任何攻击类词条；**无加攻击药水**。
- 防具：**阶位、基础防御类随机**；**0～4** 条耐性词缀（3.4）；每件 **一个力量需求**（3.5）。

### 3.1 阶位锁定

| 阶位 | 穿戴等级 | 基础防御类总值（示意，可配表） |
|------|---------|------------|
| 1阶 | 1-12 级 | 50-80 |
| 2阶 | 13-24 级 | 120-180 |
| 3阶 | 25-36 级 | 250-380 |
| 4阶 | 37-48 级 | 500-750 |

### 3.2 无品质、随机防御向

装备**没有品质颜色之分**。同阶差异来自 **基础防御类总值** 与 **耐性词缀**；输出靠 **属性与技能**（第 5～6 节），防具不再堆攻击分布。

### 3.3 三大系别（修炼方向）

| 系别 | 倾向 | 适合加点 |
|------|------|---------|
| 战系 | 物理攻防 | STR/VIT 型 |
| 弓系 | 远程输出 | STR/AGI 混合 |
| 法系 | 元素攻防 | INT 型 |

**无固定职业**；武器四维随机可跨外观（如高 INT 剑玩法系）。

### 3.4 词缀：0～4 条状态耐性（防具等）

随机 **0～4 条**；每条只 **缩短自身对应状态的持续时间**（不挡燃烧 DOT、嗜血等独立结算）。

| 词缀 | 对抗状态 |
|------|----------|
| 抗冰冻 | 冰冻（纯控制） |
| 抗燃烧 | 燃烧（含 DOT 的燃烧状态） |
| 抗毒 | 毒（缓行、CD↑、降攻等，**无 DOT**） |
| 抗嗜血 | 嗜血（被吸取生命） |

词缀条数概率可沿用：0 条 40%，1 条 30%，2 条 18%，3 条 9%，4 条 3%。

### 3.5 力量需求与强化

- **力量需求**：每件 **一个** 穿戴力量需求 = **f(阶位, 四条耐性强度之和)**；耐性越高 → 力量需求越高。
- **强化**：七部位 **共用同一套** +1、+2… **防御向递增速率**（与阶位无关）；阶位差异靠 **基础值 + 强化等级** 组合。
- 仅一种强化石；成功加数值与流光；失败 **不碎不降**。

### 3.6 武器（刀 / 剑 / 弓 / 杖）

- **无佩戴门槛**；**刀 / 剑** 同池随机，仅外观动作不同；**杖偏 INT**，**弓偏 AGI**；可单维拉满或四维均分；**仅穿戴时** 生效。
- 可有与防具 **同类四耐性**；可有 **延长你对敌人施加的负面状态持续时间** 的进攻词缀。
- **技能增强最多 4 条**：**n 阶武器** 只能出现 **≤ n 阶** 技能的增强；**允许** 多条叠 **同一技能**，**线性相加**；四阶武器也可能四条全是一阶技能增强。
- **弓系技能** 须装备 **弓**；**箭** 为 **背包消耗品**（施放消耗，细则可配）。

## 4. 技能系统：经验解锁 + 施放养成

**已取消** 技能书 / 修炼书作为核心养成。

### 4.1 经验：只干两件事

杀怪等获得的 **经验** 只能二选消耗：**人物升级** 或 **解锁技能**（不用于技能等级）。

### 4.2 解锁技能（分阶段、不可逆）

在同一人物等级阶段内，按你已解锁技能 **个数** 计价（相对「本阶段升级所需经验」）：

1. 第 **1** 个技能：约等于 **升 1 级人物** 所需经验  
2. 第 **2** 个：约等于 **本阶段人物升级经验总和**  
3. 第 **3** 个：**2×** 该总和  
4. 第 **4** 个：**4×** 该总和  

若该阶段可学技能少于 4，后续档位不用。

**跨阶段不可逆**：人物 **升到 13 级** 后，**不能再补学** 本阶段未解锁的一阶池技能；25、37 等阶段同理。

### 4.3 技能等级（Lv.1～Lv.10）

- **只靠施放次数**：每次 **成功释放**（进 CD / 扣蓝即算，**打空也算**）；**阶越高**，升到下一级所需次数越多。与经验 **完全独立**。
- 便于在 **低级怪区练招**；升级整体偏难时，**技能熟练度**仍是战力核心。

### 4.4 技能缩放公式（不变）

$$Damage = Base \times (1 + Level \times Scale)$$

- 技能等级上限：**10 级**
- **关键设计：** 1 阶技能满级（Lv.10）的威力 ≈ 4 阶技能 Lv.8 的威力——**技能等级仍是分水岭**，养成路径改为 **施放次数**。

---

# 第二部分：战斗引擎 (Combat Engine)

## 5. 属性映射表

### 5.1 四维 → 战斗属性

| 基础属性 | 战斗映射 |
|---------|---------|
| 1 STR | +3 物理攻击, +2 物理防御, +5 负重上限 |
| 1 AGI | +1% 闪避率(上限60%), +1.5% 攻速加成, +0.5% 暴击率 |
| 1 INT | +3 元素攻击, +2 元素抗性, +8 MP上限 |
| 1 VIT | +15 HP上限, +1 物理防御, +1 元素抗性, +0.3% HP回复/10s |

### 5.2 二级战斗属性

| 属性 | 公式 |
|------|------|
| 最终物理攻击 | $Atk_{phy} = STR \times 3 + Buff$（**装备无攻击**；武器四维仅通过 STR 等间接体现） |
| 最终元素攻击 | $Atk_{ele} = INT \times 3 + Buff$ |
| 最终远程攻击 | $Atk_{bow} = (STR \times 1.5 + AGI \times 1.5) + Buff$ |
| 最大HP | $100 + VIT \times 15 + Level \times 20 + EquipHP$ |
| 最大MP | $50 + INT \times 8 + Level \times 5 + EquipMP$ |
| 攻击速度 | $BaseSpeed \times (1 + AGI \times 0.015) \times WeightFactor$ |
| 暴击率 | $AGI \times 0.5\% + EquipCrit\%$ （上限 75%） |
| 暴击伤害 | 固定 150%（若保留「词缀叠暴伤」需另配表，与防具进攻词缀已拆离） |

## 6. 混合伤害模型

$$FinalDamage = (Atk_{phy} \times Mult_{phy}) + (Atk_{ele} \times Mult_{ele}) + (Atk_{bow} \times Mult_{bow})$$

**与第 7、10 节对齐：** 上式中各 `Atk_*` 在代入前，应对 **即时伤害** 先做 **分系净攻击**（已扣对应防御后的分量），再乘倍率；**禁止** 对同一分系 **既做「攻击−防御」又再乘一遍同系减伤曲线**（见下节「二选一」）。

## 7. 防御与即时伤害

**防御**：对 **即时伤害** 的减免。即时伤害 **分系**：**物系段、元素直伤段、弓系直伤段** 各自先做 **攻击 − 防御**（不低于最低伤害规则），再合成暴击与浮动等。

- **物防** 挡 **物系与弓系即时段**（弓系直伤按 **物理防御** 结算）。
- **元素抗** `RES_{ele} = a \cdot INT + b \cdot AGI + 装备`（**a > b**），**仅挡元素流水线即时段**；**不挡** 燃烧 DOT、嗜血流失。
- **燃烧 DOT**、**嗜血**：**无视** 物防与元素抗，**独立结算**。
- **减伤曲线（7.1 / 7.2）与「攻击 − 防御」二选一**：同一分系在 **一次结算** 中只采用其中一种主算法，避免 **双重减伤**；具体表由数值配置定。

### 7.1 物理减伤（参考曲线）

$$Reduction_{phy} = \frac{DEF_{phy}}{DEF_{phy} + 200 + 10 \times AttackerLv}$$

上限：75%（**若**本系已用「攻击−防御」为主算法，则本曲线 **不** 再对同一分量叠乘，除非配置显式声明为独立衰减层）

### 7.2 元素减伤（参考曲线）

$$Reduction_{ele} = \frac{RES_{ele}}{RES_{ele} + 150 + 8 \times AttackerLv}$$

上限：70%（同上，与减法主算法 **不重复扣同一刀**）

### 7.3 闪避判定

$$DodgeRate = \frac{AGI_{def} \times 0.01}{1 + \frac{AGI_{atk}}{AGI_{def} + 100}}$$

上限：60%。闪避 = 完全回避该次攻击及附带状态。被 **毒 / 冰冻** 等限制行动的状态期间闪避率归零（具体以状态表为准）。

## 8. 等级差修正（弱压制）

$$LevelFactor = 1 + (AttackerLv - DefenderLv) \times 0.01$$

- 范围：**[0.85, 1.15]**
- 每级仅 1% 伤害差，最大 ±15%

> **设计意图：** 等级压制存在但极弱。一个 20 级玩家带满强技能，对 40 级玩家仅有 -15% 伤害惩罚，完全可以靠技能强度翻盘。**技能是王，等级不是。**

## 9. 负重与机动性

$$FinalAgility = BaseAgility \times (1 - (\frac{Weight_{cur}}{Weight_{max}})^2)$$

- 负重来源：装备重量 + 药水重量（若日后开放跟班/载具等再扩展，默认不计入）
- 负重上限 = $100 + STR \times 5$

## 10. 伤害完整流水线

```
1. 分系即时伤害：各系 (攻击 − 防御) 得基础扣血分量；若配置选用曲线主算法则本步改用 7.1/7.2，与第 7 节「二选一」一致（燃烧 DOT、嗜血另走独立通道）
2. 合成 RawDmg（× 技能倍率等）
3. 暴击判定 → 若暴击: ×CritMultiplier
4. （可选）仅当第 1 步未含减伤曲线时，才可乘 (1 - Reduction) 等与第 7 节一致的单一衰减，避免双重减防
5. 闪避判定 → 若闪避: 归零
6. 浮动因子 → ×Random(0.95, 1.05)
7. 等级差修正 → ×LevelFactor
8. 最终伤害 FinalDmg（最低为 1）
```

## 11. 状态效果 (Status Effects)

**与第 1.1 节「必有克星」对齐：** 控制、DOT、嗜血、毒缓、抗性时长等 **多条轴** 并存，使任意极端配装 **总能在某一轴上被针对**；具体克制关系由技能与装备表铺全，此处只定原则。

### 11.1 负面状态由技能施加（装备不触发）

冰冻、燃烧、毒、嗜血等 **施加在目标身上** 的效果由 **技能**（与武器进攻词缀）驱动；**防具词缀不再**「攻击概率触发冰冻」之类。

| 状态 | 来源（概括） | 效果要点 |
|------|----------------|----------|
| **冰冻** | 法系技能等 | **纯控制**：定身、闪避归零；**无伤害段** |
| **燃烧** | 法系技能等 | **DOT**；DOT **无视元素抗**；持续时间可被 **抗燃烧** 缩短 |
| **毒** | 弓系带毒技能等；**INT** 同时影响 **幅度** 与 **基础持续时间**（无硬性上限） | 综合 debuff：缓行、技能 CD↑、降攻等；**无 DOT** |
| **嗜血** | 物系技能 | 持续数秒：**每秒** 目标 **失去** 一段 HP（**技能等级 + 施放者 VIT**），施放者 **获得等量**；**无视物防与元素抗**；每次触发 **独立计时**，**无层数上限** |

### 11.2 装备耐性（只缩时间）

**抗冰冻 / 抗燃烧 / 抗毒 / 抗嗜血**（防具与武器均可出现）：**只缩短** 自身对应状态的 **持续时间**，数值随机。与 **武器上延长敌人身上负面状态时间** 的进攻词缀 **对仗**。

### 11.3 状态互斥与命中减免（技能侧）

- **冰冻 + 燃烧：** 仍可设计为互相驱散（技能表维护）。
- **毒与冰冻** 等同屏多 debuff 的共存规则由技能表定义。
- **免疫期** 等防连控机制保留在技能 / 状态机层。

技能附带的「基础触发概率」对目标可用下式减免（**与 11.2 耐性不同**，后者只缩时间）：

$$EffectChance = BaseChance \times (1 - \frac{TargetRES}{TargetRES + 300})$$

`TargetRES` 可由 VIT、装备等提供（具体配表）。

## 12. 战斗流程：即时制

RPG 阶段采用 **即时制**，节奏紧凑：

- **普攻间隔：** $\frac{2.0}{1 + AtkSpeed\%}$ 秒（基准 2.0s）
- **技能 CD：** 1阶 3-5s，2阶 5-8s，3阶 10-18s，4阶 15-30s
- **目标选择：** 自动锁定最近敌人，手动可切换；AOE 以释放点为中心
- **自动战斗：** 支持挂机自动普攻+自动释放技能（CD 优先高阶），玩家可配置技能优先级

---

# 第三部分：世界观 (World Setting)

## 13. 「破晓大陆」——东西融合奇幻

### 13.1 世界背景

> 远古时代，东方的仙灵之力与西方的魔法之源在大陆中心碰撞，形成了一颗「破晓之核」。这颗核心维系着整个大陆的元素平衡。千年前，一场被称为「大崩坏」的灾难撕裂了破晓之核，碎片散落各地，催生了魔物与异变。
>
> 各地废墟中涌现出强大的守护者和怪兽，曾经统一的文明四分五裂。散布在大陆各处的核心碎片仍在释放扭曲的力量——谁能收集足够的碎片，谁就能重铸破晓之核，掌握整个大陆的命运。
>
> 你是一个在废墟中苏醒的无名冒险者，没有记忆，只有手中的武器和本能的战斗直觉。在这片弱肉强食的大陆上，变强是唯一的生存法则。

### 13.2 风格关键词

- **东方元素：** 仙灵、气功、灵玉、古阵法、东方建筑废墟
- **西方元素：** 魔法、元素之力、骑士、哥特城堡、龙族遗迹
- **融合感：** 同一场景内可见东方牌坊与西式拱门共存的废墟美学

### 13.3 终局意象（剧情向）

> 破晓之核的碎片散落在大陆各地的战略要地；大陆中心的「破晓圣殿」传说是核心重铸之地。**终局玩法以 PC MMORPG 内的大型争夺、秘境与团队目标承载**，不设 SLG 地块扩张线。

---

# 第四部分：内容设计 (Content Design)

## 14. 技能列表

**学习与升级** 见第 4 节。下表 **等级段** 仅表示技能 **设计分档 / 推荐人物等级区间**（与人物解锁阶段对应，非掉落条件）。

### 14.1 一阶技能（人物 Lv.1–12 段）

| 技能 | 系 | Base | Scale | CD | MP | 效果 |
|------|---|------|-------|----|----|------|
| 劈砍 | 战 | 30 | 0.25 | 3s | 10 | 单体物理 |
| 盾击 | 战 | 20 | 0.20 | 5s | 15 | 单体物理 + 12% 迟钝 |
| 火球术 | 法 | 35 | 0.30 | 4s | 20 | 单体元素 + 5% 燃烧 |
| 冰箭 | 法 | 25 | 0.25 | 4s | 18 | 单体元素 + 8% 冰冻 |
| 速射 | 弓 | 28 | 0.22 | 3s | 12 | 单体远程，连射 2 次 |
| 毒箭 | 弓 | 22 | 0.20 | 5s | 15 | 单体远程 + **毒**（综合 debuff，无 DOT，见第 11 节） |
| 治愈 | 法 | — | 0.15 | 8s | 30 | 回复 HP = INT×3×(1+Lv×Scale) |
| 战吼 | 战 | — | 0.10 | 15s | 25 | 10s 物理攻击 +15% |

### 14.2 二阶技能（人物 Lv.13–24 段）

| 技能 | 系 | Base | Scale | CD | MP | 效果 |
|------|---|------|-------|----|----|------|
| 旋风斩 | 战 | 60 | 0.30 | 6s | 30 | AOE 物理（半径2格） |
| 铁壁 | 战 | — | 0.12 | 20s | 35 | 8s 物理减伤 +25% |
| 火雨 | 法 | 50 | 0.35 | 8s | 45 | AOE 元素（半径3格）+ 15% 燃烧 |
| 寒冰护盾 | 法 | — | 0.15 | 25s | 40 | 吸收 INT×5 伤害，碎裂冰冻周围 |
| 穿甲箭 | 弓 | 70 | 0.28 | 7s | 25 | 单体远程，无视 30% 物防 |
| 散射 | 弓 | 40 | 0.25 | 6s | 30 | 扇形 AOE 60° |

### 14.3 三阶技能（人物 Lv.25–36 段）

| 技能 | 系 | Base | Scale | CD | MP | 效果 |
|------|---|------|-------|----|----|------|
| 天崩地裂 | 战 | 120 | 0.35 | 12s | 60 | 大范围 AOE + 20% 迟钝 |
| 狂战之魂 | 战 | — | 0.20 | 30s | 50 | 15s 攻击+30%、防御-20% |
| 陨石术 | 法 | 150 | 0.40 | 15s | 80 | 超大 AOE + 25% 燃烧 |
| 冰封领域 | 法 | 100 | 0.35 | 18s | 70 | AOE + 30% 冰冻 + 减速场 8s |
| 暴风箭雨 | 弓 | 90 | 0.32 | 10s | 55 | 大范围 AOE 远程 |
| 致命狙击 | 弓 | 200 | 0.30 | 20s | 45 | 单体远程，100% 暴击 |

### 14.4 四阶技能（人物 Lv.37–48 段）

| 技能 | 系 | Base | Scale | CD | MP | 效果 |
|------|---|------|-------|----|----|------|
| 霸王斩 | 战 | 250 | 0.45 | 20s | 100 | 单体极限物理 + 30% 迟钝 |
| 不灭战魂 | 战 | — | — | 120s | 全MP | 复活回 30% HP（每场限 1 次） |
| 天火燎原 | 法 | 200 | 0.50 | 25s | 120 | 全屏元素 + 必定燃烧 |
| 时间冻结 | 法 | 80 | 0.30 | 30s | 100 | AOE 100% 冰冻 3s |
| 万箭齐发 | 弓 | 180 | 0.40 | 22s | 90 | 全屏远程 AOE |
| 影分身 | 弓 | — | — | 60s | 80 | 50% 属性分身 15s |

### 14.5 技能缩放验证

| 对比 | 伤害 |
|------|------|
| 火球术 Lv.10 | $35 \times (1 + 10 \times 0.30) = 35 \times 4.0 = \textbf{140}$ |
| 天火燎原 Lv.8 | $200 \times (1 + 8 \times 0.50) = 200 \times 5.0 = \textbf{1000}$ 但获取几乎不可能 |
| 陨石术 Lv.1 | $150 \times (1 + 1 \times 0.40) = \textbf{210}$ |
| 陨石术 Lv.4 | $150 \times (1 + 4 \times 0.40) = 150 \times 2.6 = \textbf{390}$ |
| 火球术 Lv.10 vs 陨石术 Lv.1 | 140 vs 210，差距仅 50%，但火球 CD 4s vs 陨石 CD 15s |

> 火球术 Lv.10 的 DPS（140 / 4s = 35/s）≈ 陨石术 Lv.1 的 DPS（210 / 15s = 14/s）的 **2.5 倍**。满强低阶技能在实战中碾压初级高阶技能。

## 15. 怪物体系

### 15.1 怪物分级

| 类型 | 标识 | 特征 | 刷新 |
|------|------|------|------|
| 普通怪 | 无 | 固定属性 | 30s |
| 精英怪 | 金名 | 属性 ×3，1 个技能 | 5min |
| 区域Boss | 血条UI | 属性 ×10，3技能+阶段机制 | 被击杀后 1h 原地刷新 |
| 世界Boss | 全服公告 | 属性 ×50，5技能+狂暴 | 12h 定时刷新 |

### 15.2 区域怪物

| 区域 | 等级 | 代表怪物 | 系别 |
|------|------|---------|------|
| 觉醒废墟 | 1-10 | 腐化野兽、游荡亡灵、碎石傀儡 | 物理 |
| 灵风平原 | 11-20 | 风元素游魂、仙灵猎手、狼骑兵 | 混合 |
| 赤焰峡谷 | 21-30 | 火蜥蜴、熔岩巨人、炎魔侍从 | 元素 |
| 霜月荒原 | 31-40 | 寒冰巨狼、暗影骑士、冰霜女巫 | 物理/远程 |
| 破晓神殿外围 | 41-48 | 圣殿守卫、扭曲使徒、虚空行者 | 全系 |
| 深渊秘境 | 45-50 | 破晓碎片守护者、上古元素龙 | 超难全系 |

### 15.3 Boss 举例：熔岩巨人·伊格尼斯

- **等级：** 28（3阶区域精英 Boss）
- **HP：** 80,000
- **阶段1 (100%-50%)：** 「烈焰重击」(单体高伤) + 「岩浆喷涌」(地面 AOE 延迟)
- **阶段2 (50%↓)：** 狂暴，攻速+50%，新增「大地震颤」(全屏延迟 AOE)
- **掉落：** 100% 装备（阶位随机，偏向3阶）+ 强化石等（概率见第 18 节）
- **野外规则：** 任何人可参与攻击，击杀者获得掉落，可被PK抢夺

## 16. 地图设计

### 16.1 一整张无缝大地图

整个破晓大陆是**一张连续的无缝地图**，玩家可以从出生点一路步行到大陆尽头，不存在加载切换。

```
┌──────────────────────────────────────────────────────┐
│                    破晓大陆 (无缝大地图)                │
│                                                      │
│   ┌─────────┐                    ┌─────────┐         │
│   │觉醒废墟  │ ←── 步行 ──→     │灵风平原  │         │
│   │Lv.1-10  │                    │Lv.11-20 │         │
│   │[觉醒镇] │                    │[风灵城] │         │
│   └─────────┘                    └────┬────┘         │
│                                       │              │
│           ┌───────────────────────────┼──────┐       │
│           │                           │      │       │
│   ┌───────┴──┐                 ┌──────┴───┐  │       │
│   │赤焰峡谷   │                 │霜月荒原   │  │       │
│   │Lv.21-30  │                 │Lv.31-40  │  │       │
│   │[焰心营地] │                 │[霜月堡]  │  │       │
│   └──────────┘                 └──────────┘  │       │
│                                              │       │
│                    ┌─────────────────────┐    │       │
│                    │  破晓神殿外围        │    │       │
│                    │  Lv.41-48           │    │       │
│                    │  [圣殿前哨]         │    │       │
│                    │                     │    │       │
│                    │  ┌───────────────┐  │    │       │
│                    │  │ 深渊秘境入口   │  │    │       │
│                    │  │ Lv.45-50      │  │    │       │
│                    │  └───────────────┘  │    │       │
│                    └─────────────────────┘    │       │
│                                              │       │
└──────────────────────────────────────────────┘       │
    [方括号] = 城镇/安全区，内有传送点NPC                   │
```

### 16.2 移动与传送

| 方式 | 说明 |
|------|------|
| 步行 | 任何地方均可步行到达，无隐形墙 |
| 城镇传送 | 每个城镇安全区内有**传送NPC**，消耗灵晶可传送到其他已到达过的城镇 |
| 瞬移卷轴 | 消耗品，使用后传送回最近的城镇 |

- 传送费用：距离越远越贵（100-2000 灵晶）
- 首次到达新城镇自动解锁该传送点
- 红名玩家无法使用传送

### 16.3 安全区与 PK 区

| 区域 | PK 规则 |
|------|---------|
| 城镇/营地内 | 安全区，禁止 PK |
| 野外任何位置 | 自由 PK，触发红名机制 |
| Boss 区域 | 自由 PK（争抢Boss掉落） |
| 深渊秘境 | 强制 PK 开启，无红名惩罚 |

### 16.4 死亡复活

- 死亡后在**最近的城镇复活点**复活
- 复活后有 10s 无敌保护（防蹲尸）
- 复活不消耗任何资源（掉落惩罚在死亡时已结算）

### 16.5 区域Boss分布

所有Boss都在大地图野外固定位置刷新，没有副本系统：

| Boss | 等级 | 类型 | 刷新 | 位置 |
|------|------|------|------|------|
| 骨王·奥西里斯 | 15 | 区域Boss | 1h | 觉醒废墟·亡灵墓地 |
| 风之精灵王 | 25 | 区域Boss | 1h | 灵风平原·古树深处 |
| 熔岩巨人·伊格尼斯 | 35 | 区域Boss | 1h | 赤焰峡谷·火山口 |
| 冰霜暴君 | 42 | 区域Boss | 1h | 霜月荒原·冰封王座 |
| 破晓核心守卫 | 48 | 区域Boss | 2h | 破晓神殿·圣殿门前 |
| 上古元素龙 | 50 | 世界Boss | 12h | 深渊秘境·龙巢（全服公告） |

> 区域Boss 和世界Boss 都在野外，人人可打，可被 PK 抢夺。区域Boss 掉落归击杀者；世界Boss 走贡献阈值随机分配。

### 16.6 世界Boss 规则

- **刷新：** 每 12h 在大地图固定位置刷新，全服公告
- **参与：** 任何玩家均可攻击，野外规则（可被 PK）
- **掉落分配：** 伤害贡献达到**总伤害 1% 以上**的所有玩家，进入掉落分配池。系统对池中玩家**随机分配**掉落物（不按伤害排名）
- **设计意图：** 门槛低（1% 就够），分配随机——鼓励参与而非垄断，小号也有机会

---

# 第五部分：经济系统 (Economy)

## 17. 单一货币：灵晶 (Soulcrystal)

### 17.1 设计原则

游戏内只有**一种货币：灵晶**。所有经济行为围绕灵晶运转。

### 17.2 灵晶来源

| 来源 | 数量 |
|------|------|
| 普通怪掉落 | 5-30 |
| 精英怪掉落 | 50-200 |
| 区域Boss | 300-1500 |
| 世界Boss（参与奖） | 2000+ |
| 玩家 PK 掉落拾取 | 对方掉落的部分 |
| 月卡每日领取 | 500 |

### 17.3 灵晶消耗（沉淀池）

| 消耗 | 数量 |
|------|------|
| 装备强化（强化石 + 灵晶双重消耗） | 见强化表 |
| 药水购买（NPC） | 50-1000/个 |
| 装备修理 | 装备阶位 × 50 |
| 摆摊交易税（5%） | 卖价的 5% |
| 瞬移卷轴 | 500/个 |

### 17.4 经济循环

```
打怪掉落 灵晶 ──┬──→ 强化消耗 (主要沉淀池)
                ├──→ 药水/修理 (日常消耗)
                └──→ 交易税 (流通抽水)

打怪掉落 装备/材料 ──→ 摆摊交易 ──→ 灵晶转移 ──→ 交易税沉淀
```

## 18. 掉落系统：万物皆可掉 (Universal Drop)

> **核心原则：任何怪物都能掉落任何物品。** 1级野猪也可能爆出高阶装备——只是概率远低于高级怪。极简的规则，无限的可能。

### 18.1 掉落概率总表

掉落概率由 **怪物类型** × **物品阶位修正** 共同决定：

**基础掉落率（按怪物类型）：**

| 怪物类型 | 装备 | 强化石 |
|---------|------|--------|
| 普通怪 | 3% | 2% |
| 精英怪 | 20% | 12% |
| 区域Boss | 100% | 40% |
| 世界Boss | 多件 | 100% |

**阶位修正系数（掉落物品的阶位由以下权重随机）：**

| 掉落阶位 | 对应区域怪 | 低1阶区域怪 | 低2阶区域怪 | 低3阶区域怪 |
|---------|-----------|-----------|-----------|-----------|
| 1阶 | 60% | 75% | 85% | 90% |
| 2阶 | 25% | 18% | 10% | 7% |
| 3阶 | 12% | 5% | 4% | 2.5% |
| 4阶 | 3% | 2% | 1% | 0.5% |

> **举例：** 一只觉醒废墟(1阶区域)的普通怪，掉装备概率 3%，其中掉4阶装备的概率 = 3% × 0.5% = 0.015%。约每 6700 只怪出一件4阶。极低但真实存在——这就是"无限的可能"。

### 18.2 装备掉落细则

- 装备掉落时，阶位按上表权重随机
- 基础防御类总值在该阶位范围内随机
- 耐性词缀条数随机（0～4，见 3.4 节）
- 高级怪掉出的装备，基础总值**更倾向高段**（概率偏移）

### 18.3 技能相关掉落

- **已取消** 技能书 / 修炼书掉落；技能通过 **经验解锁**、等级通过 **施放次数** 养成（见第 4 节）。

### 18.4 强化石

| 来源 | 概率 |
|------|------|
| 普通怪 | 2% |
| 精英怪 | 12% |
| 区域Boss | 40% |
| 世界Boss | 100%（多个） |
| 月卡每日 | 3 个 |

## 19. 交易系统

### 19.1 摆摊交易

- 安全区内设立个人摊位
- 系统抽取 **5% 交易税**
- 免费 3 个摆摊位，豪华月卡 6 个

### 19.2 面对面交易

- 同地图距离 ≤ 3 格
- 3% 交易税
- 每日上限 10 次

### 19.3 红名交易限制

- 红名：不可摆摊、不可面对面交易
- 连坐：与红名有近期交易记录者，交易税升至 15%

## 20. 强化详细规则

### 20.1 成功率曲线

| 等级 | 成功率 | 强化石 | 灵晶 | 流光 |
|------|--------|--------|------|------|
| +1→+2 | 95% | 1 | 100 | — |
| +2→+3 | 90% | 1 | 200 | — |
| +3→+4 | 80% | 2 | 500 | 白色微光 |
| +4→+5 | 70% | 3 | 1,000 | — |
| +5→+6 | 55% | 5 | 2,000 | 青色流光 |
| +6→+7 | 45% | 8 | 5,000 | — |
| +7→+8 | 35% | 12 | 10,000 | 蓝色流光 |
| +8→+9 | 25% | 18 | 20,000 | — |
| +9→+10 | 18% | 25 | 40,000 | 紫色流光 |
| +10→+11 | 12% | 35 | 80,000 | — |
| +11→+12 | 8% | 50 | 150,000 | 金色流光 |
| +12→+13 | 5% | 70 | 300,000 | 红色烈焰 |
| +13→+14 | 3% | 100 | 500,000 | — |
| +14→+15 | 1% | 150 | 1,000,000 | 彩虹流光 |

- 每级提升 **防御向** 属性（七部位 **同一递增速率表**，与阶位无关）；具体百分比或固定值由配表决定
- 失败不碎不降，仅消耗材料

### 20.2 药水体系

| 药水 | 效果 | CD | 重量 | 价格(灵晶) |
|------|------|----|----|-----------|
| 小回复药 | +200 HP | 5s | 1 | 50 |
| 中回复药 | +800 HP | 5s | 2 | 200 |
| 大回复药 | +2500 HP | 8s | 3 | 1,000 |
| 特效回复药 | +50% MaxHP | 15s | 5 | Boss掉落 |
| 蓝瓶(小) | +100 MP | 5s | 1 | 50 |
| 蓝瓶(大) | +500 MP | 8s | 3 | 500 |
| 解毒药 | 清除负面 | 30s | 1 | 100 |
| 瞬移卷轴 | 传送回城 | 60s | 0.5 | 500 |

药水堆叠上限：50 个/组。

### 20.3 背包系统：纯负重制

- **没有格子概念**，背包容量完全由负重决定
- 负重上限 = $100 + STR \times 5$
- 所有物品都有重量（装备、药水、强化石、箭矢等）
- 超重后无法拾取新物品，且移速大幅下降
- **仓库：** 各城镇安全区有NPC仓库，存储上限 = 负重上限 × 2（月卡用户 ×3）
- 仓库内物品不计入角色负重

---

# 第六部分：PK 与社会秩序 (PK & Social)

## 21. PK 红名机制

| 规则 | 详情 |
|------|------|
| 触发 | 攻击/击杀白名玩家 → 变红 |
| 红名限制 | 无法回城、无法 NPC 交易、无法摆摊 |
| 洗白 | 保持在线，击杀红名时长随 PK 值递减 |
| 深渊秘境例外 | 秘境内 PK 不增加红名值 |

## 22. 死亡掉落

| 状态 | 装备掉落 | 背包物品 | 灵晶损失 |
|------|---------|---------|---------|
| 白名·PVE死亡 | 0% | 0% | -5% |
| 白名·PVP死亡 | 5%（随机1件） | 10%（1-2件） | -10% |
| 红名·任何死亡 | 15%（1-2件） | 25%（2-4件） | -20% |

## 23. 社交系统

### 23.1 组队

- 上限 3 人（小队规模固定，便于匹配与协作）
- 经验均分：3人各得 40%（总 120%，鼓励组队）
- 掉落：轮流/自由拾取（队长设）
- 自动匹配：±5 级范围

### 23.2 聊天

| 频道 | 范围 | 开放条件 |
|------|------|---------|
| 世界 | 全服 | 20 级，间隔 10s |
| 区域 | 同地图 | 即时开放 |
| 组队 | 队内 | 组队后 |
| 私聊 | 1v1 | 好友/同组队 |

- 好友上限 50 人
- 屏蔽功能

### 23.3 排行榜

| 榜单 | 依据 | 刷新 |
|------|------|------|
| 等级榜 | 等级+经验 | 实时 |
| 战力榜 | 综合评分 | 每小时 |
| PK榜 | 击杀数 | 实时 |
| 强化榜 | 最高强化等级 | 实时 |

---

# 第七部分：商业化 (Monetization)

## 24. 免费 + 月卡增益模式

### 24.1 核心原则

- 所有玩家均可免费游玩全部内容
- 月卡提供**便利和效率增益**，不提供独占战力
- 不出售任何影响战力的道具（不卖强化石、装备等）

### 24.2 月卡内容

| 项目 | 效果 | 定价 |
|------|------|------|
| **标准月卡** | 每日领取：500灵晶 + 3强化石 + 经验+20%加成 | ¥30/月 |
| **豪华月卡** | 标准月卡全部 + 自动拾取 + 摆摊位数+3 + 仓库容量×3 + 经验+50%加成 | ¥68/月 |

### 24.3 额外付费项（纯外观/便利）

| 项目 | 价格 | 说明 |
|------|------|------|
| 时装套装 | ¥30-98 | 纯外观，无属性 |
| 武器特效皮肤 | ¥50 | 纯外观，武器额外粒子特效 |
| 改名卡 | ¥18 | 修改角色名 |
| 仓库扩容 | ¥12 | 永久增加仓库负重上限 +50 |

### 24.4 激励视频广告（免费玩家福利）

| 场景 | 奖励 | 次数/日 |
|------|------|---------|
| Boss掉落翻倍 | 击杀Boss时掉落 ×2 | 3 |
| PVE 死亡复活 | 原地复活 50% HP | 3 |
| 每日免费强化石 | 1 个/次 | 3 |

> 每日广告上限 10 次，保护体验。

---

# 第八部分：经验曲线与成长节奏 (Progression)

## 25. 经验曲线

| 等级区间 | 单级经验 | 累计游戏时长 |
|---------|---------|------------|
| 1-10 | 500 → 5,000 | ~2 小时 |
| 11-20 | 8,000 → 30,000 | ~10 小时 |
| 21-30 | 40,000 → 120,000 | ~30 小时 |
| 31-40 | 150,000 → 500,000 | ~90 小时 |
| 41-45 | 800,000 → 2,000,000 | ~240 小时 |
| 46-48 | 3,000,000 → 8,000,000 | ~500 小时 |
| 49 | 15,000,000 | ~700 小时 |
| 50 | 30,000,000 | ~1000 小时 |

> RPG 阶段是一个**漫长的旅程**。满级 50 需要约 1000 小时的游戏时间，这是设计意图——慢成长让每一级、每一件装备、每一次技能释放都有重量。

## 26. 战力评分公式

$$CombatPower = ATK_{total} + DEF_{total} \times 0.8 + HP_{max} \times 0.02 + AGI \times 5 + SkillPower$$

其中 $SkillPower = \sum (SkillBase_i \times SkillLevel_i \times TierWeight_i)$

---

# 第九部分：产品范围说明

**本产品仅规划为 PC 端 MMORPG**（Windows 客户端为主，可扩展其他桌面平台）。**不做** SLG 扩张、不做微信小游戏主线交付。原「兵力 / 地块 / WarSquad」等 SLG 预留已从规约中移除；组队、秘境、破晓圣殿等仍以 **正文 MMORPG 系统** 为准。

---

# 第十部分：技术架构 (Technical Architecture)

## 30. 技术栈

| 层级 | 选型 | 理由 |
|------|------|------|
| **引擎** | Cocos Creator 3.8+ (3D模式) | 跨平台桌面发布成熟；3D 渲染支持固定俯视角 2.5D 效果 |
| **客户端语言** | TypeScript | 类型安全，适合复杂游戏逻辑 |
| **渲染模式** | 3D 场景 + 固定 45° 俯视角摄像机 | 经典 MU 风格 2.5D 视觉 |
| **服务端** | Node.js (Colyseus) | 实时对战框架，WebSocket 原生支持，TS 全栈统一 |
| **数据库** | MySQL + Redis | MySQL 持久化（角色/装备/技能），Redis 缓存（会话/排行/在线状态） |
| **通信** | WebSocket + Protobuf | 长连接实时性，Protobuf 压缩 |
| **运维** | Docker + 云主机 | 按发行地区与预算选型 |

## 31. 服务器架构：单服模式

采用**单服**模式——所有玩家共享同一个世界。人多了开新服。

```
┌──────────────────────────────────────────┐
│           PC 游戏客户端（Windows 等）      │
│  ┌────────────┐ ┌──────────┐ ┌────────┐  │
│  │ Cocos 3D   │ │ UI/HUD   │ │Network │  │
│  │ Renderer   │ │ Layer    │ │Module  │  │
│  │(45°Camera) │ │          │ │        │  │
│  └────────────┘ └──────────┘ └───┬────┘  │
└──────────────────────────────────┼────────┘
                                   │ WebSocket + Protobuf
                                   ▼
┌──────────────────────────────────────────┐
│          单服 Game Server                 │
│  ┌───────────┐  ┌──────────────────────┐ │
│  │ RPG 逻辑  │  │ 无缝大地图管理器      │ │
│  │ 战斗结算   │  │ (区域AOI + 视野管理)  │ │
│  │ PK/掉落   │  └──────────────────────┘ │
│  │ 挂机AI    │                           │
│  └───────────┘                           │
└────────┬─────────────────────────────────┘
         ▼
┌──────────────┐  ┌──────────────┐
│    MySQL     │  │    Redis     │
│ (持久存储)    │  │ (缓存/排行)   │
└──────────────┘  └──────────────┘
```

- **单服容量目标：** 同时在线 2000-5000 人
- **AOI (Area of Interest)：** 九宫格算法，只同步玩家视野范围内的实体
- **开服/合服：** 新服在线饱和后开新服；老服活跃度下降后合服

## 32. 网络同步

### 32.1 状态同步（服务端权威）

- 客户端发送操作指令（移动/攻击/使用技能/使用物品）
- **服务端计算所有战斗结果**并广播
- 客户端预测移动，服务端校正

### 32.2 挂机优化

纯刷怪游戏的挂机效率至关重要：

- 客户端发送「开始挂机」指令，附带技能优先级配置
- 服务端接管战斗 AI 逻辑（减少通信频率）
- 挂机期间服务端每 5s 推送一次状态摘要（代替逐帧推送）
- 客户端可选择显示战斗动画或仅显示数字滚动（节省性能）

### 32.3 断线重连

- 断线后服务端保持角色状态 120s（挂机游戏需要更长容忍时间）
- 重连推送完整状态快照
- 超时下线，挂机中断

## 33. 反作弊

| 场景 | 策略 |
|------|------|
| 战斗伤害 | 全部服务端计算 |
| 移动速度 | 服务端校验距离/时间 |
| 物品操作 | 全部服务端验证 |
| 交易 | 双重确认 + 日志留档 |
| 协议 | Protobuf 编码 + 包序号防重放 |

## 34. PC 客户端：资源分包与性能

### 34.1 分包 / 按需加载（示意）

| 包体 | 内容 | 预算（可随发行调整） |
|------|------|------|
| 首包 | 引擎 + 登录 + 觉醒废墟 | 控制首装体积 |
| 区域包 1 | 灵风平原 + 赤焰峡谷 | 进图再下 |
| 区域包 2 | 霜月荒原 + 破晓神殿 | 进图再下 |
| 资源包 | 音效、高清模型、CG | CDN / 启动器按需拉取 |

### 34.2 桌面端能力

- **登录与账号**：自研或第三方账号体系（与具体发行方案绑定）。
- **社交分享 / 广告**：若接入，以 **PC 渠道规范** 为准，不作为核心战斗依赖。

### 34.3 性能预算

| 指标 | 目标 |
|------|------|
| 帧率 | 30 FPS 稳定，战斗不低于 24 |
| 内存 | ≤ 350MB |
| 同屏角色 | 最多 20 个可见 |
| Draw Call | ≤ 80（3D场景预算比2D高） |
| 首屏加载 | ≤ 5s |
| 3D 模型面数 | 角色 ≤ 2000 面，场景 ≤ 50000 面 |

---

# 第十一部分：美术与体验 (Art & UX)

## 35. 美术风格

### 35.1 整体方向

- **风格：** 3D Low-Poly + 东西融合奇幻色调
- **场景：** 东方飞檐与西方拱门共存的废墟世界；用色偏暗沉但有亮色点缀（灵晶的蓝紫光芒）
- **角色：** Low-Poly 风格 3D 模型（2000面以内），保持可辨识的轮廓
- **参考游戏：** 奇迹MU的俯视角感 + 低多边形独立游戏的美术质感

### 35.2 UI 风格

- 深色石质底纹 + 灵晶蓝/紫发光边框
- 字体：标题用带有古韵的硬笔体，正文用无衬线清晰字体
- 界面简洁：底部 Tab（角色/背包/技能/社交/设置）
- 挂机状态下 UI 可一键收缩至最小化（只显示 HP/MP 条 + 小地图）

### 35.3 特效

| 场景 | 效果 |
|------|------|
| 物理攻击 | 刀光 + 金属碰撞粒子 |
| 元素技能 | 火焰/冰晶/风刃的 3D 粒子特效 |
| 强化流光 | 装备外发光边缘（颜色见强化表） |
| 升级 | 光柱上升 + 金色粒子爆发 |
| Boss 出场 | 地面裂缝 + 暗红色雾气 |

## 36. 音效音乐

### 36.1 BGM

| 场景 | 风格 |
|------|------|
| 主城/安全区 | 竖琴 + 古琴，舒缓悠远 |
| 野外 | 低沉鼓点 + 弦乐暗流 |
| 战斗 | 快节奏打击乐 + 管弦乐 |
| Boss 战 | 史诗级交响 + 人声吟唱 |
| 深渊秘境 | 低频电子 + 诡异回声 |

### 36.2 音效

| 类别 | 项目 |
|------|------|
| UI | 点击、开关面板、确认、取消、获得物品、灵晶叮当 |
| 战斗 | 刀剑、弓弦、火球爆炸、冰冻结晶、治愈光环 |
| 系统 | 升级、强化成功/失败、暴击、闪避、死亡 |
| 环境 | 废墟风声、岩浆沸腾、冰原呼啸、深渊低语 |

## 37. 新手体验

> 没有任务系统，但需要平滑引导：

### 37.1 前 10 分钟

```
0:00  简短 CG（大崩坏 + 废墟中苏醒，可跳过）
0:30  角色创建（性别 + 外观）
1:00  操作教学：虚拟摇杆移动 + 点击攻击（击杀 3 只腐化野兽）
2:00  拾取第一件装备 → 教学：装备穿戴
3:00  学习第一个技能「劈砍」→ 教学：技能使用
4:00  遇到精英怪 → 教学：使用药水
5:00  到达第一个安全营地 → 教学：NPC 商店
6:00  自由刷怪开始
8:00  升到 5 级 → 教学：属性点分配（弹窗强调不可逆）
10:00 教学完成，彻底自由探索
```

### 37.2 关键解锁节点

| 等级 | 解锁 | 提示 |
|------|------|------|
| 5 | 属性分配 | 不可重置警告 |
| 10 | 强化系统 | 赠送 5 强化石 |
| 13 | 2阶区域开放 | 地图提示 |
| 15 | 摆摊交易 | 交易引导 |
| 20 | 组队系统 | 自动匹配引导 |
| 25 | PK 系统开放 | 红名规则说明 |
| 37 | 4阶区域开放 | — |
| 45 | 深渊秘境 | PK 无惩罚说明 |

---

# 附录

## A. 版本路线图

| 版本 | 内容 | 预计周期 |
|------|------|---------|
| v0.1 Alpha | 核心战斗 + 觉醒废墟 + 装备/技能/强化 | 8 周 |
| v0.2 Beta | 完整大地图 + 区域Boss + 组队 + 挂机系统 | 6 周 |
| v0.3 | PK + 红名 + 交易 + 排行榜 | 4 周 |
| v0.4 | 世界Boss + 深渊秘境 | 4 周 |
| v0.5 | 月卡 + 广告 + 商业化 | 3 周 |
| v1.0 上线 | 全功能打磨 + 压测 | 4 周 |
| v2.0 (远期) | 大型资料片 / 新地图与终局玩法（仍以 MMORPG 为边界） | 待定 |

## B. 项目目录结构（Cocos Creator 3D）

> **当前仓库**仅保留根目录 `README.MD`；下列为 **工程创建后** 的示意布局，供后续开发计划对齐。

```
project-root/
├── assets/
│   ├── scenes/          # 各地图场景
│   ├── models/          # 3D模型（角色/怪物/装备）
│   ├── textures/        # 贴图
│   ├── effects/         # 特效/粒子
│   ├── audio/           # BGM + 音效
│   ├── scripts/
│   │   ├── core/        # 核心系统（属性/战斗/伤害计算）
│   │   ├── entity/      # 实体（玩家/怪物/NPC）
│   │   ├── combat/      # 战斗引擎
│   │   ├── inventory/   # 背包/装备/强化
│   │   ├── skill/       # 技能系统
│   │   ├── social/      # 组队/聊天/排行
│   │   ├── pk/          # PK/红名/掉落
│   │   ├── economy/     # 交易/商店
│   │   ├── network/     # 网络通信层
│   │   ├── ui/          # UI组件
│   │   ├── ai/          # 挂机AI/怪物AI
│   │   └── platform/    # 桌面端适配（分辨率、键鼠、窗口等）
│   └── ui/              # UI预制件/图集
├── server/
│   ├── src/
│   │   ├── rooms/       # Colyseus房间（地图实例）
│   │   ├── schemas/     # 状态同步Schema
│   │   ├── combat/      # 服务端战斗计算
│   │   ├── db/          # 数据库操作层
│   │   └── anti-cheat/  # 反作弊校验
│   ├── config/          # 数值配置表（JSON）
│   └── proto/           # Protobuf定义
├── shared/
│   └── interfaces/      # 客户端/服务端共享类型定义
└── docs/                # 文档目录（待按开发计划重建）
```

## C. 数值配置表格式

所有数值配置使用 JSON 文件，便于热更新：

```json
{
  "skills": {
    "fireball": {
      "tier": 1,
      "type": "ele",
      "base": 35,
      "scale": 0.30,
      "cd": 4,
      "mp": 20,
      "effects": [{ "type": "burn", "chance": 5 }]
    }
  },
  "skill_progression": {
    "note": "技能：经验解锁；Lv.1–10：施放次数养成（见正文第4节）"
  },
  "monsters": {
    "corrupted_beast": {
      "level": 3,
      "hp": 150,
      "atk_phy": 20,
      "drop_table": "drop_awakening_normal"
    }
  },
  "drop_tables": {
    "drop_awakening_normal": {
      "soulcrystal": { "min": 5, "max": 30 },
      "equip_tier1": { "chance": 0.03 },
      "enhance_stone": { "chance": 0.02 }
    }
  },
  "enhance": {
    "1": { "rate": 95, "stones": 1, "gold": 100 },
    "2": { "rate": 90, "stones": 1, "gold": 200 }
  }
}
```

---

## D. 开发计划（仓库重建后执行）

以下为 **PC 端《破晓纪元》** 与正文规约对齐的 **推荐阶段**；周期为粗估，可按人力调整。**实际引擎栈：** Unity（`EpochOfDawn/`），与下表早期「Cocos」表述以本节为准。

| 阶段 | 目标 | 主要交付 |
|------|------|-----------|
| **D0 工程奠基** | 可运行的空壳 | 安装 Unity Hub + **Unity 2022 LTS**；新建 **URP 3D** 工程；**Windows Build** 跑通；**`.gitignore`** 排除 `Library/` 等 |
| **D1 垂直切片** | 单场景闭环 | **已完成**：地面 + 角色；WASD；相机跟随；普攻；敌人生死与重生；掉落与拾取；最小 HUD；可 `Build And Run` |
| **D2 战斗与状态** | 可玩小循环 | 技能 **CD/MP**；状态 **最小子集**（对齐第 11 节方向）；装备 **耐性 + 力量需求** 数据结构；GitHub 首提交与 `docs/` 入门 |
| **D3 成长与经济** | 刷装意义 | 经验 **升级 vs 解锁技能**（第 4 节）；施放计数升技能级；掉落 + 背包负重；强化 **全装同一曲线** |
| **D4 世界与社交** | MMORPG 壳 | 无缝或分区大地图 AOI；组队/聊天占位；PK 与红名 **可先关开关** |
| **D5 商业化与运维** | 可上线灰度 | 月卡与反作弊按正文；压测与合服策略 |

**并行原则：** 客户端表现与 **服务端战斗校验** 从 **D2** 起就要预留接口（日志/命令式校验亦可），避免后期推翻。

**下一步：** **D2-1 已完成**；按 **「D2 计划」子任务表** 从 **D2-2** 起继续；需要时可在 `d:\mygame\` 增加 **`server/` 占位** 与根级 **`docs/getting-started.md`**（与 `EpochOfDawn/` 并列）。

## 本次续写（2026-04-17，P2 第2组继续）

- 完成 `PlayerStateExportSimple` 的 DTO/数据结构分层：新增 `PlayerStateExportSimple.Types.cs`，集中托管 D5 请求/响应 Lite DTO、本地事件结构与枚举。
- 完成字段层拆分：新增 `PlayerStateExportSimple.Fields.cs`，集中托管 Inspector 配置、运行态缓存、统计字段与属性。
- 主文件 `PlayerStateExportSimple.cs` 进一步收敛为生命周期入口（`Awake/Reset/Update`）与调度逻辑，继续保持行为不变。
- 清理主文件头部依赖，移除已不再使用的 `using`，降低后续冲突面。

## 本次续写（2026-04-17，P2 提速拆分）

- 完成 `Fields` 二次拆分并落地为三块：`PlayerStateExportSimple.Config.cs`（Inspector/配置）、`PlayerStateExportSimple.RuntimeState.cs`（运行态/队列/状态机字段）、`PlayerStateExportSimple.MetricsState.cs`（HUD 指标摘要字段）。
- 删除历史聚合文件 `PlayerStateExportSimple.Fields.cs`，降低单文件体积与多人并行修改冲突概率。
- 保持生命周期与行为不变：`PlayerStateExportSimple.cs` 继续仅承担入口与调度职责，状态存储仅做文件搬迁不改语义。
- 已完成 lint 检查，当前新增与改动文件未发现错误。

## 本次续写（2026-04-17，P2 提速拆分-2）

- 将 `PlayerStateExportSimple.Config.cs` 继续细分为 4 个配置文件：
  - `PlayerStateExportSimple.ConfigPost.cs`（导出与 POST/队列相关配置）
  - `PlayerStateExportSimple.ConfigGet.cs`（基础 GET rehearsal 配置）
  - `PlayerStateExportSimple.ConfigMetricsQuery.cs`（Metrics 相关开关与查询参数）
  - `PlayerStateExportSimple.ConfigRefs.cs`（组件引用与 `Hotkeys` 访问）
- 删除旧聚合文件 `PlayerStateExportSimple.Config.cs`，进一步减少单文件规模与回归面。
- 本轮为纯搬迁重组，不改业务行为；生命周期与流程保持原样。
- 已完成 lint 检查，当前改动文件无错误。

## 本次续写（2026-04-17，P2 提速拆分-3）

- 将 `PlayerStateExportSimple.RuntimeState.cs` 按职责继续拆分为 3 个文件：
  - `PlayerStateExportSimple.RuntimeState.InFlight.cs`（in-flight 状态、队列容器、重试倒计时）
  - `PlayerStateExportSimple.RuntimeState.LastResult.cs`（最近一次请求结果、HTTP/时延、服务端校验回显）
  - `PlayerStateExportSimple.RuntimeState.CountersAndStreaks.cs`（累计计数、连胜/连败、最近成功时间戳）
- 删除旧聚合文件 `PlayerStateExportSimple.RuntimeState.cs`，进一步降低单文件复杂度。
- 本轮仍为字段搬迁重组，不改任何业务流程与行为。
- 已完成 lint 检查，新增与改动文件无错误。

## 本次续写（2026-04-17，P2 连做20步）

- 连续完成 20 个拆分步骤（纯搬迁/分层，不改行为），核心目标：进一步降低单文件复杂度、减少 AI 连续改动时的误伤面与回归面。
- `RehearsalEntrypoints` 拆分完成：
  - 新增 `PlayerStateExportSimple.RehearsalEntrypoints.Basic.cs`
  - 新增 `PlayerStateExportSimple.RehearsalEntrypoints.Metrics.cs`
  - 删除旧 `PlayerStateExportSimple.RehearsalEntrypoints.cs`
- `ParseMetrics` 拆分完成：
  - 新增 `PlayerStateExportSimple.ParseMetrics.Core.cs`
  - 新增 `PlayerStateExportSimple.ParseMetrics.Rows.cs`
  - 新增 `PlayerStateExportSimple.ParseMetrics.Alerts.cs`
  - 删除旧 `PlayerStateExportSimple.ParseMetrics.cs`
- `IOAndQueue` 拆分完成：
  - 新增 `PlayerStateExportSimple.IOAndQueue.QueueOps.cs`
  - 新增 `PlayerStateExportSimple.IOAndQueue.Storage.cs`
  - 新增 `PlayerStateExportSimple.IOAndQueue.Events.cs`
  - 新增 `PlayerStateExportSimple.IOAndQueue.DtoAndUtils.cs`
  - 删除旧 `PlayerStateExportSimple.IOAndQueue.cs`
- `HudText` 拆分完成：
  - 新增 `PlayerStateExportSimple.HudText.SyncLine.cs`
  - 新增 `PlayerStateExportSimple.HudText.Events.cs`
  - 新增 `PlayerStateExportSimple.HudText.Status.cs`
  - 新增 `PlayerStateExportSimple.HudText.BeginnerAndTuning.cs`
  - 删除旧 `PlayerStateExportSimple.HudText.cs`
- `MetricsState` 拆分完成：
  - 新增 `PlayerStateExportSimple.MetricsState.Basic.cs`
  - 新增 `PlayerStateExportSimple.MetricsState.Advanced.cs`
  - 删除旧 `PlayerStateExportSimple.MetricsState.cs`
- 已完成 lint 检查：当前改动文件未发现错误。

## 本次续写（2026-04-17，P2 连做20步-2）

- 持续执行“纯搬迁、行为不变”的细拆，继续压低单文件复杂度并减少 AI 连续改动冲突面。
- `CoroutinePullBasic` 拆分完成：
  - 新增 `PlayerStateExportSimple.CoroutinePullBasic.State.cs`
  - 新增 `PlayerStateExportSimple.CoroutinePullBasic.Health.cs`
  - 新增 `PlayerStateExportSimple.CoroutinePullBasic.Players.cs`
  - 删除旧 `PlayerStateExportSimple.CoroutinePullBasic.cs`
- `CoroutineMetricsA` 拆分完成：
  - 新增 `PlayerStateExportSimple.CoroutineMetricsA.Recent.cs`
  - 新增 `PlayerStateExportSimple.CoroutineMetricsA.Report.cs`
  - 删除旧 `PlayerStateExportSimple.CoroutineMetricsA.cs`
- `CoroutineMetricsB` 拆分完成：
  - 新增 `PlayerStateExportSimple.CoroutineMetricsB.Players.cs`
  - 新增 `PlayerStateExportSimple.CoroutineMetricsB.Codes.cs`
  - 新增 `PlayerStateExportSimple.CoroutineMetricsB.Rejections.cs`
  - 删除旧 `PlayerStateExportSimple.CoroutineMetricsB.cs`
- `CoroutineMetricsC` 拆分（本轮已落地 3 个核心文件）：
  - 新增 `PlayerStateExportSimple.CoroutineMetricsC.Anomalies.cs`
  - 新增 `PlayerStateExportSimple.CoroutineMetricsC.Alerts.cs`
  - 新增 `PlayerStateExportSimple.CoroutineMetricsC.AlertPlayers.cs`
  - 新增 `PlayerStateExportSimple.CoroutineMetricsC.Dashboard.cs`
  - 删除旧 `PlayerStateExportSimple.CoroutineMetricsC.cs`
- `ParseLite` 拆分完成：
  - 新增 `PlayerStateExportSimple.ParseLite.StateAndHealth.cs`
  - 新增 `PlayerStateExportSimple.ParseLite.PostError.cs`
  - 新增 `PlayerStateExportSimple.ParseLite.Players.cs`
  - 删除旧 `PlayerStateExportSimple.ParseLite.cs`
- 已完成 lint 检查：当前改动文件未发现错误。

## 本次续写（2026-04-17，P2 连续推进）

- 继续按“只搬迁不改行为”细拆，完成以下模块分层：
- `ParseHelpers` 拆分完成：
  - 新增 `PlayerStateExportSimple.ParseHelpers.StateError.cs`
  - 新增 `PlayerStateExportSimple.ParseHelpers.WarningsByCode.cs`
  - 新增 `PlayerStateExportSimple.ParseHelpers.JsonObject.cs`
  - 删除旧 `PlayerStateExportSimple.ParseHelpers.cs`
- `SnapshotAndRisk` 拆分完成：
  - 新增 `PlayerStateExportSimple.SnapshotAndRisk.Export.cs`
  - 新增 `PlayerStateExportSimple.SnapshotAndRisk.Auto.cs`
  - 新增 `PlayerStateExportSimple.SnapshotAndRisk.HistoryAndRisk.cs`
  - 删除旧 `PlayerStateExportSimple.SnapshotAndRisk.cs`
- `Export` 拆分完成：
  - 新增 `PlayerStateExportSimple.Export.Core.cs`
  - 新增 `PlayerStateExportSimple.Export.RequestPayload.cs`
  - 删除旧 `PlayerStateExportSimple.Export.cs`
- `D5Hints` 拆分完成：
  - 新增 `PlayerStateExportSimple.D5Hints.HotkeyLine.cs`
  - 新增 `PlayerStateExportSimple.D5Hints.ProbeSuffixes.cs`
  - 删除旧 `PlayerStateExportSimple.D5Hints.cs`
- `ServerValidation` 拆分完成：
  - 新增 `PlayerStateExportSimple.ServerValidation.Reset.cs`
  - 新增 `PlayerStateExportSimple.ServerValidation.Parse.cs`
  - 删除旧 `PlayerStateExportSimple.ServerValidation.cs`
- 已完成 lint 检查：当前改动文件无错误。

## 本次续写（2026-04-17，P2 继续提速）

- 继续完成剩余核心块细拆（纯搬迁、行为不变）：
- `Prefs` 拆分完成：
  - 新增 `PlayerStateExportSimple.Prefs.Tuning.cs`
  - 新增 `PlayerStateExportSimple.Prefs.EventPanel.cs`
  - 删除旧 `PlayerStateExportSimple.Prefs.cs`
- `D5Prefs` 拆分完成：
  - 新增 `PlayerStateExportSimple.D5Prefs.Load.cs`
  - 新增 `PlayerStateExportSimple.D5Prefs.Save.cs`
  - 删除旧 `PlayerStateExportSimple.D5Prefs.cs`
- `Tuning` 拆分完成：
  - 新增 `PlayerStateExportSimple.Tuning.Cycle.cs`
  - 新增 `PlayerStateExportSimple.Tuning.Apply.cs`
  - 删除旧 `PlayerStateExportSimple.Tuning.cs`
- `CoroutinePost` 拆分完成：
  - 新增 `PlayerStateExportSimple.CoroutinePost.Main.cs`
  - 新增 `PlayerStateExportSimple.CoroutinePost.Finalize.cs`
  - 删除旧 `PlayerStateExportSimple.CoroutinePost.cs`
- 已完成 lint 检查：当前改动文件无错误。

## 本次续写（2026-04-17，P2 收口巡检与微拆）

- 完成模块边界巡检：确认当前 `PlayerStateExportSimple` 未出现同名方法/字段冲突，且拆分后引用关系稳定。
- 基于巡检继续做小粒度拆分（行为不变）：
  - 新增 `PlayerStateExportSimple.RehearsalEntrypoints.MetricsA.cs`
  - 新增 `PlayerStateExportSimple.RehearsalEntrypoints.MetricsB.cs`
  - 删除旧 `PlayerStateExportSimple.RehearsalEntrypoints.Metrics.cs`
- 当前目标：让后续 AI 连续修改时，单次上下文集中在更少 endpoint，降低误改风险。
- 已完成 lint 检查：当前改动文件无错误。

## 本次续写（2026-04-17，P2 收官体检）

- 完成一次模块化收官体检（按文件方法密度 + 关键字段分布扫描）：
  - 当前无同名方法/字段冲突；
  - 拆分后引用关系稳定，未出现缺失符号问题；
  - 高频风险点主要集中在 `CoroutineMetrics*` 请求模板重复（属于可接受的“显式重复”，便于后续逐接口调试）。
- 在体检基础上继续微拆并收口：
  - `RehearsalEntrypoints.Metrics` 已分为 `MetricsA/MetricsB` 两块，降低单文件上下文切换成本。
- 当前状态结论：
  - `PlayerStateExportSimple` 已完成高颗粒度模块化，主干逻辑与状态结构清晰可维护；
  - 已适合从“结构优化阶段”切回“功能迭代阶段”（例如 D5 联调体验、HUD 可读性、异常提示策略）。
- 已完成 lint 检查：当前改动文件无错误。

## 本次续写（2026-04-17，功能迭代-1）

- 开始功能向优化（不改网络流程）：增强 D5 新手动作提示与优先级判定。
- 在 `PlayerStateExportSimple.HudText.BeginnerAndTuning.cs` 中新增三类明确指引：
  - 当服务端校验返回高危告警（`LastServerWarnHigh > 0`）时，提示优先查看 `SrvVal` / `warningsByCode` 并标记为紧急。
  - 当 `metrics/dashboard` 或 `metrics/alerts` 拉取失败时，提示先检查窗口/阈值参数。
  - 当 `metrics/recent` 拉取失败时，提示先检查 `limit` 与服务端 metrics 数据文件。
- 同步更新 `BuildBeginnerActionPriorityLabel()`：上述场景会提升为 `[紧急]` 或 `[注意]`，让 HUD 行动建议更可执行。
- 已完成 lint 检查：本次改动无错误。

## 本次续写（2026-04-17，功能迭代-2）

- 继续优化 D5 快速排障指引：重写 `BuildQuickOpsChecklist()` 的判断顺序，改为按失败类型分支输出三条短路径。
- 新增三类“快修路径”：
  - `快修[参数]`：针对 metrics 参数/阈值风险（days/top/limit、alerts 阈值、since/until、SrvVal 高危告警）。
  - `快修[POST]`：针对 POST 失败（先看 `last_post_error` + `SrvVal`，再清队列和重试）。
  - `快修[GET]`：针对 GET 失败（先看失败接口状态，再校验 `localServerBaseUrl` 与端口，最后按对应快捷键重拉）。
- 保留原有风险/限频/常规分支作为兜底，不改变网络请求逻辑。
- 已完成 lint 检查：本次改动无错误。

## 本次续写（2026-04-17，功能迭代-3）

- 继续提升“可执行性”：在 `BuildQuickOpsChecklist()` 的三类快修路径中加入统一按键提示片段（POST/GET/清队列/立即重试）。
- 新增内部辅助方法：
  - `ResolveHotkeySymbol(...)`：按当前热键配置生成稳定可读符号。
  - `BuildActionKeysHint()`：生成统一动作键提示（例如 `[F3/F2/F1/\\]`，会跟随自定义热键变化）。
- 结果：快修文案从“文字建议”升级为“文字 + 具体按键”的动作指令，降低联调时的来回确认成本。
- 已完成 lint 检查：本次改动无错误。

## 本次续写（2026-04-17，功能迭代-连做20步）

- 以“联调效率优先”为目标，完成 20 个高性价比动作决策与可读性增强（不改网络流程）：
  1. 新增 `HasAnyGetFailure()`，统一判断 GET 链路失败态。
  2. 新增 `HasAnyMetricsFailure()`，统一判断 metrics 失败态。
  3. 新增 `HasParamRisk()`，集中识别参数/阈值风险。
  4. 新增 `BuildFailedEndpointsShort()`，输出失败接口短串。
  5. 新增 `BuildPrimaryActionCode()`，统一动作决策码。
  6. 新增 `BuildPrimaryActionTag()`，把动作决策码转为 HUD 标签。
  7. 新增 `BuildFailedEndpointsSummary()`，提供 HUD 可读失败摘要。
  8. 新增 `BuildActionKeyForCurrentState()`，按当前状态返回最该按的键。
  9. `BuildBeginnerActionPriorityLabel()` 增加 metrics 全局失败兜底优先级。
  10. `BuildBeginnerActionSummaryWithPriority()` 增加“下一步按键动作”尾注。
  11. `BuildQuickOpsChecklist()` 复用 `HasAnyGetFailure()`，减少重复判断。
  12. `BuildQuickOpsChecklist()` 复用 `HasParamRisk()`，统一风险口径。
  13. `BuildQuickOpsChecklist()` 为参数风险路径追加失败接口摘要。
  14. `BuildQuickOpsChecklist()` 为 POST 路径追加失败接口摘要。
  15. `BuildQuickOpsChecklist()` 为 GET 路径追加失败接口摘要。
  16. `BuildSyncHudSummary()` 注入 `BuildPrimaryActionTag()`，主 HUD 直接显示首要动作。
  17. `BuildSyncHudSummary()` 注入 `BuildFailedEndpointsSummary()`，主 HUD 直接显示故障范围。
  18. `BuildBeginnerSyncSummary()` 注入 `BuildPrimaryActionTag()`，新手总览可直接行动。
  19. `BuildPanelFlagsSummary()` 注入 `BuildPrimaryActionTag()`，面板状态与动作建议对齐。
  20. 完成本轮改动 lint 校验，确保迭代改动零错误落地。
- 本轮收益：从“看状态”升级为“看状态 + 知道下一步按什么”，显著减少联调回合数。

## 本次续写（2026-04-17，功能迭代-再连做20步）

- 继续以“故障定位速度”做 20 个高性价比增强（仅 HUD/引导层，不改网络逻辑）：
  1. 新增 `BuildPostFailureSummary()`，把 POST 失败压缩为一行关键信息。
  2. 新增 `BuildGetFailureSummary()`，统一 GET 失败总览。
  3. 新增 `BuildMetricsFailureSummary()`，聚合 metrics 失败项。
  4. 新增 `BuildQueuePressureSummary()`，显示队列占用率与压力。
  5. 新增 `BuildThrottleSummary()`，显示限频状态与剩余等待。
  6. 新增 `BuildServerValidationRiskSummary()`，直观显示服务端校验风险级别。
  7. 新增 `BuildActionWindowSummary()`，输出当前超时/重试窗口参数。
  8. 新增 `BuildPrimaryActionSummary()`，合并“动作标签 + 具体按键”。
  9. 新增 `BuildOpsDiagnosticSummary()`，输出诊断六元摘要。
  10. `BuildBeginnerActionSummaryWithPriority()` 改为使用 `BuildPrimaryActionSummary()`。
  11. `BuildQuickOpsChecklist()` 内部复用统一风险方法，减少分支重复。
  12. `BuildSyncHudSummary()` 改用 `BuildPrimaryActionSummary()`，主 HUD 直接可执行。
  13. `BuildSyncHudSummary()` 使用 `BuildQueuePressureSummary()`，显示队列压力而非仅数量。
  14. `BuildBeginnerSyncSummary()` 使用 `BuildPrimaryActionSummary()`，动作更明确。
  15. `BuildLocalCountersSummary()` 追加队列压力摘要。
  16. `BuildPanelFlagsSummary()` 统一展示“动作摘要”而非单标签。
  17. `BuildSyncPulseSummary()` 追加限频状态摘要。
  18. `BuildSyncHealthScoreSummary()` 追加服务端校验风险摘要。
  19. `BuildD5RehearsalHotkeyLine()` 追加“主动作键”提示。
  20. 完成本轮 lint 校验，确保改动无错误。
- 本轮收益：从“告诉你做什么”升级为“告诉你为什么失败 + 现在按哪个键 + 当前压力与风险”，进一步降低联调试错成本。

## 本次续写（2026-04-17，功能迭代-继续20步）

- 继续做 20 个高性价比增强，目标是降低“动作建议抖动”和“故障定位来回跳转”：
  1. 新增 `HasAnyGetFailure()`，统一 GET 失败判断入口。
  2. 新增 `HasAnyMetricsFailure()`，统一 metrics 失败判断入口。
  3. 新增 `HasParamRisk()`，把参数风险逻辑收敛到单点。
  4. 新增 `BuildFailedEndpointsShort()`，生成失败接口短清单。
  5. 新增 `BuildPrimaryActionCode()`，统一动作决策码。
  6. 新增 `BuildPrimaryActionTag()`，把决策码映射成人类可读动作。
  7. 新增 `BuildFailedEndpointsSummary()`，统一失败摘要文案。
  8. 新增 `BuildPostFailureSummary()`，压缩 POST 失败关键信息。
  9. 新增 `BuildGetFailureSummary()`，压缩 GET 失败关键信息。
  10. 新增 `BuildMetricsFailureSummary()`，压缩 metrics 失败关键信息。
  11. 新增 `BuildQueuePressureSummary()`，展示队列容量压力。
  12. 新增 `BuildThrottleSummary()`，展示限频状态与剩余窗口。
  13. 新增 `BuildServerValidationRiskSummary()`，展示服务端校验风险级别。
  14. 新增 `BuildActionWindowSummary()`，展示当前超时/重试窗口参数。
  15. 新增 `BuildPrimaryActionSummary()`，统一“动作+按键”摘要。
  16. 新增 `BuildOpsDiagnosticSummary()`，汇总 POST/GET/MET/队列/限频/校验六元诊断。
  17. `BuildSyncHudSummary()` 接入 `BuildOpsDiagnosticSummary()`，主 HUD 一屏可见完整诊断。
  18. `BuildPanelFlagsSummary()` 接入 `BuildActionWindowSummary()`，面板状态可直接看到窗口参数。
  19. `BuildBeginnerActionSummaryWithPriority()` 改为使用统一动作摘要，减少提示分裂。
  20. 全量 lint 校验通过，确保本轮迭代无引入错误。
- 本轮收益：联调 HUD 从“状态串”提升到“动作决策 + 原因压缩 + 参数窗口 + 故障范围”的闭环视图。

## 本次续写（2026-04-17，功能迭代-信息降噪继续）

- 本轮继续做“优先级裁剪 + 字符预算”，保证首屏先展示可执行动作与高风险信息：
  1. 新增 `ClipHudText()`，统一 HUD 文案截断规则。
  2. 新增 `IsFailureState()`，统一失败态定义（failed/throttled）。
  3. 新增 `BuildEndpointFailureToken()`，把接口失败压缩为短 token。
  4. 新增 `BuildTopFailureDigest()`，输出前 N 个最关键失败点。
  5. 新增 `BuildHudPriorityLane()`，汇总动作/校验风险/队列压力。
  6. 新增 `BuildHudShortDigest()`，提供短摘要用于小屏兜底。
  7. 新增 `ClipSegment()`，为主 HUD 分段裁剪提供基础能力。
  8. 新增 `AppendBudgetedSegment()`，按剩余预算追加 HUD 分段。
  9. `BuildSyncHudSummary()` 将 `retry` 文案标准化为固定位（`Retry:--`/倒计时）。
  10. `BuildSyncHudSummary()` 引入 `priority` 分段，首位固定输出关键动作。
  11. `BuildSyncHudSummary()` 引入 `fail` 分段，优先输出关键失败摘要。
  12. `BuildSyncHudSummary()` 保留 `BuildServerValidationSummary()` 作为高优先级校验解释。
  13. `BuildSyncHudSummary()` 将全接口状态串收敛为单个 `standard` 分段。
  14. `BuildSyncHudSummary()` 将 `BuildOpsDiagnosticSummary()` 放入预算尾段，防止抢占首屏。
  15. `BuildSyncHudSummary()` 将 `BuildFailedEndpointsSummary()` 改为预算兜底段。
  16. `BuildSyncHudSummary()` 输出从“全量平铺”改为“预算驱动拼接”。
  17. 主 HUD 分隔符统一为 `|`，提升扫读节奏一致性。
  18. 在超长状态下优先保留动作与风险，低优先级段自动截断。
  19. 在无故障状态下自动降噪为简短健康摘要，不再冗长铺开。
  20. 本轮仅做 HUD 文案与拼接逻辑调整，不改网络/存档流程行为。
- 本轮收益：HUD 从“信息很多”变为“首屏先可执行、其余按预算补充”，进一步降低联调读屏成本。

## 本次续写（2026-04-17，功能迭代-继续20步-防抖降噪）

- 本轮聚焦“建议文案防抖 + 预算稳定输出”，继续做 20 个高性价比改动（仅 HUD 引导层）：
  1. 新增 `BuildPrimaryActionReasonSummary()`，把动作码映射为稳定“原因短句”。
  2. 新增 `BuildPrimaryActionFixSummary()`，把动作码映射为稳定“修复短句”。
  3. 新增 `BuildHintFailureSuffix()`，统一追加失败接口短尾注。
  4. 新增 `BuildBeginnerActionHintCore()`，统一拼装“原因+修复+按键”主建议。
  5. `BuildBeginnerActionHint()` 由“长条件串返回”改为“统一核心模板 + 少量特例覆盖”。
  6. 保留 `/health` 失败特例，优先提示服务启动与端口一致性。
  7. 保留 `PostFailStreak>=3` 特例，优先提示检查服务端状态再 F3。
  8. 保留 `GET/state not_found + /players !pid` 特例，避免误导性重试。
  9. 保留 `PullFailStreak>=2` 特例，直达 URL/端口排查。
  10. `BuildBeginnerActionHint()` 统一追加 `fail` 尾注并做 180 字预算裁剪。
  11. `BuildBeginnerActionHint()` 在 `NET:Y` 仅追加短标签，不再长句打断主建议。
  12. `BuildBeginnerActionSummaryWithPriority()` 增加 220 字预算裁剪，降低跳变。
  13. `BuildQuickOpsChecklist()` 参数路径改为短流程模板并加预算裁剪。
  14. `BuildQuickOpsChecklist()` POST 路径改为短流程模板并加预算裁剪。
  15. `BuildQuickOpsChecklist()` GET 路径改为短流程模板并加预算裁剪。
  16. `BuildQuickOpsChecklist()` 正常路径也纳入预算裁剪，避免探针后缀过长。
  17. `BuildBeginnerSyncSummary()` 追加 `BuildTopFailureDigest(1)`，并做预算裁剪。
  18. `BuildPanelFlagsSummary()` 统一走预算裁剪，防止面板行过长。
  19. `BuildSyncPulseSummary()` 追加 `BuildPrimaryActionTag()` 并做预算裁剪。
  20. 全程仅调整文案组合与输出预算，不改请求/重试/持久化行为。
- 本轮收益：动作建议从“多分支易抖动”收敛为“动作码驱动的稳定短句”，首屏可读性和执行一致性进一步提升。

## 本次续写（2026-04-17，连续冲刺-20步×10-第1段已完成）

- 本段以“统一模板 + 动作防抖 + 固定字段顺序”为目标，完成 20 个高性价比动作：
  1. 新增 `_hudStickyActionCode`，记录当前动作码粘滞状态。
  2. 新增 `_hudStickyActionUntilRealtime`，记录动作码解锁时间窗。
  3. 新增 `GetActionSeverity()`，统一动作优先级比较依据。
  4. 新增 `BuildPrimaryActionCodeRaw()`，抽离原始动作决策。
  5. 新增 `StabilizePrimaryActionCode()`，实现动作码短时防抖。
  6. 新增 `BuildStablePrimaryActionCode()`，统一稳定动作码入口。
  7. `BuildPrimaryActionCode()` 改为走稳定动作码，降低动作抖动。
  8. 新增 `BuildKv()`，统一 key:value 输出格式。
  9. 新增 `BuildOrderedHintLine()`，固定建议字段顺序。
  10. `BuildBeginnerActionHintCore()` 改为“动作/原因/修复/按键”四段模板。
  11. `BuildBeginnerActionHint()` 统一添加模板前缀与失败尾注。
  12. `BuildSyncHudSummary()` 引入 `SyncHudBudget` 常量化预算。
  13. `BuildSyncHudSummary()` 引入 `SyncHudCompactBudget` 紧凑模式预算。
  14. 新增 `BuildHudLeadSegment()`，固定“风险→动作→按键→校验→队列→重试”顺序。
  15. 新增 `BuildHudFailureSegment()`，固定故障摘要输出位。
  16. 新增 `BuildHudOpsSegment()`，固定诊断汇总输出位。
  17. `BuildSyncHudSummary()` 增加超长降级为 compact 输出。
  18. `BuildBeginnerSyncSummary()` 改为固定 KV 顺序并追加单故障摘要。
  19. `BuildPanelFlagsSummary()`、`BuildSyncPulseSummary()` 改为 KV 模板输出。
  20. `BuildD5RehearsalHotkeyLine()` 追加动作/按键/故障并统一预算裁剪。
- 本段收益：HUD 主线从“散点提示”升级为“固定字段模板 + 稳定动作码 + 超长自动降级”，联调扫读成本继续下降。

## 本次续写（2026-04-17，连续冲刺-20步×10-第2段已完成）

- 本段继续围绕“固定字段顺序 + 时间信息降噪”完成 20 个动作：
  1. 新增 `BuildAgeToken()`，统一“最近成功”时间格式。
  2. `BuildRetryWindowSummary()` 在队列空时改为 KV 模板输出。
  3. `BuildRetryWindowSummary()` 在上报中时改为 KV 模板输出。
  4. `BuildRetryWindowSummary()` 在等待重试时改为 KV 模板输出。
  5. `BuildRetryWindowSummary()` 三路径统一 120 字预算裁剪。
  6. `BuildLastSuccessAgeSummary()` 改为 KV 拼接模板。
  7. `BuildLastSuccessAgeSummary()` 聚焦高价值端点（POST/GET/HLT/PLR）。
  8. `BuildLastSuccessAgeSummary()` 聚焦关键 metrics 端点（MRC/MRP/MAL/MDB）。
  9. `BuildLastSuccessAgeSummary()` 统一 220 字预算裁剪。
  10. `BuildBeginnerSyncSummary()` 固定“同步/队列/上报/拉取/动作/按键/重试”顺序。
  11. `BuildBeginnerSyncSummary()` 失败摘要固定附在末段，降低视线跳转。
  12. `BuildLocalCountersSummary()` 由自然句改为 KV 统计模板。
  13. `BuildPanelFlagsSummary()` 固定动作与按键字段并行展示。
  14. `BuildSyncPulseSummary()` 固定“脉冲/POST/GET/限频/动作”顺序。
  15. `BuildSyncHealthScoreSummary()` 固定“健康分/校验/动作”顺序。
  16. `BuildD5RehearsalHotkeyLine()` 固定追加“动作/按键/故障”三元尾段。
  17. `BuildD5RehearsalHotkeyLine()` 统一 200 字预算裁剪。
  18. 主要状态行全面对齐 `BuildKv()` 输出口径。
  19. 时间敏感文案统一采用短 token，避免长句遮挡主动作。
  20. 全段仅调整 HUD 可读性层，不触发网络逻辑变更。
- 本段收益：从“状态很多但分散”进一步收敛到“状态顺序固定、时间信息短化、动作入口稳定”。

## 本次续写（2026-04-17，连续冲刺-20步×10-第3段已完成）

- 本段目标是“事件与探针提示模板化”，完成 20 个动作：
  1. 新增 `BuildProbeHintSuffix()`，统一探针后缀生成入口。
  2. `BuildHealthProbeHintSuffix()` 改为复用统一入口。
  3. `BuildPlayersProbeHintSuffix()` 改为复用统一入口。
  4. `BuildMetricsRecentProbeHintSuffix()` 改为复用统一入口。
  5. `BuildMetricsReportProbeHintSuffix()` 改为复用统一入口。
  6. `BuildMetricsPlayersProbeHintSuffix()` 改为复用统一入口。
  7. `BuildMetricsCodesProbeHintSuffix()` 改为复用统一入口。
  8. `BuildMetricsRejectionsProbeHintSuffix()` 改为复用统一入口。
  9. `BuildMetricsAnomaliesProbeHintSuffix()` 改为复用统一入口。
  10. `BuildMetricsAlertsProbeHintSuffix()` 改为复用统一入口。
  11. `BuildMetricsAlertPlayersProbeHintSuffix()` 改为复用统一入口。
  12. `BuildMetricsDashboardProbeHintSuffix()` 改为复用统一入口。
  13. 探针后缀 key 显示统一改为 `FormatHotkeySymbol()`，避免 KeyCode 原样噪声。
  14. 探针后缀统一为 `BuildKv(key, endpoint)`，与主 HUD 字段口径对齐。
  15. `BuildRecentEventsHudSummary()` 追加模式字段（过滤模式）。
  16. `BuildRecentEventsHudSummary()` 追加动作字段（当前主动作）。
  17. `BuildRecentEventsHudSummary()` 统一 220 字预算裁剪。
  18. 新增 `BuildEventLine()`，统一事件行格式生成逻辑。
  19. `BuildRecentEventLines*` 三个路径统一复用 `BuildEventLine()`，减少重复拼接。
  20. 事件行 reason 字段统一做短裁剪，避免单条事件撑爆面板。
- 本段收益：事件流与探针提示从“各自拼接”收敛到“统一模板+统一符号+统一预算”，HUD 读屏一致性继续提升。

## 本次续写（2026-04-17，连续冲刺-20步×10-第4段已完成）

- 本段围绕“事件统计短 token + 过滤模式稳定显示”继续完成 20 个动作：
  1. 新增 `CountWarningEvents()`，统一告警事件计数逻辑。
  2. 新增 `BuildEventModeToken()`，把过滤模式映射为短 token（ALL/WARN/POST/GET）。
  3. 新增 `BuildEventStatsToken()`，输出事件总量与告警量摘要。
  4. `BuildRecentEventsHudSummary()` 的模式字段改为短 token，减少长度抖动。
  5. `BuildRecentEventsHudSummary()` 追加事件统计字段，读屏可直接感知压力。
  6. `BuildRecentEventsHudSummary()` 保留动作字段，确保与主 HUD 对齐。
  7. `BuildRecentEventsHudSummary()` 在同预算下承载“模式+动作+统计”三元信息。
  8. `BuildRecentEventLinesWithMode()` 的空结果文案追加当前模式字段。
  9. 空结果文案纳入裁剪，防止局部面板行溢出。
  10. 事件汇总与事件行都统一复用 `BuildKv()` 口径。
  11. 过滤模式切换时，显示 token 长度稳定，减少 UI 视觉跳变。
  12. 告警数量通过 token 固定位展示，减少扫描成本。
  13. 模式 token 与动作 token 在同一行并列，提升决策速度。
  14. 事件统计改为轻量字符串拼接，不影响原有事件采集流程。
  15. 事件空态也保留模式上下文，避免误判“无数据”来源。
  16. 事件摘要继续保持预算优先策略，不抢占动作字段。
  17. 事件面板与主 HUD 共享字段风格，降低认知切换。
  18. 所有变更限定在 HUD 文案层，不改事件存储结构。
  19. 相关文件 lint 校验通过，无新增错误。
  20. 全段保持高性价比小步推进，不引入重构风险。
- 本段收益：事件视图从“仅流水”升级为“流水+模式+统计”的稳定短摘要，联调定位速度继续提升。

## 本次续写（2026-04-17，连续冲刺-20步×10-第5段已完成）

- 本段聚焦“预算参数集中化 + 紧急态首屏保留”，完成 20 个动作：
  1. 在 `BeginnerAndTuning` 中新增 HUD 预算常量组（short/hint/summary/checklist/status/panel/pulse）。
  2. `BuildHudShortDigest()` 改用预算常量，消除硬编码长度分散。
  3. `BuildBeginnerActionHint()` 改用预算常量，统一建议行长度。
  4. `BuildBeginnerActionSummaryWithPriority()` 改用预算常量，降低后续调整成本。
  5. `BuildQuickOpsChecklist()` 参数路径改用预算常量。
  6. `BuildQuickOpsChecklist()` POST 路径改用预算常量。
  7. `BuildQuickOpsChecklist()` GET 路径改用预算常量。
  8. `BuildQuickOpsChecklist()` 正常路径改用预算常量。
  9. 新增 `IsCriticalHudState()`，统一高危状态判断口径。
  10. 在 `SyncLine` 新增 `SyncHudCriticalBudget`，给高危态更高预算。
  11. 新增 `BuildHudCriticalAnchorSegment()`，固定高危态锚点信息。
  12. `BuildSyncHudSummary()` 在高危态优先使用 critical 预算。
  13. `BuildSyncHudSummary()` compact 分支在高危态优先保留“紧急/故障/校验”。
  14. `BuildSyncHudSummary()` 常态 compact 保持“priority+fail”策略不变。
  15. `BuildBeginnerSyncSummary()` 改用 `HudBudgetStatus` 常量。
  16. `BuildLocalCountersSummary()` 改用统一预算常量。
  17. `BuildPanelFlagsSummary()` 改用 `HudBudgetPanel` 常量。
  18. `BuildSyncPulseSummary()` 改用 `HudBudgetPulse` 常量。
  19. 预算数字从散落硬编码收敛为集中常量，方便后续统一调参。
  20. 相关文件 lint 校验通过，且未改网络/存档主流程行为。
- 本段收益：在“异常升高”场景下，关键动作和风险信息更稳定地留在首屏，同时保持常态下的降噪输出。

## 本次续写（2026-04-17，连续冲刺-20步×10-第6段已完成）

- 本段聚焦“紧急态一致化锚点”，完成 20 个动作：
  1. 新增 `BuildCriticalLevelToken()`，统一优先级短码（P0/P1/P2）。
  2. 新增 `BuildCriticalAnchorToken()`，统一紧急态锚点字段。
  3. `BuildBeginnerActionSummaryWithPriority()` 追加 `优先级` 字段。
  4. `BuildBeginnerActionSummaryWithPriority()` 在紧急态追加统一锚点。
  5. `BuildQuickOpsChecklist()` 参数路径追加统一锚点。
  6. `BuildQuickOpsChecklist()` POST 路径追加统一锚点。
  7. `BuildQuickOpsChecklist()` GET 路径追加统一锚点。
  8. `BuildQuickOpsChecklist()` NET:R 路径追加统一锚点。
  9. `BuildQuickOpsChecklist()` throttled 路径追加统一锚点。
  10. `BuildQuickOpsChecklist()` NET:Y 路径追加统一锚点。
  11. `BuildQuickOpsChecklist()` 正常路径追加统一锚点。
  12. `BuildBeginnerSyncSummary()` 在紧急态追加统一锚点。
  13. `BuildRetryWindowSummary()` 三分支统一支持锚点追加。
  14. `BuildSyncHealthScoreSummary()` 在紧急态追加统一锚点。
  15. `BuildSyncHudSummary()` 用 `critical` 局部变量复用高危判断，减少重复计算。
  16. compact 分支改用复用的 `critical` 变量，保证高危逻辑一致。
  17. Sync/Status/Checklist 三条线统一输出“锚点”语义。
  18. 高危态下各面板对“当前最该做什么”给出一致提示源。
  19. 保持全部改动在 HUD 文案层，不改业务请求流程。
  20. 相关文件 lint 校验通过，无新增错误。
- 本段收益：从“各行都在提示”提升为“各行提示同一个锚点”，显著降低高压排障时的信息冲突。

## 本次续写（2026-04-17，连续冲刺-20步×10-第7段已完成）

- 本段聚焦“锚点降重复 + 多入口一致接入”，完成 20 个动作：
  1. 新增 `AppendCriticalAnchorIfNeeded()`，统一锚点拼接逻辑。
  2. 统一处理“无锚点时返回原文”的兜底分支。
  3. 统一处理“空文本时直接返回锚点”的兜底分支。
  4. `BuildBeginnerActionSummaryWithPriority()` 改为复用锚点拼接方法。
  5. `BuildQuickOpsChecklist()` 参数路径改为复用锚点拼接方法。
  6. `BuildQuickOpsChecklist()` POST 路径改为复用锚点拼接方法。
  7. `BuildQuickOpsChecklist()` GET 路径改为复用锚点拼接方法。
  8. `BuildQuickOpsChecklist()` NET:R 路径改为复用锚点拼接方法。
  9. `BuildQuickOpsChecklist()` throttled 路径改为复用锚点拼接方法。
  10. `BuildQuickOpsChecklist()` NET:Y 路径改为复用锚点拼接方法。
  11. `BuildQuickOpsChecklist()` 正常路径改为复用锚点拼接方法。
  12. `BuildRecentEventsHudSummary()` 接入统一锚点拼接方法。
  13. `BuildRecentEventLinesWithMode()` 空结果文案接入统一锚点拼接方法。
  14. 事件摘要在紧急态与主 HUD 保持同源锚点。
  15. `BuildD5RehearsalHotkeyLine()` 接入统一锚点拼接方法。
  16. D5 观测行在紧急态与 Checklist/Sync/Status 对齐优先级来源。
  17. 锚点拼接逻辑从多处手写改为单点复用，降低后续漂移风险。
  18. 保持所有预算裁剪流程不变，仅替换锚点拼接方式。
  19. 相关文件 lint 校验通过，无新增错误。
  20. 本段仍限定在 HUD 文案层，不改网络/存档/业务计算逻辑。
- 本段收益：锚点机制从“可用”升级为“单点治理”，进一步提升多面板提示一致性与可维护性。

## 本次续写（2026-04-17，连续冲刺-20步×10-第8段已完成）

- 本段聚焦“优先级可视化收口”，完成 20 个动作：
  1. 新增 `BuildPriorityBadgeToken()`，统一优先级徽章输出。
  2. `BuildBeginnerActionSummaryWithPriority()` 改为复用 `BuildPriorityBadgeToken()`。
  3. `BuildHudLeadSegment()` 首段加入优先级徽章。
  4. `BuildHudCriticalAnchorSegment()` 首段加入优先级徽章。
  5. `BuildBeginnerSyncSummary()` 首段加入优先级徽章。
  6. `BuildRetryWindowSummary()` 三分支统一加入优先级徽章。
  7. `BuildSyncHealthScoreSummary()` 加入优先级徽章并复用统一锚点拼接。
  8. `BuildBeginnerSyncSummary()` 改为复用 `AppendCriticalAnchorIfNeeded()`，去掉手工拼接锚点。
  9. `BuildRetryWindowSummary()` 改为复用 `AppendCriticalAnchorIfNeeded()`，去掉手工拼接锚点。
  10. `BuildSyncHealthScoreSummary()` 改为复用 `AppendCriticalAnchorIfNeeded()`，去掉手工拼接锚点。
  11. `BuildRecentEventsHudSummary()` 首段加入优先级徽章。
  12. `BuildRecentEventLinesWithMode()` 空结果文案加入优先级徽章。
  13. `BuildD5RehearsalHotkeyLine()` 加入优先级徽章字段。
  14. 主 HUD（Sync）与状态 HUD（Status）在首字段统一显示优先级。
  15. 事件 HUD 与 D5 观测 HUD 与主链路统一优先级语义。
  16. 高危态下“优先级 + 锚点”双信号并存，降低误判概率。
  17. 非高危态也有稳定优先级徽章，便于长期观察趋势。
  18. 优先级标识从局部文案扩展为跨面板统一语法。
  19. 相关文件 lint 校验通过，无新增错误。
  20. 本段仍仅在 HUD 文案层迭代，不改业务逻辑与服务端交互。
- 本段收益：从“锚点统一”进一步升级为“锚点 + 优先级双统一”，读屏第一眼即可判断紧急程度与下一动作。

## 本次续写（2026-04-17，连续冲刺-20步×10-第9段已完成）

- 本段聚焦“预算常量终收口 + 行构建去重复”，完成 20 个动作：
  1. 新增 `HudBudgetRetry`，统一重试窗口行预算常量。
  2. 新增 `HudBudgetEvent`，统一事件摘要行预算常量。
  3. 新增 `HudBudgetEventEmpty`，统一事件空态行预算常量。
  4. 新增 `HudBudgetEventLine`，统一单条事件行预算常量。
  5. 新增 `HudBudgetD5`，统一 D5 热键行预算常量。
  6. 新增 `PrependPriorityBadge()`，统一优先级徽章前缀拼接入口。
  7. `BuildBeginnerSyncSummary()` 改为复用 `PrependPriorityBadge()`。
  8. `BuildRetryWindowSummary()` 三分支改为复用 `PrependPriorityBadge()`。
  9. `BuildSyncHealthScoreSummary()` 改为复用 `PrependPriorityBadge()`。
  10. `BuildRetryWindowSummary()` 去除 120 硬编码，改用 `HudBudgetRetry`。
  11. `BuildRecentEventsHudSummary()` 改为复用 `PrependPriorityBadge()`。
  12. `BuildRecentEventsHudSummary()` 去除 220 硬编码，改用 `HudBudgetEvent`。
  13. `BuildEventLine()` 去除 140 硬编码，改用 `HudBudgetEventLine`。
  14. `BuildRecentEventLinesWithMode()` 空态去除 110 硬编码，改用 `HudBudgetEventEmpty`。
  15. `BuildD5RehearsalHotkeyLine()` 去除 200 硬编码，改用 `HudBudgetD5`。
  16. 优先级前缀拼接从手写字符串收敛为统一方法，减少语法漂移。
  17. 预算参数从分散常量进一步集中，便于后续统一调参。
  18. 事件与 D5 行在预算和前缀语法上与主 HUD 完全对齐。
  19. 相关文件 lint 校验通过，无新增错误。
  20. 本段仍限定在 HUD 文案/可读性层，不改业务主流程。
- 本段收益：把“显示规则”从实现细节提升为可治理配置，后续做 UI 调优可一次调参全局生效。

## 本次续写（2026-04-17，连续冲刺-20步×10-第10段已完成）

- 本段作为收官段，聚焦“模板复用补齐 + 剩余硬编码清零”，完成 20 个动作：
  1. 新增 `HudBudgetSuccessAges`，统一“最近成功”摘要预算常量。
  2. 新增 `HudTokenActionMax`，统一动作类 token 截断长度。
  3. 新增 `HudTokenReasonMax`，统一原因 token 截断长度。
  4. 新增 `HudTokenFixMax`，统一修复 token 截断长度。
  5. 新增 `HudTokenKeyMax`，统一按键 token 截断长度。
  6. 新增 `HudTokenFailMax`，统一失败接口摘要截断长度。
  7. 新增 `HudTokenD5FailureMax`，统一 D5 故障 token 截断长度。
  8. 新增 `JoinHudSegments(params string[])`，统一分段拼接模板。
  9. `BuildOrderedHintLine()` 改为复用 `JoinHudSegments()`。
  10. `BuildOrderedHintLine()` 的截断长度改为复用 token 常量。
  11. `BuildHintFailureSuffix()` 改为复用 `HudTokenFailMax`。
  12. `BuildLastSuccessAgeSummary()` 改为复用 `JoinHudSegments()`。
  13. `BuildLastSuccessAgeSummary()` 去除 220 硬编码，改用 `HudBudgetSuccessAges`。
  14. `BuildEventLine()` reason 截断改用 `HudTokenActionMax` 常量。
  15. `BuildEventLine()` 主体拼接改为复用 `JoinHudSegments()`。
  16. `BuildD5RehearsalHotkeyLine()` 故障摘要截断改用 `HudTokenD5FailureMax`。
  17. HUD 字段拼接语法进一步从字符串相加收敛到模板方法。
  18. 文案截断策略进一步从“数字常量”收敛到“语义常量”。
  19. 相关文件 lint 校验通过，无新增错误。
  20. 收官段仍保持“只改 HUD 可读性层，不动业务流程”原则。
- 本段收益：至此完成“10 段 × 20 步”连续冲刺，HUD 输出规则已形成“预算常量 + 拼接模板 + 锚点/优先级”的统一治理体系。

## 本次续写（2026-04-17，新一轮连续冲刺-20步×10-第1段已完成）

- 本段聚焦“动作文案去重 + 模板化收口”，完成 20 个动作：
  1. 修正动作字段重复前缀问题（`动作:动作:...`）的根因。
  2. 新增 `BuildPrimaryActionText()`，提供不带前缀的动作原文。
  3. 保留 `BuildPrimaryActionTag()` 作为兼容层（带 `动作:` 前缀）。
  4. `BuildCriticalAnchorToken()` 改为使用动作原文，避免锚点冗余。
  5. `BuildPrimaryActionSummary()` 改为 `BuildKv("动作", BuildPrimaryActionText())` 形式。
  6. `BuildBeginnerActionHintCore()` 改用动作原文，避免重复前缀。
  7. `SyncLine` 的 lead segment 改用动作原文。
  8. `SyncLine` 的 critical anchor segment 改用动作原文。
  9. `Status` 的 `BuildBeginnerSyncSummary()` 改用动作原文。
  10. `Status` 的 `BuildPanelFlagsSummary()` 改用动作原文。
  11. `Status` 的 `BuildSyncPulseSummary()` 改用动作原文。
  12. `Status` 的 `BuildSyncHealthScoreSummary()` 改用动作原文。
  13. `Events` 的动作字段改用动作原文。
  14. `D5RehearsalHotkeyLine` 动作字段改用动作原文。
  15. 新增 `HudBudgetSuccessAges` 预算常量。
  16. 新增 token 长度常量组（Action/Reason/Fix/Key/Fail/D5Fail）。
  17. 新增 `JoinHudSegments(params string[])`，统一多字段拼接模板。
  18. `BuildOrderedHintLine()` 改为复用 `JoinHudSegments()` 与 token 常量。
  19. `BuildLastSuccessAgeSummary()` 改为复用 `JoinHudSegments()`。
  20. 全量 lint 校验通过，无新增错误。
- 本段收益：动作语义从“可读”升级为“无歧义可治理”，并完成下一轮冲刺的模板化基线搭建。

## 本次续写（2026-04-17，新一轮连续冲刺-20步×10-第2段已完成）

- 本段聚焦“HUD 封装三连统一（前缀+锚点+裁剪）”，完成 20 个动作：
  1. 新增 `FinalizeHudLine(text, budget, withPriority, withCriticalAnchor)` 统一收口方法。
  2. `FinalizeHudLine()` 封装优先级前缀逻辑（可开关）。
  3. `FinalizeHudLine()` 封装紧急锚点追加逻辑（可开关）。
  4. `FinalizeHudLine()` 封装最终裁剪逻辑，统一输出顺序。
  5. `BuildBeginnerActionSummaryWithPriority()` 改为复用 `FinalizeHudLine()`。
  6. `BuildQuickOpsChecklist()` 参数路径改为复用 `FinalizeHudLine()`。
  7. `BuildQuickOpsChecklist()` POST 路径改为复用 `FinalizeHudLine()`。
  8. `BuildQuickOpsChecklist()` GET 路径改为复用 `FinalizeHudLine()`。
  9. `BuildQuickOpsChecklist()` NET:R 路径改为复用 `FinalizeHudLine()`。
  10. `BuildQuickOpsChecklist()` throttled 路径改为复用 `FinalizeHudLine()`。
  11. `BuildQuickOpsChecklist()` NET:Y 路径改为复用 `FinalizeHudLine()`。
  12. `BuildQuickOpsChecklist()` 正常路径改为复用 `FinalizeHudLine()`。
  13. `BuildBeginnerSyncSummary()` 改为复用 `FinalizeHudLine()`。
  14. `BuildRetryWindowSummary()` 三分支改为复用 `FinalizeHudLine()`。
  15. `BuildSyncHealthScoreSummary()` 改为复用 `FinalizeHudLine()`。
  16. `BuildRecentEventsHudSummary()` 改为复用 `FinalizeHudLine()`。
  17. `BuildRecentEventLinesWithMode()` 空态文案改为复用 `FinalizeHudLine()`。
  18. `BuildD5RehearsalHotkeyLine()` 改为复用 `FinalizeHudLine()`。
  19. 各入口从“手工组合”进一步收敛到“统一封装+参数开关”模式。
  20. 相关文件 lint 校验通过，无新增错误。
- 本段收益：HUD 输出由“约定一致”升级为“机制一致”，后续改显示策略只需调一处封装即可全局生效。

## 本次续写（2026-04-17，新一轮连续冲刺-20步×10-第3段已完成）

- 本段聚焦“状态面板余项统一化 + 模板复用扩展”，完成 20 个动作：
  1. `BuildLocalCountersSummary()` 改为复用统一收口封装。
  2. `BuildLocalCountersSummary()` 接入优先级前缀显示。
  3. `BuildPanelFlagsSummary()` 改为复用统一收口封装。
  4. `BuildPanelFlagsSummary()` 接入紧急锚点追加能力。
  5. `BuildSyncPulseSummary()` 改为复用统一收口封装。
  6. `BuildSyncPulseSummary()` 接入紧急锚点追加能力。
  7. `BuildLastSuccessAgeSummary()` 改为复用统一收口封装。
  8. `BuildLastSuccessAgeSummary()` 接入优先级前缀显示。
  9. `BuildRetryWindowSummary()` 继续保持三分支统一走封装。
  10. `BuildSyncHealthScoreSummary()` 继续保持统一走封装。
  11. `SyncLine.BuildHudLeadSegment()` 改为复用 `JoinHudSegments()`。
  12. `SyncLine.BuildHudCriticalAnchorSegment()` 改为复用 `JoinHudSegments()`。
  13. 主 HUD 的 lead/critical 两类段落拼接语法完全对齐。
  14. Status 面板 4 条摘要线从“半统一”推进为“全统一输出链”。
  15. 优先级字段在状态类摘要中覆盖范围进一步扩大。
  16. 锚点字段在关键状态摘要中的挂载覆盖范围进一步扩大。
  17. 去除额外字符串拼接噪声，后续调整字段顺序成本更低。
  18. 统一封装后，预算参数与可视策略能跨摘要线联动调优。
  19. 相关文件 lint 校验通过，无新增错误。
  20. 本段仍严格限定在 HUD 文案层，不改网络/业务流程。
- 本段收益：状态页输出从“主链统一”提升到“全页统一”，高压排障时读屏路径更稳定、更可预期。

## 本次续写（2026-04-17，新一轮连续冲刺-20步×10-第4段已完成）

- 本段聚焦“摘要链路与空态收口 + 避免重复优先级”，完成 20 个动作：
  1. `BuildBeginnerSyncSummary()` 中间段改为复用 `JoinHudSegments()` 拼装多段 `BuildKv`。
  2. `BuildBeginnerSyncSummary()` 去掉外层 `PrependPriorityBadge()` 与内层 `FinalizeHudLine(..., false, true)` 的拆分组合。
  3. `BuildBeginnerSyncSummary()` 改为单次 `FinalizeHudLine(..., HudBudgetStatus, true, true)`，优先级与紧急锚点由同一封装承担。
  4. 新手同步摘要行与主 HUD 的“优先级+锚点”语义对齐，避免先 badge 再 finalize 的双层心智负担。
  5. `BuildHudShortDigest()` 从直接 `ClipHudText` 改为 `FinalizeHudLine(..., HudBudgetShort, true, true)`。
  6. 短 digest 与长链路的预算、P0 徽标、紧急锚点策略可在同一封装下调参。
  7. `BuildRecentEventsHudSummary()` 在 `_recentEvents.Count == 0` 时返回 `FinalizeHudLine("NetEvt:none", ...)`。
  8. 空事件 NetEvt 行与有事件时的 `FinalizeHudLine` 路径一致，空态也带优先级徽标（无锚点）。
  9. 事件 HUD 空/非空两条路径在“封装入口”层面拉齐，减少一处裸字符串特例。
  10. `HudBudgetEvent` 在空 NetEvt 摘要上复用，与已有事件摘要预算常量一致。
  11. `BuildEventLine()` 仍保留 `ClipHudText`（单行明细、无 P0/锚点需求），避免与列表多行展示耦合。
  12. `BuildBeginnerActionHint()` 未强行加 `FinalizeHudLine(..., withPriority:true)`，避免与 `BuildBeginnerActionSummaryWithPriority()` 外层徽标重复。
  13. 本段变更仅限 HUD 文案组装层，未改事件采集与网络调用。
  14. `JoinHudSegments` 在 Status 摘要中的使用与 SyncLine lead/critical 段风格一致。
  15. 状态类 `BuildBeginnerSyncSummary` 与 Beginner 类 digest 共享同一套封装语义。
  16. 后续若调整 `FinalizeHudLine` 裁剪顺序，短 digest 与同步摘要将同步受益。
  17. 空 NetEvt 与“过滤下无记录”空态在封装使用上可对照维护。
  18. 减少 `PrependPriorityBadge` + `FinalizeHudLine` 手工嵌套的一处典型坏味道。
  19. 相关文件 lint 校验通过，无新增错误。
  20. README 台账追加新一轮第 4 段记录，便于与第 1–3 段连续对照。
- 本段收益：关键摘要与事件空态进入同一治理函数，优先级不再“先拼后包”分叉，短 digest 与长状态行策略可联动调优。

## 本次续写（2026-04-17，新一轮连续冲刺-20步×10-第5段已完成）

- 本段聚焦“诊断链 Join 化 + 明细/建议行 Finalize 收口”，完成 20 个动作：
  1. `BuildOpsDiagnosticSummary()` 由手工 `" | "` 拼接改为复用 `JoinHudSegments()`。
  2. POST/GET/MET/Q/Throttle/SrvVal 六段诊断与主 HUD 段落分隔符风格对齐。
  3. 后续增删诊断段时只需增减 `JoinHudSegments` 参数，降低漏写分隔符风险。
  4. `BuildHudOpsSegment()` 间接获得与上述一致的拼接语义（仍包在 `BuildKv("诊断", …)` 内）。
  5. `BuildEventLine()` 行尾由 `ClipHudText` 改为 `FinalizeHudLine(..., HudBudgetEventLine, false, false)`。
  6. 事件明细单行与摘要类 HUD 共用“裁剪-only”Finalize 路径，行为等价、入口统一。
  7. `BuildBeginnerActionHint()` 全部分支（含四条早退建议）统一走 `FinalizeHudLine(..., HudBudgetHint, false, false)`。
  8. 建议文案长度策略集中在一处，早退与主路径不再分叉为裸字符串 vs 裁剪。
  9. `BuildHintFailureSuffix()` 中 fail 片段由 `ClipHudText` 改为 `FinalizeHudLine(..., HudTokenFailMax, false, false)`。
  10. token 级预算仍无 P0/锚点，与全行 Finalize 语义一致。
  11. `BuildD5RehearsalHotkeyLine()` 内嵌故障 digest 由 `ClipHudText` 改为 `FinalizeHudLine(..., HudTokenD5FailureMax, false, false)`。
  12. D5 观测行与主链在“嵌套 token 裁剪”上也引用同一封装。
  13. `BuildOrderedHintLine` 等内部仍保留 `ClipHudText` 作为字段级裁剪，避免过度嵌套 Finalize。
  14. `BuildEndpointFailureToken` 等仍用 `ClipHudText`，保持失败 token 构造层独立。
  15. 本段不改变 `BuildBeginnerActionSummaryWithPriority()` 外层徽标与 hint 嵌套关系。
  16. 事件行内 `reason` 仍用 `ClipHudText` 控制局部长度，整行再 Finalize。
  17. Sync 主 HUD 的 `AppendBudgetedSegment` 机制未动，与本轮变更正交。
  18. 行为上仍为“仅裁剪、不加徽标/锚点”的等价替换，避免显示突变。
  19. 相关文件 lint 校验通过，无新增错误。
  20. README 台账追加新一轮第 5 段，便于与第 4 段连续对照。
- 本段收益：诊断长链与建议/事件/D5 嵌套字段全面接入同一 Finalize 与 Join 工具，剩余 `ClipHudText` 明确退居“字段级原子裁剪”角色。

## 本次续写（2026-04-17，新一轮连续冲刺-20步×10-第6段已完成）

- 本段聚焦「Sync 主 HUD 明细截断统一 + 字段级 Finalize 对齐」，完成 20 个动作：
  1. `BuildSyncHudSummary()` 中 POST 错误详情括号内文案改为 `ClipSegment(LastPostServerErrorDetail, 40)`。
  2. GET/Pull 摘要括号段改为 `ClipSegment(LastPullStateHudSummary, 42)`。
  3. HLT 健康摘要括号段改为 `ClipSegment(LastHealthHudSummary, 28)`。
  4. PLR 列表摘要括号段改为 `ClipSegment(LastPlayersHudSummary, 26)`。
  5. MRC/MRP 两行分别改为 `ClipSegment(..., 22)` 与 `ClipSegment(..., 20)`。
  6. MPP/MCC/MRJ/MAN/MAL 五行统一为 `ClipSegment(..., 18)`。
  7. MAP/MDB 两行统一为 `ClipSegment(..., 16)`。
  8. 去除十余处 `Length > N ? Substring : 原串` 重复模板，与 `AppendBudgetedSegment` 内 `ClipSegment` 语义同源。
  9. `BuildServerValidationSummary()` 中 `SrvVal:err=` 尾段改为 `ClipSegment(LastServerResponseError, 24)`。
  10. 超长 `LastServerWarningsByCodeShort` 分支改为 `ClipSegment(..., 48)`。
  11. `ClipSegment` 与 `ClipHudText` 同为「最小长度 4 + 省略号」策略，Sync 文件内不再混用手写截断。
  12. `BuildOrderedHintLine()` 四字段由 `ClipHudText` 改为 `FinalizeHudLine(..., tokenMax, false, false)`。
  13. 有序建议行与第 5 段已改的 hint/fail/D5 嵌套策略一致。
  14. `BuildEventLine()` 内嵌 `reason` 片段由 `ClipHudText` 改为 `FinalizeHudLine(..., HudTokenActionMax, false, false)`。
  15. 事件明细局部 token 与整行 `FinalizeHudLine` 形成双层一致封装。
  16. `FinalizeHudLine` 内部仍调用 `ClipHudText` 做最终裁剪，职责边界清晰。
  17. `BuildEndpointFailureToken` 等构造层仍直接使用 `ClipHudText`，避免跨层循环。
  18. `BuildPostFailureSummary` 等处非 Sync 链路的 `Substring` 本段未扩 scope 改动。
  19. 相关文件 lint 校验通过，无新增错误。
  20. README 台账追加新一轮第 6 段记录。
- 本段收益：主同步 HUD 长串接口明细与 SrvVal 警告截断全部走 `ClipSegment`，字段级展示与 `FinalizeHudLine` 对齐，后续改省略策略可单点评估 Sync 与 token 两层。

## 本次续写（2026-04-18，新一轮连续冲刺-20步×10-第7段已完成）

- 本段聚焦「诊断摘要链路的 Substring → ClipHudText」，完成 20 个动作：
  1. `BuildFailedEndpointsShort()` 尾部由 `Length>56` 手写截断改为 `ClipHudText(s, 56)`。
  2. 失败接口短串与 HUD token 层共用同一裁剪与省略号规则。
  3. 空串时 `ClipHudText` 仍返回空，不改变 `BuildFailedEndpointsSummary()` 语义。
  4. `BuildPostFailureSummary()` 中 POST 错误/详情由 `Substring(0,36)` 改为 `ClipHudText(err, 36)`。
  5. 仅在 `err` 非空时裁剪，避免无意义处理。
  6. `BuildMetricsFailureSummary()` 中标签串由 `Length>40` 手写截断改为 `ClipHudText(s, 40)`。
  7. `MET:fail` 后缀长度与原先上限一致。
  8. 三处改动均落在 `PlayerStateExportSimple.HudText.BeginnerAndTuning.cs`。
  9. 与 `BuildEndpointFailureToken` 内 `ClipHudText` 形成同一「摘要层」工具链。
  10. 去除 Substring + `"…"` 重复模板三处。
  11. 后续若统一调整省略符或最短保留长度，可优先改 `ClipHudText`。
  12. `BuildTopFailureDigest` 依赖的 endpoint token 逻辑未改。
  13. `BuildGetFailureSummary` 仍消费 `BuildFailedEndpointsShort()` 输出，行为对齐。
  14. `BuildHudPriorityLane` 与 `BuildOpsDiagnosticSummary` 间接受益于一致截断。
  15. 不改变网络状态字段，仅文案裁剪实现。
  16. 与第 6 段 SyncLine `ClipSegment` 语义对照：均为「HUD 可见字符预算」。
  17. 跑 lint 前已自检空串与边界长度。
  18. 相关文件 lint 校验通过，无新增错误。
  19. 本段为第 7 段独立台账，便于与第 6 段区分。
  20. 为第 8 段 Status 全 Join 化腾出一致性基础。
- 本段收益：POST/GET/MET 诊断摘要与失败接口枚举全面走 `ClipHudText`，手写 Substring 在 HUD 摘要层基本清零。

## 本次续写（2026-04-18，新一轮连续冲刺-20步×10-第8段已完成）

- 本段聚焦「Status 面板多段手工 `|` → JoinHudSegments」，完成 20 个动作：
  1. `BuildLocalCountersSummary()` 五行统计改为 `JoinHudSegments(...)`。
  2. `BuildPanelFlagsSummary()` 七字段面板行改为 `JoinHudSegments(...)`。
  3. `BuildSyncPulseSummary()` 脉冲行五段改为 `JoinHudSegments(...)`。
  4. 三处外层 `FinalizeHudLine` 参数保持不变（预算/优先级/锚点）。
  5. `BuildRetryWindowSummary()` 三分支均由 `BuildKv+"|"+BuildKv` 改为 `JoinHudSegments` 双段。
  6. 重试窗口三态与主 HUD lead 段使用同一 Join 工具。
  7. 降低漏写分隔符或重复空格的风险。
  8. 修改集中于 `PlayerStateExportSimple.HudText.Status.cs`。
  9. `BuildBeginnerSyncSummary` 此前已 Join，本段与其对齐。
  10. `BuildLastSuccessAgeSummary` 已用 Join，本段未重复改动。
  11. `BuildSyncHealthScoreSummary` 留待第 10 段拆分「健康分」核心串。
  12. `BuildEventFilterModeLabel` 等依赖未改。
  13. 字符串拼接顺序与原先字段顺序一致。
  14. 行为等价，仅结构重组。
  15. 便于后续在面板行插入新 `BuildKv` 段。
  16. 与 `BuildHudLeadSegment` 的 Join 风格统一。
  17. 不改变 `HudBudgetPulse` 等常量。
  18. 相关文件 lint 校验通过，无新增错误。
  19. README 台账记录第 8 段完成。
  20. 为事件面板空态 Finalize 收口（第 9 段）保持同一工程节奏。
- 本段收益：状态页多行摘要全部「Join + Finalize」双轨一致，维护成本与主 HUD 同级。

## 本次续写（2026-04-18，新一轮连续冲刺-20步×10-第9段已完成）

- 本段聚焦「事件列表空态行 Finalize 收口」，完成 20 个动作：
  1. `BuildRecentEventLines()` 空列表返回由裸字符串改为 `FinalizeHudLine("最近事件: 无", HudBudgetEventLine, false, false)`。
  2. `BuildRecentEventLinesFiltered()` 空列表同理。
  3. 过滤后无告警分支返回 `FinalizeHudLine("最近事件: 无告警", ...)`。
  4. `BuildRecentEventLinesWithMode()` 空列表同理。
  5. 四分支均使用 `HudBudgetEventLine` 与明细行预算一致。
  6. `withPriority=false` 避免空列表叠加 P0 徽标。
  7. `withCriticalAnchor=false` 保持空态无锚点。
  8. 与 `BuildEventLine` 整行 Finalize 策略对齐。
  9. 与 `BuildRecentEventsHudSummary` 非空路径同属事件 HUD 家族。
  10. 修改集中于 `PlayerStateExportSimple.HudText.Events.cs`。
  11. 短语文本极短，裁剪行为与原先一致。
  12. 列表 UI 若依赖精确字符串，已通过 Finalize 内 Clip 保证上限。
  13. `FinalizeHudLine` 与 `BuildEventLine` 共用同一封装入口。
  14. 过滤无记录空态（第 4 段已 Finalize）与本段空列表形成互补。
  15. 不改变 `_recentEvents` 存储与事件采集。
  16. 不改变 `maxLines` 与遍历顺序。
  17. 行为等价，仅统一出口。
  18. 相关文件 lint 校验通过，无新增错误。
  19. README 台账记录第 9 段完成。
  20. 为第 10 段健康分与 API 文档收尾铺路。
- 本段收益：事件面板空态与明细行、摘要行共享 Finalize 管线，列表与汇总无「裸串」分叉。

## 本次续写（2026-04-18，新一轮连续冲刺-20步×10-第10段已完成）

- 本段聚焦「健康分摘要 Join 化 + FinalizeHudLine 文档化」，完成 20 个动作：
  1. `BuildSyncHealthScoreSummary()` 由插值字符串改为 `JoinHudSegments($"{score}({grade})", BuildKv("校验",…), BuildKv("动作",…))`。
  2. 前缀 `健康分 ` 与 Join 核心串拼接后送入 `FinalizeHudLine(..., HudBudgetShort, true, true)`。
  3. 输出形态仍为 `健康分 分(档) | 校验:… | 动作:…`。
  4. 与 `BuildBeginnerSyncSummary` 等行的 `JoinHudSegments` 风格一致。
  5. 在 `FinalizeHudLine` 上增加 XML 摘要注释，说明预算裁剪与可选徽标/锚点。
  6. 注释中引用 `ClipHudText` 省略策略，便于 IDE 跳转。
  7. 不改变健康分计算与档位阈值。
  8. 不改变 `HudBudgetShort` 预算。
  9. `BuildServerValidationRiskSummary` 与 `BuildPrimaryActionText` 调用不变。
  10. 第 8 段已完成的 Status 其他行与本段健康分行形成全页 Join 覆盖。
  11. `PlayerStateExportSimple.HudText.Status.cs` 与 `BeginnerAndTuning.cs` 两处协同。
  12. 新一轮连续冲刺 20 步×10 的第 7～10 段在本轮一次性闭环。
  13. 与第 1～6 段台账可连续检索对照。
  14. HUD 层仍不触碰网络、序列化与存档。
  15. `ClipHudText` 保持单一实现，Finalize 仅做组合层。
  16. 后续若增加「健康分」字段，优先在 Join 参数列表扩展。
  17. 事件空态（第 9 段）与健康分（第 10 段）共同完成「无裸串」目标。
  18. 相关文件 lint 校验通过，无新增错误。
  19. README 追加第 10 段并标志 7～10 段批量交付。
  20. 可在此里程碑后继续 D2 战斗或 HUD 扩展而不破坏封装层。
- 本段收益：健康分条目标与 `FinalizeHudLine` 文档化，新一轮第 7～10 段在代码与台账上对齐收口，Status/事件/诊断摘要层统一可维护。
