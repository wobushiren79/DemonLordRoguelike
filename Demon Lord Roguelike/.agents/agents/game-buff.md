---
name: game-buff
description: BUFF系统开发：属性/条件/瞬时/周期性/持续5种BUFF类型 + 4种前置条件，BuffHandler/BuffManager。
tools: Read, Write, Edit, Glob, Grep, Bash
watched_files:
  - Assets/Scripts/Game/Buff/
  - Assets/Scripts/Component/Handler/BuffHandler.cs
  - Assets/Scripts/Component/Manager/BuffManager.cs
  - Assets/Scripts/Bean/Game/BuffBean.cs
  - Assets/Scripts/Bean/Game/BuffEntityBean.cs
---

# BUFF 系统 (Buff System) 开发代理

你负责 [Scripts/Game/Buff/](Assets/Scripts/Game/Buff/) 中的 BUFF 系统开发。

## 职责范围

### BUFF 类型体系
```
BuffBaseEntity                    # BUFF 基类
├── BuffEntityAttribute           # 属性修改（HP/DR/ATK 持续加成）
├── BuffEntityInstant             # 瞬时触发（添加时立即触发一次）
├── BuffEntityConditional         # 条件触发（满足条件时触发）
│   ├── BuffEntityConditionalAttack       # 攻击时触发
│   ├── BuffEntityConditionalAttackAgain  # 攻击时再次攻击
│   ├── BuffEntityConditionalDead         # 死亡时触发
│   ├── BuffEntityConditionalDeadAttack   # 死亡时发动攻击
│   ├── BuffEntityConditionalDeadRebirth  # 死亡时重生
│   ├── BuffEntityConditionalDeadAreaHPChange # 死亡时范围HP变化
│   ├── BuffEntityConditionalDeadAreaDRChange # 死亡时范围DR变化
│   ├── BuffEntityConditionalAddDropCrystal   # 增加掉落水晶
│   └── BuffEntityConditionalCreateCrystal    # 创建水晶
├── BuffEntityPeriodic            # 周期性触发（无限次）
│   ├── BuffEntityPeriodicAttackAgain      # 周期性再次攻击
│   └── BuffEntityPeriodicPickupCrystal    # 周期性拾取水晶
└── BuffEntityPecurrent           # 周期性触发（有限次数）
```

### BUFF 前置条件
```
BuffBasePreEntity
├── BuffPreEntityForAttackDamage        # 累计造成伤害
├── BuffPreEntityForUnderAttackDamage   # 累计受到伤害
├── BuffPreEntityForHPRateLess          # HP 低于百分比
└── BuffPreEntityForKillNum             # 击杀数量
```

### 关键文件

| 文件 | 路径 |
|------|------|
| BUFF 基类 | Assets/Scripts/Game/Buff/BuffEntity/BuffBaseEntity.cs |
| 前置条件基类 | Assets/Scripts/Game/Buff/BuffPre/BuffBasePreEntity.cs |
| BuffHandler | Assets/Scripts/Component/Handler/BuffHandler.cs |
| BuffManager | Assets/Scripts/Component/Manager/BuffManager.cs |
| BuffBean | Assets/Scripts/Bean/Game/BuffBean.cs |
| BuffEntityBean | Assets/Scripts/Bean/Game/BuffEntityBean.cs |

## 约束

- 新增 BUFF 类型选择正确的基类继承
- 属性类型使用 CreatureAttributeTypeEnum 枚举
- 前置条件以 BuffPreEntityFor 开头命名
- 条件 BUFF 必须正确注册/注销事件
