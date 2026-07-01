---
name: buff-system
description: Demon Lord Roguelike 游戏的BUFF系统开发指南。使用此SKILL当需要创建或修改BUFF效果、BUFF触发逻辑、BUFF配置、属性修改管线、BUFF堆叠策略等，包括属性BUFF、条件触发BUFF、周期性BUFF、即时BUFF、前置条件、BuffEventDispatcher、ModifierPipeline、深渊馈赠等级替换等。
watched_files:
  - Assets/Scripts/Game/Buff/
  - Assets/Scripts/Game/Attribute/AttributeModifier.cs
  - Assets/Scripts/Bean/Game/BuffBean.cs
  - Assets/Scripts/Bean/Game/BuffEntityBean.cs
  - Assets/Scripts/Utils/BuffUtil.cs
  - Assets/Scripts/Bean/MVC/Game/BuffInfoBean.cs
  - Assets/Scripts/Bean/MVC/Game/BuffInfoBeanPartial.cs
  - Assets/Scripts/Bean/MVC/Game/BuffPreInfoBean.cs
  - Assets/Scripts/Bean/MVC/Game/BuffPreInfoBeanPartial.cs
  - Assets/Scripts/Component/Manager/BuffManager.cs
  - Assets/Scripts/Component/Handler/BuffHandler.cs
---

# BUFF系统开发指南

## 核心概念

### BUFF数据三件套

```
BuffBean          - BUFF静态数据（buffId + 实例化时确定的trigger_value/rate/chance/num/time）
BuffEntityBean    - BUFF运行时实例数据（施加者/目标/剩余次数/堆叠层数/条件值）
BuffInfoBean      - BUFF配置数据（来自BuffInfo配置表，含 class_entity / stack_mode / pre_info 等）
```

### BUFF 实体类型体系

```
BuffBaseEntity                              # 抽象基类（事件回调 + ShowBuffEffect + CheckIsPre）
├── BuffEntityAttribute                     # 属性BUFF（实现 IAttributeModifierSource）
│   ├── BuffEntityAttributeAttackTime       # 改攻击前摇/动画时间的属性BUFF（独立通道）
│   │   └── BuffEntityAttributeAttackTimeSingleTarget  # 深渊馈赠「急性子」：随机锁定一只防守生物攻速翻倍(攻击时间×0.5)，实现 IBuffSingleTarget
│   └── BuffEntityAttributeSingleTarget    # 深渊馈赠「大力出奇迹/膘肥体壮/钢铁憨憨」：随机锁定一只防守生物 ATK/HP/DR 翻倍(rate=1)，实现 IBuffSingleTarget
├── BuffEntityInstant                       # 瞬时BUFF（SetData中立即触发并isValid=false）
│   ├── BuffEntityInstantCloneDefenseCreature   # 深渊馈赠「增殖」：随机复制一个防守生物
│   ├── BuffEntityInstantRewardMoreItem         # 深渊馈赠「奖励多多」：累加 FightBeanForConquer.rewardAddItemNum（领奖时+1奖励物品）
│   └── BuffEntityInstantRewardMoreSelect       # 深渊馈赠「再来一瓶」：累加 FightBeanForConquer.rewardAddSelectNum（领奖时+1可选次数）
├── BuffEntityConditional                   # 条件触发（UpdateBuffTime 只走总时长，不走周期）
│   ├── BuffEntityConditionalAttack         # 攻击/受击事件触发：发起一次自定义AttackMode
│   ├── BuffEntityConditionalAttackAgain    # 触发立即再攻击一次（复用当前AI意图）
│   ├── BuffEntityConditionalAttribute      # 属性变化时触发
│   ├── BuffEntityConditionalDead           # 自身死亡结束时触发
│   ├── BuffEntityConditionalDeadAttack     # 死亡时发动一次攻击
│   ├── BuffEntityConditionalDeadRebirth    # 死亡时重生
│   ├── BuffEntityConditionalDeadAreaHPChange / DeadAreaDRChange  # 死亡时区域改HP/DR
│   ├── BuffEntityConditionalDeadCreateCrystal                    # 死亡时生成水晶
│   ├── BuffEntityConditionalAddDropCrystal                       # 死亡掉落水晶时叠加
│   └── BuffEntityConditionalCreateCrystal                        # 生成水晶（事件类）
├── BuffEntityPeriodic                      # 周期性触发（无次数限制）
│   ├── BuffEntityPeriodicAttackAgain       # 周期性强制再攻击
│   └── BuffEntityPeriodicPickupCrystal     # 周期性自动拾取水晶
└── BuffEntityPecurrent                     # 周期性触发（有次数限制 = trigger_num）
```

