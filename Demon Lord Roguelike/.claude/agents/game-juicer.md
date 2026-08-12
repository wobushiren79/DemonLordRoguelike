---
name: game-juicer
description: 魔物回收(魔汁机/Juicer)系统开发：基地魔汁机建筑(ScenePrefabForBase.objBuildingJuicer 出现/解锁显隐)、E键场景交互(ControlInteractionEnum.JuicerInteraction 打开 UICreatureJuicer)、多选投入魔物榨汁UI(UICreatureJuicer 多选 listSelectCreature+Start，含 CV_Juicer 镜头/等级降序排序/投入数量门控/ui_LimmitText 计数/JuicerWater 魔汁水位(按总经验等级取 level_color 着色+区间进度×0.95 水位)+JuicerText 经验预览)、选择/取消选择的魔物投入·跳出动画(ScenePrefabForBase.BuildingJuicerAnimForCreatureJumpIn/JumpOut，复用 objVatMaterialCreature 模板 DOJump 出入 Juicer/DropPoint 投料点，入机机器抖动+入汁音效)、榨汁流程演出(BuildingJuicerProcessBegin 瓶子弹出/BuildingJuicerAnimForHammer 锤子3秒砸3次首锤亮血/BuildingJuicerAnimForEssenceDrop 精华滴落入瓶/BuildingJuicerProcessEnd 收尾，CameraHandler.FocusJuicerCameraOnHole/RestoreJuicerCameraFocus/ShakeJuicerCamera 镜头聚焦滴嘴与震动)、榨汁逻辑(CreatureJuicerLogic 经 GameHandler.StartCreatureJuicer(List) 驱动，UI驱动+轻量Logic)、魔汁机镜头(CameraHandler.SetJuicerCamera→CV_Juicer)、投入数量上限(UserUnlockBean.GetUnlockJuicerCreatureMax=UserLimmitBean.juicerCreatureMax 基础5+UnlockEnum.JuicerNum 每级+1 满级15)、魔汁机研究解锁(UnlockEnum.Juicer=100600001 开启/JuicerNum=100600002 投入数量+1，excel_research_info/excel_unlock_info)、奖励结算(CreatureJuicerLogic.SettleJuiceReward：演出结束后消耗投入魔物(退装备+背包/阵容双删)→按各魔物等级 LevelInfo.juicer_exp 汇总经验→生成 1 个魔汁道具(ItemIdEnum.Juice=200001/ItemTypeEnum.Juice=11，经验存 ItemBean.juicerExp，num_max=1 不堆叠)入背包→SaveUserData 落盘→Toast 61016)、魔汁使用(入口在魔物管理页 UICreatureManager.UseJuiceItem，非榨汁UI：点魔汁→确认框 61014→选中生物 levelExp+=juicerExp+消耗道具+落盘，满级 Toast 61015 拦截，魔王不可用/背包列表对魔王隐藏魔汁)、LevelInfo 新列 juicer_exp(1~10级=同级 level_exp 的100%，新增 id=0 行 juicer_exp=20=1级的20%)、多语言 61014~61017 与道具名 ItemsInfo 200001「魔汁」。
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
  - Assets/Scripts/Enums/ItemsEnum.cs
  - Assets/Scripts/Bean/Game/ItemBean.cs
  - Assets/Scripts/Enums/GameStateEnum.cs
  - Assets/Resources/JsonText/ResearchInfo.txt
  - Assets/Resources/JsonText/UnlockInfo.txt
---

# 魔物回收 · 魔汁机 (Juicer) 系统开发代理

你负责 [Scripts/](Assets/Scripts/) 中与「魔汁机(魔物回收)」相关的代码开发。详细机制见 `juicer-system` Skill。

> **术语**：面向玩家统一叫「魔汁机」，代码用 `Juicer`/`CreatureJuicer`。（ScenePrefab 里旧「榨汁机」注释已统一为「魔汁机」。）

