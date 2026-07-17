---
name: juicer-system
description: Demon Lord Roguelike 游戏的魔物回收(魔汁机/Juicer)系统开发指南。使用此SKILL当需要创建或修改魔汁机建筑出现/解锁显隐、魔汁机E键场景交互(打开UICreatureJuicer)、多选投入魔物榨汁UI(含CV_Juicer镜头/等级降序排序/投入数量研究门控/ui_LimmitText计数)、榨汁逻辑(CreatureJuicerLogic)、魔汁机研究解锁(UnlockEnum.Juicer开启/JuicerNum投入数量+1)、以及后续接入榨汁流程与奖励等，包括 ScenePrefabForBase.objBuildingJuicer/BuildingJuicerRefresh/AnimForBuildingJuicerShow、ControlInteractionEnum.JuicerInteraction、UICreatureJuicer(多选投入+Start,listSelectCreature)、CameraHandler.SetJuicerCamera、UserUnlockBean.GetUnlockJuicerCreatureMax(基础5+每级+1,满级15)、CreatureJuicerLogic.StartJuice(List,留桩)、GameHandler.StartCreatureJuicer、excel_research_info/excel_unlock_info/excel_language 配置等。注意：榨汁流程与奖励目前为留桩，后续接入 StartJuice。
watched_files:
  - Assets/Scripts/Game/Logic/CreatureJuicerLogic.cs
  - Assets/Scripts/Component/UI/Game/CreatureJuicer/
  - Assets/Scripts/Component/Game/Scene/ScenePrefabForBase.cs
  - Assets/Scripts/Component/Game/Control/ControlForGameBase.cs
  - Assets/Scripts/Component/Handler/GameHandler.cs
  - Assets/Scripts/Enums/GameStateEnum.cs
  - Assets/Resources/JsonText/ResearchInfo.txt
  - Assets/Resources/JsonText/UnlockInfo.txt
---

# 魔物回收 · 魔汁机 (Juicer) 系统开发指南

## 核心概念

**魔汁机 (Juicer)** 是基地里的「魔物回收」设施：玩家研究解锁后，基地场景出现魔汁机建筑，走近按 **E 键**打开 `UICreatureJuicer`，从背包里**选一只目标魔物**，点 **Start** 开始榨汁（把魔物"榨"成资源/奖励）。

> **术语**：面向玩家统一叫「魔汁机」，代码用 `Juicer`/`CreatureJuicer`。项目里 ScenePrefab 旧注释「榨汁机」已统一为「魔汁机」，二者同义。

### ⚠️ 当前实现状态

目前搭好**入口骨架 + 多选投入 + 镜头 + 数量门控**：`解锁 → 建筑出现 → E键交互 → 开UI(切CV_Juicer镜头)多选投入魔物(上限研究门控) → 点Start进逻辑`。
**真正的榨汁流程（魔汁机建筑动画 / 消耗投入魔物）与奖励结算尚未实现，留桩在 `CreatureJuicerLogic.StartJuice(List<CreatureBean>)`**，后续需求接入时优先在此按 TODO 补齐，不要打散到 UI 层。

### 架构选型：UI 驱动 + 轻量 Logic（仿容器 UICreatureVat，非献祭全流程）

与献祭(`CreatureSacrificeLogic` 通过 `PreGame→StartGame` 驱动开UI)不同，魔汁机是 **UI 驱动**：E键**直接打开** `UICreatureJuicer`，逻辑层 `CreatureJuicerLogic` 只负责玩家点 Start **之后**的榨汁。二者对照见 `sacrifice-system` / `gashapon-system` Skill。

## 完整链路

```
基地场景魔汁机建筑 objBuildingJuicer
  (子物体交互碰撞体命名 JuicerInteraction, 层 LayerInfo.Interaction)
  └─ 玩家靠近按 E → ControlForGameBase.HandleForUseEUp
       case ControlInteractionEnum.JuicerInteraction (=9)
       → UIHandler.OpenUIAndCloseOther<UICreatureJuicer>(ui =>
             ui.actionForExit = () => OpenUIAndCloseOther<UIBaseMain>())
            └─ UICreatureJuicer.OpenUI: 切 CV_Juicer 镜头 + 关远景 → 多选投入魔物(listSelectCreature,上限=研究门控) → 点 Start
                 → GameHandler.Instance.StartCreatureJuicer(List<CreatureBean>)
                      → CreatureJuicerLogic.StartJuice(List) //【留桩】榨汁流程/奖励后续接入
            退出(ui_ViewExit) → actionForExit() → 回 UIBaseMain(场景,基地镜头随之还原)
```

