---
name: attack-mode-system
description: Demon Lord Roguelike 游戏的攻击模块(AttackMode)系统开发指南。使用此SKILL当需要创建或修改攻击模式、战斗弹道、攻击特效、攻击伤害逻辑等，包括近战/远程/范围/追踪/抛物线/爆炸/回复/连锁/穿透/分裂等攻击类型。
watched_files:
  - Assets/Scripts/Game/Fight/AttackMode/
  - Assets/Scripts/Bean/Game/AttackModeBean.cs
  - Assets/Scripts/Bean/MVC/Game/AttackModeInfoBean.cs
  - Assets/Scripts/Bean/MVC/Game/AttackModeInfoBeanPartial.cs
  - Assets/Scripts/Component/Handler/FightHandler.cs
  - Assets/Scripts/Component/Manager/FightManager.cs
---

# 攻击模块系统开发指南

## 核心概念

### 攻击模块数据结构

```
AttackModeBean       - 攻击模块运行时数据（包含伤害、位置、方向、攻击者/目标ID、弹道速度倍率等）
AttackModeInfoBean   - 攻击模块配置数据（来自配置表）
BaseAttackMode       - 攻击模块逻辑基类（包含碰撞检测、特效播放、生命周期管理等）
```

### 攻击者属性快照（StartAttack 时写入 AttackModeBean）

`StartAttack(attacker, attacked, ...)` 会把攻击者当前属性快照进 `attackModeData`，弹道存活期间不再随属性变化：

| 字段 | 来源属性 | 说明 |
|------|---------|------|
| `attackerDamage` | ATK | 攻击伤害 |
| `attackerCRT` | CRT | 暴击概率 |
| `attackerSpeedRate` | ASPD | 弹道飞行速度倍率：ASPD 按 0~100 线性插值映射 1~`BaseAttackMode.SpeedRateASPDMax`(当前3倍)；无攻击者时保持默认 1，`ClearData()` 重置为 1 |

### BaseAttackMode 关键状态字段

| 字段 | 类型 | 用途 |
|------|------|------|
| `isValid` | `bool` | 是否处于激活状态；`Destroy()` 时置 `false`，`UpdateHandleForAttackModePrefab` 据此跳过已回收对象 |
| `instanceId` | `long` | 由 `FightManager` 分配的实例ID，`dlAttackModePrefab`（DictionaryList）按此 key 进行 O(1) 移除 |
| `searchCreatureType` | `CreatureFightTypeEnum` | 由 `attackedLayerTarget` 推导出的搜索类型，`StartAttack` 时缓存，`Destroy` 时清零，避免每帧重算 |
| `position` | `Vector3` | **弹道当前世界坐标（DSP 方案B 位置真实源）**。移动/定位走 `SetPosition`/`TranslatePosition`（同步回 transform），起点/射线/命中/边界均读它；**禁止**直接写弹体 `transform.position`/`Translate`。读别的生物位置仍用 `creatureObj.transform.position`。供 `AttackModeInstanceRenderer` 批量绘制读取 |
| `gameObject` / `spriteRenderer` | Unity 组件 | 攻击模块可视化对象（预制字段保留：DSP 过渡期作渲染/兼容载体，位置真实源已改 `position`） |
| `attackModeInfo` / `attackModeData` | Bean | 配置数据 / 运行时数据 |

### 攻击模式类型体系

```
BaseAttackMode                      - 攻击模式基类
├── AttackModeMelee                 - 近战单体（瞬间命中目标）
├── AttackModeMeleeArea             - 近战范围（起点范围伤害）
├── AttackModeRanged                - 远程直线弹道（逐帧移动+碰撞检测）
│   ├── AttackModeRangedArea        - 远程范围弹道（击中时范围AOE）
│   ├── AttackModeRangedArc         - 远程抛物线弹道
│   │   └── AttackModeRangedArcArea - 抛物线范围（继承抛物线轨迹）
│   ├── AttackModeRangedTracking    - 远程追踪弹道（实时改变方向追击目标）
│   ├── AttackModeRangedPiercing    - 远程穿透弹道（可穿透多个目标）
│   └── AttackModeRangedSplit       - 远程分裂弹道（分裂为多条线路）
├── AttackModeExplosion             - 爆炸（以自身为中心范围伤害，攻击者死亡）
├── AttackModeFallupon              - 天降单体（直接对锁定目标造成伤害）
├── AttackModeFalluponArea          - 天降范围（对目标位置范围伤害）
├── AttackModeFalluponChain         - 天降连锁（连锁弹射多个目标，伤害递减）
├── AttackModeOverlap               - 重叠检测（范围伤害，无击中特效）
├── AttackModeLure                  - 引诱（改变被攻击者线路）
└── AttackModeRegain                - 回复基类（不造成伤害，提供增益）
    ├── AttackModeRegainHP          - 回复生命
    └── AttackModeRegainDR          - 回复护甲
```

