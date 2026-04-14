# 《破晓纪元》客户端 — 上手说明

本仓库 **Git 根目录即 Unity 工程根**（`EpochOfDawn/`：含 `Assets/`、`ProjectSettings/` 等）。产品设计规约若放在本机工作区上一级，路径以你本地布局为准。以下为在 **Windows** 上从零跑起来的步骤。

## 1. 环境要求

- **Windows 10/11**（当前工程以 Windows 为主验证）
- **Unity Hub**
- **Unity Editor 2022.3 LTS**（与工程版本一致即可，需含 **Windows Build Support**）
- 可选：**Git**（克隆或更新仓库）

## 2. 获取代码

```bash
git clone https://github.com/<你的用户名>/<仓库名>.git
cd <仓库名>
```

克隆后当前目录应为 **`EpochOfDawn`**（即本 Unity 工程根）。

> **注意：** 不要把 **`Library/`**、**`Temp/`**、**`Logs/`** 等大目录提交进 Git；本工程已配置 **`.gitignore`**。

## 3. 用 Unity 打开工程

1. 打开 **Unity Hub** → **Projects** → **Add project from disk**
2. 选择 **`EpochOfDawn`** 文件夹（内含 `Assets` 与 `ProjectSettings` 的那一层）
3. 首次打开会执行资源导入，耗时因机器而异，属正常现象

## 4. 运行游戏（编辑器内）

1. **File → Open Scene**，打开主场景（例如 **`Assets/Scenes/SampleScene`** 或团队约定场景）
2. 点击 **Play ▶**  
3. 当前切片常用操作（以工程内脚本为准）：**WASD** 移动、**空格** 普攻、**Q / R** 技能、**E** 拾取等

**D3 占位（成长与经济方向）：** 玩家可挂 **`Player Progress Simple`**（击杀得经验、升级）、**`Player Inventory Simple`**（拾取占负重）、**`Player Skill Mastery Simple`**（**q**：Q 熟练加伤害；**r**：R 熟练加长冰冻）、**`Player Wallet Simple`**（击杀掉金）、**`Player Enhance Simple`**（**T** 键扣金强化占位，`+` 为累计强化次数）。**Debug Hud Simple** 拖引用后可显示 `G`、**`q`/`r`**（小写，与 **`Q:`/`R:`** 冷却区分）、`+` 等。

## 5. 打 Windows 包（Build）

1. **File → Build Settings**
2. 平台 **PC, Mac & Linux Standalone**，**Target** 选 **Windows**
3. **Scenes In Build** 中勾选要打进的场景
4. **Build** 或 **Build And Run**，选择输出目录  
5. 运行生成的 **`.exe`** 验证

若缺模块，在 **Unity Hub** 中为该编辑器版本勾选 **Windows Build Support**。

## 6. 常见问题

| 现象 | 建议 |
|------|------|
| 克隆后体积暴涨 | 勿提交 **`Library/`**；可删本地 `Library` 后重开 Unity 重建 |
| Push 被拒 | 先 **`git pull`**（可加 **`--rebase`**）再 **`git push`** |
| 脚本不生效 | 检查 Inspector 引用（Layer、Prefab、HUD 等） |

## 7. 规约与协作（与 Git 范围约定）

- **本仓库（GitHub）里的 `README.md`**：仅作仓库说明即可，**不要求**与完整玩法规约同步，也**不必**为每个开发阶段去改它（除非你本人想写）。
- **仓库外的规约总账**：完整设计在 **`EpochOfDawn` 上一级目录** 的 **`README.MD`**（例如本机 `d:\mygame\README.MD`）。该文件**刻意不纳入本 Git 仓库**，只给你们自己看、或与本地 AI 协作用；**不要**把它 `git add` 进本工程，以免和「规约不入库」的约定冲突。
- 客户端代码与资源在 **`Assets/`**（及 `ProjectSettings/` 等）。

---

*若与仓库实际分支/场景名不一致，以当前工程为准。*
