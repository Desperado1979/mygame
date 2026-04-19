# P3～D5 审计与观测：200 条扩展清单（10 组 ×20）

> **用途：** 分波落地与续对话对齐；**【已】** 表示仓库内已有对应 **`SrvVal_*`** 或等价观测路径，**【未】** 为规划项（不承诺排期）。**里程碑原则：** 客户端审计为排练/观测，权威仍以服务端为准。

## 执行约定（无优先级）

- **全量清单**：表中 **【未】** 项**不**要求一次性做完；新对话默认**按批**（例如每批 ~20 条可交付增量）从本文件顺序或任意一组继续即可。
- **不必每次选题**：若无优先级说明，则**不**在「先做哪一组」上停留；未实现项保留为 backlog，避免与当前 Unity/服务端工程脱节时硬编码占位。
- **与 README 对齐**：里程碑状态以根目录 **`README.MD`**「当前开发进度」为准；本文件仅作**扩展库存**。

## 第 1 组 · 战斗与技能（20）

1. 【已】CD/MP 拒绝 → **`SrvVal_IllegalOperation`**
2. 【已】普攻未命中 / 无血条敌人 → **`SrvVal_CombatMiss`**
3. 【未】技能目标超出射程（占位）
4. 【未】燃烧/冰冻免疫占位（与耐性表联动）
5. 【未】PvP 开关关闭时攻击玩家占位
6. 【未】安全区内仍尝试造成伤害（与 `SafeZoneSimple` 统一口径）
7. 【未】连击窗口过期（若引入）
8. 【未】武器耐久归零（若引入）
9. 【未】箭矢不足（若引入远程）
10. 【未】沉默/打断状态拒绝施法
11. 【未】霸体与硬直冲突占位
12. 【未】召唤物上限拒绝
13. 【未】队伍伤害分摊与审计边界
14. 【未】世界 Buff 与本地状态不一致告警
15. 【未】技能等级上限拒绝
16. 【未】双持/副手规则拒绝
17. 【未】坐骑/载具状态禁止施法
18. 【未】副本内规则覆盖野外规则（占位）
19. 【未】战斗日志与审计 seq 对齐校验
20. 【未】replay 友好：关键帧带 `seq` 引用

## 第 2 组 · 经济与背包（20）

1. 【已】超重拾取 → **`SrvVal_InventoryFull`** / **`PickupDenied`**
2. 【已】仓库拒绝 → **`SrvVal_BankReject`**
3. 【已】金币不足 → **`SrvVal_WalletReject`**
4. 【已】卖药无货 → **`SrvVal_TradeReject`**
5. 【已】扣除失败 → **`SrvVal_StorageReject`**
6. 【未】交易行上架费不足
7. 【未】拍卖竞价不足
8. 【未】邮件附件超重
9. 【未】公会仓库权限拒绝
10. 【未】绑定/非绑定规则冲突
11. 【未】堆叠上限拆分失败
12. 【未】装备唯一性冲突
13. 【未】时装与属性槽分离占位
14. 【未】材料合成配方锁定
15. 【未】随机词条重Roll消耗不足
16. 【未】背包整理与审计顺序一致性
17. 【未】掉落归属与 `PickupDenied` 细分
18. 【未】死亡掉落与 `WalletReject` 联动
19. 【未】跨场景背包快照一致性检查
20. 【未】经济沙箱：单服通胀告警阈值

## 第 3 组 · 进度与养成（20）

1. 【已】U/I 经验不足 → **`SrvVal_ProgressReject`**
2. 【已】解锁阶/技能点不足 → **`SrvVal_UnlockReject`**
3. 【未】转职/重置消耗拒绝
4. 【未】天赋树前置未满足
5. 【未】成就奖励已领重复请求
6. 【未】日常/周常次数耗尽
7. 【未】赛季通行证等级锁定
8. 【未】熟练度上限拒绝再练
9. 【未】双经验药叠加规则拒绝
10. 【未】休息经验池溢出策略
11. 【未】等级封顶后经验转代币
12. 【未】技能洗点 CD
13. 【未】装备开孔/镶嵌失败审计
14. 【未】宠物养成资源不足
15. 【未】坐骑升级材料不足
16. 【未】生活技能与战斗技能资源争用
17. 【未】账号维度与角色维度进度混淆告警
18. 【未】离线收益与在线审计对齐
19. 【未】跨角色邮寄与 **`StorageReject`** 边界
20. 【未】养成回滚（GM）与审计链

