# 风险监控快速手册（只看操作）

这份文档只讲你怎么用，不讲实现细节。

## 你要做的事情（最简版）

1. 启动服务端监控进程  
2. 在游戏里按 `F3` 触发上报几次  
3. 用浏览器打开监控地址看结果  

---

## 一次性准备

在 `d:\mygame\EpochOfDawn\server` 目录执行：

`npm install`

---

## 日常操作步骤

### 1) 启动服务（普通模式）

在项目根目录 `d:\mygame` 执行：

`node EpochOfDawn/server/tools/persist_sync.cjs`

看到类似输出即成功：

- `Persist sync http://127.0.0.1:8787`
- `POST /sync`
- `GET /metrics/report...`

### 2) 触发数据上报

在 Unity 里进入 Play：

- 确保 `Player State Export Simple` 勾选了可 POST（你之前已配置）
- 按 `F3` 几次（做一些买药/打怪操作后再按更有数据）

### 3) 浏览器看监控

打开下面任意地址：

- 最近 7 天总览  
  `http://127.0.0.1:8787/metrics/report?days=7&top=10`

- 按天看趋势（每日风险）  
  `http://127.0.0.1:8787/metrics/report?days=7&top=10&groupBy=day`

- 按小时看短时波动（最近 1 天）  
  `http://127.0.0.1:8787/metrics/report?days=1&top=10&groupBy=hour`

- 只看某个玩家（按你的 playerId 替换）  
  `http://127.0.0.1:8787/metrics/report?days=7&top=10&playerId=local_player_001&groupBy=day`

- 只看“被拒绝”的请求（查拦截原因）  
  `http://127.0.0.1:8787/metrics/report?days=7&rejectedOnly=true&groupBy=day`

- 导出 CSV（方便 Excel）  
  `http://127.0.0.1:8787/metrics/report?days=7&top=10&groupBy=day&format=csv`

- 风险玩家榜（谁最需要先排查）  
  `http://127.0.0.1:8787/metrics/players?days=7&top=10`

- 风险代码榜（哪类问题最多）  
  `http://127.0.0.1:8787/metrics/codes?days=7&top=10`

- 拒绝原因榜（系统为什么拦）  
  `http://127.0.0.1:8787/metrics/rejections?days=7`

- 异常突增检测（最近窗口 vs 上一窗口）  
  `http://127.0.0.1:8787/metrics/anomalies?hours=24&compareHours=24&top=5&minCount=1`

---

## 如何读结果（只看关键字段）

- `requests.total`：总请求数  
- `requests.accepted/rejected`：通过/拒绝  
- `requests.acceptRate`：通过率（越高越稳）
- `warnings.low/high`：低风险/高风险总量  
- `warnings.topCodes`：最常见风险类型（排查优先看它）
- `validation.warningSummary`：单次请求里的风险数量
- `validation.warningsByCode`：单次请求里每种风险的数量
- `blockedBy`：被拦截时的高风险原因（严格模式下）

---

## 严格模式（可选）

如果你要“高风险直接拦截，不写入数据”，用这个启动方式：

PowerShell：

`$env:REJECT_HIGH_WARNINGS='1'; node EpochOfDawn/server/tools/persist_sync.cjs`

效果：

- 一旦有 `high` warning，`POST /sync` 返回 400
- 响应里会有 `error=high_warning_block` 和 `blockedBy`

---

## 命令行查看趋势（不走网页）

在 `d:\mygame\EpochOfDawn\server` 目录：

- 总览：  
  `npm run metrics-report -- --days 7 --top 10`

- 单玩家：  
  `npm run metrics-report -- --days 7 --top 10 --player local_player_001`

- 健康检查：  
  `npm run health-check`

- 导出 CSV：  
  `npm run metrics-csv -- risk_report.csv "days=7&top=10&groupBy=day"`

- 每日简报：  
  `npm run daily-risk -- 7`

- 风险玩家榜：  
  `npm run risk-players -- --days 7 --top 10`

- 风险代码榜：  
  `npm run risk-codes -- --days 7 --top 10`

- 拒绝原因榜：  
  `npm run risk-rejections -- --days 7`

- 异常突增简报：  
  `npm run anomaly-report -- --hours 24 --compareHours 24 --top 5 --minCount 1`

- 一键风险总览（推荐）：  
  `npm run risk-brief -- --days 7 --top 5 --hours 24 --compareHours 24`

- 阈值告警（红线监控，可导出 CSV）：  
  `npm run risk-alerts -- --days 7 --minAcceptRate 95 --maxHighWarnings 0 --maxRejected 0 --minSpikeDelta 2 --csv risk_alerts.csv`

- 浏览器阈值告警接口：  
  `http://127.0.0.1:8787/metrics/alerts?days=7&minAcceptRate=95&maxHighWarnings=0&maxRejected=0&minSpikeDelta=2`

- 告警 CSV（浏览器直接下载文本）：  
  `http://127.0.0.1:8787/metrics/alerts?days=7&minAcceptRate=95&maxHighWarnings=0&maxRejected=0&minSpikeDelta=2&format=csv`

- 玩家维度告警榜（谁超线）：  
  `http://127.0.0.1:8787/metrics/alerts/players?days=7&top=10&minAcceptRate=95&maxHighWarnings=0&maxRejected=0`

- 玩家告警 CSV：  
  `http://127.0.0.1:8787/metrics/alerts/players?days=7&top=10&minAcceptRate=95&maxHighWarnings=0&maxRejected=0&format=csv`

- 玩家告警命令：  
  `npm run risk-player-alerts -- --days 7 --top 10 --minAcceptRate 95 --maxHighWarnings 0 --maxRejected 0 --csv risk_player_alerts.csv`

