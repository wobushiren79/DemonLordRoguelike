---
name: portal-system
description: Demon Lord Roguelike 游戏的传送门(Portal)系统开发指南。使用此SKILL当需要创建或修改基地地图的传送门世界选择/生成、传送门随机数据(世界类型随机 Conquer/Infinite、难度、道路数/道路长度/关卡数预生成、奖励预生成)、传送门数量(研究 PortalShowNum)、地图刷新/位置避重叠/行星贴图、悬停详情气泡(UIPopupPortalDetails 四项预览+奖励, 受设施研究门控)、进入确认与难度选择对话框(UIDialogPortalDetails 左右滑动)、点击进入世界→生成 FightBean→进战斗场景等，包括 UIBasePortal、UIViewBasePortalItem、UIDialogPortalDetails、UIPopupPortalDetails、GameWorldInfoBean、GameWorldInfoRandomBean/GameWorldDifficultyRandomBean、excel_game_world_info。注意：进入后的征服战斗逻辑见 conquer-system，奖励生成规则见 fight-reward-system，研究节点/解锁机制见 research-system。
watched_files:
  - Assets/Scripts/Component/UI/Game/BasePortal/UIBasePortal.cs
  - Assets/Scripts/Component/UI/Game/BasePortal/UIViewBasePortalItem.cs
  - Assets/Scripts/Component/UI/Dialog/UIDialogPortalDetails.cs
  - Assets/Scripts/Component/UI/Dialog/PortalDetails/UIViewDialogPortalDetailsItem.cs
  - Assets/Scripts/Component/UI/Popup/UIPopupPortalDetails.cs
  - Assets/Scripts/Component/UI/Popup/PortalDetails/UIViewPopupPortalDetailsItem.cs
  - Assets/Scripts/Bean/MVC/Game/GameWorldInfoBean.cs
  - Assets/Scripts/Bean/MVC/Game/GameWorldInfoBeanPartial.cs
  - Assets/Data/Excel/excel_game_world_info[游戏世界信息].xlsx
  - Assets/Resources/JsonText/GameWorldInfo.txt
---

# 传送门 (Portal) 系统开发指南

## 核心概念

传送门系统是**基地地图的"世界选择/进入"层**——玩家在基地地图上看到若干随机生成的传送门(每个=一个世界实例)，悬停看详情、点击选难度并进入对应战斗模式。它**只负责"选哪局、进哪局"**，进入后的战斗由 `conquer-system`(征服) / `game-fight-logic`(无尽等) 接管。

- **传送门 = 一个世界随机实例**：`GameWorldInfoRandomBean`（运行时随机数据），引用一张世界配置 `GameWorldInfoBean`。
- **两种世界类型**：开局随机 `GameFightTypeEnum.Conquer`(默认) 或 `Infinite`(无尽，需 `unlock_id_infinite` 解锁)，见 `SetGameFightTypeRandom`。
- **数量受研究控制**：地图上传送门个数 = `UserUnlockBean.GetUnlockPortalShowCount()`(基础 `portalShowMax` + 研究 `UnlockEnum.PortalShowNum` 等级)。
- **数据缓存在临时存档**：`UserTempBean.listPortalWorldInfoRandomData`，打开地图不重新洗牌，只按数量缺口补足；点"刷新"（需研究 `PortalRefreshNum` 解锁、有次数限制、通关回满）才清空重洗。

### 系统架构

```
UIBasePortal (基地地图, BaseUIComponent)
    │  OpenUI→SetBasePortalCamera + InitMap()
    │  InitMap：先显示已缓存世界(listPortalWorldInfoRandomData)，再按 GetUnlockPortalShowCount() 缺口补足
    │  CreateRandomPortalWorld：随机已解锁世界id(GetUnlockGameWorldIds) → SetGameFightTypeRandom → 随机地图位置(避重叠) → iconSeed
    ▼
UIViewBasePortalItem (单个传送门, BaseUIView)
    │  行星贴图(iconSeed)/缓慢绕中心旋转/出现动画
    │  悬停 ui_BG → PopupEnum.PortalDetails → UIPopupPortalDetails(详情气泡)
    │  点击 ui_BG → OnClickForEnterWorld
    ▼
OnClickForEnterWorld
    │  确认对话框(文本401) + 难度选择 UIDialogPortalDetails
    │  确认 → ShowMask → 按 gameFightType 造 FightBeanForConquer / FightBeanForInfinite
    ▼
WorldHandler.EnterGameForFightScene(fightData)  → 进入战斗(交给 conquer-system / game-fight-logic)
```