## 第 4 组 · 场景 / 分区 / 传送（20）

1. 【已】传送无落点 → **`SrvVal_PortalReject`**
2. 【已】安全区内 PvP=ON 观测 → **`SrvVal_ZoneReject`**
3. 【未】副本门票不足
4. 【未】副本人数/组队条件不满足
5. 【未】跨服传送维护中
6. 【未】场景加载超时重试审计
7. 【未】无缝大世界分片握手失败
8. 【未】非法坐标拉回与 `ZoneReject` 细分
9. 【未】游泳/飞行区域禁止技能
10. 【未】室内/室外光照与碰撞切换审计
11. 【未】昼夜与刷新表不一致告警
12. 【未】动态事件区域满员
13. 【未】传送读条被打断
14. 【未】公会领地权限
15. 【未】战争区域与和平区域 PK 规则
16. 【未】红名惩罚区强制传送失败
17. 【未】载具召唤区域限制
18. 【未】钓鱼点占用
19. 【未】采集点枯竭
20. 【未】世界 Boss 参与次数

## 第 5 组 · 社交 / 聊天 / 队伍（20）

1. 【已】本地聊天空串 → **`SrvVal_ChatReject`**
2. 【未】敏感词占位（客户端预检）
3. 【未】频道冷却
4. 【未】私聊黑名单
5. 【未】队伍满员邀请
6. 【未】队伍目标变更权限
7. 【未】队长移交失败
8. 【未】团队副本角色职责不匹配
9. 【未】语音占位开关与审计
10. 【未】举报与工单 id 写入审计
11. 【未】公会邀请权限
12. 【未】公会战时间窗外操作
13. 【未】好友上限
14. 【未】师徒任务条件
15. 【未】邮件含非法附件
16. 【未】拍卖结束与成交审计
17. 【未】跨语言频道占位
18. 【未】表情/动作冷却
19. 【未】招募板参数非法
20. 【未】社交封禁与登录拦截联动

## 第 6 组 · 存档 / 会话 / 账号（20）

1. 【已】无存档键读档 → **`SrvVal_LoadReject`**
2. 【已】F11 清档 → **`SrvVal_StateReject`**
3. 【未】存档版本迁移失败
4. 【未】云存档冲突（双写）
5. 【未】会话 token 过期
6. 【未】多设备登录互踢审计
7. 【未】封号状态本地缓存拒绝同步
8. 【未】改名/捏脸消耗不足
9. 【未】角色槽位已满
10. 【未】删角冷静期
11. 【未】GM 回档与审计对齐
12. 【未】测试服与正式服配置混用告警
13. 【未】客户端时钟大幅偏移检测
14. 【未】关键设置篡改校验
15. 【未】反作弊：内存扫描占位
16. 【未】反作弊：加速检测占位
17. 【未】录像与审计 seq 对齐
18. 【未】崩溃上报与审计关联
19. 【未】隐私合规：日志脱敏策略
20. 【未】GDPR/删号请求与审计

## 第 7 组 · 同步与校验（D5，`persist_sync`）（20）

