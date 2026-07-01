---
name: reference_grass_particle_lit_shadow
description: 草粒子 Lit 化与阴影踩坑：Mesh 模式必配 Render Alignment=Local/World 否则朝相机+法线偏暗；"阴影脱离草"是斜光自然长影非 bug；密草地关真影用 sub-emitter 假阴影
metadata:
  type: reference
---

Effect_Grass_1 草粒子（[Shader_Particle_GrassWindSway_Lit](Assets/FrameWork/Shader/Effect/Shader_Particle_GrassWindSway_Lit.shader)）Lit 化 + 阴影的踩坑结论：

**1. 受光：billboard 法线恒朝相机 → Lit 算不出明暗。** ParticleSystemRenderer 默认 Render Mode=Billboard、m_NormalDirection=1，法线对所有粒子恒朝相机，Blinn-Phong 的 N·L 是常数 → 整片草均匀无明暗，看着"不受光"。解法：Render Mode 改 **Mesh** + 用"朝上法线"的网格（草作为植被接顶光）。

**2. 头号坑：切 Mesh 模式后必须把 Render Alignment 从 View 改成 Local/World。** 否则 Mesh+Alignment=View 会把整个 mesh（连带朝上法线）旋转去朝相机 —— 一个字段同时造成"草仍朝相机"和"法线被转偏、受光发暗"两个症状。改 Local 后草固定竖直、法线真正朝上，受光立刻正常。

**3. shader 加了 ShadowCaster + DepthOnly 两个 pass**（复用 GrassWind.hlsl 的 ApplyWind + 贴图 alpha clip → 阴影是草形且随风摆动），且去掉了 URP 的 ApplyShadowBias（薄片草不需要）。见 [[reference_shader_common_layering]]。

**4. "阴影脱离草本体"排查结论：不是 bug，是斜光下竖直草片的自然长影。** 决定性验证＝主方向光转顶光，阴影缩回草正下方贴根。血泪教训：别在 shader 的 ShadowCaster pass 里找 bias —— 该 caster 本就不含 bias；URP 的 Shadow Normal/Depth Bias 在**管线层**（当前 Quality=Ultra→Graphics 默认管线 [URP-HighFidelity.asset](Assets/Settings/URP-HighFidelity.asset)，Normal/Depth Bias=1、ShadowDistance=150），但降它也无效，因为偏移根本是斜光几何。排查时先做"顶光测试"判定自然长影 vs 真位置 bug，能省掉一堆瞎改。

**6. 草接收别人阴影：粒子 Inspector 没有 Receive Shadows 开关。** ParticleSystemRenderer 的 Renderer 模块 UI 只暴露 Cast Shadows、**不暴露 Receive Shadows**；`m_ReceiveShadows` 只能用 API/脚本设（`psr.receiveShadows=true`）。开了之后草仍"看不出变暗"＝阴影只压主光直射项、环境光 `SampleSH` 把暗部填亮（所有 URP 物体的标准行为）。解法：Lit shader 加参数 `_ShadowGIStrength`（材质显示名"阴影压暗环境光强度"，默认 0.5）—— 在 `UniversalFragmentBlinnPhong` 前用一次主光 `shadowAttenuation` 把 `inputData.bakedGI` `lerp` 到下限 `(1-强度)`（暗而不死黑），经官方 GI 管线着色；`if(_ShadowGIStrength>0)` 包裹保证参数=0 零二次采样精确回退；该 uniform 加进 GrassWind.hlsl 的 CBUFFER。诊断探针：frag 里 `return GetMainLight(shadowCoord,positionWS,shadowMask).shadowAttenuation.xxx`（黑=在阴影/白=不在）一眼判定阴影采样是否工作。

**5. 密草地最终方案：关真实 Cast Shadows + 贴地假阴影。** 1024 株竖直卡片各投长影会互相叠成杂乱暗块。改法：ParticleSystemRenderer.Cast Shadows=Off；假阴影用 **Sub Emitter(Birth)** 从草派生一个贴地阴影盘粒子（Render Mode=Horizontal Billboard + 复用 [slash_circle01_AB.mat](Assets/FrameWork/Materials/Effect/Other_2/slash_circle01_AB.mat) 圆贴片 + Start Color 染黑半透明），sub-emitter 天然对齐每株草脚下。项目既有贴地阴影盘范式见 Effect_CombatHit_Shadow_1 / Effect_CombatBullet_Shadow_Ball_1。

**7. 与 Spine 生物斜角排序翻转 → 草材质渲染队列改 2450(AlphaTest)。** 症状：某些斜视视角/位置下，Human_Material 等 Spine 生物(半透明 Queue=3000、ZWrite Off)被草粒子盖住显示在草后面，正常视角正确。根因：[Mat_Effect_Grass_1.mat](Assets/LoadResources/Materials/Effect/Mat_Effect_Grass_1.mat) 虽在 Transparent 队列(shader tag `Queue=Transparent`→3000)，但它 `_ZWrite=1` + `_Cutoff=0.5`+shader `clip` = **实为镂空实体(cutout)**；与 Spine 生物同处 3000 队列，走的是"按物体排序点距离整体前后排"而非逐像素深度，斜角下草簇排序点距离反超生物即整体盖上去。修复：把草材质渲染队列覆写为 **2450(AlphaTest)** 回到不透明阶段先写深度，生物半透明再逐像素深度测试 → 任何视角都正确。改法落到 `m_CustomRenderQueue: 2450`(而非 shader tag，避免影响其它 ZWrite Off 的真半透明粒子)。别用 SortingFudge/Order in Layer 创可贴(换视角照样翻)。**踩坑**：MCP 无工具能设任意 renderQueue(manage_asset/manage_material 只认 shader 属性)，execute_code 又踩 [[reference_unity_mcp_tool_bug]] 的 CodeDom 命令行过长；最终用 [[reference_unity_editor_self_run_delete_trick]] 的临时编辑器脚本 `[DidReloadScripts]` 里 `material.renderQueue=2450`+SaveAssets 落盘。**遗留副作用(多代理验证挖出,已确认非生物遮挡问题)**：草进 2450 后落入 URP 不透明预pass 范围(0~2500)；项目默认档 URP-HighFidelity/Balanced 开了 SSAO(Source=DepthNormals)会跑 DepthNormals 预pass 建 `_CameraDepthTexture`/`_CameraNormalsTexture`，而本 shader **只有 ForwardLit/ShadowCaster/DepthOnly、无 DepthNormals pass**→回退 `FallBack URP/Lit`，其 alpha 裁剪被 `#if _ALPHATEST_ON` 门控、而草材质 `m_ValidKeywords:[]`+`_AlphaClip:0`(裁剪是 shader 硬编码 clip 不走关键字)→草在深度/法线纹理里成**无镂空、不摆动的实心矩形**→SSAO 在草缝隙画方块暗角、景深/软粒子/体积雾误当实心块。**生物遮挡用真实 z-buffer(草 ForwardLit 带 clip+风摆写入)不受影响；只影响读深度纹理的后处理，Performant 档(无 SSAO)无此问题**。彻底修法=给 shader 补一个照抄 DepthOnly(ApplyWind+clip)并输出法线的 DepthNormals pass；快速止血=材质开 `_ALPHATEST_ON`(深度纹理里草不摆动但轮廓正确)。草贴图 grass_1.png(32x8)alpha 为严格二值{0,255}+无mipmap+Point采样,故 cutout 无半透明边缘瑕疵、整片草恒不透明(颜色画面干净)。
</content>
