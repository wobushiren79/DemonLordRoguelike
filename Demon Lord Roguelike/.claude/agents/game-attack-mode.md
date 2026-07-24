---
name: game-attack-mode
description: 攻击模式系统开发：21种攻击模式（近战/远程/特殊/恢复），BaseAttackMode 策略模式。
tools: Read, Write, Edit, Glob, Grep, Bash
watched_files:
  - Assets/Scripts/Game/Fight/AttackMode/
  - Assets/Scripts/Game/Fight/AttackModeInstanceRenderer.cs
  - Assets/Scripts/Game/Fight/AttackModeInstanceRendererTrail.cs
  - Assets/Scripts/Game/Fight/FightRaycastBatch.cs
---

# 攻击模式 (Attack Mode) 开发代理

你负责 [Scripts/Game/Fight/AttackMode/](Assets/Scripts/Game/Fight/AttackMode/) 中的攻击模式开发。

## 职责范围

基于**策略模式**的攻击模式体系：

### 近战 (Melee)
- **AttackModeMelee** - 普通近战
- **AttackModeMeleeArea** - 范围近战

### 远程 (Ranged)
- **AttackModeRanged** - 普通远程
- **AttackModeRangedArc** - 弧形远程
- **AttackModeRangedArcArea** - 弧形范围远程
- **AttackModeRangedArea** - 范围远程
- **AttackModeRangedPiercing** - 穿透远程
- **AttackModeRangedSplit** - 分裂远程-**发射器**（不飞不画不命中，按道路发射多发子弹道后自毁；配置 `child_attack_mode_id` 指向子弹道行）
- **AttackModeRangedSplitChild** - 分裂远程-**子弹道**（继承 AttackModeRanged，仅多一个「向目标道路 z 轴归位」）
- **AttackModeRangedTracking** - 追踪远程

### 特殊 (Special)
- **AttackModeExplosion** - 爆炸
- **AttackModeFallupon** - 降临
- **AttackModeFalluponArea** - 范围降临
- **AttackModeFalluponChain** - 连锁降临
- **AttackModeLure** - 引诱
- **AttackModeOverlap** - 重叠
- **AttackModeInstantArea** - 瞬时落点范围（无弹道飞行，StartAttack 当帧对 targetPos 范围攻击并自毁；支持配置 `hit_max` 命中上限(近者优先截断) + 发射方注入 `filterCreatureIds` 快照名单(StartAttack 前写入、Destroy 置空)）
- **AttackModeInstantAreaThunder** - 落雷（继承上者，`PlayHitEffect` 改走 `EffectHandler.ShowThunderEffect` 全局单例粒子——持久型 PS 需 Stop/Play 重播，不能走 effect_hit；深渊馈赠「闪电」300031~300035，由 BuffEntityPeriodicMultiInstantAttack 发射）

### 恢复 (Regain)
- **AttackModeRegain** - 恢复基类
- **AttackModeRegainHP** - HP 恢复
- **AttackModeRegainDR** - DR 恢复

## BaseAttackMode 关键字段

- **`isValid`** - 是否激活；`Destroy()` 时置 `false`，外层遍历据此跳过
- **`instanceId`** - `FightManager` 分配的实例 ID，`dlAttackModePrefab`（DictionaryList）按此 key 做 O(1) 移除
- **`position`** - 弹道当前世界坐标，**位置真实源（DSP 方案B 权威源）**。子类移动/定位一律走 `SetPosition(pos)`/`TranslatePosition(delta)`，**禁止**再直接写 `gameObject.transform.position` 或 `transform.Translate`；这两个 helper 会在写 `position` 的同时同步回 `transform`（gameObject 预制字段保留、旧渲染兼容）。起点/射线起点/命中/边界检测均已改读 `position`（`CheckIsMoveBound()` 无参重载读 `position`，`CheckIsMoveBound(GameObject)` 为兼容重载）。读取**别的生物**位置仍用 `creatureObj.transform.position`（那不是弹体自身）。一发弹道=一个 `position`，故"一次攻击打出多发"须拆成多个独立 AttackMode（分裂弹已如此，见下方 DSP 一节）
- **`searchCreatureType`** - 由 `attackModeData.attackedLayerTarget` 推导出的搜索类型，在 **`StartAttackBase()` → `RefreshSearchCreatureType()`** 中缓存、`Destroy()` 中清零，子类范围检测应复用而非每帧重算。⚠️**推导必须留在 `StartAttackBase` 里**：`StartAttack()`（无 attacker 的纯数据发射，分裂弹子弹道走的正是它）也要靠它，挪回 `StartAttack(attacker,...)` 会让这些弹道层掩码为 0、射线不入队、永远打不到人
- **`batchRayStart`** - 本帧射线批处理命令索引（`>=0` 表示已入队射线，`CheckHitTarget*` 直接读批处理结果；`-1` 走 live 路径）；由 `PrepareRaycast` 每帧重置/赋值
- **`SpeedRateASPDMax`** - 攻速 ASPD=100 时弹道飞行速度的最大加成倍率常量（当前 3 倍，数值调整入口）；`StartAttack(attacker,...)` 时把攻击者 ASPD 按 0~100 线性插值成 1~该倍率，快照进 `attackModeData.attackerSpeedRate`（与 `attackerDamage`/`attackerCRT` 同一快照模式）
- **`GetMoveSpeed()`** - 弹道实际飞行速度 = `attackModeInfo.speed_move × attackModeData.attackerSpeedRate`；远程系（Ranged/Arc/Tracking/Split 及其子类）的移动计算必须用它，**禁止**直接读 `attackModeInfo.speed_move`（天降 Fallupon 的下落速度不吃攻速加成，仍直接用配置值）
- **伤害快照 `attackerDamage`** - `StartAttack(attacker,...)` 时 = 攻击者当前 ATK × 攻击模式配置 `damage_add_rate` 伤害加成倍率（`AttackModeInfoBeanPartial.GetDamageAddRate()`：>0 取该值，0/空按 1 倍）；典型用法是自爆史莱姆（3002/300001）把基础 ATK 压到 10、配 `damage_add_rate`=50 得到 500 伤害，升级加攻收益按倍率放大