## 当前状态（重要）

魔汁机**全链路已实现**：解锁 → 建筑出现 → E键交互 → 开UI(切 CV_Juicer 镜头)多选投入魔物(上限研究门控) → 点 Start → 场景演出(瓶子弹出 → 锤子3秒砸3次首锤亮血 → 镜头聚焦滴嘴 → 精华滴落入瓶) → **奖励结算(消耗投入魔物 → 产出 1 个魔汁道具 → 落盘+Toast)** → 重回魔汁机UI(可继续榨汁，被榨汁魔物已从背包消失)。
**奖励结算已实现**：`CreatureJuicerLogic.SettleJuiceReward()`(protected,`#region 奖励结算`)在 StartJuice 演出结束后、**重开 UI 之前**调用——遍历 targetCreatures 按等级取 `LevelInfoCfg.GetItemData(level).juicer_exp` 累计总经验(null 容错) → `RemoveAllEquipToBackpack()` 退装备 → `RemoveBackpackCreature()` 移除(与献祭消耗同写法,背包+阵容双删)；生成 1 个魔汁道具(`ItemIdEnum.Juice=200001`,num_max=1 不堆叠,经验存 `ItemBean.juicerExp` 实例字段)`AddBackpackItem` 入背包 → `SaveUserData()` 立即落盘 → Toast 61016「榨汁完成，获得魔汁（经验+X）」。结算先于重开 UI,保证重开时 InitCreatureData 读到最新存档。

## 核心链路（UI 驱动 + 轻量 Logic，仿容器 UICreatureVat 而非献祭全流程）

```
基地场景魔汁机建筑(objBuildingJuicer, 子物体交互碰撞体命名 JuicerInteraction, 层 LayerInfo.Interaction)
  └─ 玩家靠近按 E → ControlForGameBase.HandleForUseEUp
       case ControlInteractionEnum.JuicerInteraction
       → UIHandler.OpenUIAndCloseOther<UICreatureJuicer>(ui => ui.actionForExit = ()=>Open UIBaseMain)
            └─ UICreatureJuicer.OpenUI: 切 CV_Juicer 镜头 + 关远景 → 多选投入魔物(listSelectCreature,上限门控) → 点 Start
                 → GameHandler.StartCreatureJuicer(List<CreatureBean>)
                      → CreatureJuicerLogic.StartJuice(List)  // 关UI看演出 → 瓶子弹出 → 锤子3秒砸3次(首锤亮血)
                                                            // → 镜头聚焦滴嘴 → 精华滴落入瓶 → 血液隐藏+镜头还原
                                                            // → SettleJuiceReward() 奖励结算(消耗魔物→产魔汁→落盘+Toast 61016) → 重回魔汁机UI(可继续榨汁)
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

### 魔物投入/跳出动画（ScenePrefabForBase，`#region 魔汁机`）
- **投料点**：场景 prefab 中 `Juicer/DropPoint` 空节点（本地 (0,1,0)，可在编辑器微调）；`GetBuildingJuicerDropPosition()` 查找缓存（`tfJuicerDropPoint`），节点缺失兜底建筑上方 1 米
- `BuildingJuicerAnimForCreatureJumpIn(creatureData, actionForComplete=null)`：**投入动画**——复用 `objVatMaterialCreature` 模板实例化临时 Spine 模型（`CreatureHandler.SetCreatureData` 刷外观，无装备/武器），投料点旁随机一侧偏上生成（缩放0.8、随机Z角）→ `DOJump` 跳入投料点(0.75s) + 缩到0.2 + 转360° + 后半程淡出；入机瞬间 `PlaySoundRandom(sound_knock_3, sound_knock_5)` 随机敲击音效 + `BuildingJuicerPunch()` 机器抖动(吞入反馈) → 销毁。与升阶 `BuildingVatAnimForStart` 素材跳 vat 同套路
- `BuildingJuicerAnimForCreatureJumpOut(creatureData, actionForComplete=null)`：**跳出动画（反向）**——投料点生成(缩放0.2) → `DOJump` 弹到机旁随机一侧(落点=建筑根部上方0.2米) + 放大回0.8 + 反转-360° + **弹出过程直接淡出** → 销毁（无音效）
- `BuildingJuicerPunch()`：机器 PunchScale 抖动（仿核心建筑 `AnimForBuildingCoreSpit`，先重置 localScale=one）
- 均为一次性临时实例、播完即毁，不跟踪持久实例；多选连点（上限15只）各自独立互不干扰，CloseUI 无需清理

