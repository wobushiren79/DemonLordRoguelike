---
name: ai-system
description: Demon Lord Roguelike 游戏的AI系统开发指南。使用此SKILL当需要创建或修改AI实体、AI意图、生物行为逻辑等，包括进攻生物AI、防守生物AI、核心生物AI、状态机切换等。
watched_files:
  - Assets/FrameWork/Scripts/AI/AIBaseEntity.cs
  - Assets/FrameWork/Scripts/AI/AIBaseIntent.cs
  - Assets/FrameWork/Scripts/AI/AIBaseCommon.cs
  - Assets/FrameWork/Scripts/Component/Manager/AIManager.cs
  - Assets/FrameWork/Scripts/Component/Handler/AIHandler.cs
  - Assets/Scripts/Enums/AIIntentEnum.cs
  - Assets/Scripts/AI/Creature/AICreatureEntity.cs
  - Assets/Scripts/AI/Creature/AIIntentCreatureAttack.cs
  - Assets/Scripts/AI/Creature/FightAttackCreature/AIAttackCreatureEntity.cs
  - Assets/Scripts/AI/Creature/FightDefenseCreature/AIDefenseCreatureEntity.cs
  - Assets/Scripts/AI/Creature/FightDefenseCoreCreature/AIDefenseCoreCreatureEntity.cs
---

# AI系统开发指南

## 核心概念

AI模块采用**状态机模式**实现，通过意图（Intent）切换控制生物在不同状态下的行为逻辑。

### 核心架构

```
AIHandler (单例)
    │
    ▼
AIManager
    │  ┌────────────────────────────────────────────┐
    ├──┤  AI实例列表 (listAIEntity)                │
    │  │  - AIAttackCreatureEntity (进攻生物)      │
    │  │  - AIDefenseCreatureEntity (防守生物)     │
    │  │  - AIDefenseCoreCreatureEntity (核心生物) │
    │  └────────────────────────────────────────────┘
    │
    ▼
AIBaseEntity (AI实体基类)
    │  - dicIntentPool: Dictionary<AIIntentEnum, AIBaseIntent>
    │  - currentIntent: AIBaseIntent
    │  - currentIntentEnum: AIIntentEnum
    │
    ├── AICreatureEntity (生物AI基类)
    │       │
    │       ├── AIAttackCreatureEntity (进攻型)
    │       │       ├── AIIntentAttackCreatureIdle    (闲置)
    │       │       ├── AIIntentAttackCreatureMove    (移动)
    │       │       ├── AIIntentAttackCreatureAttack  (攻击)
    │       │       ├── AIIntentAttackCreatureDead    (死亡)
    │       │       └── AIIntentAttackCreatureLured   (被诱惑)
    │       │
    │       ├── AIDefenseCreatureEntity (防守型)
    │       │       ├── AIIntentDefenseCreatureIdle   (闲置)
    │       │       ├── AIIntentDefenseCreatureAttack (攻击)
    │       │       ├── AIIntentDefenseCreatureDefend (防守)
    │       │       └── AIIntentDefenseCreatureDead   (死亡)
    │       │
    │       └── AIDefenseCoreCreatureEntity (核心)
    │               ├── AIIntentDefenseCoreCreatureIdle (闲置)
    │               └── AIIntentDefenseCoreCreatureDead (死亡)
    │
    └── AIBaseIntent (意图基类)
            ├── IntentEntering()   // 进入意图
            ├── IntentUpdate()     // 每帧更新
            ├── IntentFixUpdate()  // 固定频率更新
            └── IntentLeaving()    // 离开意图
```

### 意图枚举

```csharp
public enum AIIntentEnum
{
    // 进攻生物
    AttackCreatureIdle,      // 闲置
    AttackCreatureMove,      // 移动
    AttackCreatureAttack,    // 攻击
    AttackCreatureDead,      // 死亡
    AttackCreatureLured,     // 被诱惑

    // 防守生物
    DefenseCreatureIdle,     // 闲置
    DefenseCreatureAttack,   // 攻击
    DefenseCreatureDead,     // 死亡
    DefenseCreatureDefend,   // 防守

    // 核心生物
    DefenseCoreCreatureIdle, // 闲置
    DefenseCoreCreatureDead  // 死亡
}
```

---

## 创建新AI实体

### 1. 继承合适的AI基类

| 实体类型 | 基类 | 适用场景 |
|---------|------|---------|
| `AICreatureEntity` | `AIBaseEntity` | 普通生物AI |
| `AIAttackCreatureEntity` | `AICreatureEntity` | 主动进攻型生物 |
| `AIDefenseCreatureEntity` | `AICreatureEntity` | 防守型生物 |
| `AIDefenseCoreCreatureEntity` | `AICreatureEntity` | 核心/魔王生物 |

### 2. 实现必要的方法