### 拖尾（轨迹）

> 命名：本节的"轨迹"指弹道拖尾（`TrailBucket`），与 framework-core 的冲刺残影 `AfterimageGhost*` 是**两套无关系统**，勿混。

- **效果定义** - 拖尾 = **弹体贴图本身画在若干历史位置上、越老越透明**（类似冲刺/突进残影），不是连续条带。放弃了旧的三角带 billboard 方案（`AppendTrailStrip` 逐点切线×相机朝向展宽 + 逐帧建 Mesh），改为白嫖 DSP 的 GPU Instancing：轨迹就是弹体实例在历史点上多画几遍，CPU 开销≈**每桶一次 `DrawMeshInstanced`**（所有年龄档合批，见下）
- **启用来源** - 配置表 `excel_attackmode_info` 的 `trail_data` 列（单列打包，`&` 分隔项、`:` 分键值，如 `type:1&count:6&interval:0.05&startAlpha:0.5&endAlpha:0.05&color:1,1,1`）；由 `AttackModeInfoBeanPartial.GetTrailConfig()` 解析缓存为 `AttackModeTrailConfig`（`count`>0 且 `interval`>0 才 `enable`；未配 `type` 默认 Instanced、未配透明度默认 `startAlpha=0.5`/`endAlpha=0.05`）。字段：`type`(渲染方式枚举 `AttackModeTrailType`：1=Instanced 默认/2=Vfx)、`count`(轨迹段数)、`interval`(采样间隔秒)、`startAlpha`(最新档透明度)、`endAlpha`(最老档透明度)、`color`(染色 rgb，alpha 由 start/endAlpha 决定；⚠️**方案1 下是桶级**——同 `visual_name` 的多行只有首个注册者的 color 生效，**方案2 下才逐弹生效**)。⚠️**`count`/`interval`/`startAlpha`/`endAlpha` 仅方案1 有效**——方案2 的这些表现已写死在 `EffectHandler` 的 `TrailVfx*` 常量里，`type:2` 的行**只需配 `type`+`color`**，填了其余键会被静默忽略。⚠️**`enable` 判定随 type 而异**：方案1=`count>0 && interval>0`；方案2=配了 `type:2` 即启用(否则只写 `type:2&color:...` 会被整条关掉)。**`type` 只是 `trail_data` 字符串内的键，Excel 列本身不变，改它无需重生成 Bean/JSON**。本节余下描述的是**方案1(Instanced)**；方案2(Vfx)见下方独立条目。**必须配合 `visual_name` 走 DSP**——轨迹材质 = **克隆弹体桶材质**经 `SetupTrailMaterial` **把 shader 换成 `Shader_Mesh_TrailInstanced_1`**(`FrameWork/URP/MeshTrailInstanced1`)+写基色+**关掉轨迹不该有的开关**+进透明队列；换 shader 时 Unity **按属性名保留同名属性值**，故弹体材质的全部设定零拷贝继承。⚠️**轨迹 shader 是 MeshCommon1 的 drop-in 替身：23 个属性全量对齐 + 多一个逐实例 `_TrailAlpha`，同挂 `MeshCommonShaderGUI` 面板；改 MeshCommon1 的参数必须同步它**（Properties 块无法 `#include`，算法本体仍共享 `Common/*.hlsl`）。唯一结构差别=轨迹只有 Forward 一个 pass（恒 `ShadowCastingMode.Off` 且不写深度，ShadowCaster/DepthOnly 永远跑不到）。⚠️**参数全量继承意味着克隆来的关键字也会生效**，故 `SetupTrailMaterial` 的三处 `DisableKeyword` 是**正确性要求不是清理**：`_ROTATE_TIME_ON` 不关=自转角烤进矩阵后再按 `_Time` 转一遍→**转两遍**(骨头 200001 的坑；弹体材质实测就带此关键字+`_RotateSpeed=(0,0,-360)`)、`_ALPHATEST_ON` 不关=渐隐到 0.05 的尾巴被阈值 0.5 整档裁没、`_LIT_ON` 关(轨迹无光；且其 MPB 只灌 `_TrailAlpha` 不灌 `_InstancedFlatGI`，开 Lit 会偏暗)。`_OUTLINE_ON` **不关**、随弹体继承。无需模板材质资产，但**⚠️该 shader 只被代码 `Shader.Find` 引用，必须留在 `ProjectSettings > Graphics > Always Included Shaders` 否则进包即失效**（Editor 正常、构建包里 `Find` 返回 null → 方案1 拖尾整体消失，弹体不受影响）
- **历史缓冲字段** - `BaseAttackMode`：`trailMode`(单个 `AttackModeTrailType` 三态字段 None/Instanced/Vfx，**已合并旧的 `trailEnabled`/`trailVfxEnabled` 两 bool**；仅 Instanced 才采样环形缓冲、Vfx 不分配缓冲)、`trailPoints`(环形位置缓冲,懒分配 `TrailMaxPoints=32`=轨迹段数上限)、`trailSpinAngles`(环形自旋角缓冲,与 `trailPoints` 一一对应,记录每个采样点当时的时间自转角=`spinSpeed×now`,供轨迹复现旋转姿态,无自旋恒0)、`trailCount`/`trailHead`(环形写指针)、`trailSampleInterval`(=`config.interval`)。方法：`EnableTrail(config)`(懒分配两条缓冲+清空+取间隔)、`SampleTrail(now)`(按间隔 push `position`+自旋角)、`GetTrailPoint(orderIndex)`/`GetTrailSpinAngle(orderIndex)`(0=最老→count-1=最新)、**`GetTrailSample(orderIndex, out point, out spinAngle)`**(一次取回位置+自旋角，只算一遍环形下标换算；热路径优先用它，别 `GetTrailPoint`+`GetTrailSpinAngle` 各调一次白算两遍取模)、`ResetTrail()`。（已删除 `trailConfig` 字段——轨迹参数由渲染器在 `TrailBucket` 里缓存）
- **生命周期** - `InitAttackModeShow` 开头 `ResetVisualParams()` 关拖尾、末尾 `EnsureAttackModeVisual(this)` 按 `GetTrailConfig()` 调 `EnableTrail` 重开并清空；每次发射（含对象池复用）都走 `StartAttackInit`→此链路，**零残留**。渲染由 `AttackModeInstanceRenderer` 每帧 `RenderAll` 里对启用弹道 `SampleTrail` 并收集进对应 `TrailBucket`，收尾 `DrawTrailBuckets` 按**年龄档**组织：档 k = 所有弹道的第 k 个最新历史点，该档 alpha = `Lerp(startAlpha,endAlpha)`。⚠️**整桶所有档合到一次 `DrawMeshInstanced`**——档 alpha 是轨迹 shader 的**逐实例属性 `_TrailAlpha`**（每批一次 `MPB.SetFloatArray(trailAlphaBuffer)`，定长 1023、与 `matrixBuffer` 同下标），故"所有档 × 所有弹道"填进同一缓冲一次画完，只有实例数（弹道数 × 档数）超 1023 才分批。**旧实现每档一次 draw**，因为那时档 alpha 是 MPB 上的整批 uniform `_BaseColor`。⚠️**填充顺序即叠加顺序**（单次实例化绘制内按实例 ID 顺序光栅化），故仍须**由老到新填**，近处不透明档才叠在远处透明档上。**每桶每帧仅多 1 次 draw call，与弹道数、档数均无关**。⚠️**逐发基准矩阵缓存**（`trailBaseMatrix`/`trailBaseHasSpin`，跨桶复用、按 2 的幂扩容）：**无自旋**弹道整发所有档同旋转同缩放、只有平移在变，故每帧每发只算一次 `BuildInstanceMatrix`，逐档仅改写平移列 `m03/m13/m23`——省下「档数×弹道数」次 `Quaternion.AngleAxis`/`Matrix4x4.TRS`（二者均为 extern 原生调用，是本函数原先的头号热点）。**有自旋**的弹道旋转随采样时刻变，仍逐档现算，不能复用
- **`BuildInstanceMatrix` 的无旋转快速路径** - 不配起始角(`visualStartAngle==0`)且不自旋(`spinAngle==0`)时直接拼对角矩阵返回，跳过 `Quaternion.AngleAxis` + `Matrix4x4.TRS`（二者均为 extern，托管↔原生穿越按每发每帧计费）；与 `TRS(pos, identity, one*scale)` 逐位等价。改这个函数时留意别把两条路径改岔。
- **旋转弹道的轨迹（如骷髅骨头 200001）** - 轨迹材质的时间自转被 `SetupTrailMaterial` **强制关闭**（`_ROTATE_TIME_ON` off；轨迹画的是过去某刻的静态快照，不关就是转两遍），但**每个采样点的旋转姿态由 `BuildInstanceMatrix(attackMode, pos, extraSpinAngle)` 的 `extraSpinAngle` 烤进矩阵**：弹体本体传 0（时间自转交 shader），轨迹传该采样点的 `GetTrailSpinAngle`（=`spinSpeed×采样时刻`，绕 `spinAxis`）。故旋转弹道的轨迹会**复现当时的旋转角**（一串逐渐转过的骨头），而非同一角度。⚠️采样时刻用 `Time.timeSinceLevelLoad`（与 shader `_Time.y` 同基准），使最新档的轨迹角与弹体本体连续、不脱节；⚠️自旋角与 shader 完全对齐仅在**单轴自旋**（billboard 绕 Z，本项目情形）下成立，多轴自旋因 euler 合成顺序差异会有偏差
- **方案2（VFX，`type:2`，C# 绑定已实现；图 `VFX_Trail_1.vfx` 已建）** - **单个 GPU VFX 特效**，每帧经两条 `GraphicsBuffer` 一次性上传全部子弹位置**+逐弹染色**喷射轨迹粒子，**合一 draw call、与子弹数无关**（方案1 现在也是每桶 1 次 draw，故 draw call 已基本持平；**方案2 的独占优势只剩逐弹染色**）。缺图时 `type:2` 静默不显示（不影响弹体本体/方案1、不报错）。⚠️**落点：VFX 逻辑全部归 `EffectHandler`，`AttackModeInstanceRenderer` 不碰粒子**(与血液/护盾同一分工：调用方只给语义数据，粒子实例/参数/缓冲由 Effect 系统自管——改拖尾 VFX 一律去 EffectHandler)。**EffectHandler**「攻击弹道拖尾粒子(方案2 VFX)」区：VFX 属性名 ID + **表现常量 `TrailVfxLifetime`(1s)/`TrailVfxSpawnInterval`(0.02s)/`TrailVfxStartAlpha`(0.5)/`TrailVfxEndAlpha`(0.05)/`TrailVfxParticleSize`(0.1)**(⚠️桶级参数已从配置表移除、写死于此，调表现改这里) + `RegisterAttackModeTrailVfx(visualKey)`(**只收桶签名，不收 config 也不收桶材质**；去重+实例化+**就地灌一次性参数**。粒子尺寸曾取弹体材质 `_VertexScale`，现写死为 `TrailVfxParticleSize`；贴图由 VFX 预制自带、不从弹体材质覆盖。**返回该桶的 `AttackModeTrailVfxBean`**，供调用方缓存句柄)/`BeginAttackModeTrailVfxFrame()`/`AddAttackModeTrailVfxPoint(...)`(直接用 `trailColor` 原值成对 Add，**不乘弹体基色**；**两个重载**——`(visualKey,pos,color)` 查表版供外部随手调，**`(AttackModeTrailVfxBean,pos,color)` 句柄版供热路径**，渲染器走后者省掉逐发的字符串哈希+字典查找)/`FlushAttackModeTrailVfxFrame()`(**每帧参数就地设完**：两条 `SetData`+`PositionCount` 兼容 uint/int；`EnsureAttackModeTrailVfxBuffer` 两 buffer 同步扩容并**返回是否重建**，⚠️`SetGraphicsBuffer` **仅在重建时重绑**——容量没变则 VFX 持有的 buffer 引用一直有效，每帧无条件重绑是白付开销；但重建后**必须**重绑，否则 VFX 仍指着已 `Release` 的旧 buffer)/`ClearAttackModeTrailVfx`/`ClearAllAttackModeTrailVfx`/`GetAttackModeTrailModel`(私有,内含懒加载门控)。**状态**：`EffectManager.dicAttackModeTrailVfx`(key=visualKey)/`objAttackModeTrailModel`/`triedLoadAttackModeTrailModel` + `AttackModeTrailVfxBean`(`vfx`/`listPosition`/`listColor`/两 buffer/`bufferCapacity`)。**渲染器仅 3 个调用点**(Register 转交 + Begin/Add/Flush + Clear 转交)，无任何 VFX 类型。**枚举/逐弹字段**：`AttackModeTrailType`(`None`/`Instanced`/`Vfx`)+`AttackModeTrailConfig.type`；`BaseAttackMode.trailMode`(三态)+**`trailColor`**(Vector3 rgb,`EnableTrail` 从自身 `config.color` 设,`ResetVisualParams` 复位白)。**加载链**：`effectAttackModeTrailId`(1600001)→`EffectInfo` res_name(`Effect_Trail_1`)→`GetEffectModelSync`(不实例化/无需 EffectBase)→EffectHandler 每桶 Instantiate 一份；每场至多试一次、缺资源静默降级。⚠️`FightManager.EnsureTrailVfxTemplate`/`triedLoadTrailVfx`/`SetTrailVfxTemplate` **已删除**。⚠️`RenderAll` 列表为空**不可早退**(须走完 Begin→Flush 归零 `PositionCount`，否则子弹死光后 VFX 在残留位置持续喷粒子——旧实现的 bug，已修)。**VFX Graph 暴露属性合同**(名须一致，⚠️**本项目约定无下划线**如血液 `PositionStart`)：`PositionBuffer`/**`ColorBuffer`**(逐弹 rgb,与位置同索引同容量)/`PositionCount`(uint 或 int)/`MainTex`(取弹体 `_BaseMap`)/`StartAlpha`/`EndAlpha`/`Lifetime`(=count×interval)/`SpawnInterval`(=interval)/`ParticleSize`(取常量 `TrailVfxParticleSize`)；图建议 World 空间；Spawn Periodic Burst(Count=`PositionCount`,Delay=`SpawnInterval`)；Initialize `Sample Graphics Buffer`(Type=Vector3,Index=`particleId % max(PositionCount,1)`,⚠️无 spawnIndex 用 `particleId`) 设 position；透明度用手动 `Set Alpha`+`Lerp(StartAlpha,EndAlpha,age/lifetime)`(`Set Alpha over Life` 只吃 Curve)；`Set Color` 接**第二颗 Sample Graphics Buffer**(`ColorBuffer`,**复用位置那颗 Modulo 的输出**——同索引才不错配)；Output Quad 用 `MainTex`×**粒子 color 属性**朝相机。**图已接完**(两条 buffer + 共用同一 Modulo 的两颗 Sample)；**`TrailColor` 已从图与 C# 双双删除**(染色一律逐弹走 `ColorBuffer`)。⚠️**染色作用域**：方案1 桶级(同 `visual_name` 首个注册者赢，其余 color 被忽略)、方案2 逐弹级(同一 VFX 内多色并存)——需同图不同色必须 `type:2`。⚠️但方案2 下 **`count`/`interval`/`startAlpha`/`endAlpha` 仍是桶级**(注册时灌进 VFX 实例，首个 `type:2` 行赢)，只有 `color` 逐弹。⚠️**同 `visual_name` 下 `type:1` 与 `type:2` 可共存**(Instanced/Vfx 两桶分属不同字典，`RenderAll` 按每发的 `trailMode` 路由，互不干扰)。详见 attack-mode-system skill
- **⚠️「一次攻击打出多发」须拆成多个独立 `BaseAttackMode`** - DSP 每个维度都是 per-AttackMode 单份的（`position` 单点、`batchRayStart` 单条射线、`trailPoints` 单条环形缓冲），在单个 AttackMode 里自管多个 GameObject 的类型每加一个新特性都得在自己内部重做一遍多路版本，且 `RenderAll` 只读它的单个 `position`（只画一个不动的弹体）。**`AttackModeRangedSplit` 已按此改造**为纯发射器 + 独立 `AttackModeRangedSplitChild` 子弹道，各子弹自动享有视觉桶/拖尾/射线批处理/对象池。新增散射、多重箭等照此办理
- **限制** - 旧三角带方案的 `FrameWork/URP/AttackTrail` shader 与 `PathInfo.AttackTrailMatPath` 已删除（轨迹改克隆弹体材质）

