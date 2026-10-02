---
name: game-launcher
description: 游戏启动器开发：LauncherGame/LauncherTest、游戏初始化流程、场景加载、Handler 初始化。
tools: Read, Write, Edit, Glob, Grep, Bash
watched_files:
  - Assets/Scripts/Game/Launcher/
  - Assets/Scripts/Common/GameCommonInfo.cs
  - Assets/Scripts/Common/PathInfo.cs
  - Assets/Scripts/Common/ProjectConfigInfo.cs
---

# 启动器 (Launcher) 开发代理

你负责 [Scripts/Game/Launcher/](Assets/Scripts/Game/Launcher/) 中的游戏启动器开发。

## 职责范围

### 启动器类
- **BaseLauncher** - 启动器基类
- **LauncherGame** - 游戏启动器（正式）
- **LauncherTest** - 测试启动器

### 启动流程

```
1. 初始化框架层 Handler（自动创建 Manager）
   ├── ModHandler         → 初始化所有Mod(InitializeAllModsSync, BaseLauncher.Launch 首行,
   │                        必须在 TextHandler/任何Cfg首次访问前, 否则Mod的JsonText(含多语言)合并不生效)
   ├── GameDataHandler    → 加载游戏配置
   ├── AudioHandler       → 初始化音频
   ├── UIHandler          → 初始化 UI 系统
   ├── TextHandler        → 初始化多语言
   ├── ScreenResolutionHandler → 初始化窗口分辨率（窗口模式自由拖动、松手后按锚定宽高比等比吸附）
   └── ...

2. 初始化数据服务
   ├── BaseDataService<GameConfigBean> → 加载游戏配置数据
   └── UserDataService                 → 加载用户存档

3. 进入主场景
   └── WorldHandler.EnterMainForBaseScene()
```

> **LauncherGame.Launch() 初始化链补充**：在 `base.Launch()` 之后新增 `StoryHandler.Instance.InitData()`（故事演出系统初始化，真实游戏入口与 `LauncherTest.StartForNormalGame`「正常启动游戏」注册自动触发——**后者易漏调，漏调则进档后引导演出永不触发**；StoryTest 测试场景不调用，故事演出测试改走 `StartForStoryTest` 直接 `PlayStory`）。

### LauncherTest 测试入口补充

