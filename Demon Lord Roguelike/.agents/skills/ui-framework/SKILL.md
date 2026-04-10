---
name: ui-framework
description: Demon Lord Roguelike 游戏的UI框架开发指南。使用此SKILL当需要创建或修改UI界面、弹窗(Dialog)、气泡(Popup)、提示(Toast)等，包括UI脚本创建、UI管理、UI事件处理等。
---

# UI框架开发指南

> 📌 **关联文档**: `MD/ProjectUI.md` - 完整的UI清单和框架说明
> 
> ⚠️ **更新提示**: 新增UI后请同时更新本文档和 `MD/ProjectUI.md`

---

## 核心概念

### UI类型体系

```
UITypeEnum
├── UIBase = 0       // 普通UI（游戏主界面、功能界面）
├── Dialog = 1       // 弹窗（确认框、选择框）
├── Toast = 2        // 提示（浮动通知）
├── Popup = 3        // 气泡（悬浮详情）
├── Overlay = 4      // 遮罩（屏幕锁定、加载遮罩）
└── Model3D = 5      // 3D模型展示
```

### UI继承体系

```
BaseUIInit                              // UI初始化基类
│   - AutoLinkUI() 自动绑定UI控件
│   - RegisterButtons() 注册按钮点击
│   - OpenUI() / CloseUI() 打开/关闭
│   - 事件系统：RegisterEvent / TriggerEvent
│
├── BaseUIComponent                     // UI组件基类
│   │   - uiManager 引用
│   │   - uiCloseType 关闭类型 (Hide/Destory)
│   │
│   └── UIViewXXX (各类UI组件)
│
└── BaseUIView                          // UI视图基类
    │   - rectTransform 缓存
    │   - uiSizeOriginal 原始大小
    │
    ├── 普通UI (UIBaseMain, UIFightMain...)
    ├── DialogView                      // 弹窗基类
    ├── PopupShowView                   // 气泡基类
    └── ToastView                       // 提示基类
```

### 命名前缀规范

| 类型 | 前缀 | 示例 |
|------|------|------|
| 普通UI | `UI` | `UIBaseMain`, `UIFightMain` |
| 弹窗 | `UIDialog` | `UIDialogNormal`, `UIDialogSelect` |
| 气泡 | `UIPopup` | `UIPopupItemInfo` |
| 提示 | `UIToast` | `UIToastNormal` |
| 组件 | `UIView` | `UIViewCreatureCardItem` |

---

## 创建新UI

### 方式一：使用编辑器工具（推荐）

1. **打开UI创建工具**
   - 菜单：`Custom/工具弹窗/UI脚本创建`
   - 或点击Toolbar上的`UI脚本创建`按钮

2. **选择脚本类型**
   - `UI 脚本` - 普通UI（继承BaseUIView）
   - `View 脚本` - UI组件（继承BaseUIComponent）
   - `Dialog 脚本` - 弹窗（继承DialogView）
   - `Popup 脚本` - 气泡（继承PopupShowView）
   - `Toast 脚本` - 提示（继承ToastView）
   - `Common 脚本` - 通用组件

3. **设置模块名和路径**
   - 模块名：用于生成子目录
   - 生成路径：脚本保存位置

4. **点击生成**
   - 自动生成脚本文件
   - 自动添加组件到Prefab

### 方式二：手动创建

#### 1. 创建普通UI

```csharp
// Assets/Scripts/Component/UI/Game/XXX/UIExample.cs
using UnityEngine;
using UnityEngine.UI;

public class UIExample : BaseUIView
{
    // UI控件（命名规范：ui_xxx）
    public Button ui_Submit;
    public Text ui_Title;
    public Image ui_Icon;
    
    // 数据
    private ExampleData data;

    public override void Awake()
    {
        base.Awake();
        // 初始化代码
    }

    public override void OpenUI()
    {
        base.OpenUI();
        // 打开时逻辑
    }

    public override void RefreshUI(bool isOpenInit = false)
    {
        base.RefreshUI(isOpenInit);
        // 刷新UI数据
        if (data == null) return;
        ui_Title.text = data.title;
    }

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_Submit)
        {
            OnClickSubmit();
        }
    }

    public void SetData(ExampleData data)
    {
        this.data = data;
        RefreshUI();
    }

    private void OnClickSubmit()
    {
        // 按钮点击逻辑
        UIHandler.Instance.CloseUI<UIExample>();
    }
}
```