## 数据模型

### GameWorldInfoBean（世界配置, 自动生成, 禁改）
`Assets/Scripts/Bean/MVC/Game/GameWorldInfoBean.cs`（Cfg.fileName = `GameWorldInfo`）

| 字段 | 含义 |
|------|------|
| `icon_res` | 图标资源名(为空则用 iconSeed 程序化生成行星贴图) |
| `unlock_id` | 世界本身解锁id |
| `unlock_id_infinite` | 无尽模式解锁id(解锁后该世界随机类型池才含 Infinite) |
| `unlock_id_conquer_difficulty_level` | 征服难度起始解锁id |
| `unlock_id_quick_attack` | 加快进攻节奏(Quick)研究起始解锁id(0=该世界无此类研究, 实际id=起始id+难度-1) |
| `unlock_id_speed2` | 2倍速游戏研究起始解锁id(0=该世界无此类研究, 实际id=起始id+难度-1) |
| `map_pos` | 地图坐标(配置) |
| `name` | 名字多语言id(`name_language` 带缓存) |

### GameWorldInfoRandomBean（传送门运行时随机数据, 手写, 可改）
`Assets/Scripts/Bean/MVC/Game/GameWorldInfoBeanPartial.cs` — 一个传送门一份，存 `UserTempBean.listPortalWorldInfoRandomData`。

- 字段：`worldId`、`gameFightType`、`roadNum`/`roadLength`/`fightNum`(当前难度的值)、`uiPosition`(Vector2Bean 规避 Newtonsoft 递归)、`iconSeed`、`difficultyLevel`、`listDifficultyRandom`(各难度预生成数据)。
- `SetGameFightTypeRandom(worldId)`：随机世界类型(Conquer/已解锁则可 Infinite) → 调 `SetRandomData`。
- `SetRandomDataForConquer`：创建时把 `1~已解锁最高难度` 逐档随机出 `GameWorldDifficultyRandomBean`(道路数/长度/关卡数 + **预生成奖励**)缓存进 `listDifficultyRandom`，默认难度取已解锁最高。
- `SetRandomDataForInfinite`：无尽模式只随 `roadNum`。
- `SetDifficultyLevel(level)`：切换难度，把 `roadNum/roadLength/fightNum` 同步为该难度缓存值(气泡、战斗都读这些字段)。
- `GetDifficultyRandom(level)`：取某难度数据(缺失懒生成并缓存)。
- `GetDifficultyReward(level)`：取该难度**预生成奖励**(预览即实领)；空或解锁池签名变化时重新生成，见下。

### GameWorldDifficultyRandomBean（单难度随机数据）
每难度一份：`difficultyLevel`、`roadNum`、`roadLength`、`fightNum`、`listReward`(预生成通关奖励)、`rewardUnlockSign`(生成时的装备奖励池解锁签名)。

## 地图生成与刷新（UIBasePortal）

