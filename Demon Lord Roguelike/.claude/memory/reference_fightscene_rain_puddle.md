---
name: reference_fightscene_rain_puddle
description: 雨天积水系统：FrameWork/URP/Puddle1 手写shader要点（Cubemap天空反射须按仰角取近地平线天空、涟漪扩散环扰动、枢轴世界坐标做形状种子、平面反射采样须X翻转）、水洼面片摆放约束（Y=0.02、避开道路条带）、Mat_Puddle_Day/Night_1 材质
metadata:
  type: reference
---

# 战斗场景雨天积水（Puddle）要点

2026-08 为森林雨天子场景（FightScene 10003=DayRain/10004=NightLight）实装，方案=水洼面片挂 `Details/<雨天节点>/Puddles` 随 details 配置自动显隐（零代码），雨滴涟漪已由雨效 `Effect_Weather_Rain_Hard_1` 自带 SubEmitterRings 覆盖。雨声环境音 `sound_rain_1`（AudioInfo 2000002，audio_type=2）经两个雨天子场景的 `environment_sound` 列配置，进场自动播、离场自动停（10004 原 2000001 虫鸣已被雨声替换，单通道只能配一个）。

## Shader（`Assets/FrameWork/Shader/URP/Shader_Puddle_1.shader`，内部名 `FrameWork/URP/Puddle1`，手写 URP Unlit）

2026-08-17 已从游戏层 `Assets/Shaders/` 迁入框架层（跨项目复用，与 `PlanarReflection` 组件同属框架层），材质按 GUID 引用不受影响；配套材质仍留游戏层（绑场景天空盒）。

- **反射（水面感核心）**：采样 Cubemap 天空（`_SkyCube`，绑与各场景天空盒同源的贴图——材质绑 textureShape=2 的 Cubemap 资产）。**纯 `reflect()` 取天顶方向是死灰**（阴天天顶就是平的灰），要改用「水平取视线镜像方位 + 固定仰角 `_SkyElevation`（0.25）」的反射方向，取**近地平线天空**——那里云影/天色变化最丰富，低视角水洼映的本就是它。
- **雨滴涟漪**：3x3 网格哈希各一扩散环（`RippleOffset`，life=frac(t+hash)、半径随 life 扩、强度随 (1-life)² 衰）+ 低频 Fbm 慢摆，合成扰动加在反射方向水平分量上。
- **防"黑洞感"**：纯 `pow(1-V.y,2)` 菲涅尔在正俯视时深水色占主像黑洞；`_BaseReflect`（白天 0.65/夜晚 0.5）保底天空反射占比。
- **形状**：椭圆 SDF + 值噪声扰动边缘 → 不规则轮廓，`clip()` 硬裁（AlphaTest 队列 2450 + ZWrite）；**形状种子必须取面片枢轴世界坐标**（`unity_ObjectToWorld._m03/_m23`）——直接用顶点世界坐标会被插值破坏；同材质不同位置面片自动不同轮廓。
- 吃场景雾（`multi_compile_fog` + `MixFog`）；边缘 `_RimDarken` 压暗模拟湿土过渡圈。

## 平面反射（倒映场景物体/角色）

`PlanarReflection` 组件（`Assets/FrameWork/Scripts/Component/Other/PlanarReflection.cs`，框架层通用件）挂在两个 Puddles 组上随 details 启停：

- **几何镜像法**：镜像相机位置=源相机关于水平面对称、前向/上向反射后 `LookRotation` 重建（无需 invertCulling/翻转裁剪面）；斜投影近裁剪面=水平面（Water.cs 标准 `CalculateObliqueMatrix`）裁掉水下几何。
- **⚠️ RT 左右颠倒（2026-08-17 踩过）**：真镜面变换是左手系，`LookRotation(反射forward,反射up)` 重建的却是右手系——其 right 轴与真镜面相反，渲染出的 RT 整体左右翻转。**shader 采样必须 `screenUV.x = 1.0 - screenUV.x` 翻回**（竖直方向不变）。漏翻转的症状=左坑映出右侧敌人、敌人站坑上无倒影（内容左右互换）。
- **全局纹理传递**：RT 写 `Shader.SetGlobalTexture("_PuddlePlanarTex")` + 开关 `_PuddlePlanarActive`（材质不留槽位，组件关闭时 shader 回退纯天空反射）；水洼 shader 用 `ComputeScreenPos` 屏幕 UV（先 1-x 翻转）+ 涟漪扰动（`_PlanarDistort`）采样，`_PlanarWeight`（0.85）与天空盒反射混合。
- **省开销**：RT 高 288 宽随相机纵横比、`UniversalAdditionalCameraData.renderShadows/renderPostProcessing=false`、ClearFlags=Skybox 让反射自带天空、`updateEveryFrame=false` 可隔帧、`farClipOverride` 可压远裁剪、`skipWhenNotVisible`（默认开）水洼全出主相机视锥时整帧跳过（包围盒粗测+非分配 Plane[6] 缓存）；主相机引用缓存避免 Camera.main 每帧查找。
- **性能量级**（森林低模场景估算）：GPU 0.5~2ms（RT 仅主屏 ~7% 像素）+ CPU 0.3~1ms（裁剪+提交），Spine 骨骼/粒子模拟不随相机数增加（只多一次绘制提交）；实测可再用 Profiler 复核。
- **验证注意**：编辑模式下组件生命周期不跑，可用反射调 `OnEnable`/`RenderReflection(srcCam)` 走通全流程。注意"物体入镜"≠"方位正确"——2026-08 的左右颠倒 bug 在测试 Rig 下也曾显示"树木/角色正确入镜"，验证时必须核对倒影的**左右方位**（如让角色站在水坑正上方看脚下倒影）。
- 注意：反射相机也吃全局 `RenderSettings.fog`（雾烘进 RT），水洼尾端再 MixFog 会有轻微二次雾化（雨天读作雾气，可接受）。

## 摆放约束（战斗场景 prefab 局部空间）

- 地面 Y=0，道路 `FightSceneRoadHeightY=0.0001`。
- **⚠️ 反射落点几何（2026-08 踩过）**：平面反射是真镜面，**物体的倒影落在"物体与相机之间的水面"上**（不是"只能放南侧"）。路**外**水洼想映路上角色必须贴路缘（local z∈[-3.5,-1]，远了只映天空树木，呈"角色走过无倒影、远处反而有"假象）；水洼放**路中间**角色站上去，倒影就在其脚边朝南水面，反射机制天然支持。
- **路中间水洼的工程条件**：水洼须 `ZWrite Off`（保留 `ZTest LEqual`，已写死在 shader）——道路网格（Transparent 2999）与角色 Spine（3000）后画透出水面：网格透过水面可见、角色站在水中、倒影照常。若开 ZWrite，水洼 Y=0.02 的深度会把 Y=0.0001 的网格盖掉。示范：`Details/<雨天>/Puddles/Puddle_Road`（路中 (12,2.2)）。
- 材质在 `Assets/LoadResources/Materials/Scene/Mat_Puddle_Day_1.mat` / `Mat_Puddle_Night_1.mat`；Quad 面片 Euler(90,rotY,0) 放平，关投影/接收阴影。
- **MCP execute_code 改材质后必须 `EditorUtility.SetDirty` + `AssetDatabase.SaveAssets` 并在同一会话回读验证**——SetTexture 不保证落盘（2026-08 踩过：SetTexture 后 SaveAssets 仍 fileID:0）。

关联：[[reference_volumetric_light_shaft]]（夜雨月光柱）、[[reference_shadergraph_fog_bypass]]（雾因子语义）、system-volume agent 文档
