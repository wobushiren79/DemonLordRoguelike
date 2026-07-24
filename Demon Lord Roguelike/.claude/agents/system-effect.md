---
name: system-effect
description: 特效系统开发：EffectHandler/EffectManager、特效播放与管理、BaseEffectView。
tools: Read, Write, Edit, Glob, Grep, Bash
watched_files:
  - Assets/FrameWork/Scripts/Component/Handler/EffectHandler.cs
  - Assets/FrameWork/Scripts/Component/Manager/EffectManager.cs
  - Assets/FrameWork/Scripts/Component/Effect/EffectBase.cs
  - Assets/FrameWork/Scripts/Component/UI/BaseEffectView.cs
  - Assets/Scripts/Component/Handler/EffectHandler.cs
  - Assets/Scripts/Component/Manager/EffectManager.cs
---

# 特效系统 (Effect System) 开发代理

你负责特效系统的开发。

## 职责范围

### 特效管理
- **EffectHandler** - 特效逻辑处理 [FrameWork/Scripts/Component/Handler/EffectHandler.cs](Assets/FrameWork/Scripts/Component/Handler/EffectHandler.cs)
- **EffectManager** - 特效资源管理 [FrameWork/Scripts/Component/Manager/EffectManager.cs](Assets/FrameWork/Scripts/Component/Manager/EffectManager.cs)
  - **加载/播放**：`GetEffect`(一次性)、`GetEffectForEnduring`(持久,按 res_name 取单例实例)——均实例化+入池+需预制挂 `EffectBase`。
  - **仅取模型**：`GetEffectModelSync(res_name)` 返回 Effects 目录下的模型预制(缓存 `dicEffectModel`)，**不实例化/不入池/不需要 EffectBase**——供需要自管常驻 `VisualEffect` 实例的粒子用(目前仅攻击弹道拖尾方案2)。游戏层特效仍走 id→res_name(`EffectInfoCfg`)配置，如血液 `effectBloodId`、拖尾 `effectAttackModeTrailId`(=1600001,Effect_Trail_1)。
  - **游戏层全局单例粒子**（[EffectHandler.cs](Assets/Scripts/Component/Handler/EffectHandler.cs)/[EffectManager.cs](Assets/Scripts/Component/Manager/EffectManager.cs) 游戏层 partial）：血液 `ShowBloodEffect`(VFX,1200001)、护盾打击 `ShowShieldHitEffect`(VFX,1300001)、落雷 `ShowThunderEffect`(PS,`effectThunderId`=900003,`Effect_Thunder_3`)。均走 `GetEffectForEnduring` 单例 + 定位 + `PlayEffect` 重播。⚠️ PS 单例重播要点(落雷)：playing 状态直接 `Play()` 不会重新触发爆发，须先 `mainPS.Stop(true, StopEmitting)`(保活已发射粒子) 再 `Play()`，支持 0.1 秒间隔连发交叠；粒子必须**世界空间模拟**(prefab `moveWithTransform=1`)，否则移动实例会拖走上一发的残留粒子。

### 攻击弹道拖尾粒子(方案2 VFX)——非播放式常驻粒子
与血液/护盾**同样的分工**(调用方只给语义数据，粒子的实例/参数/缓冲全归 Effect 系统)，但形态特殊：**不入池、不 `PlayEffect`**，而是每个弹道视觉桶(visualKey)常驻一个 VFX 实例，由**每帧喂 `GraphicsBuffer`** 驱动喷射。

