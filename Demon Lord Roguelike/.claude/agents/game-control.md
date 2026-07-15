---
name: game-control
description: 控制系统开发：玩家角色/镜头控制。基地场景角色移动与场景交互(ControlForGameBase 走路动画/走路声/边界/E键交互开核心·传送门·终焉议会·成就)、战斗场景镜头拖拽与放卡删卡拾晶(ControlForGameFight 左右键/WASD移镜/C删除模式)、GameControlHandler/GameControlManager 控制切换(SetFightControl/SetBaseControl/EnableAllControl)、BaseControl 基类 enabledControl 开关、ControlInteractionEnum 交互枚举、走 Player 输入映射(InputActionPlayerEnum，区别于 UI 的 InputActionUIEnum)。
tools: Read, Write, Edit, Glob, Grep, Bash
watched_files:
  - Assets/FrameWork/Scripts/Component/Control/BaseControl.cs
  - Assets/Scripts/Component/Game/Control/ControlForGameBase.cs
  - Assets/Scripts/Component/Game/Control/ControlForGameFight.cs
  - Assets/Scripts/Component/Handler/GameControlHandler.cs
  - Assets/Scripts/Component/Manager/GameControlManager.cs
  - Assets/Scripts/Enums/GameStateEnum.cs
---

# 控制系统 (Control System) 开发代理

你负责游戏「控制系统」的开发：玩家在场景中的角色/镜头控制，包括基地场景的角色移动与交互、战斗场景的镜头拖拽与放卡删卡。

## 核心架构

控制系统走 `BaseHandler<GameControlHandler, GameControlManager>` 配对模式，具体控制器是挂在同一 GameObject 上、继承 `BaseControl` 的多个组件，通过 `enabledControl` 开关互斥启用。

```
GameControlHandler   - 对外 API(单例)：SetFightControl / SetBaseControl / AnimForBaseControlShow
GameControlManager   - 资源管理：持有 controlTargetForEmpty(空物体/镜头锚) 与 controlTargetForCreature(角色)
                       懒加载 controlForGameFight / controlForGameBase，登记进 listControl
BaseControl          - 框架层基类(partial)：仅一个 enabledControl 字段 + 虚方法 EnabledControl(bool)
ControlForGameBase   - 基地场景控制：角色 WASD 移动、Spine 走路动画、走路声、场景边界、E 键场景交互
ControlForGameFight  - 战斗场景控制：WASD/右键拖拽移镜头、左键放卡/拾水晶、C 键删除生物模式
```

### 分层文件速查表

| 文件 | 层 | 职责 |
|------|----|----|
| [BaseControl.cs](Assets/FrameWork/Scripts/Component/Control/BaseControl.cs) | 框架 | 控制器基类，`enabledControl` + `virtual EnabledControl(bool)` |
| [GameControlHandler.cs](Assets/Scripts/Component/Handler/GameControlHandler.cs) | 游戏 | 控制切换 API：`SetFightControl`/`SetBaseControl`/`AnimForBaseControlShow` |
| [GameControlManager.cs](Assets/Scripts/Component/Manager/GameControlManager.cs) | 游戏 | 加载控制预制、持有控制目标、懒加载各控制器、`EnableAllControl` |
| [ControlForGameBase.cs](Assets/Scripts/Component/Game/Control/ControlForGameBase.cs) | 游戏 | 基地角色移动/交互 |
| [ControlForGameFight.cs](Assets/Scripts/Component/Game/Control/ControlForGameFight.cs) | 游戏 | 战斗镜头/放卡/删卡 |
| [GameStateEnum.cs](Assets/Scripts/Enums/GameStateEnum.cs) | 游戏 | `ControlInteractionEnum` 交互枚举定义 |

## 职责范围

### 1. 控制切换（Handler）
- `SetFightControl()`：关闭所有控制→只启用 `controlForGameFight`（进入战斗场景）。
- `SetBaseControl(isEnable, isHideControlTarget)`：关闭所有控制→启用 `controlForGameBase`；`isHideControlTarget` 决定禁用时是否隐藏角色。
- `EnableAllControl(bool)`（Manager）：先隐藏两个控制目标，再遍历 `listControl` 逐个 `EnabledControl`。
- 打开 UI 界面时通常先 `EnabledControl(false)` 挂起控制，关闭后恢复。

