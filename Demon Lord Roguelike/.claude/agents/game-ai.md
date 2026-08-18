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
│   ├── AIIntentAttackCreatureKnockback  # 被击退(冲击波等位移效果：StartKnockback 强制切换，固定 0.2s 匀速推完，结束回 Idle 重新索敌)
│   └── AIIntentAttackCreatureDead       # 死亡
├── AIDefenseCreatureEntity         # 防守生物（5 个意图：Idle/Attack/Defend/Dead/Charge）
│   ├── AIIntentDefenseCreatureAttack
│   ├── AIIntentDefenseCreatureDefend
│   ├── AIIntentDefenseCreatureIdle
│   ├── AIIntentDefenseCreatureDead
│   └── AIIntentDefenseCreatureCharge    # 冲锋（冲锋自爆型：放卡即向+X冲锋，遇敌/到路尽头引爆）
└── AIDefenseCoreCreatureEntity     # 核心生物
    ├── AIIntentDefenseCoreCreatureIdle
    └── AIIntentDefenseCoreCreatureDead
```

### 通用意图
- **AIIntentCreatureAttack** - 通用攻击意图（可继承复用）；内置 **额外攻击** 机制（见下）；内置 **目标距离复查**（见下）
- **AIIntentCreatureDead** - 通用死亡意图

### 击退意图（AIIntentAttackCreatureKnockback，位移效果统一机制）
- **发起入口**：`AIAttackCreatureEntity.StartKnockback(direction, distance)`——击退参数经 `GetIntent` 直接写入击退意图实例（`SetupKnockback`）后 `ChangeIntent`；**击退中再次被击退只刷新参数**（原地续推，不重进意图）。调用先例：`AttackModeShockwaveRing`（深渊馈赠「第六次冲击」，方向固定 `Vector3.right` 沿道路向后推，不带 z 分量防敌人被推离路径）。
- **推移**：固定 `KnockbackDuration=0.2s` 匀速推完全程（任何击退距离时长一致、推速=距离/时长，计时走 `GetFightDeltaTime` 跟随 2 倍速）；落点 x 钳制：右缘硬钳 `[0.5+路长]`、左缘只防「从道路内被推出左缘」——已在左缘内(x<0.5，直冲魔王阶段)的敌人不往前拉（防击退变"前吸"），自然向右推回道路；播 Idle 动画（被控状态）。
- **结束**：剩余距离走完回 `AttackCreatureIdle`，重新走「闲置→移动→攻击」索敌流程——与防守目标的距离重新判定，不会隔空续打；强制切换本身即打断攻击循环（挥刀被打飞中断）。
- **死亡**：击退中死亡由 `FightCreatureEntityForAttack` 死亡流程 `ChangeIntent(Dead)` 覆盖，意图无需自处理。

### 冲锋意图（AIIntentDefenseCreatureCharge，冲锋自爆型防守生物）
- **适用**：`CreatureInfo.charge_attack=1` 的冲锋自爆型防守生物（如 6003 哥布林敢死队），放卡后**不站桩**、立即向 +X（敌人来向）冲锋。入口分流在 `AIDefenseCreatureEntity.StartAIEntity`——`creatureInfo.IsChargeAttack()` 为真直接进冲锋意图，跳过 Idle。
- **IntentEntering**：置 `fightCreatureData.isPositionReleased = true`（**冲锋开始即释放原占位格**，占位/删除扫描跳过它，原格可立刻放第二只魔物）；朝右 `SetFaceDirection(Right)`；播 Walk 动画（`animSpeed: ChargeSpeedRate`，动画速度=冲锋倍率5与移速同步）；缓存道路尽头 `roadEndPosX = 0.5f + fightData.sceneRoadLength`；立即安排首次索敌（贴脸放卡即爆）。
- **IntentUpdate**：按 `attack_search_time` 节奏（6003 已改配 0.1 秒）`FindCreatureEntityForSinge(DirectionEnum.Right)` 前方索敌（该生物 `attack_search_range=0.5` 即触发距离）→ 命中直接 `SetCreatureDead()`（爆炸走「死亡即引爆」统一自爆路径，见 attack-mode-system）；`x >= roadEndPosX` 到路尽头同样 `SetCreatureDead()`；否则 `Translate(+X)` 前进，速度 = `MathUtil.InterpolationLerp(MSPD,0,100,0,2f) × ChargeSpeedRate(常量5)` × `GameFightLogic.GetFightDeltaTime()`（2倍速兼容，10点MSPD≈1格/秒）。
- **注册三处**：`AIIntentFactory.RegisterAll` 工厂注册；`AIDefenseCreatureEntity.InitIntentEnum` 加枚举；入口分流见上。防守生物自此拥有移动意图（旧"防守生物无移动意图"认知不再成立）。

### 死亡结束事件顺序（AIIntentCreatureDead，契约：事件先行）
- `IntentUpdate` 现为**先 `TriggerEvent(GameFightLogic_CreatureDeadEnd)` 再 `RemoveFightCreatureEntity`**。原因：移除会同步清掉生物全部 BUFF（`BuffHandler.RemoveFightCreatureBuffs`），先移除会导致 BuffEntityConditionalDead 系（重生/死亡爆发/死亡掉水晶/死亡范围伤害等）永远收不到事件——这是 BuffHandler 注释约定的契约（事件先行）。曾误翻转（2026-05-24），现已恢复。
- 动态数量 BUFF（都是兄弟/独行者等）计数本已按 `IsDead()` 过滤，不受事件先行影响。

### 攻击意图目标距离复查（AIIntentCreatureAttack 基类，攻防通用）
- **机制**：`IntentUpdate` 开头（仅 `attackState` 0准备/1出手 阶段）调 `CheckTargetInAttackRange()`——**与索敌完全同口径**：向目标所在方向做一次同款 `FindCreatureEntityForSinge` 搜索，搜到任何目标即仍在射程内（可能搜到更近的新目标，攻击循环会自然切换），搜不到才 `ChangeIntent(intentForIdle)` 中断回待机重新索敌，防"目标被击退/诱导拉远后隔老远还在攻击"；`attackState==2`（已发射等回调）不打断，交 `ActionForAttackEnd` 重索敌自然处理。
- **⚠️不能用「中心距 ≤ searchRange」判定**（2026-08 击退后卡死 BUG 根因）：索敌射线命中的是目标 collider **表面**，命中时两中心距离 = searchRange + collider半径——中心距口径比索敌严格一个 collider 半径，会把「索敌认为能打」的位置判成脱靶，造成 Idle→Move→Attack 死循环原地卡住（每帧切换、Move 的索敌 CD 拉满即切走导致位移代码永远执行不到）。
- **无副作用场景**：敌人 Attack 中站定+目标（防守生物）不动 → 复查恒真；远程大 searchRange 恒真；近战目标被击退 0.5 出射线范围 → 搜不到 → 中断重索敌（设计目的）。

### 额外攻击（攻击模块扩展，命名通用、不限于 BOSS）
- **配置**：`NpcInfo.attack_mode_ext`（逗号分隔的 `AttackModeExtInfo` id）→ `AttackModeExtInfo`（`ext_type` 类型，目前仅 `1`=`AttackModeExtTypeEnum.BossSkill` 按间隔释放、`trigger_interval` 间隔秒、`attack_mode_id` 指向 `AttackModeInfo`）。
- **实现位置**：逻辑全部在基类 `AIIntentCreatureAttack`（`InitExtraAttack/UpdateExtraAttackTimer/GetReadyExtraAttack` + `IntentEntering/IntentUpdate/AttackCreatureStart/AttackCreatureStartEnd/IntentLeaving` 挂钩），进攻/防守生物均自动获得，**无需新增意图/枚举/工厂**。
- **运行机制（融入普通攻击循环，非并行）**：各额外攻击独立累计CD（`UpdateExtraAttackTimer` 仅计时）；在每次攻击循环开始的判定点 `AttackCreatureStart→GetReadyExtraAttack()` 选第一个CD已到的额外攻击，`AttackCreatureStartEnd` 发射并清零其CD。**额外攻击优先级>普通攻击**：本次有就绪额外攻击则替代普通攻击（占用该循环）；CD到了不立刻打断，需等下次 `attackState==0` 判定。每循环最多一次攻击 → 多个就绪按序逐循环出、天然串行。`InitExtraAttack` 仅收集 `ext_type==BossSkill`，未来新类型在此加分支。发射复用 `FightHandler.StartCreateAttackMode(self, target, ActionForAttackEnd, customAttackModeId)`。
- **术语**：敌方"BOSS"= `FightAttack` 进攻型 NPC（走 `AIIntentAttackCreatureAttack`），**不是**玩家防守的核心 `AIDefenseCoreCreatureEntity`。

### 防守生物转身攻击身后（正面优先 + 背后补搜，门控 `CreatureInfo.attack_search_back`）
- **门控**：`attack_search_back`(0/1) 开启的防守生物正面无目标时转身攻击身后，身后清空/超范围转回正面。首用者骷髅战士 `CreatureInfo id=2001`。缓存 `bool isAttackSearchBack = creatureInfo.IsAttackSearchBack()`（`IntentEntering` 缓存，避免每循环重读）。未开启行为不变、零开销。
- **双向搜索**：`AICreatureEntity.FindCreatureEntityForSingeFrontThenBack(DirectionEnum frontDirection, bool searchBack)`——正面优先命中即短路，正面无目标且 searchBack 才反向补搜一次（背后范围=正面范围）。防守正面=Right。`AIIntentDefenseCreatureIdle`/`AIIntentDefenseCreatureAttack` 均改用它。
- **基类两个 virtual 钩子**（`AIIntentCreatureAttack`，默认不改进攻/核心行为）：`FindNextTarget(BaseAttackMode)` 默认单向搜(原 `ActionForAttackEnd` 内联逻辑抽出，防守覆盖为双向搜)；`RefreshFaceForTarget()` 默认空，基类在 `AttackCreatureStart()`(出手前) 与 `ActionForAttackEnd`(切目标后) 各调一次。
- **转身**：防守 `RefreshFaceForTarget` 按 目标.x 相对 自身.x → `>=` 设 Right 否则 Left；`isAttackSearchBack==false` 直接 return。弹道方向 `BaseAttackMode` 有目标时自动=归一化(目标-自身)，攻击模块层无需改。

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

## 意图计时的游戏速度（2倍速）约定

意图内一切按时间推进的逻辑（移动 `Translate`、攻击/索敌/死亡计时等）**必须用 `GameFightLogic.GetFightDeltaTime()`**（= `Time.deltaTime × 当前游戏速度`，非战斗场景恒 1 倍），**禁止直接 `Time.deltaTime`**——否则 2倍速下该行为仍是 1 倍节奏。已由 AIHandler.Update 逐帧驱动的意图全部遵守此约定；动画播放速度不在此列，由 `FightCreatureEntity.SetAnimTimeScale`（`SkeletonAnimation.timeScale`）随 `SetGameSpeed` 全场同步。

## 约束

- 意图类名必须以 `AIIntent` 开头，后接枚举名称
- `AIBaseEntity.InitIntentEntity()` 优先走 `AIIntentFactory` 注册表创建意图实例，未注册时才回退反射 + 字符串拼接类名（兼容旧扩展）；**新增意图必须在 `AIIntentFactory.RegisterAll()` 内显式注册**
- `ChangeIntent` 的目标枚举必须属于当前 AI 实体的 `listIntentEnum`，否则只会打印 `转换AI意图Xxx失败，意图池里没有此意图` 并保留当前意图（典型坑：防守生物错误切换到 `DefenseCoreCreatureXxx`）
- AI 实体继承 BaseEvent，需在 ClearData 中调用 UnRegisterAllEvent
- AI 实例有对象池复用，InitData 必须能正确重置状态
