---
name: juicer-system
description: Demon Lord Roguelike 游戏的魔物回收(魔汁机/Juicer)系统开发指南。使用此SKILL当需要创建或修改魔汁机建筑出现/解锁显隐、魔汁机E键场景交互(打开UICreatureJuicer)、多选投入魔物榨汁UI(含CV_Juicer镜头/等级降序排序/投入数量研究门控/ui_LimmitText计数)、选择/取消选择的魔物投入·跳出动画(BuildingJuicerAnimForCreatureJumpIn/JumpOut 出入 Juicer/DropPoint 投料点)、榨汁流程演出(BuildingJuicerProcessBegin/AnimForHammer 锤子3秒3次/AnimForEssenceDrop 精华滴落/FocusJuicerCameraOnHole 镜头聚焦滴嘴)、榨汁逻辑(CreatureJuicerLogic)、魔汁机研究解锁(UnlockEnum.Juicer开启/JuicerNum投入数量+1)、奖励结算(CreatureJuicerLogic.SettleJuiceReward 消耗魔物产出魔汁道具+落盘)、魔汁道具使用(UICreatureManager 确认框加经验)等，包括 ScenePrefabForBase.objBuildingJuicer/BuildingJuicerRefresh/AnimForBuildingJuicerShow、ControlInteractionEnum.JuicerInteraction、UICreatureJuicer(多选投入+Start,listSelectCreature)、CameraHandler.SetJuicerCamera/ShakeJuicerCamera/RestoreJuicerCameraFocus、UserUnlockBean.GetUnlockJuicerCreatureMax(基础5+每级+1,满级15)、CreatureJuicerLogic.StartJuice(List)、GameHandler.StartCreatureJuicer、excel_research_info/excel_unlock_info/excel_language 配置等。奖励结算已实现：演出结束后 SettleJuiceReward 消耗投入魔物(退装备+背包/阵容双删)→按各魔物等级 LevelInfo.juicer_exp 汇总经验→生成 1 个魔汁道具(ItemIdEnum.Juice=200001/ItemTypeEnum.Juice=11,经验存 ItemBean.juicerExp,num_max=1 不堆叠)入背包→SaveUserData 落盘→Toast 61016；魔汁使用入口在魔物管理页 UICreatureManager.UseJuiceItem(确认框 61014→选中生物 levelExp+=juicerExp+消耗道具+落盘,满级 Toast 61015 拦截,魔王不可用),LevelInfo 新列 juicer_exp(1~10级=同级 level_exp 的100%,新增 id=0 行=20=1级的20%),多语言 61014~61017 与道具名 ItemsInfo 200001「魔汁」。
watched_files:
  - Assets/Scripts/Game/Logic/CreatureJuicerLogic.cs
  - Assets/Scripts/Component/UI/Game/CreatureJuicer/
  - Assets/Scripts/Component/Game/Scene/ScenePrefabForBase.cs
  - Assets/Scripts/Component/Game/Control/ControlForGameBase.cs
  - Assets/Scripts/Component/Handler/GameHandler.cs
  - Assets/Scripts/Enums/GameStateEnum.cs
  - Assets/Scripts/Enums/ItemsEnum.cs
  - Assets/Scripts/Bean/Game/ItemBean.cs
  - Assets/Resources/JsonText/ResearchInfo.txt
  - Assets/Resources/JsonText/UnlockInfo.txt
---

# 魔物回收 · 魔汁机 (Juicer) 系统开发指南

## 核心概念

**魔汁机 (Juicer)** 是基地里的「魔物回收」设施：玩家研究解锁后，基地场景出现魔汁机建筑，走近按 **E 键**打开 `UICreatureJuicer`，从背包里**多选投入魔物**，点 **Start** 开始榨汁（把魔物"榨"成 1 个**魔汁**道具——携带汇总经验的消耗品，在魔物管理页喂给其他魔物加经验）。

> **术语**：面向玩家统一叫「魔汁机」，代码用 `Juicer`/`CreatureJuicer`。项目里 ScenePrefab 旧注释「榨汁机」已统一为「魔汁机」，二者同义。

