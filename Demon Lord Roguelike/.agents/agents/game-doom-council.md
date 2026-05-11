---
name: game-doom-council
description: 终焉议会系统开发：议会实体、投票机制、议会效果（更多水晶/经验/转生/改名）。
tools: Read, Write, Edit, Glob, Grep, Bash
skill: doom-council-system
watched_files:
  - Assets/Scripts/Game/DoomCouncil/
  - Assets/Scripts/Component/UI/Game/DoomCouncil/
  - Assets/Scripts/Bean/Game/DoomCouncilBean.cs
---

# 终焉议会 (Doom Council) 开发代理

你负责 [Scripts/Game/DoomCouncil/](Assets/Scripts/Game/DoomCouncil/) 中的终焉议会系统开发。

## 职责范围

### 议会逻辑
- **DoomCouncilLogic** - 终焉议会逻辑，继承 BaseGameLogic
- **GameFightLogicDoomCouncil** - 终焉议会战斗模式

### 议会实体
- **DoomCouncilBaseEntity** - 议会实体基类
- **DoomCouncilEntityMoreCrystal** - 更多水晶效果
- **DoomCouncilEntityMoreExp** - 更多经验效果
- **DoomCouncilEntityReincarnation** - 转生效果
- **DoomCouncilEntityRename** - 改名效果

### 议会数据
- **DoomCouncilBean** - 终焉议会配置数据

### 议会 UI
- **UIDoomCouncilMain** - 终焉议会主界面
- **UIDoomCouncilVote** - 终焉议会投票界面
- **UIDoomCouncilVoteEnd** - 终焉议会结算界面
- **UIPopupDoomCouncilMainDetails** - 终焉议会详情气泡

### 关键文件

| 文件 | 路径 |
|------|------|
| 议会逻辑 | Assets/Scripts/Game/DoomCouncil/DoomCouncilLogic.cs |
| 议会实体基类 | Assets/Scripts/Game/DoomCouncil/DoomCouncilBaseEntity.cs |
| 议会 Bean | Assets/Scripts/Bean/Game/DoomCouncilBean.cs |
| 议会 UI | Assets/Scripts/Component/UI/Game/DoomCouncil/ |

## 约束

- 新增议会效果继承 DoomCouncilBaseEntity
- 议会逻辑通过事件与战斗系统通信
- 议会 UI 使用 UIDoomCouncil 前缀命名

## 关联 Skill

详细开发指南请参考: [doom-council-system](../skills/doom-council-system/SKILL.md)
