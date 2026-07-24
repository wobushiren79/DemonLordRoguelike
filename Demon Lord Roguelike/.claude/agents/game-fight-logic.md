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

### PreGame 扩展钩子（按时序，virtual 空实现供子类重写）
1. `PreGameForAfterInitFightSceneCamera` - 战斗镜头初始化后
2. `PreGameForAfterLoadFightScene` - 战斗场景加载后
3. `PreGameForAfterCreateDefenseCore` - 防守核心创建/`InitFightConstData` 后、开启控制前；**需要以防守核心为操作目标时（如 BuffHandler.AddAbyssalBlessing）只能用此钩子**（更早调用核心未创建会失败/被跳过）。例：`GameFightLogicTest` 在此清理并添加测试深渊馈赠

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

### 游戏速度（2倍速 Speed2）
- `UpdateGame` 的 `updateTime = Time.deltaTime * fightData.gameSpeed`——游戏时间流速倍率挂在 fightData 上（默认1，仅本场战斗有效，不改 Time.timeScale）。
- `SetGameSpeed(float)`（public）- 2倍速按钮入口：写 `fightData.gameSpeed` 并 `RefreshAllCreatureAnimTimeScale()` 给全部在场生物（防守核心+进攻/防守）`SetAnimTimeScale(gameSpeed)`（=`SkeletonAnimation.timeScale`）；新建生物在 `FightCreatureEntity.SetData` 里按当前速度自动初始化。
- `GetCurrentGameSpeed()`（static）- 当前战斗速度（非战斗逻辑/无 fightData 恒1）；`GetFightDeltaTime()`（static）= `Time.deltaTime × GetCurrentGameSpeed()`——**不经 UpdateGame 驱动的战斗系统（AI意图、弹道、掉落物寿命）统一用它替代 `Time.deltaTime`**，新增战斗内计时逻辑同此约定。常量 `GAME_SPEED_2X = 2`。2倍速按钮显隐/研究门控见 game-fight-system / research-system SKILL。

### 防守属性重算（深渊馈赠联动）
- `RefreshAllDefenseCreatureAttribute()`（public）- 刷新防守核心 + 全部防守魔物 `RefreshBaseAttribute`（由原 `EventForAbyssalBlessingChange` 的循环抽出，后者改为调它）。供动态数值馈赠（加成率随场上魔物数/累计击杀数变化；曾用于都是兄弟/杀红了眼，现役无配置、机制留存）在战况变化时重算属性。
- `EventForAbyssalBlessingChange(AbyssalBlessingEntityBean)` - 监听 `EventsInfo.Buff_AbyssalBlessingChange`：① 调 `RefreshAllDefenseCreatureAttribute()` 重算属性；② 调 `fightDefenseCoreCreature.RefreshAbyssalBlessingOrbit()` 刷新魔王身边环绕的馈赠图标（全量对账增删，升级替换先删后加天然兼容）。环绕机制详见 FightCreatureEntityForDefenseCore.cs「魔王-深渊馈赠环绕图标(GPU单Mesh)」region。
- `EventForGameFightLogicCreatureDeadEnd` 中按守卫广播：`if (BuffHandler.Instance.HasDynamicRateAbyssalBlessing()) RefreshAllDefenseCreatureAttribute();`——魔物死亡（随魔物数缩放类 N 减少）/敌人死亡（随击杀数缩放类计数增加）都重算全体防守，且**该重算放在 `CheckGameEnd()` 之前**（先处理死亡带来的属性变化，再检测游戏结束）。守卫用 O(1) 缓存 `HasDynamicRateAbyssalBlessing()`（读 BuffManager 缓存布尔，在 AddAbyssalBlessing 选取动态率馈赠时单调置 true、ClearAbyssalBlessing 复位）避免普通对局开销，热路径不遍历池。
- `EventForDefenseCreatureCreate(FightCreatureEntity)` - 监听 `EventsInfo.GameFightLogic_DefenseCreatureCreate`（由 `CreatureHandler.CreateDefenseCreatureEntity` 生成新防守魔物后推送），按同一守卫广播 `RefreshAllDefenseCreatureAttribute()`，使随魔物数缩放类馈赠随放置/增殖魔物 N 增大即时生效。即 CreatureHandler 只负责生成、推事件，重算职责归 GameFightLogic。详见 abyssal-blessing-system / buff-system SKILL。

### 魔晶拾取链路（DSP 渲染器单一路径）
- 魔晶全程 DSP 批量渲染（`FightManager.fightDropCrystalInstanceRenderer`，每颗魔晶=纯数据槽，零 GameObject/零碰撞体/零 DOTween，旧 GameObject 模式已删除；渲染器机制见 game-fight-core agent）；渲染器未就绪（视觉预制缺失）时生成/拾取接口全部零副作用。
- 三个拾取入口：`PickupCrystalForMouse`（`TryPickByScreenPoint` 鼠标射线世界距离判定→命中播音效 sound_btn_15）、`PickupCrystalForCreature`（`PickBySphere` 世界距离判定，生物半径内全吸）、`PickupCrystalForCoreAuto(count)`（`PickFIFO` 按槽生成序取最早 count 颗，`UpdateGameForDefenseCore` 按研究间隔驱动）。
- 拾取统一走 `PickupCrystalByRenderer(index)`（gameState 校验后 `StartFlyBack` 飞回核心，抛物线 CPU 参数化复刻旧 DOJump）；飞回到账由渲染器回调 `OnDropCrystalArrived`（`InitFightConstData` 注入 `actionForCrystalArrived`，`FightManager.Clear` 时清空；渲染器内到账先记账、帧末压缩后统一派发，遍历中途不 Invoke）：`AddCrystal`(内含音效) → `GameFightLogic_DropAddCrystal` 事件 → `RefreshUI`(同帧多颗到账合并为一次)。

## 约束

- 新增战斗模式需继承 GameFightLogic，实现 Pre/Start/Update/End/Clear
- 战斗逻辑通过 EventHandler 与其他系统通信
- GameHandler 是游戏逻辑的统一入口