## 创建新攻击模式

### 1. 定义攻击模式ID

```csharp
// 在 AttackModeInfo 配置表中添加新记录
// class_name 字段填写新的攻击模式类名
```

### 2. 选择攻击模式基类

根据攻击行为选择合适的基类：

| 基类 | 行为特征 | 适用场景 |
|------|---------|---------|
| `AttackModeMelee` | 瞬间命中单个目标 | 近战普攻、直接打击 |
| `AttackModeMeleeArea` | 瞬间范围伤害 | 近战AOE、旋风斩 |
| `AttackModeRanged` | 直线飞行弹道 | 箭矢、法术弹 |
| `AttackModeRangedArea` | 飞行弹道+击中AOE | 爆炸箭、火球术 |
| `AttackModeRangedArc` | 抛物线飞行 | 投石、抛物线炸弹 |
| `AttackModeRangedTracking` | 追踪目标飞行 | 追踪弹、导弹 |
| `AttackModeRangedPiercing` | 可穿透多个目标 | 穿透箭、激光 |
| `AttackModeRangedSplit` | 分裂为多路弹道 | 散射弹、分叉箭 |
| `AttackModeExplosion` | 自爆范围伤害 | 自杀式爆炸、亡语 |
| `AttackModeFallupon` | 直接对目标造成伤害 | 天降打击、瞬移攻击 |
| `AttackModeFalluponArea` | 对目标位置范围伤害 | 陨石、天降AOE |
| `AttackModeFalluponChain` | 连锁弹射递减伤害 | 闪电链、弹射攻击 |
| `AttackModeRegain` | 回复而非伤害 | 治疗术、护盾恢复 |
| `BaseAttackMode` | 完全自定义 | 特殊机制攻击 |

### 3. 创建攻击模式类

#### 近战单体示例

```csharp
// Assets/Scripts/Game/Fight/AttackMode/AttackModeMelee.cs
public class AttackModeMelee : BaseAttackMode
{
    public override void StartAttack(FightCreatureEntity attacker, FightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        if (attacker != null && attacked != null && !attacked.IsDead())
        {
            // 扣血（触发受击逻辑）
            attacked.UnderAttack(this);
            // 播放击中粒子特效
            PlayEffectForHit(attacker.creatureObj.transform.position);
        }
        // 攻击完成，回收攻击模块
        Destroy();
        // 触发攻击结束回调
        actionForAttackEnd?.Invoke(this);
    }
}
```

#### 远程弹道示例

```csharp
// Assets/Scripts/Game/Fight/AttackMode/AttackModeRanged.cs
public class AttackModeRanged : BaseAttackMode
{
    public override void StartAttack(FightCreatureEntity attacker, FightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        actionForAttackEnd?.Invoke(this);
    }

    public override void Update()
    {
        base.Update();
        // 检测是否击中单个目标
        FightCreatureEntity fightCreatureEntity = CheckHitTargetForSingle();
        if (fightCreatureEntity != null)
        {
            HandleForHitTarget(fightCreatureEntity);
            return;
        }
        // 移动处理
        HandleForMove();
        // 边界处理（飞太远自动销毁）
        HandleForBound();
    }

    public virtual void HandleForHitTarget(FightCreatureEntity fightCreatureEntity)
    {
        fightCreatureEntity.UnderAttack(this);
        Destroy();
    }

    public virtual void HandleForMove()
    {
        //实际飞行速度 = speed_move × 攻速ASPD加成倍率，统一走 GetMoveSpeed()
        gameObject.transform.Translate(attackModeData.attackDirection * Time.deltaTime * GetMoveSpeed());
    }

    public virtual void HandleForBound()
    {
        if (CheckIsMoveBound(gameObject))
        {
            Destroy();
        }
    }
}
```

#### 范围伤害示例

