---
name: game-attack-mode
description: 攻击模式系统开发：17种攻击模式（近战/远程/特殊/恢复），BaseAttackMode 策略模式。
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
- **AttackModeRangedSplit** - 分裂远程
- **AttackModeRangedTracking** - 追踪远程

### 特殊 (Special)
- **AttackModeExplosion** - 爆炸
- **AttackModeFallupon** - 降临
- **AttackModeFalluponArea** - 范围降临
- **AttackModeFalluponChain** - 连锁降临
- **AttackModeLure** - 引诱
- **AttackModeOverlap** - 重叠

### 恢复 (Regain)
- **AttackModeRegain** - 恢复基类
- **AttackModeRegainHP** - HP 恢复
- **AttackModeRegainDR** - DR 恢复

## BaseAttackMode 关键字段

- **`isValid`** - 是否激活；`Destroy()` 时置 `false`，外层遍历据此跳过
- **`instanceId`** - `FightManager` 分配的实例 ID，`dlAttackModePrefab`（DictionaryList）按此 key 做 O(1) 移除
- **`position`** - 弹道当前世界坐标，**位置真实源（DSP 方案B 权威源）**。子类移动/定位一律走 `SetPosition(pos)`/`TranslatePosition(delta)`，**禁止**再直接写 `gameObject.transform.position` 或 `transform.Translate`；这两个 helper 会在写 `position` 的同时同步回 `transform`（gameObject 预制字段保留、旧渲染兼容）。起点/射线起点/命中/边界检测均已改读 `position`（`CheckIsMoveBound()` 无参重载读 `position`，`CheckIsMoveBound(GameObject)` 为兼容重载）。读取**别的生物**位置仍用 `creatureObj.transform.position`（那不是弹体自身）。例外：`AttackModeRangedSplit` 自管多个分裂弹 GameObject，未迁移、不纳入 DSP 渲染
- **`searchCreatureType`** - 由 `attackedLayerTarget` 推导出的搜索类型，在 `StartAttack(attacker,...)` 中缓存、`Destroy()` 中清零，子类范围检测应复用而非每帧重算
- **`batchRayStart`** - 本帧射线批处理命令索引（`>=0` 表示已入队射线，`CheckHitTarget*` 直接读批处理结果；`-1` 走 live 路径）；由 `PrepareRaycast` 每帧重置/赋值
- **`SpeedRateASPDMax`** - 攻速 ASPD=100 时弹道飞行速度的最大加成倍率常量（当前 3 倍，数值调整入口）；`StartAttack(attacker,...)` 时把攻击者 ASPD 按 0~100 线性插值成 1~该倍率，快照进 `attackModeData.attackerSpeedRate`（与 `attackerDamage`/`attackerCRT` 同一快照模式）
- **`GetMoveSpeed()`** - 弹道实际飞行速度 = `attackModeInfo.speed_move × attackModeData.attackerSpeedRate`；远程系（Ranged/Arc/Tracking/Split 及其子类）的移动计算必须用它，**禁止**直接读 `attackModeInfo.speed_move`（天降 Fallupon 的下落速度不吃攻速加成，仍直接用配置值）

### 拖尾（轨迹）

> 命名：本节的"轨迹"指弹道拖尾（`TrailBucket`），与 framework-core 的冲刺残影 `AfterimageGhost*` 是**两套无关系统**，勿混。

