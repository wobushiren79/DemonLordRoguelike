---
name: game-conversation
description: 对话系统开发：议员对话界面(UIGameConversation)、台词配置(ConversationCouncilorInfo 按好感关系分档随机)、文本逐字动画(打字机/点击跳过显示全文)、说话音效(sound_talk_1)、贿赂送礼入口、无spine NPC静态头像模式(NpcInfo.icon_res + ui_IconImg)、故事演出对话框布局/目标高亮(OpenUI 自动还原 ui_Content 默认布局+隐藏高亮防残留,SetStoryContentLayout/SetStoryHighlight/HideStoryHighlight API,MaskTarget 用 Shader_UI_GuideHighlight 压暗全屏透亮目标,UV 换算相机按 Canvas 模式取:Overlay 传 null/ScreenSpaceCamera 传 worldCamera,场景目标投影才用 mainCamera)。
tools: Read, Write, Edit, Glob, Grep, Bash
skill: conversation-system
watched_files:
  - Assets/Scripts/Component/UI/Game/GameConversation/
  - Assets/Scripts/Bean/MVC/Game/ConversationCouncilorInfoBean.cs
  - Assets/Scripts/Bean/MVC/Game/ConversationCouncilorInfoBeanPartial.cs
---

# 对话系统 (Conversation) 开发代理

你负责游戏对话系统的开发。当前对话为**单轮对话**（无分支树/多轮选项），使用场景两个：终焉议会场景的议员交谈（E键交互触发）、故事演出系统（story-system）的对话步骤（`SetDataForStory` 专用入口）。

## 职责范围

### 对话 UI（UIGameConversation）
- **UIGameConversation** - 对话界面（BaseUIComponent）：头像（双模式，见下）、名字、台词文本、贿赂按钮(ui_Gift)。`SetData(creatureObj, creatureData, content, acionForEnd)` 一次性注入，结束回调由调用方给定（议会场景回到 `UIDoomCouncilMain`）。
- **故事演出入口 SetDataForStory(creatureObj, StoryTalkInfoBean talkData, actionForEnd)** - 故事演出系统（story-system）专用：npc_id=0（旁白）隐藏 ui_Icon/ui_IconImg/ui_Gift、ui_IconContent 置空、名字置空，直接 `SetContent` 起打字机；npc_id≠0 时 `new CreatureBean(NpcInfoCfg.GetItemData(npc_id))` 走现有 `SetData` 管线（spine/静态头像/名字复用），再强制隐藏 ui_Gift（故事无贿赂）。现有调用方 DoomCouncilLogic.InteractCouncilor / LauncherTest.StartForConversationTest 不受影响。
- **头像双模式（SetCardIcon 分支）** - ①spine形象模式（默认）：`GameUIUtil.SetCreatureUIForSimple(ui_Icon, creatureData, scale:2)` 放大2倍，点头像弹生物卡详情气泡（`ui_IconContent.SetData(creatureData, PopupEnum.CreatureCardDetails)`）、显示贿赂按钮；②静态头像模式（NpcInfo.icon_res 非空，无spine资源的NPC）：`IconHandler.SetUIIcon(icon_res, ui_IconImg)` 从UI图集加载静态图、隐藏spine节点，`ui_IconContent.SetData(null,…)` 不弹详情（防UI复用残留上一个生物数据）、隐藏 ui_Gift（无议会逻辑会白扣道具）。判定入口 `GetNpcIconRes(creatureData)`。当前唯一使用者：监视之塔 id=10001（新手引导NPC，creature_id=0、level/属性9999、rarity=6，icon_res=ui_book_1）。
- **无实体NPC支撑** - `CreatureBean.AddSkinForBase` 开头守卫：`CreatureInfoCfg.GetItemData(creatureId)==null` 直接跳过（无日志），creature_id=0 的NPC可正常 `new CreatureBean(npcInfo)`；注意此类生物 `creatureInfo`/`creatureModel` 为 null，`GetAttribute` 全属性链（经 `GetListBuffData` 解引用 creatureInfo）会 NRE，仅供对话展示、禁止用于战斗/详情场景。
- **UIGameConversationComponent** - AutoLinkUI 字段绑定（ui_TalkText/ui_Icon/ui_IconImg/ui_BG/ui_Gift/ui_Name/ui_IconContent），由编辑器工具生成。ui_Icon=SkeletonGraphic(spine节点)、ui_IconImg=Image(静态头像节点)，两者同为 IconContent 子节点、预制体默认隐藏、运行时按模式互斥显示。