```csharp
// Assets/Scripts/Game/Fight/AttackMode/AttackModeMeleeArea.cs
public class AttackModeMeleeArea : BaseAttackMode
{
    public void AttackHandle()
    {
        // 检测范围内敌人并执行回调
        CheckHitTargetArea(attackModeData.startPos, (targetFightCreatureEntity) =>
        {
            targetFightCreatureEntity.UnderAttack(this);
        });
        // 播放击中粒子特效
        PlayEffectForHit(attackModeData.startPos);
        // 回收攻击模块
        Destroy();
    }
}
```

#### 穿透弹道示例

```csharp
// Assets/Scripts/Game/Fight/AttackMode/AttackModeRangedPiercing.cs
public class AttackModeRangedPiercing : AttackModeRanged
{
    public int numPierceMax = 3;
    public HashSet<string> listPierceCreature;

    public override void StartAttack(FightCreatureEntity attacker, FightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        listPierceCreature = new HashSet<string>();
    }

    public override void Update()
    {
        List<FightCreatureEntity> listHitTarget = CheckHitTarget();
        if (!listHitTarget.IsNull())
        {
            for (int i = 0; i < listHitTarget.Count; i++)
            {
                var itemCreature = listHitTarget[i];
                string itemCreatureId = itemCreature.fightCreatureData.creatureData.creatureUUId;
                if (listPierceCreature.Contains(itemCreatureId))
                {
                    continue;
                }
                HandleForHitTarget(itemCreature);
                listPierceCreature.Add(itemCreatureId);
                if (listPierceCreature.Count >= numPierceMax)
                {
                    Destroy();
                    return;
                }
            }
        }
        HandleForMove();
        HandleForBound();
    }

    public override void HandleForHitTarget(FightCreatureEntity fghtCreatureEntity)
    {
        fghtCreatureEntity.UnderAttack(this);
    }
}
```

#### 天降连锁示例

```csharp
// Assets/Scripts/Game/Fight/AttackMode/AttackModeFalluponChain.cs
public class AttackModeFalluponChain : BaseAttackMode
{
    public int chainNum = 3;
    public float timeForChainChange = 0.1f;

    //已攻击过的生物ID（用 HashSet 去重，避免重复弹射）
    private HashSet<string> listAttackedCreatureId = new HashSet<string>();
    //连锁候选缓冲（复用，避免每次 new List 产生 GC）
    private readonly List<FightCreatureEntity> listCandidate = new List<FightCreatureEntity>();
    private int currentChainCount = 0;
    private int originalDamage = 0;
    private Action<BaseAttackMode> actionForAttackEnd;
    private FightCreatureEntity currentAttacked;
    private FightCreatureEntity attackerEntity;

    public override async void StartAttack(FightCreatureEntity attacker, FightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        this.actionForAttackEnd = actionForAttackEnd;
        this.attackerEntity = attacker;
        this.currentAttacked = attacked;

        if (attacker == null || attacked == null || attacked.IsDead())
        {
            EndAttack();
            return;
        }

        // 记录原始伤害并执行初始攻击
        originalDamage = attackModeData.attackerDamage;
        ExecuteAttack(attacked, originalDamage, true);

        if (currentChainCount >= chainNum) { EndAttack(); return; }

        // 连锁攻击
        for (int i = 0; i < chainNum; i++)
        {
            await new WaitForSeconds(timeForChainChange);
            if (!CheckGameState()) { EndAttack(); return; }
            if (currentAttacked == null || currentAttacked.creatureObj == null || currentAttacked.IsDead())
            {
                EndAttack();
                return;
            }
            bool hasNextTarget = HandleChainAttack();
            if (!hasNextTarget) { EndAttack(); return; }
        }
        EndAttack();
    }

    // 在 currentAttacked 处做范围检测，过滤已命中目标，随机挑一个作为下一个跳点；伤害按 chainCount 折半（最小 1）
    private bool HandleChainAttack() { /* listCandidate.Clear() 后填充；attackDirection / targetPos 同步更新 */ }

    private void ExecuteAttack(FightCreatureEntity target, int damage, bool isFirst)
    {
        attackModeData.attackerDamage = damage;
        target.UnderAttack(this);
        // 初始击中用 effect_hit[0]，连锁击中用 effect_hit[1]
        PlayEffectForHit(target.creatureObj.transform.position, isFirst ? 0 : 1);
        listAttackedCreatureId.Add(target.fightCreatureData.creatureData.creatureUUId);
    }

    private void EndAttack() { Destroy(); actionForAttackEnd?.Invoke(this); }

    // 归还对象池时清空本次攻击状态，避免复用时残留
    public override void Destroy(bool isPermanently = false)
    {
        listAttackedCreatureId.Clear();
        listCandidate.Clear();
        currentChainCount = 0;
        base.Destroy(isPermanently);
    }
}
```