#### 2. 创建弹窗

```csharp
// Assets/Scripts/Component/UI/Dialog/UIDialogExample.cs
using UnityEngine;

public class UIDialogExample : DialogView
{
    // 自定义控件
    public InputField ui_Input;
    
    public override void SetData(DialogBean dialogData)
    {
        base.SetData(dialogData);
        // 自定义数据设置
        if (dialogData.customData is string text)
        {
            ui_Input.text = text;
        }
    }

    public override void SubmitOnClick()
    {
        // 获取输入值
        string inputValue = ui_Input.text;
        dialogData.actionSubmit?.Invoke(this, dialogData);
        if (dialogData.isDestroySubmit)
            DestroyDialog();
    }
}
```

#### 3. 创建气泡

```csharp
// Assets/Scripts/Component/UI/Popup/UIPopupExample.cs
using UnityEngine;

public class UIPopupExample : PopupShowView
{
    public Text ui_Content;
    
    public override void SetData(PopupBean popupData)
    {
        base.SetData(popupData);
        ui_Content.text = popupData.content;
    }
}
```

#### 4. 创建View组件

```csharp
// Assets/Scripts/Component/UI/Common/XXX/UIViewExampleItem.cs
using UnityEngine;
using UnityEngine.UI;

public class UIViewExampleItem : BaseUIComponent
{
    public Image ui_Icon;
    public Text ui_Name;
    
    private ItemData itemData;

    public void SetData(ItemData data)
    {
        this.itemData = data;
        ui_Name.text = data.name;
        ui_Icon.sprite = data.icon;
    }
}
```

### 3. Prefab放置规范

| UI类型 | Prefab路径 | 命名规范 |
|--------|-----------|---------|
| 普通UI | `Assets/Prefabs/UI/Game/` | `UIExample` |
| 弹窗 | `Assets/Prefabs/UI/Dialog/` | `UIDialogExample` |
| 气泡 | `Assets/Prefabs/UI/Popup/` | `UIPopupExample` |
| 提示 | `Assets/Prefabs/UI/Toast/` | `UIToastExample` |

---

## UI管理器使用

### UIHandler 核心API

```csharp
// 单例访问
UIHandler.Instance

// ==================== 打开/关闭UI ====================

// 打开UI
UIHandler.Instance.OpenUI<UIExample>();

// 打开UI并设置数据
UIHandler.Instance.OpenUI<UIExample>((ui) =>
{
    ui.SetData(data);
});

// 打开UI并指定层级
UIHandler.Instance.OpenUI<UIExample>(layer: 1);

// 关闭UI
UIHandler.Instance.CloseUI<UIExample>();

// 关闭指定名称的UI
UIHandler.Instance.CloseUI("UIExample");

// 关闭所有UI
UIHandler.Instance.CloseAllUI();

// 打开UI并关闭其他
UIHandler.Instance.OpenUIAndCloseOther<UIExample>();

// ==================== 获取UI ====================

// 获取UI实例
UIExample ui = UIHandler.Instance.GetUI<UIExample>();

// 获取当前打开的UI
BaseUIComponent openUI = UIHandler.Instance.GetOpenUI();

// 获取当前打开UI的名称
string uiName = UIHandler.Instance.GetOpenUIName();

// ==================== 刷新UI ====================

// 刷新指定UI
UIHandler.Instance.RefreshUI<UIExample>();

// 刷新当前打开的UI
UIHandler.Instance.RefreshUI();

// 刷新所有UI
UIHandler.Instance.RefreshAllUI();

// ==================== 弹窗 ====================

// 显示普通弹窗
DialogBean dialogData = new DialogBean("标题", "内容", "确定", "取消");
dialogData.actionSubmit = (dialog, data) => { /* 确认操作 */ };
UIHandler.Instance.ShowDialog<UIDialogNormal>(dialogData);

// 显示选择弹窗
DialogBean selectData = new DialogBean(DialogEnum.Select, "选择", "选项1|选项2|选项3");
selectData.actionSubmit = (dialog, data) => 
{
    int selectedIndex = dialogData.selectIndex;
};
UIHandler.Instance.ShowDialog<UIDialogSelect>(selectData);

// 关闭所有弹窗
UIHandler.Instance.manager.CloseAllDialog();

// ==================== Toast提示 ====================

// 普通提示
UIHandler.Instance.ToastHint<UIToastNormal>("保存成功！");

// 带图标的提示
UIHandler.Instance.ToastHint<UIToastNormal>(iconSprite, "获得道具");

// 指定显示时间
UIHandler.Instance.ToastHint<UIToastNormal>("提示内容", 3f);

// ==================== 气泡Popup ====================

// 显示气泡
PopupBean popupData = new PopupBean(PopupEnum.ItemInfo, targetTransform);
UIHandler.Instance.ShowPopup<UIPopupItemInfo>(popupData);

// 隐藏气泡
UIHandler.Instance.HidePopup(PopupEnum.ItemInfo);

// ==================== 屏幕锁定 ====================

// 锁定屏幕（禁止点击）
UIHandler.Instance.ShowScreenLock();

// 解锁屏幕
UIHandler.Instance.HideScreenLock();
```