### BUFF 前置条件

```
BuffBasePreEntity                                # 前置基类（GetEventRole + CheckIsPre）
├── BuffPreEntityForAttackDamage          # 累计造成伤害 (EventRole = Attacker)
├── BuffPreEntityForUnderAttackDamage     # 累计受到伤害 (EventRole = Attacked)
├── BuffPreEntityForHPRateLess            # 当前HP百分比低于阈值 (EventRole = Attacked)
└── BuffPreEntityForKillNum               # 击杀数量 (EventRole = None)
```

`BuffPreEventRole`（None/Attacked/Attacker）用于在 `BuffBaseEntity.EventForUnderAttack` 中
判断本次"被攻击/攻击"事件是否归属当前BUFF目标。**任何新增的前置条件实体必须重写 `GetEventRole()`**，
否则在Attacker/Attacked事件中会被错误过滤掉。

## 关键架构（必读）

### 1. 事件绑定 — `BuffEventDispatcher`

事件订阅/反订阅由 `Assets/Scripts/Game/Buff/BuffEventBinding.cs` 集中管理，
**不再在 `BuffBaseEntity` 用 switch 硬编码**。

```csharp
//新增 BUFF 触发事件时，只需在 dicBindings 追加一行：
{ EventsInfo.YourNewEvent,
  new BuffEventBinding<YourArgType>(e => e.EventForYourNewEvent) },
```

然后在 `BuffBaseEntity` 中新增 `EventForYourNewEvent` 虚方法即可，子类按需重写。

### 2. 属性修改管线 — `ModifierPipeline` (AttributeModifier.cs)

属性BUFF不再用串行的 `ChangeData` 累乘（叠序敏感），而是通过 `IAttributeModifierSource` 收集
`AttributeModifier`，再由 `ModifierPipeline.Apply` 按通道统一计算（叠序无关）：

| 通道 | 行为 | 适用 |
|------|------|------|
| `Flat` | 直接相加 `value += m.value` | 平A加成（ATK +100） |
| `PercentAdd` | 同通道 rate 累加，最后乘一次 `(1 + Σrate)` | 普通百分比加成（+15% 与 +20% = +35%） |
| `PercentMul` | 每个 `(1+rate)` 连乘 | 独立倍率（克制系数、独立buff） |
| `Override` | 取最高 `priority` 的值 | 锁血/锁攻速/特殊状态 |

最终公式：`v = (base + flatSum) * (1 + pctAddSum) * pctMulProduct`，若有Override则取覆盖值。

`BuffEntityAttribute.EmitModifiers` 默认把 `trigger_value` 走 Flat、`trigger_value_rate` 走 PercentAdd；
但 `CRT` / `EVA` 例外：`rate` 本身就是百分比绝对值，直接 Flat 累加。

### 3. 堆叠策略 — `BuffStackMode`

向已有同ID BUFF的生物再次添加同ID BUFF时，由 `BuffInfoBean.stack_mode` 决定行为：

| 值 | 模式 | 行为 |
|----|------|------|
| 0 | `Refresh` | 刷新次数/计时 + 施加者，不叠层（默认，兼容旧行为） |
| 1 | `Stack` | `stackCount += 1`（受 `stack_max` 限制），层变化时 `RefreshBaseAttribute` |
| 2 | `Independent` | 走新增分支，独立实例分别计时（多源DOT） |
| 3 | `Ignore` | 完全忽略新BUFF |
| 4 | `ReplaceStrongest` | 仅当新 `trigger_value` 更大时替换 |

合并由 `BuffHandler.ApplyStackingPolicy` 处理。**Instant类型BUFF强制走独立分支**（每次添加都触发）。

### 4. 对象池

- `BuffManager.dicBuffEntityPool` 按类名缓存 `BuffBaseEntity` 实例
- `BuffManager.queueBuffEntityPool` 缓存 `BuffEntityBean` 实例
- `BuffManager.dicBuffPreEntity` 长期缓存前置条件实例（永不清理）

回收路径：`RemoveBuffEntity` → 标记 `isValid=false` → `ClearData()`（注销事件 + 置空 buffEntityData）→ 入池。

### 5. Instant 类型识别 — Type 继承检查

`BuffInfoBean.IsInstantBuffEntity()` 通过 `Type.GetType(class_entity)` + `IsAssignableFrom` 判断，
**不再用类名前缀**，避免改名后静默失效。结果按实例缓存。

## 创建新BUFF

### 1. 选择基类

