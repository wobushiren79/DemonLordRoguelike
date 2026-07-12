---
name: ui-framework
description: Demon Lord Roguelike 游戏的UI框架开发指南。使用此SKILL当需要创建或修改UI界面、弹窗(Dialog)、气泡(Popup)、提示(Toast)等，包括UI脚本创建、UI管理、UI事件处理等。
watched_files:
  - Assets/FrameWork/Scripts/Base/BaseUIInit.cs
  - Assets/FrameWork/Scripts/Base/BaseUIView.cs
  - Assets/FrameWork/Scripts/Base/BaseUIComponent.cs
  - Assets/FrameWork/Scripts/Component/UI/DialogView.cs
  - Assets/FrameWork/Scripts/Component/UI/PopupShowView.cs
  - Assets/FrameWork/Scripts/Component/UI/ToastView.cs
  - Assets/FrameWork/Scripts/Component/Manager/UIManager.cs
  - Assets/FrameWork/Scripts/Component/Handler/UIHandler.cs
  - Assets/FrameWork/Scripts/Enums/BaseGameEnum.cs
  - Assets/FrameWork/Editor/Base/Window/BaseUICreateWindow.cs
  - Assets/FrameWork/Editor/ScriptsTemplates/
  - Assets/Scripts/Component/UI/Game/
  - Assets/Scripts/Component/UI/Dialog/
  - Assets/Scripts/Component/UI/Popup/
  - Assets/Scripts/Component/UI/Toast/
  - Assets/Scripts/Component/UI/Common/
  - Assets/Prefabs/UI/
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

## ⚠️ 通用控件优先原则（强制约束）

**凡是有通用解决方案的 UI 需求，必须优先调用 UIHandler 上的现成方法，禁止在业务 UI 中自己造轮子。**

业务侧每次写"动画期间挂个 CanvasGroup 拦点击"、"自己拼一个确认框"、"自加 Toast Text"这类代码前，**先翻 UIHandler.cs 找现成方法**。

### 常见通用需求 → 框架方法对照表

| 业务需求 | ❌ 不要这样做 | ✅ 必须这样做 |
|---------|------------|------------|
| 动画 / 异步流程中阻挡点击 | 自挂 `CanvasGroup` + `interactable=false`、自加全屏 `Image(raycastTarget)`、自维护 isAnimating + 关闭按钮 interactable | `UIHandler.Instance.ShowScreenLock()` / `HideScreenLock()` |
| 普通提示信息 | 自写飘字、自做 Tween 隐藏 | `UIHandler.Instance.ToastHint<UIToastNormal>(content)` |
| 确认 / 选择 / 输入弹窗 | 自拼 UI + 按钮回调 | `UIHandler.Instance.ShowDialog<UIDialogNormal/UIDialogSelect/...>(dialogBean)` |
| 悬浮详情气泡 | 自加 Tooltip 子物体 + 跟随逻辑 | `UIHandler.Instance.ShowPopup<UIPopupXXX>(popupBean)` |
| 打开新 UI 并关掉当前所有 | 手动逐个 CloseUI 再 OpenUI | `UIHandler.Instance.OpenUIAndCloseOther<T>()` |
| 刷新所有打开 UI | 自维护刷新列表 | `UIHandler.Instance.RefreshAllUI()` |
| 关闭所有弹窗 | 自循环关 | `UIHandler.Instance.manager.CloseAllDialog()` |

### 判断流程

新代码遇到 UI 通用需求时：

1. **先查** `UIHandler.cs` 是否已有同类方法（关键字：`Show*` / `Hide*` / `Open*` / `Close*` / `Toast*` / `Popup*` / `Dialog*` / `ScreenLock`）。
2. **再查** `BaseUIView` / `BaseUIComponent` / `BaseUIInit` / `BaseUIManager` 基类是否已封装。
3. **都没有**再考虑业务侧实现 —— 若评估为"通用能力"，应**沉淀到 UIHandler / 基类**而非在业务 UI 内私有实现，并同步更新本文档的对照表。

### 典型案例：动画期间防止多次点击

❌ **错误做法**（自维护遮罩，散落各处难维护）：

```csharp
private CanvasGroup canvasGroup;

public void OnClickForSelect(...)
{
    if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    canvasGroup.interactable = false;
    selectedView.AnimForSelect(() =>
    {
        canvasGroup.interactable = true;
        actionForSelect?.Invoke(data);
    });
}
```

✅ **正确做法**（走框架统一通道）：

```csharp
public void OnClickForSelect(...)
{
    if (isAnimating) return;
    isAnimating = true;
    UIHandler.Instance.ShowScreenLock();

    selectedView.AnimForSelect(() =>
    {
        UIHandler.Instance.HideScreenLock();
        actionForSelect?.Invoke(data);
    });
}
```

