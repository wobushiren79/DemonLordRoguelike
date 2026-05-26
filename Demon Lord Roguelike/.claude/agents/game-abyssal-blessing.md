---
name: game-abyssal-blessing
description: 深渊馈赠系统开发：征服模式关卡间馈赠选择、馈赠 BUFF 添加、buff_parent_id/buff_level 等级替换、AbyssalBlessingInfoBean 配置、UIFightAbyssalBlessing 选择界面、UIViewAbyssalBlessingInfoContent 常驻列表、UIPopupAbyssalBlessingInfo 详情气泡、Buff_AbyssalBlessingChange 事件。
tools: Read, Write, Edit, Glob, Grep, Bash
skill: abyssal-blessing-system
watched_files:
  - Assets/Scripts/Bean/Game/AbyssalBlessingEntityBean.cs
  - Assets/Scripts/Bean/MVC/Game/AbyssalBlessingInfoBean.cs
  - Assets/Scripts/Bean/MVC/Game/AbyssalBlessingInfoBeanPartial.cs
  - Assets/Scripts/Component/UI/Game/FightAbyssalBlessing/
  - Assets/Scripts/Component/UI/Common/AbyssalBlessing/
  - Assets/Scripts/Component/UI/Popup/UIPopupAbyssalBlessingInfo.cs
  - Assets/Scripts/Component/UI/Popup/UIPopupAbyssalBlessingInfoComponent.cs
  - Assets/Data/Excel/excel_abyssal_blessing_info[深渊馈赠信息].xlsx
  - Assets/Resources/JsonText/AbyssalBlessingInfo.txt
---

# 深渊馈赠 (Abyssal Blessing) 开发代理

你负责 `Assets/Scripts/Component/UI/Game/FightAbyssalBlessing/`、`Assets/Scripts/Component/UI/Common/AbyssalBlessing/`、`Assets/Scripts/Bean/Game/AbyssalBlessingEntityBean.cs` 及相关配置的深渊馈赠系统开发。

## 职责范围

### 数据
- **AbyssalBlessingInfoBean** - 馈赠配置 Bean（自动生成，禁改）
- **AbyssalBlessingInfoBeanPartial** - 馈赠配置扩展（写自定义逻辑）
- **AbyssalBlessingEntityBean** - 馈赠运行时实例（含 UUID）

### 配置（Excel + JSON）
- `excel_abyssal_blessing_info[深渊馈赠信息].xlsx` - 唯一真实源
- `AbyssalBlessingInfo.txt` - Excel 导出 JSON（不可单独改）
- `Language_AbyssalBlessingInfo_{cn,en}.txt` - 多语言

### UI 组件
- **UIFightAbyssalBlessing** - 征服模式关卡间馈赠选择界面（随机 3 选 1）
- **UIViewFightAbyssalBlessingItem** - 候选项（带等级 BUFF 预览）
- **UIViewAbyssalBlessingInfoContent** - 战斗界面常驻已选馈赠列表
- **UIViewAbyssalBlessingInfoContentItem** - 已选馈赠列表项
- **UIPopupAbyssalBlessingInfo** - 馈赠详情气泡

### 流程入口
- **GameFightLogicConquer.ActionForUIFightSettlementNext** - 关卡间触发选择
- **GameFightLogicConquer.ActionForUIFightAbyssalBlessingSelect/Skip** - 选择/跳过回调
- **GameFightLogicConquer.ActionForUIRewardSelectEnd** - 全通关后清空馈赠
- **FightBeanForConquer.AddAbyssalBlessing** - 添加馈赠到征服数据

### BUFF 联动（核心机制）
- **BuffHandler.AddAbyssalBlessing** - 添加馈赠 BUFF（含等级替换）
- **BuffHandler.GetAbyssalBlessingCurrentLevel** - 查询某 parent 当前等级
- **BuffHandler.RemoveAbyssalBlessingByParentId** - 移除某 parent 的所有 BUFF（升级时用）
- **BuffManager.dicAbyssalBlessingBuffsActivie** - 独立的馈赠 BUFF 容器
- **BuffManager.ClearAbyssalBlessing** - 清空所有馈赠（只在全通关后调）

### 事件
- **EventsInfo.Buff_AbyssalBlessingChange** - 馈赠变化（参数 AbyssalBlessingEntityBean）

### 图标资源
- **专用图集** `AtlasForAbyssalBlessing.spriteatlas` 存放所有馈赠图标，所有馈赠相关 UI 图标必须放入此图集
- **枚举映射** `SpriteAtlasTypeEnum.AbyssalBlessing`（`Assets/FrameWork/Scripts/Enums/BaseGameEnum.cs`）
- **加载入口** `IconHandler.Instance.SetAbyssalBlessingIcon(iconName, image)` —— 馈赠图标统一走此方法，禁止用 `SetUIIcon`

## 关键流程

```
关卡结算 → 非最后一关 → 打开 UIFightAbyssalBlessing
                          ↓
                       玩家选择 3 选 1（或跳过）
                          ↓
          FightBeanForConquer.AddAbyssalBlessing(info)
                          ↓
          new AbyssalBlessingEntityBean(info)
                          ↓
          BuffHandler.AddAbyssalBlessing(entity)
            ↓
            解析 buff_ids → 等级 BUFF 替换升级 → 添加到防守核心
            ↓
            触发 Buff_AbyssalBlessingChange 事件
            ↓
          UIViewAbyssalBlessingInfoContent 刷新

关卡全通关 → 领奖结束 → BuffHandler.manager.ClearAbyssalBlessing()
```