| 触发时机 | 基类 | 备注 |
|---------|------|------|
| 持续生效的属性加成 | `BuffEntityAttribute` | 实现 `CollectModifiers`，由 ModifierPipeline 统一计算 |
| 改攻击时间 | `BuffEntityAttributeAttackTime` | 走独立分支（`BuffHandler.ChangeAttackTimeDataForBuff`） |
| 添加时立即触发一次 | `BuffEntityInstant` | 复活、克隆等一次性效果 |
| 特定事件触发 | `BuffEntityConditional` 或其子类 | 必须配置 `class_entity_events` |
| 周期性无次数 | `BuffEntityPeriodic` | `trigger_num = 0` |
| 周期性有次数 | `BuffEntityPecurrent` | `trigger_num > 0`，耗尽即销毁 |

### 2. 实体类骨架

#### 属性BUFF（带条件门控）

```csharp
public class BuffEntityAttributeWhenLowHP : BuffEntityAttribute
{
    public override void CollectModifiers(List<AttributeModifier> sink)
    {
        if (buffEntityData == null || !buffEntityData.isValid) return;
        //自定义条件：HP < 50% 才生效
        var targetCreature = GetFightCreatureEntityForTarget();
        if (targetCreature == null) return;
        float hpRate = (float)targetCreature.fightCreatureData.HPCurrent
                       / targetCreature.fightCreatureData.GetAttribute(CreatureAttributeTypeEnum.HP);
        if (hpRate > 0.5f) return;
        //条件满足，正常 emit
        EmitModifiers(sink, buffEntityData.buffData, attributeType, buffEntityData.stackCount, this);
    }
}
```

#### 条件触发BUFF

```csharp
public class BuffEntityConditionalCustom : BuffEntityConditional
{
    public override bool TriggerBuffConditional(BuffEntityBean buffEntityData)
    {
        bool ok = base.TriggerBuffConditional(buffEntityData);  // 包含 triggerChance 判定 + ShowBuffEffect
        if (!ok) return false;
        //执行自定义效果...
        return true;
    }

    public override void HandleForEvent()
    {
        base.HandleForEvent();
        if (CheckIsPre(buffEntityData))
        {
            buffEntityData.conditionalValue = 0;
            TriggerBuffConditional(buffEntityData);
        }
    }
}
```

事件订阅通过 `BuffInfoBean.class_entity_events` 配置事件名（如 `EventsInfo.GameFightLogic_UnderAttack`），
`BuffBaseEntity.SetData` 自动调用 `BuffEventDispatcher.Register`。

#### 即时BUFF

```csharp
public class BuffEntityInstantCustom : BuffEntityInstant
{
    public override bool TriggerBuffInstant(BuffEntityBean buffEntityData)
    {
        base.TriggerBuffInstant(buffEntityData);
        var target = GetFightCreatureEntityForTarget();
        if (target == null) return false;
        //执行一次性效果...
        return true;
    }
}
```

#### 周期性BUFF

```csharp
public class BuffEntityPeriodicCustom : BuffEntityPeriodic
{
    public override bool TriggerBuffPeriodic(BuffEntityBean buffEntityData)
    {
        if (!base.TriggerBuffPeriodic(buffEntityData)) return false;
        //每 trigger_time 秒执行一次...
        return true;
    }
}
```

### 3. 配置 `BuffInfo` 关键字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `id` | long | BUFF唯一ID |
| `buff_type` | int | 1攻击模块 / 2生物自带 / 3深渊馈赠 / 11/12/13稀有度BUFF |
| `rarity` | int | 稀有度（用于UI显示） |
| `buff_parent_id` / `buff_level` | long/int | 等级BUFF父链；同 parent 的新等级会替换旧的（仅深渊馈赠路径） |
| `trigger_creature_type` | int | 0所有 1防御 2进攻 99防守核心 |
| `class_entity` | string | BUFF实体类名（如 `BuffEntityAttribute`） |
| `class_entity_events` | string | 监听事件名（必须在 `BuffEventDispatcher.dicBindings` 中已注册） |
| `class_entity_data` | string | 实体数据（属性BUFF填 `CreatureAttributeTypeEnum`；Attack类填 attackModeId） |
| `pre_info` | string | 前置条件 `preId:value\|preId2:value2` |
| `trigger_value` / `trigger_value_min` | float | 触发值。扭蛋(isRandom)创建时在 **整数闭区间 [min,max]** 内随机（如1~2只得1/2，不出小数） |
| `trigger_value_rate` / `trigger_value_rate_min` | float | 触发值百分比。扭蛋(isRandom)创建时按 **整数百分点闭区间 [min,max]** 随机（如10%~20%只得11%/12%等整数，不出11.5%） |
| `trigger_chance` | float | 触发几率（0-1，0=必然触发）。**固定值不随机**（原 `trigger_chance_min` 已废弃删除） |
| `trigger_num` | int | 触发次数，0为无限 |
| `trigger_time` | float | 触发间隔（秒），-1为无限 |
| `stack_mode` | int | 堆叠策略，见 `BuffStackMode` |
| `stack_max` | int | 最大堆叠层数（0=无上限，仅Stack模式生效） |
| `trigger_effect` | long | 触发特效ID |
| `color_body` | string | 身体染色（hex） |

