---
name: game-juicer
description: 魔物回收(魔汁机/Juicer)系统开发：基地魔汁机建筑(ScenePrefabForBase.objBuildingJuicer 出现/解锁显隐)、E键场景交互(ControlInteractionEnum.JuicerInteraction 打开 UICreatureJuicer)、多选投入魔物榨汁UI(UICreatureJuicer 多选 listSelectCreature+Start，含 CV_Juicer 镜头/等级降序排序/投入数量门控/ui_LimmitText 计数)、榨汁逻辑(CreatureJuicerLogic 经 GameHandler.StartCreatureJuicer(List) 驱动，UI驱动+轻量Logic)、魔汁机镜头(CameraHandler.SetJuicerCamera→CV_Juicer)、投入数量上限(UserUnlockBean.GetUnlockJuicerCreatureMax=UserLimmitBean.juicerCreatureMax 基础5+UnlockEnum.JuicerNum 每级+1 满级15)、魔汁机研究解锁(UnlockEnum.Juicer=100600001 开启/JuicerNum=100600002 投入数量+1，excel_research_info/excel_unlock_info)。注意：榨汁流程(建筑动画/消耗魔物)与奖励结算目前为留桩，后续接入 CreatureJuicerLogic.StartJuice。
tools: Read, Write, Edit, Glob, Grep, Bash
watched_files:
  - Assets/Scripts/Game/Logic/CreatureJuicerLogic.cs
  - Assets/Scripts/Component/UI/Game/CreatureJuicer/
  - Assets/Scripts/Component/Game/Scene/ScenePrefabForBase.cs
  - Assets/Scripts/Component/Game/Control/ControlForGameBase.cs
  - Assets/Scripts/Component/Handler/GameHandler.cs
  - Assets/Scripts/Component/Handler/CameraHandler.cs
  - Assets/Scripts/Bean/Game/UserUnlockBean.cs
  - Assets/Scripts/Bean/Game/UserLimmitBean.cs
  - Assets/Scripts/Enums/GameStateEnum.cs
  - Assets/Resources/JsonText/ResearchInfo.txt
  - Assets/Resources/JsonText/UnlockInfo.txt
---

# 魔物回收 · 魔汁机 (Juicer) 系统开发代理

你负责 [Scripts/](Assets/Scripts/) 中与「魔汁机(魔物回收)」相关的代码开发。详细机制见 `juicer-system` Skill。

> **术语**：面向玩家统一叫「魔汁机」，代码用 `Juicer`/`CreatureJuicer`。（ScenePrefab 里旧「榨汁机」注释已统一为「魔汁机」。）

## 当前状态（重要）

魔汁机目前搭好**入口骨架 + 多选投入 + 镜头 + 数量门控**：解锁 → 建筑出现 → E键交互 → 开UI(切 CV_Juicer 镜头)多选投入魔物(上限研究门控) → 点 Start 进逻辑。
**真正的榨汁流程（建筑动画 / 消耗投入魔物）与奖励结算尚未实现，留桩在 `CreatureJuicerLogic.StartJuice(List)`**，由后续需求接入。改到这里时优先在 `StartJuice` 里按 TODO 补，不要打散到别处。

## 核心链路（UI 驱动 + 轻量 Logic，仿容器 UICreatureVat 而非献祭全流程）

```
基地场景魔汁机建筑(objBuildingJuicer, 子物体交互碰撞体命名 JuicerInteraction, 层 LayerInfo.Interaction)
  └─ 玩家靠近按 E → ControlForGameBase.HandleForUseEUp
       case ControlInteractionEnum.JuicerInteraction
       → UIHandler.OpenUIAndCloseOther<UICreatureJuicer>(ui => ui.actionForExit = ()=>Open UIBaseMain)
            └─ UICreatureJuicer.OpenUI: 切 CV_Juicer 镜头 + 关远景 → 多选投入魔物(listSelectCreature,上限门控) → 点 Start
                 → GameHandler.StartCreatureJuicer(List<CreatureBean>)
                      → CreatureJuicerLogic.StartJuice(List)  //【留桩】榨汁流程/奖励后续接入
            退出(ui_ViewExit) → actionForExit() → 回 UIBaseMain(基地镜头随之还原)
```

## 职责范围

