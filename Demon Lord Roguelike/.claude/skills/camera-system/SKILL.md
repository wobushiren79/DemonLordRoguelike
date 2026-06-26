---
name: camera-system
description: Demon Lord Roguelike 游戏的摄像机(Camera)系统开发指南。使用此SKILL当需要创建或修改摄像机控制、Cinemachine 虚拟相机切换、场景镜头(战斗/基地/终焉议会/奖励选择/卡片测试)、跟随目标、镜头混合动画(Blend)、FieldOfView、屏幕适配等，包括 CameraHandler/CameraManager(框架层+游戏层 partial)、CinemachineCamera(cm_Fight/cm_Base)、CinemachineBrain、CV_List 场景虚拟相机组(CV_Core/CV_Portal/CV_GashaponMachine 等)、CinemachineCameraEnum、SetCameraForControl/SetCameraForBaseScene、HideAllCM、SetMainCameraDefaultBlend、GetDistanceFollow 等。
watched_files:
  - Assets/FrameWork/Scripts/Component/Handler/CameraHandler.cs
  - Assets/FrameWork/Scripts/Component/Manager/CameraManager.cs
  - Assets/Scripts/Component/Handler/CameraHandler.cs
  - Assets/Scripts/Component/Manager/CameraManager.cs
  - Assets/Scripts/Enums/GameStateEnum.cs
---

# 摄像机系统 (Camera System) 开发指南

## 核心概念

本项目摄像机基于 **Unity Cinemachine 3.x**（`Unity.Cinemachine` 命名空间，类型为 `CinemachineCamera` / `CinemachineBrain` / `CinemachineFollow`）。
逻辑通过 `BaseHandler<CameraHandler, CameraManager>` 配对模式组织，框架层与游戏层各有一个 `partial` 文件。

```
CameraHandler   - 摄像机逻辑处理器（对外 API，单例 CameraHandler.Instance）
CameraManager   - 摄像机资源管理器（持有 Camera / CinemachineCamera / CinemachineBrain 引用）
CinemachineBrain- 主摄像机上的大脑，负责在多个虚拟相机间混合切换
cm_Fight        - 战斗跟随虚拟相机（CMFollow）
cm_Base         - 基地跟随虚拟相机（CMBase）
CV_List         - 各场景预制体下的虚拟相机组（按用途命名的多个 CinemachineCamera）
```

### 分层文件速查表

| 文件 | 层 | 职责 |
| --- | --- | --- |
| [Assets/FrameWork/Scripts/Component/Handler/CameraHandler.cs](Assets/FrameWork/Scripts/Component/Handler/CameraHandler.cs) | 框架 | 通用逻辑：`ChangeAngleForCamera`、`GetDistanceFollow` |
| [Assets/FrameWork/Scripts/Component/Manager/CameraManager.cs](Assets/FrameWork/Scripts/Component/Manager/CameraManager.cs) | 框架 | `mainCamera` / `uiCamera` 懒加载属性 |
| [Assets/Scripts/Component/Handler/CameraHandler.cs](Assets/Scripts/Component/Handler/CameraHandler.cs) | 游戏 | 各场景镜头切换 API（战斗/基地/议会/奖励/卡片测试/控制） |
| [Assets/Scripts/Component/Manager/CameraManager.cs](Assets/Scripts/Component/Manager/CameraManager.cs) | 游戏 | `cm_Fight`/`cm_Base`/`cinemachineBrain` 引用与加载、`HideAllCM`、`SetMainCameraDefaultBlend` |
| [Assets/Scripts/Enums/GameStateEnum.cs](Assets/Scripts/Enums/GameStateEnum.cs) | 游戏 | `CinemachineCameraEnum` 枚举 |

> 提示：`CameraHandler` / `CameraManager` 都是 `partial` 类，框架层与游戏层共同组成同一个类。修改时按职责选对应层的文件。

## 摄像机枚举

```csharp
// Assets/Scripts/Enums/GameStateEnum.cs
public enum CinemachineCameraEnum
{
    None,
    Base,   // 基地控制镜头 cm_Base
    Fight,  // 战斗控制镜头 cm_Fight
}
```

## 初始化流程

```csharp
// 游戏启动时调用，加载主摄像机预制体
CameraHandler.Instance.InitData();   // -> manager.LoadMainCamera()
```

`LoadMainCamera()` 通过 `LoadAddressablesUtil.LoadAssetSync<GameObject>(PathInfo.CameraDataPath)` 实例化摄像机预制体，并缓存：
- `MainCamera` 节点 → `mainCamera`（同时取其上的 `CinemachineBrain`）
- `CMFollow` 节点 → `cm_Fight`
- `CMBase` 节点 → `cm_Base`

## 关键 API

### 1. 控制镜头切换（战斗 / 基地）

```csharp
// 切换到指定控制镜头（先 HideAllCM 再启用目标）
CameraHandler.Instance.SetCameraForControl(CinemachineCameraEnum.Fight);
CameraHandler.Instance.SetCameraForControl(CinemachineCameraEnum.Base);
```
- `Base`：启用 `cm_Base`，并按当前场景类型设置 `Lens.FieldOfView`（普通基地 55、终焉议会 50）。
- `Fight`：启用 `cm_Fight`，`Priority = int.MaxValue`。

### 2. 初始化场景镜头

```csharp
// 战斗场景：跟随空目标 controlTargetForEmpty
await CameraHandler.Instance.InitFightSceneCamera();

// 基地场景：跟随生物，传入生物数据与起始位置
await CameraHandler.Instance.InitBaseSceneControlCamera(creatureData, startPosition);
```
两者都会 `SetMainCameraDefaultBlend(0)` 关闭切换动画后再设置 `Follow`/`LookAt`，并把 `PreviousStateIsValid = false` 以避免上一镜头插值残留。

