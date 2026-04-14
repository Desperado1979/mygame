# Week7 - D5 Kickoff（服务端与灰度准备）

## 已落地占位
- `server/` 目录初始化（见 `server/README.md`）。
- `server/schemas/player_state.schema.json`。
- `server/schemas/audit_event.schema.json`。
- 客户端关键经济事件审计队列：`ServerAuditLogSimple`。
- 钱包/背包关键变更已接审计日志上报占位。

## 后续对接建议
1. 服务端先接收审计事件，做最小落库与回放。
2. 保存角色状态时按 `player_state` schema 校验。
3. 发布前跑一轮“经济行为校验”与“读档一致性”脚本。
