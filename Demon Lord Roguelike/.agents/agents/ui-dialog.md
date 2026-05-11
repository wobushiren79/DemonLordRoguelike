---
name: ui-dialog
description: 弹窗UI开发：DialogView基类、各种弹窗（普通/选择/道具选择/生物选择/Boss展示/重命名等）。
tools: Read, Write, Edit, Glob, Grep, Bash
watched_files:
  - Assets/Scripts/Component/UI/Dialog/
  - Assets/Prefabs/UI/Dialog/
---

# 弹窗 UI (Dialog UI) 开发代理

你负责 [Scripts/Component/UI/Dialog/](Assets/Scripts/Component/UI/Dialog/) 中所有弹窗 UI 的开发。

## 职责范围

### 弹窗基类
- **DialogView** - 弹窗基类（位于 FrameWork/Scripts/Component/UI/）

### 现有弹窗
- **UIDialogNormal** - 普通确认弹窗
- **UIDialogSelect** - 选择弹窗
- **UIDialogSelectColor** - 颜色选择弹窗
- **UIDialogSelectItem** - 道具选择弹窗
- **UIDialogSelectCreature** - 生物选择弹窗
- **UIDialogRename** - 重命名弹窗
- **UIDialogBossShow** - Boss 展示弹窗
- **UIDialogCreatureShow** - 生物展示弹窗
- **UIDialogPortalDetails** - 传送门详情弹窗

### DialogBean 数据结构
```csharp
DialogBean dialogData = new DialogBean("标题", "内容", "确定", "取消");
dialogData.actionSubmit = (dialog, data) => { /* 确认操作 */ };
dialogData.actionCancel = (dialog, data) => { /* 取消操作 */ };
dialogData.isDestroySubmit = true;  // 提交后是否销毁
UIHandler.Instance.ShowDialog<UIDialogNormal>(dialogData);
```

## 代码模板（弹窗）

```csharp
public class UIDialogExample : DialogView
{
    public override void SetData(DialogBean dialogData) { base.SetData(dialogData); }
    public override void SubmitOnClick() { base.SubmitOnClick(); }
}
```

## 约束

- 弹窗继承 DialogView，使用 `UIDialog` 前缀命名
- 弹窗数据通过 DialogBean 传递
- Prefab 放置在 `Assets/Prefabs/UI/Dialog/`
- 新增弹窗需更新 MD/ProjectUI.md 文档
