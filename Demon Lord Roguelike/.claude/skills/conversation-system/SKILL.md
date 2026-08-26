---
name: conversation-system
description: Demon Lord Roguelike 游戏的对话系统开发指南。使用此SKILL当需要创建或修改对话界面、议员交谈、对话台词配置、文本逐字动画(打字机)、说话音效、贿赂送礼交互、无spine NPC静态头像(NpcInfo.icon_res + ui_IconImg)等，包括 UIGameConversation、ConversationCouncilorInfo 配置表(按好感关系分档随机台词)、DoomCouncilLogic.InteractCouncilor 触发入口、文本动画(点击跳过显示全文)、sound_talk_1 音效等。
watched_files:
  - Assets/Scripts/Component/UI/Game/GameConversation/
  - Assets/Scripts/Bean/MVC/Game/ConversationCouncilorInfoBean.cs
  - Assets/Scripts/Bean/MVC/Game/ConversationCouncilorInfoBeanPartial.cs
---

# 对话系统开发指南

## 系统定位

当前对话系统是**轻量单轮对话**：点一下角色 → 弹一句台词（带逐字动画+说话音效）→ 可贿赂或点空白结束。**没有**分支树、多轮选项、剧情节点——如需这些属于新功能扩展（见文末「扩展指引」）。

使用场景有两个：① **终焉议会（DoomCouncil）场景的议员交谈**；② **故事演出系统（story-system）** 的对话步骤（经 `SetDataForStory` 专用入口，见下）。

## 核心文件

| 文件 | 说明 |
|---|---|
| [UIGameConversation.cs](Assets/Scripts/Component/UI/Game/GameConversation/UIGameConversation.cs) | 对话界面逻辑（BaseUIComponent） |
| [UIGameConversationComponent.cs](Assets/Scripts/Component/UI/Game/GameConversation/UIGameConversationComponent.cs) | AutoLinkUI 字段绑定（ui_TalkText/ui_Icon/ui_IconImg/ui_BG/ui_Gift/ui_Name/ui_IconContent） |
| [ConversationCouncilorInfoBeanPartial.cs](Assets/Scripts/Bean/MVC/Game/ConversationCouncilorInfoBeanPartial.cs) | 台词按关系筛选 `GetDataByRelationship` |
| [DoomCouncilLogic.cs](Assets/Scripts/Game/Logic/DoomCouncilLogic.cs) | 对话触发入口 `InteractCouncilor` |

## 对话流程

```
玩家在议会场景 E键点议员（ControlForGameBase）
  → DoomCouncilLogic.InteractCouncilor(targetObj)
    → 取议员 CreatureBean → GetRelationshipForNpc() 当前好感关系档
    → ConversationCouncilorInfoCfg.GetDataByRelationship(关系档) 筛台词池
    → 随机抽一条 → content_language 多语言文本
    → UIHandler.OpenUIAndCloseOther<UIGameConversation>()
    → SetData(creatureObj, councilorData, content, ActionForCouncilorConversationEnd)
    → GameControlHandler.SetBaseControl(false) 停止移动
  → 玩家看台词（逐字动画）→ 点背景结束 → ActionForCouncilorConversationEnd
    → OpenUIAndCloseOther<UIDoomCouncilMain>() 回议会主界面
```

## 故事演出入口（SetDataForStory）

故事演出系统（story-system）的对话步骤走专用入口 `SetDataForStory(GameObject creatureObj, StoryTalkInfoBean talkData, Action actionForEnd)`，按 `talkData.npc_id` 分两种形态；现有调用方（DoomCouncilLogic.InteractCouncilor / LauncherTest.StartForConversationTest）不受影响：

- **npc_id=0（旁白）**：隐藏 ui_Icon/ui_IconImg/ui_Gift、ui_IconContent 置空、名字置空，直接 `SetContent` 起打字机（无头像无名字纯文字）。
- **npc_id≠0**：`new CreatureBean(NpcInfoCfg.GetItemData(npc_id))` 走现有 `SetData` 管线（spine/静态头像/名字复用），再强制隐藏 ui_Gift（故事无贿赂）。

## 头像图片模式（无 spine NPC）

对话头像为双模式，`UIGameConversation.SetCardIcon(creatureData, iconRes)` 按 `GetNpcIconRes()` 判定分支：

- **spine 形象模式（默认）**：`GameUIUtil.SetCreatureUIForSimple(ui_Icon, creatureData, scale:2)` 放大 2 倍；点头像弹生物卡详情气泡（`ui_IconContent.SetData(creatureData, PopupEnum.CreatureCardDetails)`）；显示贿赂按钮 `ui_Gift`。
- **静态头像模式（NpcInfo 配置 `icon_res` 非空）**：`IconHandler.Instance.SetUIIcon(icon_res, ui_IconImg)` 从 UI 图集加载静态图（支持「名,图集」后缀强制指定图集），隐藏 spine 节点 `ui_Icon`；`ui_IconContent.SetData(null,…)` 不弹详情（无生物模型可渲染，且防 UI 复用残留上一个生物的气泡数据）；隐藏 `ui_Gift`（无议会态度/好感逻辑，点了会白扣道具）。

