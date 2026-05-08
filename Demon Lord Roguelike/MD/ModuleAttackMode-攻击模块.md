# 攻击模块 (AttackMode Module) 分析文档

> 最后更新：2026 年 5 月 8 日

---

## 一、模块概述

攻击模块负责游戏中所有攻击行为的定义、创建、移动、碰撞检测、伤害结算和回收。采用**策略模式**实现，通过继承 `BaseAttackMode` 基类来实现不同攻击行为（近战、远程、范围、追踪、抛物线、爆炸、回复等）。

### 核心职责
- 攻击行为的创建与初始化
- 弹道移动与碰撞检测
- 伤害计算与受击回调
- 特效播放（击中特效、受伤特效）
- 攻击模块的对象池复用与回收

---

## 二、核心数据结构

### 2.1 AttackModeBean（攻击模块运行时数据）

**文件**: `Assets/Scripts/Bean/Game/AttackModeBean.cs`

攻击模块的运行时实例数据，每次攻击时从对象池获取并填充。

| 字段 | 类型 | 说明 |
|------|------|------|
| `attackModeId` | `long` | 攻击模式配置ID，对应 AttackModeInfoBean.id |
| `attackerDamage` | `int` | 攻击者攻击力（由生物属性 ATK 决定） |
| `attackerCRT` | `float` | 攻击者暴击概率（由生物属性 CRT 决定） |
| `startPos` | `Vector3` | 攻击起始位置 |
| `targetPos` | `Vector3` | 目标位置（被攻击者位置） |
| `attackDirection` | `Vector3` | 攻击方向（归一化向量） |
| `attackerId` | `string` | 攻击者生物UUID |
| `attackedId` | `string` | 被攻击者生物UUID（锁定目标，不一定击中） |
| `attackedLayerTarget` | `int` | 被攻击者所在层级（LayerInfo.CreatureAtt / CreatureDef） |
| `attackerCreatureId` | `long` | 攻击者生物配置ID（用于初始化攻击样式） |
| `attackerWeaponItemId` | `long` | 攻击者武器道具ID（用于初始化攻击样式） |

**关键方法**:
- `InitData(long attackModeId)` — 初始化数据
- `ClearData()` — 清理数据（回收对象池时调用）

---

### 2.2 AttackModeInfoBean（攻击模块配置数据）

**文件**: `Assets/Scripts/Bean/MVC/Game/AttackModeInfoBean.cs` + `AttackModeInfoBeanPartial.cs`

JSON配置表数据，继承 `BaseBean`，由 `AttackModeInfoCfg` 管理。

| 字段 | 类型 | 说明 |
|------|------|------|
| `class_name` | `string` | 攻击模式类名（反射创建实例用） |
| `prefab_name` | `string` | 预制体名称（空表示无预制体） |
| `buff` | `string` | 攻击附带的BUFF（格式：`ID:概率|ID:概率`） |
| `attack_search_type` | `int` | 攻击搜索类型（0射线 11球形范围 21盒形范围等） |
| `collider_size` | `float` | 碰撞检测大小（点到点检测用） |
| `collider_area_type` | `int` | 范围检测类型（11球形 21盒形） |
| `collider_area_size` | `string` | 范围检测大小（半径或半extents，逗号分隔） |
| `effect_hit` | `long` | 击中特效ID |
| `effect_damage` | `string` | 受伤特效ID（默认不填，0为关闭） |
| `speed_move` | `float` | 移动速度（远程弹道用） |
| `sound_miss` | `int` | 未击中音效ID |
| `sound_hit` | `int` | 击中音效ID |
| `remark` | `string` | 备注 |

**Partial 扩展** (`AttackModeInfoBeanPartial.cs`):
- `GetListBuff()` — 解析 `buff` 字符串，返回 `List<BuffBean>`
- `GetColliderAreaSize()` — 解析 `collider_area_size` 字符串，返回 `float[]`
- `GetColliderAreaSerachType()` — 返回范围检测枚举 `CreatureSearchType`
- `GetCreatureSerachType()` — 返回攻击搜索枚举 `CreatureSearchType`

**配置管理器** `AttackModeInfoCfg`:
- 继承 `BaseCfg<long, AttackModeInfoBean>`，从 `"AttackModeInfo"` JSON 文件加载
- `GetItemData(long key)` — 按ID查询
- `GetAllData()` / `GetAllArrayData()` — 全量查询
- `InitTestData(string buffTestData)` — 初始化测试数据（批量设置BUFF）

---

## 三、攻击模式类型体系

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

---

## 四、核心类详解

### 4.1 BaseAttackMode（攻击模式基类）

**文件**: `Assets/Scripts/Game/Fight/AttackMode/BaseAttackMode.cs`

所有攻击模式的抽象基类，管理攻击生命周期、碰撞检测、特效播放。