1. 【已】`POST /sync` **`auditSummary`**
2. 【已】`GET /metrics/audit-categories`
3. 【已】`LOG_AUDIT_SUMMARY` 控制台
4. 【已】HUD **`tot:`** + 缩写 + **`AudC:`**
5. 【已】**P3-2**：`REJECT_SRVVAL_AUDIT`、`SRVVAL_REJECT_CATEGORIES`、`SRVVAL_REJECT_THRESHOLD_JSON`、`SRVVAL_ALERT_THRESHOLD_JSON`（见 **`persist_sync.cjs`**）
6. 【已】`audit[]` 与 `player_state`：**`audit_duplicate_seq`** 硬错误；批次含 **`SrvVal_*`** → **`audit_contains_srvval`**（**low** 告警）；**`SYNC_AUDIT_STATE_STRICT=1`** 时尾 vs **`state`** 不一致 → **400**；全量经济/战斗权威仍 **【未】**
7. 【已】replay：**`audit_replay_cli`**；**200** 响应 **`replayObservation`**（观测）；与服权威对比仍 **【未】**
8. 【未】压缩/分包上传失败重试审计
9. 【已】幂等键：**`Idempotency-Key`** + 体哈希；**仅**缓存 **200**（**`SYNC_IDEMPOTENCY_TTL_MS`**）；**`SYNC_IDEMPOTENCY_PERSIST`** → **`data/idempotency-cache.json`**；**Redis** 集群仍 **【未】**
10. 【已】**`X-Sync-Signature`**：**HMAC-SHA256(body)**（**`SYNC_HMAC_SECRET`**，可选）
11. 【已】**`issuedAtUtc`** 时间窗（**`SYNC_ISSUED_AT_MAX_SKEW_SEC`**）；**nonce** 仍 **【未】**
12. 【已】速率限制：**`SYNC_RATE_LIMIT_PER_MINUTE`**（按 IP，**429**）
13. 【已】灰度占位：**`SYNC_REQUIRE_STAGING_HEADER`** → 需 **`X-Sync-Staging: 1`**
14. 【已】维护模式：**`MAINTENANCE_MODE`** → **503**
15. 【已】**`state.version` vs `schemaVersion`**：**`audit_validate`** **`state_version_vs_schema_mismatch`**（**low**）；硬协商仍 **【未】**
16. 【已】PATCH 策略草案（**`GET /rehearsal/patch-strategy`**）；**`POST /rehearsal/validate-patch`**；**`REHEARSAL_PATCH_WRITE` + `POST /rehearsal/apply-patch`**（**`state`** 排练写盘）；**`ETag`/`If-Match`（D16，`GET /rehearsal/etag-concurrency`）**；Unity **`POST /sync` `If-Match`（D17，`PlayerStateExportSimple`）**；**生产 PATCH** 仍 **【未】**
17. 【已】**`warningsByCode` → `SrvVal_*`**：**`SYNC_WARNING_SRVVAL_BRIDGE`** / **`WARNING_CODE_TO_SRVVAL_JSON`** → **`auditSummary.srvValFromWarnings`**（观测）；与 **`audit[]`** 真 **`SrvVal_*`** 深度合并策略仍 **【未】**
18. 【已】**`metrics_archive_lines.cjs`**（按日追加 **`data/metrics-archive/`**）；**冷热分层/压缩** 仍 **【未】**
19. 【已】**`audit_export_bundle.cjs`** + **`manifest.json`**（S3 式 **sha256** 分片）；真桶 ACL/生命周期仍 **【未】**
20. 【已】**`GET /rehearsal/compliance-bundle`** + **hint** 环境变量；法务裁定的周期策略仍 **【未】**

## 第 8 组 · 运维与可观测（20）

