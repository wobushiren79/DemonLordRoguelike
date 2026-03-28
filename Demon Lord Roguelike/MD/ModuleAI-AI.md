# AI 模块分析文档

> 最后更新：2026 年 3 月 27 日

---

## 一、模块概述

AI 模块负责游戏中所有非玩家控制生物的行为决策和状态管理。采用**状态机模式**实现，通过意图（Intent）切换控制生物在不同状态下的行为逻辑。

### 核心职责
- 生物行为决策（闲置、移动、攻击、死亡等）
- 目标搜索与锁定
- 状态切换与管理
- 事件响应（如敌方生物死亡、新卡放置等）

---

## 二、核心架构

### 2.1 架构图

```
AIHandler (单例)
    │
    ▼
AIManager
    │  ┌──────────────────────────────────────────────────┐
    ├──┤  AI 实例列表 (listAIEntity)                     │
    │  │  - AIAttackCreatureEntity (进攻生物)            │
    │  │  - AIDefenseCreatureEntity (防守生物)           │
    │  │  - AIDefenseCoreCreatureEntity (核心生物)       │
    │  └──────────────────────────────────────────────────┘
    │
    ▼
AIBaseEntity (AI 实体基类)
    │  - dicIntentPool: Dictionary<AIIntentEnum, AIBaseIntent>
    │  - currentIntent: AIBaseIntent
    │  - currentIntentEnum: AIIntentEnum
    │
    ├── AICreatureEntity (生物 AI 基类)
    │       │
    │       ├── AIAttackCreatureEntity (进攻型生物)
    │       │       ├── AIIntentAttackCreatureIdle    (闲置)
    │       │       ├── AIIntentAttackCreatureMove    (移动)
    │       │       ├── AIIntentAttackCreatureAttack  (攻击)
    │       │       ├── AIIntentAttackCreatureDead    (死亡)
    │       │       └── AIIntentAttackCreatureLured   (被诱惑)
    │       │
    │       ├── AIDefenseCreatureEntity (防守型生物)
    │       │       ├── AIIntentDefenseCreatureIdle   (闲置)
    │       │       ├── AIIntentDefenseCreatureAttack (攻击)
    │       │       ├── AIIntentDefenseCreatureDefend (防守)
    │       │       └── AIIntentDefenseCreatureDead   (死亡)
    │       │
    │       └── AIDefenseCoreCreatureEntity (核心生物)
    │               ├── AIIntentDefenseCoreCreatureIdle (闲置)
    │               └── AIIntentDefenseCoreCreatureDead (死亡)
    │
    └── AIBaseIntent (意图基类)
            ├── IntentEntering()   // 进入意图
            ├── IntentUpdate()     // 每帧更新
            ├── IntentFixUpdate()  // 固定频率更新
            └── IntentLeaving()    // 离开意图
```

### 2.2 状态流转图

#### 进攻生物状态流转

```
                    ┌─────────────────────────────────────┐
                    │                                     ▼
            ┌──────────────┐    发现目标    ┌──────────────┐
            │ AttackCreature│ ─────────────►│ AttackCreature│
            │    Idle      │               │     Move     │
            └──────────────┘               └──────┬───────┘
                    ▲                             │
                    │                             │ 进入攻击范围
                    │    目标死亡/超出范围         ▼
                    │                      ┌──────────────┐
                    └──────────────────────│ AttackCreature│
                                           │    Attack    │
                                           └──────┬───────┘
                                                  │
                    ┌─────────────────────────────┘
                    │ 生物死亡
                    ▼
            ┌──────────────┐
            │ AttackCreature│
            │     Dead     │
            └──────────────┘
```

---

## 三、核心类详解

### 3.1 AIBaseEntity (AI 实体基类)

**文件**: `FrameWork/Scrpits/AI/AIBaseEntity.cs`

AI 实体的抽象基类，管理意图池和状态切换。