- 每日简报模板（自动生成 md 文件）：  
  `npm run daily-risk-template -- --days 7 --top 5`

- 多环境日报（带负责人/工单号）：  
  `npm run daily-risk-template -- --days 7 --top 5 --env prod --owner ops_lead --incident INC-2026-0415`

- 日报同时导出 JSON 快照：  
  `npm run daily-risk-template -- --days 7 --top 5 --env stage --owner qa_lead --jsonOut data/daily_risk_stage.json`

- 归档索引（汇总历史日报）：  
  `npm run daily-risk-index -- --dir data --out data/daily_risk_index.md --limit 60`

- 归档索引按环境筛选（例如 prod）：  
  `npm run daily-risk-index -- --dir data --out data/daily_risk_prod_index.md --env prod --limit 60`

- 归档索引按日期区间筛选：  
  `npm run daily-risk-index -- --dir data --out data/daily_risk_2026w15.md --since 2026-04-01 --until 2026-04-30 --limit 200`

- 归档索引导出 JSON：  
  `npm run daily-risk-index -- --dir data --out data/daily_risk_index.md --jsonOut data/daily_risk_index.json`

- 一键仪表盘快照：  
  `npm run risk-dashboard -- --days 7 --top 5 --hours 24 --compareHours 24 --out data/risk_dashboard.json`

- 仪表盘 CSV 导出：  
  `npm run risk-dashboard -- --days 7 --top 5 --hours 24 --compareHours 24 --csvOut data/risk_dashboard.csv`

- 仪表盘分段 CSV（players/codes/rejections/alerts 分段）：  
  `npm run risk-dashboard -- --days 7 --top 5 --csvMode sectioned --csvOut data/risk_dashboard_sectioned.csv`

- 仪表盘 Markdown 快照：  
  `npm run risk-dashboard -- --days 7 --top 5 --markdownOut data/risk_dashboard.md`

- 浏览器直接拿 dashboard CSV：  
  `http://127.0.0.1:8787/metrics/dashboard?days=7&top=5&hours=24&compareHours=24&format=csv&csvMode=sectioned`

- 浏览器直接拿 dashboard Markdown：  
  `http://127.0.0.1:8787/metrics/dashboard?days=7&top=5&hours=24&compareHours=24&format=md`

- 浏览器拿紧凑 Markdown（不含 rejection/spike 分段）：  
  `http://127.0.0.1:8787/metrics/dashboard?days=7&top=5&hours=24&compareHours=24&format=md&mdMode=compact`

- 健康巡检（接口联通性）：  
  `npm run risk-doctor -- --base http://127.0.0.1:8787`

- 巡检导出 JSON：  
  `npm run risk-doctor -- --base http://127.0.0.1:8787 --jsonOut data/risk_doctor.json`

- 巡检导出 Markdown（并设置重试/并行）：  
  `npm run risk-doctor -- --base http://127.0.0.1:8787 --timeoutMs 6000 --retries 1 --parallel 1 --markdownOut data/risk_doctor.md`

- 巡检并行失败后自动串行复检（默认开启）：  
  `npm run risk-doctor -- --base http://127.0.0.1:8787 --parallel 1 --autoFallbackSerial 1`

- 巡检只检查核心三项：  
  `npm run risk-doctor -- --base http://127.0.0.1:8787 --only health,metrics_report,dashboard`

- 巡检 CI 状态码模式（便于流水线分类失败）：  
  `npm run risk-doctor -- --base http://127.0.0.1:8787 --exitCodeMode ci`

- 巡检失败演练（不改服务，只模拟指定项失败）：  
  `npm run risk-doctor -- --base http://127.0.0.1:8787 --simulateFail dashboard`

- 一键 smoke（doctor + dashboard + alerts）：  
  `npm run risk-smoke`

- 指定端口做 smoke（例如 8788）：  
  `npm run risk-smoke -- --base http://127.0.0.1:8788`

- smoke 导出汇总报告：  
  `npm run risk-smoke -- --base http://127.0.0.1:8788 --jsonOut data/risk_smoke.json --markdownOut data/risk_smoke.md`

- smoke 对比上一次基线并写历史：  
  `npm run risk-smoke -- --base http://127.0.0.1:8788 --jsonOut data/risk_smoke_now.json --compareWith data/risk_smoke_baseline.json --saveAsBaseline data/risk_smoke_baseline.json --historyOut data/risk_smoke_history.ndjson`

- smoke 门禁（不通过就返回失败）：  
  `npm run risk-smoke -- --base http://127.0.0.1:8788 --maxFailed 0 --maxDeltaFailed 0 --requireOk 1`

- smoke 历史简报（7/30 天）：  
  `npm run risk-smoke-history`

- 日报自动对比上一份 json（变化率/变化量）：  
  `npm run daily-risk-template -- --days 7 --top 5 --jsonOut data/daily_risk_today.json --autoPrev 1`

---

## 本地文件在哪

- 服务端状态数据：`d:\mygame\EpochOfDawn\server\data\*.json`
- 风险趋势日志：`d:\mygame\EpochOfDawn\server\data\metrics.ndjson`

---

## 常见问题（你只看这里）

- 访问接口没反应：先确认 `persist_sync.cjs` 正在运行。
- 监控里一直没数据：你没触发 `F3`，或客户端没 POST 成功。
- 看到 400：说明校验在拦截，去看响应里的 `error` 和 `blockedBy`。
- 页面有数据但看不懂：先只看 `acceptRate` 和 `warnings.topCodes` 两个字段。

