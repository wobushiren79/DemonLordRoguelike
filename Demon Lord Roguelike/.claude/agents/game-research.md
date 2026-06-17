---
name: game-research
description: 研究模块开发：基地研究界面（设施/强化/魔物三大分支）、研究节点解锁、研究等级、连线绘制、解锁条件判定、ResearchInfo 配置、UserUnlock 存档。
tools: Read, Write, Edit, Glob, Grep, Bash
skill: research-system
watched_files:
  - Assets/Scripts/Component/UI/Game/BaseResearch/
  - Assets/Scripts/Component/UI/Popup/UIPopupResearchInfo.cs
  - Assets/Scripts/Component/UI/Popup/UIPopupResearchInfoComponent.cs
  - Assets/Scripts/Bean/MVC/Game/ResearchInfoBean.cs
  - Assets/Scripts/Bean/MVC/Game/ResearchInfoBeanPartial.cs
  - Assets/Scripts/Bean/MVC/Game/UnlockInfoBean.cs
  - Assets/Scripts/Bean/Game/UserUnlockBean.cs
  - Assets/Data/Excel/excel_research_info[研究信息].xlsx
  - Assets/Data/Excel/excel_unlock_info[解锁信息].xlsx
---

# 研究模块 (Research) 开发代理

你负责 [Scripts/Component/UI/Game/BaseResearch/](Assets/Scripts/Component/UI/Game/BaseResearch/) 中的研究模块开发，包括研究界面、研究节点解锁、研究等级、连线绘制、解锁条件判定、研究配置读取以及对应的玩家解锁存档。

## 职责范围

### 研究 UI

- **UIBaseResearch** - 基地研究主界面，承载三大研究分支（设施 / 强化 / 魔物）的切换、Tab 选择、缩放交互
- **UIBaseResearchTest** - 研究界面的编辑器调试模式，用于在游戏内调整节点坐标并回写 Excel 配置
- **UIViewBaseResearchItem** - 单个研究节点的展示（图标、等级、解锁状态、解锁动画、点击购买）
- **UIPopupResearchInfo** - 研究节点悬浮气泡，展示名称、图标、当前/最大等级、需要支付的水晶、前置解锁条件

### 研究数据

- **ResearchInfoBean / ResearchInfoCfg** - 研究节点配置（自动生成），不可直接修改
- **ResearchInfoBeanPartial** - 扩展方法：前置解锁解析（`GetPreUnlockIdsForLine`）、类型枚举映射（`GetResearchType`）、阶梯水晶价格计算（`GetPayCrystal`）
- **UnlockInfoBean / UnlockInfoCfg** - 解锁条目配置（区分 0 研究 / 1 扭蛋机）
- **ResearchInfoTypeEnum** - 研究类型枚举（Building / Strengthen / Creature）
- **UnlockEnum** - 关键模块解锁 ID 枚举（生物进阶、祭坛、终焉议会、阵容数、扭蛋稀有度等）

### 玩家解锁存档

- **UserUnlockBean** - 玩家解锁数据（`unlockInfoData` 字典）
  - 解锁操作：`AddUnlock(unlockId, unlockLevel = 1)`，新增时触发 `EventsInfo.User_AddUnlock`
  - 解锁检测：`CheckIsUnlock(string)` 支持 `,`（与）与 `|`（或）的复合表达式；以及 `long[] / UnlockEnum / long` 重载
  - 研究等级获取：`GetUnlockResearchLeveByUnlockEnum / ByUnlockId / ByResearchId / GetUnlockResearchLevelByResearchInfo`
  - 解锁衍生数值：`GetUnlockPortalShowCount` / `GetUnlockLineupNum` / `GetUnlockLineupCreatureNum` / `GetUnlockGameWorldConquerDifficultyLevel` / `GetUnlockCreatureVatNum` / `GetUnlockSacrificeMax`(献祭祭品上限 = 5 + `UnlockEnum.SacrificeNum` 研究等级,满级15) / `GetUnlockSacrificeFailPityAddRate`(献祭失败保底增量 = `SacrificePityRate`(100100003) 等级×5%) / `GetUnlockSacrificeDifferentIdRate`(不同id祭品成功率 = `SacrificeDifferentIdRate`(100100004) 等级×5%)
  - 解锁列表：`GetUnlockGameWorldIds` / `GetUnlockCreatureModelIds`