`ShowScreenLock` 会同时：
- 拉起 Overlay 层的全透明 `UIScreenLock`（`raycastTarget=1`）拦截所有 UI 点击；
- 把 `UIManager.CanClickUIButtons` / `CanInputActionStarted` 置 false，键盘 / 手柄输入一起锁；
- `HideScreenLock` 对称恢复。**比自挂 CanvasGroup 更彻底、可在跨 UI 流程中复用。**

> 配套保留 `bool isAnimating` 做"同帧重入"双保险（ShowScreenLock 是异步 OpenUI，本帧内仍可能被点到第二次）。

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

## 业务 UI 速记（按需补充，完整清单见 ProjectUI.md）

### UICreatureVat（魔物进阶 / CreatureVat 培养槽）

`Assets/Scripts/Component/UI/Game/CreatureVat/UICreatureVat.cs`，魔物进阶主界面（升稀有度 + 授予稀有度 BUFF）：

- **进阶效果**：目标魔物稀有度 +1，并把开始时即确定的「预定 BUFF」写入 `creatureData.dicRarityBuff[新稀有度]`。
- **开始进阶即移出阵容**：`OnClickForStart` 确认后除设 `creatureState=Vat` 外，还调 `userData.RemoveLineupCreature(uuid)` 移出阵容（进阶期间不可出战）；完成时 `UserAscendBean.RemoveAscendData` 复位 `Idle`（仍在背包、不自动回阵容）。确认弹窗文案 80010 含「进阶中的魔物将移出阵容」提示。
- **目标列表**：仅 Idle 且未满级（`RarityInfoCfg.GetAscendTimeByRarity(rarity) > 0`，排除 L）。**默认排序**（`InitCreaturekDataForTarget` 内 `List.Sort`）：稀有度升序 N→L，同稀有度按等级降序。
- **素材列表**：Idle + 排除目标 + 排除上阵（`UserDataBean.CheckIsInAnyLineup`）+ 仅保留稀有度高于目标的魔物；可选上限做成研究 `GetUnlockCreatureVatMaterialMax()`=基础5(`UserLimmitBean.creatureVatMaterialMax`)+`UnlockEnum.CreatureVatMaterialNum`(100000008)等级(满级10)，超出弹 Toast（文本 id 80011）。`LimmitText` 经 `RefreshMaterialLimitText()` 显示「已选/上限」，满时数量转通用警示红 `ColorUtil.WrapLimitFull`。**默认排序**（`InitCreaturekDataForMaterial` 内 `List.Sort`）：目标下一阶段稀有度(=目标稀有度+1)置顶，其余稀有度升序，同稀有度等级降序。
- **预定 BUFF**：`BuffUtil.CreateAscendRarityBuff(newRarity, materials)`（素材 BUFF 按 id 聚合，每 id 10%×数量 命中概率，命中继承并重随机数值≥素材原值；UR/L 无类型为 null）。
- **耗时**：按源稀有度查 `RarityInfoCfg.GetAscendTimeByRarity`（excel_rarity_info 新列 `ascend_time`）作 `timeMax`；被动 tick 每秒 +1 秒。**魔晶加速研究门控**：`GetUnlockCreatureVatAddProgressLevel()`(`UnlockEnum.CreatureVatAddProgress`=100000007,level_max=5) 0级隐藏加速按钮；已研究时**恒消耗1魔晶**，等级=进度增加秒数=进度倍率(Lv N=1魔晶+N秒)，按钮文本 80009「加速进阶 +{等级}秒/晶」。
- **临时进阶数据**：`UserAscendDetailsBean`（随存档序列化）—— `progress` 为「已累积秒数」，含 `targetRarity`/`timeMax`/`ascendBuff`，`IsComplete()` / `GetProgressNormalized()` 驱动完成判定与进度条。
- **进度条配色（`RefreshVatProgress`）**：`ui_ProgressText` 与 `ui_Progress`(fillAmount) 的 `color` 按归一化进度 `ColorUtil.GetProgressColor(progressNormalized)` 分段着色（与献祭成功率同口径）。
- **存档时机**：开始进阶存一次、点完成存一次；培养过程（`GameDataHandler.HandleForAscendData` 每秒 `AddProgress()` + 广播 `CreatureAscend_AddProgress`、魔晶加速）不主动存档。
- **测试模式不落盘**：上述两处直接 `SaveUserData()`，不落盘由全局 `GameDataManager.isTestSimulation`（存档层统一拦截）负责——魔物进阶测试（`TestSceneTypeEnum.CreatureVat`）与献祭测试共用该机制，见 test-system skill；本 UI 不再自带测试标记。
- **完成进阶收尾（`OnClickForComplete`）**：落地数据→`RemoveAscendData`→存档→清空容器后做**反馈**（胜利音效 `AudioEnum.sound_win_1` + 容器处庆祝粒子 `EffectHandler.ShowCreatureAscendCompleteEffect(pos, rarityColor)`——专用粒子 `EffectAscendComplete_1`(白模板 ParticleSystem,运行时按新稀有度 `ui_board_color` 给 startColor 上色 = 稀有度流光) + 成功 Toast `GetTextById(80013)` state=1 绿色，口径同献祭 61007），并**重建目标列表**（`targetCreatureSelect=null`+清素材+`InitCreaturekDataForTarget()`）以反映升阶后的新稀有度，否则列表停留在进阶前状态。
- **进阶详情 UI（AscendData）**：仅「素材选择阶段（`userAscendDetails==null`）+ 已选目标」时显示 `ui_AscendData`、隐藏 `ui_ProgressContent`（培养阶段反之），统一在 `RefreshAscendData()` 切换。`ui_ProgressContent` 未序列化进 Component，靠运行时 `AutoLinkUI` 按名绑定（同理 `ui_AscendIcon` 误绑 Image 也由 AutoLink 自愈到 Animator）。
  - 升阶前/后卡牌 `ui_UIViewCreatureCardItem_BeforeAscend/_AfterAscend` 用 `CardUseStateEnum.ShowNoPopup` 关 popup；After 卡走 `BuildAscendPreviewCreature(target,newRarity)`（稀有度+1、引用字段共享）；两卡 `PlayCardDropIn` 从上掉落+OutBack 缩放。
  - BUFF 增益面板 `ui_AscendBuffs`：`BuffUtil.GetCreatureAscendBuffChances(newRarity, materials)` 算概率，子项 `UIViewCreatureVatAscendBuffItem` 实时克隆/复用缓存、一排≤5 个超出 y 轴下移，出现/消失/移动均 DOTween；`SetData(chance,rarity)` 名字+BG(`ui_BG_Image`)按稀有度配色(`RarityInfo.buff_color`)，BG(`ui_BG_PopupButtonCommonView`)悬浮提示 BUFF 内容(`content_language`，占位参数 `{..}` 数值未定故 Regex 替 `???`)；AscendIcon 用向右戳循环 Animator。
