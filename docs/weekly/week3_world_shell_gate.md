# Week3 Gate - 主城/野外与社交壳验收

## 场景挂载
- 场景推荐新增 `WorldZoneConfigSimple`（统一主城中心、半径、主城出生点、野外出生点）。
- 玩家：
  - `PlayerAreaStateSimple`（可直接绑定 `WorldZoneConfigSimple`）
  - `PlayerPvpSimple`
  - `D4FlowValidatorSimple`（流程验收器）
  - `TeamIdSimple`
  - `PartyPlaceholderSimple`
  - `ChatPlaceholderSimple`
- HUD：
  - `DebugHudSimple.areaState`、`DebugHudSimple.pvp`、`DebugHudSimple.d4Flow` 绑定玩家组件
- 场景：
  - 放一个 `SafeZoneSimple`（可直接绑定 `WorldZoneConfigSimple` 自动同步半径）
  - 如需快速切换区域，放两个传送点并挂 `AreaPortalSimple`（绑定 `WorldZoneConfigSimple`，分别设置 `toCity=true/false`）

## 验收步骤
1. 进入主城范围，HUD 显示 `A:City`。
2. 离开主城范围，HUD 显示 `A:Field`。
3. 在主城内，敌人接触不应扣血（安全区生效）。
4. 在野外，敌人接触会正常扣血。
5. 按 `K` 切换 PvP，HUD `PvP:On/Off` 同步变化。
6. 按 `=`/`-` 调整队伍占位人数（观察 Inspector 变化）。
7. 回车发送本地频道占位消息，反引号发送系统消息占位。
8. 同队（同 `teamId`）角色应可无视归属保护拾取掉落（占位规则）。
9. HUD 可看到 `Party:x/y`、`Drop:Share/Solo`、`PvP:On/Off` 与红名标记。
10. HUD 的 `Flow:...` 会按顺序推进到 `Flow Complete`。

## 通过标准
- 8 条全部通过且无阻断错误。
