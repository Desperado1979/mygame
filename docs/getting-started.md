# EpochOfDawn — 入门与本地构建

## 环境

- **Unity Editor**：**2022.3 LTS**（与工程 `ProjectVersion.txt` 一致）。
- **渲染**：**URP**（工程已按 URP 模板创建）。
- **平台**：Windows（当前主验收平台）。

## 打开工程

1. 安装 **Unity Hub**，安装 **Unity 2022.3 LTS**（含 **Windows Build Support**）。
2. 在 Hub 中选择 **Add**，指向本仓库中的 **`EpochOfDawn`** 文件夹（内含 `Assets`、`Packages`、`ProjectSettings`）。
3. 首次打开会导入资源并生成 `Library/`（勿提交到 Git）。

## 运行 Play

1. 打开场景 **`Assets/Scenes/SampleScene.unity`**。
2. 按 **Play**。  
   - 若使用占位胶囊角色，确保场景内玩家对象挂载了 **`PlayerMoveSimple`** 等已有脚本（以场景实际引用为准）。

## Windows 可执行文件构建

1. **File → Build Settings**。
2. **Platform** 选 **Windows**，**Switch Platform**（若尚未切换）。
3. **Add Open Scenes** 将当前场景加入列表。
4. **Build** 或 **Build And Run**，选择输出目录。

## D5 本地排练：导出三件套（与 `server/` 联调）

在运行时使用（默认热键，可在 **`PlayerHotkeysSimple`** 上修改）：

| 热键 | 输出 | 说明 |
|------|------|------|
| **F12** | `player_state_export.json` | 当前状态快照（`Application.persistentDataPath`） |
| **F4** | `audit_events.ndjson` | 客户端审计队列 |
| **F3** | `client_sync_request.json` | 符合 `server/schemas/client_sync_request.schema.json` 的整包请求体（`dry_run_no_network`） |

**需先启动** `npm run persist`（默认 `http://127.0.0.1:8787`）后，可在运行时：

| 热键 | 作用 |
|------|------|
| **F2** | `POST /sync` 上传当前整包（`mode: sync_rehearsal`），响应摘要写入 HUD 可选槽位，完整正文见 `last_sync_response.json` |
| **`[`** | `GET /metrics/alerts/players?...` 摘要（调试用） |
| **`]`** | `GET /metrics/dashboard?...` 摘要（调试用） |
| **F1** | `GET /health`（HUD **`HLT:`** 一行摘要） |
| **`;`** | `GET /metrics/audit-categories?days=7`（HUD **`AudC:`**，聚合 **`SrvVal_*`**） |

（默认与 **`PartyPlaceholderSimple`** 的 **小键盘 +/-** 分队热键错开；若改热键请注意冲突。）

**审计类别聚合（浏览器 / curl）**：在已有 **`metrics.ndjson`** 记录的前提下，可请求  
`GET http://127.0.0.1:8787/metrics/audit-categories?days=7`（可选 **`&playerId=...`**），合并各次 **`POST /sync`** 写入的 **`auditSummary.byCategory`**，用于观察 **`SrvVal_*`** 累计分布。

**CLI（在 `EpochOfDawn/server/`）**：`npm run audit-categories`（可选参数：`node tools/fetch_audit_categories.cjs 7 local_player_001`）。环境变量 **`SYNC_BASE_URL`** 可改服务根地址。

**persist_sync 控制台**：设置环境变量 **`LOG_AUDIT_SUMMARY=1`** 可在每次 **`POST /sync` 成功** 后多打一行 **`auditSummary`** JSON，便于本机对照。生产/共享日志可开 **`LOG_REDACT_PII=1`**（脱敏控制台中的邮箱、疑似卡号、IP 末段等；**不**改写 **`metrics.ndjson`**）。

**P3-2（可选策略，默认关闭）**：与 **`REJECT_HIGH_WARNINGS`** 独立。