> **连锁实现要点**：
> - 候选列表 `listCandidate` 与 `listAttackedCreatureId` 在 `Destroy` 中显式清空，确保对象池复用安全；
> - 每次跳点会更新 `attackModeData.attackDirection` / `targetPos`，保证后续 `PlayEffectForHit` / `UnderAttack` 朝向正确；
> - `PlayEffectForHit` 第 2 参数表示 `effect_hit` 中的索引：`0=初始击中特效`，`1=连锁击中特效`；
> - `CheckGameState()` 在每次 `await` 后校验 `gameLogic.gameState == Gaming`，防止战斗结束/暂停期间继续推进连锁。

#### 完全自定义示例

```csharp
// 继承 BaseAttackMode 实现完全自定义逻辑
public class AttackModeCustom : BaseAttackMode
{
    public override void StartAttack(FightCreatureEntity attacker, FightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        // 自定义初始化逻辑...
        actionForAttackEnd?.Invoke(this);
    }

    public override void Update()
    {
        base.Update();
        // 自定义每帧更新逻辑...
    }
}
```

### 4. 配置攻击模式参数

```csharp
// AttackModeInfoBean 关键配置字段
{
    "id": 100001,                    // 攻击模式唯一ID
    "class_name": "AttackModeRanged", // 攻击模式类名（反射创建用）
    "prefab_name": "ArrowPrefab",    // 预制体名称（空表示无预制体）
    "buff": "1001:0.5|1002:1.0",     // 攻击附带的BUFF（ID:创建概率）
    "attack_search_type": 0,         // 攻击搜索类型（0射线 11球形范围 21盒形范围）
    "collider_size": 0.5,            // 碰撞检测大小（点到点）
    "collider_area_type": 11,        // 范围检测类型（11球形 21盒形）
    "collider_area_size": "2,2,2",   // 范围检测大小（半径或半extents）
    "effect_hit": "10001&10002",     // 击中特效ID列表（&分隔，index 0=初始，index 1=连锁）
    "effect_damage": "1002",         // 受伤特效ID（默认不填，0关闭）
    "speed_move": 10,                // 移动速度（远程弹道用；运行时会再乘攻击者的攻速倍率 attackerSpeedRate）
    "sound_start": 100001,           // 起始音效ID（long，默认0不播放；攻击模块创建成功时在 FightManager.GetAttackModePrefab 播放）
    "sound_miss": 1,                 // 未击中音效ID（long）
    "sound_hit": 2,                  // 击中音效ID（long）
    "remark": "远程箭矢"             // 备注
}
```

### 5. 配置预制体（可选）

如果攻击需要可视化表现（如箭矢、火球）：

1. 在 `Assets/LoadResources/AttackMode/` 下创建预制体
2. 在配置表的 `prefab_name` 字段填写预制体名称（不含后缀）
3. 预制体需包含 `SpriteRenderer` 用于显示攻击样式

## 搜索类型枚举

```csharp
CreatureSearchType
├── Ray = 0              // 默认射线，从攻击者射向远处
├── RaySelf = 1          // 射线，从远处射向自己
├── AreaSphere = 11      // 球形范围
├── AreaSphereFront = 14 // 球形范围前方
├── AreaBox = 21         // 盒形范围
├── AreaBoxFront = 24    // 盒形范围前方
├── DisMinByAll = 30     // 遍历距离最近的生物
├── DisMinByRoad = 31    // 遍历同一路线距离最近生物
└── DisMaxByAll = 40     // 遍历距离最远的生物
```

## 常用代码模板

### 创建攻击模块（生物对战）

```csharp
// 通过战斗处理器创建攻击（生物对战）
FightHandler.Instance.StartCreateAttackMode(
    attackerCreature,   // 攻击者
    attackedCreature,   // 被攻击者
    (attackMode) =>     // 创建完成回调
    {
        // 攻击已开始
    },
    customAttackModeId: 100001  // 自定义攻击模式ID（0使用生物自带）
);
```

### 直接创建攻击模块（无目标）

```csharp
AttackModeBean attackModeData = new AttackModeBean(100001);
attackModeData.startPos = startPosition;
attackModeData.attackDirection = direction;
FightHandler.Instance.StartCreateAttackMode(attackModeData);
```

### 获取攻击模式配置

