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
- **UIViewCreatureCardItem / List / Details** - 生物卡片
- **UIViewBasePortalItem** - 传送门项
- **UIViewBaseResearchItem** - 研究项
- **UIViewStoreItem** - 商店道具项
- **UIViewBuffShowItem** - Buff 展示项
- **UIViewColorShow** - 颜色展示
- **UIViewAbyssalBlessingInfoContent** - 深渊祝福内容
- **UIViewBaseInfoContent** - 基础信息内容

## 约束

- 框架层组件放在 FrameWork/Scripts/Component/UI/
- 游戏层组件放在 Scripts/Component/UI/Common/
- 通用组件继承 BaseUIComponent，使用 `UIView` 前缀命名
- 组件功能保持单一职责，可复用
