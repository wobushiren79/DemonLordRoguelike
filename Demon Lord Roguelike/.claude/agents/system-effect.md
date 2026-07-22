---
name: system-effect
description: 特效系统开发：EffectHandler/EffectManager、特效播放与管理、BaseEffectView。
tools: Read, Write, Edit, Glob, Grep, Bash
watched_files:
  - Assets/FrameWork/Scripts/Component/Handler/EffectHandler.cs
  - Assets/FrameWork/Scripts/Component/Manager/EffectManager.cs
  - Assets/FrameWork/Scripts/Component/Effect/EffectBase.cs
  - Assets/FrameWork/Scripts/Component/UI/BaseEffectView.cs
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