```csharp
AttackModeInfoBean attackModeInfo = AttackModeInfoCfg.GetItemData(attackModeId);
string className = attackModeInfo.class_name;
float moveSpeed = attackModeInfo.speed_move;
List<BuffBean> buffList = attackModeInfo.GetListBuff();
// effect_hit 为 & 分隔的多个粒子ID，用 GetEffectHitId(index) 获取指定位置的ID
long effectId = attackModeInfo.GetEffectHitId(0);  // 第0个（默认/初始）
long chainEffectId = attackModeInfo.GetEffectHitId(1);  // 第1个（连锁）
```

### 播放击中特效

```csharp
// 在攻击模式内部调用
PlayEffectForHit(gameObject.transform.position);
```

### 范围伤害检测

```csharp
// 检测范围内敌人并执行回调
CheckHitTargetArea(checkPosition, (targetCreature) =>
{
    if (targetCreature != null && !targetCreature.IsDead())
    {
        targetCreature.UnderAttack(this);
    }
});
```

### 检测单个目标

```csharp
// 检测是否击中生物（返回第一个命中目标）
FightCreatureEntity hitTarget = CheckHitTargetForSingle();
// 或指定位置检测
FightCreatureEntity hitTarget = CheckHitTargetForSingle(checkPosition);
```

## 攻击模块生命周期

```csharp
// 1. 创建并初始化数据
AttackModeBean attackModeData = FightManager.Instance.GetAttackModeData(attackModeId);

// 2. 获取攻击模块实例（优先对象池，否则反射创建）；
//    出池/新建后 FightManager 会自增 instanceId 并以该 ID 注册到 dlAttackModePrefab
FightManager.Instance.GetAttackModePrefab(attackModeId, (attackMode) =>
{
    attackMode.StartAttackInit(attackModeData);
});

// 3. 开始攻击（两个重载）；StartAttack(attacker,...) 内部会缓存 searchCreatureType（由 attackedLayerTarget 推导）
attackMode.StartAttack();  // 无目标攻击
attackMode.StartAttack(attacker, attacked, actionForAttackEnd);  // 生物对战

// 4. 每帧更新（仅远程/持续型攻击模式）；
//    UpdateHandleForAttackModePrefab 会在循环开始处一次性缓存 count，
//    防止本帧内被 Update 新创建的攻击模块立刻被遍历到（避免一帧内多次 Update）
attackMode.Update();

// 5. 销毁（回收至对象池）
attackMode.Destroy();  // 回收，FightManager 通过 instanceId 在 DictionaryList 中 O(1) 移除
attackMode.Destroy(isPermanently: true);  // 永久销毁（连同 GameObject）
```

> **`FightManager.dlAttackModePrefab` 的演进**：旧版使用 `List<BaseAttackMode> listAttackModePrefab`，每次 `Destroy` 走 `List.Remove(item)` 是 O(N)；现在改用 `DictionaryList<long, BaseAttackMode>` + `instanceId` key，`RemoveByKey(instanceId)` 为 O(1)，同时通过 `.List` 仍保持顺序遍历能力。子类如有需要直接遍历，请使用 `manager.dlAttackModePrefab.List`。

## BaseAttackMode 核心方法速查

| 方法 | 说明 |
|------|------|
| `InitAttackModeShow()` | 初始化攻击样式（根据武器道具ID设置精灵图） |
| `StartAttackInit(AttackModeBean)` | 攻击初始化（设置数据+外观） |
| `StartAttackBase()` | 基础攻击开始（设置GO位置和激活） |
| `StartAttack()` | 无目标攻击 |
| `StartAttack(FightCreatureEntity, FightCreatureEntity, Action)` | 生物对战攻击；缓存 `searchCreatureType`，避免每帧重算 |
| `Update()` | 每帧更新（在射线批处理调度**之后**的消费阶段调用） |
| `PrepareRaycast(FightRaycastBatch)` | 收集阶段调用：走射线的子类重写此方法入队本帧射线；默认仅重置 `batchRayStart=-1`（非射线弹道 no-op） |
| `EnqueueSingleRay(FightRaycastBatch)` | 按当前配置入队 1 条单射线的复用辅助（起点/方向/距离/层级与 `FindCreatureEntityByRay/BySelf` 对齐） |
| `Destroy(bool isPermanently = false)` | 回收或永久销毁；同时将 `isValid=false` 并重置 `searchCreatureType` |
| `PlayEffectForHit(Vector3, int effectIndex = 0)` | 播放击中特效，`effectIndex` 对应 `effect_hit` 中以 `&` 分隔的第几个粒子ID |
| `CheckHitTargetForSingle()` | 检测单个目标 |
| `CheckHitTarget()` | 检测多个目标 |
| `CheckHitTarget(Vector3)` | 在指定位置检测多个目标，使用缓存的 `searchCreatureType` |
| `CheckHitTargetArea(Vector3, Action<FightCreatureEntity>)` | 范围检测并回调；内部使用 `FightManager.GetCachedFightLogic()` 避免每个 collider 反射查询 |
| `GetMoveSpeed()` | 弹道实际飞行速度 = `speed_move × attackerSpeedRate`；远程系（Ranged/Arc/Tracking/Split）移动计算必须用它，禁止直接读 `attackModeInfo.speed_move`（天降 Fallupon 下落速度不吃攻速加成） |
| `GetHitTargetAreaCollider(Vector3)` | 按配置 `collider_area_type` 取范围内 colliders |
| `CheckIsMoveBound(GameObject)` | 检测是否超出边界 |