- **测试模式默认全开所有 Mod**：`LauncherTest.Launch()` 在 `base.Launch()` 前置 `ModHandler.Instance.manager.isForceAllModsEnabled = true`——`FilterEnabledMods` 跳过 `GameConfig.listModEnable` 过滤全量加载（仅内存标记不持久化，正式游戏 `LauncherGame` 不置位、用户设置项不受影响），卡片/幻化药等全部测试入口默认可见所有 Mod 资源，免逐一手动开启+重启（机制详见 mod-system skill「测试模式强制全开」）。
- **卡片编辑器测试**：`LauncherTest.StartForCreatureCardEditor(long creatureId, long npcInfoId)`——清场+DoF Off+镜头初始化+CloseAllUI 后，挂纯代码 IMGUI 面板 `TestCreatureCardGUI`（实例化真实卡片预制体，自由设置稀有度/等级/生物/NPC 与显示颜色，可写回配置表）。详见 test-system skill「卡片编辑器测试」。
- **Mod幻化药测试**：`LauncherTest.StartForTransformPotionTest(long itemId)`——同卡片编辑器的前置清理后，挂纯代码 IMGUI 面板 `TestTransformPotionGUI`（实例化真实卡片预制体，下拉选幻化药→基础生物设 transformItemId 走真实 SetData 链，小卡=Chess/大卡=Avator 高清；场景Spine展示=世界空间并排左基础原形象+右幻化形象，走真实 SetCreatureData 链，与卡片编辑器同机位取景+地平面基准+显隐开关；含 other_data 解析与 spine 资源 Mod 命中状态展示）。详见 test-system skill「卡片编辑器测试」。
- **故事演出测试**：`LauncherTest.StartForStoryTest(long storyId, int saveSlot = 0)`——saveSlot>0 时先读档（`UserDataService.ChangeSlot(saveSlot).Load(false)` → `SetUserData`，献祭测试同范式，全程内存模拟不写回真实存档；0=使用 InitTestData 伪造数据），再 `isTestSimulation=true`，按故事 scene_type 进场景（Base=EnterGameForBaseScene+一次性 World_EnterGameForBaseScene 回调；Fight=内置默认测试战斗数据 `BuildStoryTestFightData()` 进战斗+一次性 UIFightMain_CardCreateAnimEnd 回调(卡片出现动画播完,与真实触发同钩点)；DoomCouncil=StartDoomCouncil(议案1000000001)+`WaitForDoomCouncilThenPlayStory` 轮询就绪），场景就绪后 `StoryHandler.Instance.PlayStory(storyId)`；一次性回调统一走 `RegisterStoryTestPlayCallback(eventName, storyId)`（重复调用先清旧回调）。详见 test-system skill。
- **纯GUI测试面板清理收口**：`LauncherTest.ClearTestGUIs()`——销毁 `TestEffectGUI.Instance`/`TestCreatureCardGUI.Instance`/`TestGameMasterGUI.Instance`/`TestTransformPotionGUI.Instance` 四个手工创建的纯 IMGUI 面板（前两个挂在常驻启动场景，`ClearWorldData`/`UnLoadAllScene` 卸载不到；GM GUI/幻化药面板创建于游戏场景会随场景卸载，仍统一登记）；**所有 `StartFor*` 测试入口首行统一调用**，切换测试模块时防旧面板与其场景残留（新增入口必须照做）。详见 test-system skill。
- **挑战100勇士测试**：`LauncherTest.StartForChallengeHundredTest(long rowId, int saveSlot = 0)`——校验 `FightTypeChallengeHundredInfoCfg.GetItemData(rowId)` 配置行存在后，saveSlot>0 时读档 `SetUserData`（献祭/故事测试同范式）并统一 `isTestSimulation=true`（测试数据不落盘），手搓 `GameWorldInfoRandomBean`（worldId=1、`gameFightType=ChallengeHundred`、调 `SetRandomDataForChallengeHundred` 冻结配置行/道路/3箱奖励，与真实传送门生成共用同一冻结链路）→ `new FightBeanForChallengeHundred(gameWorldInfoRandomData)`（防守方=当前 UserData 魔王+出战阵容）→ `WorldHandler.EnterGameForFightScene(fightData)`；不走真实传送门的出现概率判定。面板入口=GameTestEditor 战斗场景测试的子模式 `FightTestModeEnum.ChallengeHundred`（配置行下拉+存档槽位 0~3）。详见 test-system skill「挑战100勇士测试」。
- **无尽模式测试**：`LauncherTest.StartForInfiniteTest(long worldId, int difficultyLevel, int saveSlot = 0)`——硬校验同难度征服行 `FightTypeConquerInfoCfg.GetItemData(worldId, difficultyLevel)`（无尽复用其怪物构成/数量/时长/场景/道路区间，缺失 LogError 返回）+ 软校验无尽行 `FightTypeInfiniteInfoCfg.GetItemData`（缺失仅 LogWarning 按1降级），saveSlot>0 时读档 `SetUserData`（挑战100勇士测试同范式）并统一 `isTestSimulation=true`（测试数据不落盘），手搓 `GameWorldInfoRandomBean`（worldId、`gameFightType=Infinite`、difficultyLevel、roadNum/roadLength 复用同难度征服行 `GetRandomRoadNum()/GetRandomRoadLength()` 区间随出并冻结，不经 `SetRandomDataForInfinite` 的解锁判定可测未解锁难度）→ `new FightBeanForInfinite(gameWorldInfoRandomData)`（防守方=当前 UserData 魔王+出战阵容）→ `WorldHandler.EnterGameForFightScene(fightData)`。面板入口=GameTestEditor 战斗场景测试的子模式 `FightTestModeEnum.Infinite`（配置行(世界+难度)下拉+存档槽位 0~3）。详见 test-system skill「无尽模式测试」。

### 关键文件

| 文件 | 路径 |
|------|------|
| 启动器基类 | Assets/Scripts/Game/Launcher/BaseLauncher.cs |
| 游戏启动器 | Assets/Scripts/Game/Launcher/LauncherGame.cs |
| 测试启动器 | Assets/Scripts/Game/Launcher/LauncherTest.cs |
| 通用信息 | Assets/Scripts/Common/GameCommonInfo.cs |
| 路径信息 | Assets/Scripts/Common/PathInfo.cs |
| 项目配置 | Assets/Scripts/Common/ProjectConfigInfo.cs |

### 全局配置（ProjectConfigInfo）补充

- **GM 模式开关**：`ProjectConfigInfo.IsGMMode()`——编辑器内恒 true；正式包读 `Resources/GMMode.txt`（0/1，带缓存），由「打包游戏」工具的「开启 GM 模式」选项在打包时写入（机制详见 editor-extension-system skill）。消费入口：`UIBaseMain` 的 F12 打开 GM 测试面板 `UITestBase`。

## 约束

- 初始化顺序遵循依赖关系（底层 Handler 先初始化）
- LauncherTest 仅用于开发测试，不影响正式流程
- 场景名称使用 ScenesEnum 枚举管理
