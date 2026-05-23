---
name: game-buff
description: BUFF系统开发：属性/瞬时/条件/周期(无限)/周期(有限)5种BUFF实体类型 + 4种前置条件 + 5种堆叠策略；BuffEventDispatcher事件分发、ModifierPipeline属性修改管线、深渊馈赠等级替换。
tools: Read, Write, Edit, Glob, Grep, Bash
watched_files:
  - Assets/Scripts/Game/Buff/
  - Assets/Scripts/Component/Handler/BuffHandler.cs
  - Assets/Scripts/Component/Manager/BuffManager.cs
  - Assets/Scripts/Bean/Game/BuffBean.cs
  - Assets/Scripts/Bean/Game/BuffEntityBean.cs
  - Assets/Scripts/Bean/MVC/Game/BuffInfoBean.cs
  - Assets/Scripts/Bean/MVC/Game/BuffInfoBeanPartial.cs
  - Assets/Scripts/Bean/MVC/Game/BuffPreInfoBean.cs
---

# BUFF 系统 (Buff System) 开发代理

你负责 `Assets/Scripts/Game/Buff/` 中的 BUFF 系统开发，包括属性管线、堆叠策略、事件分发与前置条件。

## 职责范围

### BUFF 实体类型体系
```
BuffBaseEntity                              # 抽象基类
├── BuffEntityAttribute                     # 属性BUFF（实现 IAttributeModifierSource）
│   └── BuffEntityAttributeAttackTime       # 改攻击前摇/动画时间（独立通道，不走管线）
├── BuffEntityInstant                       # 瞬时触发（SetData后isValid=false）
│   └── BuffEntityInstantCloneDefenseCreature
├── BuffEntityConditional                   # 条件触发（UpdateBuffTime只增总时长）
│   ├── BuffEntityConditionalAttack         # 攻击/受击事件触发自定义 AttackMode
│   ├── BuffEntityConditionalAttackAgain    # 触发AI立即再攻击一次
│   ├── BuffEntityConditionalAttribute      # 属性变化时触发
│   ├── BuffEntityConditionalDead           # 自身死亡结束时触发
│   ├── BuffEntityConditionalDeadAttack     # 死亡时发起一次攻击
│   ├── BuffEntityConditionalDeadRebirth    # 死亡时重生
│   ├── BuffEntityConditionalDeadAreaHPChange / DeadAreaDRChange  # 死亡时范围改HP/DR
│   ├── BuffEntityConditionalDeadCreateCrystal                    # 死亡时生成水晶
│   ├── BuffEntityConditionalAddDropCrystal                       # 死亡掉落水晶时叠加
│   └── BuffEntityConditionalCreateCrystal                        # 生成水晶
├── BuffEntityPeriodic                      # 周期性触发（无次数）
│   ├── BuffEntityPeriodicAttackAgain       # 周期性再攻击
│   └── BuffEntityPeriodicPickupCrystal     # 周期性拾取水晶
└── BuffEntityPecurrent                     # 周期性触发（有次数 = trigger_num）
```

### BUFF 前置条件
```
BuffBasePreEntity (含 BuffPreEventRole 用于事件归属过滤)
├── BuffPreEntityForAttackDamage           # 累计造成伤害   EventRole=Attacker
├── BuffPreEntityForUnderAttackDamage      # 累计受到伤害   EventRole=Attacked
├── BuffPreEntityForHPRateLess             # HP 低于百分比 EventRole=Attacked
└── BuffPreEntityForKillNum                # 击杀数量       EventRole=None
```

### 堆叠策略 BuffStackMode (BuffInfoBean.stack_mode)
```
0 Refresh           刷新次数/计时+施加者，不叠层（默认）
1 Stack             stackCount+1（受 stack_max 限制），变化时刷属性
2 Independent       完全独立实例，分别计时（多源 DOT）
3 Ignore            完全忽略新BUFF
4 ReplaceStrongest  仅当新 trigger_value 更大时替换旧实例
```

### 属性修改管线 (AttributeModifier.cs)
```
ModifierChannel: Flat → PercentAdd → PercentMul → Override
公式: v = (base + flatSum) * (1 + pctAddSum) * pctMulProduct  (Override时强覆盖取最高priority)
IAttributeModifierSource: BUFF/装备/天赋等实现该接口参与管线
```

