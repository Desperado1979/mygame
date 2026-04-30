# EpochOfDawn — `server/`（D5 `persist_sync` 排练服）

> 本目录**不是**早期「仅占位」的草案：主程序为 **`tools/persist_sync.cjs`**，提供 **`POST /sync`**、**`/health`**、**`/metrics/*`**、**`/rehearsal/*`** 等联调与观测能力。详细端点、环境变量、热键与 Unity 侧 **F2/F3/F4/F12** 见根路径文档。

## 快速开始

```bash
cd EpochOfDawn/server
npm ci
npm run persist
```

默认监听 **http://127.0.0.1:8787**（以启动日志与 `GET /health` 为准）。**另一终端探活（可选，须 persist 已跑）：** `npm run health-check`。

## 文档索引（以这些为准，勿依赖本文件旧版「Next steps」列表）

| 内容 | 路径 |
|------|------|
| Unity 开工程、Build、D5 热键与 Inspector 开关说明；**§6.5** 战斗 HUD Debug（`tgt*` / `ply*` / `tgt:def`） | `../../docs/getting-started.md` |
| 风险/指标快览 | `RISK_MONITORING_QUICKSTART.md` |
| 规约、进度主表、补漏口径 | 仓库根目录 `README.MD` |
| 开发状态合并摘要（多文档归纳，非真源） | 仓库根目录 `游戏开发阶段性计划与进度.md` |
| P3–D5 扩展库存（【未】= backlog，不自动开工） | 见根目录 `游戏开发阶段性计划与进度.md` **D3. 旧库存说明** 与 **D1** 区 D9–D21 速查（无单独 Appendix） |

## 常用脚本