- **效果定义** - 拖尾 = **弹体贴图本身画在若干历史位置上、越老越透明**（类似冲刺/突进残影），不是连续条带。放弃了旧的三角带 billboard 方案（`AppendTrailStrip` 逐点切线×相机朝向展宽 + 逐帧建 Mesh），改为白嫖 DSP 的 GPU Instancing：轨迹就是弹体实例在历史点上多画几遍，CPU 开销≈每档一次 `DrawMeshInstanced`
- **启用来源** - 配置表 `excel_attackmode_info` 的 `trail_data` 列（单列打包，`&` 分隔项、`:` 分键值，如 `type:1&count:6&interval:0.05&startAlpha:0.5&endAlpha:0.05&color:1,1,1`）；由 `AttackModeInfoBeanPartial.GetTrailConfig()` 解析缓存为 `AttackModeTrailConfig`（`count`>0 且 `interval`>0 才 `enable`；未配 `type` 默认 Instanced、未配透明度默认 `startAlpha=0.5`/`endAlpha=0.05`）。字段：`type`(渲染方式枚举 `AttackModeTrailType`：1=Instanced 默认/2=Vfx)、`count`(轨迹段数)、`interval`(采样间隔秒)、`startAlpha`(最新档透明度)、`endAlpha`(最老档透明度)、`color`(染色 rgb，alpha 由 start/endAlpha 决定；⚠️**方案1 下是桶级**——同 `visual_name` 的多行只有首个注册者的 color 生效，**方案2 下才逐弹生效**)。⚠️**`count`/`interval`/`startAlpha`/`endAlpha` 仅方案1 有效**——方案2 的这些表现已写死在 `EffectHandler` 的 `TrailVfx*` 常量里，`type:2` 的行**只需配 `type`+`color`**，填了其余键会被静默忽略。⚠️**`enable` 判定随 type 而异**：方案1=`count>0 && interval>0`；方案2=配了 `type:2` 即启用(否则只写 `type:2&color:...` 会被整条关掉)。**`type` 只是 `trail_data` 字符串内的键，Excel 列本身不变，改它无需重生成 Bean/JSON**。本节余下描述的是**方案1(Instanced)**；方案2(Vfx)见下方独立条目。**必须配合 `visual_name` 走 DSP**——轨迹材质 = **克隆弹体桶材质**（继承贴图/UV/宽高比 `_VertexScaleXY`/缩放 `_VertexScale`）经 `SetupTrailMaterial` 翻成透明+无光+冻结自旋，无需模板材质/独立 shader
- **历史缓冲字段** - `BaseAttackMode`：`trailMode`(单个 `AttackModeTrailType` 三态字段 None/Instanced/Vfx，**已合并旧的 `trailEnabled`/`trailVfxEnabled` 两 bool**；仅 Instanced 才采样环形缓冲、Vfx 不分配缓冲)、`trailPoints`(环形位置缓冲,懒分配 `TrailMaxPoints=32`=轨迹段数上限)、`trailSpinAngles`(环形自旋角缓冲,与 `trailPoints` 一一对应,记录每个采样点当时的时间自转角=`spinSpeed×now`,供轨迹复现旋转姿态,无自旋恒0)、`trailCount`/`trailHead`(环形写指针)、`trailSampleInterval`(=`config.interval`)。方法：`EnableTrail(config)`(懒分配两条缓冲+清空+取间隔)、`SampleTrail(now)`(按间隔 push `position`+自旋角)、`GetTrailPoint(orderIndex)`/`GetTrailSpinAngle(orderIndex)`(0=最老→count-1=最新)、`ResetTrail()`。（已删除 `trailConfig` 字段——轨迹参数由渲染器在 `TrailBucket` 里缓存）
- **生命周期** - `InitAttackModeShow` 开头 `ResetVisualParams()` 关拖尾、末尾 `EnsureAttackModeVisual(this)` 按 `GetTrailConfig()` 调 `EnableTrail` 重开并清空；每次发射（含对象池复用）都走 `StartAttackInit`→此链路，**零残留**。渲染由 `AttackModeInstanceRenderer` 每帧 `RenderAll` 里对启用弹道 `SampleTrail` 并收集进对应 `TrailBucket`，收尾 `DrawTrailBuckets` 按**年龄档**绘制：档 k = 所有弹道的第 k 个最新历史点，整档共享一个 alpha（`Lerp(startAlpha,endAlpha)`）一次 `DrawMeshInstanced`；由老到新绘制（近处不透明档叠在远处透明档上）。**每桶每帧仅多 `count` 次 draw call，与弹道数无关**
- **旋转弹道的轨迹（如骷髅骨头 200001）** - 轨迹材质本身**冻结**了时间自转（`SetupTrailMaterial` 关 `_ROTATE_TIME_ON`），但**每个采样点的旋转姿态由 `BuildInstanceMatrix(attackMode, pos, extraSpinAngle)` 的 `extraSpinAngle` 烤进矩阵**：弹体本体传 0（时间自转交 shader），轨迹传该采样点的 `GetTrailSpinAngle`（=`spinSpeed×采样时刻`，绕 `spinAxis`）。故旋转弹道的轨迹会**复现当时的旋转角**（一串逐渐转过的骨头），而非同一角度。⚠️采样时刻用 `Time.timeSinceLevelLoad`（与 shader `_Time.y` 同基准），使最新档的轨迹角与弹体本体连续、不脱节；⚠️自旋角与 shader 完全对齐仅在**单轴自旋**（billboard 绕 Z，本项目情形）下成立，多轴自旋因 euler 合成顺序差异会有偏差
- **方案2（VFX，`type:2`，C# 绑定已实现；图 `VFX_Trail_1.vfx` 已建）** - **单个 GPU VFX 特效**，每帧经两条 `GraphicsBuffer` 一次性上传全部子弹位置**+逐弹染色**喷射轨迹粒子，**合一 draw call、与子弹数无关**（对比方案1 `桶数×count`）。缺图时 `type:2` 静默不显示（不影响弹体本体/方案1、不报错）。⚠️**落点：VFX 逻辑全部归 `EffectHandler`，`AttackModeInstanceRenderer` 不碰粒子**(与血液/护盾同一分工：调用方只给语义数据，粒子实例/参数/缓冲由 Effect 系统自管——改拖尾 VFX 一律去 EffectHandler)。**EffectHandler**「攻击弹道拖尾粒子(方案2 VFX)」区：VFX 属性名 ID + **表现常量 `TrailVfxLifetime`(1s)/`TrailVfxSpawnInterval`(0.02s)/`TrailVfxStartAlpha`(0.5)/`TrailVfxEndAlpha`(0.05)/`TrailVfxParticleSize`(0.1)**(⚠️桶级参数已从配置表移除、写死于此，调表现改这里) + `RegisterAttackModeTrailVfx(visualKey)`(**只收桶签名，不收 config 也不收桶材质**；去重+实例化+**就地灌一次性参数**。粒子尺寸曾取弹体材质 `_VertexScale`，现写死为 `TrailVfxParticleSize`；贴图由 VFX 预制自带、不从弹体材质覆盖)/`BeginAttackModeTrailVfxFrame()`/`AddAttackModeTrailVfxPoint(visualKey,position,trailColor)`(直接用 `trailColor` 原值成对 Add，**不乘弹体基色**)/`FlushAttackModeTrailVfxFrame()`(**每帧参数就地设完**：两条 `SetData`+`SetGraphicsBuffer`+`PositionCount` 兼容 uint/int；`EnsureAttackModeTrailVfxBuffer` 两 buffer 同步扩容)/`ClearAttackModeTrailVfx`/`ClearAllAttackModeTrailVfx`/`GetAttackModeTrailModel`(私有,内含懒加载门控)。**状态**：`EffectManager.dicAttackModeTrailVfx`(key=visualKey)/`objAttackModeTrailModel`/`triedLoadAttackModeTrailModel` + `AttackModeTrailVfxBean`(`vfx`/`listPosition`/`listColor`/两 buffer/`bufferCapacity`)。**渲染器仅 3 个调用点**(Register 转交 + Begin/Add/Flush + Clear 转交)，无任何 VFX 类型。**枚举/逐弹字段**：`AttackModeTrailType`(`None`/`Instanced`/`Vfx`)+`AttackModeTrailConfig.type`；`BaseAttackMode.trailMode`(三态)+**`trailColor`**(Vector3 rgb,`EnableTrail` 从自身 `config.color` 设,`ResetVisualParams` 复位白)。**加载链**：`effectAttackModeTrailId`(1600001)→`EffectInfo` res_name(`Effect_Trail_1`)→`GetEffectModelSync`(不实例化/无需 EffectBase)→EffectHandler 每桶 Instantiate 一份；每场至多试一次、缺资源静默降级。⚠️`FightManager.EnsureTrailVfxTemplate`/`triedLoadTrailVfx`/`SetTrailVfxTemplate` **已删除**。⚠️`RenderAll` 列表为空**不可早退**(须走完 Begin→Flush 归零 `PositionCount`，否则子弹死光后 VFX 在残留位置持续喷粒子——旧实现的 bug，已修)。**VFX Graph 暴露属性合同**(名须一致，⚠️**本项目约定无下划线**如血液 `PositionStart`)：`PositionBuffer`/**`ColorBuffer`**(逐弹 rgb,与位置同索引同容量)/`PositionCount`(uint 或 int)/`MainTex`(取弹体 `_BaseMap`)/`StartAlpha`/`EndAlpha`/`Lifetime`(=count×interval)/`SpawnInterval`(=interval)/`ParticleSize`(取常量 `TrailVfxParticleSize`)；图建议 World 空间；Spawn Periodic Burst(Count=`PositionCount`,Delay=`SpawnInterval`)；Initialize `Sample Graphics Buffer`(Type=Vector3,Index=`particleId % max(PositionCount,1)`,⚠️无 spawnIndex 用 `particleId`) 设 position；透明度用手动 `Set Alpha`+`Lerp(StartAlpha,EndAlpha,age/lifetime)`(`Set Alpha over Life` 只吃 Curve)；`Set Color` 接**第二颗 Sample Graphics Buffer**(`ColorBuffer`,**复用位置那颗 Modulo 的输出**——同索引才不错配)；Output Quad 用 `MainTex`×**粒子 color 属性**朝相机。**图已接完**(两条 buffer + 共用同一 Modulo 的两颗 Sample)；**`TrailColor` 已从图与 C# 双双删除**(染色一律逐弹走 `ColorBuffer`)。⚠️**染色作用域**：方案1 桶级(同 `visual_name` 首个注册者赢，其余 color 被忽略)、方案2 逐弹级(同一 VFX 内多色并存)——需同图不同色必须 `type:2`。⚠️但方案2 下 **`count`/`interval`/`startAlpha`/`endAlpha` 仍是桶级**(注册时灌进 VFX 实例，首个 `type:2` 行赢)，只有 `color` 逐弹。⚠️**同 `visual_name` 下 `type:1` 与 `type:2` 可共存**(Instanced/Vfx 两桶分属不同字典，`RenderAll` 按每发的 `trailMode` 路由，互不干扰)。详见 attack-mode-system skill
- **限制** - `AttackModeRangedSplit`(分裂弹)自管多个 GameObject，不纳入 DSP、无拖尾；旧三角带方案的 `FrameWork/URP/AttackTrail` shader 与 `PathInfo.AttackTrailMatPath` 已删除（轨迹改克隆弹体材质）

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
        EnqueueSingleRay(batch);   // 单射线复用；多子弹(Split)则逐个 batch.Enqueue 并记录索引
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
- **分桶 key = `attackModeInfo.visual_name`（新配置字段）**：`RegisterVisual(visualName, mesh, material)` 注册视觉桶（mesh 通常朝相机 Quad、material 须开 GPU Instancing）；每桶固定 `Matrix4x4[1023]` 复用缓冲，满批即绘、收尾绘剩余，无热路径分配。
- **`visual_name` 与 `prefab_name` 是独立的两套渲染通道**：`visual_name` 走 DSP 批量渲染；`prefab_name` 仍是**原预制渲染**（`FightManager.GetAttackModePrefab` Instantiate prefab 挂 SpriteRenderer/VisualEffect，逻辑不变）。配置侧二选一，别同一行两个都填(会双重渲染)。
- **常开(无总开关)但天然零副作用**：`visual_name` 为空、或未 `RegisterVisual` 该桶的弹道会被跳过(什么都不画)——所以现有全部弹道(visual_name 均空)行为不变。
- **`visual_name` 是配置表字段**：加在 `excel_attackmode_info[攻击方式].xlsx` 的 `AttackModeInfo` 表(prefab_name 之后)，需在 Unity 跑 ExcelEditorWindow「生成 Entity + 导出」重新生成 `AttackModeInfoBean` + `AttackModeInfo.json` 后 `attackModeInfo.visual_name` 才可用。
- **视觉资源 = 一个预制(mesh+material)**：`Assets/LoadResources/AttackModeVisual/<visual_name>.prefab`（`PathInfo.AttackModeVisualPath`），预制上挂 `MeshFilter`(Quad) + `MeshRenderer`(material 须开 GPU Instancing)；美术可直接在 Editor 看效果。必须注册进 Addressables(address=全路径)否则加载返 null。
- **懒注册**：`FightManager.EnsureAttackModeVisual(attackModeInfo)` 在 `GetAttackModePrefab` 里被调——visual_name 非空且 `attackModeInstanceRenderer.HasVisual` 为假时，`GetModelForAddressablesSync(dicAttackModeVisualObj, ...)` 加载预制、取 **sharedMesh/sharedMaterial**(勿用 .mesh/.material) 注册。只加载一次。⚠️**此重载只管弹体桶、不派生拖尾桶**：它在发射前调用，换图/自旋要到 `InitAttackModeShow` 才解析，按基础 `visual_name` 注册的拖尾桶对换图弹道永远收不到采样点(方案2 = 场景多一个常驻空跑 VFX、方案1 = 白克隆轨迹材质)。拖尾统一由 `EnsureAttackModeVisual(BaseAttackMode)` 按**实际签名**派生。
- **per-instance 视觉参数(逐弹差异)**：武器 `attack_mode_data`(ItemsInfo)按发设置的缩放/起始角/自旋，已迁成 `BaseAttackMode` 的 `visualScale`/`visualStartAngle`/`spinSpeed`/`spinAxis` 字段，`RenderAll` 据此构建每发的 TRS 矩阵(缩放 + 起始角绕视图前向)。**`visualScale` 默认 `-1`=未配置**(武器无 `StartSize` 时保持，已删旧 `uniformScale` 统一缩放)：矩阵缩放取 1，弹体大小交由**桶共享材质自身的 `_VertexScale`**(`Shader_Mesh_Common_1`「变换>大小」参数，默认1)决定；配了 `StartSize` 才 `visualScale>=0` 用武器值覆盖。要给整类弹道统一改大小又不逐武器配 `StartSize`，直接调该 visual 材质的 `_VertexScale`。**自旋不进矩阵**：`RenderAll` 里 `ApplyBucketSpin` 把 `spinAxis×spinSpeed`(每轴 度/秒)直接写进**桶共享材质**的 shader 自转参数(`_RotateSpeed` + 开 `_ROTATE_TIME_ON`；spinSpeed=0 则关)，由 shader 按全局 `_Time` 自转(方向靠 spinAxis 符号)——故已删 `spawnTime`；按桶缓存 `appliedRotateSpeed` 仅值变化时 SetVector。**per-instance 相位**：材质自转全桶同速同相(任意时刻角度一样)，故每发随机一个 `spinPhase`(`StartAttackBase` 里 `Random.Range(0,360)`)，`RenderAll` 在 `spinSpeed≠0` 时把它作绕 `spinAxis` 的静态角叠进 TRS→「同速不同相」各转各的；要不同速/不同向须改回整段自旋走矩阵(材质关 `_ROTATE_TIME_ON`)。⚠️材质整桶共享(同 visual 多武器不同自旋会互相覆盖)；⚠️别同时让材质烤 `_RotateSpeed`+`_ROTATE_TIME_ON` 又走 DSP 矩阵自旋——两套同速反向会**抵消看似不转**(骨头 200001 踩过)。`HandleItemsInfoAttackModeData`(ItemsInfoBeanPartial) **双写**：既写这些字段(供 DSP)、又保留写 spriteRenderer/material(供现有 sprite 渲染)。`InitAttackModeShow` 开头 `ResetVisualParams()` 还原默认(对象池复用不残留)、末尾调 `FightManager.EnsureAttackModeVisual(this)`(BaseAttackMode 重载)登记视觉桶。
- **子桶分桶(ShowSprite 换图 + 自旋，方案B 已落地)**：GPU Instancing 整批共用一张贴图/一份材质，「逐弹换图」「逐弹不同自旋」不能单桶表达(解决上面 ⚠️自旋互相覆盖的旧限制)。按**视觉签名** `AttackModeInstanceRenderer.BuildVisualBucketKey(visual_name, ShowSprite名, spinAxis×spinSpeed)` 细分子桶：无换图无自旋=默认桶(`visual_name`,复用基础 sharedMaterial)；有覆盖项才拼签名,每个不同(贴图,自旋)组合各占独立子桶+**克隆基材质**互不覆盖,桶内仍合批。**缩放/起始角/相位是逐弹矩阵参数,不进签名**(单桶即可逐弹不同),只有影响共享材质的贴图/自旋才分桶。key 缓存 `BaseAttackMode.visualBucketKey`,`RenderAll` 按它取桶(替代旧的直接读 `visual_name`);新字段 `visualBucketKey`/`visualSpriteName`(`ResetVisualParams` 清空)。注册:`EnsureAttackModeVisual(BaseAttackMode)`(与配置版 `EnsureAttackModeVisual(AttackModeInfoBean)` 重载,非弹道专属)默认签名走配置版基础桶(已抽 `TryGetAttackModeVisualSource` 取源);子桶克隆材质缓存 `dicAttackModeVisualMat`(去重+整场 `ClearAttackModeAssetCache` 统一 `Destroy`),**换图子桶异步**从图集(`IconHandler.GetIconSprite(Items,名)`)取 sprite→`DataUtility.GetOuterUV` 算图集内 UV→写克隆材质 `_BaseMap`+`_BaseMap_ST`(shader `TRANSFORM_TEX` 采样,**不改 shader**)→**贴图就绪后才 `RegisterVisual`**(未就绪当帧被跳过不画,数帧后显),仅自旋子桶直接登记。⚠️**换图带宽高比修正(须在 shader 对象空间做)**：DSP 是固定 1×1 方 Quad,非方形 sprite 会拉伸(症状"高被拉长")→按 `sprite.rect` contain 归一化(长边=1)写克隆材质 `_VertexScaleXY`(`Shader_Mesh_Common_1` 新增,对象空间 XY 缩放,默认(1,1)),shader 在**自旋之前最内层**缩放。⚠️**别再塞进 `RenderAll` 矩阵缩放**：自旋是 shader 转的(在实例矩阵内层),矩阵非均匀缩放叠外层会让自旋武器(`VertexRotateSpeed≠0`,如骷髅骨头)随角度**抖动**;放 shader 最内层(自旋前)才对,不自旋(弓箭)也正确。默认桶 (1,1) 不受影响。⚠️每种(贴图,自旋)组合 +1 draw call(通常几种可忽略;自旋极离散致膨胀再考虑把 `_RotateSpeed` 提成 per-instance 属性);⚠️图集开旋转/紧密打包会致 `GetOuterUV` 矩形 UV 错位(Items 图集未开,可接受)。`ItemsInfoBeanPartial` ShowSprite 分支现**多写** `attackMode.visualSpriteName`(供 DSP 子桶)+保留 spriteRenderer 换图。
- **缓存跨关卡保留、整场结束才释放**：关卡间(`ClearGameForSimple`/`ClearAttackModePrefab`)**不释放** `dicAttackModeVisualObj`/`dicAttackModeObj`，保留供下关复用；**打完所有关卡**(`ClearGame`→`FightManager.Clear`)时调 `ClearAttackModeAssetCache()`——`LoadAddressablesUtil.Release` 释放弹道预制+视觉预制的 Addressables 句柄、清空两个 dict、`attackModeInstanceRenderer.ClearVisuals()` 清桶。`UnregisterVisual` 仅供热替换。
- **Lit 材质亮度对齐(平坦环境光补偿)**：`DrawMeshInstanced` 的 `SampleSH` 读不到全局环境探针(本 shader 未启用逐实例 SH，故 CustomProvided+`CopySHCoefficientArraysFrom` 灌 SH **无效**)，开 `_LIT_ON` 的桶材质比"拖预制到场景"的 `MeshRenderer` **偏暗一份环境光**；且实例化绘制也拿不到逐物体附加光——但战斗场景只有平行光+天光，差异纯是环境光。**确定性修法**：`RenderAll` 开头 `RefreshAmbientSH()` 把 `RenderSettings.ambientProbe` 6 轴求值取平均得平坦 GI，`sharedMPB.SetVector("_InstancedFlatGI", ...)`(仅环境光变化时重求值)；`Shader_Mesh_Common_1` 新增 `_InstancedFlatGI`，Lit 分支 `litColor.rgb += col.rgb * _InstancedFlatGI.rgb`。绘制走 `DrawBucket`→带 MPB 的完整重载 + `LightProbeUsage.Off`；普通渲染不设该属性=0 不受影响，不开 Lit 也无副作用。**⚠️MPB 必须运行时懒建(`RefreshAmbientSH` 里 `if(null)new`)，勿写字段初始化器**——本类在 MonoBehaviour `FightManager` 构造期被 new，字段初始化器里 `new MaterialPropertyBlock()` 会触发 `CreateImpl is not allowed from a MonoBehaviour constructor` 并连带组件创建失败。
- **已知局限/待办**：拖尾(轨迹)已落地（见上方「拖尾（轨迹）」）；朝相机 billboard 交由 shader（`RenderAll` 目前用单位旋转 + `visualScale`(未配置回退材质 `_VertexScale`)，Editor 里看到的是未朝相机的静态 Quad）；`AttackModeRangedSplit` 不纳入（自管多 GameObject）；示例 visual 预制与 instanced 火焰 shader 资源尚未建（C# 骨架就位，建好并登记 Addressables 后即生效）。