### 场景交互（ControlForGameBase）
- `ControlInteractionEnum.JuicerInteraction = 9`（提示文本 textId = 2000+9 = 2009 = "魔汁机"）
- 交互物体命名必须为 `JuicerInteraction`（`GetInteractionEnum` 用 `Enum.TryParse` 取下划线前段匹配）
- `HandleForUseEUp` 的 `case JuicerInteraction` 直接 `OpenUIAndCloseOther<UICreatureJuicer>`，并注入 `actionForExit = ()=>OpenUIAndCloseOther<UIBaseMain>()`（场景交互开的界面退出回场景，不回 UIBaseCore；与成就/容器同口径）

### 榨汁 UI（UICreatureJuicer : BaseUIComponent）
- 组件字段（`UICreatureJuicerComponent`）：`ui_BtnStart`/`ui_BtnStartText`/`ui_UIViewBaseInfoContent`/`ui_UIViewCreatureCardList_Target`/**`ui_LimmitText`(TMP,计数)**/`ui_ViewExit`/**`ui_JuicerWater`(Image,魔汁水位)**/**`ui_JuicerText`(TMP,经验预览)**。`ui_LimmitText` 靠 `AutoLinkUI` 按名绑定,**预制体需有同名子物体**
- `actionForExit`：退出回调，由打开入口注入
- **镜头**：`OpenUI` 切 `CameraHandler.Instance.SetJuicerCamera(int.MaxValue,true)`(CV_Juicer,固定机位无 Follow) + `VolumeHandler.SetDepthOfFieldActive(false)`；`CloseUI` 还原 DoF=true(基地镜头由返回 UIBaseMain 统一还原)
- 可投入魔物列表：背包内**空闲态(CreatureStateEnum.Idle)且未上阵(CheckIsInAnyLineup)** 的魔物；**默认按等级降序排序**(`listCreatureData.Sort((a,b)=>b.level.CompareTo(a.level))`)
- **多选**（`listSelectCreature` List，再次点已选则移出；加入前判 `GetJuicerMax()` 上限，达上限弹 Toast 61012 拦截）；`OnCellChangeForTarget` 按 `Contains` 回填高亮
- **选择/取消选择动画**：`EventForCardClickSelect` 里 Add 分支调 `scenePrefab.BuildingJuicerAnimForCreatureJumpIn(魔物)`（跳入投料点+机器抖动+随机敲击音效 sound_knock_3/5）、Remove 分支调 `BuildingJuicerAnimForCreatureJumpOut(魔物)`（弹出过程直接淡出）；`scenePrefab` 字段在 `OpenUI` 抓 `WorldHandler.GetCurrentScenePrefab<ScenePrefabForBase>(BaseGaming)`（与 UICreatureVat 同口径）
- **投入上限**：`GetJuicerMax()`=`GetUnlockJuicerCreatureMax()`(基础5+JuicerNum 研究等级,满级15)；`RefreshLimitText()` 显示「已选/上限」,`ColorUtil.WrapLimitFull` 达上限转红
- **魔汁水位/经验预览**（`RefreshJuicerWater()`,`#region 魔汁水位与经验预览`,挂进 `RefreshUI()` 与选择事件末尾）：汇总已选魔物 `LevelInfo.juicer_exp` 得总经验 → 取「juicer_exp ≤ 总经验」的最高等级(0级区间下限按0计) → **水色**=该级 `LevelInfo.level_color`(经 `LevelInfoCfg.GetLevelColor`,0级白,前层原色/后层×0.6暗化写 `_LayerColor1/2`)、**水位**=区间进度(总经验在 本级~下一级juicer_exp 间的比例)×0.95 写 `_WaterLevel`(无下一级=满级进度拉满)、**经验文本** `ui_JuicerText` 显示 `+总经验`；**一个素材都没选时隐藏水位与文本**。水位材质=`Mat_UICreatureJuicer_Exp`(Shader_UI_ImageWaterWave,2层水),运行时 `new Material` 懒克隆防改共享资源(`juicerWaterMatInstance`,`OnDestroy` 销毁)
- **卡片使用态复用 `CardUseStateEnum.CreatureAscendTarget` + `CardStateEnum.CreatureAscendSelect/NoSelect`**——预制体 `ui_UIViewCreatureCardList_Target` 挂的卡片变体是 `UIViewCreatureCardItemForCreatureAscend`
- 选择走 `EventsInfo.UIViewCreatureCardItem_OnClickSelect` 事件（`this.RegisterEvent`）
- Start：`listSelectCreature.Count==0` → `ToastHintText(61010)` 拦截；否则 → `GameHandler.Instance.StartCreatureJuicer(listSelectCreature)`

