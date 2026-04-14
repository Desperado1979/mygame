# Week1 - D3 收尾执行与验收

## 范围锁定
- 热键集中到 `PlayerHotkeysSimple`，禁止继续在脚本里散落硬编码。
- HUD 拆分为“运行核心信息 + 调试细节开关”（`DebugHudSimple.showDebugDetails`）。
- 存档增加版本字段（`PlayerSaveSimple` 的 `version`）。

## 场景挂载要求
- 玩家对象挂载：`PlayerHotkeysSimple`、`PlayerSaveSimple`。
- HUD 对象使用：`DebugHudSimple`，并按需要切换 `showDebugDetails`。

## 本周回归用例（15 条）
1. 移动与普攻：WASD + 空格可用。  
2. Q 技能：耗蓝、CD、生效。  
3. R 技能：耗蓝、CD、生效。  
4. 状态：燃烧 DOT 可见。  
5. 状态：冰冻控制可见。  
6. 击杀得经验：`XP` 增加。  
7. 经验分配：`U` 升级进度可涨。  
8. 经验分配：`I` 可换 `SP`。  
9. 技能解锁：`O/P` 解锁 Q2/R2。  
10. 经济：金币可增减（击杀/购买/卖药）。  
11. 背包：超重无法拾取。  
12. 仓库：F5~F8 存取可用。  
13. 死亡复活：掉金+复活可用。  
14. 存档：F9 保存后 F10 读取恢复数值。  
15. 清档：F11 后再次 F10 不应恢复旧档。

## 本周通过标准
- 连续执行 3 轮用例，无阻断错误。
- HUD 数值与系统真实状态一致。