### 4. 添加新事件类型

1. 在 `EventsInfo` 添加事件常量
2. 在 `BuffEventDispatcher.dicBindings` 追加一行：
   ```csharp
   { EventsInfo.YourEvent, new BuffEventBinding<TArg>(e => e.EventForYourEvent) },
   ```
3. 在 `BuffBaseEntity` 添加 `public virtual void EventForYourEvent(TArg arg)` 虚方法
4. 在子类按需重写

### 5. 添加新前置条件

```csharp
public class BuffPreEntityForCustomCondition : BuffBasePreEntity
{
    //【必填】声明在 UnderAttack 事件中的归属角色
    public override BuffPreEventRole GetEventRole() => BuffPreEventRole.Attacker;

    public override bool CheckIsPre(BuffEntityBean buffEntityData, float preValue)
    {
        var creature = GetTargetCreatureEntity(buffEntityData.targetCreatureUUId);
        if (creature == null) return false;
        return buffEntityData.conditionalValue >= preValue;
    }
}
```

然后在 `BuffPreInfo` 配置表添加一行，`class_entity` 填类名即可。

## 使用 BUFF 系统

### 添加 BUFF 到生物（带创建概率）

```csharp
BuffBean buffData = new BuffBean(buffId, isRandom: true, createRate: 1f);
BuffHandler.Instance.AddFightCreatureBuff(
    new List<BuffBean>() { buffData },
    applierCreatureUUId,
    targetCreatureUUId
);
```

`AddFightCreatureBuff` 内部会：
1. 调用 `CheckBuffCreate` 用 `createRate` 过滤
2. 走 `ApplyStackingPolicy` 判定是否合并到已有BUFF
3. 触发 `EventsInfo.Buff_FightCreatureChange`

### 添加深渊馈赠 BUFF（全局，作用于防守核心）

```csharp
AbyssalBlessingEntityBean blessing = new AbyssalBlessingEntityBean(abyssalBlessingId);
BuffHandler.Instance.AddAbyssalBlessing(blessing);
```

`AddAbyssalBlessing` 内部按**馈赠表自身的 `parent_id`/`level`**（不是 BUFF 的 `buff_parent_id`/`buff_level`，那是已废弃的旧设计）做同族升级替换：
- `AbyssalBlessingInfoCfg.GetFamilyRootId(id)` 回溯族根 → `RemoveAbyssalBlessingByRootId` 移除同族旧级 → 解析新级 `buff_ids`(逗号分隔)逐个加到防守核心 → 触发 `Buff_AbyssalBlessingChange`。详见 `abyssal-blessing-system` SKILL「等级链替换机制」。

### 移除 BUFF

```csharp
BuffHandler.Instance.RemoveFightCreatureBuffs(creatureUUId);              // 全部
BuffHandler.Instance.RemoveFightCreatureBuffs<BuffEntityAttribute>(id);   // 指定类型
```

**死亡流程注意**：调用 `RemoveFightCreatureBuffs` 之前，应先 TriggerEvent
`GameFightLogic_CreatureDeadEnd`，让 `BuffEntityConditionalDead` 能完成触发。

### 查询生物BUFF

```csharp
List<BuffBaseEntity> buffs = BuffHandler.Instance.manager.GetFightCreatureBuffsActivie(creatureId);
bool hasBuff = buffs?.Any(b => b.buffEntityData.buffId == targetBuffId) ?? false;
```

### 攻击时间修正（专用通道）

```csharp
float timeAttackPre = 1.0f, timeAttacking = 0.5f;
BuffHandler.Instance.ChangeAttackTimeDataForBuff(creatureUUId, ref timeAttackPre, ref timeAttacking);
```

只会遍历 `BuffEntityAttributeAttackTime`，不走 ModifierPipeline（因为对应的是时间常量而非属性）。
**该方法除遍历生物自身的战斗BUFF外，还会扫描深渊馈赠池 `dicAbyssalBlessingBuffsActivie`** 中的攻速类BUFF：
若该BUFF实现 `IBuffSingleTarget` 且其 `SingleTargetCreatureUUId == creatureUUId` 才生效，
以此支持「急性子」这类"随机一只防守生物攻速翻倍"的单体定向馈赠（见下「单体定向深渊馈赠」）。

