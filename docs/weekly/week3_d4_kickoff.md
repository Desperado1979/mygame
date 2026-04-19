# Week3 - D4 Kickoff（世界与社交壳）

## 已落地占位
- 安全区：`SafeZoneSimple`（区域内屏蔽敌对伤害）。
- PvP 开关与红名占位：`PlayerPvpSimple`（`K` 切换）。
- 组队占位：`PartyPlaceholderSimple`（`=`/`-` 改队伍人数）。
- 聊天占位：`ChatPlaceholderSimple`（回车本地消息，反引号系统消息）。
- 掉落归属队伍共享：`DropItemSimple` 支持 `ownerTeamId`，同队可直接拾取。

## 场景接入
- 玩家建议挂：`TeamIdSimple`、`PlayerPvpSimple`、`PartyPlaceholderSimple`、`ChatPlaceholderSimple`。
- 场景放置一个安全区节点并挂 `SafeZoneSimple`。
