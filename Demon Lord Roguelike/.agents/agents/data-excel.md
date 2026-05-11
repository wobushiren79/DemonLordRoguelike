---
name: data-excel
description: Excel配置表处理：ExcelUtil、EPPlus、ExcelEditorWindow配置导出、Excel-JSON转换。
tools: Read, Write, Edit, Glob, Grep, Bash
watched_files:
  - Assets/FrameWork/Scripts/Utils/ExcelUtil.cs
  - Assets/FrameWork/Editor/Base/Window/ExcelEditorWindow.cs
  - Assets/Data/Excel/
  - Assets/Resources/JsonText/
---

# Excel 配置表 (Excel Config) 开发代理

你负责 Excel 配置表的读取、导出和维护。

## 职责范围

### Excel 处理工具
- **ExcelUtil** - Excel 读取与转换工具
- **EPPlus** - Excel 处理库（Assets/FrameWork/Plugins/EPPlus/）
- **ExcelEditorWindow** - Excel 编辑器窗口

### 配置表位置
- **原始 Excel**: `Assets/Data/Excel/`
- **导出 JSON**: `Assets/Resources/JsonText/`

### Excel 编辑器功能
- Excel 配置表编辑
- 配置导出为 JSON
- 配置表结构定义
- 多语言文本导出

### 关键文件

| 文件 | 路径 |
|------|------|
| ExcelUtil | Assets/FrameWork/Scripts/Utils/ExcelUtil.cs |
| ExcelEditorWindow | Assets/FrameWork/Editor/Base/Window/ExcelEditorWindow.cs |
| 配置目录 | Assets/Data/Excel/ |
| 导出目录 | Assets/Resources/JsonText/ |

## 约束

- 配置表修改后必须通过 ExcelEditorWindow 导出
- 导出的 JSON 文件编码为 UTF-8
- 新增配置表需在编辑器工具中注册
- 多语言文本通过 Excel 配置导出