### 单体定向深渊馈赠（随机一只防守生物属性/攻速翻倍）

普通深渊馈赠BUFF（`dicAbyssalBlessingBuffsActivie`）对**所有匹配生物**生效（每个生物 `RefreshBaseAttribute` 都会收集整个馈赠池）。
但「大力出奇迹/膘肥体壮/钢铁憨憨/急性子」要求只作用于**随机一只防守生物**，为此引入标记接口：

```csharp
public interface IBuffSingleTarget { string SingleTargetCreatureUUId { get; } }
```

- 实现类：`BuffEntityAttributeSingleTarget`（ATK/HP/DR，rate=1 即翻倍）、`BuffEntityAttributeAttackTimeSingleTarget`（攻速，攻击时间rate=0.5）。
- **选取时机锁定目标**：两类的 `SetData` 调用 `GameHandler...GetGameLogic<GameFightLogic>()?.fightData?.GetRandomDefenseCreatureUUId()`(实例方法在 `FightBean` 上) 从 `dlDefenseCreatureData` 随机取一只，存其 UUID；`ClearData` 归还对象池时清空。
- **单体过滤落点（两处）**：
  - 属性类：`FightCreatureBean.CollectFromBuffList` 在 `trigger_creature_type` 过滤之后追加一句——`buff is IBuffSingleTarget st && st.SingleTargetCreatureUUId != creatureData.creatureUUId` 则 `continue`，从而该 modifier 只进入被锁定那只生物的 `dicAttribute` 计算。
  - 攻速类：`BuffHandler.ChangeAttackTimeDataForBuff` 扫描馈赠池时按同一 UUID 比对。
- **复制魔物(增殖)不继承单体定向**：`BuffEntityInstantCloneDefenseCreature` 克隆出的新魔物是**新 UUID**，与单体定向馈赠锁定的原魔物 UUID 不匹配，故克隆体**不继承也不显示**原魔物的单体定向馈赠；克隆体只继承「作用于全体防守生物」的馈赠(靠 `trigger_creature_type` 过滤、与 UUID 无关)。
- **卡片展示口径统一**：`AbyssalBlessingUtil.IsAbyssalBlessingTargetCreature(buff, creatureData, fightType)`(在 `Assets/Scripts/Utils/AbyssalBlessingUtil.cs`) 封装「trigger_creature_type 过滤 + 单体定向 UUID 过滤 + 仅 IAttributeModifierSource/BuffEntityAttributeAttackTime 算作用于生物」三连，供战斗卡片(`UIViewCreatureCardItemForFight`)展示「作用于本魔物的馈赠」复用，确保展示与实际效果同步。
- **不污染存档**：`dlDefenseCreatureData` 内的 `CreatureBean` 与玩家存档**共享引用**，故绝不能改 `creatureAttribute`；本方案只改运行时计算出的 `dicAttribute`/攻击时间，馈赠在征服全通关领奖后随 `ClearAbyssalBlessing` 清空。
- **可重复选取(level=0)**：每次选取新建一个BUFF实例、各自锁定一只新随机生物叠加（同族 level=0 不触发替换）；一局可获次数由配置 `max_count` 控制（当前这 4 个 `max_count=1` 即整局限 1 次，候选层 `BuffHandler.GetAbyssalBlessingPickCount` 门控，与本 BUFF 叠加逻辑独立）。
- ⚠️ **选取后立即刷新已在场生物（事件驱动）**：属性类(ATK/HP/DR)依赖 `dicAttribute` 重算，`BuffHandler.AddAbyssalBlessing` 末尾 `TriggerEvent(Buff_AbyssalBlessingChange)`，由 `GameFightLogic.EventForAbyssalBlessingChange` 监听并立即对防守核心 + 全部防守生物 `RefreshBaseAttribute`（BuffHandler 只触发事件、不直接刷新，职责更解耦）。否则征服「普通关卡→普通关卡」走 `ContinueNextLevelInSameScene` 保留现场、不重载场景也不重算属性，加成要等到下次场景重载（切BOSS关 `StartNextGameForBoss` 重建生物实体）才生效——典型BUG「普通关选了不生效、切BOSS才生效」。攻速类(急性子)每次攻击实时缩放不依赖刷新，一并刷新也无害。

## 事件名速查（已注册到 BuffEventDispatcher）

| 事件 | 参数 | 默认回调 |
|------|------|----------|
| `GameFightLogic_UnderAttack_Dead` | `FightUnderAttackBean` | `EventForUnderAttackDead` |
| `GameFightLogic_UnderAttack` | `FightUnderAttackBean` | `EventForUnderAttack`（带前置角色过滤） |
| `GameFightLogic_CreatureDeadDropCrystal` | `FightDropCrystalBean` | `EventForCreatureDeadDropCrystal` |
| `GameFightLogic_CreatureDeadStart` | `FightCreatureEntity` | `EventForCreatureDeadStart` |
| `GameFightLogic_CreatureDeadEnd` | `FightCreatureEntity` | `EventForCreatureDeadEnd` |