| 环境变量 | 作用 |
|----------|------|
| **`REJECT_SRVVAL_AUDIT=1`** | 若 **`audit[]`** 中任一条 **`category`** 以 **`SrvVal_`** 开头，则 **`POST /sync`** 返回 **400**（**不写**存档文件） |
| **`SRVVAL_REJECT_CATEGORIES`** | 逗号分隔的**精确**类别名，命中即 **400** |
| **`SRVVAL_REJECT_THRESHOLD_JSON`** | JSON 对象，键为类别、值为**本包** `auditSummary.byCategory` 计数阈值（`>=` 即 **400**） |
| **`SRVVAL_ALERT_THRESHOLD_JSON`** | 同上形状，仅 **`console.warn`**，**不**拒收 |

启动 **`npm run persist`** 时控制台会打印当前是否开启；**`GET /health`** JSON 含 **`rejectAnySrvValAudit`**、**`srvValCategoryRejectList`**、**`metricsPrometheusPath`** 等摘要。

**Prometheus 文本（可选）**：**`GET http://127.0.0.1:8787/metrics/prometheus?days=7`**（参数与 **`audit-categories`** 相同）；CLI：**`npm run metrics-prometheus`**（同 **`fetch_audit_categories`** 参数位置）。

**GitHub Actions**：仓库 **`.github/workflows/d5-validate.yml`** 在变更 **`EpochOfDawn/server/**`** 时跑 **`npm ci`**、**`validate_sync_file`**、**`audit_replay_cli`**、**`check_schema_version_constants`**、**`check_schema_files_snapshot`**、**`audit_export_bundle`**、**`check_locale_placeholder`**、**`patch_validate_rehearsal --smoke`**（无 **`Assets/Localization`** 时跳过 locale 步）。

**`persist_sync` 进阶（默认均关闭，见启动日志与 `GET /health`）**：

| 环境变量 | 作用 |
|----------|------|
| **`MAINTENANCE_MODE=1`** | **`POST /sync`** → **503** |
| **`SYNC_RATE_LIMIT_PER_MINUTE`** | 每 IP 每分钟最大次数；超限 **429** + **`Retry-After`** |
| **`SYNC_IDEMPOTENCY_TTL_MS`** | **`Idempotency-Key`**：与请求体绑定哈希，TTL 内重复请求直接返回缓存的 **200** |
| **`SYNC_HMAC_SECRET`** | 要求 **`X-Sync-Signature`** = **hex(HMAC-SHA256(body))** |
| **`SYNC_REQUIRE_STAGING_HEADER=1`** | 要求 **`X-Sync-Staging: 1`**，否则 **403** |
| **`SYNC_ISSUED_AT_MAX_SKEW_SEC`** | **`issuedAtUtc`** 与服务器时钟偏差上限（秒）；超出则 **400**（**0** 关闭） |
| **`SYNC_ETAG_DISABLED=1`** | **D16**：不校验 **`If-Match`** 与当前存档是否一致（响应仍可能带 **`ETag`**） |

**D9（续九）补充**：**`POST /sync` 200** 可含 **`replayObservation`**（审计尾与 **`state`** 是否对齐）；**`GET /rehearsal/patch-strategy`** 为 PATCH 路线草案；**`npm run check-schema-version`**；**`npm run metrics-archive [天数]`** 将旧行追加到 **`data/metrics-archive/`**。详见 **`README.MD`**「本次续写 · D9 续九」。

**D10（续十）补充**：**`GET /rehearsal/compliance-bundle`**（留存/PII/S3 导出**说明**）；**`LOG_REDACT_PII`**；**`npm run check-schema-snapshot`**（**`schemas/*.schema.json`** 与快照 **`tools/schema_files_snapshot.json`**）；**`npm run audit-export-bundle`**（**`data/audit-export/export-*/manifest.json`**）；Unity **Window → General → Test Runner** 跑 **`EpochOfDawn.Tests.EditMode`**。详见 **`README.MD`**「本次续写 · D10 续十」。

**D11（续十一）补充**：**`SYNC_WARNING_SRVVAL_BRIDGE=1`** 或 **`WARNING_CODE_TO_SRVVAL_JSON`** → 响应 **`auditSummary.srvValFromWarnings`**；**`GET /rehearsal/warning-srvval-bridge`**；Grafana 见 **`EpochOfDawn/server/examples/grafana-persist-sync-minimal.json`**。详见 **`README.MD`**「本次续写 · D11 续十一」。