- **`npm run persist`**：启动 `persist_sync` 排练服务。  
- **`npm run health-check`**：对已运行的服务请求 **`GET /health`**（默认 **`http://127.0.0.1:8787`**，`tools/health_check.cjs`）；用于人工或脚本快速确认存活。  
- **`npm run check-syntax`**：对若干 **`tools/*.cjs`** 做 **`node --check`** 语法门禁（含 **`health_check.cjs`**、**`validate_sync_file.cjs`**、**`compare_state_export.cjs`** 等，与 **`package.json`** 一致；改脚本前后可跑）。  
- **`npm run validate`**：`tools/validate_sync_file.cjs`，校验**单个**导出/sync JSON（schema 门禁）；可加 `--fail-on-high`，有高危告警时返回非 0。  
- **`npm run compare-state`**：`tools/compare_state_export.cjs`，**两份** JSON 离线比对（服务端/sync **`state`** vs Unity **F12 `player_state_export.json`** 等）；**`Usage`** 与示例见脚本文件头。  
- **`npm run validate-patch-rehearsal`**：`tools/patch_validate_rehearsal.cjs --smoke`，**PATCH rehearsal** 白名单自检（与 **`/rehearsal/*`** 同源向），**不写盘**；成功输出 **`patch_validate_rehearsal`** JSON。  
- **`npm run check-schema-version`**：校验 schemaVersion 常量（`client_sync_request.schema.json` vs Unity `Export.cs`）。  
- **`npm run check-schema-snapshot`**：校验 `schemas/*.schema.json` 哈希快照；若 FAIL，按脚本提示 `--write` 后复跑。  
- **`npm run check-locale-ci`**：运行 locale 占位门禁（无 `Assets/Localization` 时 `skip` 且 exit 0）。  
- **`npm run sync-summary`**：请求 `GET /metrics/sync-summary` 输出最近窗口摘要（默认 7 天；可传 `days`、`playerId`），用于快速看 accepted/rejected 与延迟分位。  
- **`npm run metrics-prometheus`**：请求 `GET /metrics/prometheus` 输出 OpenMetrics 文本（默认 7 天；可传 `days`、`playerId`），用于 Prometheus/Grafana 接入前的本机核对。  
- **`npm run metrics-report -- --days 7 --top 5`**：离线读取 `data/metrics.ndjson` 生成汇总（支持 `--days`/`--top`/`--player`），用于快速看 acceptRate、warnings 与 top code 趋势。  
- **`npm run audit-categories`**：请求 `GET /metrics/audit-categories` 输出分类汇总 JSON（默认 7 天；可传 `days`、`playerId`），用于快速看审计类别分布。  
- **`npm run metrics-csv`**：拉取 `/metrics/report?format=csv` 并写本地 CSV（默认 `metrics_report.csv`；可传输出文件名与 queryString）。  
- **`npm run metrics-archive`**：将早于阈值天数的 `data/metrics.ndjson` 行按日期追加到 `data/metrics-archive/part-YYYY-MM-DD.ndjson`（默认 30 天，非破坏性归档）。  
- **`npm run merge-metrics-ndjson -- <path>`**：读取 ndjson 并按 `ts` 升序输出到 stdout（仅排序，不去重），用于离线整理 metrics 行序。  
- **`npm run risk-brief`**：汇总输出风险简报（请求 report/players/rejections/anomalies 四个端点），用于值班时快速查看 top 风险玩家、拒绝原因与异常尖峰。  
- **`npm run risk-alerts`**：按阈值规则输出风险告警（`GREEN/RED`），可附 `--minAcceptRate` / `--maxHighWarnings` / `--maxRejected` / `--minSpikeDelta`，并支持 `--csv` 导出告警表。  
- **`npm run risk-player-alerts`**：按玩家维度输出风险告警（`flagged_players` + `GREEN/RED`），可附 `--days` / `--top` / 阈值参数，并支持 `--csv` 导出。  
- **`npm run daily-risk`**：输出按天风险摘要（`window/overall/byDay`），用于日报快速汇总。  
- **`npm run daily-risk-template`**：生成日报模板 Markdown（默认落 `data/daily_risk_YYYY-MM-DD.md`），可选同时输出 JSON 快照并做与上次快照的增量对比。  
- **`npm run daily-risk-index`**：生成日报归档索引（默认 `data/daily_risk_index.md`），支持按环境与时间窗过滤并可输出 JSON 索引。  
- **`npm run risk-dashboard`**：汇总输出风险看板快照（`/metrics/dashboard`），默认打印 requests/acceptRate/warnings/top 项，可选导出 JSON/CSV/Markdown。  
- **`npm run risk-doctor`**：执行风险巡检（health/report/alerts/player_alerts/dashboard），默认并行 + 自动串行回退；支持导出 JSON/Markdown 诊断报告。  
- **`npm run risk-doctor-serial`**：`risk-doctor` 串行快捷入口（`--parallel 0 --retries 1`）。  
- **`npm run risk-doctor-core`**：`risk-doctor` 核心检查入口（`--only health,metrics_report,dashboard`）。  
- **`npm run risk-smoke`**：执行风险链路冒烟（doctor + dashboard + alerts），输出 `risk_smoke_result=PASS/FAIL`。  
- **`npm run risk-smoke-report` / `risk-smoke-trend` / `risk-smoke-gate` / `risk-smoke-history`**：风险冒烟报告、趋势对比、门禁与历史汇总脚本族。  
- **`npm run risk-players` / `risk-codes` / `risk-rejections` / `anomaly-report`**：风险玩家、告警码、拒绝原因与异常尖峰的离线/接口汇总。  
- **`npm run audit-export-bundle`**：导出审计打包（含 `manifest.json`）。  
- **`npm run audit-replay -- <client_sync_request.json>`**：审计重放 CLI；无参数时显示 Usage 并返回非 0（预期）。  
- 其余 **`metrics-*` / `risk-*` / `audit-*`** 见 `package.json` 的 `scripts`。

## 范围说明

- **当前定位**：与 Unity 导出的 **State / Audit / Request** 做 Schema 与排练链路上的对齐，**不是**已上线的商业 MMORPG 战斗服。  
- **「登录 / 分区 / 真·生产写盘」** 若要做，应作为**新里程碑**写入根 `README.MD` 主表后再实现，避免与 D5 排练层混作一谈。
