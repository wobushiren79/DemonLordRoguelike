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
  - **仅取模型**：`GetEffectModelSync(res_name)` 返回 Effects 目录下的模型预制(缓存 `dicEffectModel`)，**不实例化/不入池/不需要 EffectBase**——供需要自管常驻 `VisualEffect` 实例的粒子用(目前仅攻击弹道拖尾方案2)。游戏层特效仍走 id→res_name(`EffectInfoCfg`)配置，res_name 缓存统一在 `manager.dicEffectResName` 字典(2026-08 由逐效果 `resNameXxx` 字段改为字典)，如血液 `effectBloodId`、拖尾 `effectAttackModeTrailId`(=1600001,Effect_Trail_1)。
  - **游戏层持久型粒子两大统一通道**（2026-08 重构，[EffectHandler.cs](Assets/Scripts/Component/Handler/EffectHandler.cs)/[EffectManager.cs](Assets/Scripts/Component/Manager/EffectManager.cs) 游戏层 partial；新增同类粒子走现成通道即可，勿再各写 `GetEffectForEnduring` 样板）：
    - `ShowEnduringEffect(effectId, configAction)` **通用底层**（VFX 属性型粒子）：按 id 查 `dicEffectResName` 缓存→`GetEffectForEnduring` 取实例→configAction 专属配置→统一 `PlayEffect`。现役：血液 `ShowBloodEffect`(1200001)、护盾打击 `ShowShieldHitEffect`(1300001)、进阶光点 `ShowCreatureAscendAddProgressEffect`(1400001)。
    - `ShowEnduringSingletonEffect(effectId, param, actionForGet=null)` **全局单例定位版**（PS 型粒子为主、亦支持 VFX 老特效，入参 [SingletonEffectParam.cs](Assets/Scripts/Bean/Game/SingletonEffectParam.cs) 结构体，struct 零堆分配热路径友好）：移动唯一实例到落点 + `InjectVfxConfigData` 注入 VFX 暴露属性（见下）+ **有参数才设置**主粒子参数（`duration`/`startSizeMultiplier`/`startLifetimeMultiplier`，哨兵默认值 0=不设置、保持 prefab 原值）+ `Stop(StopEmitting)` 保活 + `Play` 重播；`actionForGet` 可选回传特效实例（PlayEffect 后调用），供发射方持有实例做播放后逐帧控制（如冲击波 simulationSpeed 逐帧跟随游戏速度）。**VFX 配置注入（2026-08-22 补，修复 VFX 刀光不显示回归）**：`InjectVfxConfigData(effectId, targetEffect, param)` 把 EffectInfo 的 float/int/vector3/vector4 配置写入 VFX 暴露属性（逻辑移植自 `ShowEffect`：`{Direction}` 占位取 `param.direction`、`{Size}` 占位乘 `param.size`、`{StartPosition}` 以 `param.targetPos` 为基点加偏移；实例无 VFX 组件或配置为空则跳过）——VFX 老特效（BOSS 刀光 400002=`Effect_Slash_1`，Speed/LifeTime/Direction/StartPosition 全靠此注入，预制默认值 Speed=0/LifeTime=0.1 会原地一闪不可见）必须经此才有正确表现；PS 新粒子该配置为空不受影响。现役：落雷(攻击模块表 300031~035 配 `effect_hit`=900003,`Effect_Thunder_3`)、近战斩击(101001 配 `effect_hit`=400003,`Effect_Slash_2`——4001战之魅魔专用)、地面火焰(300101 配 `effect_hit`=1800001)、冲击波(1700001)、放置魔物(1000001 `Effect_Mana_1`/1100001 `Effect_CreatureShow_1`，2026-08-16 起由 `ShowCreaturePlaceEffect(effectId, targetPos)` 合并原 `ShowManaEffect`/`ShowCreatureShowEffect` 统一走本通道，原一次性实例化路径废弃)。⚠️2026-08-16 起**攻击类命中粒子(全部 `effect_hit`)统一由 `BaseAttackMode.PlayEffectForHit` 走本通道、无 id 分流**——manager 的 `effectThunderId`/`effectSlashId`/`effectFloorFireId`/`effectShockwaveId` 字段已删除，击中粒子 ID 一律配在攻击模块表 `effect_hit` 列**；故所有 effect_hit 的 PS 粒子须为 World 空间模拟+burst 一次性爆发，否则单例移动会拖走旧粒子/重播不触发爆发（VFX 型特效不受此约束——`SendEvent(OnPlay)` 天然重触发爆发、无 Stop 保活需求，如 BOSS 刀光 `Effect_Slash_1` 为 VFX 图）。
  - ⚠️ **PS 单例重播要点**(落雷/斩击/地面火焰)：playing 状态直接 `Play()` 不会重新触发爆发，须先 `mainPS.Stop(true, StopEmitting)`(保活已发射粒子) 再 `Play()`——实测(Unity 6.3)：Stop(StopEmitting) 把系统置于 stopped 状态，随后 `Play()` **从 time 0 重启并重触发 burst、且不清理旧粒子**(World 空间旧粒子驻留继续模拟)，故多实例交叠成立；粒子必须**世界空间模拟**(prefab `moveWithTransform=1`)，否则移动实例会拖走上一发的残留粒子。⚠️斩击刀光软粒子坑：刀光面片大(主粒子 startSize 4.5,半高≈2.25)，材质(`slash03_AB`/`magic_orb2_ADD`/`slash03_ADD`)均开**软粒子**(SoftParticles 0~0.2/0~0.5 深度带,URP `m_RequireDepthTexture=1`)会把贴地粒子实时淡出——症状"透明度每次不同、甚至为 0"(曾用 `targetPos.y += 0.4` 抬到 y≈0.9 修复，当前工作区已移除该偏移、仅靠 4001 配置 `attack_start_position=0,0.5,0` 出手点抬高)；若再遇淡出先查播放高度与软粒子带。⚠️注意 `attack_start_position` 同时是近战判定盒中心高度(BaseAttackMode startPos)，改它会影响判定盒 Y 范围(4001: 0→0.5 后盒 Y 从[-1,1]变[-0.5,1.5]，贴地目标仍可命中)。
  - **地形火焰=全局单例通道**（`ShowEnduringSingletonEffect(effect_hit=1800001, param{targetPos, duration=BurningDuration})`，由 `AttackModeRangedArcGround.SwitchToBurning` 取 `attackModeInfo.GetEffectHitId(0)` 调，PS,`Effect_FloorFire_1`，深渊馈赠「瓶装炼狱火」）：**全局单例**（`GetEffectForEnduring`，配置 show_type=1(Enduring)）——移动唯一实例到落点 + param.duration 重设主粒子时长(燃烧时长) + `Stop(StopEmitting 保活)` + `Play` 重播；旧落点已发射粒子 World 驻留继续燃烧 → 多片火焰=多团粒子共用一个实例。**单例重播成立的粒子前提**（2026-08 已在包装 prefab `Effect_FloorFire_1.prefab` 的覆盖里落实，基源 `Effect_Fire_Floor_2.prefab` 不动）：落地双 burst(t=0/0.1) 爆发 + 主火/GroundFire/GlowFlat 寿命随机 3.5~5s（相位去同步+渐进熄灭防整团同闪同灭）+ rateOverTime=0 + World 空间 + playOnAwake=0；Sparks 火星=落地一次性溅射 burst（保持原短寿命弹道，不随火焰持续）。⚠️若改回 rateOverTime 持续发射：Stop 会让旧落点断供（短寿命粒子转瞬死光），表现为"只剩最后一片火、前一片被顶掉"——2026-08 修复的历史 bug 正是此因（代码按 burst 单例写、prefab 却仍是持续发射+0.35s 寿命）。
  - **冲击波=全局单例通道**（`ShowEnduringSingletonEffect(effect_hit=1700001, param{targetPos, startSizeMultiplier, startLifetimeMultiplier}, actionForGet)`，由 `AttackModeShockwaveRing.StartAttackBase` 取 `attackModeInfo.GetEffectHitId()`(攻击模块表 300091 配置，2026-08-18 起原 manager `effectShockwaveId` 常量删除) 按判定参数换算后调，`Effect_Shockwave_1`，深渊馈赠「第六次冲击」）：2026-08-16 起同走全局单例通道（原 `ShowShockwaveEffect` 薄壳与一次性对象池路径废弃，视觉基准常量 `ShockwaveVisualBaseRadius/BaseDuration` 移入 `AttackModeShockwaveRing`）——播放前对主粒子设 `startSizeMultiplier=maxRadius/3` 与 `startLifetimeMultiplier=waveDuration/0.5`，使视觉波前与 `AttackModeShockwaveRing` 判定环带严格重合（prefab 主粒子=Mesh 模式圆环+Size over Lifetime 归一化扩张，Sparks 子粒子为装饰不同步）；multiplier 每次播放前重设（常驻单例复用无残留）；Stop 语义由 StopEmittingAndClear 变 StopEmitting 保活——冲击波 10 秒一波、波寿命远小于间隔，旧波自然消散、行为无差异；**游戏速度同步（2026-08-25 补，修复 2 倍速下视觉扩散不加速）**：粒子按真实时间模拟、不吃 `fightData.gameSpeed` 数据倍率，故经 `actionForGet` 回调缓存实例后 `AttackModeShockwaveRing.Update` 逐帧把主粒子 `simulationSpeed` 对齐 `GetCurrentGameSpeed()`，中途切 1x/2x 视觉与判定仍严格同步（BOSS 特写 Time.timeScale 变化时双方天然同缩无需处理）；`EffectBase.listPS` 为空的 prefab 用 `SetParticleSystemSize` 不生效，故统一方法直接操作 `mainPS`。视觉与判定不重合时校准 `AttackModeShockwaveRing.ShockwaveVisualBaseRadius` 常量（或改 prefab 主粒子 startSize）。