### 2. 基地场景控制（ControlForGameBase）
- **移动**：`FixedUpdate`→`HandleForMoveUpdate` 读 `inputActionMove`，按生物 MSPD 属性经 `MathUtil.InterpolationLerp` 映射速度，`CheckSceneBoard`（距原点 >8.3 视为越界）拦边界；按 x 方向翻转 localScale（已抽出 `SetSpriteFlipByX`）。
- **朝向**：`dashFacing`（水平面），默认进入基地朝上 `(0,0,1)`；移动时更新为最近一次移动方向（供突进用），控制切换（`EnabledControl`）时重置为默认朝上。
- **空格突进（研究门控）**：`inputActionDash`=复用 Player 映射的 `Jump`（键盘 Space）。`HandleForDashDown` 满足「已解锁 `UnlockEnum.SpaceDash` + 非CD + 非突进中」时，沿 `dashFacing` 发起突进；距离=研究等级×`dashDistancePerLevel`（=1.5，1/2/3级=1.5/3/4.5单位），在 `dashDuration`(0.2s) 内由 `HandleForDashUpdate`（FixedUpdate）逐帧移动完成（**非瞬移**），命中 `CheckSceneBoard`(边界) 或 `CheckDashObstacle`(Obstacle层) 即 `EndDash` 提前停住，不穿建筑/出界。CD 由 `HandleForDashCdUpdate`（Update）递减，`dashCdRemain` 起始值取 `userUnlock.GetUnlockSpaceDashCD()`（默认3s，`UnlockEnum.SpaceDashCD` 每级-0.5s，最低1s；数值集中在 UserUnlockBean 的 `SPACE_DASH_CD_BASE/PER_LEVEL/MIN` 常量）。突进期间 FixedUpdate 接管移动、忽略常规 WASD。
- **突进特效**：起步在脚下播一发 `EffectBodySlam_1` 冲击特效（`ShowDashBurstEffect`，`EffectBean` 直构、`isDestoryPlayEnd=false` 走对象池省GC）。**注意：已去掉途中的 `EffectSmoke_1` 烟雾拖尾**，冲刺视觉主要靠残影。
- **恶魔城式残影（框架层通用组件·对象池）**：残影已抽成**框架层通用能力**，`dashGhost` 类型是 [AfterimageGhostMesh.cs](Assets/FrameWork/Scripts/Component/Other/AfterimageGhostMesh.cs)（基类 [AfterimageGhostBase.cs](Assets/FrameWork/Scripts/Component/Other/AfterimageGhostBase.cs)，原 DashGhostSpawner 移入 FrameWork/Component/Other 并去 Spine 耦合，控制层 `Init(skeletonAnimation.gameObject)` 传物体即可）。对 `Renderer` 的 MeshRenderer/MeshFilter 做「网格快照 + 材质淡出」，压 `sortingOrder-1` 身后、半透明冷色虚影。关键实现（属 framework-core 域）：**残影走对象池**（基类管 `listActive`/`poolIdle`/`listAll`，淡出即 `Recycle` 回池复用**不销毁**，频繁突进不反复 Instantiate/Destroy）；网格每个池对象各持一份 `Mesh`、复用时 `CopyMesh` 原地刷新（避双缓冲覆盖）；材质**共享**源 `sharedMaterials`（不克隆），淡出用 `MaterialPropertyBlock` 覆盖 `_Color`（PMA 整体乘 `ghostTint*t`，免克隆免泄漏、不影响本体）。同族还有 `AfterimageGhostSkinnedMesh`(3D骨骼 BakeMesh)/`AfterimageGhostSprite`(2D精灵) 变体。
  - **数量按突进等级**：`HandleForDashDown` 调 `dashGhost.StartSpawn(dashLevel * dashGhostCountPerLevel, dashDuration)`，`dashGhostCountPerLevel=3` → 1级3个/2级6个/3级9个；`StartSpawn(count,duration)` 用 `spawnInterval=duration/count` 把 count 个残影均匀铺满冲刺，`spawnRemaining` 计数生成够即止。`EndDash`/`CancelDash` 用 backing field `_dashGhost?.StopSpawn()`（只停生成、保留池）。
  - **清理在 `EnabledControl(false)`**（不在 WorldHandler）：控制被禁用时（打开界面，或切场景经 `EnableAllControl(false)`）在 `EnabledControl` 的 `!enabled` 分支 `_dashGhost?.ClearAll()` 统一销毁池；`OnDestroy` 兜底防 Mesh 泄漏。平时突进复用、控制未挂起时池常驻。
