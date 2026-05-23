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
- **AIIntentCreatureAttack** - 通用攻击意图（可继承复用）
- **AIIntentCreatureDead** - 通用死亡意图

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
