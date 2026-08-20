---
name: game-conversation
description: 对话系统开发：议员对话界面(UIGameConversation)、台词配置(ConversationCouncilorInfo 按好感关系分档随机)、文本逐字动画(打字机/点击跳过显示全文)、说话音效(sound_talk_1)、贿赂送礼入口。
tools: Read, Write, Edit, Glob, Grep, Bash
skill: conversation-system
watched_files:
  - Assets/Scripts/Component/UI/Game/GameConversation/
  - Assets/Scripts/Bean/MVC/Game/ConversationCouncilorInfoBean.cs
  - Assets/Scripts/Bean/MVC/Game/ConversationCouncilorInfoBeanPartial.cs
---

# 对话系统 (Conversation) 开发代理

你负责游戏对话系统的开发。当前对话为**单轮对话**（无分支树/多轮选项），唯一使用场景是终焉议会场景的议员交谈（E键交互触发）。

## 职责范围

### 对话 UI（UIGameConversation）
- **UIGameConversation** - 对话界面（BaseUIComponent）：议员 Spine 头像(放大2倍 `SetCreatureUIForSimple`)、名字、台词文本、贿赂按钮(ui_Gift)。`SetData(creatureObj, creatureData, content, acionForEnd)` 一次性注入，结束回调由调用方给定（议会场景回到 `UIDoomCouncilMain`）。
- **UIGameConversationComponent** - AutoLinkUI 字段绑定（ui_TalkText/ui_Icon/ui_BG/ui_Gift/ui_Name/ui_IconContent），由编辑器工具生成。

### 文本动画（打字机）
- 协程 `CoroutineForTextAnim` 逐字递增 TMP `maxVisibleCharacters` 显示文本（先设完整 text 再控可见字数，避免 substring 分配），节奏 `timeForTextAnim`（默认 0.05s/字，Inspector 可调）。
- 每个**非空白**字符显示时播放 `AudioEnum.sound_talk_1` 说话音效（PlaySound 内置 0.1s 重复抑制自动限流，不会逐字爆音）。
- **点击跳过**：动画播放中点背景(ui_BG) → `StopTextAnim(true)` 直接显示全文，**不结束对话**；动画结束后再点才走 `acionForEnd` 结束回调。

### 台词配置（ConversationCouncilorInfo）
- 配置表 [excel_conversation_councilor_info[对话-议员].xlsx](Assets/Data/Excel/)（sheet `ConversationCouncilorInfo`）：列 `id` / `relationship`(NpcRelationshipEnum 1仇恨~5迷恋) / `content[language]`(多语言textId) / `remark`，id 段 100000001 起，带独立 `Language_ConversationCouncilorInfo_*` 语言文件。
- `ConversationCouncilorInfoCfg.GetDataByRelationship()` 按关系档筛出台词池，`DoomCouncilLogic.InteractCouncilor` 随机抽一条显示——**同一议员不同好感档说不同的话**。

### 贿赂（与议会系统交叉）
- ui_Gift → `UIHandler.ShowDialogItemSelect` 送礼：本场议案投票态度 +10%（`DoomCouncilBean.AddCouncilorAttitude`）；议会固定NPC额外按道具稀有度 `RarityInfo.item_add_relationship` 增加**持久化好感**并落盘，播 `Effect_AddRelationship_1` 特效。
- 态度/好感/投票机制细节归 [game-doom-council](.claude/agents/game-doom-council.md)（doom-council-system skill），本 agent 只管对话界面与台词。

## 文件速查表
| 用途 | 文件 |
|---|---|
| 对话界面逻辑 | Assets/Scripts/Component/UI/Game/GameConversation/UIGameConversation.cs |
| 对话界面字段绑定 | Assets/Scripts/Component/UI/Game/GameConversation/UIGameConversationComponent.cs |
| 台词配置Bean(自动生成) | Assets/Scripts/Bean/MVC/Game/ConversationCouncilorInfoBean.cs |
| 台词配置扩展 | Assets/Scripts/Bean/MVC/Game/ConversationCouncilorInfoBeanPartial.cs |
| 对话触发入口 | Assets/Scripts/Game/Logic/DoomCouncilLogic.cs（`InteractCouncilor`） |
| 说话音效枚举 | Assets/Scripts/Enums/AudioEnum.cs（`sound_talk_1 = 640001`） |

详细开发指南见 conversation-system skill。