## 新增攻击模式模板

```csharp
public class AttackModeCustom : BaseAttackMode
{
    public override void StartAttack(FightCreatureEntity attacker, FightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        // 自定义初始化（伤害/方向已在 base 中根据 attacker/attacked 写入 attackModeData）
        actionForAttackEnd?.Invoke(this);
    }

    // 走射线检测(Ray/RaySelf)的弹道需重写：把本帧射线入队批处理（在 Update 之前的收集阶段调用）
    public override void PrepareRaycast(FightRaycastBatch batch)
    {
        batchRayStart = -1;
        EnqueueSingleRay(batch);   // 单射线复用（一发弹道=一条射线；要打出多发请拆成多个独立 AttackMode，见上方分裂弹）
    }

    public override void Update()
    {
        base.Update();
        // 远程/持续类型在此驱动；CheckHitTargetForSingle/CheckHitTarget 会自动读上面入队的批处理结果；命中后 Destroy() 回收
    }

    public override void Destroy(bool isPermanently = false)
    {
        // 清理子类自有的缓冲（如候选 List/HashSet），避免对象池复用残留
        base.Destroy(isPermanently);
    }
}
```

## 弹道渲染 (DSP 式 GPU Instancing，AttackModeInstanceRenderer)

借鉴戴森球计划「只记录位置，一起绘制」：不为每发弹道单独渲染，而是每帧遍历活跃弹道、按视觉类型分桶、用各弹道 `position` 批量 `Graphics.DrawMeshInstanced` 一次画完，替代「N 个 GameObject 各挂 VisualEffect/SpriteRenderer」的 N 份固定开销。