### 建筑出现（ScenePrefabForBase）
- 字段 `objBuildingJuicer`（魔汁机建筑，已登记进 `AllBuildingShowObjs` 出现登记表）
- `BuildingJuicerRefresh()`：按 `userUnlock.CheckIsUnlock(UnlockEnum.Juicer)` 显隐建筑（建筑上的 JuicerInteraction 交互碰撞体随之启用/关闭）
- `AnimForBuildingJuicerShow(timeForShow)`：从地下升起的出现动画（复用 `AnimForBuildingShowItem`）
- `RefreshScene()` 调 `BuildingJuicerRefresh()`；`AnimForBuildingShow()` 并入 `AnimForBuildingJuicerShow`
- 解锁即时出现：`IsBuildingShowUnlock` 与 `EventForUserAddUnlock` 的 switch 均已加 `case UnlockEnum.Juicer`（研究购买后触发出现动画）
- 与祭坛/成就/终焉议会等设施同构，新增建筑相关表现照此区块（`#region 魔汁机`）补

### 场景交互（ControlForGameBase）
- `ControlInteractionEnum.JuicerInteraction = 9`（提示文本 textId = 2000+9 = 2009 = "魔汁机"）
- 交互物体命名必须为 `JuicerInteraction`（`GetInteractionEnum` 用 `Enum.TryParse` 取下划线前段匹配）
- `HandleForUseEUp` 的 `case JuicerInteraction` 直接 `OpenUIAndCloseOther<UICreatureJuicer>`，并注入 `actionForExit = ()=>OpenUIAndCloseOther<UIBaseMain>()`（场景交互开的界面退出回场景，不回 UIBaseCore；与成就/容器同口径）

### 榨汁 UI（UICreatureJuicer : BaseUIComponent）
- 组件字段（`UICreatureJuicerComponent`）：`ui_BtnStart`/`ui_BtnStartText`/`ui_UIViewBaseInfoContent`/`ui_UIViewCreatureCardList_Target`/**`ui_LimmitText`(TMP,计数)**/`ui_ViewExit`。`ui_LimmitText` 靠 `AutoLinkUI` 按名绑定,**预制体需有同名子物体**
- `actionForExit`：退出回调，由打开入口注入
- **镜头**：`OpenUI` 切 `CameraHandler.Instance.SetJuicerCamera(int.MaxValue,true)`(CV_Juicer,固定机位无 Follow) + `VolumeHandler.SetDepthOfFieldActive(false)`；`CloseUI` 还原 DoF=true(基地镜头由返回 UIBaseMain 统一还原)
- 可投入魔物列表：背包内**空闲态(CreatureStateEnum.Idle)且未上阵(CheckIsInAnyLineup)** 的魔物；**默认按等级降序排序**(`listCreatureData.Sort((a,b)=>b.level.CompareTo(a.level))`)
- **多选**（`listSelectCreature` List，再次点已选则移出；加入前判 `GetJuicerMax()` 上限，达上限弹 Toast 61012 拦截）；`OnCellChangeForTarget` 按 `Contains` 回填高亮
- **投入上限**：`GetJuicerMax()`=`GetUnlockJuicerCreatureMax()`(基础5+JuicerNum 研究等级,满级15)；`RefreshLimitText()` 显示「已选/上限」,`ColorUtil.WrapLimitFull` 达上限转红
- **卡片使用态复用 `CardUseStateEnum.CreatureAscendTarget` + `CardStateEnum.CreatureAscendSelect/NoSelect`**——预制体 `ui_UIViewCreatureCardList_Target` 挂的卡片变体是 `UIViewCreatureCardItemForCreatureAscend`
- 选择走 `EventsInfo.UIViewCreatureCardItem_OnClickSelect` 事件（`this.RegisterEvent`）
- Start：`listSelectCreature.Count==0` → `ToastHintText(61010)` 拦截；否则 → `GameHandler.Instance.StartCreatureJuicer(listSelectCreature)`

### 榨汁逻辑（CreatureJuicerLogic : BaseGameLogic）
- 轻量逻辑，存 `GameManager.gameLogic` 单槽（`GetGameLogic<CreatureJuicerLogic>()` 可取）；**不走 PreGame/StartGame 全流程**，由 UI 的 Start 经 `GameHandler.StartCreatureJuicer` 直接 `StartJuice`
- 字段：`targetCreatures`（本次投入魔物 List）、`scenePrefab`（基地场景预制，榨汁动画作用于 `scenePrefab.objBuildingJuicer`）
- `StartJuice(List<CreatureBean> targets)`：记录投入列表 + 抓 `WorldHandler.GetCurrentScenePrefab<ScenePrefabForBase>(BaseGaming)`；**当前仅 LogUtil 打点，TODO 留桩**（关UI/切镜头 → objBuildingJuicer 榨汁动画消耗 targetCreatures → 奖励入账+存档 → 反馈返回）
- `GameHandler.StartCreatureJuicer(List<CreatureBean>)`：get-or-create logic → `StartJuice(list)`（与 `StartGashaponMachine`/`StartCreatureSacrifice` 同风格）