### ⚠️ 当前实现状态

**全链路已实现**：`解锁 → 建筑出现 → E键交互 → 开UI(切CV_Juicer镜头)多选投入魔物(上限研究门控) → 点Start → 场景演出(锤子3秒砸3次亮血 → 镜头聚焦滴嘴 → 精华滴落入瓶) → 奖励结算(消耗投入魔物 → 产出 1 个魔汁道具 → 落盘+Toast) → 重回魔汁机UI(可继续榨汁，被榨汁魔物已从背包消失)`。
**奖励结算已实现**：`CreatureJuicerLogic.SettleJuiceReward()`（protected,`#region 奖励结算`）在 StartJuice 演出结束后、**重开 UI 之前**调用，统一收在 Logic 层，不散落到 UI。

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
                      → CreatureJuicerLogic.StartJuice(List) // 关UI看演出 → 瓶子弹出 → 锤子3秒砸3次(首锤亮血)
                                                           // → 镜头聚焦滴嘴 → 精华滴落入瓶 → 血液隐藏+镜头还原
                                                           // → SettleJuiceReward() 奖励结算(消耗魔物→产魔汁→落盘+Toast 61016) → 重回魔汁机UI(可继续榨汁)
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

### 2.5 魔物投入/跳出动画（ScenePrefabForBase，`#region 魔汁机`）

选择/取消选择魔物时的场景表现，与升阶 `BuildingVatAnimForStart` 素材跳 vat 同套路（复用 `objVatMaterialCreature` Spine 模板，临时实例播完即毁）：

- **投料点**：场景 prefab 中 `Juicer/DropPoint` 空节点（本地 (0,1,0)，编辑器可微调）；`GetBuildingJuicerDropPosition()` 查找并缓存（字段 `tfJuicerDropPoint`），节点缺失时兜底为建筑上方 1 米
- `BuildingJuicerAnimForCreatureJumpIn(creatureData, actionForComplete=null)`：**投入**——投料点旁随机一侧、上方0.3~0.5米生成(缩放0.8、随机Z角) → `DOJump` 跳入投料点(0.75s,跳高0.8) + `DOScale` 缩到0.2 + `DORotate` 转360° + 后半程 `skeleton.SetColor` 淡出；入机瞬间 `PlaySoundRandom(sound_knock_3, sound_knock_5)` 随机敲击音效 + `BuildingJuicerPunch()` 机器抖动(吞入反馈) → 销毁
- `BuildingJuicerAnimForCreatureJumpOut(creatureData, actionForComplete=null)`：**跳出（反向）**——投料点生成(缩放0.2) → `DOJump` 弹到机旁随机一侧(落点 y=建筑根部+0.2米,跳高1.2) + 放大回0.8 + 反转-360° + **弹出过程直接淡出**(全程渐变,无停留) → 销毁（无音效）
- `BuildingJuicerPunch()`：机器 `DOPunchScale(0.15,-0.15,0.15)` 抖动（仿核心建筑 `AnimForBuildingCoreSpit`，先重置 localScale=one）
- 多选连点（上限15只）各自实例独立互不干扰；CloseUI 无需清理

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
- **选择/取消选择动画**：`EventForCardClickSelect` 里 Add 分支调 `scenePrefab.BuildingJuicerAnimForCreatureJumpIn(魔物)`（跳入投料点+机器抖动+入汁音效）、Remove 分支调 `BuildingJuicerAnimForCreatureJumpOut(魔物)`（弹出过程直接淡出）；`scenePrefab` 字段在 `OpenUI` 抓 `WorldHandler.GetCurrentScenePrefab<ScenePrefabForBase>(BaseGaming)`（与 UICreatureVat 同口径，动画细节见 2.5 节）
- **投入上限**：`GetJuicerMax()` = `GetUnlockJuicerCreatureMax()`(基础5 + JuicerNum 研究等级,满级15)。`RefreshLimitText()` 显示「已选/上限」,`ColorUtil.WrapLimitFull` 达上限转红
- **卡片使用态复用 `CardUseStateEnum.CreatureAscendTarget` + `CardStateEnum.CreatureAscendSelect/NoSelect`**——因为预制体 `ui_UIViewCreatureCardList_Target` 的卡片变体是 `UIViewCreatureCardItemForCreatureAscend`。`OnCellChangeForTarget` 按 `listSelectCreature.Contains` 回填高亮
- 选择事件走 `EventsInfo.UIViewCreatureCardItem_OnClickSelect`（`this.RegisterEvent`，实例事件）
- Start(`OnClickForStart`)：`listSelectCreature.Count==0` → `ToastHintText(61010 "请选择要榨汁的目标魔物")` 拦截；否则 → `GameHandler.Instance.StartCreatureJuicer(listSelectCreature)`
- **Start 按钮文本** `ui_BtnStartText`（`UITextLanguageView`）：预制上 `textId = 61011`（无代码赋值）
- `OpenUI` 关基地移动控制 `GameControlHandler.Instance.SetBaseControl(false)`