---

## UI生命周期与事件

### 生命周期方法

```csharp
public class UIExample : BaseUIView
{
    public override void Awake()
    {
        base.Awake();
        // 初始化：自动绑定UI控件、注册按钮
    }

    public override void OnEnable()
    {
        base.OnEnable();
        // UI启用时：注册输入事件
    }

    public override void OpenUI()
    {
        base.OpenUI();
        // 打开UI：显示、刷新、播放动画
    }

    public override void RefreshUI(bool isOpenInit = false)
    {
        base.RefreshUI(isOpenInit);
        // 刷新UI数据
    }

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        // 处理按钮点击
    }

    public override void OnInputActionForStarted(InputActionUIEnum inputType, CallbackContext callback)
    {
        base.OnInputActionForStarted(inputType, callback);
        // 处理快捷键输入
    }

    public override void OnDisable()
    {
        base.OnDisable();
        // UI禁用时
    }

    public override void CloseUI()
    {
        base.CloseUI();
        // 关闭UI：隐藏、注销事件
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        // 销毁时：清理资源
    }
}
```

### UI事件系统

```csharp
// 注册事件
RegisterEvent(EventsInfo.XXX, OnEventTriggered);
RegisterEvent<int>(EventsInfo.XXX, OnEventWithParam);
RegisterEvent<int, string>(EventsInfo.XXX, OnEventWithParams);

// 触发事件
TriggerEvent(EventsInfo.XXX);
TriggerEvent(EventsInfo.XXX, data);

// 注销事件（通常不需要手动调用，CloseUI时自动注销）
UnRegisterAllEvent();
```

---

## 常用代码模板

### 带列表的UI

```csharp
public class UIExampleList : BaseUIView
{
    public ScrollGridVertical ui_List;
    public GameObject pf_Item;
    
    private List<Data> dataList;

    public override void OpenUI()
    {
        base.OpenUI();
        InitList();
    }

    private void InitList()
    {
        ui_List.SetData(dataList.Count, (index, objItem) =>
        {
            UIViewExampleItem item = objItem.GetComponent<UIViewExampleItem>();
            item.SetData(dataList[index]);
        });
    }
}
```

### 带Tab切换的UI

```csharp
public class UIExampleTab : BaseUIView
{
    public Button ui_Tab1;
    public Button ui_Tab2;
    public GameObject ui_Content1;
    public GameObject ui_Content2;
    
    private int currentTab = 0;

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_Tab1) SwitchTab(0);
        if (viewButton == ui_Tab2) SwitchTab(1);
    }

    private void SwitchTab(int tabIndex)
    {
        currentTab = tabIndex;
        ui_Content1.SetActive(tabIndex == 0);
        ui_Content2.SetActive(tabIndex == 1);
    }
}
```

### 带确认关闭的UI