新增事件参考前文「添加新事件类型」。

## 属性类型枚举（`CreatureAttributeTypeEnum`）

```
None = 0
HP                // 生命         （枚举成员按顺序自增：HP=1）
MP                // 魔法         （MP=2，魔王/核心向，普通生物一般不消费）
DR                // 护甲(防御)   （DR=3）
ATK               // 攻击力       （ATK=4）
MSPD              // 移动速度     （MSPD=5）
ASPD              // 攻击速度     （ASPD=6）
CRT               // 暴击率       （CRT=7，rate 走 Flat 绝对百分点）
EVA               // 闪避         （EVA=8，rate 走 Flat 绝对百分点）
RCD               // 复活CD       （RCD=9，扭蛋稀有度BUFF可挂 class_entity_data=RCD 负rate 减少召唤CD）
MPR               // 魔法回复%    （MPR=10，魔法向）
MPF               // 魔法回复     （MPF=11，魔法向）
CMP               // 召唤魔力消耗 （CMP=12，仅作BUFF修正标签、非生物常驻战斗属性；GetAttribute(CMP)=基础CMP×(1+等级/稀有度增加倍率)，扭蛋稀有度BUFF负rate 再减少召唤耗魔）

> ⚠️ **无 `HPRegeneration`/生命回复 属性**：真实枚举里没有该成员（曾误列 `HPRegeneration=11`，实际 index 11 是 MPF 魔法回复）。游戏也**没有被动每秒回血刻**，回血只走主动治疗攻击 `AttackModeRegain`。设计 R 属性BUFF时不要用 HPRegeneration，续航向请改用 HP/EVA/DR 等已存在属性。
```

> **扭蛋稀有度 R BUFF 池（buff_type=11）现含**：HP/DR/ATK 增益、ASPD 攻速增益(+50~100%)、RCD 召唤CD减益、CMP 召唤魔力消耗减益(均 -25~50%)。RCD/CMP 减益走负 rate：RCD 走 `GetAttribute(RCD, true)`（基础值/角色加点/装备/自身BUFF→再按需叠加深渊馈赠全局池；includeAbyssalBlessing=true 开启，原 GetRCD 已并入 GetAttribute），CMP 在 `GetAttribute(CMP)` 管线内叠加（先 基础CMP×(1+等级/稀有度增加倍率)，再叠加BUFF；`GetAttributeInt(CMP)` 为 int 封装）。

## 扭蛋/稀有度 BUFF 分档设计规则（buff_type 11/12/13，必读）

稀有度 BUFF 池按稀有度分三档，**每档对「效果性质」有硬性约束**——新增/设计稀有度 BUFF 必须落到正确档位。生成入口 `BuffUtil.CreateRandomRarityBuff` 只按 `buff_type` 取池随机、**不校验效果性质**，归档正确性完全靠本规则人工保证：

| 稀有度 | buff_type | 效果性质（硬约束） | 适用实体类 |
|--------|-----------|-------------------|-----------|
| **R** | 11 | **纯属性 BUFF**：只做属性数值加/减益，常驻生效、无触发条件、无特殊副作用 | `BuffEntityAttribute`（改攻击时间用 `BuffEntityAttributeAttackTime`） |
| **SR** | 12 | **条件/周期被动触发 BUFF**：满足条件（累计造成/受到伤害、击杀数、血量阈值）或按固定周期被动触发后产生效果 | `BuffEntityConditional*`（**非死亡类**）/ `BuffEntityPeriodic*` / `BuffEntityPecurrent`；条件走 `pre_info`+`BuffPreEntityFor*`，事件走 `class_entity_events` |
| **SSR** | 13 | **特殊类 BUFF**：质变/规则改写型，「什么情况都可能发生」——死亡重生、死亡反击、死亡区域治疗/加防、克隆增殖、生成/改变水晶掉落、改变奖励掉落等 | `BuffEntityConditionalDead*` / `BuffEntityInstant*` / 各类特殊实体 |

