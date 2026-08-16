---
name: reference_volumetric_light_shaft
description: 体积光柱（月光/上帝光）配置要点：URP 物理光强 Candela 量级、雾高须覆盖光柱全程、enableAdditionalLightsContribution 开关、VolumetricAdditionalLight 用法
metadata:
  type: reference
---

# 体积光柱（Volumetric Light Shaft）配置要点

项目用第三方 `com.cqf.urpvolumetricfog`（CristianQiu URP Volumetric Light）做体积雾+光柱。夜晚森林月光柱（`FightScene_Forest_1.prefab` 的 `Details/Night/MoonShaft_*`）首次配置时"完全不可见"的排查结论：

## 不可见的三大原因（按致命度排序）

1. **光强量级错误**：URP 物理光单位下 Spot 灯 `m_LightUnit=1` 即 **Candela**，`intensity=3` 照 10 米外≈0.03 lux——体积散射里完全不可见。月光柱级别要 **800~1500 cd** 起步（当前配 1000）。
2. **雾高度没覆盖光柱路径**：体积雾是高度雾（`baseHeight`→`maximumHeight` 间才有浓度）。灯在 Y=10 而雾只到 5 → 光柱上半截无雾不散射。雾高要 ≥ 灯高（当前 0~12 米，density 相应降到 0.06 防白茫茫）。
3. **`enableAdditionalLightsContribution` 必须开**（VolumeComponent 上，默认 false）——关了额外灯对雾零贡献。FightScene 表 `volumetric_fog` 配置里写 `MainLight:1&AdditionalLight:1` 即显式 override 它和 `enableMainLightContribution` 为 true（防御 profile 资产被改），代码侧由 `SetVolumetricFog` 的可空灯光贡献参数落地。

## 其它要点

- `VolumetricAdditionalLight`（空命名空间）挂 Spot/Point 上：`Anisotropy`（0~1 朝光源看更亮）/`Scattering`（散射强度 0~16，光柱亮度主调参）/`Radius`（原点降噪半径）。
- 光柱方向与场景主方向光对齐（夜晚森林主光 rot=55,135）视觉才统一；Spot 不开阴影省性能。
- 散射亮度 ∝ 光强×Scattering；不够亮先拉 Scattering（上限 16）再拉光强。
- 体积雾渲染要求相机开 Post Processing + URP Renderer 已挂 VolumetricFogRendererFeature（领奖场景已验证可用）。
- 全局雾"进场设置、离场由下次任意场景 `VolumeHandler.InitData` 开头统一 `SetVolumetricFogActive(false)` 兜底"。

关联：[[reference_shadergraph_fog_bypass]]（内置雾因子语义坑）、system-volume agent 文档