预制体结构：`Icon`（SkeletonGraphic）与 `IconImg`（Image）同为 `IconContent` 子节点、默认均隐藏，运行时按模式互斥 `ShowObj`。

### 无实体 NPC（creature_id=0）

- **创建支撑**：`CreatureBean.AddSkinForBase` 开头守卫——`CreatureInfoCfg.GetItemData(creatureId)==null` 时直接跳过（直查 Cfg 无日志，绕开 `creatureInfo` getter 的 LogError），空 `skin_data`/`equip_*` 列本已安全（SplitForListLong("")→空表、InitRandomEquip poolId=0 早退），故 `new CreatureBean(npcInfo)` 全程不碰 spine。
- **已知脆弱点（禁止踩）**：此类生物 `creatureInfo`/`creatureModel` 为 null，`GetAttribute` **任意属性**都会经 `GetListBuffData`→`creatureInfo.GetCreatureBuffs()` NRE（不止 MPR/MPF/RCD/CMP）——仅供对话展示，禁止进战斗/生物详情/榨汁等一切属性链路。
- **当前唯一实例**：监视之塔 id=10001（新手引导 NPC，excel_npc_info 行：creature_id 空、level/HP/MP/DR/ATK/ASPD/MSPD/attack_search_range=9999、rarity=6(L)、icon_res=`ui_book_1`；名字多语言 id 同 10001）。新增同类 NPC：excel_npc_info 加行（icon_res 填图集 sprite 名）+ excel_language 的 NpcInfo 子表配同名 id 的多语言行（注意该子表 14-19 列是 4-9 列的重复列，需双份同值）→ 编辑器「生成 Entity」+「导出 JSON」。

## 台词配置（ConversationCouncilorInfo）

- **Excel 真实源**：`Assets/Data/Excel/excel_conversation_councilor_info[对话-议员].xlsx`，sheet `ConversationCouncilorInfo`。
- **列**：`id`(long，段 100000001 起) / `relationship`(int，`NpcRelationshipEnum` 枚举值) / `content[language]`(多语言 textId) / `remark`(中文备注，即台词原文参考)。
- **关系档**（`NpcRelationshipEnum`，[NpcEnum.cs](Assets/Scripts/Enums/NpcEnum.cs)）：`Hatred=1 仇恨` / `Neutral=2 冷淡` / `Acquaintance=3 中立` / `FriendShip=4 友好` / `Infatuation=5 迷恋`。
- **多语言**：content 走独立语言文件 `Language_ConversationCouncilorInfo_{cn,en,...}.txt`（TextHandler 按 id 取词）。
- **运行时**：Bean 为自动生成（`ConversationCouncilorInfoBean.cs`，勿手改），扩展写 `ConversationCouncilorInfoBeanPartial.cs`；`GetDataByRelationship()` 遍历全表筛关系档返回列表，调用方自行随机。
- 新增台词：Excel 加行（id 递增、relationship 填关系档、content 填 textId 并配语言）→ 运行配置导出工具生成 JSON。详见 excel-io / data-excel 流程。

## 文本动画（打字机效果）

实现在 `UIGameConversation`（无需 DOTween，项目 DOTweenModuleUI 仅支持 Unity UI Text，不支持 TMP）：