- **R 档可用属性**：`HP / DR / ATK / ASPD / MSPD / CRT / EVA / RCD / CMP`（CRT/EVA 的 rate 走 Flat；另有 MP/MPR/MPF 魔法向，普通生物一般不消费）。**无 HPRegeneration 生命回复属性**（见下方枚举说明）。R 只能是这些属性的常驻加/减益，**不得**带触发条件、死亡/召唤等特殊行为。
- **高稀有度累积低档**：SSR 生物 = R+SR+SSR 各 1 条、SR 生物 = R+SR 各 1 条（`CreatureBean.RandomRarityBuffForCreate` 逐级授予）。设计 SR/SSR 时应默认玩家已持有低档属性底子，SR/SSR 要体现「质」的差异而非再堆纯属性。
- **归档自检**：设计一条稀有度 BUFF 前先问——它是「常驻纯属性」(→R)、「满足条件/到周期才触发」(→SR)、还是「改变战斗规则的特殊效果」(→SSR)？错档会让扭蛋体验错乱（例如 R 抽出死亡重生）。

## 稀有度 BUFF 生成工具（`BuffUtil`）

稀有度 BUFF（buff_type=11 R / 12 SR / 13 SSR）的「按稀有度随机抽一条」规则已抽到游戏层工具 `Assets/Scripts/Utils/BuffUtil.cs` 统一收口，**扭蛋（GashaponItemBean）与魔物进阶（UICreatureVat）共用同一口径**：

| API | 返回 | 说明 |
|-----|------|------|
| `BuffUtil.GetRarityBuffType(RarityEnum)` | `BuffTypeEnum` | 稀有度 → 稀有度 BUFF 类型。仅 R/SR/SSR 有对应类型（`CreatureRarityR/SR/SSR`）；N/UR/L 返回 `None` |
| `BuffUtil.CreateRandomRarityBuff(RarityEnum)` | `BuffBean` | **扭蛋通用规则**：按 `GetRarityBuffType` 取该类型的 BUFF 列表随机抽 1 条，`new BuffBean(id, isRandom:true)`；无对应类型返回 `null` |
| `BuffUtil.CreateAscendRarityBuff(RarityEnum newRarity, List<CreatureBean> materials)` | `BuffBean` | **魔物进阶规则**：默认走通用随机；素材魔物在 `newRarity` 槽位的 BUFF 按 **buff id** 聚合，每个 id 提供 `10%×数量` 的直接命中概率；命中则继承该 id 并用 `BuffBean.CreateRandomWithFloor` 重随机数值（结果≥素材原值），未命中回退通用随机。UR/L 无类型返回 `null`（只升稀有度不授 BUFF） |
| `BuffUtil.GetCreatureAscendBuffChances(RarityEnum newRarity, List<CreatureBean> materials)` | `List<CreatureAscendBuffChanceStruct>` | **进阶详情展示用**（与 `CreateAscendRarityBuff` 同口径算概率）：每个素材 BUFF id 一项 `rate=10×数量`，列表末尾追加一项「随机增益」(`buffId=-1`，名 `BuffUtil.AscendRandomBuffName`)表示剩余概率 `100-素材命中总和`；UR/L 无类型返回**空列表**（不授 BUFF 不展示）。供 `UICreatureVat` 孵化缸进阶详情面板实时展示。返回结构体 `CreatureAscendBuffChanceStruct` 及内部聚合结构体 `CreatureAscendMaterialBuffStruct` 同放 `Assets/Scripts/Struct/CreatureAscendStruct.cs` |

> 「按稀有度逐级授予稀有度 BUFF」收口在 `CreatureBean.RandomRarityBuffForCreate()`（孕育 `GashaponItemBean.RandomRarity` 与测试面板 `UITestBase.OnClickForAddTestCreature` 共用），单档随机再调 `BuffUtil.CreateRandomRarityBuff`（原 `GashaponItemBean.RandomRarityBuff` 已删除）。新增/调整稀有度 BUFF 仍只需在 `excel_buff_info` 配表，属性类 BUFF 自动进入对应 buff_type 池。

### `BuffBean.CreateRandomWithFloor`（带下限随机工厂）

```csharp
//沿用扭蛋整数闭区间随机口径，但随机下限抬到 max(配置min, floor)，保证结果≥下限
public static BuffBean CreateRandomWithFloor(long id, float floorValue, float floorValueRate, float createRate = 1f)
```

专供魔物进阶「继承素材 BUFF 并重随机数值」使用：`floorValue` / `floorValueRate` 传素材原 BUFF 的 `trigger_value` / `trigger_value_rate`，保证重随机结果不会低于素材原值。

## 常用代码模板

### 创建带创建概率与触发几率的BUFF

```csharp
BuffBean buffData = new BuffBean(100001);
buffData.createRate = 0.3f;       // 30% 概率创建
buffData.trigger_chance = 0.5f;   // 创建后每次触发50%命中
buffData.trigger_value = 100;
buffData.trigger_value_rate = 0.2f;
BuffHandler.Instance.AddFightCreatureBuff(
    new List<BuffBean>() { buffData }, applierId, targetId);
```

