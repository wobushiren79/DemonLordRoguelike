---
name: reference_shadergraph_fog_bypass
description: URP ShaderGraph 无"关雾"开关的破解与关键教训：URP 雾因子语义是"保色度"(1=无雾)不是雾量，缩放须用 1-(1-因子)*权重；FightSceneRoad 路面已导出为手工维护的 Shader_FightSceneRoad_1.shader（内部名 Game/Fight/FightSceneRoad_1，导出器一次性用完已删）
metadata:
  type: reference
---

需求「某 ShaderGraph 材质不想被场景雾(RenderSettings.fog)影响」的解法与教训，2026-08 为 FightSceneRoad 路面实装（雾把大平面洗白像反光）：

## 最终状态
- 路面材质 `MatFightSceneRoad.mat` 指向 [Shader_FightSceneRoad_1.shader](../../../Assets/Shaders/Shader_FightSceneRoad_1.shader)（内部名 **`Game/Fight/FightSceneRoad_1`**，与项目手写游戏层 shader 命名约定一致；从 `Shader_FightSceneRoad_1.shadergraph` 一次性导出+补丁，文件同名不同扩展名可共存），材质面板有 **Fog Influence** 滑杆（**0=完全不受雾影响，1=与原生一致**，当前为 0）。
- 生成用的编辑器导出器（FightSceneRoadShaderExporter）**用户要求一次性用完即删**；该 .shader 现为**手工维护**——改 .shadergraph 不会同步，文件头注释已写明。
- 路面 shadergraph 本身是纯漫反射棋盘格（ColorA/ColorB 双色，Smoothness=0、Metallic=0）。

## ⚠️ 最大教训：URP 雾因子语义是"保色度"
`MixFog(fragColor, fogFactor)` 内部是 `fragColor * fogIntensity + fogColor * (1 - fogIntensity)`（见 URP ShaderVariablesFunctions.hlsl MixFogColor）——**fogFactor=1 表示完全无雾、0 表示全雾色**，与内置管线"雾量"语义相反！
- ❌ 错误补丁 `MixFog(c, coord * _FogInfluence)`：滑杆 0 → 因子 0 → 全雾色（实测路面全白）；1 → 原样。
- ✅ 正确换算 `MixFog(c, 1.0 - (1.0 - coord) * _FogInfluence)`：0=无雾、1=原生，对 Linear/Exp/Exp2 三种雾模式都成立。

## 技术要点（复用价值）
- URP ShaderGraph（含 Unity 6.3 / URP 17）Lit/Unlit 目标**都没有 fog 开关**，图内无法关雾（HDRP 才有 Receives Fog）。
- 导出方案：反射调 ShaderGraph 内部 API（**Unity 6.3 起 `GraphData`/`MultiJson`/`Generator`/`GenerationMode` 全 internal**，程序集 `Unity.ShaderGraph.Editor`；Generator 取 8 参构造，`generatedShader` 属性取源码，`messageManager` 可空）。
- 生成代码**不内联 pass 模板**，`MixFog` 藏在 `#include ".../Editor/ShaderGraph/Includes/PBRForwardPass.hlsl"` 里 → 字符串替换找不到，须用**宏拦截**：include 前 `#define MixFog(color, coord) MixFog(color, <换算>)`、include 后 `#undef`（预处理器对自引用宏不递归展开；**每个 Pass 独立编译**，PBRForwardPass/PBRGBufferPass 要逐处包裹）。
- 材质重指 shader 后注意保持原 `renderQueue`（路面是 2999）。
- 雾的开关/参数由 `VolumeHandler.SetFog` 按战斗场景配置驱动（WorldHandler 进战斗场景时设置）。
