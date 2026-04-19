# P3～D5：10 波 ×20 步滚动计划（共 200 步）

> **说明：** 「**再走 10 个 20 步**」= 按 **10 波**推进，每波约 **20 条可交付增量**；**非**要求单次会话写满 200 条实现。已完成波次在 **`README.MD`**「本次续写」与 **`p3-d5-roadmap-200.md`** 交叉对照。

| 波次 | 主题 | 状态（概要） |
|------|------|----------------|
| 波 1 | P3 审计基线、`SrvVal_*`、F4/F2 | 已多轮落地 |
| 波 2 | `persist_sync` 聚合、`auditSummary`、HUD | 已落地 |
| 波 3 | P3-2 策略拒收、Prometheus、CI validate | 已落地 |
| 波 4 | 分区/传送/存档/聊天审计、200 条 roadmap 文件 | 已落地 |
| 波 5 | 重复 seq、SrvVal 低告警、Party、Prometheus、CI | 已落地 |
| 波 6 | 维护/限流/幂等/HMAC/Staging、Docker、k6、NDJSON 合并 | 已落地 |
| 波 7 | sync-summary、mock 200、audit_replay、`.env.example`、`syn:` | 已落地 |
| 波 8 | `durationMs` 全路径、`X-Sync-Duration-Ms`、延迟分位数、schema/state 对齐告警 | **已落地** |
| **波 9** | **issuedAt 时间窗、`replayObservation`、PATCH 草案、schema CI、metrics 归档、429 重试** | **已落地（D9 续九）** |
| **波 10** | **合规 bundle、PII 日志脱敏、S3 式审计导出、schema 快照 CI、locale CI 占位、Unity EditMode** | **已落地（D10 续十）** |

---

## 波 8（~20 步）— 已落地