## 热路径性能约束

攻击模块在战斗每帧被大量遍历，扩展时请遵守：

1. **避免热路径反射 / 字符串拼接查找战斗逻辑**：需要 `GameFightLogic` 时调用 `FightHandler.Instance.manager.GetCachedFightLogic()`，懒加载且 `FightManager.Clear()` 会自动失效，**禁止**直接 `GameHandler.Instance.manager.GetGameLogic<GameFightLogic>()`。
2. **避免每帧 new List/HashSet**：连锁/穿透等类型需要复用候选缓冲（参考 `AttackModeFalluponChain.listCandidate` / `AttackModeRangedPiercing.listPierceCreature`），并在 `Destroy` 中 `Clear()` 防止对象池复用残留。
3. **缓存搜索类型**：`StartAttack(attacker,...)` 已根据 `attackedLayerTarget` 缓存 `searchCreatureType`，子类的范围/射线检测应复用该字段，不要在 `Update()` 里重新推导。
4. **遍历 `dlAttackModePrefab` 时缓存 `count`**：`UpdateHandleForAttackModePrefab` 在循环开始处一次性缓存 `List.Count`，确保本帧内 `Update` 新生成的攻击模块在下一帧才参与遍历（避免一帧多次 Update / 死循环）。
5. **射线检测走批处理，勿在 `Update()` 里 live Physics 射线**：走 `Ray/RaySelf` 的弹道命中检测统一由 `FightRaycastBatch`（`RaycastCommand` 批量并行）完成，见下方「射线检测批处理」。新增射线类弹道时重写 `PrepareRaycast(batch)` 入队射线，`Update()` 里照常调用 `CheckHitTargetForSingle/CheckHitTarget` 即会读批处理结果，**不要**自己调 `Physics.Raycast*`。

## 射线检测批处理（RaycastCommand 两段式）

> 背景：旧实现是「每个弹道在自己 `Update()` 里各自 `Physics.RaycastAll`」——主线程串行 + 每帧每球一个 `RaycastHit[]` 分配（GC）。100+ 火球时是主要 CPU/GC 瓶颈。现改为 `RaycastCommand` 批量并行、同帧 Schedule+Complete（**命中零延迟**），`NativeArray` 常驻复用（`Allocator.Persistent`）。

`UpdateHandleForAttackModePrefab` 改为**同帧两段式**（不是每球即时射线）：

1. **收集阶段**：遍历所有 `isValid` 弹道，调用 `PrepareRaycast(batch)`——走射线的子类把本帧射线（起点/方向/距离/层掩码）入队 `manager.raycastBatch`，记录命令索引 `batchRayStart`（Split 记 `listBatchRayIndex` 逐子弹）。非射线弹道 no-op。
2. **调度阶段**：`raycastBatch.Schedule()` → `RaycastCommand.ScheduleBatch(...).Complete()` 同帧跑完全部射线。
3. **消费阶段**：再遍历调用各自 `Update()`；`CheckHitTargetForSingle/CheckHitTarget` 检测到 `batchRayStart>=0` 时**直接读批处理结果**（`ResolveFirstAliveFromBatch`/`ResolveAllAliveFromBatch`，命中窗口内跳过已死目标），否则回落 live `FindCreatureEntity`（非射线类型 Area/Dis，或未入队的兜底）。

各子类 `PrepareRaycast` 约定：

