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
  - Assets/Scripts/AI/Creature/AIIntentFactory.cs
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
    │       │       ├── AIIntentAttackCreatureIdle       (闲置)
    │       │       ├── AIIntentAttackCreatureMove       (移动)
    │       │       ├── AIIntentAttackCreatureAttack     (攻击防守生物, 走 AttackMode)
    │       │       ├── AIIntentAttackCreatureAttackCore (攻击魔王: 靠近后固定处决, 不走 AttackMode)
    │       │       ├── AIIntentAttackCreatureDead       (死亡)
    │       │       └── AIIntentAttackCreatureLured      (被诱惑)
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
    AttackCreatureIdle,       // 闲置
    AttackCreatureMove,       // 移动
    AttackCreatureAttack,     // 攻击(打防守生物, 走 AttackMode)
    AttackCreatureAttackCore, // 攻击魔王(靠近后固定处决, 不走 AttackMode)
    AttackCreatureDead,       // 死亡
    AttackCreatureLured,      // 被诱惑

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

> 注意：当前优先通过 `AIIntentFactory` 显式注册工厂方法创建意图，**新增意图必须在 [AIIntentFactory.cs](Assets/Scripts/AI/Creature/AIIntentFactory.cs) 中追加一行注册**；仅在未注册时才回退到反射 + 字符串拼接类名（兼容旧扩展），因此类名拼写错误会在工厂注册环节于编译期暴露，避免运行时静默失败。

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

### 4. 在 AIIntentFactory 中注册工厂方法（必做）

```csharp
// Assets/Scripts/AI/Creature/AIIntentFactory.cs
private static void RegisterAll()
{
    // ... 已有注册 ...
    AIBaseEntity.RegisterIntentFactory(AIIntentEnum.CustomCreatureIdle, () => new AIIntentCustomCreatureIdle());
}
```

`AIIntentFactory.RegisterAll()` 由 `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]` 在场景加载前自动调用，所有意图工厂在游戏启动早期就会注入 `AIBaseEntity` 的注册表。

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
| `InitIntentEntity()` | 创建所有意图实例并初始化：优先调用工厂表，未注册时回退反射 |
| `StartAIEntity()` | 启动AI (抽象方法) |
| `CloseAIEntity()` | 关闭AI (抽象方法) |
| `InitIntentEnum(List<AIIntentEnum>)` | 初始化意图枚举列表 (抽象方法) |
| `static RegisterIntentFactory(AIIntentEnum, Func<AIBaseIntent>)` | 注册指定枚举的意图工厂方法（由 `AIIntentFactory` 统一调用） |
| `static CreateIntentByFactory(AIIntentEnum)` | 通过工厂创建意图实例，找不到返回 null（私有，仅供 `InitIntentEntity` 使用） |

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
| `FindCreatureEntityForSingeFrontThenBack(DirectionEnum frontDirection, bool searchBack)` | 正面优先搜 frontDirection，命中即短路返回；正面无目标且 searchBack==true 时才向反方向补搜一次（复用同一 searchType/searchRange，背后范围=正面范围）。防守生物「转身攻击身后」用 |
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

## 额外攻击（攻击模块扩展，按间隔自动释放）

通用攻击意图 [AIIntentCreatureAttack.cs](Assets/Scripts/AI/Creature/AIIntentCreatureAttack.cs) 内置了一套"额外攻击"机制（命名通用、不限于 BOSS）：凡 **NPC 配置了 `NpcInfo.attack_mode_ext`** 的生物（典型为敌方 BOSS，如 `1010020001` 持盾战士-Boss），进入攻击状态后会在普通攻击之外，按各额外攻击自己的间隔**额外**释放攻击模块。

