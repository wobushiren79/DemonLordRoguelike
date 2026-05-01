---
name: attack-mode-system
description: Demon Lord Roguelike 游戏的攻击模块(AttackMode)系统开发指南。使用此SKILL当需要创建或修改攻击模式、战斗弹道、攻击特效、攻击伤害逻辑等，包括近战/远程/范围/追踪/抛物线/爆炸/回复等攻击类型。
---

# 攻击模块系统开发指南

## 核心概念

### 攻击模块数据结构

```
AttackModeBean       - 攻击模块运行时数据（包含伤害、位置、方向、攻击者/目标ID等）
AttackModeInfoBean   - 攻击模块配置数据（来自配置表）
BaseAttackMode       - 攻击模块逻辑基类（包含碰撞检测、特效播放、生命周期管理等）
```

### 攻击模式类型体系

```
BaseAttackMode                    - 攻击模式基类
├── AttackModeMelee               - 近战单体（瞬间命中目标）
├── AttackModeMeleeArea           - 近战范围（起点范围伤害）
├── AttackModeRanged              - 远程直线弹道（逐帧移动+碰撞检测）
│   ├── AttackModeRangedArea      - 远程范围弹道（击中时范围AOE）
│   ├── AttackModeRangedArc       - 远程抛物线弹道
│   │   └── AttackModeRangedArcArea - 抛物线范围（继承抛物线轨迹）
│   ├── AttackModeRangedTracking  - 远程追踪弹道（实时改变方向追击目标）
│   ├── AttackModeRanged​Piercing  - 远程穿透弹道（可穿透多个目标）
│   └── AttackModeRanged​Split     - 远程分裂弹道（分裂为多条线路）
├── AttackModeExplosion           - 爆炸（以自身为中心范围伤害，攻击者死亡）
├── AttackModeFallupon            - 天降单体（直接对锁定目标造成伤害）
├── AttackModeFalluponArea        - 天降范围（对目标位置范围伤害）
├── AttackModeOverlap             - 重叠检测（范围伤害，无击中特效）
├── AttackModeLure                - 引诱（改变被攻击者线路）
└── AttackModeRegain              - 回复基类（不造成伤害，提供增益）
    ├── AttackModeRegainHP        - 回复生命
    └── AttackModeRegainDR        - 回复护甲
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
| `AttackModeExplosion` | 自爆范围伤害 | 自杀式爆炸、亡语 |
| `AttackModeFallupon` | 直接对目标造成伤害 | 天降打击、瞬移攻击 |
| `AttackModeFalluponArea` | 对目标位置范围伤害 | 陨石、天降AOE |
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
        gameObject.transform.Translate(attackModeData.attackDirection * Time.deltaTime * attackModeInfo.speed_move);
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
    "class_name": "AttackModeRanged", // 攻击模式类名
    "prefab_name": "ArrowPrefab",    // 预制体名称（空表示无预制体）
    "buff": "1001:0.5|1002:1.0",     // 攻击附带的BUFF（ID:创建概率）
    "attack_search_type": 0,         // 攻击搜索类型（0射线 11球形范围 21盒形范围）
    "collider_size": 0.5,            // 碰撞检测大小
    "collider_area_type": 11,        // 范围检测类型（11球形 21盒形）
    "collider_area_size": "2,2,2",   // 范围检测大小（半径或半-extents）
    "effect_hit": 1001,              // 击中特效ID
    "effect_damage": "1002",         // 受伤特效ID（默认不填，0关闭）
    "speed_move": 10,                // 移动速度（远程弹道用）
    "sound_miss": 1,                 // 未击中音效ID
    "sound_hit": 2,                  // 击中音效ID
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

### 创建攻击模块

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
// 1. 创建并初始化
attackMode.StartAttackInit(attackModeData);

// 2. 开始攻击（两个重载）
attackMode.StartAttack();  // 无目标攻击
attackMode.StartAttack(attacker, attacked, actionForAttackEnd);  // 生物对战

// 3. 每帧更新（仅远程/持续型攻击模式）
attackMode.Update();

// 4. 销毁（回收至对象池）
attackMode.Destroy();  // 回收
attackMode.Destroy(isPermanently: true);  // 永久销毁
```

## 文件位置速查

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
| 追踪弹道 | `Assets/Scripts/Game/Fight/AttackMode/AttackModeRangedTracking.cs` |
| 爆炸 | `Assets/Scripts/Game/Fight/AttackMode/AttackModeExplosion.cs` |
| 天降单体 | `Assets/Scripts/Game/Fight/AttackMode/AttackModeFallupon.cs` |
| 天降范围 | `Assets/Scripts/Game/Fight/AttackMode/AttackModeFalluponArea.cs` |
| 回复基类 | `Assets/Scripts/Game/Fight/AttackMode/AttackModeRegain.cs` |
| 攻击预制体路径 | `Assets/LoadResources/AttackMode/` |