### 攻击弹道拖尾粒子(方案2 VFX)——非播放式常驻粒子
与血液/护盾**同样的分工**(调用方只给语义数据，粒子的实例/参数/缓冲全归 Effect 系统)，但形态特殊：**不入池、不 `PlayEffect`**，而是每个弹道视觉桶(visualKey)常驻一个 VFX 实例，由**每帧喂 `GraphicsBuffer`** 驱动喷射。

- **落点**：[EffectHandler.cs](Assets/Scripts/Component/Handler/EffectHandler.cs) 的「攻击弹道拖尾粒子(方案2 VFX)」区独占实现——VFX 属性名、实例化、灌参、buffer 扩容/上传/释放**全在此**，别处不得再写拖尾 VFX 代码。状态存 [EffectManager.cs](Assets/Scripts/Component/Manager/EffectManager.cs) 的 `dicAttackModeTrailVfx`(key=visualKey) / `objAttackModeTrailModel` / `triedLoadAttackModeTrailModel`；单桶状态见 [AttackModeTrailVfxBean.cs](Assets/Scripts/Bean/Game/AttackModeTrailVfxBean.cs)。
- **对外接口**(调用方=`AttackModeInstanceRenderer`，它**不碰 VFX**)：`RegisterAttackModeTrailVfx(visualKey)` 注册并灌一次性参数、**返回该桶的 `AttackModeTrailVfxBean` 句柄**(调用方挂在自己的视觉桶上缓存) → 每帧 `BeginAttackModeTrailVfxFrame()` 清收集 → 逐弹 `AddAttackModeTrailVfxPoint(...)` 只报语义数据 → `FlushAttackModeTrailVfxFrame()` 上传驱动；清理 `ClearAttackModeTrailVfx(visualKey)` / `ClearAllAttackModeTrailVfx()`。
- **`AddAttackModeTrailVfxPoint` 有两个重载**：`(visualKey, position, trailColor)` 查表版供外部随手调用；**`(AttackModeTrailVfxBean, position, trailColor)` 句柄版是热路径专用**——桶签名字符串长(含换图/自旋后缀)、哈希按长度计费，逐发查表是每帧的白付开销，故渲染器缓存句柄走后者。两者语义完全一致(查表版内部就是转调句柄版)。
- **⚠️表现参数写死在本类常量**(`TrailVfxLifetime=1` / `TrailVfxSpawnInterval=0.02` / `TrailVfxStartAlpha=0.5` / `TrailVfxEndAlpha=0.05` / `TrailVfxParticleSize=0.05`)：这些是**桶级**参数(注册时灌进实例、同 visualKey 只注册一次)，放配置表等于误导"可逐行调"，故统一收在此处——**要调拖尾表现就改这几个常量**。注册方法因此既不收 `AttackModeTrailConfig` 也不收桶材质，只需桶签名(粒子尺寸曾取弹体材质 `_VertexScale`，现全局写死；贴图由 VFX 预制自带)。配置表侧 `trail_data` 走 `type:2` 时**只需配 `type` + `color`**。
- **VFX Graph 属性合同**(图 `VFX_Trail_1.vfx`，⚠️**无下划线**，与血液 `PositionStart` 同约定)：`PositionBuffer`/`ColorBuffer`(两条 StructuredBuffer&lt;float3&gt;，**同索引配对**=逐弹染色的关键)、`PositionCount`(uint 或 int)、`StartAlpha`/`EndAlpha`/`Lifetime`/`SpawnInterval`/`ParticleSize`(均取本类 `TrailVfx*` 常量)。⚠️图内的 `MainTex`(粒子贴图)**不由 C# 设置**——由 VFX 预制自带，拖尾是独立粒子美术、不再与弹体同图。
- **⚠️Begin→Flush 每帧必须走完**，即使本帧一发子弹都没有——Flush 会把 `PositionCount` 归零，否则子弹死光后 VFX 会在残留位置持续喷粒子。
- **⚠️参数作用域**：配置表侧只有 `color` 逐弹生效(经 `ColorBuffer`)；`count`/`interval`/`startAlpha`/`endAlpha` **已不再从配置表读**(见上条，写死在本类常量)；方案1 的 `shrink`(缩放递减步长)方案2 不支持。
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
