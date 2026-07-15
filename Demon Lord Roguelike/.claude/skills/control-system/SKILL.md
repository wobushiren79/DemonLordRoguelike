---
name: control-system
description: Demon Lord Roguelike 游戏的控制系统(Control)开发指南。使用此SKILL当需要创建或修改玩家角色/镜头控制，包括基地场景角色移动与场景交互(ControlForGameBase：WASD移动/Spine走路动画/走路声/场景边界CheckSceneBoard/E键交互开核心·传送门·终焉议会·成就石碑)、战斗场景镜头拖拽与放卡删卡拾晶(ControlForGameFight：WASD或右键拖拽移镜头/ClampCameraPosition限位/左键放卡PutCard·拾水晶/C键删除生物模式)、控制切换(GameControlHandler.SetFightControl/SetBaseControl、GameControlManager.EnableAllControl)、BaseControl 基类 enabledControl 互斥开关、ControlInteractionEnum 交互枚举、走 Player 输入映射(InputActionPlayerEnum，区别于 UI 的 InputActionUIEnum)等。
watched_files:
  - Assets/FrameWork/Scripts/Component/Control/BaseControl.cs
  - Assets/Scripts/Component/Game/Control/ControlForGameBase.cs
  - Assets/Scripts/Component/Game/Control/ControlForGameFight.cs
  - Assets/Scripts/Component/Handler/GameControlHandler.cs
  - Assets/Scripts/Component/Manager/GameControlManager.cs
  - Assets/Scripts/Enums/GameStateEnum.cs
---

# 控制系统 (Control System) 开发指南

## 核心概念

「控制系统」= 玩家在**世界场景**中的角色/镜头输入控制，与 UI 层输入是两套并行体系：

- **UI 输入**：`InputActionUIEnum` + `OnInputActionForStarted`（见输入处理规则）。
- **世界控制输入**：`InputActionPlayerEnum` + `InputAction` 回调（本系统）。

逻辑通过 `BaseHandler<GameControlHandler, GameControlManager>` 配对模式组织；具体控制器是挂在同一 GameObject 上、继承 `BaseControl` 的组件，靠 `enabledControl` 开关**互斥启用**（同一时刻只有一个控制器活跃）。

```
GameControlHandler   - 对外 API(单例 GameControlHandler.Instance)
GameControlManager   - 资源管理：加载控制预制(PathInfo.ControlDataPath)，持有控制目标，懒加载控制器
BaseControl          - 框架层基类(partial)：enabledControl 字段 + virtual EnabledControl(bool)
ControlForGameBase   - 基地场景：角色移动 + 场景交互(E 键)
ControlForGameFight  - 战斗场景：镜头拖拽 + 放卡/删卡/拾晶(鼠标左右键 + C 键)
controlTargetForEmpty    - 战斗镜头锚点(空物体)
controlTargetForCreature - 基地可控角色(子节点 Renderer=Spine, Interaction=交互提示)
```

### 分层文件速查表

| 文件 | 层 | 职责 |
|------|----|----|
| [BaseControl.cs](Assets/FrameWork/Scripts/Component/Control/BaseControl.cs) | 框架 | 基类：`enabledControl` + `virtual EnabledControl(bool)` |
| [GameControlHandler.cs](Assets/Scripts/Component/Handler/GameControlHandler.cs) | 游戏 | 控制切换：`SetFightControl`/`SetBaseControl`/`AnimForBaseControlShow` |
| [GameControlManager.cs](Assets/Scripts/Component/Manager/GameControlManager.cs) | 游戏 | 加载/持有控制目标、懒加载控制器、`EnableAllControl` |
| [ControlForGameBase.cs](Assets/Scripts/Component/Game/Control/ControlForGameBase.cs) | 游戏 | 基地角色移动/交互 |
| [ControlForGameFight.cs](Assets/Scripts/Component/Game/Control/ControlForGameFight.cs) | 游戏 | 战斗镜头/放卡/删卡 |
| [AfterimageGhostMesh.cs](Assets/FrameWork/Scripts/Component/Other/AfterimageGhostMesh.cs) | **框架** | 冲刺残影用的框架层通用网格残影(原 DashGhostSpawner,已移入 FrameWork/Component/Other 并去 Spine 耦合)。控制层只 `Init(skeletonAnimation.gameObject)`+`StartSpawn` 使用，实现细节属 framework-core 域 |
| [GameStateEnum.cs](Assets/Scripts/Enums/GameStateEnum.cs) | 游戏 | `ControlInteractionEnum` 交互枚举 |

## 控制切换

```csharp
// 进入战斗场景：只启用战斗控制
GameControlHandler.Instance.SetFightControl();

// 进入基地场景：启用基础移动控制(isHideControlTarget 控制禁用时是否隐藏角色)
GameControlHandler.Instance.SetBaseControl(isEnable: true, isHideControlTarget: true);

// 打开界面时挂起、关闭后恢复(典型模式)
GameControlHandler.Instance.manager.controlForGameBase.EnabledControl(false);
```