**D12（续十二）补充**：**`SYNC_IDEMPOTENCY_PERSIST=1`**（且 **`SYNC_IDEMPOTENCY_TTL_MS`>0**）将幂等 **200** 缓存写入 **`data/idempotency-cache.json`**；**`GET /rehearsal/idempotency-persist`**。详见 **`README.MD`**「本次续写 · D12 续十二」。

**D13（续十三）补充**：**`POST /rehearsal/validate-patch`**（**`ops` + 可选 `baseState`**，白名单路径，**不写盘**）；**`npm run validate-patch-rehearsal`**。详见 **`README.MD`**「本次续写 · D13 续十三」。

**D14（续十四）补充**：**`SYNC_AUDIT_STATE_STRICT=1`** 时，**audit 重放尾**与 **`state`**（金/药/version/playerId/schema）不一致则 **400**；**`GET /rehearsal/audit-state-strict`**。详见 **`README.MD`**「本次续写 · D14 续十四」。

**D15（续十五）补充**：**`REHEARSAL_PATCH_WRITE=1`** 时 **`POST /rehearsal/apply-patch`**（**`playerId` + `ops`**，读已有存档，仅改 **`state`**）；**`GET /rehearsal/apply-patch`**。详见 **`README.MD`**「本次续写 · D15 续十五」。

**D16（续十六）补充**：**`GET /state`** / **`POST /sync`** / **`POST /rehearsal/apply-patch`** 成功响应带 **`ETag`**（存档 UTF-8 字节的 **SHA-256**）；可送 **`If-Match`** 做乐观并发（**412** **`precondition_failed`**）；**`GET /rehearsal/etag-concurrency`**；上表 **`SYNC_ETAG_DISABLED`**。详见 **`README.MD`**「本次续写 · D16 续十六」。

**Docker**：在 **`EpochOfDawn/server/`** 执行 **`docker compose up`**（映射 **8787**）。**NDJSON**：**`npm run merge-metrics-ndjson -- data/metrics.ndjson`** 或管道排序。**k6**（可选）：**`k6 run examples/k6-smoke.js`**。

**SLO 摘要**：**`GET /metrics/sync-summary?days=7`**；CLI：**`npm run sync-summary`**。**Mock 响应**：**`GET /rehearsal/mock-sync-200`**。**审计重放**：**`npm run audit-replay -- examples/request_payload_example.json`**。**环境变量模板**：**`EpochOfDawn/server/.env.example`**。

**Unity HUD**：**F2 POST** 后若有 **`PlayerStateExportSimple`**，可显示 **`syn:`**（如 **`syn:ok`**、**`syn:maint`**、**`syn:time`**），与 **`tot:`** 独立；若服务端返回 **`X-Sync-Duration-Ms`**，可显示 **`d:Nms`**；若 **429** 后重试，可显示 **`r:`**（重试次数）。

**延迟与 SLO**：**`GET /metrics/sync-summary`** 的 **`latencyMs`**（**p50/p90/p99**）；**Prometheus** 路径亦含 **`persist_sync_post_duration_*`**（有 **`durationMs`** 样本时）。**滚动计划（10×20）**：**`EpochOfDawn/docs/p3-d5-waves-10x20.md`**。

**HUD 缩写补充（与 `PlayerStateExportSimple.SyncResponseParse` 一致）**：除 **`SV/Inv/Pk/…/St`** 外，另有 **`Zo`**（`SrvVal_ZoneReject`）、**`Pt`**（`SrvVal_PortalReject`）、**`Sa`**（`SrvVal_StateReject`）、**`Ch`**（`SrvVal_ChatReject`）等，均来自服务端返回的 **`auditSummary.byCategory`** 键名。

**扩展清单（10 组 ×20 条）**：见 **`EpochOfDawn/docs/p3-d5-roadmap-200.md`**（【已】/【未】分波落地用）。

## P1-A 数据驱动（推荐）

