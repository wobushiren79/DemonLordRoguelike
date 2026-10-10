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
- 奖励选择镜头：`SetCameraForRewardSelectScene(blendTime)` 切镜；`RefreshRewardSelectCameraFov(halfWidthForBox)` 按宝箱横向总宽度自适应 FOV——宝箱>=6 个时默认 FOV(60) 装不下两侧宝箱，由 `ScenePrefabForRewardSelect.InitRewardBox` 在遮罩盖住期间调用，几何关系 tan(hFov/2)=半宽/相机z距离、tan(hFov/2)=tan(vFov/2)*aspect，只在不够装时才放大
- 运行期聚焦/震动（`#region 魔汁机镜头聚焦/震动`）：`GetBaseSceneCamera(cvName)` 仅查找不改态；`FocusJuicerCameraOnHole/RestoreJuicerCameraFocus` 运行期改 CV_Juicer 的 Follow/LookAt/FollowOffset/TargetOffset 做滴嘴特写（缓存还原，`isJuicerCameraFocused` 门控）；`ShakeJuicerCamera` 抬升 Perlin 振幅做冲击震动——同一 CV 运行期改字段的范式，不动预制
- 故事演出镜头**已迁至 Story 系统自管**（CameraHandler 不再有故事 region）：StoryHandler 自管一台专用 CinemachineCamera（纯代码懒创建挂 StoryHandler 常驻 GameObject 下），演出开始从 `CinemachineBrain.ActiveVirtualCamera` 复制 Lens/FollowOffset/TrackerSettings/TargetOffset/Damping，停靠原虚拟相机（仅改激活态，Follow/LookAt 全程不动）后 blend=0 瞬切，移动补间 Story 自有锚点，结束回位后还原激活态与默认混合时长——详见 `story-system` Skill
- **演出期锁镜头**：`SetCameraForControl` 在 `StoryHandler.Instance.manager.isStoryPlaying` 为 true 时直接 return——外部切镜（如 UIBaseMain/UIDoomCouncilMain 的 OpenUI）会以 blend=0 瞬切抢走 CinemachineBrain，导致演出镜头移动不可见；归还由 StoryHandler.EndStoryCamera 负责

## 约束

- 摄像机操作通过 CameraHandler 调用
- 支持多摄像机场景管理
- 屏幕适配考虑不同分辨率
- 透明排序：`HideAllCM()` 内 `RefreshTransparencySortForCurrentScene()` 按当前场景统一刷新——战斗/基地/终焉议会场景调 `SetTransparencySortForGameScene()` 设 CustomAxis(世界Z轴)，其余场景(主菜单/奖励选择等)调 `ResetTransparencySort()` 还原 Default；场景类型经 `WorldHandler.GetCurrentSceneType()` 反查 `dicCurrentScene` 获得（战斗场景无 ScenePrefabBase 组件，不能靠组件识别）。所有镜头切换路径都先经 `HideAllCM`，故排序始终匹配当前场景；Front 层生物 Spine Z 前移 0.1 的"显示在前"依赖此机制与镜头角度无关
