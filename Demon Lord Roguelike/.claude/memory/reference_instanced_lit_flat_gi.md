---
name: reference_instanced_lit_flat_gi
description: Graphics.DrawMeshInstanced 的 Lit 材质比预制 MeshRenderer 偏暗(缺环境光)的根因与确定性修法：平坦 GI 属性补光，别指望 CustomProvided SH
metadata:
  type: reference
---

`Graphics.DrawMeshInstanced` 批量绘制**开 Lit 的材质**，会比"把同 mesh+material 的预制拖进场景(走 MeshRenderer)"**偏暗**——差的是一份**环境光(GI)**。

## 根因（两条，均是 DrawMeshInstanced 的能力边界）

1. **环境光缺失**：Lit shader 里 `bakedGI = SampleSH(normalWS)`。`MeshRenderer` 的 `m_LightProbeUsage=BlendProbes`，即使场景没烘焙光照探针也会采到**环境探针**(天空盒/Ambient)的 SH → 亮一份环境光。而 `DrawMeshInstanced` 默认(`LightProbeUsage.BlendProbes` 无 LightProbeProxyVolume)**不给实例填 SH**，`SampleSH` 读到 0 → 暗。
2. **附加光缺失**：`DrawMeshInstanced` 也拿不到逐物体的**附加光(点光/聚光)列表**(URP 逐物体光源剔除只对 MeshRenderer 做)，故点光/聚光对实例化绘制无贡献。本项目战斗场景只有**平行光+天光/环境色**，没附加光，所以差异纯是环境光、可补齐。

## ⚠️ 无效的弯路（别再走）

试过用 `LightProbeUsage.CustomProvided` + `MaterialPropertyBlock.CopySHCoefficientArraysFrom` 把环境探针灌成 per-instance SH —— **没用**。因为逐实例 SH 读取被 `UnityInstancing.hlsl` 的 `#ifdef UNITY_USE_SHCOEFFS_ARRAYS` ← `UNITY_INSTANCED_SH` 门控，而**自定义 shader 只写 `#pragma multi_compile_instancing`、没启用它**(URP 官方 Lit 还带 `#pragma instancing_options renderinglayer`)，`SampleSH` 仍读非实例化的零 SH，灌进去的白灌。

## ✓ 确定性修法（平坦 GI 属性补光，不依赖任何实例化 SH 机制）

场景 GI 是全局的、billboard 法线近恒定 → 一份平坦环境光足够：

- **Shader**（如 `Shader_Mesh_Common_1`）：加 `[HideInInspector] _InstancedFlatGI`(Vector,默认0)+ CBUFFER 字段；Lit 分支 `litColor.rgb += col.rgb * _InstancedFlatGI.rgb`(加到反照率上=GI 项)。**默认0，普通渲染(预制/材质直用)完全不受影响，向后兼容**。
- **C#**：把 `RenderSettings.ambientProbe` 在 6 轴(up/down/left/right/forward/back)`Evaluate` 取平均得平坦色，`MaterialPropertyBlock.SetVector("_InstancedFlatGI", rgb)`(**仅环境光变化时**重算，靠 `SphericalHarmonicsL2 ==` 守卫，静态场景零开销)；绘制走带 MPB 的 `DrawMeshInstanced` 完整重载 + `LightProbeUsage.Off`。
- **属性必须走 MPB 不能写材质**：该 material 被预制 MeshRenderer 共用，写材质会让预制也变亮(双份环境光)；MPB 是逐 draw 的，只作用于实例化绘制。

若平坦平均和预制按法线的 SampleSH 有 ±10~20% 偏差，可改成按 billboard 法线方向精确求值，或加可调倍率。

## 另一个连带坑（MonoBehaviour 构造期）

`MaterialPropertyBlock` 是 Unity 原生对象，**禁止在字段初始化器 `new`**：若该纯 C# 类被 MonoBehaviour(如 `FightManager`)在**构造期/字段初始化器**创建，会触发 `UnityException: CreateImpl is not allowed to be called from a MonoBehaviour constructor`，并连带该组件 `AddComponent` 失败(表现为后续 `manager` 为 null 的 NRE)。→ **延迟到运行时首帧懒建**(`if(mpb==null) mpb=new(...)`)。同理适用于其它 Unity 原生对象。

首次落地：攻击模块弹道 DSP 批量渲染器 `AttackModeInstanceRenderer`（`Mat_AttackModeVisual_RangedNormal` 偏暗）。相关 [[reference_shader_common_layering]]、[[reference_grass_particle_lit_shadow]]。详见 attack-mode-system skill / game-attack-mode agent。