`EnableAllControl(false)` 会先隐藏两个控制目标，再遍历 `listControl` 逐个禁用——**切换控制前必先全关，再启用目标控制器**。

## 基地场景控制（ControlForGameBase）

| 环节 | 位置 | 要点 |
|------|------|------|
| 移动 | `FixedUpdate → HandleForMoveUpdate` | 读 `inputActionMove`，按生物 `MSPD` 经 `MathUtil.InterpolationLerp(msp,0,100,2,5)` 映射速度；`CheckSceneBoard`(BOX 边界)拦边界；按 x 翻转 localScale |
| 动画 | `PlayAnimForControlTarget` | `SpineAnimationStateEnum`(Idle/Walk)，去重避免重复切；走路动画速度 = `moveSpeedFinal*0.8` |
| 走路声 | 移动/静止/禁用三处 | 移动 `PlayLoopSound(sound_walk_1, pitch:1.5f)`（加快脚步节奏, 1.5 倍速）；静止、禁用、打开界面均 `StopLoopSound`（幂等） |
| 朝向 | `dashFacing` | 水平面朝向，默认进入基地朝上 `(0,0,1)`；移动时更新为最近移动方向；`EnabledControl` 切换时重置为默认朝上 |
| 空格突进 | `HandleForDashDown`(Jump/Space) + `HandleForDashUpdate`(FixedUpdate) | 研究门控：`UnlockEnum.SpaceDash` 等级>0 才可突进，距离=等级×`dashDistancePerLevel`(=1.5，1/2/3级=1.5/3/4.5单位)，`dashDuration`(0.2s) 内**逐帧移动非瞬移**；命中 `CheckSceneBoard`/`CheckDashObstacle`(Obstacle层) 即 `EndDash` 停住不穿建筑/出界 |
| 突进冷却 | `HandleForDashCdUpdate`(Update) | `dashCdRemain` 起始 = `GetUnlockSpaceDashCD()`(默认3s，`UnlockEnum.SpaceDashCD` 每级-0.5s，最低1s；数值在 UserUnlockBean 的 `SPACE_DASH_CD_BASE/PER_LEVEL/MIN` 常量)，每帧递减，归零可再突进；突进期间 FixedUpdate 接管移动忽略常规 WASD |
| 突进特效 | `ShowDashBurstEffect` + `AfterimageGhostMesh` | 起步 `EffectBodySlam_1` 冲击(EffectBean 直构、`isDestoryPlayEnd=false` 走对象池)；**已去掉 `EffectSmoke_1` 烟雾拖尾**。**恶魔城式残影** = 框架层 `AfterimageGhostMesh` 对 Spine `Renderer` 网格快照+材质淡出(MPB 覆盖 `_Color`、PMA 整体乘)压身后半透明虚影，`HandleForDashDown` StartSpawn / `EndDash`·`CancelDash` StopSpawn |
| 残影对象池 | `AfterimageGhostMesh`(框架层,基类 `AfterimageGhostBase` 管 `listActive`/`poolIdle`/`listAll`) | 残影淡出即 `Recycle` 回池复用**不销毁**(频繁突进不反复 Instantiate/Destroy)；网格每对象各持一份 `Mesh`、`CopyMesh` 原地刷新；材质共享角色 `sharedMaterials`(不克隆)。**数量按突进等级**：`StartSpawn(count,duration)`，`count=dashLevel*dashGhostCountPerLevel(3)`(1级3/3级9)，`spawnInterval=duration/count` 均匀铺满。**清理在 `EnabledControl(false)`**(打开界面/切场景经 `EnableAllControl` 都会走到)：`_dashGhost.ClearAll()`；`OnDestroy` 兜底 |
| 交互探测 | `Update → HandleForInteraction` | 每 `0.2s`(timeMaxForInteraction) `RayUtil.OverlapToSphere` 探 `LayerInfo.Interaction`，命中显示提示气泡 |
| 交互触发 | `HandleForUseEUp` | E 键抬起 → `GetInteractionEnum` → 按 `ControlInteractionEnum` 开 UI/调逻辑，命中有效物体播 `sound_btn_1` |
| 交互提示文本 | `GetInteractionEnumName` | `textId = 2000 + (int)枚举` 走 `TextHandler.GetTextById` |

> **空格突进研究**（强化类）：解锁研究 `UnlockEnum.SpaceDash`(200600001，3级) 决定突进距离，子研究 `UnlockEnum.SpaceDashCD`(200700001，4级) 缩短 CD；运行时读取 `GameDataHandler.Instance.manager.GetUserData().GetUserUnlockData().GetUnlockSpaceDashLevel()/GetUnlockSpaceDashCD()`。研究节点/解锁配置属 **research-system** 领域。

**交互枚举 → 动作映射**（`HandleForUseEUp` 内 switch）：

