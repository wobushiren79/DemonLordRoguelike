---
name: reference_screen_distortion_heathaze
description: 屏幕空间扭曲(热浪/折射)技术套路 + URP 三档均开 Opaque Texture；FrameWork/Effect/HeatHaze shader
metadata:
  type: reference
---

**URP 前提**：`Assets/Settings/URP-{HighFidelity,Balanced,Performant}.asset` 三档质量**全部** `m_RequireOpaqueTexture: 1`（且 `m_OpaqueDownsampling: 1`），所以任何折射/扭曲/热浪类 shader 都能直接靠 `_CameraOpaqueTexture` / `SampleSceneColor(uv)` 取到背景像素，无需额外加 Renderer Feature 抓屏。

**屏幕空间扭曲通用套路**（沙漠热浪即用此实现）：透明网格(Quad) → 顶点 `ComputeScreenPos` 得屏幕 UV → 片元按滚动噪声偏移屏幕 UV → `SampleSceneColor(offsetUV)` 采样不透明贴图 → 输出。关键点：①`#include ".../DeclareOpaqueTexture.hlsl"` 提供 `SampleSceneColor`；②渲染态 `Queue=Transparent`(不透明贴图在 opaque 后 transparent 前捕获)、`ZWrite Off`、`Cull Off`(双面免管朝向)；③偏移量随遮罩(四边羽化)衰减避免边缘硬跳变；④只距离背景一层，采不到透明物(Spine 生物若在 transparent 队列则不被扭曲，仅扭曲地面/不透明物，热浪场景可接受)。

**沙漠热浪落地**（2026-07 方案B）：
- Shader `Assets/FrameWork/Shader/Shader_HeatHaze.shader`，内部名 `FrameWork/Effect/HeatHaze`。**程序化值噪声**(inline `Hash2`+`ValueNoise`，无需噪声贴图)两层解相关随时间上升滚动生成 X/Y 扭曲量。参数：`_NoiseScale`(波纹密度)/`_DistortStrength`(0~0.1 偏移量,默认0.02)/`_RiseSpeed`(上升)/`_WaveSpeed`(横摆)/`_Alpha`(整体强度)/`_TintColor`(染色,默认白)/`_EdgeFade`(边缘羽化)/`_VerticalFade`(沿 UV.Y 衰减)。参数显示名遵循 [[feedback_shader_chinese_labels]] 中文规则。
- 材质 `Assets/LoadResources/Materials/MatHeatHaze.mat`。
- **Sorting Priority 控件**(仿 URP Lit)：shader 加 `[HideInInspector] _QueueOffset`(URP 自己也只在 Properties 声明、不进 CBUFFER→不破 SRP Batcher，纯编辑器用途) + `CustomEditor "HeatHazeShaderGUI"`(`Shader/Editor/HeatHazeShaderGUI.cs`,无 asmdef→Assembly-CSharp-Editor)。GUI 画「优先级」IntSlider(-50~50)，变更时把 `material.renderQueue = RenderQueue.Transparent(3000) + offset` 写入所选材质(支持多选)。URP 里 `_QueueOffset` 是"加号"叠加到基础队列(不透明2000/AlphaTest2450/透明3000)，见 BaseShaderGUI.cs:1148-1149。
- 场景挂载：`FightScene_Desert_1.prefab` 的 `Effect` 节点下加 `HeatHaze` Quad(内置 Quad mesh)，`localPos(5,0.1,5)`/`rot(90,0,0)`横铺地面上方/`scale(30,24,1)`；`shadowCastingMode=Off`+`receiveShadows=false`。视觉大小/位置/强度均在编辑器按相机取景微调。
- 未做：纹理驱动噪声、随距离雾联动、扭曲透明生物(需在生物之后再抓屏)。想做全屏热浪则改用 Full Screen Pass Renderer Feature(见当初方案A)。

Shader 分层与其他 FrameWork shader 见 [[reference_shader_common_layering]]。改预制体走 execute_code `PrefabUtility.LoadPrefabContents/SaveAsPrefabAsset` 见 [[reference_unity_mcp_tool_bug]]。
