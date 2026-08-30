---
name: system-camera
description: 摄像机系统开发：CameraHandler/CameraManager、摄像机控制、屏幕适配。
tools: Read, Write, Edit, Glob, Grep, Bash
watched_files:
  - Assets/FrameWork/Scripts/Component/Handler/CameraHandler.cs
  - Assets/FrameWork/Scripts/Component/Manager/CameraManager.cs
  - Assets/Scripts/Component/Manager/CameraManager.cs
  - Assets/Scripts/Component/Handler/CameraHandler.cs
---

# 摄像机系统 (Camera System) 开发代理

你负责摄像机系统的开发。

## 职责范围

### 摄像机管理
- **CameraHandler** - 摄像机逻辑处理 [FrameWork/Scripts/Component/Handler/CameraHandler.cs](Assets/FrameWork/Scripts/Component/Handler/CameraHandler.cs)
- **CameraManager** - 摄像机资源管理 [FrameWork/Scripts/Component/Manager/CameraManager.cs](Assets/FrameWork/Scripts/Component/Manager/CameraManager.cs)

### 游戏摄像机
- [Scripts/Component/Manager/CameraManager.cs](Assets/Scripts/Component/Manager/CameraManager.cs)
- [Scripts/Component/Handler/CameraHandler.cs](Assets/Scripts/Component/Handler/CameraHandler.cs)
- 基地 CV_List 语义镜头：`SetXxxCamera(priority, isEnable)` 系列转调 `SetCameraForBaseScene`（详见 `camera-system` Skill）
- 运行期聚焦/震动（`#region 魔汁机镜头聚焦/震动`）：`GetBaseSceneCamera(cvName)` 仅查找不改态；`FocusJuicerCameraOnHole/RestoreJuicerCameraFocus` 运行期改 CV_Juicer 的 Follow/LookAt/FollowOffset/TargetOffset 做滴嘴特写（缓存还原，`isJuicerCameraFocused` 门控）；`ShakeJuicerCamera` 抬升 Perlin 振幅做冲击震动——同一 CV 运行期改字段的范式，不动预制
- 故事演出镜头**已迁至 Story 系统自管**（CameraHandler 不再有故事 region）：StoryHandler 自管一台专用 CinemachineCamera（纯代码懒创建挂 StoryHandler 常驻 GameObject 下），演出开始从 `CinemachineBrain.ActiveVirtualCamera` 复制 Lens/FollowOffset/TrackerSettings/TargetOffset/Damping，停靠原虚拟相机（仅改激活态，Follow/LookAt 全程不动）后 blend=0 瞬切，移动补间 Story 自有锚点，结束回位后还原激活态与默认混合时长——详见 `story-system` Skill

## 约束

- 摄像机操作通过 CameraHandler 调用
- 支持多摄像机场景管理
- 屏幕适配考虑不同分辨率
- 透明排序：启用 cm_Fight 时 `SetTransparencySortForFight()` 设 CustomAxis(世界Z轴)，`HideAllCM()` 内 `ResetTransparencySort()` 还原 Default——仅战斗场景生效，Front 层生物 Spine Z 前移 0.1 的"显示在前"依赖此机制与镜头角度无关