见 **`README.MD`**「本次续写 · D8 续八」：全路径 **`durationMs`**/**`httpStatus`**、**`sync-summary.latencyMs`**、Prometheus 延迟、`state.version` vs **`schemaVersion`** 告警、HUD **`d:`**。

---

## 波 9（~20 步）— 已落地（D9 续九）

1. **`SYNC_ISSUED_AT_MAX_SKEW_SEC`** 可选校验 **`issuedAtUtc`**。
2. **`missing_issuedAtUtc` / `invalid_issuedAtUtc` / `issued_at_skew`** 与 metrics。
3. **200** 响应字段 **`replayObservation`**（**`goldMatch`**、药尾匹配等）。
4. **`buildReplayObservation`** 复用 **`replayGoldTailForCli`** / **`replayPotionInventoryTail`**。
5. **`GET /rehearsal/patch-strategy`** 草案 JSON。
6. **`GET /health`**：**`rehearsalPatchStrategyPath`**、**`syncIssuedAtMaxSkewSec`**。
7. **`tools/check_schema_version_constants.cjs`** + **`npm run check-schema-version`**。
8. **CI** **`.github/workflows/d5-validate.yml`** 增加 schema 版本对齐步。
9. **`tools/metrics_archive_lines.cjs`** + **`npm run metrics-archive`**。
10. **Unity** **`PostSyncRoutine`**：**429** 至多 **2** 次尝试、**`WaitForSeconds(Retry-After)`**。
11. **`LastSyncRetryCount`**；**`DebugHudSimple`** **`r:`**。
12. **`syn:time`**：**400** 体含 **`issued_at`**。
13. **`.env.example`** 补充 **`SYNC_ISSUED_AT_MAX_SKEW_SEC`**。
14. **里程碑**：**replayObservation** 为观测；**PATCH** 未实装写盘逻辑。
15. **归档脚本**：追加副本，**不**破坏原 **`metrics.ndjson`**。
16. **与波 8**：**`X-Sync-Duration-Ms`** 仍返回。
17. **下一跳**：**warnings→SrvVal**、真 **PATCH**、**Redis 幂等** 仍归 **波 10 / roadmap**。
18. **文档**：**`README`**「D9 续九」、**`getting-started`** 可检索 **D9** 关键词。
19. **断网恢复**：以 **`README`** 与 **`persist_sync.cjs`** 为准。
20. **验收**：见 **「本次续写 · D9 续九」** 第 9 条。

---

## 波 10（~20 步）— 已落地（D10 续十）

1. **`GET /rehearsal/compliance-bundle`**：留存 hint、PII 说明、S3 导出约定。
2. **`LOG_REDACT_PII`** + **`redactPiiForLog`** / **`logPathForSyncSaved`**。
3. **`COMPLIANCE_*_RETENTION_HINT_DAYS`**、**`AUDIT_EXPORT_BUCKET_PREFIX`**。
4. **`tools/audit_export_bundle.cjs`**：**`manifest.json`**（**`parts[].sha256`**）。
5. **`tools/check_schema_files_snapshot.cjs`** + **`schema_files_snapshot.json`**。
6. **`npm run check-schema-snapshot`**、**`audit-export-bundle`**、**`check-locale-ci`**。
7. **CI**：**`d5-validate.yml`** 增加上述步骤。
8. **Unity**：**`AuditSummaryHudPreviewFromJson`** + **EditMode** **`AuditSummaryHudPreviewTests`**。
9. **`.env.example`**：D10 变量。
10. **`README`**「D10 续十」、**`getting-started`**、**`roadmap`** 更新。
11. **`GET /health`** 合规相关字段。
12. **里程碑**：合规为**排练**；**执法**在部署侧。
13. **与 D9**：**`replayObservation`** 等不变。
14. **Schema 变更**：需 **`--write`** 快照并提交。
15. **S3**：无云 SDK；仅本地打包与 manifest。
16. **metrics**：脱敏**不**写回 **`metrics.ndjson`**。
17. **players-index**：仅元数据，避免导出整份存档到默认 bundle。
18. **locale**：无 **`Assets/Localization`** 时 CI **skip**。
19. **验收**：**`check-schema-snapshot`**；**Test Runner** EditMode。
20. **下一跳**：roadmap 余下 **【未】**（**PATCH 写盘**、**Redis**、**warnings→SrvVal** 等）。

---

## 续波 11（D11 续十一 · ~12 步）— 已落地

1. **`SYNC_WARNING_SRVVAL_BRIDGE`** / **`WARNING_CODE_TO_SRVVAL_JSON`**。
2. **`auditSummary.srvValFromWarnings`**（**`total` / `byCategory`**）。
3. **`GET /rehearsal/warning-srvval-bridge`**。
4. **`GET /health`** 桥接相关字段。
5. **`/metrics/audit-categories`** 聚合含 **`srvValFromWarnings`**。
6. **`examples/grafana-persist-sync-minimal.json`**。
7. **`.env.example`**。
8. **里程碑**：观测桥，**非**拒收逻辑。
9. **验收**：见 **`README`**「D11 续十一」。
10. **下一跳**：**PATCH**、**Redis**、**audit↔state 深度交叉**。
11. **与波 10**：合规/脱敏 **不变**。
12. **文档**：**`README`**、**`roadmap`**、**`getting-started`**。

---

## 续波 12（D12 续十二 · ~8 步）— 已落地

1. **`SYNC_IDEMPOTENCY_PERSIST`** + **`data/idempotency-cache.json`**。
2. **`idempotencyLoadFromDisk` / `idempotencyPersistSnapshot`**。
3. **`GET /rehearsal/idempotency-persist`**。
4. **`GET /health`** 字段。
5. **单进程**；多实例 **Redis** 仍 **【未】**。
6. **验收**：重启后 **`idempotent_cache_hit`**。
7. **`.env.example`**。
8. **里程碑**：同步可靠性排练。

---

## 续波 13（D13 续十三 · ~10 步）— 已落地

1. **`POST /rehearsal/validate-patch`**。
2. **`patch_validate_rehearsal.cjs`**。
3. **`npm run validate-patch-rehearsal`** / **CI**。
4. **`GET /health`**、**`GET /rehearsal/patch-strategy`** 规则。
5. **真 PATCH 写盘** 仍 **【未】**。
6. **验收**：见 **`README`**「D13 续十三」。
7. **白名单**：**`gold` / `level` / `version` / `playerId` / `inventory/*`**。
8. **最多 64 ops**。
9. **里程碑**：排练。
10. **文档**：**`README`**、**`getting-started`**、**`roadmap`**。

---

## 续波 14（D14 续十四 · ~10 步）— 已落地

1. **`SYNC_AUDIT_STATE_STRICT`**。
2. **`audit_validate.cjs`** 升格 **errors**。
3. **`GET /rehearsal/audit-state-strict`**。
4. **`GET /health`**。
5. **默认 OFF**。
6. **验收**：见 **`README`**「D14 续十四」。
7. **里程碑**：排练门禁。
8. **与 D13**：**PATCH 排练** 独立。
9. **`.env.example`**。
10. **全玩法权威** 仍 **【未】**。

---

## 续波 15（D15 续十五 · ~10 步）— 已落地

1. **`REHEARSAL_PATCH_WRITE`**。
2. **`POST /rehearsal/apply-patch`**。
3. **`GET /rehearsal/apply-patch`**。
4. **与 `/sync`** 同门禁（**HMAC** / **限流** / **P3-2** 等）。
5. **`GET /health`**。
6. **验收**：见 **`README`**「D15 续十五」。
7. **里程碑**：排练写 **`state`**。
8. **ETag** → **D16** **【已】**。
9. **`.env.example`**。
10. **Unity 客户端** 未接 **apply-patch** / **If-Match**（仍 **【未】**）。

---

## 续波 16（D16 续十六 · ~10 步）— 已落地

1. **`ETag`**：**`GET /state`**、**`POST /sync`**、**`POST /rehearsal/apply-patch`** **200**。
2. **`If-Match`**：与磁盘存档 **SHA-256** 不一致 → **412**（**`SYNC_ETAG_DISABLED=1`** 跳过校验）。
3. **`GET /rehearsal/etag-concurrency`**。
4. **`GET /health`**：**`syncEtagIfMatchEnabled`**。
5. **幂等重放 200**：**`ETag`**（序列化体哈希）。
6. **验收**：见 **`README`**「D16 续十六」。
7. **`.env.example`**：**`SYNC_ETAG_DISABLED`**。
8. **Redis** 仍 **【未】**。
