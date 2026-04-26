# EpochOfDawn — `server/`（D5 `persist_sync` 排练服）

> 本目录**不是**早期「仅占位」的草案：主程序为 **`tools/persist_sync.cjs`**，提供 **`POST /sync`**、**`/health`**、**`/metrics/*`**、**`/rehearsal/*`** 等联调与观测能力。详细端点、环境变量、热键与 Unity 侧 **F2/F3/F4/F12** 见根路径文档。

## 快速开始

```bash
cd EpochOfDawn/server
npm ci
npm run persist
```

默认监听 **http://127.0.0.1:8787**（以启动日志与 `GET /health` 为准）。

## 文档索引（以这些为准，勿依赖本文件旧版「Next steps」列表）

| 内容 | 路径 |
|------|------|
| Unity 开工程、Build、D5 热键与 P3-2 开关说明 | `../docs/getting-started.md` |
| 风险/指标快览 | `RISK_MONITORING_QUICKSTART.md` |
| 规约、进度主表、补漏口径 | 仓库根目录 `README.MD` |
| 开发状态合并摘要（多文档归纳，非真源） | 仓库根目录 `游戏开发阶段性计划与进度.md` |
| P3–D5 扩展清单（【未】= backlog，不自动开工） | `../../游戏开发阶段性计划与进度.md`（Appendix F） |

## 常用脚本

- **`npm run persist`**：启动 `persist_sync` 排练服务。  
- **`npm run validate`**、**`npm run compare-state`**：本地校验/对照导出。  
- 其余 **`metrics-*` / `risk-*` / `audit-*`** 见 `package.json` 的 `scripts`。

## 范围说明

- **当前定位**：与 Unity 导出的 **State / Audit / Request** 做 Schema 与排练链路上的对齐，**不是**已上线的商业 MMORPG 战斗服。  
- **「登录 / 分区 / 真·生产写盘」** 若要做，应作为**新里程碑**写入根 `README.MD` 主表后再实现，避免与 D5 排练层混作一谈。