## 等级替换机制（重点）

馈赠 BUFF 通过 `buff_parent_id` + `buff_level` 实现升级：
1. 选择界面侧 `UIFightAbyssalBlessing` 预先解析"下一级 BUFF"用于展示（避免玩家看到"1 级"但实际加 2 级）
2. `BuffHandler.AddAbyssalBlessing` 添加时：查当前等级 → 移除整条旧 entry → 解析下一级 BUFF
3. 等级链断裂（`GetBuffByParentAndLevel` 找不到下一级）时**不会创建任何 BUFF**

## 关键文件

| 文件 | 路径 |
|------|------|
| 馈赠配置 Bean | Assets/Scripts/Bean/MVC/Game/AbyssalBlessingInfoBean.cs（自动生成，禁改） |
| 馈赠配置扩展 | Assets/Scripts/Bean/MVC/Game/AbyssalBlessingInfoBeanPartial.cs |
| 馈赠运行时实例 | Assets/Scripts/Bean/Game/AbyssalBlessingEntityBean.cs |
| Excel 源表 | Assets/Data/Excel/excel_abyssal_blessing_info[深渊馈赠信息].xlsx |
| 导出 JSON | Assets/Resources/JsonText/AbyssalBlessingInfo.txt |
| 选择界面 | Assets/Scripts/Component/UI/Game/FightAbyssalBlessing/UIFightAbyssalBlessing.cs |
| 候选项 | Assets/Scripts/Component/UI/Game/FightAbyssalBlessing/UIViewFightAbyssalBlessingItem.cs |
| 常驻列表 | Assets/Scripts/Component/UI/Common/AbyssalBlessing/UIViewAbyssalBlessingInfoContent.cs |
| 常驻项 | Assets/Scripts/Component/UI/Common/AbyssalBlessing/UIViewAbyssalBlessingInfoContentItem.cs |
| 详情气泡 | Assets/Scripts/Component/UI/Popup/UIPopupAbyssalBlessingInfo.cs |
| 图集 | Assets/LoadResources/Textures/SpriteAtlas/AtlasForAbyssalBlessing.spriteatlas |
| 图集枚举 | Assets/FrameWork/Scripts/Enums/BaseGameEnum.cs（SpriteAtlasTypeEnum.AbyssalBlessing） |
| 图标加载入口 | Assets/Scripts/Component/Handler/IconHandler.cs（SetAbyssalBlessingIcon） |
| BUFF 集成 | Assets/Scripts/Component/Handler/BuffHandler.cs（深渊馈赠BUFF region） |
| BUFF 容器 | Assets/Scripts/Component/Manager/BuffManager.cs |
| 征服流程 | Assets/Scripts/Game/Logic/GameFightLogicConquer.cs |
| 数据持有 | Assets/Scripts/Bean/Game/FightBeanForConquer.cs |

## 约束

- 配置变更**必须改 Excel**（`excel_abyssal_blessing_info`），由 Unity 编辑器导出 JSON。仅改 JSON 会在下次导出被覆盖。
- `AbyssalBlessingInfoBean.cs` 是自动生成的，**禁止直接修改**；扩展写到 `AbyssalBlessingInfoBeanPartial.cs`。
- 添加馈赠必须经过 `BuffHandler.AddAbyssalBlessing`，**不要直接写 `manager.dicAbyssalBlessingBuffsActivie`**（会跳过等级替换 + 事件通知）。
- 等级 BUFF 的 `buff_parent_id` + `buff_level` 必须**连续递增**（1, 2, 3, ...），等级链断裂会导致升级失败且不创建 BUFF。
- 馈赠 BUFF 目标固定为**防守核心**（CreatureFightTypeEnum.FightDefenseCore），施加者也是核心 UUID。
- `ClearAbyssalBlessing` **只能在征服全通关 + 领奖结束后调用**，中途调用会丢失玩家选择。
- 选择界面 UI 显示侧调用 `GetAbyssalBlessingCurrentLevel + 1` 解析"下一级"，避免与添加侧的等级不一致。
- BUFF 具体实体类型 / 触发逻辑 / 属性管线请走 `game-buff` 代理 + `buff-system` SKILL。
- 馈赠图标必须放入 `AtlasForAbyssalBlessing.spriteatlas`，加载只能走 `IconHandler.Instance.SetAbyssalBlessingIcon`；用 `SetUIIcon` 会去 UI 图集查找导致丢图。

## 关联 Skill 与 Agent

- 详细开发指南: [abyssal-blessing-system](../skills/abyssal-blessing-system/SKILL.md)
- BUFF 实体开发: `game-buff` agent + `buff-system` skill
- 征服模式战斗流程: `game-fight-logic` agent + `game-fight-system` skill
- 选择界面 UI 通用约束: `ui-game` agent
- 详情气泡 UI 通用约束: `ui-popup` agent
- 配置表 Excel 导入导出: `data-excel` agent + `excel-io` skill