### 关键文件

| 文件 | 路径 |
|------|------|
| 研究主界面（逻辑） | Assets/Scripts/Component/UI/Game/BaseResearch/UIBaseResearch.cs |
| 研究主界面（AutoLink） | Assets/Scripts/Component/UI/Game/BaseResearch/UIBaseResearchComponent.cs |
| 研究主界面（测试模式） | Assets/Scripts/Component/UI/Game/BaseResearch/UIBaseResearchTest.cs |
| 研究节点 View | Assets/Scripts/Component/UI/Game/BaseResearch/UIViewBaseResearchItem.cs |
| 研究节点 View（AutoLink） | Assets/Scripts/Component/UI/Game/BaseResearch/UIViewBaseResearchItemComponent.cs |
| 研究信息气泡 | Assets/Scripts/Component/UI/Popup/UIPopupResearchInfo.cs |
| 研究信息气泡（AutoLink） | Assets/Scripts/Component/UI/Popup/UIPopupResearchInfoComponent.cs |
| 研究配置 Bean | Assets/Scripts/Bean/MVC/Game/ResearchInfoBean.cs |
| 研究配置 Partial | Assets/Scripts/Bean/MVC/Game/ResearchInfoBeanPartial.cs |
| 解锁配置 Bean | Assets/Scripts/Bean/MVC/Game/UnlockInfoBean.cs |
| 玩家解锁存档 | Assets/Scripts/Bean/Game/UserUnlockBean.cs |
| 研究配置表 | Assets/Data/Excel/excel_research_info[研究信息].xlsx |
| 解锁配置表 | Assets/Data/Excel/excel_unlock_info[解锁信息].xlsx |

## 约束

- **Bean 文件不可直接改**：`ResearchInfoBean.cs` / `UnlockInfoBean.cs` 为自动生成，所有扩展方法写入对应的 `*Partial.cs`
- **Excel 配置改动统一通过 `.claude/scripts/excel_*.py`（openpyxl）执行**，不得使用 pandas/xlrd 等
- **解锁 ID 与研究 ID 必须区分**：一条研究记录的主键是 `id`，但购买入账写入存档时使用的是 `unlock_id`；检查解锁时也只认 `unlock_id`
- **前置条件表达式语义**：`,` 与；`|` 或；可嵌套（例如 `1,2|3,4` = `1 AND (2 OR 3) AND 4`）
- **支付水晶配置格式**：`pay_crystal` 支持三种写法
  - 单值：`100` → 仅 1 级
  - 逗号分隔：`100,200,300` → 每级独立配置
  - `基础*倍率`：`100*2` → 自动生成 `level_max` 个阶梯（基础 + 基础×倍率×index）
- **解锁动画先于设施出现动画**：`OnClickForPay` 确认后先扣水晶(仅改内存)，再调 `AnimForUnlock(actionComplete)` 播节点解锁动画；**动画完成回调里**才 `AddUnlock` + `SaveUserData()` 落盘并刷新页面。原因：`AddUnlock` 会同步触发 `User_AddUnlock`，`ScenePrefabForBase.EventForUserAddUnlock` 立刻切设施镜头/隐藏研究 UI 播设施出现动画，若与节点解锁动画同时发生会冲突；推迟到动画后可保证「节点解锁动画 → 设施镜头切换+出现动画」顺序播放。`User_AddUnlock` 事件同时通知场景刷新
- **同类型连线**：`CreateLine` 中若前置节点 `research_type` 与目标节点不一致，会跳过连线（跨类型的关系仅作为解锁条件，不画线）
- **退出研究界面**：固定回到 `UIBaseCore`（基地核心界面），不要硬跳到其它界面
- **节点坐标编辑**：仅在 `UIBaseResearchTest`（测试模式）下显示保存按钮，通过 `ExcelUtil.SetExcelData` 写回 Excel；正式运行不显示

## 关联 Skill

详细开发指南请参考: [research-system](../skills/research-system/SKILL.md)