## 各环节详解

### 1. 解锁 / 研究（设施类）

- 解锁枚举：`UnlockEnum.Juicer = 100600001`（新设施块 1006，`Assets/Scripts/Enums/GameStateEnum.cs`）
- 判定解锁：
  ```csharp
  var userUnlock = GameDataHandler.Instance.manager.GetUserData().GetUserUnlockData();
  bool isUnlock = userUnlock.CheckIsUnlock(UnlockEnum.Juicer);
  ```
- 研究节点 `excel_research_info[研究信息].xlsx` → `ResearchInfo.txt`（设施类，共**两个**节点）：
  - **开启节点**：`research_type=1`、`unlock_id=name=id=100600001`、`pre_unlock_ids="100500001"`(前置=成就)、`pay_crystal="5"`、`level_max=1`、`icon_res="ui_research_65"`
  - **投入数量节点(JuicerNum)**：`id=unlock_id=name=100600002`、`pre_unlock_ids="100600001"`(前置=开启魔汁机)、`level_max=10`、`pay_crystal="1000,2000,…,10000,"`(逐级)、`icon_res="ui_research_65"`(**占位待替换专属**)、`position_x/y` 需在研究图编辑器微调避免重叠
- 投入上限计算：`UserUnlockBean.GetUnlockJuicerCreatureMax()` = `UserLimmitBean.juicerCreatureMax`(基础5) + `UnlockEnum.JuicerNum` 研究等级(每级+1,满级15)
- 解锁项 `excel_unlock_info[解锁信息].xlsx` → `UnlockInfo.txt`：`unlock_type=0`；`id=100600001`(开启魔汁机) / `id=100600002`(魔汁机投入数量+1)
- **配置改 Excel 唯一真实源，并同步 JSON .txt 让运行时即时生效**（详见 `research-system` Skill 与「Excel 读写规则」）

### 2. 建筑出现（ScenePrefabForBase，`#region 魔汁机`）

- 字段 `public GameObject objBuildingJuicer;`（已登记进 `AllBuildingShowObjs` 出现登记表 → 建造音效/整场出现据此判断）
- `BuildingJuicerRefresh()`：按 `CheckIsUnlock(UnlockEnum.Juicer)` `SetActive` 显隐建筑；建筑上的 `JuicerInteraction` 交互碰撞体随建筑显隐启用/关闭（未解锁即无交互）
- `AnimForBuildingJuicerShow(timeForShow)`：从地下(-1)升起的出现动画，复用通用 `AnimForBuildingShowItem`
- `RefreshScene()` 调 `BuildingJuicerRefresh()`；`AnimForBuildingShow()` 并入 `AnimForBuildingJuicerShow`
- **解锁即时出现**：`IsBuildingShowUnlock` 与 `EventForUserAddUnlock` 两处 switch 均加了 `case UnlockEnum.Juicer`（研究购买触发 `User_AddUnlock` 事件 → 播出现动画，研究界面下会切自定义镜头观看）
- 与祭坛/成就/终焉议会等设施同构；新增魔汁机建筑表现照此区块补

### 3. 场景交互（ControlForGameBase）

- `ControlInteractionEnum.JuicerInteraction = 9`（`GameStateEnum.cs`）
- 提示文本 textId = `2000 + (int)枚举值` = **2009** = "魔汁机"（`GetInteractionEnumName`）
- 交互物体 GameObject **必须命名 `JuicerInteraction`**：`GetInteractionEnum` 用 `Enum.TryParse` 取下划线前段与枚举名匹配
- `HandleForUseEUp` 的 `case JuicerInteraction`：直接 `OpenUIAndCloseOther<UICreatureJuicer>`，注入 `actionForExit = () => OpenUIAndCloseOther<UIBaseMain>()`（场景交互开的界面退出回场景，不回 UIBaseCore；与成就/容器同口径）