### 5. 榨汁逻辑（CreatureJuicerLogic : BaseGameLogic）

- 轻量逻辑，存 `GameManager.gameLogic` 单槽，`GetGameLogic<CreatureJuicerLogic>()` 可取；**不走 PreGame/StartGame 全流程**
- 字段：`targetCreatures`（本次投入魔物 List，StartJuice 里**复制一份**防 UI 列表后续被清空）、`scenePrefab`（基地场景预制，榨汁动画作用于 `scenePrefab.objBuildingJuicer`）
- `StartJuice(List<CreatureBean> targets)`（async void，仿扭蛋 ProcessForShowEgg）按序编排榨汁流程：
  1. **锁UI+镜头**：`UIHandler.CloseAllUI()`（UICreatureJuicer 随之关闭）+ 保持 `SetJuicerCamera(MaxValue,true)` + `SetDepthOfFieldActive(false)`
  2. **流程开始**：`scenePrefab.BuildingJuicerProcessBegin()`（瓶子弹出/液体隐藏/锤子归位/血液隐藏）→ 等 0.35s
  3. **锤子阶段**：`await scenePrefab.BuildingJuicerAnimForHammer()`（3秒3次，首锤亮血）
  4. **镜头聚焦滴嘴**：`CameraHandler.FocusJuicerCameraOnHole(scenePrefab.GetBuildingJuicerHole())` → 等 0.9s 镜头推近
  5. **精华滴落**：`await scenePrefab.BuildingJuicerAnimForEssenceDrop()` → 等 0.6s 静置
  6. **收尾**：`BuildingJuicerProcessEnd()`（血液隐藏/瓶子隐藏/锤子归位/清理水滴）+ `RestoreJuicerCameraFocus()` → 等 0.6s
  7. **奖励结算**：`SettleJuiceReward()`（须先于重开 UI，保证重开时 InitCreatureData 读到最新存档）→ `OpenUIAndCloseOther<UICreatureJuicer>(ui => ui.actionForExit = () => OpenUIAndCloseOther<UIBaseMain>())`（**重回魔汁机UI，玩家可继续榨汁，被榨汁魔物已从背包消失**；注入退出回调与场景E键入口一致，DoF 由 UI 的 OpenUI 自行关闭）
- **奖励结算 `SettleJuiceReward()`**（protected,`#region 奖励结算`）：遍历 targetCreatures 按等级取 `LevelInfoCfg.GetItemData(level).juicer_exp` 累计总经验（null 容错）→ `RemoveAllEquipToBackpack()` 退装备 → `userData.RemoveBackpackCreature()` 移除（与献祭消耗同写法，背包+阵容双删）；`new ItemBean((long)ItemIdEnum.Juice, 1)` + `juicerExp=总经验` → `AddBackpackItem()`（不堆叠重载，num_max=1）→ `SaveUserData()` 立即落盘 → `ToastHintText(string.Format(GetTextById(61016), 总经验))` 提示「榨汁完成，获得魔汁（经验+X）」
- `GameHandler.StartCreatureJuicer(List<CreatureBean> targetCreatures)`：get-or-create `CreatureJuicerLogic` → `StartJuice(targetCreatures)`（与 `StartGashaponMachine` / `StartCreatureSacrifice` 同风格）

