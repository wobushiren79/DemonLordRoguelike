---
name: system-localization
description: 多语言系统开发：TextHandler/TextManager、UITextLanguageView、语言切换、语言资源加载。
tools: Read, Write, Edit, Glob, Grep, Bash
watched_files:
  - Assets/FrameWork/Scripts/Component/Handler/TextHandler.cs
  - Assets/FrameWork/Scripts/Component/Manager/TextManager.cs
  - Assets/FrameWork/Scripts/Component/UI/UITextLanguageView.cs
  - Assets/Resources/JsonText/
---

# 多语言系统 (Localization System) 开发代理

你负责多语言系统的开发。

## 职责范围

### 多语言管理
- **TextHandler** - 多语言逻辑处理，语言切换接口 [FrameWork/Scripts/Component/Handler/TextHandler.cs](Assets/FrameWork/Scripts/Component/Handler/TextHandler.cs)
- **TextManager** - 多语言文本管理器，加载语言 JSON 资源 [FrameWork/Scripts/Component/Manager/TextManager.cs](Assets/FrameWork/Scripts/Component/Manager/TextManager.cs)

### 多语言 UI 组件
- **UITextLanguageView** - 多语言文本组件，自动根据 key 切换语言 [FrameWork/Scripts/Component/UI/UITextLanguageView.cs](Assets/FrameWork/Scripts/Component/UI/UITextLanguageView.cs)

### 多语言数据
- **LanguageBean / LanguageBeanPartial** - 多语言数据模型
- **UITextBean / UITextBeanPartial** - UI 文本数据模型

### 语言资源
- 存放路径：`Assets/Resources/JsonText/Language_UIText_*.txt`

### 数据流
```
TextManager 加载 Language_UIText_*.txt
    → LanguageBean / UITextBean 解析
    → UITextLanguageView 绑定 key
    → 语言切换时自动更新所有 UITextLanguageView
    → 触发 EventsInfo 语言变更事件
    → 各 UI 模块刷新显示文本
```

## 约束

- 多语言 key 统一放在 Excel 中管理，通过 ExcelEditorWindow 导出
- 所有文本显示必须使用 UITextLanguageView 或通过 TextHandler 获取
- 新增文本 key 需在 Excel 配置中添加
- 语言切换触发全局事件，所有 UI 需响应刷新
