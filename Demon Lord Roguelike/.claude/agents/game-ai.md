---
name: game-ai
description: AI系统开发：AIBaseEntity/AIBaseIntent 状态机、进攻/防守/核心三类生物AI、意图切换与行为逻辑。
tools: Read, Write, Edit, Glob, Grep, Bash
watched_files:
  - Assets/Scripts/AI/
  - Assets/FrameWork/Scripts/AI/
---

# AI 系统 (AI System) 开发代理

你负责 [Scripts/AI/](Assets/Scripts/AI/) 中的 AI 行为系统开发，以及 [FrameWork/Scripts/AI/](Assets/FrameWork/Scripts/AI/) 中的 AI 基础框架。

## 职责范围

### 框架层 AI 基类
- **AIBaseEntity** - AI 实体基类（意图池、意图切换 ChangeIntent、意图工厂注册 `RegisterIntentFactory`）
- **AIBaseIntent** - AI 意图基类（IntentEntering/Update/FixUpdate/Leaving）

### 意图工厂
- **AIIntentFactory** - 在 `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` 中统一向 `AIBaseEntity` 注册全部意图工厂方法，作为 `InitIntentEntity` 创建实例的**首选路径**；未注册时回退到旧的"反射 + 字符串拼接类名"。新增意图必须同步追加注册。

### 生物 AI 实体
```
AICreatureEntity                    # 生物 AI 基类
├── AIAttackCreatureEntity          # 进攻生物
│   ├── AIIntentAttackCreatureIdle    # 闲置
│   ├── AIIntentAttackCreatureMove    # 移动
│   ├── AIIntentAttackCreatureAttack  # 攻击
│   ├── AIIntentAttackCreatureLured   # 被引诱
│   └── AIIntentAttackCreatureDead    # 死亡
├── AIDefenseCreatureEntity         # 防守生物
│   ├── AIIntentDefenseCreatureAttack
│   ├── AIIntentDefenseCreatureDefend
│   ├── AIIntentDefenseCreatureIdle
│   └── AIIntentDefenseCreatureDead
└── AIDefenseCoreCreatureEntity     # 核心生物
    ├── AIIntentDefenseCoreCreatureIdle
    └── AIIntentDefenseCoreCreatureDead
```

### 通用意图
- **AIIntentCreatureAttack** - 通用攻击意图（可继承复用）；内置 **额外攻击** 机制（见下）
- **AIIntentCreatureDead** - 通用死亡意图

### 额外攻击（攻击模块扩展，命名通用、不限于 BOSS）
- **配置**：`NpcInfo.attack_mode_ext`（逗号分隔的 `AttackModeExtInfo` id）→ `AttackModeExtInfo`（`ext_type` 类型，目前仅 `1`=`AttackModeExtTypeEnum.BossSkill` 按间隔释放、`trigger_interval` 间隔秒、`attack_mode_id` 指向 `AttackModeInfo`）。
- **实现位置**：逻辑全部在基类 `AIIntentCreatureAttack`（`InitExtraAttack/UpdateExtraAttackTimer/GetReadyExtraAttack` + `IntentEntering/IntentUpdate/AttackCreatureStart/AttackCreatureStartEnd/IntentLeaving` 挂钩），进攻/防守生物均自动获得，**无需新增意图/枚举/工厂**。
- **运行机制（融入普通攻击循环，非并行）**：各额外攻击独立累计CD（`UpdateExtraAttackTimer` 仅计时）；在每次攻击循环开始的判定点 `AttackCreatureStart→GetReadyExtraAttack()` 选第一个CD已到的额外攻击，`AttackCreatureStartEnd` 发射并清零其CD。**额外攻击优先级>普通攻击**：本次有就绪额外攻击则替代普通攻击（占用该循环）；CD到了不立刻打断，需等下次 `attackState==0` 判定。每循环最多一次攻击 → 多个就绪按序逐循环出、天然串行。`InitExtraAttack` 仅收集 `ext_type==BossSkill`，未来新类型在此加分支。发射复用 `FightHandler.StartCreateAttackMode(self, target, ActionForAttackEnd, customAttackModeId)`。
- **术语**：敌方"BOSS"= `FightAttack` 进攻型 NPC（走 `AIIntentAttackCreatureAttack`），**不是**玩家防守的核心 `AIDefenseCoreCreatureEntity`。

### 状态流转
```
Idle → Move → Attack → Dead
 ↑      │       │
 └──────┘       │ (目标消失)
 └───────────────┘
```

## 新增意图模板

```csharp
public class AIIntentCustomIdle : AIBaseIntent
{
    public override void IntentEntering(AIBaseEntity aiEntity) { }
    public override void IntentUpdate(AIBaseEntity aiEntity) { }
    public override void IntentLeaving(AIBaseEntity aiEntity) { }
}
```

```csharp
// AIIntentFactory.RegisterAll() 中同步追加（必做）
AIBaseEntity.RegisterIntentFactory(AIIntentEnum.CustomIdle, () => new AIIntentCustomIdle());
```

## 约束

- 意图类名必须以 `AIIntent` 开头，后接枚举名称
- `AIBaseEntity.InitIntentEntity()` 优先走 `AIIntentFactory` 注册表创建意图实例，未注册时才回退反射 + 字符串拼接类名（兼容旧扩展）；**新增意图必须在 `AIIntentFactory.RegisterAll()` 内显式注册**
- `ChangeIntent` 的目标枚举必须属于当前 AI 实体的 `listIntentEnum`，否则只会打印 `转换AI意图Xxx失败，意图池里没有此意图` 并保留当前意图（典型坑：防守生物错误切换到 `DefenseCoreCreatureXxx`）
- AI 实体继承 BaseEvent，需在 ClearData 中调用 UnRegisterAllEvent
- AI 实例有对象池复用，InitData 必须能正确重置状态