### 榨汁逻辑（CreatureJuicerLogic : BaseGameLogic）
- 轻量逻辑，存 `GameManager.gameLogic` 单槽（`GetGameLogic<CreatureJuicerLogic>()` 可取）；**不走 PreGame/StartGame 全流程**，由 UI 的 Start 经 `GameHandler.StartCreatureJuicer` 直接 `StartJuice`
- 字段：`targetCreatures`（本次投入魔物 List，StartJuice 里**复制一份**防 UI 列表后续被清空）、`scenePrefab`（基地场景预制，榨汁动画作用于 `scenePrefab.objBuildingJuicer`）
- `StartJuice(List<CreatureBean> targets)`（async void，仿扭蛋 ProcessForShowEgg）按序编排：① 锁UI+镜头(`CloseAllUI`+`SetJuicerCamera(MaxValue,true)`+DoF false) → ② `BuildingJuicerProcessBegin()`(瓶子弹出/液体隐藏/锤子归位/血液隐藏)等0.35s → ③ `await BuildingJuicerAnimForHammer()`(3秒3次,首锤亮血) → ④ `FocusJuicerCameraOnHole(GetBuildingJuicerHole())` 等0.9s → ⑤ `await BuildingJuicerAnimForEssenceDrop()` 等0.6s → ⑥ `BuildingJuicerProcessEnd()`(血液隐藏/瓶子隐藏/锤子归位/清残留水滴)+`RestoreJuicerCameraFocus()` 等0.6s → ⑦ `SettleJuiceReward()` 奖励结算(须先于重开UI,保证重开读到最新存档) → **重回 `OpenUIAndCloseOther<UICreatureJuicer>(ui => ui.actionForExit = ()=>Open UIBaseMain)`（玩家可继续榨汁，被榨汁魔物已从背包消失；注入退出回调与场景E键入口一致，DoF 由 UI 的 OpenUI 自行关闭）**
- **奖励结算 `SettleJuiceReward()`**（protected,`#region 奖励结算`）：遍历 targetCreatures 按等级取 `LevelInfoCfg.GetItemData(level).juicer_exp` 累计总经验(null 容错) → `RemoveAllEquipToBackpack()` 退装备 → `userData.RemoveBackpackCreature()` 移除(与献祭消耗同写法,背包+阵容双删)；`new ItemBean((long)ItemIdEnum.Juice, 1)` + `juicerExp=总经验` → `AddBackpackItem()`(不堆叠重载,num_max=1) → `SaveUserData()` 立即落盘 → `ToastHintText(string.Format(GetTextById(61016), 总经验), 1)`(state=1 成功图标) 提示「榨汁完成，获得魔汁（经验+X）」
- `GameHandler.StartCreatureJuicer(List<CreatureBean>)`：get-or-create logic → `StartJuice(list)`（与 `StartGashaponMachine`/`StartCreatureSacrifice` 同风格）