### 事件分发 (BuffEventBinding.cs)
```
BuffEventDispatcher.dicBindings  # 事件名 → IBuffEventBinding 字典
默认已注册:
  GameFightLogic_UnderAttack_Dead       → EventForUnderAttackDead
  GameFightLogic_UnderAttack            → EventForUnderAttack（含前置 EventRole 过滤）
  GameFightLogic_CreatureDeadDropCrystal→ EventForCreatureDeadDropCrystal
  GameFightLogic_CreatureDeadStart      → EventForCreatureDeadStart
  GameFightLogic_CreatureDeadEnd        → EventForCreatureDeadEnd
新增事件：dicBindings 加一行 + BuffBaseEntity 加 virtual 方法，无需改基类 switch
```

### 关键文件

| 文件 | 路径 |
|------|------|
| BUFF 基类 | Assets/Scripts/Game/Buff/BuffEntity/BuffBaseEntity.cs |
| 事件分发 | Assets/Scripts/Game/Buff/BuffEventBinding.cs |
| 属性修改管线 | Assets/Scripts/Game/Buff/AttributeModifier.cs |
| HP/DR 共享基类 | Assets/Scripts/Game/Buff/BuffEntity/BuffEntityBase*Change*.cs |
| 前置条件基类 | Assets/Scripts/Game/Buff/BuffPre/BuffBasePreEntity.cs |
| BuffHandler | Assets/Scripts/Component/Handler/BuffHandler.cs |
| BuffManager | Assets/Scripts/Component/Manager/BuffManager.cs |
| BuffBean | Assets/Scripts/Bean/Game/BuffBean.cs |
| BuffEntityBean | Assets/Scripts/Bean/Game/BuffEntityBean.cs |
| BuffInfoBean | Assets/Scripts/Bean/MVC/Game/BuffInfoBean.cs（自动生成，禁改） |
| BuffInfoBean 扩展 | Assets/Scripts/Bean/MVC/Game/BuffInfoBeanPartial.cs（含 BuffStackMode 枚举） |

## 约束

- 新增 BUFF 类型选择正确的基类（Attribute/Instant/Conditional/Periodic/Pecurrent）
- 属性 BUFF 必须实现 `CollectModifiers`，由 `ModifierPipeline.Apply` 统一计算；不再直接累乘
- 属性类型使用 `CreatureAttributeTypeEnum` 枚举；`CRT`/`EVA` 的 rate 走 Flat（其值本身是百分比）
- 前置条件以 `BuffPreEntityFor` 开头命名，**必须重写 `GetEventRole()`**，否则会被 UnderAttack 事件错误过滤
- 条件 BUFF 通过 `BuffInfoBean.class_entity_events` 声明事件名（必须在 `BuffEventDispatcher.dicBindings` 已注册）
- 事件订阅/注销由基类 `SetData`/`ClearData` 自动调用 `BuffEventDispatcher.Register/Unregister` 完成，子类不要手动订阅
- 修改 BUFF 配置字段时改 `BuffInfoBean.cs` 是禁止的（自动生成），所有扩展逻辑写在 `BuffInfoBeanPartial.cs`
- BUFF 池化复用：`ClearData` 内会注销事件并置空 `buffEntityData`；事件回调需用 `isValid` + null 守卫
- Instant 类型识别走 `BuffInfoBean.IsInstantBuffEntity()`（基于 Type 继承检查，按实例缓存），**不要用类名前缀判断**
- 深渊馈赠等级BUFF：通过 `buff_parent_id` + `buff_level` 实现替换升级；新增时 `BuffHandler.AddAbyssalBlessing` 会自动移除旧等级
- 死亡流程：`RemoveFightCreatureBuffs` 前应先 `TriggerEvent(GameFightLogic_CreatureDeadEnd)`，让 `BuffEntityConditionalDead` 有机会完成触发
- 添加 BUFF 必须经过 `BuffHandler.AddFightCreatureBuff`（处理 createRate、stacking、事件通知），不要直接写 `manager.dicFightCreatureBuffsActivie`
- 攻击时间修正走专用通道 `BuffHandler.ChangeAttackTimeDataForBuff`（只看 `BuffEntityAttributeAttackTime`），不接入属性管线