## 约束

- 攻击模式必须继承 BaseAttackMode
- **位置读写走 `position`（`SetPosition`/`TranslatePosition`），禁止直接操作弹体 `transform` 位置**（见上「关键字段 position」）；新增移动型弹道沿用此约定，才能被 AttackModeInstanceRenderer 正确批量绘制
- 每种攻击模式独立一个文件
- 文件名与类名一致：AttackMode + 类型名
- **禁止在热路径调用 `GameHandler.Instance.manager.GetGameLogic<GameFightLogic>()`**，需要战斗逻辑统一通过 `FightHandler.Instance.manager.GetCachedFightLogic()`（懒加载，`FightManager.Clear()` 会自动失效）
- 维护跨帧状态（如连锁记录、穿透命中名单、候选缓冲）的子类必须重写 `Destroy(bool)` 清空它们，否则下一次出对象池时会带上一次的数据
- 复用候选缓冲使用 `readonly List<>` 字段配合 `Clear()`，禁止在 Update 里 `new List<>` 造成 GC
- `effect_hit` 配置允许 `&` 分隔多组特效，调用 `PlayEffectForHit(pos, index)` 时通过 `index` 选择（如 `AttackModeFalluponChain` 用 0/1 区分初始击中与连锁击中）
- **射线命中检测走批处理，禁止在 `Update()` 里 live `Physics.Raycast*`**：`FightHandler.UpdateHandleForAttackModePrefab` 为两段式（收集 `PrepareRaycast` → `FightRaycastBatch.Schedule` 批量并行 `RaycastCommand` → 消费 `Update`）。射线类弹道重写 `PrepareRaycast` 入队即可，`CheckHitTargetForSingle/CheckHitTarget` 自动读结果；单条命中窗口上限 `FightRaycastBatch.MaxHitsPerRay`(当前4)。逻辑三阶段之后有**阶段4 `attackModeInstanceRenderer.RenderAll`** 批量绘制（见上「弹道渲染」）。详见 attack-mode-system skill「射线检测批处理」