- **数据来源**：`NpcInfo.attack_mode_ext`（逗号分隔的 `AttackModeExtInfo` id）→ `AttackModeExtInfo`（`ext_type` 为额外攻击类型，目前仅 `1`=`AttackModeExtTypeEnum.BossSkill` 按间隔释放；`trigger_interval` 释放间隔秒、`attack_mode_id` 指向 `AttackModeInfo`）。运行时经 `fightCreatureData.creatureData.creatureNpcData?.npcInfo.GetListAttackModeExtInfo()` 读取（仅 NPC 敌人有 `creatureNpcData`，玩家生物为 null 不受影响）。
- **挂载在基类**：逻辑写在基类 `AIIntentCreatureAttack`，故进攻/防守生物（`AIIntentAttackCreatureAttack` / `AIIntentDefenseCreatureAttack`）均自动获得，**无需新增意图/枚举/工厂**。敌方 BOSS 是 `FightAttack` 型 → 走 `AIIntentAttackCreatureAttack`（继承基类、仅重写 `IntentEntering` 并 `base` 调用，故基类 `IntentUpdate/IntentLeaving` 直接生效）。
- **计时与判定（融入普通攻击循环，非并行）**：`IntentEntering→InitExtraAttack()` 重置各额外攻击计时器；`IntentUpdate→UpdateExtraAttackTimer()` 每帧**仅累加**各额外攻击CD（不在此释放）。释放融入普通攻击循环：在 `AttackCreatureStart`（attackState 准备完毕、本次攻击开始的判定点）调 `GetReadyExtraAttack()` 选出第一个CD已到的额外攻击；在 `AttackCreatureStartEnd` 用其 `attack_mode_id` 发射并把该额外攻击CD清零。`IntentLeaving` 清空、离开/重入会重新计时。
- **优先级与串行**：**额外攻击优先级 > 普通攻击**——某次攻击判定时若有就绪额外攻击，本次出额外攻击（占用该攻击循环、替代普通攻击）；没有则照常普通攻击。CD到了**不会立刻打断**当前攻击，要等下一次 `attackState==0` 的判定。每个攻击循环最多出一次攻击，故多个就绪的额外攻击按列表顺序逐循环释放、天然串行（无需额外标志）。
- **类型筛选与扩展**：`InitExtraAttack` 仅收集 `ext_type==BossSkill` 的额外攻击；未来新增其他 `ext_type` 可在该筛选处加独立分支（不同类型可有不同触发逻辑）。
- **发射入口**：复用 `FightHandler.StartCreateAttackMode(self, target, ActionForAttackEnd, customAttackModeId: ext.attack_mode_id)`（与 BUFF 触发攻击同一入口），回调即普通攻击的 `ActionForAttackEnd`（找下个目标并回到 `attackState==0`）。

> 术语提醒：游戏内"BOSS"=**敌方强力 NPC**（征服 `enemy_boss_ids` 刷出、`FightAttack` 进攻型），**不是**玩家防守的"魔王核心" `AIDefenseCoreCreatureEntity`。给 BOSS（或任意 NPC）加额外攻击应改基类攻击意图，而非核心生物 AI。

---

## 防守生物转身攻击身后（正面优先 + 背后补搜）

防守生物默认朝右（正面 = 进攻生物来袭方向）。开启 `CreatureInfo.attack_search_back`(0/1) 的防守生物在**正面无目标时会转身攻击身后**，身后清空/超范围则转回正面待机。首个使用者：骷髅战士 `CreatureInfo id=2001`。

- **双向搜索**：搜索改走 `AICreatureEntity.FindCreatureEntityForSingeFrontThenBack(DirectionEnum.Right, isAttackSearchBack)`（防守生物正面=Right）。正面优先，命中即短路；正面无目标且门控开启时才向反方向补搜一次，背后范围 = 正面范围。`AIIntentDefenseCreatureIdle`（待机搜目标）与 `AIIntentDefenseCreatureAttack`（攻击结束找下个目标）均改用它。
- **门控缓存**：两个防守意图在 `IntentEntering` 缓存 `bool isAttackSearchBack = creatureInfo.IsAttackSearchBack()`（避免每次攻击循环重复读配置）。未开启的生物短路掉背后搜索，行为与原来完全一致、零额外开销。
- **基类两个 virtual 钩子**（挂在 `AIIntentCreatureAttack`，默认不改变进攻/核心生物行为）：
  - `protected virtual FightCreatureEntity FindNextTarget(BaseAttackMode attackMode)`：默认沿本发攻击方向单向搜（原 `ActionForAttackEnd` 内联逻辑抽出），`ActionForAttackEnd` 改为调用它。防守攻击意图覆盖为 `FindCreatureEntityForSingeFrontThenBack`。
  - `protected virtual void RefreshFaceForTarget()`：默认空实现；基类在 `AttackCreatureStart()`（出手前、目标存活校验后、播攻击动画前）与 `ActionForAttackEnd`（切到新目标后）各调一次。
