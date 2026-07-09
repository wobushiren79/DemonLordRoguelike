---
name: game-fight-logic
description: 战斗游戏逻辑开发：各种战斗模式逻辑（征服、终焉议会、无限、测试），GameFightLogic 基类与子类。
tools: Read, Write, Edit, Glob, Grep, Bash
watched_files:
  - Assets/Scripts/Game/Logic/
  - Assets/Scripts/Game/Base/BaseGameLogic.cs
  - Assets/Scripts/Component/Handler/GameHandler.cs
  - Assets/Scripts/Component/Manager/GameManager.cs
---

# 战斗逻辑 (Fight Logic) 开发代理

你负责 [Scripts/Game/Logic/](Assets/Scripts/Game/Logic/) 中的战斗逻辑代码开发。

## 职责范围

### 战斗逻辑类
- **GameFightLogic** - 战斗逻辑基类，继承 BaseGameLogic
- **GameFightLogicConquer** - 征服模式战斗
- **GameFightLogicDoomCouncil** - 终焉议会战斗
- **GameFightLogicInfinite** - 无限模式战斗
- **GameFightLogicTest** - 测试战斗

### 游戏状态流转
```
PreGame → StartGame → UpdateGame → EndGame → ClearGame
```

### 关键文件

| 文件 | 路径 |
|------|------|
| 战斗逻辑基类 | Assets/Scripts/Game/Logic/GameFightLogic.cs |
| 征服模式 | Assets/Scripts/Game/Logic/GameFightLogicConquer.cs |
| 终焉议会 | Assets/Scripts/Game/Logic/GameFightLogicDoomCouncil.cs |
| 无限模式 | Assets/Scripts/Game/Logic/GameFightLogicInfinite.cs |
| 测试模式 | Assets/Scripts/Game/Logic/GameFightLogicTest.cs |
| BaseGameLogic | Assets/Scripts/Game/Base/BaseGameLogic.cs |
| GameHandler | Assets/Scripts/Component/Handler/GameHandler.cs |
| GameManager | Assets/Scripts/Component/Manager/GameManager.cs |

### 魔王魔力(MP)系统（仅战斗中有效）
- `UpdateGameForMPRecover(updateTime)` - 每帧给魔王核心恢复 MPF*updateTime 点魔力（MPF=每秒恢复量），并调用 `RefreshMPShow()` 通知刷新魔力显示
- `PutCard()` - 召唤耗魔取 `creatureData.GetAttributeInt(CreatureAttributeTypeEnum.CMP)`（= 基础CMP×(1+等级/稀有度增加倍率)经自身/稀有度BUFF修正，如扭蛋 CMP 减益；倍率求和见 `CreatureBean.GetCreateMPAddRate()`）；放置前检查魔王 `MPCurrent >= GetAttributeInt(CMP)`，不足则 Toast"魔力不足"(UIText 50006)；足够则 `ChangeMP(-GetAttributeInt(CMP))` 扣除并刷新显示。放置成功后播放两个配置粒子：在魔王(防守核心)位置 `EffectHandler.ShowManaEffect(coreCreature.creatureObj.transform.position)`(消耗魔力,EffectInfo id=1000001 Effect_Mana_1)、在生成位置 `EffectHandler.ShowCreatureShowEffect(selectTargetPos)`(魔物登场,id=1100001 Effect_CreatureShow_1)，再播 `AudioEnum.sound_btn_19`。复活CD判定走 `GetAttribute(CreatureAttributeTypeEnum.RCD, true)`（基础值creatureInfo.RCD→角色加点→装备→自身/稀有度RCD减益→再叠加深渊馈赠全局池；第二参 includeAbyssalBlessing=true 开启深渊馈赠按需叠加，逻辑统一在 CreatureBean.GetAttribute 内，原 GetRCD 已并入）

### 进攻刷怪 / Quick(加快进攻节奏)
- `UpdateGameForAttackCreate(updateTime)` - 逐帧累加，达标即出下一波：`fightAttackData.GetNextAttackDetailData()` 取波次 → 刷新间隔 `timeUpdateTargetForAttackCreate=timeNextAttack` → BOSS 首波 `ShowBossDialog` → `CreatureHandler.CreateAttackCreature`。
- `QuickAdvanceAttackCreate(advanceRate=0.1f)`（public）- Quick 按钮调用：立即推进「`timeAttackTotal*advanceRate`(默认10%)」的时间，用与逐帧刷怪同一套步进语义**逐波消费**推进时间、把这段时间本应生成的波次全部立即生成；队列耗尽则停止、进度封顶 100%；返回推进后的最新进度(0~1)。无消耗无冷却。Quick 按钮与世界绑定的显隐见 game-fight-system / research-system SKILL。

### 防守属性重算（深渊馈赠联动）
- `RefreshAllDefenseCreatureAttribute()`（public）- 刷新防守核心 + 全部防守魔物 `RefreshBaseAttribute`（由原 `EventForAbyssalBlessingChange` 的循环抽出，后者改为调它）。供动态数值馈赠（当前用于 都是兄弟/杀红了眼，加成率随场上魔物数/累计击杀数变化）在战况变化时重算属性。
- `EventForGameFightLogicCreatureDeadEnd` 中按守卫广播：`if (BuffHandler.Instance.HasDynamicRateAbyssalBlessing()) RefreshAllDefenseCreatureAttribute();`——魔物死亡（都是兄弟 N 减少）/敌人死亡（杀红了眼击杀数增加）都重算全体防守，且**该重算放在 `CheckGameEnd()` 之前**（先处理死亡带来的属性变化，再检测游戏结束）。守卫用 O(1) 缓存 `HasDynamicRateAbyssalBlessing()`（读 BuffManager 缓存布尔，在 AddAbyssalBlessing 选取动态率馈赠时单调置 true、ClearAbyssalBlessing 复位）避免普通对局开销，热路径不遍历池。
- `EventForDefenseCreatureCreate(FightCreatureEntity)` - 监听 `EventsInfo.GameFightLogic_DefenseCreatureCreate`（由 `CreatureHandler.CreateDefenseCreatureEntity` 生成新防守魔物后推送），按同一守卫广播 `RefreshAllDefenseCreatureAttribute()`，使「都是兄弟」随放置/增殖魔物 N 增大即时生效。即 CreatureHandler 只负责生成、推事件，重算职责归 GameFightLogic。详见 abyssal-blessing-system / buff-system SKILL。

## 约束

- 新增战斗模式需继承 GameFightLogic，实现 Pre/Start/Update/End/Clear
- 战斗逻辑通过 EventHandler 与其他系统通信
- GameHandler 是游戏逻辑的统一入口
