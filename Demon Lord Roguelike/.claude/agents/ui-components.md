---
name: ui-components
description: UI通用组件开发：ScrollGrid、SelectView、CartogramBarView、ProgressView、DropdownView、RadioButton等框架层UI组件。
tools: Read, Write, Edit, Glob, Grep, Bash
watched_files:
  - Assets/FrameWork/Scripts/Component/UI/
  - Assets/Scripts/Component/UI/Common/
---

# UI 通用组件 (UI Components) 开发代理

你负责框架层 [FrameWork/Scripts/Component/UI/](Assets/FrameWork/Scripts/Component/UI/) 和游戏层 [Scripts/Component/UI/Common/](Assets/Scripts/Component/UI/Common/) 中的通用 UI 组件开发。

## 职责范围

### 框架层 UI 组件
- **ScrollGrid** - 滚动网格（Horizontal/Vertical/Cell/BaseContent）
- **SelectView / SelectColorView** - 选择器 / 颜色选择器
- **CartogramBarView / CartogramBarForItem / CartogramBaseView** - 柱状图组件
- **ProgressView** - 进度条
- **DropdownView** - 下拉框
- **RadioButtonView / RadioGroupView** - 单选按钮 / 组
- **ButtonExtendView** - 扩展按钮
- **LongPressButton** - 长按按钮
- **LineView** - 连线组件
- **UITextLanguageView** - 多语言文本组件
- **DialogView** - 弹窗基类
- **PopupShowView / PopupShowCommonView** - 气泡基类
- **PopupButtonView / PopupButtonCommonView** - 气泡按钮
- **ToastView** - 提示基类
- **MsgView** - 消息视图
- **AudioView / ButtonAudio** - 音频控制组件
- **CursorView** - 光标组件
- **MaskUIView** - UI 遮罩
- **BaseEffectView** - 特效视图基类
- **SecretCode** - 秘钥输入

### 游戏层通用组件 (Common)
- **UIViewItemBackpack / UIViewItemBackpackList** - 背包相关（`FilterItems` 过滤规则：`creatureInfo.CanEquipItem` 或（`GetItemType()==Juice` 且 `!creatureData.IsDemonLord()`）——魔汁例外：选中魔王时魔汁在管理页列表隐藏、普通魔物可见；`creatureData==null` 如 UIDialogSelectItem 显示全部不受影响）
- **UIViewItemEquip** - 装备项
- **UIViewCreatureCardItem / List / Details** - 生物卡片（战斗卡片 `UIViewCreatureCardItemForFight` 含 `ui_AbyssalBlessingContent`(GridLayout)+`ui_AbyssalBlessingItem`(Image 模板)：`RefreshAbyssalBlessing` 遍历 `dicAbyssalBlessingBuffsActivie`，用 `AbyssalBlessingUtil.IsAbyssalBlessingTargetCreature(...,FightDefense)` 取「实际作用于本魔物」的馈赠——含全体防守加成与定向到本魔物的(单体定向类，按锁定 UUID 匹配)，排除敌方/核心/掉落/奖励/复制类；克隆体(增殖)只显示全体馈赠不显示单体定向；按个数动态克隆 Item 图标(缓存池复用)，并监听 `Buff_AbyssalBlessingChange` 刷新。⚠️ 现役 4 族馈赠均不改生物数值，当前卡面不会出现馈赠图标）。**魔王(`CreatureBean.IsDemonLord()`)特殊渲染**：卡片项/详情稀有度统一按 `RarityEnum.L` 显示、隐藏等级(`SetLevel(level,isHide)` / `SetLevelData` 隐藏等级与经验条)、详情隐藏召唤耗魔 `ui_MP`；魔王在魔物管理列表恒置顶第一位（`UIViewCreatureCardList.OrderListCreature` 加最高主键 `OrderByDescending(IsDemonLord)`）。**阵容卡片拆分**：阵容行卡片 `UIViewCreatureCardItemForLineup`（带 IBeginDrag/IDrag/IEndDrag 拖拽换位，仅用于阵容行）与阵容管理列表卡片 `UIViewCreatureCardItemForLineupList`（无拖拽接口，接管 LineupSelect 遮罩，作列表 tempCell）——ScrollGrid 列表 cell 若带拖拽接口会截获 uGUI 拖拽事件（不再冒泡给父级 ScrollRect）导致列表无法拖拽滚动，故列表/阵容行分两类
- **UIViewBasePortalItem** - 传送门项
- **UIViewBaseResearchItem** - 研究项
- **UIViewStoreItem** - 商店道具项（孕育扩展 `UIViewStoreItemPartialGashaponMatchine`：`ui_ContentShow`(PopupButtonCommonView+PopupEnum.Text) 悬浮弹窗列出可抽生物及各稀有度实际命中概率，稀有度文本按 `RarityInfo.ui_board_color` 主色着色；概率来自 `GashaponItemBean.GetRarityProbabilityList()`；生物列表跳过职业未解锁(`creatureInfo.unlock_id`)者，与 `UIGashaponMachine.StartGashaponMachine` 抽取过滤口径一致）
- **UIViewBuffShowItem** - Buff 展示项（悬浮提示按 `TextReplaceEnum` 替换占位：`{Percentage}`=roll后率、`{Time_S}`=trigger_time、`{Value}`=roll后trigger_value + 前置条件参数）
- **UIViewColorShow** - 颜色展示
- **UIViewAbyssalBlessingInfoContent** - 深渊祝福内容
- **UIViewBaseInfoContent** - 基础信息内容
- **UIViewPressCommon** - 通用按键提示（单键）：`SetData(KeyCode/string)` 设键名（`GetKeyDisplayName` 数字键去 Alpha/Keypad 前缀、功能键给短别名）、`HideForNoKey()` 标记无有效键恒隐藏；显隐统一受游戏设置「按键提示显示」(`GameConfigBean.pressKeyTipShow`) 门控（Awake 注册 `GameSetting_PressKeyTipShowChange`，`RefreshShow` 无有效键或开关关闭时隐藏）；`SetMaskCD(remainingCD,totalCD)` 控制子节点 `MaskCD`(Image，Filled/Radial360、黑 60% 遮罩、预制默认隐藏) 冷却中按 剩余/总时长 径向填充、结束隐藏（`isMaskCDShowing` 缓存避免每帧 SetActive）
- **UIViewPressControlForGameBase** - 基地/终焉议会基础操作按键提示组（`UIBaseMain`/`UIDoomCouncilMain` 子视图，子项 W/A/S/D/E/Space 均为 UIViewPressCommon）：W/A/S/D 移动常驻；E 互动键仅 `ControlForGameBase.IsInteractionShowing` 为真时显示（Update 轮询、变更才切换）；Space 突进键按研究 `UnlockEnum.SpaceDash` 解锁显隐（注册 `User_AddUnlock` 实时刷新），冷却中经 `SetMaskCD` 展示 CD 遮罩（剩余读 `ControlForGameBase.DashCdRemain`，总时长实时读 `UserUnlockBean.GetUnlockSpaceDashCD()`）；整体显隐随各子项受「按键提示显示」设置门控

## 约束

- 框架层组件放在 FrameWork/Scripts/Component/UI/
- 游戏层组件放在 Scripts/Component/UI/Common/
- 通用组件继承 BaseUIComponent，使用 `UIView` 前缀命名
- 组件功能保持单一职责，可复用