| 类 | 入队策略 |
|----|---------|
| `AttackModeRanged`（Piercing/Area 继承） | 当前位置沿 `attackDirection` 入队 1 条（Piercing 靠 `CheckHitTarget` 读多命中窗口） |
| `AttackModeRangedTracking` | 先按当前位置实时算朝向目标的方向，再入队 1 条 |
| `AttackModeRangedArc` | `progress<0.5` 前半程不检测→不入队 |
| `AttackModeFallupon` | 下落中在当前位置入队 1 条 |
| `AttackModeRangedSplit` | 每个活跃子弹各入队 1 条，命中改为「先判定后移动」 |

约束：
- **检测时机统一为「移动前位置」**：两段式在移动前一次性收集，Split 由原「移动后检测」改为「移动前检测」（0.1 射程下位移差可忽略）。
- **命中窗口上限** `FightRaycastBatch.MaxHitsPerRay`（当前 4）：单条射线最多记录的命中数；弹道射线极短（`collider_size~0.1`），穿透 `numPierceMax` 亦够用。若将来需要单射线穿透 >4，需上调此常量。
- **层掩码** 由 `GetSearchLayerMask()` 从 `StartAttack` 缓存的 `searchCreatureType` 推导；无攻击者的 `StartAttack()` 数据路径 `searchCreatureType=None`→不入队→与 live 路径同样不命中（无回归）。
- **生命周期**：`raycastBatch` 挂在 `FightManager`，`Clear()` 时 `Dispose()` 释放 `NativeArray`，下场战斗首次入队按需重新分配。

## 弹道批量渲染（DSP 式 GPU Instancing）

> 背景：旧实现每发弹道 = 一个 GameObject 挂 VisualEffect(GPU 粒子)/SpriteRenderer，同屏 N 发 = N 份「每实例 CPU graph 求值 + GPU dispatch」固定开销。借鉴戴森球计划「只记录位置，一起绘制」——弹道位置抽为纯数据(`BaseAttackMode.position`)，渲染由一个渲染器每帧统一批量 `DrawMeshInstanced`。

`UpdateHandleForAttackModePrefab` 逻辑三阶段之后追加 **阶段4**：`manager.attackModeInstanceRenderer.RenderAll(listAttackMode)`。

- **[AttackModeInstanceRenderer](Assets/Scripts/Game/Fight/AttackMode/AttackModeInstanceRenderer.cs)**（纯 C# 类，非 MonoBehaviour，不持 GameObject）：`FightManager.attackModeInstanceRenderer` 持有。
- **分桶 key = `attackModeInfo.visual_name`（新配置字段）**：`RegisterVisual(visualName, mesh, material)` 登记视觉桶（mesh=朝相机 Quad，material 须开 GPU Instancing）；每桶固定 `Matrix4x4[1023]` 复用缓冲，满批即绘 + 收尾绘剩余，**无热路径分配**；用 `position` 构建 `TRS`（旋转暂用单位、缩放 `uniformScale`，billboard 交 shader）。
- **`visual_name`(DSP 批量) 与 `prefab_name`(原预制) 是独立两套渲染通道**：`prefab_name` 仍由 `FightManager.GetAttackModePrefab` Instantiate 原 prefab(SpriteRenderer/VisualEffect)渲染，逻辑不变；配置侧二选一，勿同行都填。
- **常开(已去除 `enableRender` 总开关)但天然零副作用**：`visual_name` 为空、或未登记该桶的弹道被跳过(不画)——现有弹道 visual_name 均空，行为不变。
- **`visual_name` 配置落地**：加在 `excel_attackmode_info[攻击方式].xlsx` 的 `AttackModeInfo` 表(prefab_name 之后)，**必须在 Unity 跑 ExcelEditorWindow「生成 Entity + 导出」重新生成 `AttackModeInfoBean.cs` + `AttackModeInfo.json`** 后 `attackModeInfo.visual_name` 才编译可用。
- **视觉资源与懒注册**：视觉源是一个预制 `Assets/LoadResources/AttackModeVisual/<visual_name>.prefab`(`PathInfo.AttackModeVisualPath`)，挂 `MeshFilter`(Quad)+`MeshRenderer`(material 开 GPU Instancing)，注册进 Addressables(address=全路径)。`FightManager.EnsureAttackModeVisual(attackModeInfo)` 在 `GetAttackModePrefab` 里懒加载：未 `HasVisual` 时 `GetModelForAddressablesSync(dicAttackModeVisualObj,...)` 取 **sharedMesh/sharedMaterial** 注册，只加载一次。
- **缓存跨关卡保留、整场结束才释放**：关卡间(`ClearGameForSimple`/`ClearAttackModePrefab`)不释放 `dicAttackModeVisualObj`/`dicAttackModeObj`(留给下关复用)；打完所有关卡(`ClearGame`→`FightManager.Clear`)调 `ClearAttackModeAssetCache()`：`LoadAddressablesUtil.Release` 释放弹道+视觉预制的 Addressables 句柄、清空两个 dict、`ClearVisuals()` 清桶。`UnregisterVisual` 仅热替换用。
- **前置约束（方案B）**：弹道位置真实源已从 `transform` 迁到 `BaseAttackMode.position`；移动型子类用 `SetPosition`/`TranslatePosition`（同步回 transform）。**新增移动弹道必须遵守，否则渲染器读到的 `position` 不准**。
- **已知局限/待办**：拖尾(Trail)未做；朝相机 billboard 待 shader(Editor 里看到的是未朝相机的静态 Quad)；`AttackModeRangedSplit` 自管多 GameObject 未迁移、不纳入；示例 visual 预制与 instanced 火焰 shader 资源尚未建（C# 骨架就位，建好并登记 Addressables 后即生效）。

