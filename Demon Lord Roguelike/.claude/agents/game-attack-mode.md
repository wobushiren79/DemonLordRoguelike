---
name: game-attack-mode
description: 攻击模式系统开发：17种攻击模式（近战/远程/特殊/恢复），BaseAttackMode 策略模式。
tools: Read, Write, Edit, Glob, Grep, Bash
watched_files:
  - Assets/Scripts/Game/Fight/AttackMode/
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

- **[AttackModeInstanceRenderer](Assets/Scripts/Game/Fight/AttackMode/AttackModeInstanceRenderer.cs)**：纯 C# 类（非 MonoBehaviour，不持有 GameObject），`FightManager.attackModeInstanceRenderer` 持有，`FightHandler.UpdateHandleForAttackModePrefab` **阶段4** 调 `RenderAll(listAttackMode)`。
- **分桶 key = `attackModeInfo.visual_name`（新配置字段）**：`RegisterVisual(visualName, mesh, material)` 注册视觉桶（mesh 通常朝相机 Quad、material 须开 GPU Instancing）；每桶固定 `Matrix4x4[1023]` 复用缓冲，满批即绘、收尾绘剩余，无热路径分配。
- **`visual_name` 与 `prefab_name` 是独立的两套渲染通道**：`visual_name` 走 DSP 批量渲染；`prefab_name` 仍是**原预制渲染**（`FightManager.GetAttackModePrefab` Instantiate prefab 挂 SpriteRenderer/VisualEffect，逻辑不变）。配置侧二选一，别同一行两个都填(会双重渲染)。
- **常开(无总开关)但天然零副作用**：`visual_name` 为空、或未 `RegisterVisual` 该桶的弹道会被跳过(什么都不画)——所以现有全部弹道(visual_name 均空)行为不变。
- **`visual_name` 是配置表字段**：加在 `excel_attackmode_info[攻击方式].xlsx` 的 `AttackModeInfo` 表(prefab_name 之后)，需在 Unity 跑 ExcelEditorWindow「生成 Entity + 导出」重新生成 `AttackModeInfoBean` + `AttackModeInfo.json` 后 `attackModeInfo.visual_name` 才可用。
- **视觉资源 = 一个预制(mesh+material)**：`Assets/LoadResources/AttackModeVisual/<visual_name>.prefab`（`PathInfo.AttackModeVisualPath`），预制上挂 `MeshFilter`(Quad) + `MeshRenderer`(material 须开 GPU Instancing)；美术可直接在 Editor 看效果。必须注册进 Addressables(address=全路径)否则加载返 null。
- **懒注册**：`FightManager.EnsureAttackModeVisual(attackModeInfo)` 在 `GetAttackModePrefab` 里被调——visual_name 非空且 `attackModeInstanceRenderer.HasVisual` 为假时，`GetModelForAddressablesSync(dicAttackModeVisualObj, ...)` 加载预制、取 **sharedMesh/sharedMaterial**(勿用 .mesh/.material) 注册。只加载一次。
- **缓存跨关卡保留、整场结束才释放**：关卡间(`ClearGameForSimple`/`ClearAttackModePrefab`)**不释放** `dicAttackModeVisualObj`/`dicAttackModeObj`，保留供下关复用；**打完所有关卡**(`ClearGame`→`FightManager.Clear`)时调 `ClearAttackModeAssetCache()`——`LoadAddressablesUtil.Release` 释放弹道预制+视觉预制的 Addressables 句柄、清空两个 dict、`attackModeInstanceRenderer.ClearVisuals()` 清桶。`UnregisterVisual` 仅供热替换。
- **已知局限/待办**：拖尾(Trail)暂不做；朝相机 billboard 交由 shader（`RenderAll` 目前用单位旋转 + `uniformScale`，Editor 里看到的是未朝相机的静态 Quad）；`AttackModeRangedSplit` 不纳入（自管多 GameObject）；示例 visual 预制与 instanced 火焰 shader 资源尚未建（C# 骨架就位，建好并登记 Addressables 后即生效）。

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