### 魔汁道具与魔汁使用（ItemsEnum/ItemBean/UICreatureManager）
- **道具定义**：`ItemTypeEnum.Juice = 11`(消耗品,非装备,除魔王外所有生物可用)、`ItemIdEnum.Juice = 200001`(`Assets/Scripts/Enums/ItemsEnum.cs`)；配置在 excel_items_info 新行 200001(num_max=1 不堆叠、icon_res=`Item_Juicer_1` 无图集后缀,走默认 Items 图集 AtlasForItems——该图集按 Textures/Items 文件夹整包,Item_Juicer_1.png 自动入内)
- **实例经验**：`ItemBean.juicerExp`(long,仅 Juice 类型有效,榨汁时按投入魔物等级汇总;旧存档无此字段默认0)
- **经验来源**：`LevelInfo.juicer_exp`(long,每级被榨汁贡献的经验,excel_level_info)——1~10级 = 同级 level_exp 的 100%(100/1000/5000/10000/50000/100000/500000/1000000/5000000/10000000)；新增 id=0 行(level_exp=0,juicer_exp=20=1级的20%)
- **使用入口在魔物管理页(UICreatureManager,非榨汁 UI)**：道具列表点魔汁(`EventForItemBackpackClickSelect` 按 `GetItemType()==Juice` 分流) → `UseJuiceItem(itemData)` 弹确认框「是否对{生物名}使用魔汁？经验+X」(textId 61014) → 确定后当前选中生物 `levelExp += juicerExp` + `RemoveBackpackItem` 消耗 + `SaveUserData` 落盘 + 三连刷新(卡片详情/献祭按钮/背包列表)；经验只累计不自动升级(沿用战斗经验语义,升级仍走献祭)
- **拦截**：满级生物 Toast 61015 拦截防浪费；魔王不可用(`UIViewItemBackpackList` 过滤对魔王隐藏魔汁,`UseJuiceItem` 里 null/魔王兜底 return)
- **道具气泡**：`UIPopupItemInfo.SetJuiceExp` 仅 Juice 类型显示「经验+X」行(textId 61017,魔汁无属性,与属性区互斥)