- **转身**（`AIIntentDefenseCreatureAttack.RefreshFaceForTarget` 覆盖）：按 目标.x 相对 自身.x —— `目标.x>=自身.x`→`SetFaceDirection(Direction2DEnum.Right)`，否则 `Left`；`isAttackSearchBack==false` 时直接 return 不转身。待机意图开启门控时 `IntentEntering` 内 `SetFaceDirection(Right)` 转回正面。
- **弹道自动朝背后**：`BaseAttackMode` 有目标时弹道方向自动 = 归一化(目标-自身)，设为身后目标后攻击/AreaBoxFront 自动朝背后，**攻击模块层无需改**。

---

## 注意事项

1. **意图命名规范**: 意图类名必须以 `AIIntent` 开头，后接枚举名称。例如 `AIIntentEnum.AttackCreatureIdle` 对应类 `AIIntentAttackCreatureIdle`。回退反射路径使用 `AIIntent + 枚举名称` 拼接类名，因此命名错位会在运行时静默失败（错误日志：`创建AI意图失败：未在工厂中注册且反射也未找到类 AIIntentXxx`）。

2. **意图工厂注册（优先路径）**: `AIBaseEntity.InitIntentEntity()` 优先通过工厂表创建意图实例，未命中时才回退到反射。**新增意图必须在 [AIIntentFactory.cs](Assets/Scripts/AI/Creature/AIIntentFactory.cs) 中添加一行 `AIBaseEntity.RegisterIntentFactory(AIIntentEnum.Xxx, () => new AIIntentXxx());`**，工厂方法在 `BeforeSceneLoad` 自动注入，编译期就能暴露类名/命名空间错误。

3. **事件清理**: AI实体继承 `BaseEvent`，需要在 `ClearData()` 中调用 `UnRegisterAllEvent()` 避免内存泄漏。

4. **对象池复用**: AI实例被移除时会进入对象池，下次创建同类型AI时会复用，因此 `InitData()` 必须能正确重置状态。

5. **意图切换的目标枚举要与归属生物类型一致**：例如防守生物在搜索不到目标时应切回 `DefenseCreatureIdle` 而不是 `DefenseCoreCreatureIdle`，否则会因 `dicIntentPool` 中没有该枚举而触发 `ChangeIntent` 失败的日志告警，并保持当前意图不变。

6. **敌人攻击魔王（核心）不走 AttackMode**：进攻生物（近战/远程一视同仁）**不再用 AttackMode 伤害魔王**。`AIIntentAttackCreatureMove` 核心分支持续向魔王推进，与魔王距离 `< AIIntentAttackCreatureMove.CloseCoreDistance`(0.25) 时切 `AttackCreatureAttackCore`；该意图固定播一次攻击动作（`GetAttackAnimTime` 缺省用 0.5s 保底），出手时对魔王播出血特效并直接 `coreCreature.SetCreatureDead()`，随后核心走 `DefenseCoreCreatureDead`，死亡结束事件驱动 `GameFightLogic.CheckGameEnd()` 判定战斗失败。背景：远程弹道靠 layer 掩码只检测 `CreatureDef` 层、魔王核心在默认层 layer0 本就打不到，遂将近远程统一为"靠近即固定处决"。新增意图已同步 `AIAttackCreatureEntity.InitIntentEnum` 与 `AIIntentFactory`。

7. **意图计时必须走 `GameFightLogic.GetFightDeltaTime()`**：意图内一切按时间推进的逻辑（移动 `Translate`、攻击准备/出手计时、索敌间隔、死亡计时等）统一用 `GameFightLogic.GetFightDeltaTime()`（= `Time.deltaTime × 当前游戏速度`，非战斗场景恒 1 倍）**替代 `Time.deltaTime`**——否则 2倍速（Speed2 按钮，`fightData.gameSpeed=2`）下该行为仍是 1 倍节奏。动画播放速度不在此列：由 `FightCreatureEntity.SetAnimTimeScale`（`SkeletonAnimation.timeScale`）随 `GameFightLogic.SetGameSpeed` 全场同步。

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
| 意图工厂注册器 | `Assets/Scripts/AI/Creature/AIIntentFactory.cs` |
| 进攻生物AI | `Assets/Scripts/AI/Creature/FightAttackCreature/AIAttackCreatureEntity.cs` |
| 防守生物AI | `Assets/Scripts/AI/Creature/FightDefenseCreature/AIDefenseCreatureEntity.cs` |
| 核心生物AI | `Assets/Scripts/AI/Creature/FightDefenseCoreCreature/AIDefenseCoreCreatureEntity.cs` |
