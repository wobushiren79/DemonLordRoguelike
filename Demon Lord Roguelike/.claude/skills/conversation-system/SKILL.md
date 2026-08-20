---
name: conversation-system
description: Demon Lord Roguelike 游戏的对话系统开发指南。使用此SKILL当需要创建或修改对话界面、议员交谈、对话台词配置、文本逐字动画(打字机)、说话音效、贿赂送礼交互等，包括 UIGameConversation、ConversationCouncilorInfo 配置表(按好感关系分档随机台词)、DoomCouncilLogic.InteractCouncilor 触发入口、文本动画(点击跳过显示全文)、sound_talk_1 音效等。
watched_files:
  - Assets/Scripts/Component/UI/Game/GameConversation/
  - Assets/Scripts/Bean/MVC/Game/ConversationCouncilorInfoBean.cs
  - Assets/Scripts/Bean/MVC/Game/ConversationCouncilorInfoBeanPartial.cs
---

# 对话系统开发指南

## 系统定位

当前对话系统是**轻量单轮对话**：点一下角色 → 弹一句台词（带逐字动画+说话音效）→ 可贿赂或点空白结束。**没有**分支树、多轮选项、剧情节点——如需这些属于新功能扩展（见文末「扩展指引」）。

唯一使用场景：**终焉议会（DoomCouncil）场景的议员交谈**。

## 核心文件

| 文件 | 说明 |
|---|---|
| [UIGameConversation.cs](Assets/Scripts/Component/UI/Game/GameConversation/UIGameConversation.cs) | 对话界面逻辑（BaseUIComponent） |
| [UIGameConversationComponent.cs](Assets/Scripts/Component/UI/Game/GameConversation/UIGameConversationComponent.cs) | AutoLinkUI 字段绑定（ui_TalkText/ui_Icon/ui_BG/ui_Gift/ui_Name/ui_IconContent） |
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

## 台词配置（ConversationCouncilorInfo）

- **Excel 真实源**：`Assets/Data/Excel/excel_conversation_councilor_info[对话-议员].xlsx`，sheet `ConversationCouncilorInfo`。
- **列**：`id`(long，段 100000001 起) / `relationship`(int，`NpcRelationshipEnum` 枚举值) / `content[language]`(多语言 textId) / `remark`(中文备注，即台词原文参考)。
- **关系档**（`NpcRelationshipEnum`，[NpcEnum.cs](Assets/Scripts/Enums/NpcEnum.cs)）：`Hatred=1 仇恨` / `Neutral=2 冷淡` / `Acquaintance=3 中立` / `FriendShip=4 友好` / `Infatuation=5 迷恋`。
- **多语言**：content 走独立语言文件 `Language_ConversationCouncilorInfo_{cn,en,...}.txt`（TextHandler 按 id 取词）。
- **运行时**：Bean 为自动生成（`ConversationCouncilorInfoBean.cs`，勿手改），扩展写 `ConversationCouncilorInfoBeanPartial.cs`；`GetDataByRelationship()` 遍历全表筛关系档返回列表，调用方自行随机。
- 新增台词：Excel 加行（id 递增、relationship 填关系档、content 填 textId 并配语言）→ 运行配置导出工具生成 JSON。详见 excel-io / data-excel 流程。

## 文本动画（打字机效果）

实现在 `UIGameConversation`（无需 DOTween，项目 DOTweenModuleUI 仅支持 Unity UI Text，不支持 TMP）：

- **机制**：`StartTextAnim(content)` 先 `ui_TalkText.text = content` 设全文，协程 `CoroutineForTextAnim` 逐字递增 TMP `maxVisibleCharacters`（0 → 全文长度），利用 TMP 原生可见字符控制，无 substring 分配。
- **节奏**：`timeForTextAnim`（public，默认 0.05s/字，Inspector 可调）。
- **说话音效**：每显示一个**非空白**字符播放一次 `AudioHandler.Instance.PlaySound(AudioEnum.sound_talk_1)`；`PlaySound` 内置 0.1s 同音效重复抑制，自动限流（约每 2 字一音），不会爆音。
- **点击跳过**：`OnClickForEnd` 判 `isTextAnimPlaying`——播放中点背景仅 `StopTextAnim(true)`（`maxVisibleCharacters = int.MaxValue` 显示全文）**不结束对话**；动画结束后再点才走 `acionForEnd`。
- **生命周期**：`BaseUIComponent.CloseUI()` 会 `StopAllCoroutines()`，协程随 UI 关闭自动停止；`StartTextAnim` 开头先 `StopTextAnim()` 防复用时协程叠加。
- 音效配置：`sound_talk_1 = 640001`（AudioInfo id），资源 `Assets/LoadResources/Audio/Sound/sound_talk_1.wav`，`audio_type=0` 音效。新增音频流程见 audio-system skill。

## 贿赂（送礼）

- 入口 `ui_Gift` 按钮 → `UIHandler.ShowDialogItemSelect` 选道具 → `ActionForItemSelectGift`：
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
- 对话打开时 `SetBaseControl(false)` 停移动但**不停 timeScale**，协程用 `WaitForSeconds` 正常计时。