- **[AttackModeInstanceRenderer](Assets/Scripts/Game/Fight/AttackModeInstanceRenderer.cs)**：纯 C# 类（非 MonoBehaviour，不持有 GameObject），`FightManager.attackModeInstanceRenderer` 持有，`FightHandler.UpdateHandleForAttackModePrefab` **阶段4** 调 `RenderAll(listAttackMode)`。⚠️已从 `Fight/AttackMode/` **上移到 `Fight/`**，且拆 **partial 两文件**：主文件=弹体桶/RenderAll/环境光，[AttackModeInstanceRendererTrail.cs](Assets/Scripts/Game/Fight/AttackModeInstanceRendererTrail.cs)=轨迹拖尾全部逻辑。
- **分桶 key = `attackModeInfo.visual_name`（新配置字段）**：`RegisterVisual(visualKey, mesh, material, spinAxis = default, spinSpeed = 0f)` 注册视觉桶（mesh 通常朝相机 Quad、material 须开 GPU Instancing）；每桶固定 `Matrix4x4[1023]` 复用缓冲，满批即绘、收尾绘剩余，无热路径分配。**自旋在此一次性写进桶材质**（桶签名已按 `spinAxis×spinSpeed` 细分→整桶自旋恒定），故基础桶(key=visual_name，按签名规则必然无自旋)用默认值 0 即对。
- **弹体默认不投/不收阴影**：`BodyShadowCasting`/`BodyReceiveShadows` 常量（主文件「常量」区）默认 `Off`/`false`——满屏子弹的阴影几乎看不出，却让每个弹体桶多走一遍 ShadowCaster Pass。要恢复旧表现改回 `On`/`true` 即可（原实现是 `On`/`true`）。
- **`visual_name` 与 `prefab_name` 是独立的两套渲染通道**：`visual_name` 走 DSP 批量渲染；`prefab_name` 仍是**原预制渲染**（`FightManager.GetAttackModePrefab` Instantiate prefab 挂 SpriteRenderer/VisualEffect，逻辑不变）。配置侧二选一，别同一行两个都填(会双重渲染)。
- **常开(无总开关)但天然零副作用**：`visual_name` 为空、或未 `RegisterVisual` 该桶的弹道会被跳过(什么都不画)——所以现有全部弹道(visual_name 均空)行为不变。
- **`visual_name` 是配置表字段**：加在 `excel_attackmode_info[攻击方式].xlsx` 的 `AttackModeInfo` 表(prefab_name 之后)，需在 Unity 跑 ExcelEditorWindow「生成 Entity + 导出」重新生成 `AttackModeInfoBean` + `AttackModeInfo.json` 后 `attackModeInfo.visual_name` 才可用。
- **视觉资源 = 一个预制(mesh+material)**：`Assets/LoadResources/AttackModeVisual/<visual_name>.prefab`（`PathInfo.AttackModeVisualPath`），预制上挂 `MeshFilter`(Quad) + `MeshRenderer`(material 须开 GPU Instancing)；美术可直接在 Editor 看效果。必须注册进 Addressables(address=全路径)否则加载返 null。
- **懒注册**：`FightManager.EnsureAttackModeVisual(attackModeInfo)` 在 `GetAttackModePrefab` 里被调——visual_name 非空且 `attackModeInstanceRenderer.HasVisual` 为假时，`GetModelForAddressablesSync(dicAttackModeVisualObj, ...)` 加载预制、取 **sharedMesh/sharedMaterial**(勿用 .mesh/.material) 注册。只加载一次。⚠️**此重载只管弹体桶、不派生拖尾桶**：它在发射前调用，换图/自旋要到 `InitAttackModeShow` 才解析，按基础 `visual_name` 注册的拖尾桶对换图弹道永远收不到采样点(方案2 = 场景多一个常驻空跑 VFX、方案1 = 白克隆轨迹材质)。拖尾统一由 `EnsureAttackModeVisual(BaseAttackMode)` 按**实际签名**派生。
- **per-instance 视觉参数(逐弹差异)**：武器 `attack_mode_data`(ItemsInfo)按发设置的缩放/起始角/自旋，已迁成 `BaseAttackMode` 的 `visualScale`/`visualStartAngle`/`spinSpeed`/`spinAxis` 字段，`RenderAll` 据此构建每发的 TRS 矩阵(缩放 + 起始角绕视图前向)。**`visualScale` 默认 `-1`=未配置**(武器无 `StartSize` 时保持，已删旧 `uniformScale` 统一缩放)：矩阵缩放取 1，弹体大小交由**桶共享材质自身的 `_VertexScale`**(`Shader_Mesh_Common_1`「变换>大小」参数，默认1)决定；配了 `StartSize` 才 `visualScale>=0` 用武器值覆盖。要给整类弹道统一改大小又不逐武器配 `StartSize`，直接调该 visual 材质的 `_VertexScale`。**自旋不进矩阵**：`ApplyBucketSpin` 把 `spinAxis×spinSpeed`(每轴 度/秒)直接写进**桶共享材质**的 shader 自转参数(`_RotateSpeed` + 开 `_ROTATE_TIME_ON`；spinSpeed=0 则关)，由 shader 按全局 `_Time` 自转(方向靠 spinAxis 符号)——故已删 `spawnTime`。⚠️它**在 `RegisterVisual` 注册期调一次**，不在 `RenderAll` 热路径：桶签名已含自旋→整桶自旋恒定，逐发重写纯属白费（旧实现逐发调 + `appliedRotateSpeed` 变化检测，现已一并删除）。**per-instance 相位**：材质自转全桶同速同相(任意时刻角度一样)，故每发随机一个 `spinPhase`(`StartAttackBase` 里 `Random.Range(0,360)`)，`RenderAll` 在 `spinSpeed≠0` 时把它作绕 `spinAxis` 的静态角叠进 TRS→「同速不同相」各转各的；要不同速/不同向须改回整段自旋走矩阵(材质关 `_ROTATE_TIME_ON`)。⚠️材质整桶共享(同 visual 多武器不同自旋会互相覆盖)；⚠️别同时让材质烤 `_RotateSpeed`+`_ROTATE_TIME_ON` 又走 DSP 矩阵自旋——两套同速反向会**抵消看似不转**(骨头 200001 踩过)。`HandleItemsInfoAttackModeData`(ItemsInfoBeanPartial) **双写**：既写这些字段(供 DSP)、又保留写 spriteRenderer/material(供现有 sprite 渲染)。`InitAttackModeShow` 开头 `ResetVisualParams()` 还原默认(对象池复用不残留)、末尾调 `FightManager.EnsureAttackModeVisual(this)`(BaseAttackMode 重载)登记视觉桶。
- **子桶分桶(ShowSprite 换图 + 自旋，方案B 已落地)**：GPU Instancing 整批共用一张贴图/一份材质，「逐弹换图」「逐弹不同自旋」不能单桶表达(解决上面 ⚠️自旋互相覆盖的旧限制)。按**视觉签名** `AttackModeInstanceRenderer.BuildVisualBucketKey(visual_name, ShowSprite名, spinAxis×spinSpeed)` 细分子桶：无换图无自旋=默认桶(`visual_name`,复用基础 sharedMaterial)；有覆盖项才拼签名,每个不同(贴图,自旋)组合各占独立子桶+**克隆基材质**互不覆盖,桶内仍合批。**缩放/起始角/相位是逐弹矩阵参数,不进签名**(单桶即可逐弹不同),只有影响共享材质的贴图/自旋才分桶。key 缓存 `BaseAttackMode.visualBucketKey`,`RenderAll` 按它取桶(替代旧的直接读 `visual_name`);新字段 `visualBucketKey`/`visualSpriteName`(`ResetVisualParams` 清空)。注册:`EnsureAttackModeVisual(BaseAttackMode)`(与配置版 `EnsureAttackModeVisual(AttackModeInfoBean)` 重载,非弹道专属)默认签名走配置版基础桶(已抽 `TryGetAttackModeVisualSource` 取源);子桶克隆材质缓存 `dicAttackModeVisualMat`(去重+整场 `ClearAttackModeAssetCache` 统一 `Destroy`),**换图子桶异步**从图集(`IconHandler.GetIconSprite(Items,名)`)取 sprite→`DataUtility.GetOuterUV` 算图集内 UV→写克隆材质 `_BaseMap`+`_BaseMap_ST`(shader `TRANSFORM_TEX` 采样,**不改 shader**)→**贴图就绪后才 `RegisterVisual`**(未就绪当帧被跳过不画,数帧后显),仅自旋子桶直接登记。⚠️**换图带宽高比修正(须在 shader 对象空间做)**：DSP 是固定 1×1 方 Quad,非方形 sprite 会拉伸(症状"高被拉长")→按 `sprite.rect` contain 归一化(长边=1)写克隆材质 `_VertexScaleXY`(`Shader_Mesh_Common_1` 新增,对象空间 XY 缩放,默认(1,1)),shader 在**自旋之前最内层**缩放。⚠️**别再塞进 `RenderAll` 矩阵缩放**：自旋是 shader 转的(在实例矩阵内层),矩阵非均匀缩放叠外层会让自旋武器(`VertexRotateSpeed≠0`,如骷髅骨头)随角度**抖动**;放 shader 最内层(自旋前)才对,不自旋(弓箭)也正确。默认桶 (1,1) 不受影响。⚠️每种(贴图,自旋)组合 +1 draw call(通常几种可忽略;自旋极离散致膨胀再考虑把 `_RotateSpeed` 提成 per-instance 属性);⚠️图集开旋转/紧密打包会致 `GetOuterUV` 矩形 UV 错位(Items 图集未开,可接受)。`ItemsInfoBeanPartial` ShowSprite 分支现**多写** `attackMode.visualSpriteName`(供 DSP 子桶)+保留 spriteRenderer 换图。
- **缓存跨关卡保留、整场结束才释放**：关卡间(`ClearGameForSimple`/`ClearAttackModePrefab`)**不释放** `dicAttackModeVisualObj`/`dicAttackModeObj`，保留供下关复用；**打完所有关卡**(`ClearGame`→`FightManager.Clear`)时调 `ClearAttackModeAssetCache()`——`LoadAddressablesUtil.Release` 释放弹道预制+视觉预制的 Addressables 句柄、清空两个 dict、`attackModeInstanceRenderer.ClearVisuals()` 清桶。`UnregisterVisual` 仅供热替换。
- **Lit 材质亮度对齐(平坦环境光补偿)**：`DrawMeshInstanced` 的 `SampleSH` 读不到全局环境探针(本 shader 未启用逐实例 SH，故 CustomProvided+`CopySHCoefficientArraysFrom` 灌 SH **无效**)，开 `_LIT_ON` 的桶材质比"拖预制到场景"的 `MeshRenderer` **偏暗一份环境光**；且实例化绘制也拿不到逐物体附加光——但战斗场景只有平行光+天光，差异纯是环境光。**确定性修法**：`RenderAll` 开头 `RefreshAmbientSH()` 把 `RenderSettings.ambientProbe` 6 轴求值取平均得平坦 GI，`sharedMPB.SetVector("_InstancedFlatGI", ...)`(仅环境光变化时重求值)；`Shader_Mesh_Common_1` 新增 `_InstancedFlatGI`，Lit 分支 `litColor.rgb += col.rgb * _InstancedFlatGI.rgb`。绘制走 `DrawBucket`→带 MPB 的完整重载 + `LightProbeUsage.Off`；普通渲染不设该属性=0 不受影响，不开 Lit 也无副作用。**⚠️MPB 必须运行时懒建(`RefreshAmbientSH` 里 `if(null)new`)，勿写字段初始化器**——本类在 MonoBehaviour `FightManager` 构造期被 new，字段初始化器里 `new MaterialPropertyBlock()` 会触发 `CreateImpl is not allowed from a MonoBehaviour constructor` 并连带组件创建失败。
- **逐实例世界速度 `_VelocityWS` + 种子 `_SeedOffset`（火球/冰球这类"shader 内自带粒子"的桶专用）**：`Shader_Mesh_FireBallInstanced_1` 在 vertex shader 里模拟火星，但 shader 只知道弹体**现在**在哪、没有"每颗火星出生时弹体在哪"的记忆，故火星默认**刚性绑在实例矩阵上**跟着火球平移(像挂在车上的装饰)。修法=喂**逐实例世界速度矢量**(方向×速率，单位/秒)，shader 按 `出生点 = 当前位置 − 速度 × 已存活秒数` 把每颗火星退回出生地→脱离火球被甩在身后。`_SeedOffset` 同理必须灌(复用每发的 `spinPhase`)：`_Time.y` 是全局的，不灌则同屏所有火球的火星**同一帧同时爆同时灭**。
  - **速度来源=渲染器帧差分**：`CalculateVelocityWS(attackMode, deltaTime)` 用 `(position − lastRenderPosition) / deltaTime`(`BaseAttackMode.lastRenderPosition` 新字段，`StartAttackBase` 里与 `position` 同步初始化防对象池残留)。差的是本帧平均速度，故直线/追踪/抛物线弹**全部自动正确**，无需每种 AttackMode 各自暴露飞行方向。⚠️**瞬移钳制**：`SetPosition` 可瞬间改位置，差分会算出荒谬速度把火星甩到天边→超过 `GetMoveSpeed() × VelocityClampRate`(1.5，余量给抛物线弹的重力分量)即判瞬移、本帧取 0(退化一帧不可察)；`speed_move=0` 的原地弹道同样取 0。
  - **零成本降级**：桶是否启用由 `RegisterVisual` 里 `material.HasProperty(_VelocityWS)` 判定一次(`VisualBucket.hasVelocity`)。未声明该属性的桶**两个缓冲恒为 null**(不占内存)、热路径一条指令不多走；shader 侧属性默认 0 → 退化成"火星挂在弹体上"，不灌不会坏(挂 `MeshRenderer` 单发预览即走此降级：逐实例属性无 MPB 数组时回退读材质值，故两属性均 `[HideInInspector]`——材质面板填的值对 DSP 路径无效)。
  - **⚠️数组现灌现画、共用 `sharedMPB`**：`DrawBucket` 里 `Set*Array` 后紧接着提交，各桶轮流借用同一个 MPB 不会串数据，故**无需每桶再建 MPB**(那样反要为每个新 MPB 同步补灌 `_InstancedFlatGI`，漏灌即偏暗)。必须每桶一份的只有 `velocityBuffer`/`seedBuffer` 数组本身——各桶填充是交错进行的。定长 1023 整份上传(与轨迹桶 `_TrailAlpha` 同理)。
  - **⚠️网格 bounds 必须覆盖拖拽距离**：火星退回出生点后实际占据"火球身后 弹速×火星寿命"的范围，而 `DrawMeshInstanced` 用 `实例矩阵 × mesh.bounds` 逐实例剔除、**没有外部传 bounds 的口子** → bounds 不够大时火球自身一出画面，还留在画面内的火星尾巴会**整条突然消失**。故「火球网格生成器」的**包围盒半径默认已提到 13** = 最大弹速 9(`speed_move` 上限 3 × `attackerSpeedRate` 上限 `SpeedRateASPDMax` 3) × 火星最大寿命 1.43s(`1/(_SparkRate 1 × 生命倍率下限 0.7)`)。⚠️拖拽在世界空间做(不随缩放变)、bounds 却是物体空间且被实例矩阵 `visualScale` 缩小 → `visualScale<1` 的弹道需按 `13/visualScale` 再放大。**改弹速/`_SparkRate`/生命倍率后须重新生成网格**(bounds 是烤进 mesh 资源的，改生成器默认值不影响已生成的 `FireSparkMesh.asset`)。