- **不洗牌补足**：`InitMap` 先显示 `listPortalWorldInfoRandomData` 已缓存世界，再 `for cachedCount..showCount` 用 `CreateRandomPortalWorld` 补足差额并 `AddPortalWorldInfoRandomData`。解锁研究提高数量后，旧世界不动、只新增——避免每次打开重洗。
- **刷新（研究解锁 + 次数限制 + 通关回满）**：默认关闭——设施研究 `UnlockEnum.PortalRefreshNum`(100300006) 未解锁时 `ui_BtnRefresh` 整个隐藏（`RefreshBtnRefreshState` 在 `InitMap` 末尾按 `CheckIsUnlockPortalRefresh` 控制）。已解锁时 `ui_BtnRefreshNum` 显示剩余次数(形如 `x5`)；`OnClickForRefresh` 仅剩余>0 才 `ReducePortalRefreshNum`+`ClearPortalWorldInfoRandomData`+`ClearMap`+`InitMap`(全部重随)+`SaveUserData`，剩余=0 弹提示(UI文本 id 2007「刷新次数已用完」)不刷新。
- **刷新次数账本（"已用次数"模型）**：剩余 = 刷新研究等级(上限，每级+1，满级10) − `UserTempBean.portalRefreshUsedNum`(已用)；存取 `GetPortalRefreshRemainNum`/`ReducePortalRefreshNum`(已用+1)/`RefillPortalRefreshNum`(已用归0回满)。研究升级使上限+1时剩余自动+1，无需特判研究购买流程。
- **通关后：刷新次数回满 + 世界全量重洗**：通关一个世界 `GameFightLogicConquer.ActionForUIRewardSelectEnd` 同时调 `RefillPortalRefreshNum`(次数回满) 与 `ClearPortalWorldInfoRandomData`(清空整个世界列表) → 下次打开传送门UI `InitMap` 缓存为空、全量重新生成全部世界(随 `EndGameAndReturnToBase` 的 `SaveUserData` 落盘)。**仅通关路径**；战败/中途退出走 `EndGameAndReturnToBase` 不清世界、不回满，故世界保留不重洗。**为何"通关清空→下次打开惰性重生"而非通关瞬间重建**：`CreateRandomPortalWorld`→`GetRandomMapPos` 依赖传送门UI的 `ui_Content`/item rect，UI 关着时取不到布局，无法即时重建。
- **位置避重叠**：`GetRandomMapPos(listOldPos)` 在内容区 90% 范围内随机，与已占位置(含配置项尺寸)碰撞则递归重取。
- **行星贴图**：`icon_res` 为空时 `CreateTools.CreatePlanetTexture(new CreateToolsForPlanetTextureBean(iconSeed))` 程序化生成（见 utils/framework）。
- **退出/刷新**：ESC/ui_ViewExit → `UIBaseMain`；ui_BtnRefresh → 消耗1次刷新次数并重洗（未解锁则按钮隐藏、用完弹提示）。

## 传送门item（UIViewBasePortalItem）

- 悬停气泡：`ui_BG` 上的 `PopupButtonCommonView` 注册 Enter/Exit，`SetData((gameWorldInfo, gameWorldInfoRandom, difficultyLevel), PopupEnum.PortalDetails)`（地图item展示**当前难度** `gameWorldInfoRandom.difficultyLevel`）。悬停时停止旋转(`isRotate=false`)。
  - **气泡展示哪个难度**：SetData 第三参 `difficultyLevel` 即展示难度。地图item传的是**当前难度**——它在世界创建时由 `SetRandomDataForConquer` 末尾 `SetDifficultyLevel(unlockDifficultyMax)` 置为**已解锁最高难度**(非难度1；构造器 `difficultyLevel=1` 只是占位，创建时被覆盖)，之后跟随难度对话框选择。故解锁多难度时，地图气泡默认显示**已解锁最高难度**。对话框各item则传各自难度号(展示该item自身难度)。事后解锁更高难度不会自动抬高已缓存世界(clamp 只向下夹)，需世界重新生成(通关清空/刷新)才重取。
- 缓慢绕 `rotateCenter` 旋转(随机 speed)；出现 `DOScale` OutBack。
- 点击 `ui_BG` → `OnClickForEnterWorld`。

## 进入流程（OnClickForEnterWorld）

1. 造确认 `DialogBean`(content=文本401「是否开启{0}的征服之旅？」)，`actionSubmit` 内：item 放大靠近动画 + `ShowMask` → 按 `gameFightType` 造 `FightBeanForConquer`/`FightBeanForInfinite` → `WorldHandler.EnterGameForFightScene(fightData)`。
2. 同时 `UIHandler.ShowDialogPortalDetails(dialogData)` 弹**难度选择对话框** `UIDialogPortalDetails.SetData(gameWorldInfo, gameWorldInfoRandom)`。
3. `actionCancel` → `isSelectWorld=false`(恢复旋转)。

