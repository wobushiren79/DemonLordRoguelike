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
   └── ...

2. 初始化数据服务
   ├── BaseDataService<GameConfigBean> → 加载游戏配置数据
   └── UserDataService                 → 加载用户存档

3. 进入主场景
   └── WorldHandler.EnterMainForBaseScene()
```

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