### 文本动画（打字机）
- **UniTask 异步推进**（非协程/非 Update，统一走框架层 GTask 封装）：`TextAnimForContent` 内 `await GTask.WaitReal(timeForTextAnim, token)` 逐字递增 TMP `maxVisibleCharacters`（先设完整 text 再控可见字数，避免 substring 分配；**实时逐字、不受 timeScale 影响**——故事演出在战斗场景 timeScale=0 暂停时打字机照常，议会场景 timeScale 恒 1 无差异），节奏 `timeForTextAnim`（默认 0.05s/字，Inspector 可调）。
- **取消用 `cancelForTextAnim`（GTaskCancel，懒创建复用）**：开始 `Reset()` 重建令牌（首次 `GTask.NewCancel(gameObject)` 链接销毁令牌自动收口）、停止 `Cancel()`；推进方法为 `async UniTaskVoid`、调用点 `_ = TextAnimForContent()` 显式丢弃（消除未观察调用警告；UniTaskVoid 静默 OCE、真异常由 UniTaskScheduler 记录，无需 try/catch）；`CloseUI` 重写调 `StopTextAnim()` 收口，`OnDestroy` 重写 `cancelForTextAnim?.Dispose()` 释放。
- **收尾拆分**：`StopTextAnim` = `Cancel()` + `FinishTextAnim()`；自然播完只调 `FinishTextAnim(true)`（显示全文/复位标记/截断音效），**不动取消源**——取消源留给下次 Start 的 `Reset()` 复用。
- 说话音效 `AudioEnum.sound_talk_1`（全长 1.42s）：动画开始 `PlaySoundOnce` **整条只播一次**（独立音源，非逐字触发）；收尾（`FinishTextAnim`：自然播完/点击跳过/关闭）即 `StopSoundOnce` 立即截断——动画比音效短时在动画结束点直接停掉无残留，动画更长时音效自然播完、截断空操作。
- **点击跳过**：动画播放中点背景(ui_BG) → `StopTextAnim(true)` 直接显示全文，**不结束对话**；动画结束后再点才走 `acionForEnd` 结束回调。

### 台词配置（ConversationCouncilorInfo）
- 配置表 [excel_conversation_councilor_info[对话-议员].xlsx](Assets/Data/Excel/)（sheet `ConversationCouncilorInfo`）：列 `id` / `relationship`(NpcRelationshipEnum 1仇恨~5迷恋) / `content[language]`(多语言textId) / `remark`，id 段 100000001 起，带独立 `Language_ConversationCouncilorInfo_*` 语言文件。
- `ConversationCouncilorInfoCfg.GetDataByRelationship()` 按关系档筛出台词池，`DoomCouncilLogic.InteractCouncilor` 随机抽一条显示——**同一议员不同好感档说不同的话**。

### 贿赂（与议会系统交叉）
- ui_Gift → `UIHandler.ShowDialogItemSelect` 送礼：本场议案投票态度 +10%（`DoomCouncilBean.AddCouncilorAttitude`）；议会固定NPC额外按道具稀有度 `RarityInfo.item_add_relationship` 增加**持久化好感**并落盘，播 `Effect_AddRelationship_1` 特效。弹窗内嵌 `UIViewItemSelect` 通用选项控件，此处只传 `actionForSelectGift` 回调，故选项只显示「赠送」按钮。
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