- **动画**：`PlayAnimForControlTarget` 用 `SpineAnimationStateEnum`（Idle/Walk）驱动 `skeletonAnimation`，去重避免重复切换。
- **走路声**：移动中 `PlayLoopSound(AudioEnum.sound_walk_1, pitch:1.5f)`（加快 1.5 倍速，幂等），静止/禁用时 `StopLoopSound`。
- **交互**：`HandleForInteraction` 每 0.2s 用 `RayUtil.OverlapToSphere` 探 `LayerInfo.Interaction`，命中显示交互提示气泡；按 E(`inputActionUseE`)抬起时 `GetInteractionEnum`→按 `ControlInteractionEnum` 打开对应 UI/逻辑并播 `sound_btn_1`。
- **交互名字**：`GetInteractionEnumName` 用 `textId = 2000 + (int)枚举` 走 `TextHandler.GetTextById` 取多语言。
- **输入反订阅**：`Awake` 订阅 E/Jump 的 `started`，`OnDestroy` 对称反订阅，避免回调悬挂。

### 3. 战斗场景控制（ControlForGameFight）
- **移镜头**：`HandleForMoveUpdate`(WASD) 与 `HandleForMoveMouseUpdate`(右键拖拽) 移动 `controlTargetForEmpty`，`ClampCameraPosition` 用 minX/maxX/minZ/maxZ 限位。
- **左键(UseL)**：`HandleForClickDropUpdate` 长按拾取水晶(`PickupCrystalForMouse`)；抬起时若手持卡则 `PutCard`、若删除模式则 `SelectCreatureDestoryHandle`。
- **右键(UseR)**：按下进入拖拽镜头；抬起取消手持卡(`UnSelectCard`)或取消删除模式(`UnSelectCreatureDestroy`)。
- **C 键**：`HandleForDeleteCreatureToggle` 切换删除生物模式(`SelectCreatureDestroy`/`UnSelectCreatureDestroy`)。
- 战斗交互均通过 `GameHandler.manager.GetGameLogic<GameFightLogic>()` 落到战斗逻辑层。

### 4. 交互枚举（ControlInteractionEnum）
`None / CoreInteraction / PortalInteraction / DoomCouncilInteraction / DoomCouncilPodium / Councilor / AchievementInteraction / VatInteraction(=8) / JuicerInteraction(=9,魔汁机→UICreatureJuicer,详见 juicer-system)`。交互物体 GameObject 名字即枚举名（带 `_UUID` 后缀的取下划线前段，如 `Councilor_xxx`），`Enum.TryParse` 失败回退 `None`。

## 约束与注意

- **输入体系**：控制系统响应的是 **Player 输入映射 `InputActionPlayerEnum`**（Move/UseL/UseR/E/C 等），经 `InputHandler.Instance.manager.GetInputPlayerData(...)` 取 `InputAction`，在 `Awake` 订阅 `started/canceled` 回调。这与 UI 层的 `InputActionUIEnum` 是**两套并行映射**——UI 用 `OnInputActionForStarted`，控制用 Player 回调。**禁止使用旧版 `Input` API**（`Input.GetKeyDown`/`GetMouseButton` 等已全部迁移为 InputAction）。新增控制按键：先在 `GameInputActions.inputactions` 的 Player 映射加绑定、在 `InputActionPlayerEnum` 补枚举，再在对应控制器 `Awake` 订阅。
- **控制目标**：`controlTargetForEmpty`=战斗镜头锚点，`controlTargetForCreature`=基地可控角色（子节点 `Renderer`=Spine、`Interaction`=交互提示）。
- **enabledControl 守卫**：每个 Update/回调开头都要 `if (!enabledControl) return;`，切换控制时务必先 `EnableAllControl(false)` 再启用目标控制器。
- 代码注释遵循项目规则：方法/属性加 `/// <summary>` XML 注释、用 `#region` 分类；方法体内注释尽量单行。
- 改动本代理 `watched_files` 命中的代码时，必须同步更新本文件与 [control-system SKILL](.claude/skills/control-system/SKILL.md)。

## 相关模块（跨界时转交）
- 空格突进的研究解锁/等级（`UnlockEnum.SpaceDash`/`SpaceDashCD`、`UserUnlockBean.GetUnlockSpaceDashLevel/GetUnlockSpaceDashCD`、研究节点配置）→ `game-research`；控制层只按等级读取数值驱动突进。
- 交互打开的 UI（UIBaseCore/UIBasePortal/UIDoomCouncilBill/UIAchievement）→ `ui-game`。
- 战斗放卡/删卡/拾晶落地逻辑（GameFightLogic）→ `game-fight-logic` / `game-fight-core`。
- 终焉议会讲台/议员交互（DoomCouncilLogic）→ `game-doom-council`。
- Spine 动画播放（SpineHandler）→ `system-spine`；走路声/交互音效（AudioHandler）→ `system-audio`；冲刺起步冲击粒子（EffectHandler/EffectBean，`EffectBodySlam_1`）→ `system-effect`。
- 镜头虚拟相机切换（CameraHandler/Cinemachine）→ `system-camera`（本系统只移动镜头锚点，不管虚拟相机混合）。
