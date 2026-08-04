---
name: reference_instanced_lit_flat_gi
description: Graphics.DrawMeshInstanced 的 Lit 材质比预制 MeshRenderer 偏暗(缺环境光+缺整份主光)的根因与确定性修法：MPB 灌平坦GI/SH + 模拟主光(_InstancedLightDir/Color 两桶共灌)
metadata:
  type: reference
---

`Graphics.DrawMeshInstanced` 批量绘制**开 Lit 的材质**，会比"把同 mesh+material 的预制拖进场景(走 MeshRenderer)"**偏暗**——差的是一份**环境光(GI)**和**整份主光**。

## 根因（三条，均是 DrawMeshInstanced 的能力边界）

1. **环境光缺失**：Lit shader 里 `bakedGI = SampleSH(normalWS)`。`MeshRenderer` 的 `m_LightProbeUsage=BlendProbes`，即使场景没烘焙光照探针也会采到**环境探针**(天空盒/Ambient)的 SH → 亮一份环境光。而 `DrawMeshInstanced` 默认(`LightProbeUsage.BlendProbes` 无 LightProbeProxyVolume)**不给实例填 SH**，`SampleSH` 读到 0 → 暗。
2. **主光缺失**（2026-08-04 实测确诊，骷髅投手骨头 200001 偏暗）：`DrawMeshInstanced` 下 URP 主光 uniform 无效——**Play 中把方向光 Intensity 归零，DSP 弹体毫无变化而场景地面/树明显变暗**（排除阴影假设：同实验关 Shadow Type 无变化；排除 GI 不足：Flat 模式 ambientIntensity 不影响 ambientProbe，补偿值满 1.0）。GI 补偿只够回到贴图原色，在强度 1.5 主光照亮 ~1.75 倍的场景里仍显暗。shader 侧无 `_RECEIVE_SHADOWS_OFF` 变体故 `DrawMeshInstanced(receiveShadows:false)` 参数在 URP 下无效（弹体实际接收阴影，但非本案根因）。
3. **附加光缺失**：`DrawMeshInstanced` 也拿不到逐物体的**附加光(点光/聚光)列表**(URP 逐物体光源剔除只对 MeshRenderer 做)，故点光/聚光对实例化绘制无贡献。本项目战斗场景只有**平行光+天光/环境色**，没附加光，差异纯是环境光+主光、可补齐。

## ⚠️ 无效的弯路（别再走）

试过用 `LightProbeUsage.CustomProvided` + `MaterialPropertyBlock.CopySHCoefficientArraysFrom` 把环境探针灌成 per-instance SH —— **没用**。因为逐实例 SH 读取被 `UnityInstancing.hlsl` 的 `#ifdef UNITY_USE_SHCOEFFS_ARRAYS` ← `UNITY_INSTANCED_SH` 门控，而**自定义 shader 只写 `#pragma multi_compile_instancing`、没启用它**(URP 官方 Lit 还带 `#pragma instancing_options renderinglayer`)，`SampleSH` 仍读非实例化的零 SH，灌进去的白灌。

## ✓ 确定性修法（MPB 灌 GI + 模拟主光，不依赖任何实例化 SH 机制）

场景 GI 全局恒定、主光全局唯一 → 各一份即可：

- **Shader**（如 `Shader_Mesh_Common_1`）：`[HideInInspector] _InstancedFlatGI`(Vector,默认0) + `_InstancedGI`(模式开关) + `_InstancedSH0..6` + `_InstancedLightDir/_InstancedLightColor`(模拟主光)；`SampleInstancedGI(normalWS)` 按 `_InstancedGI` 分流(0=全局 SampleSH 普通渲染/1=Flat/2=SH)，**Flat/SH 两分支都再加 `_InstancedLightColor × NdotL(_InstancedLightDir)`**——面片桶(法线恒定)=恒定提亮对齐预制受光，3D 立体模型桶(如矿车)=恢复明暗立体感。**默认0/普通渲染(_InstancedGI=0)完全不受影响，向后兼容**。
- **C#**：`RenderAll` 开头 `RefreshAmbientSH()` 把 `RenderSettings.ambientProbe` 6 轴平均灌 `mpbFlat._InstancedFlatGI`、L2 PackSH 灌 `mpbSH._InstancedSH0..6`（仅探针变化时重算，`SphericalHarmonicsL2 ==` 守卫）；`RefreshInstancedLight()` 把 `RenderSettings.sun` 方向+颜色灌**两个 MPB 的 `_InstancedLightDir/_InstancedLightColor`**（主光检测独立于环境探针——换战斗场景只换灯不换环境光时也要重灌；方向/颜色各带变化检测）。绘制走带 MPB 的 `DrawMeshInstanced` 完整重载 + `LightProbeUsage.Off`。
- **属性必须走 MPB 不能写材质**：该 material 被预制 MeshRenderer 共用，写材质会让预制也变亮(双份环境光)；MPB 是逐 draw 的，只作用于实例化绘制。
- ⚠️ 若场景 `RenderSettings.sun` 未手动指定，其 getter 会自动 fallback 到场景最亮方向光（Unity 6000 实测），`RefreshInstancedLight` 照常取到灯。

若平坦平均和预制按法线的 SampleSH 有 ±10~20% 偏差，可改成按 billboard 法线方向精确求值，或加可调倍率。

## 另一个连带坑（MonoBehaviour 构造期）

`MaterialPropertyBlock` 是 Unity 原生对象，**禁止在字段初始化器 `new`**：若该纯 C# 类被 MonoBehaviour(如 `FightManager`)在**构造期/字段初始化器**创建，会触发 `UnityException: CreateImpl is not allowed to be called from a MonoBehaviour constructor`，并连带该组件 `AddComponent` 失败(表现为后续 `manager` 为 null 的 NRE)。→ **延迟到运行时首帧懒建**(`if(mpb==null) mpb=new(...)`)。同理适用于其它 Unity 原生对象。

首次落地：攻击模块弹道 DSP 批量渲染器 `AttackModeInstanceRenderer`（`Mat_AttackModeVisual_RangedNormal` 偏暗）；2026-08 补主光（骷髅骨头 200001 在 1.5 强度主光场景偏暗）。相关 [[reference_shader_common_layering]]、[[reference_grass_particle_lit_shadow]]。详见 attack-mode-system skill / game-attack-mode agent。