- **已知局限/待办**：拖尾(轨迹)已落地（见上方「拖尾（轨迹）」）；朝相机 billboard 交由 shader（`RenderAll` 目前用单位旋转 + `visualScale`(未配置回退材质 `_VertexScale`)，Editor 里看到的是未朝相机的静态 Quad；火球 shader 例外——它自己在世界空间取相机右/上轴展开 billboard）；`AttackModeRangedSplit` 不纳入（自管多 GameObject）。

## 约束

- 攻击模式必须继承 BaseAttackMode
- **位置读写走 `position`（`SetPosition`/`TranslatePosition`），禁止直接操作弹体 `transform` 位置**（见上「关键字段 position」）；新增移动型弹道沿用此约定，才能被 AttackModeInstanceRenderer 正确批量绘制
- **弹道的移动/计时必须用 `GameFightLogic.GetFightDeltaTime()`**（= `Time.deltaTime × 当前游戏速度`，非战斗场景恒 1 倍）替代 `Time.deltaTime`——否则 2倍速（`fightData.gameSpeed=2`）下弹道飞行仍是 1 倍节奏；现有 `HandleForMove`/重力加速度/归位移动已全部遵守
- 每种攻击模式独立一个文件
- 文件名与类名一致：AttackMode + 类型名
- **禁止在热路径调用 `GameHandler.Instance.manager.GetGameLogic<GameFightLogic>()`**，需要战斗逻辑统一通过 `FightHandler.Instance.manager.GetCachedFightLogic()`（懒加载，`FightManager.Clear()` 会自动失效）
- 维护跨帧状态（如连锁记录、穿透命中名单、候选缓冲）的子类必须重写 `Destroy(bool)` 清空它们，否则下一次出对象池时会带上一次的数据
- 复用候选缓冲使用 `readonly List<>` 字段配合 `Clear()`，禁止在 Update 里 `new List<>` 造成 GC
- `effect_hit` 配置允许 `&` 分隔多组特效，调用 `PlayEffectForHit(pos, index)` 时通过 `index` 选择（如 `AttackModeFalluponChain` 用 0/1 区分初始击中与连锁击中）
- **射线命中检测走批处理，禁止在 `Update()` 里 live `Physics.Raycast*`**：`FightHandler.UpdateHandleForAttackModePrefab` 为两段式（收集 `PrepareRaycast` → `FightRaycastBatch.Schedule` 批量并行 `RaycastCommand` → 消费 `Update`）。射线类弹道重写 `PrepareRaycast` 入队即可，`CheckHitTargetForSingle/CheckHitTarget` 自动读结果；单条命中窗口上限 `FightRaycastBatch.MaxHitsPerRay`(当前4)。逻辑三阶段之后有**阶段4 `attackModeInstanceRenderer.RenderAll`** 批量绘制（见上「弹道渲染」）。详见 attack-mode-system skill「射线检测批处理」
