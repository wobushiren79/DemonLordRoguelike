---
name: ui-game
description: 游戏主UI开发：战斗界面、基地界面、世界地图、设置界面、对话框界面等所有 Game 层级的 UI。
tools: Read, Write, Edit, Glob, Grep, Bash
watched_files:
  - Assets/Scripts/Component/UI/Game/
  - Assets/Prefabs/UI/Game/
---

# 游戏 UI (Game UI) 开发代理

你负责 [Scripts/Component/UI/Game/](Assets/Scripts/Component/UI/Game/) 中所有游戏主 UI 的开发。

## 职责范围

### 主菜单 UI
- **UIMainStart** - 游戏开始界面
- **UIMainCreate** - 创建角色界面
- **UIMainLoad** - 加载存档界面
- **UIMainMaker** - 制作人员界面

### 基地 UI
- **UIBaseMain** - 基地主界面
- **UIBaseCore** - 核心建筑界面
- **UIBasePortal** - 传送门界面
- **UIBaseResearch** - 研究界面

### 战斗 UI
- **UIFightMain** - 战斗主界面
- **UIFightSettlement** - 战斗结算界面
- **UIFightAbyssalBlessing** - 深渊祝福选择

### 游戏功能 UI
- **UIGameSetting** - 游戏设置界面
- **UIGameSystem** - 游戏系统界面
- **UIGameWorldMap** - 世界地图界面
- **UIGameConversation** - 对话界面
- **UIRewardSelect** - 奖励选择界面
- **UICreatureVat** - 魔物进阶界面（CreatureVat 培养槽）

### 魔物进阶 (UICreatureVat / CreatureVat 培养槽) 要点

- **进阶效果**：目标魔物稀有度 +1，并把开始时即确定的「预定 BUFF」写入 `creatureData.dicRarityBuff[新稀有度]`（旧实现「完成」只清空槽、对生物无任何加成）。
- **目标列表**：仅 Idle 且未满级（`RarityInfoCfg.GetAscendTimeByRarity(rarity) > 0`，即排除 L）。
- **素材列表**：Idle + 排除目标 + 排除上阵（`UserDataBean.CheckIsInAnyLineup`）+ 仅保留稀有度**高于**目标的魔物；最多选 5 只（`const int MaterialMax = 5`），超出弹 Toast（文本 id 80011）。
- **预定 BUFF 生成**：开始进阶时调用 `BuffUtil.CreateAscendRarityBuff(newRarity, materials)` 得到 `ascendBuff`（素材在 newRarity 槽位的 BUFF 按 id 聚合，每 id 提供 10%×数量 命中概率，命中继承并重随机数值≥素材原值，未命中回退通用随机；UR/L 无类型则为 null）。
- **耗时**：按**源稀有度**查表 `RarityInfoCfg.GetAscendTimeByRarity`（秒）作为 `timeMax`；魔晶加速每颗 +1 秒(progress)；被动 tick 每秒 +1 秒。
- **临时进阶数据**：`UserAscendBean` / `UserAscendDetailsBean`（随存档序列化）—— `progress` 现为「已累积秒数」，`targetRarity`/`timeMax`/`ascendBuff` 字段，`AddProgress(+1秒)`、`IsComplete()`、`GetProgressNormalized()`；进度条/完成判定改用后两者。`AddAscendData(index, creatureData, targetRarity, timeMax, ascendBuff)`。
- **存档时机**：开始进阶存一次、点完成存一次；培养过程（进度 tick / 魔晶加速）不主动存档。
- **被动进度**：`GameDataHandler.HandleForAscendData` 每秒 tick 给每个进阶容器 `AddProgress()`，仅广播 `CreatureAscend_AddProgress`，不存档。
- **配置**：进阶耗时来自 `excel_rarity_info` 新列 `ascend_time`（按源稀有度：N=100/R=500/SR=2500/SSR=12500/UR=62500/L=0），手写字段在 `RarityInfoBeanPartial.cs`，访问走静态 `GetAscendTimeByRarity(int rarity)`（rarity≤0 视为 N，缺失/满级返回 0）。

### 通用 UI
- **UICommonLoading** - 通用加载界面
- **UICommonMask** - 通用遮罩
- **UIScreenLock** - 屏幕锁定

## 代码模板（普通UI）

```csharp
public class UIExample : BaseUIView
{
    public Button ui_Submit;
    public Text ui_Title;

    public override void Awake() { base.Awake(); }
    public override void OpenUI() { base.OpenUI(); }
    public override void RefreshUI(bool isOpenInit = false) { base.RefreshUI(isOpenInit); }
    public override void OnClickForButton(Button viewButton) { base.OnClickForButton(viewButton); }
}
```

## 约束

- 普通 UI 继承 BaseUIView，使用 `UI` 前缀命名
- Prefab 放置在 `Assets/Prefabs/UI/Game/`
- 新增 UI 后需更新 MD/ProjectUI.md 文档
- 按钮点击在 OnClickForButton 中统一处理