### 3. 基地场景内的 CV_List 虚拟相机切换

基地（及其它场景预制体）下挂有名为 `CV_List` 的节点，内含多个按用途命名的 `CinemachineCamera`。
通过统一入口 `SetCameraForBaseScene(priority, isEnable, cvName, blendTime)` 切换，已封装了一组语义 API：

```csharp
CameraHandler.Instance.SetBaseCoreCamera(priority, isEnable);        // CV_Core 核心
CameraHandler.Instance.SetBasePortalCamera(priority, isEnable);      // CV_Portal 传送门(blend=0)
CameraHandler.Instance.SetAchievementCamera(p, e);                  // CV_Achievement 成就(UIAchievement 打开时切换)
CameraHandler.Instance.SetCreatureSacrificeCamera(p, e);            // CV_CreatureSacrifice 献祭
CameraHandler.Instance.SetCreatureVatCamera(p, e);                  // CV_CreatureVat 生物容器
CameraHandler.Instance.SetGashaponMachineCamera(p, e);             // CV_GashaponMachine 扭蛋机
CameraHandler.Instance.SetGashaponBreakCamera(p, e);               // CV_GashaponBreak 扭蛋破碎
CameraHandler.Instance.SetGameStartCamera(p, e);                   // CV_GameStart 游戏开始
CameraHandler.Instance.SetPreviewCreateCamera(p, e);              // CV_PreviewCreate 创建预览
CameraHandler.Instance.SetCustomCamera(p, e);                     // CV_Custom 自定义
```

> 新增一个基地子镜头：在场景预制体的 `CV_List` 下放一个命名为 `CV_Xxx` 的 `CinemachineCamera`，再在游戏层 `CameraHandler` 的「基地场景摄像头相关」region 里加一个语义方法转调 `SetCameraForBaseScene(priority, isEnable, "CV_Xxx")` 即可。

### 4. 其它场景镜头

```csharp
// 终焉议会投票镜头：取 DoomCouncil 场景 CV_List 下的 CinemachineCamera
CameraHandler.Instance.SetCameraForDoomCouncilVote(blendTime: 0.5f);

// 奖励选择场景镜头
CameraHandler.Instance.SetCameraForRewardSelectScene(blendTime: 0.5f);

// 卡片测试镜头（只用主相机，关闭混合动画）
CameraHandler.Instance.SetCardTestCamera();
```

### 5. 混合动画 / 隐藏 / 工具

```csharp
// 隐藏所有受管虚拟相机（cm_Fight + cm_Base）
CameraHandler.Instance.manager.HideAllCM();

// 设置主相机默认混合（切换）动画：time=0 即瞬切
CameraHandler.Instance.manager.SetMainCameraDefaultBlend(0.5f);
CameraHandler.Instance.manager.SetMainCameraDefaultBlend(0.5f, CinemachineBlendDefinition.Styles.EaseInOut);

// 根据摄像机角度修正物体角度（当前实现：归零 eulerAngles）
CameraHandler.Instance.ChangeAngleForCamera(target);

// 获取虚拟相机跟随距离（读 CinemachineFollow.FollowOffset 的模长）
float dist = CameraHandler.Instance.GetDistanceFollow(cinemachineCamera);
```

## 常见任务流程

### 切换镜头的标准范式
1. `manager.HideAllCM()`（或由 `SetCameraForBaseScene` 内部遍历关闭其它 CV）。
2. `manager.SetMainCameraDefaultBlend(blendTime)` 设置过渡（瞬切用 0）。
3. 启用目标 `CinemachineCamera.gameObject.SetActive(true)`。
4. 设置 `Priority`（抢镜用 `int.MaxValue`，关闭用 0）。
5. 跟随类镜头设置 `Follow` / `LookAt`，并把 `PreviousStateIsValid = false`。

### 新增一种「场景控制镜头」（类似 Base/Fight）
1. 在 `CinemachineCameraEnum` 增枚举值。
2. 摄像机预制体加一个 `CinemachineCamera` 子节点，在 `CameraManager.LoadMainCamera()` 缓存引用。
3. 在 `CameraManager.HideAllCM()` 补充隐藏。
4. 在 `CameraHandler.SetCameraForControl` 的 `switch` 增分支与对应 `SetCameraForControlXxx()`。

## 约束与注意事项

- **摄像机操作统一走 `CameraHandler`**，不要在业务代码里直接拿 `Camera.main` 或散落操作 `CinemachineCamera`。
- **Cinemachine 版本为 3.x**：用 `CinemachineCamera`（非旧版 `CinemachineVirtualCamera`）、`CinemachineFollow`（非 `CinemachineTransposer`）、`Lens.FieldOfView`、`cinemachineBrain.DefaultBlend`。
- **混合动画**：需要瞬切（初始化、传送门）时务必先 `SetMainCameraDefaultBlend(0)`，否则会有残留插值。
- **跟随重置**：切换跟随目标后设 `PreviousStateIsValid = false`，避免镜头从上一位置滑入。
- **场景虚拟相机依赖命名约定**：`CV_List` 容器 + `CV_Xxx` 子节点名必须与代码里的字符串一致，改名需同步代码。
- **代码规范**：所有方法/属性加 `/// <summary>` XML 注释，并用 `#region`/`#endregion` 按场景用途分类（现有文件已按「战斗/基地/议会/奖励/卡片测试/控制」分区）。
- **Bean 规则**：若涉及自动生成的 `*Bean.cs`，扩展写在对应 `*BeanPartial.cs`。
- **Unity 资源修改**（摄像机预制体、CV 节点）必须通过 Unity MCP，禁止直接编辑 `.prefab`。