### 获取属性BUFF对最终属性的修改（预览路径）

```csharp
//战斗中：FightCreatureBean.RefreshBaseAttribute 已统一走 ModifierPipeline。
//预览/卡片路径：用兼容层
float baseATK = 100;
float final = BuffEntityAttribute.ChangeData(
    buffData, CreatureAttributeTypeEnum.ATK, baseATK, stackCount: 1);
```

### 让自定义来源参与 ModifierPipeline

```csharp
public class MyEquip : IAttributeModifierSource
{
    public void CollectModifiers(List<AttributeModifier> sink)
    {
        sink.Add(new AttributeModifier {
            attributeType = CreatureAttributeTypeEnum.ATK,
            channel = ModifierChannel.PercentMul,
            value = 0.5f,        // ×1.5
            source = this,
        });
    }
}
```

## 文件位置速查

| 功能 | 文件路径 |
|------|----------|
| BUFF基类 | `Assets/Scripts/Game/Buff/BuffEntity/BuffBaseEntity.cs` |
| 事件分发 | `Assets/Scripts/Game/Buff/BuffEventBinding.cs`（`IBuffEventBinding` 接口已抽到 `Interface/`） |
| 属性修改管线 | `Assets/Scripts/Game/Attribute/AttributeModifier.cs`（**通用属性管线，已移出 Buff/**；含 `IAttributeModifierSource` 接口，BUFF/装备/天赋共用，非 BUFF 专属） |
| BUFF接口 | `Assets/Scripts/Game/Buff/Interface/`（`IBuffSingleTarget` 单体定向 / `IBuffEventBinding` 事件绑定，均以 `IBuff` 打头） |
| HP/DR共享基类 | `Assets/Scripts/Game/Buff/BuffEntity/BuffEntityBase*Change*.cs` |
| 属性BUFF | `Assets/Scripts/Game/Buff/BuffEntity/Attribute/` |
| 条件BUFF | `Assets/Scripts/Game/Buff/BuffEntity/Conditional/` |
| 即时BUFF | `Assets/Scripts/Game/Buff/BuffEntity/Instant/` |
| 周期性BUFF（无次数） | `Assets/Scripts/Game/Buff/BuffEntity/Periodic/` |
| 周期性BUFF（有次数） | `Assets/Scripts/Game/Buff/BuffEntity/Pecurrent/` |
| 前置条件基类 | `Assets/Scripts/Game/Buff/BuffPre/BuffBasePreEntity.cs` |
| 前置条件实现 | `Assets/Scripts/Game/Buff/BuffPre/` |
| BUFF数据Bean | `Assets/Scripts/Bean/Game/BuffBean.cs`（含静态工厂 `CreateRandomWithFloor` 带下限随机） |
| 稀有度 BUFF 生成工具 | `Assets/Scripts/Utils/BuffUtil.cs`（`CreateRandomRarityBuff` 通用 / `CreateAscendRarityBuff` 进阶 / `GetRarityBuffType` / `GetCreatureAscendBuffChances` 进阶概率展示） |
| BUFF运行时实例 | `Assets/Scripts/Bean/Game/BuffEntityBean.cs` |
| BUFF配置Bean | `Assets/Scripts/Bean/MVC/Game/BuffInfoBean.cs` |
| BUFF配置扩展 | `Assets/Scripts/Bean/MVC/Game/BuffInfoBeanPartial.cs`（含 `BuffStackMode` 枚举） |
| BUFF前置条件配置 | `Assets/Scripts/Bean/MVC/Game/BuffPreInfoBean.cs` |
| BuffHandler | `Assets/Scripts/Component/Handler/BuffHandler.cs` |
| BuffManager | `Assets/Scripts/Component/Manager/BuffManager.cs` |

## 常见坑

1. **新增前置条件忘记重写 `GetEventRole()`** → 在 Attacker/Attacked 事件中被错误过滤。
2. **属性BUFF 试图修改 `BuffInfoBean.cs`** → 该文件自动生成，扩展请写在 `BuffInfoBeanPartial.cs`。
3. **在 `ClearData` 后访问 `buffEntityData`** → 已置 null，请用 `isValid` 守卫。
4. **`SetData` 重复调用** → 基类已做保护性 `Unregister`，但仍应避免；池化复用时务必先 `ClearData`。
5. **Stack 模式忘配 `stack_max`** → 0 表示无上限；属性BUFF用 stack 时确保 `EmitModifiers` 已用 `stackCount`。
6. **新增事件类型只改 `BuffBaseEntity` 不改 `BuffEventDispatcher`** → 事件订阅不会生效。