- **落点**：[EffectHandler.cs](Assets/Scripts/Component/Handler/EffectHandler.cs) 的「攻击弹道拖尾粒子(方案2 VFX)」区独占实现——VFX 属性名、实例化、灌参、buffer 扩容/上传/释放**全在此**，别处不得再写拖尾 VFX 代码。状态存 [EffectManager.cs](Assets/Scripts/Component/Manager/EffectManager.cs) 的 `dicAttackModeTrailVfx`(key=visualKey) / `objAttackModeTrailModel` / `triedLoadAttackModeTrailModel`；单桶状态见 [AttackModeTrailVfxBean.cs](Assets/Scripts/Bean/Game/AttackModeTrailVfxBean.cs)。
- **对外接口**(调用方=`AttackModeInstanceRenderer`，它**不碰 VFX**)：`RegisterAttackModeTrailVfx(visualKey)` 注册并灌一次性参数、**返回该桶的 `AttackModeTrailVfxBean` 句柄**(调用方挂在自己的视觉桶上缓存) → 每帧 `BeginAttackModeTrailVfxFrame()` 清收集 → 逐弹 `AddAttackModeTrailVfxPoint(...)` 只报语义数据 → `FlushAttackModeTrailVfxFrame()` 上传驱动；清理 `ClearAttackModeTrailVfx(visualKey)` / `ClearAllAttackModeTrailVfx()`。
- **`AddAttackModeTrailVfxPoint` 有两个重载**：`(visualKey, position, trailColor)` 查表版供外部随手调用；**`(AttackModeTrailVfxBean, position, trailColor)` 句柄版是热路径专用**——桶签名字符串长(含换图/自旋后缀)、哈希按长度计费，逐发查表是每帧的白付开销，故渲染器缓存句柄走后者。两者语义完全一致(查表版内部就是转调句柄版)。
- **⚠️表现参数写死在本类常量**(`TrailVfxLifetime=1` / `TrailVfxSpawnInterval=0.02` / `TrailVfxStartAlpha=0.5` / `TrailVfxEndAlpha=0.05` / `TrailVfxParticleSize=0.1`)：这些是**桶级**参数(注册时灌进实例、同 visualKey 只注册一次)，放配置表等于误导"可逐行调"，故统一收在此处——**要调拖尾表现就改这几个常量**。注册方法因此既不收 `AttackModeTrailConfig` 也不收桶材质，只需桶签名(粒子尺寸曾取弹体材质 `_VertexScale`，现全局写死；贴图由 VFX 预制自带)。配置表侧 `trail_data` 走 `type:2` 时**只需配 `type` + `color`**。
- **VFX Graph 属性合同**(图 `VFX_Trail_1.vfx`，⚠️**无下划线**，与血液 `PositionStart` 同约定)：`PositionBuffer`/`ColorBuffer`(两条 StructuredBuffer&lt;float3&gt;，**同索引配对**=逐弹染色的关键)、`PositionCount`(uint 或 int)、`StartAlpha`/`EndAlpha`/`Lifetime`/`SpawnInterval`/`ParticleSize`(均取本类 `TrailVfx*` 常量)。⚠️图内的 `MainTex`(粒子贴图)**不由 C# 设置**——由 VFX 预制自带，拖尾是独立粒子美术、不再与弹体同图。
- **⚠️Begin→Flush 每帧必须走完**，即使本帧一发子弹都没有——Flush 会把 `PositionCount` 归零，否则子弹死光后 VFX 会在残留位置持续喷粒子。
- **⚠️参数作用域**：配置表侧只有 `color` 逐弹生效(经 `ColorBuffer`)；`count`/`interval`/`startAlpha`/`endAlpha` **已不再从配置表读**(见上条，写死在本类常量)。
- **降级**：模板资源缺失时桶仍登记、每帧照常收集但不建实例(拖尾静默不显示，弹体本体与方案1 不受影响)；`triedLoadAttackModeTrailModel` 保证每场至多试加载一次，避免 Addressables 缺 key 逐桶抛异常刷屏。