| 字段/属性 | 类型 | 说明 |
|-----------|------|------|
| `isValid` | `bool` | 是否有效（用于标记是否已回收） |
| `gameObject` | `GameObject` | 攻击模块对应的GameObject（不一定有） |
| `spriteRenderer` | `SpriteRenderer` | 精灵渲染器（用于显示攻击样式） |
| `attackModeInfo` | `AttackModeInfoBean` | 攻击模块配置信息 |
| `attackModeData` | `AttackModeBean` | 攻击模块运行时数据 |

**核心方法**:

| 方法 | 说明 |
|------|------|
| `InitAttackModeShow()` | 初始化攻击样式（根据武器道具ID设置精灵图，无武器则显示?图标） |
| `StartAttackInit(AttackModeBean)` | 开始攻击初始化，设置数据和外观 |
| `StartAttackBase()` | 基础攻击开始（设置GameObject位置和激活状态） |
| `StartAttack()` | 无目标攻击（默认实现） |
| `StartAttack(FightCreatureEntity, FightCreatureEntity, Action<BaseAttackMode>)` | 生物对战攻击（计算伤害、方向、位置等） |
| `Update()` | 每帧更新（远程/持续型攻击模式重写） |
| `Destroy(bool isPermanently = false)` | 回收攻击模块（默认入对象池，true则永久销毁） |
| `PlayEffectForHit(Vector3)` | 播放击中特效 |
| `CheckIsMoveBound(GameObject)` | 检测是否超出边界（x: -5~15, y: -5~15） |
| `CheckHitTargetForSingle()` / `CheckHitTargetForSingle(Vector3)` | 检测是否击中单个目标 |
| `CheckHitTarget()` / `CheckHitTarget(Vector3)` | 检测是否击中多个目标 |
| `CheckHitTargetArea(Vector3, Action<FightCreatureEntity>)` | 范围伤害检测，对范围内敌人执行回调 |
| `GetHitTargetAreaCollider(Vector3)` | 获取打击区域内的Collider数组 |

---

### 4.2 各攻击模式实现

#### AttackModeMelee（近战单体）

**文件**: `Assets/Scripts/Game/Fight/AttackMode/AttackModeMelee.cs`

瞬间命中单个目标，立即回收。

```csharp
public override void StartAttack(FightCreatureEntity attacker, FightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
{
    base.StartAttack(attacker, attacked, actionForAttackEnd);
    if (attacker != null && attacked != null && !attacked.IsDead())
    {
        attacked.UnderAttack(this);
        PlayEffectForHit(attacker.creatureObj.transform.position);
    }
    Destroy();
    actionForAttackEnd?.Invoke(this);
}
```

#### AttackModeRanged（远程直线弹道）

**文件**: `Assets/Scripts/Game/Fight/AttackMode/AttackModeRanged.cs`

逐帧移动并检测碰撞，击中目标后销毁。

- `HandleForHitTarget(FightCreatureEntity)` — 击中处理（扣血+销毁）
- `HandleForMove()` — 直线移动
- `HandleForBound()` — 边界检测

#### AttackModeRangedArea（远程范围弹道）

**文件**: `Assets/Scripts/Game/Fight/AttackMode/AttackModeRangedArea.cs`

继承 `AttackModeRanged`，击中时触发范围AOE伤害。

#### AttackModeRangedArc（抛物线弹道）

**文件**: `Assets/Scripts/Game/Fight/AttackMode/AttackModeRangedArc.cs`

继承 `AttackModeRanged`，按抛物线路径飞行，前半段不检测碰撞。

- `arcHeight = 3f` — 抛物线高度
- `progress` — 飞行进度 0~1

#### AttackModeRangedTracking（追踪弹道）

**文件**: `Assets/Scripts/Game/Fight/AttackMode/AttackModeRangedTracking.cs`

继承 `AttackModeRanged`，实时更新方向追击目标。

#### AttackModeRangedPiercing（穿透弹道）

**文件**: `Assets/Scripts/Game/Fight/AttackMode/AttackModeRangedPiercing.cs`

继承 `AttackModeRanged`，可穿透多个目标（默认最多3个），不立即销毁。

- `numPierceMax = 3` — 最大穿透数
- `listPierceCreature` — 已穿透生物记录

#### AttackModeRangedSplit（分裂弹道）

**文件**: `Assets/Scripts/Game/Fight/AttackMode/AttackModeRangedSplit.cs`

继承 `BaseAttackMode`，分裂为多条线路向不同道路飞行。

- `splitNum = 2` — 分裂数量
- `listSplitAttackObj` — 分裂出的攻击物体列表
- `listSplitRoad` — 各分裂弹道对应的道路索引

#### AttackModeMeleeArea（近战范围）

**文件**: `Assets/Scripts/Game/Fight/AttackMode/AttackModeMeleeArea.cs`

在起点位置进行范围伤害检测。

#### AttackModeExplosion（爆炸）

**文件**: `Assets/Scripts/Game/Fight/AttackMode/AttackModeExplosion.cs`

以自身为中心范围伤害，攻击者死亡（自爆）。

#### AttackModeFallupon（天降单体）