```csharp
public class UIExample : BaseUIView
{
    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton.name == "ui_Close")
        {
            ShowCloseConfirm();
        }
    }

    private void ShowCloseConfirm()
    {
        DialogBean dialog = new DialogBean("确认", "是否保存更改？", "保存", "不保存");
        dialog.actionSubmit = (d, data) => 
        {
            SaveAndClose();
        };
        dialog.actionCancel = (d, data) =>
        {
            UIHandler.Instance.CloseUI<UIExample>();
        };
        UIHandler.Instance.ShowDialog<UIDialogNormal>(dialog);
    }
}
```

### 多语言文本绑定

```csharp
public class UIExample : BaseUIView
{
    // 使用UITextLanguageView组件绑定文本key
    public UITextLanguageView ui_TitleText;
    
    public override void Awake()
    {
        base.Awake();
        // 或在代码中设置
        ui_TitleText.SetTextKey("UI_Example_Title");
    }
}
```

---

## 更新UI框架文档

### 何时更新

新增UI后必须同时更新：
1. **SKILL.md** - 添加代码模板和使用示例（如需要）
2. **MD/ProjectUI.md** - 添加UI清单条目

### 更新清单

- [ ] 在对应UI类型分类下添加新UI信息
- [ ] 更新目录结构
- [ ] 更新最后更新时间
- [ ] 更新更新记录表

---

## 文件位置速查

| 功能 | 文件路径 |
|------|----------|
| UI基类 | `Assets/FrameWork/Scripts/Base/BaseUIInit.cs` |
| UI视图基类 | `Assets/FrameWork/Scripts/Base/BaseUIView.cs` |
| UI组件基类 | `Assets/FrameWork/Scripts/Base/BaseUIComponent.cs` |
| 弹窗基类 | `Assets/FrameWork/Scripts/Component/UI/DialogView.cs` |
| 气泡基类 | `Assets/FrameWork/Scripts/Component/UI/PopupShowView.cs` |
| 提示基类 | `Assets/FrameWork/Scripts/Component/UI/ToastView.cs` |
| UI管理器 | `Assets/FrameWork/Scripts/Component/Manager/UIManager.cs` |
| UI处理器 | `Assets/FrameWork/Scripts/Component/Handler/UIHandler.cs` |
| UI类型枚举 | `Assets/FrameWork/Scripts/Enums/BaseGameEnum.cs` |
| UI编辑器 | `Assets/FrameWork/Editor/Base/Window/BaseUICreateWindow.cs` |
| UI脚本模板 | `Assets/FrameWork/Editor/ScriptsTemplates/` |
| 游戏UI目录 | `Assets/Scripts/Component/UI/Game/` |
| 弹窗目录 | `Assets/Scripts/Component/UI/Dialog/` |
| 气泡目录 | `Assets/Scripts/Component/UI/Popup/` |
| 提示目录 | `Assets/Scripts/Component/UI/Toast/` |
| 通用组件目录 | `Assets/Scripts/Component/UI/Common/` |
| Prefab目录 | `Assets/Prefabs/UI/` |
| 完整UI文档 | `MD/ProjectUI.md` |

---

## 相关事件

```csharp
// UI相关事件
EventsInfo.UI_Open                        // UI打开
EventsInfo.UI_Close                       // UI关闭
EventsInfo.UI_Refresh                     // UI刷新

// 弹窗相关
EventsInfo.Dialog_Show                    // 显示弹窗
EventsInfo.Dialog_Close                   // 关闭弹窗

// 背包相关
EventsInfo.Backpack_Item_Change           // 背包道具变化
EventsInfo.UIViewItemBackpack_OnClickSelect // 背包道具点击

// 生物相关
EventsInfo.Creature_Select                // 选择生物
EventsInfo.Creature_Change                // 生物变化

// 语言切换
EventsInfo.Language_Change                // 语言切换
```

---

## 更新记录

| 日期 | 更新内容 | 更新人 |
|------|----------|--------|
| 2026-04-10 | 创建UI框架SKILL | - |

---

*SKILL结束 - 完整UI清单请参考 [MD/ProjectUI.md](../../MD/ProjectUI.md)*
