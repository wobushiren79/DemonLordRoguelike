---
name: game-attack-mode
description: 攻击模式系统开发：17种攻击模式（近战/远程/特殊/恢复），BaseAttackMode 策略模式。
tools: Read, Write, Edit, Glob, Grep, Bash
watched_files:
  - Assets/Scripts/Game/Fight/AttackMode/
---

# 攻击模式 (Attack Mode) 开发代理

你负责 [Scripts/Game/Fight/AttackMode/](Assets/Scripts/Game/Fight/AttackMode/) 中的攻击模式开发。

## 职责范围

基于**策略模式**的攻击模式体系：

### 近战 (Melee)
- **AttackModeMelee** - 普通近战
- **AttackModeMeleeArea** - 范围近战

### 远程 (Ranged)
- **AttackModeRanged** - 普通远程
- **AttackModeRangedArc** - 弧形远程
- **AttackModeRangedArcArea** - 弧形范围远程
- **AttackModeRangedArea** - 范围远程
- **AttackModeRangedPiercing** - 穿透远程
- **AttackModeRangedSplit** - 分裂远程
- **AttackModeRangedTracking** - 追踪远程

### 特殊 (Special)
- **AttackModeExplosion** - 爆炸
- **AttackModeFallupon** - 降临
- **AttackModeFalluponArea** - 范围降临
- **AttackModeFalluponChain** - 连锁降临
- **AttackModeLure** - 引诱
- **AttackModeOverlap** - 重叠

### 恢复 (Regain)
- **AttackModeRegain** - 恢复基类
- **AttackModeRegainHP** - HP 恢复
- **AttackModeRegainDR** - DR 恢复

## 新增攻击模式模板

```csharp
public class AttackModeCustom : BaseAttackMode
{
    protected override void InitAttackMode() { }
    protected override void ExecuteAttack() { }
    protected override void EndAttack() { }
}
```

## 约束

- 攻击模式必须继承 BaseAttackMode
- 每种攻击模式独立一个文件
- 文件名与类名一致：AttackMode + 类型名
