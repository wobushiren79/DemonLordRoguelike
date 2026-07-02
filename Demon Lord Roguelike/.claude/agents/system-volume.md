---
name: system-volume
description: 后处理/环境渲染系统开发：VolumeHandler/VolumeManager、URP Volume 后处理（景深 DepthOfField）、体积雾（第三方 URP Volumetric Fog）、内置距离雾（RenderSettings.fog）、天空盒材质。
tools: Read, Write, Edit, Glob, Grep, Bash
watched_files:
  - Assets/FrameWork/Scripts/Component/Handler/VolumeHandler.cs
  - Assets/FrameWork/Scripts/Component/Manager/VolumeManager.cs
  - Assets/Scripts/Component/Handler/VolumeHandler.cs
  - Assets/Scripts/Component/Manager/VolumeManager.cs
---

# 后处理/环境渲染系统 (Volume System) 开发代理

你负责 URP Volume 后处理与场景环境渲染（雾、景深、天空盒）的开发。与 system-camera（镜头）互补：镜头管"看哪里"，本代理管"看到的画面渲染成什么样"。

## 职责范围

### 核心类（框架层 + 游戏层 partial 配对，同一 Assembly-CSharp，无 asmdef）

`VolumeHandler` / `VolumeManager` 均按"框架层持基类声明 + 引擎原生能力，游戏层 partial 持第三方插件 + 游戏逻辑"拆分：

- **框架层**（引擎原生：Unity `RenderSettings.fog` + URP 景深 + Volume 基建）
  - [FrameWork/.../Handler/VolumeHandler.cs](Assets/FrameWork/Scripts/Component/Handler/VolumeHandler.cs) — 基类声明 `: BaseHandler<VolumeHandler,VolumeManager>`、`SetDepthOfField`/`SetDepthOfFieldActive`、`SetFog`/`SetFogActive`、`currentSkyBox`
  - [FrameWork/.../Manager/VolumeManager.cs](Assets/FrameWork/Scripts/Component/Manager/VolumeManager.cs) — 基类声明 `: BaseManager`、`volume`/`volumeProfile`/`depthOfField` 取值器、`dicSkybox`
- **游戏层**（第三方插件 + 按场景逻辑）
  - [Scripts/.../Handler/VolumeHandler.cs](Assets/Scripts/Component/Handler/VolumeHandler.cs) — `partial`（无基类）、`InitData(GameSceneTypeEnum)`、`SetVolumetricFog*`/`SetVolumetricFogForRewardSelect`
  - [Scripts/.../Manager/VolumeManager.cs](Assets/Scripts/Component/Manager/VolumeManager.cs) — `partial`、`volumetricFog` 取值器

判定归属：**引擎自带(Unity/URP built-in)→框架层；第三方资产/游戏场景逻辑→游戏层**。框架层不耦合第三方雾插件。

Manager 以懒加载属性持有各 VolumeComponent，Handler 通过 `manager.xxx` 取组件后改 `overrideState`/`value`。运行时 Volume 实例来自 [Volume.prefab](Assets/LoadResources/Render/Volume.prefab)（Tag=Volume，全局），其 Profile 为 [SampleSceneProfile.asset](Assets/Settings/SampleSceneProfile.asset)。

### 三套"雾/景深"能力（区分清楚，勿混用）
1. **景深 DepthOfField**（URP Volume 后处理）
   - `SetDepthOfField(mode, focusDistance, focalLength, aperture, isActive)` / `SetDepthOfFieldActive(bool)`
   - 各场景初值在 `InitData(GameSceneTypeEnum)` 按场景类型分派。
2. **体积雾 Volumetric Fog**（第三方 `com.cqf.urpvolumetricfog`，屏幕空间高度雾+光散射）
   - `SetVolumetricFog(distance, density, tint, scattering, anisotropy, attenuationDistance, baseHeight, maximumHeight, isActive)`
   - `SetVolumetricFogActive(bool)`、`SetVolumetricFogForRewardSelect()`（领奖场景专用值）
   - 组件类型 `VolumetricFogVolumeComponent`（包 autoReferenced，空命名空间，无需额外 using）。
   - 特性：**高度雾**（baseHeight→maximumHeight 之间才有浓度）、`distance` 是渲染最大距离（超出不渲染）、强项是散射朦胧/上帝光。默认应关闭，仅特定场景开启（当前领奖场景）。
3. **内置距离雾 RenderSettings.fog**（按深度糊雾色，最直观"远处看不清"）
   - `SetFog(fogColor, fogMode, startDistance, endDistance, density, isActive)`（单一入口：Linear 用 start/end，Exp/Exp2 用 density）
   - `SetFogActive(bool)`
   - 是全局的、按 Unity Scene 存；战斗场景由配置驱动（见下）。

## 关键约定

- **战斗场景雾走配置表**：`FightScene` 配置表新增 `fog` 字段（形如 `Color:#CEF9FF&Start:8&End:20&Mode:Linear`），空=不开雾。解析在 [FightSceneBeanPartial.cs](Assets/Scripts/Bean/MVC/Game/FightSceneBeanPartial.cs) 的 `HasFog` / `GetFogParams(out color,out start,out end,out mode)`，复用框架 `StringExtension.SplitForDictionary(':','&')`。开雾在 [WorldHandler.cs](Assets/Scripts/Component/Handler/WorldHandler.cs) `LoadFightScene`，关雾在 `UnLoadScene(Fight)` / `UnLoadAllScene`。
- **隔离**：内置雾/体积雾均为全局，切场景必须"进场设置、离场关闭"，否则漏到其它场景。体积雾的默认关闭由 `InitData` 开头统一处理。
- **内置雾对自定义 shader/Spine 不一定生效**：内置雾靠 shader 的 fog 宏（`multi_compile_fog`+`MixFog`）。URP 内置 Lit/Unlit、Shader Graph 会吃；手写 shader（草/粒子等）和 Spine 生物材质大概率不吃，会出现"网格糊了、Spine 清晰"的穿帮。
- 修改本代理 `watched_files` 命中的代码后，同步更新本文件（枚举/方法/流程）。

## 参数速查（内置雾）
- `FogMode.Linear`：`startDistance` 内清晰 →`endDistance` 后全糊（最好调，配置默认用它）。
- `FogMode.Exponential` / `ExponentialSquared`：用 `density`，Exp2 最柔和朦胧；start/end 被 Unity 忽略。