```csharp
// Assets/Scripts/AI/Creature/xxx/AIxxxEntity.cs
public class AICustomCreatureEntity : AICreatureEntity
{
    public void InitData(FightCreatureEntity selfCreatureEntity)
    {
        // 注册事件
        this.selfCreatureEntity = selfCreatureEntity;
    }

    public override void StartAIEntity()
    {
        // 默认进入闲置状态
        ChangeIntent(AIIntentEnum.AttackCreatureIdle);
    }

    public override void CloseAIEntity()
    {
        // 关闭时的清理
    }

    public override void ClearData()
    {
        base.ClearData();
        selfCreatureEntity = null;
        targetCreatureEntity = null;
    }

    public override void InitIntentEnum(List<AIIntentEnum> listIntentEnum)
    {
        listIntentEnum.Add(AIIntentEnum.AttackCreatureIdle);
        listIntentEnum.Add(AIIntentEnum.AttackCreatureAttack);
        listIntentEnum.Add(AIIntentEnum.AttackCreatureDead);
    }
}
```

### 3. 在生物创建处绑定AI

```csharp
public void CreateCreatureAndBindAI(FightCreatureEntity creature)
{
    var aiEntity = AIHandler.Instance.CreateAIEntity<AICustomCreatureEntity>(ai =>
    {
        ai.InitData(creature);
    });
}
```

---

## 创建新意图

### 1. 在AIIntentEnum中添加枚举值

```csharp
// Assets/Scripts/Enums/AIIntentEnum.cs
public enum AIIntentEnum
{
    // ... 已有枚举 ...
    CustomCreatureIdle,  // 新增的自定义意图
}
```

### 2. 创建意图类

**命名规范**: 意图类名必须以 `AIIntent` 开头，后接枚举名称。例如 `AIIntentEnum.CustomCreatureIdle` 对应类 `AIIntentCustomCreatureIdle`。

```csharp
// Assets/Scripts/AI/Creature/xxx/AIIntentCustomCreatureIdle.cs
public class AIIntentCustomCreatureIdle : AIBaseIntent
{
    public AICustomCreatureEntity selfAIEntity;

    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        selfAIEntity = aiEntity as AICustomCreatureEntity;

        // 搜索目标
        selfAIEntity.targetCreatureEntity = selfAIEntity.FindCreatureEntityForSinge(DirectionEnum.Left);

        // 播放待机动画
        selfAIEntity.selfCreatureEntity.SetFaceDirection(Direction2DEnum.Left);
        selfAIEntity.selfCreatureEntity.PlayAnim(SpineAnimationStateEnum.Idle, true);
    }

    public override void IntentUpdate(AIBaseEntity aiEntity)
    {
        // 发现目标后切换到其他状态
        if (selfAIEntity.targetCreatureEntity != null)
        {
            selfAIEntity.ChangeIntent(AIIntentEnum.AttackCreatureMove);
        }
    }

    public override void IntentLeaving(AIBaseEntity aiEntity)
    {
        // 离开时的清理
    }
}
```

### 3. 在AI实体中注册该意图

```csharp
public override void InitIntentEnum(List<AIIntentEnum> listIntentEnum)
{
    listIntentEnum.Add(AIIntentEnum.CustomCreatureIdle);
    // ... 其他意图
}
```

---

## 核心类详解

### AIBaseEntity (AI实体基类)

**文件**: `FrameWork/Scripts/AI/AIBaseEntity.cs`

| 字段/属性 | 类型 | 说明 |
|-----------|------|------|
| `listIntentEnum` | `List<AIIntentEnum>` | 该实体支持的所有意图枚举 |
| `currentIntent` | `AIBaseIntent` | 当前激活的意图实例 |
| `currentIntentEnum` | `AIIntentEnum` | 当前意图枚举值 |
| `dicIntentPool` | `Dictionary<AIIntentEnum, AIBaseIntent>` | 意图对象池 |

| 方法 | 说明 |
|------|------|
| `ChangeIntent(AIIntentEnum)` | 切换意图，触发离开/进入回调 |
| `AddIntent(AIBaseIntent)` | 向意图池添加意图 |
| `GetIntent<T>(AIIntentEnum)` | 从意图池获取指定类型的意图 |
| `InitIntentEntity()` | 反射创建所有意图实例并初始化 |
| `StartAIEntity()` | 启动AI (抽象方法) |
| `CloseAIEntity()` | 关闭AI (抽象方法) |
| `InitIntentEnum(List<AIIntentEnum>)` | 初始化意图枚举列表 (抽象方法) |

### AIBaseIntent (意图基类)

**文件**: `FrameWork/Scripts/AI/AIBaseIntent.cs`

| 方法 | 说明 |
|------|------|
| `InitData(AIIntentEnum, AIBaseEntity)` | 初始化意图数据 |
| `IntentEntering(AIBaseEntity)` | 进入该意图时调用 (一次) |
| `IntentUpdate(AIBaseEntity)` | 每帧调用，执行业务逻辑 |
| `IntentFixUpdate(AIBaseEntity)` | 固定频率调用 |
| `IntentLeaving(AIBaseEntity)` | 离开该意图时调用 (一次) |
| `ChangeIntent(AIIntentEnum)` | 切换到其他意图 |