### 5.5 榨汁流程演出（ScenePrefabForBase `#region 魔汁机` + CameraHandler `#region 魔汁机镜头聚焦/震动`）

场景节点（BaseScene.prefab `Juicer` 建筑子物体，初始状态已在预制设好）：`JuicerHammer`(锤子,含锤图预制)、`JuicerHammer/JuicerBlood`(血液,**默认隐藏**,内含 `JuicerBloodPS` 循环粒子 playOnAwake)、`Juicer/JuicerBloodPS`(血液喷溅粒子,**锤子每次锤中时播放**,Init 时 Stop 防 playOnAwake 误喷)、`JuicerBottle`(瓶子,**默认隐藏**)、`JuicerBottle/Juicer`(瓶内液体 SpriteRenderer)、`JuicerHole`(滴嘴,本地(0,0.314,0))、`JuicerEssenceDrop`(精华水滴,**默认隐藏**,大小/贴图在编辑器直接调)。

- `InitBuildingJuicerProcessNodes()`：懒查找缓存上述节点 + 瓶子/液体/水滴原始缩放（只做一次，`isInitJuicerProcessNodes` 门控）；关键节点缺失时 LogError 打点，流程自动降级跳过对应演出
- `BuildingJuicerProcessBegin()`：锤子 localY 归零 + 血液隐藏 + 水滴隐藏 + **瓶子弹出显示**(DOScale 0→原始缩放,OutBack 0.3s) + **液体隐藏**(精华未落入前不显示)
- `BuildingJuicerAnimForHammer()`（async Task）：**3秒内落下3次再升起**，单次节奏1秒 = 砸落0.2s(`DOLocalMoveY(0→-1.5)`,InQuad 加速重物坠地) + 触底停0.25s + 抬起0.45s(OutQuad 缓慢重物感) + 顶点停0.1s；每锤触底触发 `psJuicerBloodSplash.Stop+Play` **血液喷溅**(与砸击同步) + `BuildingJuicerPunch()` 机器抖动 + `PlaySoundRandom(sound_hit_1, sound_hit_3)` 打击音 + `CameraHandler.ShakeJuicerCamera()` 镜头震动；**首锤落下后 `JuicerBlood.SetActive(true)`**（内含循环喷血粒子随显示常开）
- `BuildingJuicerAnimForEssenceDrop()`（async Task）：**复用预制水滴节点 `JuicerEssenceDrop`**(大小/贴图编辑器调,代码缓存其原始缩放为 full 大小) → 移到滴嘴处由零变大(OutBack 0.4s,膨胀孕育感) → 纵向拉长(x×0.75/y×1.3)+InQuad 加速坠入瓶口(0.3s,落点=液体位置) → 压扁(x×1.5/y×0.3,0.08s)后隐藏(节点复用不销毁) → **液体弹出显示**(0→原始缩放 OutBack) + `PlaySoundRandom(sound_water_1, sound_water_3)` 滴水音 + 瓶子 DOPunchScale 抖动；节点缺失兜底直接显示液体
- `BuildingJuicerProcessEnd()`：血液隐藏 + **瓶子隐藏**（液体随父级一并隐藏，下次榨汁 ProcessBegin 重新弹出）+ 锤子归位 + 精华水滴归位隐藏（流程被打断兜底）
- **镜头聚焦/还原/震动**（CameraHandler）：
  - `GetBaseSceneCamera(cvName)`：仅查找 CV_List 下镜头，不改激活态/优先级（通用）
  - `FocusJuicerCameraOnHole(targetHole)`：CV_Juicer 的 `Follow/LookAt` 切到滴嘴 + `RotationComposer.TargetOffset` 清零 + DOTween 推近 `CinemachineFollow.FollowOffset` 至 (0,0,-1.5)（原 (0,1.5,-4)，与滴嘴同高平视特写，机位可调）；原状态缓存（`isJuicerCameraFocused` 门控）
  - `RestoreJuicerCameraFocus()`：还原 Follow/LookAt/两个偏移并拉回 FollowOffset（未聚焦过则空操作）
  - `ShakeJuicerCamera(amplitude=0.8, time=0.35)`：瞬时抬升 CV_Juicer 自带 `CinemachineBasicMultiChannelPerlin.AmplitudeGain` 后 DOTween 回落原值（首次震动缓存原振幅）
  - 注：CV_Juicer 预制上 `TrackingTarget`=Juicer 建筑根、`FollowOffset(0,1.5,-4)`、`RotationComposer.TargetOffset(0,1,0)`、Perlin 振幅0.1——聚焦方案正是改这些运行期字段，不动预制