### 飘字(伤害数字)——GPU Instancing 批量渲染
落点：[EffectHandler.cs](Assets/Scripts/Component/Handler/EffectHandler.cs) `ShowTextNumEffect`(类型 0普通/1闪避/2暴击/3HP/4护甲，颜色字段在 [EffectManager.cs](Assets/Scripts/Component/Manager/EffectManager.cs) `colorDamage` 等；闪避显示 0)，转发 `FightTextInstanceRenderer.ShowNumber`(int 反复除 10 拆位进复用缓冲，全程不经 string——热路径零分配的入口；`FightManager.fightTextInstanceRenderer`，纯 C# 类，`FightHandler.Update` 每帧 `RenderAll()`)。原理与弹道渲染器同思路(DSP 式)：每条飘字按**字符**拆实例槽(诞生时一次算好 锚点矩阵+格序索引+颜色+出生时刻)，每帧一次 `DrawMeshInstanced` 画完(≤512 字符槽，槽满按"整条所需字符数"预检、整条丢弃新飘字保旧——不放"12"这种半截数字上屏)；上浮/淡出/弹跳全在 shader 用 `_Time.y-_TextTime` 时间驱动——无 TMP、无 DOTween、热路径零 GC(旧 TMP GameObject 对象池方案已删除)；MPB 逐实例数组按定长 512 整份上传、每帧填充+上传(超出 count 的部分被忽略，与弹体桶 _VelocityWS、轨迹 _TrailAlpha 同一写法——曾用 List 变长上传+dirty 跳帧优化，导致多位数字只显示首位，已回退勿再引入)。shader = [Shader_Mesh_TextInstanced_1.shader](Assets/FrameWork/Shader/URP/Shader_Mesh_TextInstanced_1.shader)(`FrameWork/URP/MeshTextInstanced1`)；**图集约定**：等分格(行列数=材质面板 `_AtlasCols`/`_AtlasRows`，默认 4×4)、第 0 格左上、格序=`atlasChars`("0123456789" 纯数字)，表外字符跳过；C# 逐实例只灌格序索引，UV 由 shader 按材质行列数解算(改材质即生效)；**格子宽高比修正**：单格非正方形时按格子像素比(格宽/格高)横向补偿(C# `cellAspect` 每秒刷新)，字形不被拉伸。当前预制=[FightText_1.prefab](Assets/LoadResources/Common/FightText_1.prefab)(Quad+Mat_FightText_1，图集 10×1)；排版居中、锚点沿相机右轴排开，暴击字号 ×1.5。战斗结束 `ClearTextNumEffect` 清在屏字符槽(渲染器/材质跨场复用)。**渲染队列**：shader 写死 Queue=Transparent+500——Spine 生物/粒子默认 Transparent=3000 且 ZWrite Off，透明组内互不看深度，遮挡纯按「队列→距离从远到近」排序、后画者盖前画者，ZTest 管不到不写深度的透明物体(ZTest Always 只挡写深度的不透明/AlphaTest 物体)；曾用默认 3000，进攻生物比飘字锚点更靠近相机而后画，把飘字压在身后，+500 使其在它们全部之后绘制恒在最前。
- **装配门控**：`TrySetupTextNumInstanced` 整场至多试一次(`EffectManager.triedSetupTextNumInstanced`，与拖尾 `triedLoadAttackModeTrailModel` 同门控)；预制缺失/仍是 TMP 结构/缺 MeshFilter/MeshRenderer 时报错不装配。

### 特效基础类
- **EffectBase** - 特效基类 [FrameWork/Scripts/Component/Effect/EffectBase.cs](Assets/FrameWork/Scripts/Component/Effect/EffectBase.cs)
- **BaseEffectView** - 特效视图基类 [FrameWork/Scripts/Component/UI/BaseEffectView.cs](Assets/FrameWork/Scripts/Component/UI/BaseEffectView.cs)
- **UIParticleSystemOld** - UI 粒子系统旧版兼容

### 特效数据
- **EffectBean** - 特效资源数据

## 约束

- 特效通过 EffectHandler 统一创建和管理
- 特效资源使用 EffectBean 配置
- 战斗特效和 UI 特效分层管理
- 特效播放完后需回收或销毁