### AICreatureEntity (生物AI基类)

**文件**: `Scripts/AI/Creature/AICreatureEntity.cs`

| 字段 | 类型 | 说明 |
|------|------|------|
| `selfCreatureEntity` | `FightCreatureEntity` | 该AI控制的生物实体 |
| `targetCreatureEntity` | `FightCreatureEntity` | 当前锁定的目标生物 |

| 方法 | 说明 |
|------|------|
| `FindCreatureEntityForSinge(DirectionEnum)` | 朝指定方向搜索单个目标 |
| `FindCreatureEntity(DirectionEnum)` | 朝指定方向搜索多个目标 |
| `FindCreatureEntityForSinge(Vector3)` | 朝指定向量方向搜索单个目标 |
| `FindCreatureEntity(Vector3)` | 朝指定向量方向搜索多个目标 |

### AIHandler (AI处理器)

**文件**: `FrameWork/Scripts/Component/Handler/AIHandler.cs`

| 方法 | 说明 |
|------|------|
| `CreateAIEntity<T>(Action<T>)` | 创建AI实例并启动 |
| `RemoveAIEntity<T>(T)` | 移除AI实例 |

---

## 常用代码模板

### 创建并启动AI

```csharp
var aiEntity = AIHandler.Instance.CreateAIEntity<AIAttackCreatureEntity>(ai =>
{
    ai.InitData(creature);
});
```

### 移除AI

```csharp
AIHandler.Instance.RemoveAIEntity(aiEntity);
```

### 切换意图

```csharp
// 在AI实体内部
ChangeIntent(AIIntentEnum.AttackCreatureMove);

// 在意图内部
ChangeIntent(AIIntentEnum.AttackCreatureIdle);
```

### 搜索目标

```csharp
// 向左搜索单个目标
var target = selfAIEntity.FindCreatureEntityForSinge(DirectionEnum.Left);

// 向右搜索多个目标
var targets = selfAIEntity.FindCreatureEntity(DirectionEnum.Right);

// 自定义方向搜索
var target = selfAIEntity.FindCreatureEntityForSinge(Vector3.left);
```

### 播放动画

```csharp
// 播放循环动画
selfAIEntity.selfCreatureEntity.PlayAnim(SpineAnimationStateEnum.Idle, true);

// 播放单次动画
selfAIEntity.selfCreatureEntity.PlayAnim(SpineAnimationStateEnum.Attack, false);

// 设置面向方向
selfAIEntity.selfCreatureEntity.SetFaceDirection(Direction2DEnum.Left);
```

### 攻击意图示例 (继承通用攻击)

```csharp
public class AIIntentCustomAttack : AIIntentCreatureAttack
{
    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        selfAIEntity = aiEntity as AICustomCreatureEntity;
        intentForIdle = AIIntentEnum.CustomCreatureIdle;
        intentForDead = AIIntentEnum.CustomCreatureDead;
        base.IntentEntering(aiEntity);
    }
}
```

---

## 注意事项

1. **意图命名规范**: 意图类名必须以 `AIIntent` 开头，后接枚举名称。例如 `AIIntentEnum.AttackCreatureIdle` 对应类 `AIIntentAttackCreatureIdle`。

2. **反射创建**: `AIBaseEntity.InitIntentEntity()` 使用反射自动创建意图实例，类名必须与枚举名匹配，否则创建失败。

3. **事件清理**: AI实体继承 `BaseEvent`，需要在 `ClearData()` 中调用 `UnRegisterAllEvent()` 避免内存泄漏。

4. **对象池复用**: AI实例被移除时会进入对象池，下次创建同类型AI时会复用，因此 `InitData()` 必须能正确重置状态。

---

## 文件位置速查

| 功能 | 文件路径 |
|------|----------|
| AI实体基类 | `Assets/FrameWork/Scripts/AI/AIBaseEntity.cs` |
| 意图基类 | `Assets/FrameWork/Scripts/AI/AIBaseIntent.cs` |
| AI通用工具 | `Assets/FrameWork/Scripts/AI/AIBaseCommon.cs` |
| AI管理器 | `Assets/FrameWork/Scripts/Component/Manager/AIManager.cs` |
| AI处理器 | `Assets/FrameWork/Scripts/Component/Handler/AIHandler.cs` |
| 意图枚举 | `Assets/Scripts/Enums/AIIntentEnum.cs` |
| 生物AI基类 | `Assets/Scripts/AI/Creature/AICreatureEntity.cs` |
| 通用攻击意图 | `Assets/Scripts/AI/Creature/AIIntentCreatureAttack.cs` |
| 进攻生物AI | `Assets/Scripts/AI/Creature/FightAttackCreature/AIAttackCreatureEntity.cs` |
| 防守生物AI | `Assets/Scripts/AI/Creature/FightDefenseCreature/AIDefenseCreatureEntity.cs` |
| 核心生物AI | `Assets/Scripts/AI/Creature/FightDefenseCoreCreature/AIDefenseCoreCreatureEntity.cs` |
