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
│   ├── AIIntentAttackCreatureIdle       # 闲置
│   ├── AIIntentAttackCreatureMove       # 移动
│   ├── AIIntentAttackCreatureAttack     # 攻击(打防守生物, 走 AttackMode)
│   ├── AIIntentAttackCreatureAttackCore # 攻击魔王(靠近后固定触发一次攻击并让魔王死亡, 不走 AttackMode)
│   ├── AIIntentAttackCreatureLured      # 被引诱
│   └── AIIntentAttackCreatureDead       # 死亡
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
- **进攻生物出生线守卫**：`AIIntentAttackCreatureMove` 在"找到目标→切 `AttackCreatureAttack`"处加了位置判定——自身 `x > 10.5`（出生线 x≈11.5 附近）时**不进入攻击意图**，保留目标并继续向左推进，直到 `x <= 10.5` 才允许切攻击。
- **进攻生物打魔王（核心）专用路径**：敌人（近战/远程一视同仁）**不会用 AttackMode 攻击魔王**。`AIIntentAttackCreatureMove` 的核心分支持续向魔王推进，当与魔王距离 `< AIIntentAttackCreatureMove.CloseCoreDistance`(0.25) 时切到 `AttackCreatureAttackCore`；该意图固定播放一次攻击动作（`GetAttackAnimTime` 缺省用 0.5s 保底），出手时对魔王播出血特效并直接 `coreCreature.SetCreatureDead()` 让魔王死亡（不经任何 AttackMode），随后核心走 `DefenseCoreCreatureDead` 死亡意图，死亡结束事件驱动 `GameFightLogic.CheckGameEnd()` 判定战斗失败、游戏结束。原因：远程弹道靠 layer 掩码只检测 `CreatureDef` 层，而魔王核心在默认层 layer0，弹道本就打不到；近战原本直接结算能打死核心——现统一改为"靠近即固定处决"，让近远程行为一致。
  - **多单位并发**：允许多个进攻生物同时靠近并各自播攻击动作，但"魔王出血死亡"全局只结算一次——`KillDefenseCore` 内 `IsDead()` 守卫拦截同帧/后续单位的重复致死；魔王已被他人处决时本单位直接回 `AttackCreatureIdle`，不空转、不重复播出血/结束游戏。

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
