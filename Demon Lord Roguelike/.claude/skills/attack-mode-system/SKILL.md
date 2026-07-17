---
name: attack-mode-system
description: Demon Lord Roguelike 游戏的攻击模块(AttackMode)系统开发指南。使用此SKILL当需要创建或修改攻击模式、战斗弹道、攻击特效、攻击伤害逻辑等，包括近战/远程/范围/追踪/抛物线/爆炸/回复/连锁/穿透/分裂等攻击类型。
watched_files:
  - Assets/Scripts/Game/Fight/AttackMode/
  - Assets/Scripts/Game/Fight/AttackModeInstanceRenderer.cs
  - Assets/Scripts/Game/Fight/AttackModeInstanceRendererTrail.cs
  - Assets/Scripts/Game/Fight/FightRaycastBatch.cs
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

- **[AttackModeInstanceRenderer](Assets/Scripts/Game/Fight/AttackModeInstanceRenderer.cs)**（纯 C# 类，非 MonoBehaviour，不持 GameObject；⚠️已从 `Fight/AttackMode/` **上移到 `Fight/` 目录**，且拆成 **partial 两文件**：主文件=弹体桶/`RenderAll`/环境光，[AttackModeInstanceRendererTrail.cs](Assets/Scripts/Game/Fight/AttackModeInstanceRendererTrail.cs)=轨迹拖尾全部逻辑）：`FightManager.attackModeInstanceRenderer` 持有。
- **分桶 key = `attackModeInfo.visual_name`（新配置字段）**：`RegisterVisual(visualName, mesh, material)` 登记视觉桶（mesh=朝相机 Quad，material 须开 GPU Instancing）；每桶固定 `Matrix4x4[1023]` 复用缓冲，满批即绘 + 收尾绘剩余，**无热路径分配**；用 `position` 构建 `TRS`（旋转暂用单位、缩放取 `visualScale`（`<0`/未配置时取1、大小改由桶材质自身 `_VertexScale` 决定），billboard 交 shader）。已删除 `uniformScale` 统一缩放字段。
- **`visual_name`(DSP 批量) 与 `prefab_name`(原预制) 是独立两套渲染通道**：`prefab_name` 仍由 `FightManager.GetAttackModePrefab` Instantiate 原 prefab(SpriteRenderer/VisualEffect)渲染，逻辑不变；配置侧二选一，勿同行都填。
- **常开(已去除 `enableRender` 总开关)但天然零副作用**：`visual_name` 为空、或未登记该桶的弹道被跳过(不画)——现有弹道 visual_name 均空，行为不变。
- **`visual_name` 配置落地**：加在 `excel_attackmode_info[攻击方式].xlsx` 的 `AttackModeInfo` 表(prefab_name 之后)，**必须在 Unity 跑 ExcelEditorWindow「生成 Entity + 导出」重新生成 `AttackModeInfoBean.cs` + `AttackModeInfo.json`** 后 `attackModeInfo.visual_name` 才编译可用。
- **视觉资源与懒注册**：视觉源是一个预制 `Assets/LoadResources/AttackModeVisual/<visual_name>.prefab`(`PathInfo.AttackModeVisualPath`)，挂 `MeshFilter`(Quad)+`MeshRenderer`(material 开 GPU Instancing)，注册进 Addressables(address=全路径)。`FightManager.EnsureAttackModeVisual(attackModeInfo)` 在 `GetAttackModePrefab` 里懒加载：未 `HasVisual` 时 `GetModelForAddressablesSync(dicAttackModeVisualObj,...)` 取 **sharedMesh/sharedMaterial** 注册，只加载一次。
- **per-instance 视觉参数(逐弹差异)**：武器 `attack_mode_data` 逐弹设的缩放/起始角/自旋已迁成 `BaseAttackMode.visualScale/visualStartAngle/spinSpeed/spinAxis`，`RenderAll` 构建每发 TRS(缩放 + 起始角绕视图前向)。**`visualScale` 默认 `-1`=未配置**(武器 `attack_mode_data` 无 `StartSize` 时保持)：此时矩阵缩放取 1，弹体实际大小交由**桶共享材质自身的 `_VertexScale`**(见 `Shader_Mesh_Common_1` 的「变换>大小」参数，物体空间整体缩放，默认1)决定；`StartSize` 配了才 `visualScale>=0` 用武器配置值覆盖。想给某类弹道统一改大小、又不逐武器配 `StartSize`，直接调该 visual 材质的 `_VertexScale` 即可。**自旋不再进矩阵**：`RenderAll` 里 `ApplyBucketSpin` 把 `spinAxis×spinSpeed`(每轴 度/秒)直接写进**桶共享材质**的 shader 自转参数(`_RotateSpeed` + 开 `_ROTATE_TIME_ON`，spinSpeed=0 则关)，由 shader 按全局 `_Time` 自转(方向靠 spinAxis 符号，材质 `_RotateDirection` 保持正向)——故已删除 `spawnTime`。按桶缓存 `appliedRotateSpeed` 仅在值变化时 SetVector。⚠️材质整桶共享，同一 visual 被多把武器复用且自旋不同会互相覆盖(通常一 visual 对一武器外观，可接受)。**per-instance 相位错开**：材质自转全桶「同速同相」(所有实例任意时刻角度一样)，故给每发一个随机 `spinPhase`(发射时 `Random.Range(0,360)` 写入，见 `StartAttackBase`)，`RenderAll` 里当 `spinSpeed≠0` 时把它作为绕 `spinAxis` 的**静态角**叠进 TRS——与材质的时间自转合成即「同速不同相」，各转各的。要「不同速/不同方向」得改回整段自旋走矩阵(材质关 `_ROTATE_TIME_ON`)。⚠️别再让材质资产烤 `_RotateSpeed`+`_ROTATE_TIME_ON` 又走 DSP 矩阵累积——两套同速反向自旋会**互相抵消看似不转**(骨头 200001 曾踩此坑)。`HandleItemsInfoAttackModeData` **双写**(写字段供 DSP + 保留写 spriteRenderer/material 供现有渲染)；`InitAttackModeShow` 开头 `ResetVisualParams()` 还原默认(对象池复用不残留)。
- **子桶分桶(ShowSprite 换图 + 自旋差异化，方案B 已落地)**：GPU Instancing 整批共用一张贴图/一份材质，故「逐弹不同贴图」「逐弹不同自旋」不能在单桶内表达(⚠️旧限制：同 visual 多武器自旋不同会互相覆盖)。现按**视觉签名**细分子桶：`AttackModeInstanceRenderer.BuildVisualBucketKey(visual_name, ShowSprite名, spinAxis×spinSpeed)` 生成 key——无换图无自旋=默认桶(直接 `visual_name`、复用基础 sharedMaterial)；有覆盖项才拼子桶签名，每个不同(贴图,自旋)组合各占独立子桶、**克隆一份基材质**互不覆盖，桶内仍 instancing 合批。**缩放/起始角/相位仍是逐弹矩阵参数**(不进签名，单桶即可逐弹不同)，只有影响共享材质的贴图/自旋才需分桶。key 缓存到 `BaseAttackMode.visualBucketKey`，`RenderAll` 按它取桶(替代旧的直接读 `visual_name`)。签名/换图名字段：`visualBucketKey`/`visualSpriteName`(`ResetVisualParams` 清空)。注册流程：`FightManager.EnsureAttackModeVisual(BaseAttackMode)`(与配置版 `EnsureAttackModeVisual(AttackModeInfoBean)` 重载,非弹道专属)在 `InitAttackModeShow` 末尾调——默认签名走配置版基础桶；子桶克隆材质缓存于 `dicAttackModeVisualMat`(兼去重,整场结束 `ClearAttackModeAssetCache` 统一 `Destroy`)，**换图子桶异步**从图集(`IconHandler.GetIconSprite(Items,名)`)取 sprite→`DataUtility.GetOuterUV` 算图集内 UV 子区域→写克隆材质 `_BaseMap`+`_BaseMap_ST`(shader `Shader_Mesh_Common_1` 用 `TRANSFORM_TEX` 采样,**无需改 shader**)→**贴图就绪后才 `RegisterVisual`**(未就绪的弹道当帧被 `RenderAll` 跳过不画,数帧后即显),仅自旋子桶直接登记(自旋由 `ApplyBucketSpin` 写克隆材质)。⚠️**换图必带宽高比修正(且必须在 shader 对象空间做, 不能用矩阵)**：DSP 用固定 1×1 方 Quad，非方形 sprite 会被拉伸(症状"高被拉长")。修正=按 `sprite.rect` 宽高 contain 归一化(长边=1)写进克隆材质的 `_VertexScaleXY`(`Shader_Mesh_Common_1` 新增, 对象空间 XY 各轴缩放, 默认(1,1)不改)，shader 在**自旋之前的最内层**缩放顶点。⚠️**为什么不能像最初那样把宽高比塞进 `RenderAll` 的矩阵缩放**：自旋是 shader 按 `_Time` 转的(对象空间, 在实例矩阵之内层)，非均匀缩放叠在实例矩阵=作用在"正在旋转的四边形"外层→随角度忽宽忽扁**抖动**(骷髅骨头等 `VertexRotateSpeed:360` 自旋武器踩过)；放进 shader 最内层(自旋前)则转的是已比例正确的矩形, 自旋/不自旋都不失真。默认桶/无换图 `_VertexScaleXY=(1,1)` 不受影响。⚠️代价:每种不同(贴图,自旋)组合 +1 draw call(通常几种,可忽略);若自旋取值极离散致子桶膨胀,再考虑把 `_RotateSpeed` 提成 per-instance 实例属性。⚠️图集若开旋转/紧密打包,`GetOuterUV` 矩形 UV 映射会错位(本项目 Items 图集未开,可接受)。
- **缓存跨关卡保留、整场结束才释放**：关卡间(`ClearGameForSimple`/`ClearAttackModePrefab`)不释放 `dicAttackModeVisualObj`/`dicAttackModeObj`(留给下关复用)；打完所有关卡(`ClearGame`→`FightManager.Clear`)调 `ClearAttackModeAssetCache()`：`LoadAddressablesUtil.Release` 释放弹道+视觉预制的 Addressables 句柄、清空两个 dict、`ClearVisuals()` 清桶。`UnregisterVisual` 仅热替换用。
- **前置约束（方案B）**：弹道位置真实源已从 `transform` 迁到 `BaseAttackMode.position`；移动型子类用 `SetPosition`/`TranslatePosition`（同步回 transform）。**新增移动弹道必须遵守，否则渲染器读到的 `position` 不准**。
- **Lit 材质亮度对齐（平坦环境光补偿）**：`DrawMeshInstanced` 的 `SampleSH` **读不到全局环境探针**（本自定义 shader 只有 `#pragma multi_compile_instancing`、未启用逐实例 SH，故 `LightProbeUsage.CustomProvided` + `CopySHCoefficientArraysFrom` 那套 SH 注入**无效**、试过没用），导致开 `_LIT_ON` 的桶材质比"拖预制到场景"的 `MeshRenderer`(BlendProbes→环境探针) **偏暗一份环境光**。⚠️`DrawMeshInstanced` 同样拿不到逐物体附加光(点光/聚光)列表——但本项目战斗场景只有平行光+天光/环境色，差异纯是环境光，故可补齐。**确定性修法**(不依赖实例化 SH 内部机制)：`RenderAll` 开头 `RefreshAmbientSH()` 把 `RenderSettings.ambientProbe` 在 6 轴求值取平均得一份平坦 GI 颜色，`sharedMPB.SetVector("_InstancedFlatGI", ...)`(仅环境光变化时重求值)；`Shader_Mesh_Common_1` 新增 `[HideInInspector] _InstancedFlatGI`，Lit 分支 `litColor.rgb += col.rgb * _InstancedFlatGI.rgb` 补到 `SampleSH`(实例化≈0)之上。绘制走 `DrawBucket`→带 MPB 的 `DrawMeshInstanced` 完整重载 + `LightProbeUsage.Off`。普通渲染(预制/材质直用)不设此属性=0，**完全不受影响**；桶材质不开 Lit 时也无副作用。**⚠️`MaterialPropertyBlock` 必须运行时懒建（`RefreshAmbientSH` 里 `if(null)new`），禁止写成字段初始化器**——本类由 MonoBehaviour `FightManager` 构造期 new 出来，字段初始化器里 `new MaterialPropertyBlock()` 会触发 Unity `CreateImpl is not allowed ... from a MonoBehaviour constructor` 异常并连带 `FightManager` 组件创建失败。
- **拖尾（轨迹，已落地）**：⚠️命名——此"轨迹"指弹道拖尾（`TrailBucket`），与 framework-core 冲刺残影 `AfterimageGhost*` 是**两套无关系统**，勿混。效果 = **弹体贴图本身画在若干历史位置上、越老越透明**（类似冲刺/突进残影），不是连续三角带。**放弃了旧的方案B 三角带**（`AppendTrailStrip` 逐点切线×相机朝向展宽 billboard + 逐帧建动态 Mesh，CPU 重）；改为白嫖 DSP 的 GPU Instancing——轨迹就是弹体实例在历史点上多画几遍。配置表**单列 `trail_data`**（`&` 分隔项、`:` 分键值，如 `type:1&count:6&interval:0.05&startAlpha:0.5&endAlpha:0.05&color:1,1,1`；`count`>0 且 `interval`>0 才启用），`AttackModeInfoBeanPartial.GetTrailConfig()` 解析缓存为 `AttackModeTrailConfig`（字段 `type`渲染方式枚举 `AttackModeTrailType`(1=Instanced 默认/2=Vfx)/`count`轨迹段数/`interval`采样间隔秒/`startAlpha`最新档透明度/`endAlpha`最老档透明度/`color`染色rgb；未配 type 默认 Instanced、未配透明度默认 0.5/0.05）。**`type` 选渲染方式**：省略或 `type:1`=下述现有 Instanced 方案；`type:2`=下条 VFX 方案(骨架)。**注意 `type` 是 `trail_data` 字符串内的键，Excel 列本身没变，改配置无需重生成 Bean/JSON**。**依赖 `visual_name` 走 DSP**——轨迹材质 = `AttackModeInstanceRenderer.RegisterTrailFromVisual` **克隆弹体桶材质**（继承 `_BaseMap`/`_BaseMap_ST`/宽高比 `_VertexScaleXY`/缩放 `_VertexScale`）再 `SetupTrailMaterial` 翻成透明(`_Surface=1`,SrcAlpha/OneMinusSrcAlpha,ZWrite off)+无光(`_LIT_ON` off)+冻结自旋(`_ROTATE_TIME_ON` off)+关裁剪，**无需模板材质/独立 shader**。`BaseAttackMode` 环形历史缓冲（`trailPoints` 位置 + `trailSpinAngles` 自旋角，一一对应；`trailCount`/`trailHead`，`TrailMaxPoints=32`=轨迹段数上限）与 `EnableTrail`（懒分配两缓冲+清空+`trailSampleInterval=config.interval`）/`SampleTrail(now)`（按间隔 push `position`+自旋角 `spinSpeed×now`）/`GetTrailPoint(orderIndex)`+`GetTrailSpinAngle(orderIndex)`（0=最老→count-1=最新）；**已删 `trailConfig` 字段**（轨迹参数由渲染器在 `TrailBucket` 里缓存）。`RenderAll` 里对启用弹道 `SampleTrail` 并收集进对应 `TrailBucket.frameAttackModes`，收尾 `DrawTrailBuckets` 按**年龄档**绘制：档 k = 所有弹道第 k 个最新历史点(`order=trailCount-1-k`)，整档共享一个 alpha(`Lerp(startAlpha,endAlpha,k/(count-1))`)一次 `DrawMeshInstanced`(`ShadowCastingMode.Off`)；**由老到新绘制**(近处不透明档叠远处透明档上)。矩阵用 `BuildInstanceMatrix(am, pos, extraSpinAngle)`(弹体/轨迹共用)：**旋转弹道(如骷髅骨头)的轨迹复现旋转**——轨迹材质虽冻结时间自转(`SetupTrailMaterial` 关 `_ROTATE_TIME_ON`)，但每档把该采样点的 `GetTrailSpinAngle`(=`spinSpeed×采样时刻`,绕 `spinAxis`)作 `extraSpinAngle` 烤进矩阵；弹体本体传 0(时间自转交 shader)。采样时刻用 `Time.timeSinceLevelLoad`(与 shader `_Time.y` 同基准)使最新档与弹体角连续；⚠️与 shader 完全对齐仅单轴自旋(billboard 绕 Z)成立。**每桶每帧仅多 `count` 次 draw call，与弹道数无关**（对比旧三角带的逐点 O(弹道×点数) 顶点组装+重传 Mesh，CPU 省一个量级）。`FightManager.EnsureAttackModeVisual(BaseAttackMode)` 弹体桶注册后调 `RegisterTrailFromVisual` + 每发 `EnableTrail`（⚠️**拖尾桶只在这个按实际签名注册的重载里派生**，预热用的 `EnsureAttackModeVisual(attackModeInfo)` 重载**不派生拖尾**——它在 `GetAttackModePrefab` 里发射前调用，此时换图/自旋尚未解析，按基础 `visual_name` 注册的拖尾桶对换图弹道永远收不到采样点，方案2 下即场景里多一个常驻空跑的 `AttackModeTrailVfx_*` 实例，方案1 下白克隆一份轨迹材质。已修）；`ClearVisuals`/`UnregisterVisual` 销毁克隆轨迹材质(Mesh 是弹体桶共享 Mesh，不销毁)。生命周期随 `ResetVisualParams`（关）+`EnableTrail`（重开清空），对象池复用零残留。⚠️`trail_data` 改了**语义**(旧 `width/time/tile` → 新 `count/interval/startAlpha/endAlpha`)，改 Excel 后须在 Unity 跑「生成 Entity + 导出」重生成 `AttackModeInfoBean.cs`+JSON；⚠️旧三角带方案的 `FrameWork/URP/AttackTrail` shader 及 `PathInfo.AttackTrailMatPath` **已删除**（轨迹改克隆弹体材质）。
- **拖尾方案2（VFX，`type:2`，C# 绑定已实现；图 `VFX_Trail_1.vfx` 已建）**：`trail_data` 配 `type:2` 走此路径——**单个 GPU VFX 特效**，每帧把所有子弹位置**+逐弹染色**一次性经两条 `GraphicsBuffer` 上传给 VFX，在这些位置喷射**各自颜色**的轨迹粒子，**全部粒子合一 draw call、与子弹数量无关**（对比方案1 每桶每帧 `count` 次、多视觉种类时 `桶数×count` 线性增长）。缺图时 `type:2` 静默不显示（弹体本体与方案1 均不受影响、不报错刷屏）。
- **⚠️染色作用域：方案1 是桶级、方案2 是逐弹级（选型关键差异）**：轨迹桶按 `visualKey`(=`visual_name`+换图+自旋，**不含 color**)建、且注册去重「先到先得」，故**方案1 下同 `visual_name` 的多行攻击模式配了不同 `color` 时，只有首个注册者的 color 生效，其余被静默忽略**（`TrailBucket.baseColor` 是桶字段）。方案2 无此问题：颜色不进桶签名、不做桶级 uniform，而是**每帧逐弹上传 `ColorBuffer`**，图内与位置用**同一索引**采样，故同一个 VFX 实例内可并存任意多种颜色。**需要"同图不同色"就用 `type:2`**；方案1 想要不同色只能拆 `visual_name`（或把 color 并进 `BuildVisualBucketKey`，未做）。
- **⚠️`type:2` 的配置只需 `type` + `color`（其余参数已写死在 EffectHandler）**：`count`/`interval`/`startAlpha`/`endAlpha` 本就是**桶级**(注册时灌进 VFX 实例、同 visualKey 只注册一次，逐行填了也只有首个 `type:2` 行生效)，留在配置表里等于误导策划"可逐行调"，故已**从配置表移除**、统一收进 `EffectHandler` 的 `TrailVfxLifetime`(1s) / `TrailVfxSpawnInterval`(0.02s) / `TrailVfxStartAlpha`(0.5) / `TrailVfxEndAlpha`(0.05) / `TrailVfxParticleSize`(0.1，粒子尺寸；曾取弹体材质 `_VertexScale` 跟随弹体，现全局写死) 五个常量——**要调 VFX 拖尾表现就改这五个常量**(单发同时存活粒子数≈Lifetime/SpawnInterval≈50)。`type:2` 的行再填 count/interval/alpha **会被静默忽略**。
- **⚠️`enable` 判定随 type 而异**：方案1 = `count>0 && interval>0`；**方案2 = 配了 `type:2` 即启用**(不看 count/interval)。否则只写 `type:2&color:...` 会因 `enable=false` 被整条关掉——`AttackModeTrailConfig.Parse` 已按此实现。
- **同一 `visual_name` 下 `type:1` 与 `type:2` 可共存**：两种轨迹桶**分属两处、互不冲突**（方案1 在渲染器的 `dicTrailBucket`，方案2 在 `EffectManager.dicAttackModeTrailVfx`，key 同为 visualKey 亦无妨），`RenderAll` 按**每发弹道自己的 `trailMode`** 路由（Instanced 的进 `SampleTrail`+年龄档、Vfx 的报给 `EffectHandler.AddAttackModeTrailVfxPoint`），互不干扰。故同一 visual 可以一部分攻击模式走残影、另一部分走 VFX（如 200001 走 `type:1`、210001 走 `type:2`）。它们共用同一个弹体桶（同贴图/材质），仅拖尾表现不同。
  - **⚠️代码落点：VFX 逻辑全部归 EffectHandler，`AttackModeInstanceRenderer` 不碰粒子**（与血液/护盾同一分工：调用方只给语义数据，粒子的实例/参数/缓冲由 Effect 系统自管。改拖尾 VFX 一律去 EffectHandler，别在渲染器里加）：
    - **EffectHandler 侧**（[EffectHandler.cs](Assets/Scripts/Component/Handler/EffectHandler.cs) 的「攻击弹道拖尾粒子(方案2 VFX)」区）：VFX 属性名 ID、**表现常量 `TrailVfx*`**、`RegisterAttackModeTrailVfx(visualKey)`(⚠️**只收桶签名，不收 config 也不收桶材质**——表现参数含粒子尺寸已全部写死、贴图由 VFX 预制自带、逐弹 color 走每帧上传；去重+实例化+**就地灌一次性参数**)、`BeginAttackModeTrailVfxFrame()`、`AddAttackModeTrailVfxPoint(visualKey, position, trailColor)`(直接用该发 `trailColor` 原值成对 Add)、`FlushAttackModeTrailVfxFrame()`(**每帧参数就地设完**：两条 `SetData`+`SetGraphicsBuffer`+`PositionCount`(兼容 uint/int)；`EnsureAttackModeTrailVfxBuffer` 两 buffer 同容量同步按 2 的幂扩容)、`ClearAttackModeTrailVfx(key)`/`ClearAllAttackModeTrailVfx()`、`GetAttackModeTrailModel()`(私有，内部懒加载+门控)。
    - **状态**：[EffectManager.cs](Assets/Scripts/Component/Manager/EffectManager.cs) 的 `dicAttackModeTrailVfx`(key=visualKey)/`objAttackModeTrailModel`/`triedLoadAttackModeTrailModel`；单桶状态 Bean = [AttackModeTrailVfxBean.cs](Assets/Scripts/Bean/Game/AttackModeTrailVfxBean.cs)(`vfx`/`listPosition`/`listColor`/`positionBuffer`/`colorBuffer`/`bufferCapacity`)。
    - **渲染器侧**（仅 3 个调用点，无任何 VFX 类型）：`RegisterTrailFromVisual` 按 type=Vfx 转交 `EffectHandler.Instance.RegisterAttackModeTrailVfx(visualKey)`；`RenderAll` 开头 `BeginAttackModeTrailVfxFrame()`、逐弹 `AddAttackModeTrailVfxPoint(...)`、末尾 `FlushAttackModeTrailVfxFrame()`；`UnregisterVisual`/`ClearVisuals` 转交 `ClearAttackModeTrailVfx`/`ClearAllAttackModeTrailVfx`。
    - **枚举/逐弹字段**：`AttackModeTrailType`(`None=0`/`Instanced=1`/`Vfx=2`) + `AttackModeTrailConfig.type`；`BaseAttackMode.trailMode`(三态，`EnableTrail` 按 type 设，Vfx 不分配 CPU 环形缓冲) + **`trailColor`**(Vector3 rgb 逐弹染色，`EnableTrail` 从自身 `config.color` 设、`ResetVisualParams` 复位白)。
    - **加载链(与血液/护盾同源)**：`EffectManager.effectAttackModeTrailId`(=`1600001`) → 查 `EffectInfo` 得 res_name(`Effect_Trail_1`) → `EffectManager.GetEffectModelSync` 取 Effects 目录模型(缓存 `dicEffectModel`，**不实例化/不入池/不需要 `EffectBase`**) → EffectHandler 自行 Instantiate 每桶一份。配置行已在 `excel_effect_info[粒子信息].xlsx` + `EffectInfo.txt`(id 1600001,res_name Effect_Trail_1,show_type 1)。⚠️`FightManager` 侧的 `EnsureTrailVfxTemplate`/`triedLoadTrailVfx`/`SetTrailVfxTemplate` **已全部删除**(门控内移进 EffectHandler)。
  - **⚠️`RenderAll` 列表为空不可早退**：即便本帧一发子弹都没有，也必须走完 `BeginAttackModeTrailVfxFrame`→`FlushAttackModeTrailVfxFrame`——Flush 会把 `PositionCount` 归零，否则最后一批子弹死光后 VFX 会在它们的残留位置**持续喷粒子**(旧实现 `if (count == 0) return;` 曾有此 bug，已修)。故早退条件只剩 `dicBucket.Count == 0`。
  - **VFX Graph 暴露属性合同**（图内 Exposed Property 名必须一致，是 C# 与图的唯一耦合；⚠️**本项目粒子命名约定无下划线**，如血液的 `PositionStart`/`Direction`）：`PositionBuffer`(GraphicsBuffer StructuredBuffer&lt;float3&gt; 本帧子弹位置)、**`ColorBuffer`**(GraphicsBuffer StructuredBuffer&lt;float3&gt; 本帧**逐弹染色 rgb**，与 `PositionBuffer` **同索引配对**、同容量)、`PositionCount`(uint 或 int，有效数,驱动喷发)、`StartAlpha`/`EndAlpha`/`Lifetime`/`SpawnInterval`/`ParticleSize`(float，均取 EffectHandler 的 `TrailVfx*` 写死常量)。⚠️图内的 `MainTex`(粒子贴图)**已不由 C# 设置**——由 VFX 预制自带；拖尾是独立粒子美术，不再要求与弹体同图(故 `matTint` 继承弹体染色的链路也一并废除，配置表 color 所见即所得)。图内建议：System Simulation Space=**World**；Spawn 用 Periodic Burst(Count=`PositionCount`,Delay=`SpawnInterval`)；Initialize 用 **Sample Graphics Buffer**(Type=Vector3/stride12,Buffer=`PositionBuffer`,Index=`particleId % max(PositionCount,1)`——⚠️用内置属性 `particleId`,VFX 无 spawnIndex 属性) 设 position；**透明度不能用 `Set Alpha over Life`(只吃 Curve)**,改手动 `Set Alpha`(Overwrite) 接 `Lerp(StartAlpha,EndAlpha,saturate(age/lifetime))`；`Set Color`(Initialize) 接**第二个 Sample Graphics Buffer**(Type=Vector3,Buffer=`ColorBuffer`,Index=**复用位置那颗 Modulo 的输出**——必须同索引,否则子弹与颜色错配；alpha 交给 Set Alpha)；Output Quad 用 `MainTex`×**粒子 color 属性**朝相机(Face Camera Plane)+`Set Size`=`ParticleSize`；粒子不加速度(留在原地→弹体飞走成拖尾)；Bounds 设大/自动重算防剔除。
  - **⚠️`particleId % PositionCount` 为何配对永远正确**：`particleId` 全系统单调递增、不重置，故索引会随批次轮转（如上批 5 发用 0~4、下批从 5 开始）。但**同一批喷发内 `{startId..startId+N-1} mod N` 恰是 `0..N-1` 的一个排列**，且位置与颜色取**同一个 i**，故"第 i 发的位置必配第 i 发的颜色"恒成立——轮转只打乱"哪个粒子领哪发子弹"，不打乱配对本身。这正是"一个 VFX 装下多种颜色"的关键。同理**任何新增的逐弹 buffer 都必须复用同一颗 Modulo 的输出**。
  - **图/预制现状（逐弹染色已接完）**：`VFX_Trail_1.vfx`(guid `a632d682cdf5e494bb8d46b699353693`，由 `Effect_Trail_1.prefab` 引用) 已建 `PositionBuffer`+`ColorBuffer` 两条 buffer、两颗共用同一 `Modulo`(`particleId % PositionCount`) 索引的 `Sample Buffer`，与 position/lifetime/size/alpha/color 五个 `SetAttribute`。**`TrailColor` 属性已从图与 C# 双双删除**——染色一律逐弹走 `ColorBuffer`，不再保留"整桶一色"的 uniform 回退。⚠️GraphicsBuffer 类属性**不会**出现在预制的属性覆盖列表里(运行时绑定)，故预制只序列化标量/贴图属性属**正常现象**，不代表图缺 buffer 属性(旧文档曾据此误判"图缺 PositionBuffer")。
  - **性能权衡**：多视觉种类高并发场景 VFX 大幅省 draw call/CPU 带宽，但有 VFX compute 固定开销、低负载不划算、overdraw 与方案1 同级、轨迹语义偏"喷射"而非精确历史残影。