### 5.6 魔汁道具与魔汁使用（ItemsEnum/ItemBean/UICreatureManager）

- **道具定义**：`ItemTypeEnum.Juice = 11`（消耗品，非装备，除魔王外所有生物可用）、`ItemIdEnum.Juice = 200001`（`Assets/Scripts/Enums/ItemsEnum.cs`）；配置在 `excel_items_info[道具信息].xlsx` 新行 200001（num_max=1 不堆叠、icon_res=`Item_Juicer_1` 无图集后缀，走默认 Items 图集 AtlasForItems——该图集按 Textures/Items 文件夹整包，Item_Juicer_1.png 自动入内）
- **实例经验**：`ItemBean.juicerExp`（long，仅 Juice 类型有效，榨汁时按投入魔物等级汇总；旧存档无此字段默认 0）
- **经验来源**：`LevelInfo.juicer_exp`（long，每级被榨汁贡献的经验，`excel_level_info[等级信息].xlsx`）——1~10 级 = 同级 level_exp 的 100%（100/1000/5000/10000/50000/100000/500000/1000000/5000000/10000000）；新增 id=0 行（level_exp=0，juicer_exp=20=1 级的 20%）
- **使用入口在魔物管理页（UICreatureManager，非榨汁 UI）**：道具列表点魔汁（`EventForItemBackpackClickSelect` 按 `GetItemType()==Juice` 分流，其余道具照旧走装备）→ `UseJuiceItem(itemData)` 弹确认框「是否对{生物名}使用魔汁？经验+X」（textId 61014，`ShowDialogNormal`）→ 确定后当前选中生物 `levelExp += juicerExp` + `RemoveBackpackItem` 消耗 + `SaveUserData` 落盘 + 三连刷新（卡片详情 `SetCardDetails` / 献祭按钮 `RefreshSacrificeButton` / 背包列表 `InitBackpackItemsData`）；经验只累计不自动升级（沿用战斗经验语义，升级仍走献祭）
- **拦截**：满级生物（`IsMaxLevel()`）Toast 61015 拦截防浪费；魔王不可用（`UIViewItemBackpackList` 过滤对魔王隐藏魔汁，`UseJuiceItem` 里 null/魔王兜底 return）
- **道具气泡**：`UIPopupItemInfo.SetJuiceExp` 仅 Juice 类型显示「经验+X」行（textId 61017，魔汁无属性，与属性区互斥自动隐藏）

## 多语言