## 难度选择对话框（UIDialogPortalDetails）

- **3+1 item 对象池**：常驻显示"上一个/当前/下一个"三档，切换时第 4 个临时进出(`InitItemPool` 克隆共 4 个)。
- 左右滑动切换(`AnimSwitchDifficulty`，`itemSpacing=500`，`Ease.OutBack`)，到边界回弹(`AnimSwitchBlocked`)；超 `unlockDifficultyMax` 不可选，更高且 `configDifficultyMax>unlockDifficultyMax` 时 Toast「难度未解锁」(文本404)。
- 切换调 `gameWorldInfoRandom.SetDifficultyLevel`(同步该难度道路/关卡数据)。
- 输入：ESC 取消、左右方向键切换。
- **UIViewDialogPortalDetailsItem**：单难度卡，行星图标(iconSeed)、难度文本(403「难度 等级{0}」)、解锁/未解锁灰罩、`bg_color`(征服难度表)、完成度、透明度/缩放动画、悬停 `PopupEnum.PortalDetails`(展示**该item自身难度**)。

## 详情气泡（UIPopupPortalDetails）

继承 `PopupShowCommonView`，`SetData((GameWorldInfoBean, GameWorldInfoRandomBean, int difficultyLevel))`。AutoLinkUI 按名绑定的 5 个 `UIViewPopupPortalDetailsItem`(名字/难度/线路数/关卡数/路径长度) + `ui_UIViewItem` 模板缓存池(奖励道具)。

- **名字/难度始终显示（不受研究门控），其余 4 项受「设施」研究门控**（`UserUnlock.CheckIsUnlock(UnlockEnum.PortalPreview*)`，**未解锁该项整行隐藏**；无尽模式不展示难度/关卡数/路径长度/奖励，即只有征服模式才有难度概念）：

  | 详情项 | 文本id | UnlockEnum | 值 |
  |--------|--------|-----------|----|
  | 名字 | 411 | (无门控,始终显示) | — |
  | 难度 | 415 | (无门控,仅征服模式显示,内容=difficultyLevel) | — |
  | 线路数量 | 412 | `PortalPreviewRoadNum` | 100300002 |
  | 关卡数量 | 413 | `PortalPreviewFightNum` | 100300003 |
  | 路径长度 | 414 | `PortalPreviewRoadLength` | 100300004 |
  | 奖励道具 | — | `PortalPreviewReward` | 100300005 |

- **奖励缓存池**：以 `ui_UIViewItem` 为模板的 `listRewardItemPool`(池首=模板、不足克隆、多余隐藏)，按 `GetDifficultyReward(difficultyLevel)` 填充；改动后 `LayoutRebuilder.ForceRebuildLayoutImmediate(ui_Items)` 再整体重建。
- `UIViewPopupPortalDetailsItem.SetData(title, content, isShow)`：`isShow==false` 整行隐藏。
- `UIViewItem`(`Common/Item`)：通用道具项(图标+数量+ItemInfo悬停气泡)，子类 `UIViewItemBackpack`/`UIViewItemEquip`。

## 奖励预生成与解锁重生成（预览即实领）

- 创建传送门时 `CreateDifficultyRandom` 按难度**预生成并冻结** `listReward`(`RewardSelectBean.CreateRewardListForConquer`) + 记录 `rewardUnlockSign`(`GetConquerEquipPoolSign`)。
- `GetDifficultyReward`：`listReward` 空(老存档) 或 解锁新魔物掉落致签名变化(`rewardUnlockSign != GetConquerEquipPoolSign()`)时按配置**重新生成并刷新签名**(魔物掉落装备需研究解锁，解锁后奖励池变大→重 roll)。
- 通关 BOSS 领奖消费同一份(`GameFightLogicConquer.ActionForUIFightSettlementNext → InitDataForReward`)，保证**预览=实领**。详见 `fight-reward-system` / `conquer-system`。

## 解锁相关（UnlockEnum / UserUnlockBean）