| 字段/属性 | 类型 | 说明 |
|-----------|------|------|
| `listIntentEnum` | `List<AIIntentEnum>` | 该实体支持的所有意图枚举 |
| `currentIntent` | `AIBaseIntent` | 当前激活的意图实例 |
| `currentIntentEnum` | `AIIntentEnum` | 当前意图枚举值 |
| `dicIntentPool` | `Dictionary<AIIntentEnum, AIBaseIntent>` | 意图对象池 |

**核心方法**:

| 方法 | 说明 |
|------|------|
| `InitData()` | 初始化数据，调用 `InitIntentEntity()` |
| `Update()` | 每帧调用当前意图的 `IntentUpdate()` |
| `FixedUpdate()` | 固定频率调用当前意图的 `IntentFixUpdate()` |
| `ChangeIntent(AIIntentEnum)` | 切换意图，触发离开/进入回调 |
| `AddIntent(AIBaseIntent)` | 向意图池添加意图 |
| `GetIntent<T>(AIIntentEnum)` | 从意图池获取指定类型的意图 |
| `InitIntentEntity()` | 反射创建所有意图实例并初始化 |
| `StartAIEntity()` | 启动 AI (抽象方法，子类实现) |
| `CloseAIEntity()` | 关闭 AI (抽象方法，子类实现) |
| `InitIntentEnum(List<AIIntentEnum>)` | 初始化意图枚举列表 (抽象方法) |

### 3.2 AIBaseIntent (意图基类)

**文件**: `FrameWork/Scrpits/AI/AIBaseIntent.cs`

定义单个状态的行为逻辑。

| 字段 | 类型 | 说明 |
|------|------|------|
| `aiEntity` | `AIBaseEntity` | 所属的 AI 实体 |
| `aiIntent` | `AIIntentEnum` | 意图类型枚举 |

**核心方法**:

| 方法 | 说明 |
|------|------|
| `InitData(AIIntentEnum, AIBaseEntity)` | 初始化意图数据 |
| `IntentEntering(AIBaseEntity)` | 进入该意图时调用 (一次) |
| `IntentUpdate(AIBaseEntity)` | 每帧调用，执行业务逻辑 |
| `IntentFixUpdate(AIBaseEntity)` | 固定频率调用，用于物理相关逻辑 |
| `IntentLeaving(AIBaseEntity)` | 离开该意图时调用 (一次) |
| `ChangeIntent(AIIntentEnum)` | 切换到其他意图 |

### 3.3 AICreatureEntity (生物 AI 基类)

**文件**: `Scrpits/AI/Creature/AICreatureEntity.cs`

生物类 AI 的基类，扩展了目标搜索功能。

| 字段 | 类型 | 说明 |
|------|------|------|
| `selfCreatureEntity` | `FightCreatureEntity` | 该 AI 控制的生物实体 |
| `targetCreatureEntity` | `FightCreatureEntity` | 当前锁定的目标生物 |

**核心方法**:

| 方法 | 说明 |
|------|------|
| `FindCreatureEntityForSinge(DirectionEnum)` | 朝指定方向搜索单个目标 |
| `FindCreatureEntity(DirectionEnum)` | 朝指定方向搜索多个目标 |
| `FindCreatureEntityForSinge(Vector3)` | 朝指定向量方向搜索单个目标 |
| `FindCreatureEntity(Vector3)` | 朝指定向量方向搜索多个目标 |

### 3.4 AIManager (AI 管理器)

**文件**: `FrameWork/Scrpits/Component/Manager/AIManager.cs`

负责 AI 实例的创建、缓存和销毁。

| 字段 | 类型 | 说明 |
|------|------|------|
| `listAIEntity` | `List<AIBaseEntity>` | 当前活跃的所有 AI 实体 |
| `poolAIEntity` | `Dictionary<string, Queue<AIBaseEntity>>` | AI 实体对象池，按类型缓存 |

**核心方法**:

