---
name: game-doom-council
description: 终焉议会系统开发：议会实体、投票机制、议会效果（更多水晶/经验/转生/改名）。
tools: Read, Write, Edit, Glob, Grep, Bash
skill: doom-council-system
watched_files:
  - Assets/Scripts/Game/DoomCouncil/
  - Assets/Scripts/Game/Logic/DoomCouncilLogic.cs
  - Assets/Scripts/Component/UI/Game/DoomCouncil/
  - Assets/Scripts/Component/Game/Scene/ScenePrefabForDoomCouncil.cs
  - Assets/Scripts/Component/UI/Game/GameConversation/UIGameConversation.cs
  - Assets/Scripts/Bean/Game/DoomCouncilBean.cs
  - Assets/Scripts/Bean/Game/UserRelationshipBean.cs
  - Assets/Scripts/Bean/MVC/Game/NpcInfoBeanPartial.cs
  - Assets/Scripts/Bean/MVC/Game/DoomCouncilInfoBeanPartial.cs
  - Assets/Scripts/Enums/NpcEnum.cs
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

### 议员与投票态度系统（核心机制）
- **NPC 类型** `NpcTypeEnum`：`Councilor=2` 议会固定NPC（固定装备/样貌 + 独立持久化好感），`CouncilorRandom=3` 议会随机NPC（随机外貌、每场临时生成）。
- **议员生成**（`DoomCouncilLogic.GenerateCouncilors`）：议会人数在议案 `DoomCouncilInfo.council_num`("min,max") 区间随机；每席随机一种生物(CreatureInfo id 1001-7004) + 按权重随机评级(1~5: 50/30/15/10/5 归一化, `NpcInfoCfg.GetRandomCouncilorNpc`)；整场 10% 概率出现 1 名固定NPC(`GetRandomFixedCouncilorNpc`)。每生物×评级各一条 `npc_type=3` 的 NpcInfo 行(共30×5=150)。
- **投票态度**（存于 `DoomCouncilBean.dicCouncilorAttitude`，Key=议员UUID，Value 0~100=投赞成概率；态度只与本场议案绑定，不放 CreatureBean、不入存档）：`GenerateCouncilorAttitudes` 按议案 `success_rate` 字面生成——低态度组人数=总数×通过率→{0,25}，其余→{75,100}，再随机取10%覆盖为50。固定NPC再叠加好感修正 `(关系类型-3)×50`(仇恨-100/冷淡-50/中立0/友好+50/迷恋+100)。
- **投票**（`StartVote`）：每名议员 `Random(0,100) < attitude ? 赞成 : 反对`，票数按评级 `DoomCouncilRatingsInfo.vote`（已移除旧的「随机 vs success_rate + 30%睡觉」逻辑）。
- **贿赂**（`UIGameConversation.ActionForItemSelectGift`）：送礼一次态度+10%；固定NPC额外加好感(按道具稀有度 `RarityInfo.item_add_relationship`)并持久化到 `UserRelationshipBean`，随即 `RefreshCouncilorView`。
- **场景显示**（`ScenePrefabForDoomCouncil`）：议员预制下 `Success` SpriteRenderer 用颜色表态度(0红/50白/100绿 `GetAttitudeColor`)；`Relationship` SpriteRenderer 显示固定NPC好感图标。固定NPC：Relationship.x=-0.1、Success.x=0.1；随机NPC：隐藏 Relationship、Success.x=0。
- **好感持久化**：`UserRelationshipBean`（按 npcId 存好感，默认0=仇恨）作为独立存档 `UserRelationship_{slot}`，经 `UserDataService` Load/Save/Delete 注入落盘。

### 议会数据
- **DoomCouncilBean** - 终焉议会配置数据

### 议会 UI
- **UIDoomCouncilMain** - 议会场景主界面（议会进行中替换 UIBaseMain；通过 `ui_SuccessText` 显示「当前议案通过率」，文案 UIText id `53014`，`MathUtil.GetPercentage(success_rate,2)` + `string.Format`）
- **UIDoomCouncilBill** - 终焉议会议案选择界面
- **UIDoomCouncilVote** - 终焉议会投票界面
- **UIDoomCouncilVoteEnd** - 终焉议会结算界面
- **UIPopupDoomCouncilBillDetails** - 终焉议会详情气泡

### 关键文件

| 文件 | 路径 |
|------|------|
| 议会逻辑(议员生成/态度/投票) | Assets/Scripts/Game/Logic/DoomCouncilLogic.cs |
| 议会实体基类 | Assets/Scripts/Game/DoomCouncil/DoomCouncilBaseEntity.cs |
| 议会 Bean | Assets/Scripts/Bean/Game/DoomCouncilBean.cs |
| 议会场景预制(议员/态度色/好感图标) | Assets/Scripts/Component/Game/Scene/ScenePrefabForDoomCouncil.cs |
| 议员态度/类型辅助 | Assets/Scripts/Bean/Game/CreatureBeanPartial.cs |
| 随机/固定议员抽取 | Assets/Scripts/Bean/MVC/Game/NpcInfoBeanPartial.cs |
| 议会人数解析(council_num) | Assets/Scripts/Bean/MVC/Game/DoomCouncilInfoBeanPartial.cs |
| 固定NPC好感存档 | Assets/Scripts/Bean/Game/UserRelationshipBean.cs |
| 贿赂(态度/好感) | Assets/Scripts/Component/UI/Game/GameConversation/UIGameConversation.cs |
| NPC枚举(类型/关系/投票) | Assets/Scripts/Enums/NpcEnum.cs |
| 议会主界面 | Assets/Scripts/Component/UI/Game/DoomCouncil/UIDoomCouncilMain.cs |
| 议会 UI | Assets/Scripts/Component/UI/Game/DoomCouncil/ |

## 约束

- 新增议会效果继承 DoomCouncilBaseEntity
- 议会逻辑通过事件与战斗系统通信
- 议会 UI 使用 UIDoomCouncil 前缀命名

## 关联 Skill

详细开发指南请参考: [doom-council-system](../skills/doom-council-system/SKILL.md)