- 世界池：`GetUnlockGameWorldIds()`；传送门数量：`GetUnlockPortalShowCount()`(含 `UnlockEnum.PortalShowNum=100300001` 研究等级)。
- 征服难度上限：`GetUnlockGameWorldConquerDifficultyLevel(worldId)`。
- 详情预览门控：`UnlockEnum.PortalPreviewRoadNum/FightNum/RoadLength/Reward = 100300002~100300005`(设施研究)。新增/调整这些研究节点(配置表/解锁/多语言)见 `research-system`。

## 配置（Excel）

- `excel_game_world_info[游戏世界信息].xlsx`(工作表 `GameWorldInfo`) —— 世界配置唯一真实源；导出 `Assets/Resources/JsonText/GameWorldInfo.txt`。
- 改配置**必须改 Excel** 再由 Unity 导出 JSON；仅改 JSON 下次导出会被覆盖。

## 关键文件

| 文件 | 路径 |
|------|------|
| 基地地图(传送门容器) | Assets/Scripts/Component/UI/Game/BasePortal/UIBasePortal.cs |
| 单个传送门item | Assets/Scripts/Component/UI/Game/BasePortal/UIViewBasePortalItem.cs |
| 难度选择对话框 | Assets/Scripts/Component/UI/Dialog/UIDialogPortalDetails.cs |
| 难度item | Assets/Scripts/Component/UI/Dialog/PortalDetails/UIViewDialogPortalDetailsItem.cs |
| 详情气泡(名字/难度+4预览+奖励,门控) | Assets/Scripts/Component/UI/Popup/UIPopupPortalDetails.cs |
| 详情项 | Assets/Scripts/Component/UI/Popup/PortalDetails/UIViewPopupPortalDetailsItem.cs |
| 世界配置 Bean(禁改) | Assets/Scripts/Bean/MVC/Game/GameWorldInfoBean.cs |
| 传送门随机数据 | Assets/Scripts/Bean/MVC/Game/GameWorldInfoBeanPartial.cs |
| 传送门数据缓存 | Assets/Scripts/Bean/Game/UserTempBean.cs (listPortalWorldInfoRandomData) |
| 世界配置 Excel | Assets/Data/Excel/excel_game_world_info[游戏世界信息].xlsx |
| 导出 JSON | Assets/Resources/JsonText/GameWorldInfo.txt |

## 约束

- 配置变更**必须改 Excel**(`excel_game_world_info`)，由 Unity 导出 JSON；仅改 JSON 下次导出被覆盖。
- `GameWorldInfoBean.cs` 自动生成**禁改**；扩展写 `GameWorldInfoBeanPartial.cs`。`GameWorldInfoRandomBean`/`GameWorldDifficultyRandomBean` 是手写 Bean，可直接改。
- 地图位置用 `Vector2Bean` 包装序列化(规避 Newtonsoft 序列化 Vector2 的 normalized 递归栈溢出)。
- `InitMap` 是"补足不洗牌"语义：别改成每次重随(会让已解锁数量/缓存失效、每次打开都变)；要重洗走 `OnClickForRefresh`。
- 气泡名字/难度始终显示(不门控)，线路数/关卡数/路径长度/奖励四项**未解锁整行隐藏**(不是占位)；无尽模式不展示难度/关卡数/路径长度/奖励(难度是征服模式专属概念)。
- 输入走 `InputActionUIEnum`(ESC/Navigate)，禁用旧版 `Input` API。

## 关联 Skill 与 Agent

- 进入后的征服战斗(多关卡/BOSS/结算): `game-conquer` agent + `conquer-system` skill
- 无尽等其它战斗模式: `game-fight-logic` agent + `game-fight-system` skill
- 通关奖励生成规则/领奖: `game-fight-reward` agent + `fight-reward-system` skill
- 预览门控/数量研究节点配置与解锁: `game-research` agent + `research-system` skill
- 对话框/气泡 UI 基类: `ui-dialog` / `ui-popup` agent + `ui-framework` skill
- 基地相机(SetBasePortalCamera): `system-camera` agent + `camera-system` skill
- 世界配置表导入导出: `data-excel` agent + `excel-io` skill
- 传送门奖励预生成跨系统速查: 见记忆 `reference_portal_reward_pregen_research_gate`
