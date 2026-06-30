---
name: reference_shader_common_layering
description: Shader 公共 hlsl 分层：Common=跨效果通用件 / Effect 下=效果专属业务件；判定"跨效果可复用才进 Common"，被复用≠通用
metadata:
  type: reference
---

Shader 内 `#include` 的公共 `.hlsl` 按"是否跨效果通用"分层存放：

- **`Assets/FrameWork/Shader/Common/`** = 跨效果通用基础设施库。例：`ParticleCommon.hlsl`（URP 透明粒子通用件：`TEXTURE2D(_BaseMap)` 采样器 + `PARTICLE_COMMON_CBUFFER` 公共材质字段宏 + `ParticleFade(screenPos, softNear, softFar, camNear, camFar)` 柔和粒子/相机淡出函数）。
- **`Assets/FrameWork/Shader/Effect/<效果名>/`** = 效果专属业务件。例：`Effect/WindSway/GrassWind.hlsl`、`TreeWind.hlsl`（草/树风摆的 CBUFFER 风摆字段 + `ApplyWind` 算法：草=`pow(uv.y,_Stiffness)`硬度+`_WindDir`顺风弯倒；树=`_AnchorBottom`锚点+树冠下压）。

**判定标准（术语校准结论）**：「被多个 shader 复用 ≠ 通用」。只有"跟具体效果解耦、能被无关效果直接 include 复用"的才进 Common。检验法：「一个无关粒子效果(如火花)能拿来即用吗？」能→Common，不能→效果目录。`Effect/` 不是"粒子专用"目录，`Effect/<效果>/` 是该效果专属目录，业务件就近放=高内聚，Common 保持纯净。

**风摆粒子 4 shader 落地结构**：`Shader_Particle_{Grass,Tree}WindSway_{Unlit,Lit}.shader` 只剩 Properties + vert/frag；include 链 `.shader → Effect/WindSway/{Grass,Tree}Wind.hlsl → ../../Common/ParticleCommon.hlsl`。改通用淡出=改 `ParticleCommon`(1处全生效)；改草/树风摆=改对应 `*Wind.hlsl`(1处, Unlit+Lit 同步)。

**约束**：①`PARTICLE_COMMON_CBUFFER` 宏须在引用方 `CBUFFER_START(UnityPerMaterial)` 块首展开、其后接 shader 特有字段，保证所有 material uniform 在单一 UnityPerMaterial 块内(SRP Batcher 兼容)。②ShaderLab 的 Properties 块无法 `#include`，每个 `.shader` 仍各留一份参数声明（改动频率低）。③`.hlsl` include 用相对路径(`../../Common/...`)，`.hlsl.meta` 用 `ShaderIncludeImporter`。

参数显示名中文规则见 [[feedback_shader_chinese_labels]]。