- 详细 BUFF 生成规则见 [buff-system](../buff-system/SKILL.md) / [utils-system](../utils-system/SKILL.md)。

---

## 更新记录

| 日期 | 更新内容 | 更新人 |
|------|----------|--------|
| 2026-04-10 | 创建UI框架SKILL | - |
| 2026-05-26 | 新增"通用控件优先原则"章节，强制要求遮罩/弹窗/Toast/气泡等通用需求走 UIHandler 已有方法 | - |
| 2026-06-24 | 新增 UICreatureVat（魔物进阶/培养槽）业务 UI 速记：升稀有度+授予稀有度 BUFF、素材过滤、最多 5 只、耗时按稀有度、临时进阶数据随存档、被动进度 tick | - |
| 2026-06-27 | UICreatureVat 新增「进阶详情 UI」(AscendData)：素材选择阶段切 ProgressContent↔AscendData、升阶前/后卡牌掉落动画(ShowNoPopup)、AscendIcon 向右戳 Animator、BUFF 增益概率面板(子项 UIViewCreatureVatAscendBuffItem 池化+DOTween)；概率算法 BuffUtil.GetCreatureAscendBuffChances，结构体 CreatureAscendBuffChanceStruct/CreatureAscendMaterialBuffStruct 同放 Assets/Scripts/Struct/CreatureAscendStruct.cs | - |
| 2026-07-04 | UICreatureVat 完成进阶收尾补齐：① 修复完成后目标列表不刷新(重建 InitCreaturekDataForTarget 反映新稀有度)；② 新增进阶成功反馈——胜利音效 sound_win_1 + 容器庆祝粒子 EffectHandler.ShowCreatureAscendCompleteEffect(pos,rarityColor)——经 Unity MCP execute_code 新建专用 EffectAscendComplete_1(ParticleSystem+软发光贴图+additive材质)并注册 Addressables(组 Effect),运行时按新稀有度 ui_board_color 上色(稀有度流光) + 成功 Toast(新增文本 id 80013) | - |

---

*SKILL结束 - 完整UI清单请参考 [MD/ProjectUI.md](../../MD/ProjectUI.md)*