| ControlInteractionEnum | 动作 |
|------------------------|------|
| CoreInteraction | 打开 `UIBaseCore`（核心） |
| PortalInteraction | 打开 `UIBasePortal`（传送门） |
| DoomCouncilInteraction | 打开 `UIDoomCouncilBill`（终焉议会入口） |
| DoomCouncilPodium | `DoomCouncilLogic.InteractPodium()`（讲台） |
| Councilor | `DoomCouncilLogic.InteractCouncilor(go)`（议员） |
| AchievementInteraction | 打开 `UIAchievement`（退出直接回 `UIBaseMain`） |
| VatInteraction | 打开 `UICreatureVat`（魔物进阶容器，退出直接回 `UIBaseMain`；枚举值=8跳过7，因提示文本 textId=2000+值=2008，避开2007已占用） |
| JuicerInteraction | 打开 `UICreatureJuicer`（魔汁机/魔物回收，退出直接回 `UIBaseMain`；枚举值=9，提示文本 textId=2009；交互碰撞体命名 `JuicerInteraction`。详见 juicer-system Skill） |

> 交互物体 GameObject 名字即枚举名；带 `_UUID` 后缀的（如 `Councilor_xxx`）只取下划线前段；`Enum.TryParse` 失败回退 `None`。
> 由场景交互打开的 UI（成就/魔物进阶）用注入式退出回调 `actionForExit` 决定退出去向，`OnClickForExit` 只 `actionForExit?.Invoke()`：**各入口自行注入**——场景交互入口注入回 `UIBaseMain`，基地核心/测试入口注入回 `UIBaseCore`（不再有 UI 内的默认分支）。

## 战斗场景控制（ControlForGameFight）

| 输入 | 处理 | 行为 |
|------|------|------|
| WASD (`inputActionMove`) | `HandleForMoveUpdate` | 移动 `controlTargetForEmpty`，`ClampCameraPosition` 限位(minX/maxX/minZ/maxZ) |
| 鼠标右键拖拽 (`UseR`) | `HandleForMoveMouseUpdate` + Down/Up | 按下记 `dragCameraOrigin` 进拖拽；按位移移镜头；抬起退出拖拽 |
| 鼠标左键长按 (`UseL`) | `HandleForClickDropUpdate` | 手里无卡/非删除模式时 `PickupCrystalForMouse()` 拾水晶 |
| 鼠标左键抬起 | `HandleForUseLUp` | 手持卡 → `PutCard()`；删除模式 → `SelectCreatureDestoryHandle()` |
| 鼠标右键抬起 | `HandleForUseRUp` | 手持卡 → `UnSelectCard()`；删除模式 → `UnSelectCreatureDestroy()` |
| C 键 (`inputActionDeleteCreature`) | `HandleForDeleteCreatureToggle` | 切换删除生物模式(`SelectCreatureDestroy`/`UnSelectCreatureDestroy`) |

战斗交互均通过 `GameHandler.Instance.manager.GetGameLogic<GameFightLogic>()` 落到战斗逻辑；`CheckUtil.CheckIsPointerUI()` 拦截 UI 上的点击。

## 输入接线规范（重要）

控制器在 `Awake` 里从 **Player 映射**取 `InputAction` 并订阅回调，`OnDestroy` 反订阅：

```csharp
inputActionUseE = InputHandler.Instance.manager.GetInputPlayerData(InputActionPlayerEnum.E);
inputActionUseE.started  += HandleForUseEDown;
inputActionUseE.canceled += HandleForUseEUp;
```

- **禁止旧版 `Input` API**（`Input.GetKeyDown`/`GetMouseButton`/`GetAxis` 等已全部迁移为 InputAction）。
- **新增控制按键**：① 在 `GameInputActions.inputactions` 的 **Player** 映射加绑定 → ② 在 `InputActionPlayerEnum`（[BaseGameEnum.cs](Assets/FrameWork/Scripts/Enums/BaseGameEnum.cs)：`Move/MoveMouse/Look/UseL/UseR/E/Jump/B/Q/C/Shift/Ctrl/ShortcutsSelect/CameraDistance`）补枚举 → ③ 在控制器 `Awake` 订阅、`OnDestroy` 反订阅。
- 每个 Update/回调开头 `if (!enabledControl) return;` 守卫，避免挂起态误响应。

## 代码规范

- 方法/属性加 `/// <summary>` XML 注释，用 `#region`/`#endregion` 分类；方法体内注释尽量单行。
- 改动本 SKILL `watched_files` 命中的代码时，同步更新本文件与 [game-control agent](.claude/agents/game-control.md)。

## 跨模块转交
- 交互打开的 UI（UIBaseCore/UIBasePortal/UIDoomCouncilBill/UIAchievement）→ **ui-framework** / `ui-game`。
- 放卡/删卡/拾晶落地逻辑（GameFightLogic）→ **game-fight-system**。
- 终焉议会讲台/议员交互（DoomCouncilLogic）→ **doom-council-system**。
- Spine 动画 → **spine-system**；走路声/交互音效 → **audio-system**；冲刺起步冲击粒子(EffectHandler/EffectBean) → **effect 系统**；残影用的框架层通用组件 `AfterimageGhostMesh`/`AfterimageGhostBase`(FrameWork/Component/Other) → **framework-core**（控制层只调 Init/StartSpawn/ClearAll）。
- 虚拟相机混合切换 → **camera-system**（本系统只移动镜头锚点，不管 Cinemachine 混合）。