- 在 Project 中 **右键 → Create → EpochOfDawn → P1-A Content Config**，得到 **`P1AContentConfig`** 资源（类型定义在 **`P1A1QuestState.cs`** 同文件，与 `P1A1QuestState` 共用同一脚本 GUID）。
- 将资源拖到 **`P1A1QuestState`** / **`WaveSpawnerSimple`** / **`P1A1WildSpawner`** / **`P1MiniBossSimple`** / **`P1BossPhaseSimple`** 的 **`contentConfig`** 字段，即可用**同一套表**驱动目标击杀数、波次、半径与小 Boss 参数（与 README「数据驱动」一致）。
- **不拖也行**：工程已包含 **`Assets/Resources/P1A/DefaultP1AContent.asset`**；上述组件 **`contentConfig` 留空** 时，运行时会 **`Resources.Load("P1A/DefaultP1AContent")`** 自动采用（可用自定义资源覆盖路径或改表数值）。

## P1-A 可选组件

- **`P1A1WildSpawner`**：开局按圆周刷固定数量普通怪（验收 P1-A-1）。**`SampleScene`** 中对象 **`P1A_WildRing`** 默认**关闭**，需要时再勾选激活。
- **`WaveSpawnerSimple`**：按 **`waveEnemyCounts`** 分波、清场间隔再刷（验收 P1-A-2）；**`DebugHudSimple`** 可显示 **`Wv:`** 进度。**`SampleScene`** 已挂 **`P1A_WaveSpawner`**（默认开启）。
- **`P1MiniBossSimple`**：挂在敌人预制体上，拉高 **`EnemyHealthSimple.maxHp`**（验收 P1-A-3 数值向小 Boss）。
- **`P1BossPhaseSimple`**（可选）：与上同挂；血量低于 **`hpFractionForPhase2`** 时提高 **`EnemyChaseSimple.moveSpeed`**，占位「阶段」。

## P1-A-5（里程碑）Windows Build & Run 验收清单

在 **Unity 2022.3 LTS** 下完成下列项即可视为本里程碑 **可验收**（是否「已完成」以你在 README 勾选为准）：

1. **File → Build Settings**：平台 **Windows**，**SampleScene**（或当前主场景）已加入列表。
2. **构建方式（二选一）**
   - **菜单**：**EpochOfDawn → Build Windows Player (P1-A-5)** → 输出到工程根下 **`Build/P1-A5-Windows/EpochOfDawn.exe`**（需先关闭其他占用本工程的 Unity 实例，否则批处理会失败）。
   - **或** **File → Build Settings → Build**，自选输出目录。
3. 双击 **`.exe`** 能进入游戏、无红字崩溃。
4. **玩法复现（最低）**：能 **WASD 移动**，至少一次 **普攻/技能** 与敌人交互；若场景已挂 **P1A1** / **Wave** / **D5**，按你本地配置抽样点验即可。
5. **可选**：录屏或保存一份「操作步骤」文本，便于后续对话续接。

**D5 排练（可选）**：先 **`npm run persist`**，游戏内 **F2 POST /sync**，`server/data/` 下出现对应 **`*.json`** 即视为联调路径可用。

服务端演练脚本与校验见 **`EpochOfDawn/server/`**。

**校验 F3 导出文件（在 `server/` 目录）：**

```bash
npm install
npm run validate "C:/Users/<你>/AppData/LocalLow/DefaultCompany/EpochOfDawn/client_sync_request.json"
```

期望输出含 **`schema: OK`** 与 **`audit: OK`**（`schemas/` 内需包含 `client_sync_request.schema.json`、`player_state.schema.json`、`audit_event.schema.json`）。

本地起服务排练：`npm run persist`（见 `server/package.json`）。

---

**会话收尾备忘（2026-04-19）**：**`origin/main`** 已推送；**D16**（**`ETag`/`If-Match`**）与 **`npm run check-syntax`** 在 **`server/`**；完整进度与「续接口令」见工程根目录 **`README.MD`**（与上级工作区 **`d:\mygame\README.MD`** 宜保持同步）。
