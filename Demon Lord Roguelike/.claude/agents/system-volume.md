---
name: system-volume
description: 后处理/环境渲染系统开发：VolumeHandler/VolumeManager、URP Volume 后处理（景深 DepthOfField）、体积雾（第三方 URP Volumetric Fog）、内置距离雾（RenderSettings.fog）、天空盒材质、战斗场景环境光（ambient_light 配置驱动）。
tools: Read, Write, Edit, Glob, Grep, Bash
watched_files:
  - Assets/FrameWork/Scripts/Component/Handler/VolumeHandler.cs
  - Assets/FrameWork/Scripts/Component/Manager/VolumeManager.cs
  - Assets/Scripts/Component/Handler/VolumeHandler.cs
  - Assets/Scripts/Component/Manager/VolumeManager.cs
  - Assets/Scripts/Component/Game/Scene/ScenePrefabForFight.cs
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
   - 各场景初值在 `InitData(GameSceneTypeEnum)` 按场景类型分派；其中 Fight 分支的 mode/focalLength/aperture 由 FightScene 表 `depth_of_field` 配置驱动（见下「关键约定」），focusDistance 仍取 cm_Fight 跟随距离。
2. **体积雾 Volumetric Fog**（第三方 `com.cqf.urpvolumetricfog`，屏幕空间高度雾+光散射）
   - `SetVolumetricFog(distance, density, tint, scattering, anisotropy, attenuationDistance, baseHeight, maximumHeight, isActive, mainLightContribution?, additionalLightContribution?)`（灯光贡献为可空参数：null=不处理保持 profile 原值，非 null 显式 override 防 profile 资产被改）
   - `SetVolumetricFogActive(bool)`、`SetVolumetricFogForRewardSelect()`（领奖场景专用值）；战斗场景体积雾无专用方法，由 FightScene 表 `volumetric_fog` 配置驱动（见下「关键约定」）
   - 组件类型 `VolumetricFogVolumeComponent`（包 autoReferenced，空命名空间，无需额外 using）。
   - 特性：**高度雾**（baseHeight→maximumHeight 之间才有浓度）、`distance` 是渲染最大距离（超出不渲染）、强项是散射朦胧/上帝光。默认应关闭，仅特定场景开启（当前：领奖场景 + 配置了 `volumetric_fog` 的战斗场景，如树林夜晚 10002、沙漠夜晚 20002）。
   - **体积光柱（额外灯）**：包附 `VolumetricAdditionalLight` 组件（空命名空间）挂在 Spot/Point 灯上，体积雾激活期间灯光锥内产生可见散射。`Anisotropy`、`Scattering`、`Radius` 均提供公共 setter，写入时由组件限制范围。树林夜晚、树林雨夜、沙漠夜晚、平原夜晚和夜晚花海的旧 Spot `MoonShaft_1` 已停用，沿用各分支的方向灯和雾配置。峡谷夜晚沿用原方向灯及补光，体积雾配置为空。
   - **夜晚场景动态月光**：`10002`、`10004`、`20002`、`40002`、`40004`、`70002` 的对应 `Details` 分支各有一个 `Moonlight_Beam`，共用包含四个三角形的透明 Unlit 网格和单 Pass 材质，顶点色 R 区分空中斜束和贴地柔光。共享材质使用 4 秒周期、收束 0.1、空中强度 0.35、地面强度 0.20，地面近似加色。绘制顺序为棋盘 2998、月光 2999、角色 3000。各分支的局部位置、资源及测试入口见 [战斗场景接入指南](../skills/fight-scene-integration/SKILL.md#夜晚场景动态月光)。
3. **内置距离雾 RenderSettings.fog**（按深度糊雾色，最直观"远处看不清"）
   - `SetFog(fogColor, fogMode, startDistance, endDistance, density, isActive)`（单一入口：Linear 用 start/end，Exp/Exp2 用 density）
   - `SetFogActive(bool)`
   - 是全局的、按 Unity Scene 存；战斗场景由配置驱动（见下）。

## 关键约定

- **夜晚场景调色**：上述六个夜晚分支各自挂 Global Volume，优先级 10，权重 1，共用 `Assets/Settings/ForestNightColorProfile.asset`。`ColorAdjustments` 使用曝光 −0.2 EV、对比度 +8、饱和度 −12，色调由 `ChannelMixer` 调整。输出 R、G、B 的输入 RGB 权重（%）分别为 `(91,-17,0)`、`(0,78,9)`、`(0,55,102)`。Volume 随各自的 `details` 分支显隐，保留各场景的主光和雾配置。

- **战斗天空盒的曝光和漂移由统一战斗场景组件隔离**。普通战斗在 `WorldHandler.LoadFightScene` 自动确保根节点有 `ScenePrefabForFight`（`enableSkyboxMotion` 默认 false，不改全局天空盒）；天空石台 60001 在预制体根节点启用，复用配置表选定的 `Skybox_1`，运行时副本曝光倍率 1.65、每秒偏航 0.8°。组件只更新自己的副本，外部替换天空盒后停更；禁用时仅在仍持有当前天空盒的情况下恢复源材质并销毁副本。漂移跟随 `Time.timeScale`，不跟随战斗 0/2 速按钮。

- **战斗场景雾走配置表**：`FightScene` 配置表新增 `fog` 字段（形如 `Color:#CEF9FF&Start:8&End:20&Mode:Linear`），空=不开雾。解析在 [FightSceneBeanPartial.cs](Assets/Scripts/Bean/MVC/Game/FightSceneBeanPartial.cs) 的 `HasFog` / `GetFogParams(out color,out start,out end,out mode)`，复用框架 `StringExtension.SplitForDictionary(':','&')`。开雾在 [WorldHandler.cs](Assets/Scripts/Component/Handler/WorldHandler.cs) `LoadFightScene`，关雾在 `UnLoadScene(Fight)` / `UnLoadAllScene`。
- **战斗场景景深走配置表**：`FightScene` 表 `depth_of_field` 字段（形如 `mode:Bokeh&length:130&aperture:12`），空=默认 Bokeh/180/12。解析在 [FightSceneBeanPartial.cs](Assets/Scripts/Bean/MVC/Game/FightSceneBeanPartial.cs) 的 `HasDepthOfField` / `GetDepthOfFieldParams(out mode,out focalLength,out aperture)`（复用 `SplitForDictionary`，缺省键回退默认值）。仅 `VolumeHandler.InitData` 的 **Fight 分支**读取（经 `GameHandler.gameLogic` 当前 `GameFightLogic.fightData.fightSceneId` 查 `FightSceneCfg`），其它场景类型保持硬编码；focusDistance 始终用相机跟随距离，不由配置控制。沙漠(20001)已配 `length:130`（起伏地形 + 180mm 长焦时清晰带仅约 1.7m，透明 Spine 生物会跟着焦外背景一起糊，故沙漠降到 130 加厚景深）。
- **隔离**：内置雾/体积雾均为全局，切场景必须"进场设置、离场关闭"，否则漏到其它场景。体积雾的默认关闭由 `InitData` 开头统一处理。
- **内置雾对自定义 shader/Spine 不一定生效**：内置雾靠 shader 的 fog 宏（`multi_compile_fog`+`MixFog`）。URP 内置 Lit/Unlit、Shader Graph 会吃；手写 shader（草/粒子等）和 Spine 生物材质大概率不吃，会出现"网格糊了、Spine 清晰"的穿帮。
- **夜晚/氛围环境光走配置表**：GameScene 全局环境光是 Flat 模式白色×0.5（`RenderSettings.ambientLight`），不随战斗场景自动变暗。`FightScene` 配置表 `ambient_light` 字段（hex 颜色，如 `#364863`；空=不修改）由 [WorldHandler.cs](Assets/Scripts/Component/Handler/WorldHandler.cs) `LoadFightScene` 进场时直接设置（先缓存原值），卸载战斗场景（`UnLoadScene(Fight)`/`UnLoadAllScene`）时 `RestoreFightSceneAmbientLight` 还原缓存值。解析在 [FightSceneBeanPartial.cs](Assets/Scripts/Bean/MVC/Game/FightSceneBeanPartial.cs) 的 `HasAmbientLight` / `GetAmbientLightColor()`。**受光粒子（如 Effect_Grass_1 草的 GrassWindSway shader `SampleSH` 环境项）靠它才能在夜晚变暗**；仅调暗方向光不够（环境项仍亮）。可视化调节入口：`游戏/战斗场景配置`（FightSceneEditorWindow，雾/环境光/天空盒/道路色直观编辑，Play 时实时应用）。
- **战斗场景体积雾走配置表**：`FightScene` 表 `volumetric_fog` 字段（形如 `Distance:48&Density:0.06&Tint:#B8CCFF&Scattering:0.05&Anisotropy:0.5&Attenuation:96&BaseHeight:0&MaxHeight:12&MainLight:1&AdditionalLight:1`），空=不开。解析在 [FightSceneBeanPartial.cs](Assets/Scripts/Bean/MVC/Game/FightSceneBeanPartial.cs) 的 `HasVolumetricFog` / `GetVolumetricFogParams(out VolumetricFogParamsBean)`（复用 `SplitForDictionary`，缺省键回退默认值=与 `SetVolumetricFog` 参数默认值一致；`MainLight`/`AdditionalLight` 配 1=强制开 0=强制关、缺省=不处理保持 profile 原值），`WorldHandler.LoadFightScene` 读取后调 `SetVolumetricFog(...)` 开启。离场不用显式关，下次任意场景 `InitData` 开头统一 `SetVolumetricFogActive(false)` 兜底。夜晚氛围内容（如萤火虫粒子 Fireflies、动态月光 Moonlight_Beam）直接挂在 prefab 的对应 `Details` 分支下，随 `details` 切换显隐。同理：雨天积水（`Details/DayRain|Puddles` 与 `Details/NightRain|Puddles` 的 Quad 面片，Y=0.02；倒影落点=物体与相机之间的水面——路外南侧水洼贴路缘 local z∈[-3.5,-1] 映路上角色，路中间 `Puddle_Road` 角色站上去倒影在脚边；shader `FrameWork/URP/Puddle1`（框架层 `FrameWork/Shader/URP/Shader_Puddle_1.shader`，跨项目通用）用 `ZWrite Off`+保留 ZTest 让道路网格/角色后画透出水面，故水洼可放任意位置）也随 details 显隐，材质 `Mat_Puddle_Day_1/Night_1.mat`（LoadResources/Materials/Scene/，`_SkyCube` 绑各场景同源天空盒 Cubemap）。Puddles 组上还挂 `PlanarReflection` 组件（FrameWork/Scripts/Component/Other/，随 details 启停）：镜像相机把场景渲染到 RT 写全局 `_PuddlePlanarTex`/`_PuddlePlanarActive`，水洼 shader 屏幕空间采样实现倒映树木/角色（关阴影与后处理省开销，斜投影裁水面以下；⚠️采样 UV 必须 `x=1-x` 翻转——LookRotation 重建的右手系相机 right 轴与真镜面相反，RT 整体左右颠倒，漏翻转会出现"左坑映右敌、站坑上无倒影"）。
- 修改本代理 `watched_files` 命中的代码后，同步更新本文件（枚举/方法/流程）。

## 参数速查（内置雾）
- `FogMode.Linear`：`startDistance` 内清晰 →`endDistance` 后全糊（最好调，配置默认用它）。
- `FogMode.Exponential` / `ExponentialSquared`：用 `density`，Exp2 最柔和朦胧；start/end 被 Unity 忽略。