## 文件位置速查

| 功能 | 文件路径 |
|------|----------|
| 攻击模式基类 | `Assets/Scripts/Game/Fight/AttackMode/BaseAttackMode.cs` |
| 射线批处理调度器 | `Assets/Scripts/Game/Fight/AttackMode/FightRaycastBatch.cs` |
| 弹道批量渲染器(DSP) | `Assets/Scripts/Game/Fight/AttackMode/AttackModeInstanceRenderer.cs` |
| 攻击模式数据Bean | `Assets/Scripts/Bean/Game/AttackModeBean.cs` |
| 攻击模式配置Bean | `Assets/Scripts/Bean/MVC/Game/AttackModeInfoBean.cs` |
| 攻击模式配置扩展 | `Assets/Scripts/Bean/MVC/Game/AttackModeInfoBeanPartial.cs` |
| 战斗处理器 | `Assets/Scripts/Component/Handler/FightHandler.cs` |
| 战斗管理器 | `Assets/Scripts/Component/Manager/FightManager.cs` |
| 近战单体 | `Assets/Scripts/Game/Fight/AttackMode/AttackModeMelee.cs` |
| 近战范围 | `Assets/Scripts/Game/Fight/AttackMode/AttackModeMeleeArea.cs` |
| 远程弹道 | `Assets/Scripts/Game/Fight/AttackMode/AttackModeRanged.cs` |
| 远程范围 | `Assets/Scripts/Game/Fight/AttackMode/AttackModeRangedArea.cs` |
| 抛物线弹道 | `Assets/Scripts/Game/Fight/AttackMode/AttackModeRangedArc.cs` |
| 抛物线范围 | `Assets/Scripts/Game/Fight/AttackMode/AttackModeRangedArcArea.cs` |
| 追踪弹道 | `Assets/Scripts/Game/Fight/AttackMode/AttackModeRangedTracking.cs` |
| 穿透弹道 | `Assets/Scripts/Game/Fight/AttackMode/AttackModeRangedPiercing.cs` |
| 分裂弹道 | `Assets/Scripts/Game/Fight/AttackMode/AttackModeRangedSplit.cs` |
| 爆炸 | `Assets/Scripts/Game/Fight/AttackMode/AttackModeExplosion.cs` |
| 天降单体 | `Assets/Scripts/Game/Fight/AttackMode/AttackModeFallupon.cs` |
| 天降范围 | `Assets/Scripts/Game/Fight/AttackMode/AttackModeFalluponArea.cs` |
| 天降连锁 | `Assets/Scripts/Game/Fight/AttackMode/AttackModeFalluponChain.cs` |
| 重叠检测 | `Assets/Scripts/Game/Fight/AttackMode/AttackModeOverlap.cs` |
| 回复基类 | `Assets/Scripts/Game/Fight/AttackMode/AttackModeRegain.cs` |
| 回复生命 | `Assets/Scripts/Game/Fight/AttackMode/AttackModeRegainHP.cs` |
| 回复护甲 | `Assets/Scripts/Game/Fight/AttackMode/AttackModeRegainDR.cs` |
| 攻击预制体路径 | `Assets/LoadResources/AttackMode/` |
| 攻击模块视觉预制(DSP mesh+material) | `Assets/LoadResources/AttackModeVisual/` |
