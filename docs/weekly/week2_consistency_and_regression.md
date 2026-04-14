# Week2 - 数据一致性与回归固化

## 本周目标
- 统一物品 ID：`potion`、`mana`、`drop`、`shard`。
- 所有掉落/背包/存档入口都走统一规范化逻辑。
- 固化可重复执行的回归脚本（严格按顺序）。

## 一致性改动摘要
- 新增统一 ID 常量：`GameItemIdsSimple`。
- `PlayerInventorySimple`、`DropItemSimple`、`PlayerPickupSimple`、`PlayerSaveSimple` 已改为统一 ID。
- 增加 `OnValidate` 防错：非法数量、负权重、空 ID 自动纠正。

## 回归执行单（严格顺序）
1. 清档：`F11`。  
2. 打怪拿掉落，确认 `E` 可拾取。  
3. 设置掉落 `pickupId` 为 `potion`，确认 HUD `H` 增加。  
4. 设置掉落 `pickupId` 为 `mana`，确认 HUD `M` 增加。  
5. 设置掉落 `pickupId` 为 `HP_POTION`（大小写变体），确认仍归到 `H`。  
6. 设置掉落 `pickupId` 为空字符串，确认能拾取且不报错。  
7. `1` 使用红药，确认 `HP` 增、`H` 减。  
8. `2` 使用蓝药，确认 `MP` 增、`M` 减。  
9. `B/N/V` 买卖药与金币变化一致。  
10. `F5~F8` 仓库存取后，`BG/BH/BM` 与背包数值一致。  
11. `U/I` 经验分配正常，`XP/SP` 变化正确。  
12. `O/P` 解锁 Q2/R2 后技能行为增强。  
13. 死亡后掉金比例正确并复活。  
14. `F9` 保存，退出 Play 再进，`F10` 读取后数值恢复。  
15. 再次 `F11` 后 `F10` 不应恢复旧档。

## 通过标准
- 15 条全通过且无阻断报错。
- HUD 显示与行为一致，无明显错账/错档。