| 方法 | 说明 |
|------|------|
| `CreateAIEntity<T>()` | 创建 AI 实例（优先从对象池获取） |
| `RemoveAIEntity<T>(T)` | 移除 AI 实例（移入对象池） |
| `ClearAIEntity<T>(T)` | 清理 AI 实例数据 |
| `Clear()` | 清理所有 AI 实例和对象池 |

### 3.5 AIHandler (AI 处理器)

**文件**: `FrameWork/Scrpits/Component/Handler/AIHandler.cs`

AI 模块的入口，驱动所有 AI 更新。

**核心方法**:

| 方法 | 说明 |
|------|------|
| `Update()` | 每帧调用所有 AI 实体的 `Update()` |
| `FixedUpdate()` | 固定频率调用所有 AI 实体的 `FixedUpdate()` |
| `CreateAIEntity<T>(Action<T>)` | 创建 AI 并启动 |
| `RemoveAIEntity<T>(T)` | 移除 AI 实例 |

---

## 四、意图枚举

**文件**: `Scrpits/Enums/AIIntentEnum.cs`

```csharp
public enum AIIntentEnum
{
    // 进攻生物意图
    AttackCreatureIdle,     // 闲置状态
    AttackCreatureMove,     // 移动状态
    AttackCreatureAttack,   // 攻击状态
    AttackCreatureDead,     // 死亡状态
    AttackCreatureLured,    // 被诱惑状态

    // 防守生物意图
    DefenseCreatureIdle,    // 闲置状态
    DefenseCreatureAttack,  // 攻击状态
    DefenseCreatureDead,    // 死亡状态
    DefenseCreatureDefend,  // 防守状态

    // 核心生物意图
    DefenseCoreCreatureIdle,// 闲置状态
    DefenseCoreCreatureDead // 死亡状态
}
```

---

## 五、具体意图实现示例

### 5.1 AIIntentAttackCreatureIdle (进攻生物闲置意图)

**文件**: `Scrpits/AI/Creature/FightAttackCreature/AIIntentAttackCreatureIdle.cs`

```csharp
public class AIIntentAttackCreatureIdle : AIBaseIntent
{
    public AIAttackCreatureEntity selfAIEntity;

    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        selfAIEntity = aiEntity as AIAttackCreatureEntity;
        
        // 1. 搜索路线上的敌人
        selfAIEntity.targetCreatureEntity = selfAIEntity.FindCreatureEntityForSinge(DirectionEnum.Left);
        
        // 2. 播放待机动画
        selfAIEntity.selfCreatureEntity.SetFaceDirection(Direction2DEnum.Left);
        selfAIEntity.selfCreatureEntity.PlayAnim(SpineAnimationStateEnum.Idle, true);
        
        // 3. 如果没有防守生物，目标设为魔王核心
        if (selfAIEntity.targetCreatureEntity == null)
        {
            var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
            selfAIEntity.targetCreatureEntity = gameFightLogic.fightData.fightDefenseCoreCreature;
        }
        
        // 4. 记录目标位置
        if (selfAIEntity.targetCreatureEntity != null)
        {
            selfAIEntity.targetMovePos = selfAIEntity.targetCreatureEntity.creatureObj.transform.position;
        }
    }

    public override void IntentUpdate(AIBaseEntity aiEntity)
    {
        // 发现目标后切换到移动状态
        if (selfAIEntity.targetCreatureEntity != null)
        {  
            selfAIEntity.ChangeIntent(AIIntentEnum.AttackCreatureMove);
        }
    }
}
```

### 5.2 AIIntentAttackCreatureAttack (进攻生物攻击意图)

**文件**: `Scrpits/AI/Creature/FightAttackCreature/AIIntentAttackCreatureAttack.cs`

继承通用的 `AIIntentCreatureAttack`，仅配置特定参数：

```csharp
public class AIIntentAttackCreatureAttack : AIIntentCreatureAttack
{
    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        selfAIEntity = aiEntity as AIAttackCreatureEntity;
        intentForIdle = AIIntentEnum.AttackCreatureIdle;   // 攻击完成后回到闲置
        intentForDead = AIIntentEnum.AttackCreatureDead;   // 死亡时切换到死亡意图
        base.IntentEntering(aiEntity);
    }
}
```