### 镜头（CameraHandler）
- `SetJuicerCamera(int priority, bool isEnable)` → `SetCameraForBaseScene(..., "CV_Juicer")`（固定机位，无 Follow，同扭蛋机 `SetGashaponMachineCamera`）
- CV_Juicer 的 GameObject 由用户在基地场景 `CV_List` 手建；返回 UIBaseMain 时 `SetCameraForControl(Base)` 自动还原

### 投入数量上限（研究门控）
- `UserLimmitBean.juicerCreatureMax = 5`（基础值）
- `UserUnlockBean.GetUnlockJuicerCreatureMax()` = `juicerCreatureMax` + `GetUnlockResearchLeveByUnlockEnum(UnlockEnum.JuicerNum)`（每级+1，level_max=10，满级15；同献祭/进阶素材口径）

### 解锁 / 研究（设施类，两个节点）
- `UnlockEnum.Juicer = 100600001`(开启) / `UnlockEnum.JuicerNum = 100600002`(投入数量+1)（新设施块 1006，`GameStateEnum.cs`）
- 研究节点 `excel_research_info[研究信息].xlsx` + `ResearchInfo.txt`：
  - 开启：`research_type=1`、`id=100600001`、`pre_unlock_ids="100500001"`(前置=成就)、`pay_crystal="5"`、`level_max=1`、`icon_res="ui_research_65"`
  - 投入数量：`id=100600002`、`pre_unlock_ids="100600001"`、`level_max=10`、`pay_crystal="1000,…,10000,"`(逐级)、`icon_res="ui_research_65"`(**占位待替换**)、`position` 需在研究图编辑器微调
- 解锁项 `excel_unlock_info` + `UnlockInfo.txt`：`unlock_type=0`；`id=100600001`/`100600002`
- 多语言：研究名 `Language_ResearchInfo_cn/en`(100600001 开启魔汁机 / 100600002 魔汁机投入数量+1)；UI 文本 `Language_UIText_cn/en`(2009 魔汁机 / 61010 未投入提示 / 61011 开始榨汁 / 61012 超上限提示)。**真实源是 excel_language 同名工作表**
- 判定解锁：`userData.GetUserUnlockData().CheckIsUnlock(UnlockEnum.Juicer)`

### 关键文件

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

## 约束

- **UI 驱动**：E键直接开 UICreatureJuicer（不像献祭那样 Logic.PreGame 驱动开UI）；Logic 只负责 Start 之后的榨汁。
- 榨汁流程/奖励**统一收进 `CreatureJuicerLogic.StartJuice(List)`** 后续接入，别散落到 UI。
- 建筑显隐**唯一门控** `UnlockEnum.Juicer`；**投入数量上限**门控 `UnlockEnum.JuicerNum`(基础5+每级+1,满级15,改基础值改 `UserLimmitBean.juicerCreatureMax`)；交互碰撞体命名固定 `JuicerInteraction`。
- **镜头 CV_Juicer** 固定机位(无 Follow,同扭蛋机)；GameObject 由用户在场景 CV_List 手建；返回 UIBaseMain 自动还原基地镜头。
- **多选**投入(`listSelectCreature`)，默认等级降序、已排除上阵/非Idle；计数文本 `ui_LimmitText`(AutoLinkUI 按名绑定,预制需同名子物体)。
- 目标列表卡片态**复用 CreatureAscend 系列**（预制体已如此接线），改卡片变体前先确认预制体挂的脚本。
- 配置改 **Excel 唯一真实源**，同步 JSON 让运行时即时生效；自动生成 Bean/JSON 不手改结构。
- UI 继承 `BaseUIComponent`；输入走 `InputActionUIEnum`（禁用旧版 Input）。
- 研究 `icon_res` 目前是占位（`ui_research_65`），需要专属魔汁机研究图标时**先征得用户同意再用 PixelLab**。