- **机制**：`StartTextAnim(content)` 先 `ui_TalkText.text = content` 设全文，**UniTask 异步推进**（`await GTask.WaitReal(timeForTextAnim, token)` 逐字递增 TMP `maxVisibleCharacters`，**实时逐字、不受 timeScale 影响**——故事演出在战斗场景 timeScale=0 暂停时打字机照常；议会场景 timeScale 恒 1 行为无差异），利用 TMP 原生可见字符控制，无 substring 分配。**不用协程、不用 Update 轮询**（见 CLAUDE.md「异步与定时逻辑规则」）。
- **节奏**：`timeForTextAnim`（public，默认 0.05s/字，Inspector 可调）。
- **取消（框架层 GTask 封装）**：`cancelForTextAnim`（`GTaskCancel`，懒创建一次复用）——`StartTextAnim` 里 `Reset()` 重建令牌（首次 `GTask.NewCancel(gameObject)` 创建并链接销毁令牌，UI 直接销毁也自动取消）、`StopTextAnim` 里 `Cancel()` 收口（跳过/重开/关闭统一），在途推进在 await 点抛 `OperationCanceledException`；推进方法为 `async UniTaskVoid`、调用点 `_ = TextAnimForContent()` 显式丢弃（消除未观察调用警告）——UniTaskVoid 静默 OCE、真异常由 UniTaskScheduler 记录，**无需 try/catch，禁止 async void**（其未捕获 OCE 会进 Console）。`OnDestroy` 重写里 `cancelForTextAnim?.Dispose()` 显式释放。
- **收尾拆分**：`StopTextAnim` = `Cancel()` + `FinishTextAnim(isShowAll)`（显示全文/复位 `isTextAnimPlaying`/截断音效）；自然播完只调 `FinishTextAnim(true)`、**不 Cancel**——取消源留给下次 Start 的 `Reset()` 复用。
- **说话音效**：动画开始时 `AudioHandler.Instance.PlaySoundOnce(AudioEnum.sound_talk_1)` **整条只播一次**（独立音源，非逐字触发）；收尾 `FinishTextAnim`（自然播完/点击跳过，`StopTextAnim` 与 `CloseUI` 亦经此）内 `StopSoundOnce(sound_talk_1)` 立即截断——动画比音效（全长 1.42s）短时在动画结束点直接停掉、无残留长尾巴；动画更长时音效已自然播完、截断为空操作。（旧实现为逐字 `PlayOneShot` 叠加：1.42s 长 clip 每 0.1s 重发叠加浑浊，且动画结束后残留最长 1.42s 无法中断——`PlaySound` 无句柄；单次音效 API 见 audio-system skill「单次音效」）
- **点击跳过**：`OnClickForEnd` 判 `isTextAnimPlaying`——播放中点背景仅 `StopTextAnim(true)`（`maxVisibleCharacters = int.MaxValue` 显示全文）**不结束对话**；动画结束后再点才走 `acionForEnd`。
- **生命周期**：`CloseUI` 重写里调 `StopTextAnim()`——Cancel 取消源终止在途异步推进，并截断说话音效；`StartTextAnim` 开头先 `StopTextAnim()` 重置状态防复用时叠加。
- 音效配置：`sound_talk_1 = 640001`（AudioInfo id），资源 `Assets/LoadResources/Audio/Sound/sound_talk_1.wav`，`audio_type=0` 音效。新增音频流程见 audio-system skill。

## 贿赂（送礼）

- 入口 `ui_Gift` 按钮 → `UIHandler.ShowDialogItemSelect` 选道具（弹窗内嵌 `UIViewItemSelect` 通用选项控件，只传 `actionForSelectGift` 回调故选项只显示「赠送」按钮）→ `ActionForItemSelectGift`：
  1. 背包扣除该道具；
  2. **所有议员**：本场议案投票态度 +10%（`DoomCouncilBean.AddCouncilorAttitude(uuid, 10)`，态度仅存本场）；
  3. **议会固定NPC**（`IsFixedCouncilor()`）：额外按道具稀有度 `RarityInfo.item_add_relationship` 增加**持久化好感**（`UserRelationshipBean.AddRelationship` + `SaveUserData()` 落盘）；
  4. `DoomCouncilLogic.RefreshCouncilorView(uuid)` 刷新场景显示；
  5. 播 `Effect_AddRelationship_1` 粒子特效。
- 态度/好感/投票的完整机制（态度生成、好感区间、地区过滤等）见 **doom-council-system** skill，本 skill 不重复。

## 扩展指引（新增对话场景）

复用 `UIGameConversation` 于新场景时：
1. 调用方 `UIHandler.Instance.OpenUIAndCloseOther<UIGameConversation>()` 打开；
2. `SetData(creatureObj, creatureData, content, 结束回调)` 注入数据——结束回调里打开你的后续 UI；
3. 台词来源若为新的配置表，参照 ConversationCouncilorInfo 建 Excel（`content[language]` 列走多语言）+ 生成 Bean；
4. 文本动画/跳过/音效已内置，无需额外处理；如需关闭动画，直接 `StopTextAnim(true)`。
若需要多轮/分支对话，需在 `UIGameConversation` 上扩展选项按钮与台词节点数据结构（当前均无）。

## 注意事项

- `UIGameConversation` 同时被本 skill 与议会流程使用：贿赂的态度/好感语义归 doom-council-system，本 skill 管界面与台词动画。
- Bean 修改规则：`ConversationCouncilorInfoBean.cs` 带 `AUTO-GENERATED-DO-NOT-EDIT` 标记，扩展只能写 `*BeanPartial.cs`。
- `ui_TalkText` 为 TextMeshProUGUI：动画依赖 TMP `maxVisibleCharacters`，勿改回 Unity UI Text。
- 对话打开时 `SetBaseControl(false)` 停移动但**不停 timeScale**；打字机走 `GTask.WaitReal` 实时推进，即便外部把 timeScale 调 0（如故事演出在战斗场景暂停）也照常逐字。