- **已知局限/待办**：朝相机 billboard（弹体本体）待 shader(Editor 里看到的是未朝相机的静态 Quad)；`AttackModeRangedSplit` 自管多 GameObject 未迁移、不纳入 DSP 也无拖尾；示例 visual 预制与 instanced 火焰 shader 资源尚未建（C# 骨架就位，建好并登记 Addressables 后即生效）；**拖尾方案2(VFX `type:2`) 已完整落地**（C# 绑定 + 加载走 EffectHandler(res_name `Effect_Trail_1`) + 图 `VFX_Trail_1.vfx` 位置定位与逐弹染色均已接完）；⚠️**`trail_data` 的 `color` 在方案1 下是桶级(同 `visual_name` 首个注册者赢)、方案2 下才逐弹生效**；⚠️**方案2 下 `count`/`interval`/`startAlpha`/`endAlpha` 仍是桶级**(注册时灌进 VFX 实例，同 `visual_name` 首个 `type:2` 行赢)——只有 `color` 逐弹，需要不同段数/间隔/透明度须拆 `visual_name`。

## 文件位置速查

| 功能 | 文件路径 |
|------|----------|
| 攻击模式基类 | `Assets/Scripts/Game/Fight/AttackMode/BaseAttackMode.cs` |
| 射线批处理调度器 | `Assets/Scripts/Game/Fight/FightRaycastBatch.cs`(已从 AttackMode/ 上移至 Fight/) |
| 弹道批量渲染器(DSP 主文件：弹体桶/RenderAll/环境光) | `Assets/Scripts/Game/Fight/AttackModeInstanceRenderer.cs`(已上移至 Fight/) |
| 弹道批量渲染器(方案1 轨迹拖尾 partial：TrailBucket/注册/绘制) | `Assets/Scripts/Game/Fight/AttackModeInstanceRendererTrail.cs` |
| **拖尾方案2(VFX)全部逻辑**(实例/参数/缓冲/上传；渲染器不碰) | `Assets/Scripts/Component/Handler/EffectHandler.cs`「攻击弹道拖尾粒子(方案2 VFX)」区 |
| 拖尾方案2(VFX)状态(dicAttackModeTrailVfx/模板缓存+门控) | `Assets/Scripts/Component/Manager/EffectManager.cs` |
| 拖尾方案2(VFX)单桶状态 Bean | `Assets/Scripts/Bean/Game/AttackModeTrailVfxBean.cs` |
| 拖尾方案2(VFX) VFX Graph 图 / 模板预制 | `Assets/FrameWork/Prefabs/Effect/VFX/VFX_Trail_1.vfx` / `Assets/LoadResources/Effects/Effect_Trail_1.prefab` |
| ~~拖尾 shader(旧三角带方案B)~~ 已删除 | ~~`Assets/FrameWork/Shader/URP/Shader_Attack_Trail.shader`~~(已删；轨迹改克隆弹体材质) |
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
