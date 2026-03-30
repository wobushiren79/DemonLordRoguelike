---
name: buff-system
description: Demon Lord Roguelike 游戏的BUFF系统开发指南。使用此SKILL当需要创建或修改BUFF效果、BUFF触发逻辑、BUFF配置等，包括属性BUFF、条件触发BUFF、周期性BUFF、即时BUFF、前置条件等。
---

# BUFF系统开发指南

## 核心概念

### BUFF数据结构

```
BuffBean          - BUFF基础数据（包含触发值、触发几率、触发次数等）
BuffEntityBean    - BUFF运行时实例数据（包含目标生物、施加者、剩余触发次数等）
BuffInfoBean      - BUFF配置数据（来自配置表）
```

### BUFF类型体系

```
BuffBaseEntity                    - BUFF基类
├── BuffEntityAttribute           - 属性修改BUFF（HP/DR/ATK等）
├── BuffEntityInstant             - 即时触发BUFF（初始化时触发一次）
├── BuffEntityConditional         - 条件触发BUFF（满足条件时触发）
│   ├── BuffEntityConditionalAttack       - 攻击时触发
│   ├── BuffEntityConditionalDead         - 死亡时触发
│   ├── BuffEntityConditionalAttribute    - 属性变化时触发
│   └── ...
├── BuffEntityPeriodic            - 周期性触发BUFF（无次数限制）
└── BuffEntityPecurrent           - 周期性触发BUFF（有次数限制）

BuffBasePreEntity                 - BUFF前置条件基类
├── BuffPreEntityForAttackDamage        - 累计造成伤害
├── BuffPreEntityForUnderAttackDamage   - 累计受到伤害
├── BuffPreEntityForHPRateLess          - 血量低于百分比
└── BuffPreEntityForKillNum             - 击杀数量
```

## 创建新BUFF

### 1. 定义BUFF ID

```csharp
// 在 BuffInfoBean 配置表中添加
// buff_type: 1攻击模块 2生物自带 3深渊馈赠 11/12/13生物稀有度BUFF
```

### 2. 选择BUFF实体类型

根据触发时机选择合适的基类：

| 实体类型 | 触发时机 | 适用场景 |
|---------|---------|---------|
| `BuffEntityAttribute` | 持续生效 | 属性加成（HP/DR/ATK等） |
| `BuffEntityInstant` | 添加时立即触发一次 | 一次性效果（复活、克隆等） |
| `BuffEntityConditional` | 特定事件触发 | 攻击/死亡/受击等条件触发 |
| `BuffEntityPeriodic` | 周期性触发 | 持续回复、持续伤害 |
| `BuffEntityPecurrent` | 周期性触发（有限次数） | 固定次数的效果触发 |

### 3. 创建BUFF实体类

#### 属性BUFF示例

```csharp
// Assets/Scripts/Game/Buff/BuffEntity/Attribute/BuffEntityAttribute.cs
public class BuffEntityAttribute : BuffBaseEntity
{
    public CreatureAttributeTypeEnum attributeType = CreatureAttributeTypeEnum.None;

    public override void SetData(BuffEntityBean buffEntityData)
    {
        base.SetData(buffEntityData);
        var buffInfo = buffEntityData.GetBuffInfo();
        attributeType = buffInfo.class_entity_data.GetEnum<CreatureAttributeTypeEnum>();
    }

    /// <summary>
    /// 修改属性值
    /// </summary>
    public virtual float ChangeData(CreatureAttributeTypeEnum targetAttributeType, float targetData)
    {
        if (targetAttributeType != attributeType)
            return targetData;
        
        // 数值加成逻辑
        targetData += buffEntityData.buffData.trigger_value;
        targetData *= 1 + buffEntityData.buffData.trigger_value_rate;
        return targetData;
    }
}
```

#### 条件触发BUFF示例