### 榨汁流程演出（ScenePrefabForBase `#region 魔汁机`）
场景节点（BaseScene.prefab `Juicer` 建筑子物体，初始状态预制已设好）：`JuicerHammer`(锤子)、`JuicerHammer/JuicerBlood`(血液,**默认隐藏**,内含 `JuicerBloodPS` 循环粒子 playOnAwake)、`Juicer/JuicerBloodPS`(血液喷溅粒子,**锤子每次锤中时播放**,Init 时 Stop 防 playOnAwake 误喷)、`JuicerBottle`(瓶子,**默认隐藏**)、`JuicerBottle/Juicer`(瓶内液体 SpriteRenderer)、`JuicerHole`(滴嘴,本地(0,0.314,0))、`JuicerEssenceDrop`(精华水滴,**默认隐藏**,大小/贴图在编辑器直接调)。
- `InitBuildingJuicerProcessNodes()`：懒查找缓存节点+瓶子/液体/水滴原始缩放（`isInitJuicerProcessNodes` 只做一次）；关键节点缺失 LogError 打点，流程降级跳过对应演出
- `BuildingJuicerProcessBegin()`：锤子 localY 归零 + 血液隐藏 + 水滴隐藏 + 瓶子弹出显示(DOScale 0→原始缩放 OutBack 0.3s) + 液体隐藏
- `BuildingJuicerAnimForHammer()`(async Task)：**3秒内落下3次再升起**，单次1秒 = 砸落0.2s(`DOLocalMoveY(0→-1.5)` InQuad 加速重物坠地)+触底停0.25s+抬起0.45s(OutQuad 缓慢重物感)+顶点停0.1s；每锤触底 `psJuicerBloodSplash.Stop+Play` **血液喷溅**(与砸击同步)+`BuildingJuicerPunch()` 机器抖动+`PlaySoundRandom(sound_hit_1, sound_hit_3)`+`ShakeJuicerCamera()` 镜头震动；**首锤落下后亮血**(内含循环喷血粒子随显示常开)
- `BuildingJuicerAnimForEssenceDrop()`(async Task)：**复用预制水滴节点 `JuicerEssenceDrop`**(大小/贴图编辑器调,代码缓存其原始缩放为 full 大小) → 移到滴嘴处由零变大(OutBack 0.4s) → 拉长(x×0.75/y×1.3)+InQuad 加速坠入瓶口(0.3s,落点=液体位置) → 压扁(x×1.5/y×0.3,0.08s)后隐藏(节点复用不销毁) → 液体弹出显示(OutBack 0.35s)+`PlaySoundRandom(sound_water_1, sound_water_3)`+瓶子 DOPunchScale；节点缺失兜底直接显示液体
- `BuildingJuicerProcessEnd()`：血液隐藏+**瓶子隐藏**(液体随父级一并隐藏,下次榨汁 ProcessBegin 重新弹出)+锤子归位+精华水滴归位隐藏(打断兜底)

### 镜头（CameraHandler）
- `SetJuicerCamera(int priority, bool isEnable)` → `SetCameraForBaseScene(..., "CV_Juicer")`；CV_Juicer 预制配置：`TrackingTarget`=Juicer建筑根 + `CinemachineFollow.FollowOffset(0,1.5,-4)` + `RotationComposer.TargetOffset(0,1,0)` + Perlin 振幅0.1
- `#region 魔汁机镜头聚焦/震动`：`GetBaseSceneCamera(cvName)`(仅查找不改态,通用) / `FocusJuicerCameraOnHole(hole)`(Follow/LookAt 切滴嘴+TargetOffset 清零+DOTween 推近 FollowOffset 至(0,0,-1.5),与滴嘴同高平视特写,缓存原状态 `isJuicerCameraFocused` 门控) / `RestoreJuicerCameraFocus()`(还原,未聚焦空操作) / `ShakeJuicerCamera(0.8,0.35)`(抬升 Perlin 振幅后回落,首次缓存原振幅)——**运行期改镜头字段,不动预制**
- 返回 UIBaseMain 时 `SetCameraForControl(Base)` 自动还原基地镜头

### 测试入口（魔汁机测试）
- `TestSceneTypeEnum.CreatureJuicer = 12`：选存档槽位(1~3) + 投入上限滑条(5~15,默认拉满) → 进基地直接开 `UICreatureJuicer`，全程 `isTestSimulation` 内存模拟不落盘
- `LauncherTest.StartForCreatureJuicerTest(saveSlot, juicerCreatureMax)`：加载存档 → `SetUserData`+`isTestSimulation=true` → 覆盖 `AddUnlock(Juicer)` + `AddUnlock(JuicerNum, 目标上限-基础juicerCreatureMax)`(按 `ResearchInfoCfg.level_max` 钳制) → 一次性 `World_EnterGameForBaseScene` 回调开 UI，`actionForExit → UIBaseMain`(与场景E键入口一致)
- Editor 面板 `GameTestEditor.DrawCreatureJuicerTest()`；详见 `test-system` / `juicer-system` Skill

### 投入数量上限（研究门控）
- `UserLimmitBean.juicerCreatureMax = 5`（基础值）
- `UserUnlockBean.GetUnlockJuicerCreatureMax()` = `juicerCreatureMax` + `GetUnlockResearchLeveByUnlockEnum(UnlockEnum.JuicerNum)`（每级+1，level_max=10，满级15；同献祭/进阶素材口径）