| 文本 | 文件(真实源=excel_language 同名工作表) | id | cn | en |
|------|------|----|----|----|
| 研究节点名(开启) | Language_ResearchInfo_cn/en | 100600001 | 开启魔汁机 | Unlock Juicer |
| 研究节点名(投入+1) | Language_ResearchInfo_cn/en | 100600002 | 魔汁机投入数量+1 | Juicer Input +1 |
| 交互提示 | Language_UIText_cn/en | 2009 | 魔汁机 | Juicer |
| 未投入提示 | Language_UIText_cn/en | 61010 | 请选择要榨汁的目标魔物 | Please select a monster to juice |
| Start 按钮 | Language_UIText_cn/en | 61011 | 开始榨汁 | Begin Juicing |
| 超上限提示 | Language_UIText_cn/en | 61012 | 最多只能投入{0}只魔物 | Up to {0} monsters can be juiced |
| 魔汁使用确认框 | Language_UIText_cn/en | 61014 | 是否对{0}使用魔汁？经验+{1} | Use Demon Juice on {0}? EXP +{1} |
| 满级拦截 | Language_UIText_cn/en | 61015 | 目标已满级，无法使用魔汁 | Target is at max level. Cannot use Demon Juice |
| 榨汁获得提示 | Language_UIText_cn/en | 61016 | 榨汁完成，获得魔汁（经验+{0}） | Juicing complete! Obtained Demon Juice (EXP +{0}) |
| 经验行(道具气泡) | Language_UIText_cn/en | 61017 | 经验+{0} | EXP +{0} |
| 魔汁道具名 | Language_ItemsInfo_cn/en | 200001 | 魔汁 | Demon Juice |

注：61014~61017 与 ItemsInfo 200001 均已在 excel_language 对应工作表配齐 12 语种（cn/en/jp/kr/tw/de/fr/ru/es/br/pl/tr）。

## 关键文件

| 文件 | 路径 |
|------|------|
| 榨汁逻辑 | Assets/Scripts/Game/Logic/CreatureJuicerLogic.cs |
| 榨汁 UI | Assets/Scripts/Component/UI/Game/CreatureJuicer/ (UICreatureJuicer + Component) |
| 逻辑入口 | Assets/Scripts/Component/Handler/GameHandler.cs (`StartCreatureJuicer`) |
| 建筑出现+投入/跳出动画+榨汁流程演出 | Assets/Scripts/Component/Game/Scene/ScenePrefabForBase.cs (`#region 魔汁机`，含 BuildingJuicerAnimForCreatureJumpIn/JumpOut/BuildingJuicerPunch 及 BuildingJuicerProcessBegin/AnimForHammer/AnimForEssenceDrop/ProcessEnd) |
| 投料点/流程节点 | BaseScene.prefab 中 `Juicer/DropPoint`(本地(0,1,0))、`Juicer/JuicerHammer`、`Juicer/JuicerHammer/JuicerBlood`、`Juicer/JuicerBloodPS`(血液喷溅,锤击播放)、`Juicer/JuicerBottle(/Juicer)`、`Juicer/JuicerHole`(本地(0,0.314,0))、`Juicer/JuicerEssenceDrop`(精华水滴,大小/贴图编辑器调) |
| E键交互 | Assets/Scripts/Component/Game/Control/ControlForGameBase.cs (`HandleForUseEUp`) |
| 枚举 | Assets/Scripts/Enums/GameStateEnum.cs (`UnlockEnum.Juicer/JuicerNum` / `ControlInteractionEnum.JuicerInteraction`) |
| 镜头 | Assets/Scripts/Component/Handler/CameraHandler.cs (`SetJuicerCamera`→CV_Juicer；`GetBaseSceneCamera`/`FocusJuicerCameraOnHole`/`RestoreJuicerCameraFocus`/`ShakeJuicerCamera`) |
| 投入上限 | Assets/Scripts/Bean/Game/UserUnlockBean.cs (`GetUnlockJuicerCreatureMax`) · UserLimmitBean.cs (`juicerCreatureMax`) |
| 魔汁道具/枚举 | Assets/Scripts/Enums/ItemsEnum.cs (`ItemTypeEnum.Juice=11` / `ItemIdEnum.Juice=200001`) · Assets/Scripts/Bean/Game/ItemBean.cs (`juicerExp`) · excel_items_info(200001,num_max=1 不堆叠) |
| 榨汁经验配置 | excel_level_info(`juicer_exp` 列,1~10级=同级 level_exp 100%,新增 id=0 行=20) · LevelInfoBean |
| 魔汁使用 | Assets/Scripts/Component/UI/Game/CreatureManager/UICreatureManager.cs (`UseJuiceItem`) · UIViewItemBackpackList.cs(魔王隐藏魔汁过滤) · UIPopupItemInfo.cs(`SetJuiceExp` 61017) |
| 研究/解锁 | excel_research_info(100600001/100600002) · excel_unlock_info · ResearchInfo.txt · UnlockInfo.txt |
| 多语言 | excel_language · Language_ResearchInfo_cn/en · Language_UIText_cn/en |
| 测试入口 | Assets/Scripts/Game/Launcher/LauncherTest.cs (`StartForCreatureJuicerTest`) · Assets/Editor/GameTestEditor.cs (`DrawCreatureJuicerTest`) |