**文件**: `Assets/Scripts/Game/Fight/AttackMode/AttackModeFallupon.cs`

直接对锁定目标造成伤害，无视距离。

#### AttackModeFalluponArea（天降范围）

**文件**: `Assets/Scripts/Game/Fight/AttackMode/AttackModeFalluponArea.cs`

对目标位置进行范围伤害（如陨石、天降AOE）。

#### AttackModeFalluponChain（天降连锁）

**文件**: `Assets/Scripts/Game/Fight/AttackMode/AttackModeFalluponChain.cs`

对初始目标造成伤害后，连锁弹射周围目标，伤害逐次减半。

- `chainNum = 3` — 连锁次数
- `timeForChainChange = 0.1f` — 连锁间隔
- 使用 `HashSet<string>` 记录已攻击生物避免重复

#### AttackModeOverlap（重叠检测）

**文件**: `Assets/Scripts/Game/Fight/AttackMode/AttackModeOverlap.cs`

范围伤害，无击中特效（与MeleeArea的区别）。

#### AttackModeRegain（回复基类）

**文件**: `Assets/Scripts/Game/Fight/AttackMode/AttackModeRegain.cs`

不造成伤害，提供增益效果。

- `AttackModeRegainHP` — 回复生命
- `AttackModeRegainDR` — 回复护甲

---

## 五、攻击模块生命周期

```
1. 创建数据
   FightManager.GetAttackModeData(attackModeId)  // 从对象池获取 AttackModeBean

2. 创建攻击模块实例
   FightManager.GetAttackModePrefab(attackModeId, callback)
   // 优先从 dicPoolAttackModeObj 对象池获取
   // 无缓存则反射创建 class_name 对应类
   // 如有 prefab_name，加载预制体并实例化

3. 初始化攻击
   attackMode.StartAttackInit(attackModeData)  // 设置数据、初始化外观
   attackMode.StartAttack()                    // 无目标攻击
   attackMode.StartAttack(attacker, attacked, callback)  // 生物对战

4. 每帧更新（仅远程/持续型）
   attackMode.Update()

5. 销毁回收
   attackMode.Destroy()                        // 入对象池
   attackMode.Destroy(isPermanently: true)     // 永久销毁
   // FightHandler.RemoveAttackMode -> FightManager.RemoveAttackModePrefab
```

---

## 六、使用示例

### 6.1 生物对战创建攻击

```csharp
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

### 6.2 直接创建攻击模块（无目标）

```csharp
AttackModeBean attackModeData = new AttackModeBean(100001);
attackModeData.startPos = startPosition;
attackModeData.attackDirection = direction;
FightHandler.Instance.StartCreateAttackMode(attackModeData);
```

### 6.3 获取攻击模式配置

```csharp
AttackModeInfoBean attackModeInfo = AttackModeInfoCfg.GetItemData(attackModeId);
string className = attackModeInfo.class_name;
float moveSpeed = attackModeInfo.speed_move;
List<BuffBean> buffList = attackModeInfo.GetListBuff();
```

### 6.4 实现自定义攻击模式

```csharp
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

---

## 七、搜索类型枚举

**文件**: `Assets/Scripts/Enums/CreatureSearchType.cs`

```csharp
public enum CreatureSearchType
{
    Ray = 0,              // 默认射线，从攻击者射向远处
    RaySelf = 1,          // 射线，从远处射向自己
    AreaSphere = 11,      // 球形范围
    AreaSphereFront = 14, // 球形范围前方
    AreaBox = 21,         // 盒形范围
    AreaBoxFront = 24,    // 盒形范围前方
    DisMinByAll = 30,     // 遍历距离最近的生物
    DisMinByRoad = 31,    // 遍历同一路线距离最近生物
    DisMaxByAll = 40      // 遍历距离最远的生物
}
```

---

## 八、文件位置速查

| 功能 | 文件路径 |
|------|----------|
| 攻击模式基类 | `Assets/Scripts/Game/Fight/AttackMode/BaseAttackMode.cs` |
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

---

## 九、注意事项

1. **对象池复用**: 攻击模块实例和数据Bean均使用对象池，创建时优先复用，销毁时回收入池。`InitData()` 和 `ClearData()` 必须能正确重置状态。

2. **反射创建**: `FightManager.GetAttackModePrefab()` 使用反射根据 `class_name` 创建实例，类名必须与配置表中的 `class_name` 字段完全匹配。

3. **预制体加载**: 如果 `prefab_name` 不为空，会通过 `GetModelForAddressablesSync` 从 `Assets/LoadResources/AttackMode/` 加载预制体。

4. **武器样式**: `InitAttackModeShow()` 会根据 `attackerWeaponItemId` 查找道具配置，解析 `attack_mode_data` 设置攻击样式的精灵图、旋转、位置等。

5. **边界检测**: 远程弹道默认边界为 x: -5~15, y: -5~15，超出后自动销毁。

6. **特效方向**: `PlayEffectForHit()` 会根据攻击方向自动设置特效朝向（Left/Right）。

---

*文档结束*