```csharp
// Assets/Scripts/Game/Buff/BuffEntity/Conditional/xxx.cs
public class BuffEntityConditionalAttack : BuffEntityConditional
{
    public override void SetData(BuffEntityBean buffEntityData)
    {
        base.SetData(buffEntityData);
        // 注册攻击事件监听
        nameRegisterEvent = EventsInfo.GameFightLogic_CreatureAttack;
        RegisterEvent(nameRegisterEvent);
    }

    public override void EventForCreatureAttack(FightAttackBean fightAttack)
    {
        if (buffEntityData.isValid == false) return;
        
        // 检查是否满足前置条件
        if (!CheckIsPre(buffEntityData))
            return;
        
        // 执行BUFF效果
        TriggerBuffConditional(buffEntityData);
    }
}
```

#### 即时触发BUFF示例

```csharp
// Assets/Scripts/Game/Buff/BuffEntity/Instant/BuffEntityInstantCloneDefenseCreature.cs
public class BuffEntityInstantCloneDefenseCreature : BuffEntityInstant
{
    public override bool TriggerBuffInstant(BuffEntityBean buffEntityData)
    {
        base.TriggerBuffInstant(buffEntityData);
        
        // 立即执行克隆逻辑
        var targetCreature = GetFightCreatureEntityForTarget();
        if (targetCreature != null)
        {
            // 克隆防御生物...
        }
        return true;
    }
}
```

### 4. 配置BUFF参数

```csharp
// BuffInfoBean 关键配置字段
{
    "id": 100001,                    // BUFF唯一ID
    "buff_type": 1,                  // BUFF类型
    "class_entity": "BuffEntityConditionalAttack",  // 实体类名
    "class_entity_events": "GameFightLogic_CreatureAttack",  // 监听事件
    "class_entity_data": "ATK",      // 实体数据（如属性类型）
    "pre_info": "1001:500|2001:3",   // 前置条件（条件ID:数值）
    "trigger_value": 100,            // 触发数值
    "trigger_value_rate": 0.5,       // 触发数值百分比
    "trigger_chance": 1.0,           // 触发几率（0-1）
    "trigger_num": 5,                // 触发次数（0为无限）
    "trigger_time": 1.0,             // 触发间隔（秒）
    "trigger_effect": 1001           // 触发特效ID
}
```

### 5. 添加前置条件（可选）

```csharp
// Assets/Scripts/Game/Buff/BuffPre/BuffPreEntityForXXX.cs
public class BuffPreEntityForCustomCondition : BuffBasePreEntity
{
    public override bool CheckIsPre(BuffEntityBean buffEntityData, float preValue)
    {
        FightCreatureEntity creatureEntity = GetTargetCreatureEntity(buffEntityData.targetCreatureUUId);
        if (creatureEntity == null)
            return false;
        
        // 自定义条件判断逻辑
        return buffEntityData.conditionalValue >= preValue;
    }
}
```

## 使用BUFF系统

### 添加BUFF到生物

```csharp
// 创建BUFF数据
BuffBean buffData = new BuffBean(buffId, isRandom: true, createRate: 1f);

// 添加到战斗生物
BuffHandler.Instance.AddFightCreatureBuff(
    new List<BuffBean>() { buffData },
    applierCreatureId,    // 施加者ID
    targetCreatureId      // 目标生物ID
);
```

### 添加深渊馈赠BUFF

```csharp
// 深渊馈赠是全局BUFF，作用于防御核心
AbyssalBlessingEntityBean abyssalBlessing = new AbyssalBlessingEntityBean(abyssalBlessingId);
BuffHandler.Instance.AddAbyssalBlessing(abyssalBlessing);
```

### 移除BUFF

```csharp
// 移除生物的所有BUFF
BuffHandler.Instance.RemoveFightCreatureBuffs(creatureId);

// 移除特定类型的BUFF
BuffHandler.Instance.RemoveFightCreatureBuffs<BuffEntityAttribute>(creatureId);
```

### 检查BUFF创建概率

```csharp
// 检查BUFF是否满足创建概率
BuffEntityBean buffEntity = BuffHandler.Instance.CheckBuffCreate(
    buffData, 
    applierCreatureId, 
    targetCreatureId
);
```

## BUFF事件类型