## 测试入口（魔汁机测试）

测试模式 `TestSceneTypeEnum.CreatureJuicer`（=12）：与献祭/进阶测试同套路——**选存档槽位(1~3) → 滑条选投入魔物上限(5~15，默认拉满) → 进基地直接打开 `UICreatureJuicer`**，全程内存模拟不落盘（`isTestSimulation`）。

- **Editor**：`GameTestEditor.DrawCreatureJuicerTest()`（存档槽位 IntPopup + 投入上限 IntSlider）
- **入口**：`LauncherTest.StartForCreatureJuicerTest(saveSlot, juicerCreatureMax)`：加载存档 → `SetUserData` + `isTestSimulation=true` → 覆盖解锁 `AddUnlock(Juicer)` + `AddUnlock(JuicerNum, 目标上限-基础juicerCreatureMax)`（按 `ResearchInfoCfg.level_max` 钳制）→ 一次性 `World_EnterGameForBaseScene` 回调里开 UI，`actionForExit → UIBaseMain`（与场景E键交互入口一致）
- 详见 `test-system` Skill「魔汁机测试 (CreatureJuicer)」

## 约束与注意

- 奖励结算**统一收在 `CreatureJuicerLogic.SettleJuiceReward()`**（演出后、重开 UI 前调用，保证重开读到最新存档），别散到 UI；魔汁使用入口在 **UICreatureManager**（魔物管理页道具列表），不在榨汁 UI。榨汁流程演出已按「Logic 编排 → ScenePrefab 演出方法 → CameraHandler 镜头」分层，加新演出照此分层。
- 建筑显隐**唯一门控** `UnlockEnum.Juicer`；交互碰撞体命名固定 `JuicerInteraction`。
- **投入数量上限**门控 `UnlockEnum.JuicerNum`(基础5+每级+1,满级15)；改基础值改 `UserLimmitBean.juicerCreatureMax`。
- **镜头 CV_Juicer** 预制配置：`TrackingTarget`=Juicer建筑根+`CinemachineFollow.FollowOffset(0,1.5,-4)`+`RotationComposer.TargetOffset(0,1,0)`+Perlin振幅0.1；榨汁流程通过 `FocusJuicerCameraOnHole/RestoreJuicerCameraFocus` **运行期改这些字段**聚焦/还原滴嘴，不动预制；返回 UIBaseMain 自动还原基地镜头。
- UI 驱动：E键直接开 UICreatureJuicer（不像献祭那样 Logic 驱动开UI）。**多选**投入(`listSelectCreature`)；默认按等级降序、已排除上阵/非Idle。
- 卡片态**复用 CreatureAscend 系列**；计数文本 `ui_LimmitText`(TMP,AutoLinkUI 按名绑定,预制需同名子物体)。
- 配置改 **Excel 唯一真实源**并同步 JSON；自动生成 Bean/JSON 不手改结构（Bean 扩展写 Partial）。
- UI 继承 `BaseUIComponent`；输入走 `InputActionUIEnum`（禁用旧版 Input API）。
- 研究 `icon_res` 目前占位（`ui_research_65`），需专属图标时**先征得用户同意再用 PixelLab**。
- 预制体（objBuildingJuicer 交互碰撞体、CV_Juicer 镜头、UICreatureJuicer 接线含 `ui_LimmitText`）由用户手动接好；改动涉及预制体时优先走 Unity MCP 或提示用户手工处理。