### 4. 榨汁 UI（UICreatureJuicer : BaseUIComponent）

- 组件字段（`UICreatureJuicerComponent`）：`ui_BtnStart` / `ui_BtnStartText` / `ui_UIViewBaseInfoContent` / `ui_UIViewCreatureCardList_Target` / **`ui_LimmitText`(TMP,计数文本)** / `ui_ViewExit`。`ui_LimmitText` 靠运行时 `AutoLinkUI` 按名绑定,**预制体需有同名子物体**(否则计数不显示,`RefreshLimitText` 已判空兜底)
- `actionForExit`：退出回调，由打开入口注入
- **镜头**：`OpenUI` 切 `CameraHandler.Instance.SetJuicerCamera(int.MaxValue,true)`(CV_Juicer,固定机位无需 Follow) + `VolumeHandler.SetDepthOfFieldActive(false)`;`CloseUI` 还原 DoF=true(基地镜头由返回 UIBaseMain 统一还原)
- **可投入魔物列表**：`InitCreatureData()` 取背包内 **`CreatureStateEnum.Idle` 且未上阵(`CheckIsInAnyLineup`)** 的魔物；**默认排序 `listCreatureData.Sort((a,b)=>b.level.CompareTo(a.level))` 等级降序(高→低)**
- **多选**：`listSelectCreature`(List)，`EventForCardClickSelect` 里已选则移出、未选则加入；加入前判 `GetJuicerMax()` 上限，达上限弹 Toast `61012` 拦截
- **投入上限**：`GetJuicerMax()` = `GetUnlockJuicerCreatureMax()`(基础5 + JuicerNum 研究等级,满级15)。`RefreshLimitText()` 显示「已选/上限」,`ColorUtil.WrapLimitFull` 达上限转红
- **卡片使用态复用 `CardUseStateEnum.CreatureAscendTarget` + `CardStateEnum.CreatureAscendSelect/NoSelect`**——因为预制体 `ui_UIViewCreatureCardList_Target` 的卡片变体是 `UIViewCreatureCardItemForCreatureAscend`。`OnCellChangeForTarget` 按 `listSelectCreature.Contains` 回填高亮
- 选择事件走 `EventsInfo.UIViewCreatureCardItem_OnClickSelect`（`this.RegisterEvent`，实例事件）
- Start(`OnClickForStart`)：`listSelectCreature.Count==0` → `ToastHintText(61010 "请选择要榨汁的目标魔物")` 拦截；否则 → `GameHandler.Instance.StartCreatureJuicer(listSelectCreature)`
- **Start 按钮文本** `ui_BtnStartText`（`UITextLanguageView`）：预制上 `textId = 61011`（无代码赋值）
- `OpenUI` 关基地移动控制 `GameControlHandler.Instance.SetBaseControl(false)`

### 5. 榨汁逻辑（CreatureJuicerLogic : BaseGameLogic）

- 轻量逻辑，存 `GameManager.gameLogic` 单槽，`GetGameLogic<CreatureJuicerLogic>()` 可取；**不走 PreGame/StartGame 全流程**
- 字段：`targetCreatures`（本次投入魔物 List）、`scenePrefab`（基地场景预制，榨汁动画将作用于 `scenePrefab.objBuildingJuicer`）
- `StartJuice(List<CreatureBean> targets)`：记录投入列表 + `WorldHandler.Instance.GetCurrentScenePrefab<ScenePrefabForBase>(GameSceneTypeEnum.BaseGaming)`；**当前仅 `LogUtil.Log` 打点，TODO 留桩**：
  ```
  //1.锁UI/切魔汁机镜头; 2.播 objBuildingJuicer 榨汁动画(消耗 targetCreatures);
  //3.结算奖励入账 + 存档; 4.反馈提示并返回。
  ```
- `GameHandler.StartCreatureJuicer(List<CreatureBean> targetCreatures)`：get-or-create `CreatureJuicerLogic` → `StartJuice(targetCreatures)`（与 `StartGashaponMachine` / `StartCreatureSacrifice` 同风格）