---

## 六、AI 辅助工具

### 6.1 AIBaseCommon (AI 通用工具)

**文件**: `FrameWork/Scrpits/AI/AIBaseCommon.cs`

```csharp
public static class AIBaseCommon
{
    /// <summary>
    /// 视线搜索-圆形范围
    /// </summary>
    /// <param name="sourcePosition">眼睛位置</param>
    /// <param name="searchRadius">搜索半径</param>
    /// <param name="layerSearchTarget">搜索物体的层级</param>
    /// <param name="layerObstacles">遮挡物体层级</param>
    public static List<Collider> SightSearchCircle(
        Vector3 sourcePosition, 
        float searchRadius, 
        int layerSearchTarget, 
        int layerObstacles)
}
```

---

## 七、使用示例

### 7.1 创建并启动 AI

```csharp
// 在生物创建时绑定 AI
public void CreateCreatureAndBindAI(FightCreatureEntity creature)
{
    // 创建 AI 实例
    var aiEntity = AIHandler.Instance.CreateAIEntity<AIAttackCreatureEntity>(ai =>
    {
        // 在启动前初始化数据
        ai.InitData(creature);
    });
    
    // AI 自动启动，默认进入 Idle 状态
}
```

### 7.2 移除 AI

```csharp
// 生物死亡时移除 AI
public void OnCreatureDead(FightCreatureEntity creature)
{
    var aiEntity = creature.aiEntity;
    AIHandler.Instance.RemoveAIEntity(aiEntity);
}
```

### 7.3 实现自定义 AI 实体

```csharp
public class AICustomEntity : AICreatureEntity
{
    public override void InitIntentEnum(List<AIIntentEnum> listIntentEnum)
    {
        listIntentEnum.Add(AIIntentEnum.AttackCreatureIdle);
        listIntentEnum.Add(AIIntentEnum.AttackCreatureAttack);
        listIntentEnum.Add(AIIntentEnum.AttackCreatureDead);
    }

    public override void StartAIEntity()
    {
        // 默认进入闲置状态
        ChangeIntent(AIIntentEnum.AttackCreatureIdle);
    }

    public override void CloseAIEntity()
    {
        // 清理逻辑
    }
}
```

### 7.4 实现自定义意图

```csharp
public class AIIntentCustomAttack : AIBaseIntent
{
    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        // 进入攻击状态时的初始化
        var creatureAI = aiEntity as AICreatureEntity;
        creatureAI.selfCreatureEntity.PlayAnim(SpineAnimationStateEnum.Attack, false);
    }

    public override void IntentUpdate(AIBaseEntity aiEntity)
    {
        // 每帧检查攻击是否完成
        var creatureAI = aiEntity as AICreatureEntity;
        
        if (AttackCompleted())
        {
            // 攻击完成，回到闲置
            ChangeIntent(AIIntentEnum.AttackCreatureIdle);
        }
    }

    public override void IntentLeaving(AIBaseEntity aiEntity)
    {
        // 离开攻击状态时的清理
    }
}
```

---

## 八、注意事项

1. **意图命名规范**: 意图类名必须以 `AIIntent` 开头，后接枚举名称（去掉 `AIIntentEnum` 部分），例如 `AIIntentEnum.AttackCreatureIdle` 对应类 `AIIntentAttackCreatureIdle`

2. **反射创建**: `AIBaseEntity.InitIntentEntity()` 使用反射自动创建意图实例，类名必须与枚举名匹配

3. **事件清理**: AI 实体继承 `BaseEvent`，需要在 `ClearData()` 中调用 `UnRegisterAllEvent()` 避免内存泄漏

4. **对象池复用**: AI 实例被移除时会进入对象池，下次创建同类型 AI 时会复用，因此 `InitData()` 必须能正确重置状态

---

*文档结束*
