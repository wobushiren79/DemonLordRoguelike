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

## BaseAttackMode 关键字段

- **`isValid`** - 是否激活；`Destroy()` 时置 `false`，外层遍历据此跳过
- **`instanceId`** - `FightManager` 分配的实例 ID，`dlAttackModePrefab`（DictionaryList）按此 key 做 O(1) 移除
- **`searchCreatureType`** - 由 `attackedLayerTarget` 推导出的搜索类型，在 `StartAttack(attacker,...)` 中缓存、`Destroy()` 中清零，子类范围检测应复用而非每帧重算

## 新增攻击模式模板

```csharp
public class AttackModeCustom : BaseAttackMode
{
    public override void StartAttack(FightCreatureEntity attacker, FightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        // 自定义初始化（伤害/方向已在 base 中根据 attacker/attacked 写入 attackModeData）
        actionForAttackEnd?.Invoke(this);
    }

    public override void Update()
    {
        base.Update();
        // 远程/持续类型在此驱动；命中后调用 Destroy() 回收
    }

    public override void Destroy(bool isPermanently = false)
    {
        // 清理子类自有的缓冲（如候选 List/HashSet），避免对象池复用残留
        base.Destroy(isPermanently);
    }
}
```

## 约束

- 攻击模式必须继承 BaseAttackMode
- 每种攻击模式独立一个文件
- 文件名与类名一致：AttackMode + 类型名
- **禁止在热路径调用 `GameHandler.Instance.manager.GetGameLogic<GameFightLogic>()`**，需要战斗逻辑统一通过 `FightHandler.Instance.manager.GetCachedFightLogic()`（懒加载，`FightManager.Clear()` 会自动失效）
- 维护跨帧状态（如连锁记录、穿透命中名单、候选缓冲）的子类必须重写 `Destroy(bool)` 清空它们，否则下一次出对象池时会带上一次的数据
- 复用候选缓冲使用 `readonly List<>` 字段配合 `Clear()`，禁止在 Update 里 `new List<>` 造成 GC
- `effect_hit` 配置允许 `&` 分隔多组特效，调用 `PlayEffectForHit(pos, index)` 时通过 `index` 选择（如 `AttackModeFalluponChain` 用 0/1 区分初始击中与连锁击中）