## 多语言

| 文本 | 文件(真实源=excel_language 同名工作表) | id | cn | en |
|------|------|----|----|----|
| 研究节点名(开启) | Language_ResearchInfo_cn/en | 100600001 | 开启魔汁机 | Unlock Juicer |
| 研究节点名(投入+1) | Language_ResearchInfo_cn/en | 100600002 | 魔汁机投入数量+1 | Juicer Input +1 |
| 交互提示 | Language_UIText_cn/en | 2009 | 魔汁机 | Juicer |
| 未投入提示 | Language_UIText_cn/en | 61010 | 请选择要榨汁的目标魔物 | Please select a monster to juice |
| Start 按钮 | Language_UIText_cn/en | 61011 | 开始榨汁 | Begin Juicing |
| 超上限提示 | Language_UIText_cn/en | 61012 | 最多只能投入{0}只魔物 | Up to {0} monsters can be juiced |

## 关键文件

| 文件 | 路径 |
|------|------|
| 榨汁逻辑 | Assets/Scripts/Game/Logic/CreatureJuicerLogic.cs |
| 榨汁 UI | Assets/Scripts/Component/UI/Game/CreatureJuicer/ (UICreatureJuicer + Component) |
| 逻辑入口 | Assets/Scripts/Component/Handler/GameHandler.cs (`StartCreatureJuicer`) |
| 建筑出现 | Assets/Scripts/Component/Game/Scene/ScenePrefabForBase.cs (`#region 魔汁机`) |
| E键交互 | Assets/Scripts/Component/Game/Control/ControlForGameBase.cs (`HandleForUseEUp`) |
| 枚举 | Assets/Scripts/Enums/GameStateEnum.cs (`UnlockEnum.Juicer/JuicerNum` / `ControlInteractionEnum.JuicerInteraction`) |
| 镜头 | Assets/Scripts/Component/Handler/CameraHandler.cs (`SetJuicerCamera`→CV_Juicer) |
| 投入上限 | Assets/Scripts/Bean/Game/UserUnlockBean.cs (`GetUnlockJuicerCreatureMax`) · UserLimmitBean.cs (`juicerCreatureMax`) |
| 研究/解锁 | excel_research_info(100600001/100600002) · excel_unlock_info · ResearchInfo.txt · UnlockInfo.txt |
| 多语言 | excel_language · Language_ResearchInfo_cn/en · Language_UIText_cn/en |

## 约束与注意

- **榨汁流程/奖励统一收进 `CreatureJuicerLogic.StartJuice(List)`** 后续接入，别散到 UI。
- 建筑显隐**唯一门控** `UnlockEnum.Juicer`；交互碰撞体命名固定 `JuicerInteraction`。
- **投入数量上限**门控 `UnlockEnum.JuicerNum`(基础5+每级+1,满级15)；改基础值改 `UserLimmitBean.juicerCreatureMax`。
- **镜头 CV_Juicer** 固定机位(无 Follow,同扭蛋机)；GameObject 由用户在场景 CV_List 手建；返回 UIBaseMain 自动还原基地镜头。
- UI 驱动：E键直接开 UICreatureJuicer（不像献祭那样 Logic 驱动开UI）。**多选**投入(`listSelectCreature`)；默认按等级降序、已排除上阵/非Idle。
- 卡片态**复用 CreatureAscend 系列**；计数文本 `ui_LimmitText`(TMP,AutoLinkUI 按名绑定,预制需同名子物体)。
- 配置改 **Excel 唯一真实源**并同步 JSON；自动生成 Bean/JSON 不手改结构（Bean 扩展写 Partial）。
- UI 继承 `BaseUIComponent`；输入走 `InputActionUIEnum`（禁用旧版 Input API）。
- 研究 `icon_res` 目前占位（`ui_research_65`），需专属图标时**先征得用户同意再用 PixelLab**。
- 预制体（objBuildingJuicer 交互碰撞体、CV_Juicer 镜头、UICreatureJuicer 接线含 `ui_LimmitText`）由用户手动接好；改动涉及预制体时优先走 Unity MCP 或提示用户手工处理。
