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

- **故事演出测试**：`LauncherTest.StartForStoryTest(long storyId, int saveSlot = 0)`——saveSlot>0 时先读档（`UserDataService.ChangeSlot(saveSlot).Load(false)` → `SetUserData`，献祭测试同范式，全程内存模拟不写回真实存档；0=使用 InitTestData 伪造数据），再 `isTestSimulation=true`，按故事 scene_type 进场景（Base=EnterGameForBaseScene+一次性 World_EnterGameForBaseScene 回调；Fight=内置默认测试战斗数据 `BuildStoryTestFightData()` 进战斗+一次性 UIFightMain_CardCreateAnimEnd 回调(卡片出现动画播完,与真实触发同钩点)；DoomCouncil=StartDoomCouncil(议案1000000001)+`WaitForDoomCouncilThenPlayStory` 轮询就绪），场景就绪后 `StoryHandler.Instance.PlayStory(storyId)`；一次性回调统一走 `RegisterStoryTestPlayCallback(eventName, storyId)`（重复调用先清旧回调）。详见 test-system skill。

### 关键文件

| 文件 | 路径 |
|------|------|
| 启动器基类 | Assets/Scripts/Game/Launcher/BaseLauncher.cs |
| 游戏启动器 | Assets/Scripts/Game/Launcher/LauncherGame.cs |
| 测试启动器 | Assets/Scripts/Game/Launcher/LauncherTest.cs |
| 通用信息 | Assets/Scripts/Common/GameCommonInfo.cs |
| 路径信息 | Assets/Scripts/Common/PathInfo.cs |
| 项目配置 | Assets/Scripts/Common/ProjectConfigInfo.cs |

## 约束

- 初始化顺序遵循依赖关系（底层 Handler 先初始化）
- LauncherTest 仅用于开发测试，不影响正式流程
- 场景名称使用 ScenesEnum 枚举管理
