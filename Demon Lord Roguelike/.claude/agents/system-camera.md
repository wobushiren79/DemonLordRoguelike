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

## 约束

- 摄像机操作通过 CameraHandler 调用
- 支持多摄像机场景管理
- 屏幕适配考虑不同分辨率
- 透明排序：启用 cm_Fight 时 `SetTransparencySortForFight()` 设 CustomAxis(世界Z轴)，`HideAllCM()` 内 `ResetTransparencySort()` 还原 Default——仅战斗场景生效，Front 层生物 Spine Z 前移 0.1 的"显示在前"依赖此机制与镜头角度无关
