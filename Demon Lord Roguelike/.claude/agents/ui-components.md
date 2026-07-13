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
- **UIViewItemBackpack / UIViewItemBackpackList** - 背包相关
- **UIViewItemEquip** - 装备项
- **UIViewCreatureCardItem / List / Details** - 生物卡片（战斗卡片 `UIViewCreatureCardItemForFight` 含 `ui_AbyssalBlessingContent`(GridLayout)+`ui_AbyssalBlessingItem`(Image 模板)：`RefreshAbyssalBlessing` 遍历 `dicAbyssalBlessingBuffsActivie`，用 `AbyssalBlessingUtil.IsAbyssalBlessingTargetCreature(...,FightDefense)` 取「实际作用于本魔物」的馈赠——含全体防守加成与定向到本魔物的(大力出奇迹等，按锁定 UUID 匹配)，排除敌方/核心/掉落/奖励/复制类；克隆体(增殖)只显示全体馈赠不显示单体定向；按个数动态克隆 Item 图标(缓存池复用)，并监听 `Buff_AbyssalBlessingChange` 刷新）。**魔王(`CreatureBean.IsDemonLord()`)特殊渲染**：卡片项/详情稀有度统一按 `RarityEnum.L` 显示、隐藏等级(`SetLevel(level,isHide)` / `SetLevelData` 隐藏等级与经验条)、详情隐藏召唤耗魔 `ui_MP`；魔王在魔物管理列表恒置顶第一位（`UIViewCreatureCardList.OrderListCreature` 加最高主键 `OrderByDescending(IsDemonLord)`）
- **UIViewBasePortalItem** - 传送门项
- **UIViewBaseResearchItem** - 研究项
- **UIViewStoreItem** - 商店道具项（孕育扩展 `UIViewStoreItemPartialGashaponMatchine`：`ui_ContentShow`(PopupButtonCommonView+PopupEnum.Text) 悬浮弹窗列出可抽生物及各稀有度实际命中概率，稀有度文本按 `RarityInfo.ui_board_color` 主色着色；概率来自 `GashaponItemBean.GetRarityProbabilityList()`；生物列表跳过职业未解锁(`creatureInfo.unlock_id`)者，与 `UIGashaponMachine.StartGashaponMachine` 抽取过滤口径一致）
- **UIViewBuffShowItem** - Buff 展示项
- **UIViewColorShow** - 颜色展示
- **UIViewAbyssalBlessingInfoContent** - 深渊祝福内容
- **UIViewBaseInfoContent** - 基础信息内容

## 约束

- 框架层组件放在 FrameWork/Scripts/Component/UI/
- 游戏层组件放在 Scripts/Component/UI/Common/
- 通用组件继承 BaseUIComponent，使用 `UIView` 前缀命名
- 组件功能保持单一职责，可复用