### 解锁 / 研究（设施类，两个节点）
- `UnlockEnum.Juicer = 100600001`(开启) / `UnlockEnum.JuicerNum = 100600002`(投入数量+1)（新设施块 1006，`GameStateEnum.cs`）
- 研究节点 `excel_research_info[研究信息].xlsx` + `ResearchInfo.txt`：
  - 开启：`research_type=1`、`id=100600001`、`pre_unlock_ids="100500001"`(前置=成就)、`pay_crystal="5"`、`level_max=1`、`icon_res="ui_research_65"`
  - 投入数量：`id=100600002`、`pre_unlock_ids="100600001"`、`level_max=10`、`pay_crystal="1000,…,10000,"`(逐级)、`icon_res="ui_research_65"`(**占位待替换**)、`position` 需在研究图编辑器微调
- 解锁项 `excel_unlock_info` + `UnlockInfo.txt`：`unlock_type=0`；`id=100600001`/`100600002`
- 多语言：研究名 `Language_ResearchInfo_cn/en`(100600001 开启魔汁机 / 100600002 魔汁机投入数量+1)；UI 文本 `Language_UIText_cn/en`(2009 魔汁机 / 61010 未投入提示 / 61011 开始榨汁 / 61012 超上限提示 / 61014 魔汁使用确认框 / 61015 满级拦截 / 61016 榨汁获得提示 / 61017 经验+{0}道具气泡,12 语种已配)；道具名 `Language_ItemsInfo_cn/en`(200001 魔汁)。**真实源是 excel_language 同名工作表**
- 判定解锁：`userData.GetUserUnlockData().CheckIsUnlock(UnlockEnum.Juicer)`

### 关键文件

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

## 约束

- **UI 驱动**：E键直接开 UICreatureJuicer（不像献祭那样 Logic.PreGame 驱动开UI）；Logic 只负责 Start 之后的榨汁。
- 榨汁演出分层：**Logic 编排时序 → ScenePrefab 演出方法(Begin/Hammer/Drop/End) → CameraHandler 镜头(聚焦/还原/震动)**；奖励结算**统一收在 `CreatureJuicerLogic.SettleJuiceReward()`**（演出后、重开 UI 前调用,保证重开读到最新存档），别散落到 UI；魔汁使用入口在 **UICreatureManager**（魔物管理页道具列表），不在榨汁 UI。
- 建筑显隐**唯一门控** `UnlockEnum.Juicer`；**投入数量上限**门控 `UnlockEnum.JuicerNum`(基础5+每级+1,满级15,改基础值改 `UserLimmitBean.juicerCreatureMax`)；交互碰撞体命名固定 `JuicerInteraction`。
- **镜头 CV_Juicer** 预制带 TrackingTarget(=Juicer建筑根)/CinemachineFollow/RotationComposer/Perlin；榨汁聚焦滴嘴走 `FocusJuicerCameraOnHole/RestoreJuicerCameraFocus` **运行期改字段**，不动预制；返回 UIBaseMain 自动还原基地镜头。
- **多选**投入(`listSelectCreature`)，默认等级降序、已排除上阵/非Idle；计数文本 `ui_LimmitText`(AutoLinkUI 按名绑定,预制需同名子物体)。
- 目标列表卡片态**复用 CreatureAscend 系列**（预制体已如此接线），改卡片变体前先确认预制体挂的脚本。
- 配置改 **Excel 唯一真实源**，同步 JSON 让运行时即时生效；自动生成 Bean/JSON 不手改结构。
- UI 继承 `BaseUIComponent`；输入走 `InputActionUIEnum`（禁用旧版 Input）。
- 研究 `icon_res` 目前是占位（`ui_research_65`），需要专属魔汁机研究图标时**先征得用户同意再用 PixelLab**。