```csharp
// 可用的事件名称（class_entity_events 字段）
EventsInfo.GameFightLogic_UnderAttack_Dead      // 被攻击致死
EventsInfo.GameFightLogic_UnderAttack           // 被攻击
EventsInfo.GameFightLogic_CreatureDeadDropCrystal // 生物死亡掉落水晶
EventsInfo.GameFightLogic_CreatureDeadStart     // 生物死亡开始
EventsInfo.GameFightLogic_CreatureDeadEnd       // 生物死亡结束
```

## 属性类型枚举

```csharp
CreatureAttributeTypeEnum
├── None = 0
├── HP = 1           // 生命值
├── DR = 2           // 防御
├── ATK = 3          // 攻击
├── ASPD = 4         // 攻击速度
├── MSPD = 5         // 移动速度
├── CRT = 6          // 暴击率
├── EVA = 7          // 闪避率
├── RCD = 8          // 冷却缩减
├── HPRegeneration = 11   // HP回复
└── ...
```

## 常用代码模板

### 创建属性BUFF

```csharp
// 创建增加100点攻击力的BUFF
BuffBean buffData = new BuffBean(100001);
buffData.trigger_value = 100;
buffData.trigger_value_rate = 0;

BuffHandler.Instance.AddFightCreatureBuff(
    new List<BuffBean>() { buffData },
    applierId,
    targetId
);
```

### 创建带几率触发的BUFF

```csharp
// 创建有30%几率触发的BUFF
BuffBean buffData = new BuffBean(100002);
buffData.createRate = 0.3f;  // 30%创建几率
buffData.trigger_chance = 0.5f;  // 触发后50%执行效果
```

### 检查生物是否有特定BUFF

```csharp
List<BuffBaseEntity> buffs = BuffHandler.Instance.manager
    .GetFightCreatureBuffsActivie(creatureId);

bool hasBuff = buffs?.Any(b => b.buffEntityData.buffId == targetBuffId) ?? false;
```

### 获取BUFF属性加成

```csharp
// 在生物属性计算中使用
float baseATK = 100;
float buffBonus = BuffEntityAttribute.ChangeData(
    buffData, 
    CreatureAttributeTypeEnum.ATK, 
    baseATK
);
```

## 相关事件

```csharp
// BUFF变化事件
EventHandler.Instance.TriggerEvent(EventsInfo.Buff_FightCreatureChange, applierId, targetId);

// 深渊馈赠变化事件
EventHandler.Instance.TriggerEvent(EventsInfo.Buff_AbyssalBlessingChange, abyssalBlessingData);
```

## 文件位置速查

| 功能 | 文件路径 |
|------|----------|
| BUFF基类 | `Assets/Scripts/Game/Buff/BuffEntity/BuffBaseEntity.cs` |
| BUFF前置条件基类 | `Assets/Scripts/Game/Buff/BuffPre/BuffBasePreEntity.cs` |
| BUFF数据Bean | `Assets/Scripts/Bean/Game/BuffBean.cs` |
| BUFF实例Bean | `Assets/Scripts/Bean/Game/BuffEntityBean.cs` |
| BUFF配置Bean | `Assets/Scripts/Bean/MVC/Game/BuffInfoBean.cs` |
| BUFF前置条件配置 | `Assets/Scripts/Bean/MVC/Game/BuffPreInfoBean.cs` |
| BUFF管理器 | `Assets/Scripts/Component/Manager/BuffManager.cs` |
| BUFF处理器 | `Assets/Scripts/Component/Handler/BuffHandler.cs` |
| 属性BUFF | `Assets/Scripts/Game/Buff/BuffEntity/Attribute/` |
| 条件BUFF | `Assets/Scripts/Game/Buff/BuffEntity/Conditional/` |
| 即时BUFF | `Assets/Scripts/Game/Buff/BuffEntity/Instant/` |
| 周期性BUFF | `Assets/Scripts/Game/Buff/BuffEntity/Periodic/` |
| 前置条件 | `Assets/Scripts/Game/Buff/BuffPre/` |
