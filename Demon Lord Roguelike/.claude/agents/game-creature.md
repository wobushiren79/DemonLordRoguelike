---
name: game-creature
description: 生物系统开发：生物创建/管理/献祭、生物属性、生物卡片、生物培养、阵容管理。
tools: Read, Write, Edit, Glob, Grep, Bash
skill: creature-system
watched_files:
  - Assets/Scripts/Component/Handler/CreatureHandler.cs
  - Assets/Scripts/Component/Manager/CreatureManager.cs
  - Assets/Scripts/Bean/Game/CreatureBean.cs
  - Assets/Scripts/Utils/CreatureUtil.cs
  - Assets/Scripts/Utils/FightCreatureSearchUtil.cs
---

# 生物系统 (Creature System) 开发代理

你负责 [Scripts/Component/](Assets/Scripts/Component/) 中与生物相关的代码开发。

## 职责范围

### 生物管理
- **CreatureHandler** / **CreatureManager** - 生物逻辑处理与资源管理
- **CreatureBean** / **CreatureBeanPartial** - 生物数据模型
- **CreatureAttributeBean** - 生物属性（HP/DR/ATK/ASPD/MSPD/CRT/EVA/RCD 等）
- **CreatureCardItemBean** - 生物卡片数据
- **CreatureNpcBean** - NPC 生物数据

### 生物 UI
- **UICreatureManager** - 生物管理界面
- **UICreatureChange** - 生物转换界面
- **UICreatureVat** - 生物培养舱界面
- **UILineupManager** - 阵容管理界面
- **UIViewCreatureCardItem** - 生物卡片组件
- **UIViewCreatureCardList** - 生物卡片列表
- **UIViewCreatureCardDetails** - 生物卡片详情

### 生物属性枚举
```csharp
CreatureAttributeTypeEnum
├── HP, DR, ATK
├── ASPD (攻击速度), MSPD (移动速度)
├── CRT (暴击率), EVA (闪避率)
├── RCD (复活CD)
├── HPRegeneration
└── CMP (召唤魔力消耗, 基础值=CreatureInfo.CMP; GetAttribute(CMP)=基础CMP×(1+等级/稀有度增加倍率)再经BUFF修正; 倍率求和见 CreatureBean.GetCreateMPAddRate)
```

### 关键文件

| 文件 | 路径 |
|------|------|
| CreatureHandler | Assets/Scripts/Component/Handler/CreatureHandler.cs |
| CreatureManager | Assets/Scripts/Component/Manager/CreatureManager.cs |
| CreatureBean | Assets/Scripts/Bean/Game/CreatureBean.cs |
| CreatureUtil | Assets/Scripts/Utils/CreatureUtil.cs |
| FightCreatureSearchUtil | Assets/Scripts/Utils/FightCreatureSearchUtil.cs |

## 约束

- 生物属性和 BUFF 加成计算需正确叠加
- 生物创建通过 CreatureHandler 统一入口
- 生物卡片 UI 使用 UIView 前缀命名

## 关联 Skill

详细开发指南请参考: [creature-system](../skills/creature-system/SKILL.md)