1. 【已】`npm run audit-categories`
2. 【已】`GET /health`
3. 【已】**`GET /metrics/prometheus`**（OpenMetrics 文本）+ **`npm run metrics-prometheus`**
4. 【已】最小 **Grafana** 导入示例 **`server/examples/grafana-persist-sync-minimal.json`**；生产级仪表板仍 **【未】**
5. 【已】**`GET /metrics/sync-summary`**（**`npm run sync-summary`**）；专用 HTML 页仍 **【未】**
6. 【未】告警路由（PagerDuty 占位）
7. 【已】SLO：**`acceptRatePercent`**（基于 metrics 内 **`accepted`**）；未记行不计入
8. 【已】SLO：**`sync-summary.latencyMs.p99`**（源自 metrics 行 **`durationMs`**）；采样不足时为空
9. 【未】部署金丝雀与审计对比
10. 【未】配置中心：阈值热更
11. 【未】特性开关与审计旁路
12. 【已】**`examples/k6-smoke.js`**（**`k6 run`** 打 **`/health`**，可选）
13. 【未】混沌：延迟/丢包注入
14. 【未】日志采样策略
15. 【已】**`LOG_REDACT_PII`**（控制台脱敏）；全量 **PII 扫描流水线** 仍 **【未】**
16. 【未】密钥轮换
17. 【未】灾备演练记录
18. 【未】容量规划：审计队列长度
19. 【未】成本：存储 `metrics.ndjson` 增长
20. 【未】on-call 手册与 Runbook

## 第 9 组 · 工具链与工程（20）

1. 【已】`client_sync_request.schema.json` 对齐 Export
2. 【已】`audit_event.schema.json` 单条形状
3. 【已】CI：**.github/workflows/d5-validate.yml**（**`validate_sync_file`**、**`audit_replay_cli`**、**`check_schema_version_constants`**）
4. 【未】CI：Unity headless 编译检查
5. 【已】**`check_schema_files_snapshot.cjs`**（**`schemas/*.schema.json`**）；IDE 预提交钩子仍 **【未】**
6. 【未】代码生成：从 Schema 生成 C# DTO（可选）
7. 【未】双端契约测试（Pact 占位）
8. 【已】**`GET /rehearsal/mock-sync-200`**（固定 **200** 体，**`rehearsal: true`**）
9. 【已】**`docker-compose.yml`**（仅 **`persist_sync`**，无 DB）；**+DB** 仍 **【未】**
10. 【未】审计浏览器插件（DevTools）
11. 【未】F12 导出文件对比工具 GUI
12. 【已】**`merge_metrics_ndjson.cjs`** + **`npm run merge-metrics-ndjson`**
13. 【已】**`npm run metrics-archive`**（追加归档副本）；**压缩** 仍 **【未】**
14. 【已】**EditMode** **`AuditSummaryHudPreviewTests`**（**`AuditSummaryHudPreviewFromJson`**）；全覆盖仍 **【未】**
15. 【已】**`.env.example`** + **`npm run check-schema-version`**（Schema 与 **`ClientSyncRequestSchemaVersion`** 对齐）
16. 【未】LFS 资源与审计无关项剥离
17. 【未】符号服务器（崩溃）
18. 【未】自动化录屏回归
19. 【已】**`check_locale_placeholder.cjs`**（无 **`Localization/`** 则 skip）；完整多语言资源校验仍 **【未】**
20. 【未】文档与代码双向链接检查

## 第 10 组 · 产品与验收（20）

1. 【未】「无玩法改变」类审计 PR 的检查表
2. 【未】QA：触发每条 `SrvVal_*` 的用例矩阵
3. 【未】回归：HUD **`tot:`** 与 **`;`** 快照对比
4. 【未】发布说明：新增 `SrvVal_*` 需列明
5. 【未】策划：哪些审计应对应玩家可见提示
6. 【未】客服：从 `seq` 反查会话
7. 【未】法务：日志保留声明
8. 【未】商业化：付费与审计一致性
9. 【未】未成年人：时段与审计
10. 【未】跨平台：Steam/Epic 账号绑定拒绝场景
11. 【未】主机版：证书 pinning 与审计
12. 【未】云游戏：输入延迟与误触审计
13. 【未】电竞模式：禁用部分同步的声明
14. 【未】MOD：白名单接口
15. 【未】赛季结算与审计冻结点
16. 【未】合服：ID 映射与审计迁移
17. 【未】删档测试与 `StateReject` 期望
18. 【未】玩家教育：F4/F2 教程页
19. 【未】内部演练：红队伪造 `audit[]`
20. 【未】本清单自身：每季度删减「永不做」项
