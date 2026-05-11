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

## 约束

- 新增战斗模式需继承 GameFightLogic，实现 Pre/Start/Update/End/Clear
- 战斗逻辑通过 EventHandler 与其他系统通信
- GameHandler 是游戏逻辑的统一入口
