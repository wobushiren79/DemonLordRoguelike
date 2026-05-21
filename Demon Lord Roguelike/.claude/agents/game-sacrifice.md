---
name: game-sacrifice
description: 生物献祭系统开发：CreatureSacrificeLogic 献祭逻辑、献祭数据、献祭UI。
tools: Read, Write, Edit, Glob, Grep, Bash
watched_files:
  - Assets/Scripts/Game/Logic/CreatureSacrificeLogic.cs
  - Assets/Scripts/Bean/Game/CreatureSacrificeBean.cs
  - Assets/Scripts/Component/UI/Game/CreatureSacrifice/
---

# 献祭系统 (Sacrifice System) 开发代理

你负责 [Scripts/](Assets/Scripts/) 中与生物献祭相关的代码开发。

## 职责范围

### 献祭逻辑
- **CreatureSacrificeLogic** - 生物献祭逻辑，继承 BaseGameLogic

### 献祭数据
- **CreatureSacrificeBean** - 生物献祭数据

### 献祭 UI
- **UICreatureSacrifice** - 生物献祭界面

### 关键文件

| 文件 | 路径 |
|------|------|
| 献祭逻辑 | Assets/Scripts/Game/Logic/CreatureSacrificeLogic.cs |
| 献祭数据 | Assets/Scripts/Bean/Game/CreatureSacrificeBean.cs |
| 献祭 UI | Assets/Scripts/Component/UI/Game/CreatureSacrifice/ |

## 约束

- 献祭逻辑通过事件通知其他系统
- 献祭消耗和奖励数据需正确校验
- 献祭 UI 继承 BaseUIView
