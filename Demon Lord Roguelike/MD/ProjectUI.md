# 魔王 Roguelike - UI框架文档

> 最后更新：2026年4月10日
> 
> ⚠️ **重要提示**：每次新增UI都必须更新此文档！

---

## 目录

1. [UI框架架构](#一ui框架架构)
2. [核心继承体系](#二核心继承体系)
3. [UI类型分类](#三ui类型分类)
4. [UI管理器](#四ui管理器)
5. [现有UI清单](#五现有ui清单)
6. [新增UI规范](#六新增ui规范)
7. [UI编辑器工具](#七ui编辑器工具)

---

## 一、UI框架架构

### 1.1 整体架构

```
┌─────────────────────────────────────────────────────────────┐
│                      UI展示层                                │
│  普通UI | 弹窗(Dialog) | 气泡(Popup) | 提示(Toast) | 遮罩     │
├─────────────────────────────────────────────────────────────┤
│                      UI逻辑层                                │
│  UIHandler (单例) - UI的统一管理入口                          │
│  - OpenUI / CloseUI / GetUI                                  │
│  - ShowDialog / ToastHint / ShowPopup                        │
├─────────────────────────────────────────────────────────────┤
│                      UI管理层                                │
│  UIManager (MonoBehaviour) - UI生命周期与资源管理             │
│  - CreateUI / DestoryUI                                      │
│  - CreateDialog / CreateToast / CreatePopup                  │
├─────────────────────────────────────────────────────────────┤
│                      UI基类层                                │
│  BaseUIView -> BaseUIInit -> BaseMonoBehaviour               │
│  BaseUIComponent -> BaseUIInit                               │
│  DialogView / PopupShowView / ToastView -> BaseUIView        │
└─────────────────────────────────────────────────────────────┘
```

### 1.2 层级结构（UITypeEnum）

| 层级 | 用途 | 说明 |
|------|------|------|
| `UIBase` | 普通UI | 游戏主界面、功能界面 |
| `Dialog` | 弹窗 | 确认框、选择框、输入框 |
| `Toast` | 提示 | 浮动提示、通知 |
| `Popup` | 气泡 | 悬浮信息、详情展示 |
| `Overlay` | 遮罩 | 屏幕锁定、加载遮罩 |
| `Model3D` | 3D模型 | 3D预览、模型展示 |

---

## 二、核心继承体系

### 2.1 UI组件继承链

```
MonoBehaviour
    │
    ▼
BaseMonoBehaviour                          # 通用方法：Instantiate, Find, AutoLinkUI
    │
    ▼
BaseUIInit                                 # UI初始化基类
    │   - AutoLinkUI() 自动绑定UI控件
    │   - RegisterButtons() 注册按钮点击
    │   - OpenUI() / CloseUI() 打开/关闭
    │   - 事件系统：RegisterEvent / TriggerEvent
    │
    ├── BaseUIComponent                    # UI组件基类
    │   │   - uiManager 引用
    │   │   - uiCloseType 关闭类型 (Hide/Destory)
    │   │
    │   └── UIViewXXX (各类UI组件)
    │
    └── BaseUIView                         # UI视图基类
        │   - rectTransform 缓存
        │   - uiSizeOriginal 原始大小
        │
        ├── 普通UI (UIBaseMain, UIFightMain...)
        ├── DialogView                       # 弹窗基类
        │   ├── UIDialogNormal               # 普通弹窗
        │   ├── UIDialogSelect               # 选择弹窗
        │   └── UIDialogXXX...
        ├── PopupShowView                    # 气泡基类
        │   └── UIPopupXXX...
        └── ToastView                        # 提示基类
            └── UIToastXXX...
```

### 2.2 UI关闭类型

```csharp
public enum UICloseTypeEnum
{
    Hide = 0,      // 关闭时隐藏（保留实例，下次直接显示）
    Destory = 1,   // 关闭时销毁（下次重新创建）
}
```

### 2.3 UI开启动画

```csharp
public enum UIOpenAnimEnum
{
    None = 0,      // 无动画
    ScaleAnim = 1, // 缩放动画 (DOScale OutBack)
}
```

---

## 三、UI类型分类

### 3.1 普通UI (UITypeEnum.UIBase)

| UI名称 | 路径 | 说明 |
|--------|------|------|
| `UIMainStart` | Game/MainStart | 游戏开始界面 |
| `UIMainCreate` | Game/MainCreate | 创建角色界面 |
| `UIMainLoad` | Game/MainLoad | 加载存档界面 |
| `UIMainMaker` | Game/MainMaker | 制作人员界面 |
| `UIBaseMain` | Game/BaseMain | 基地主界面 |
| `UIBaseCore` | Game/BaseCore | 核心建筑界面 |
| `UIBasePortal` | Game/BasePortal | 传送门界面 |
| `UIBaseResearch` | Game/BaseResearch | 研究界面 |
| `UIFightMain` | Game/FightMain | 战斗主界面 |
| `UIFightSettlement` | Game/FightSettlement | 战斗结算界面 |
| `UIFightAbyssalBlessing` | Game/FightAbyssalBlessing | 深渊祝福选择 |
| `UICreatureManager` | Game/CreatureManager | 生物管理界面 |
| `UICreatureChange` | Game/CreatureChange | 生物转换界面 |
| `UICreatureSacrifice` | Game/CreatureSacrifice | 生物献祭界面 |
| `UICreatureVat` | Game/CreatureVat | 生物培养界面 |
| `UILineupManager` | Game/LineupManager | 阵容管理界面 |
| `UIGashaponMachine` | Game/GashaponMachine | 扭蛋机界面 |
| `UIGashaponBreak` | Game/GashaponBreak | 扭蛋爆裂界面 |
| `UIRewardSelect` | Game/RewardSelect | 奖励选择界面 |
| `UIDoomCouncilMain` | Game/DoomCouncil | 终焉议会主界面 |
| `UIDoomCouncilVote` | Game/DoomCouncil | 终焉议会投票界面 |
| `UIDoomCouncilVoteEnd` | Game/DoomCouncil | 终焉议会结算界面 |
| `UIGameSetting` | Game/GameSetting | 游戏设置界面 |
| `UIGameSystem` | Game/GameSystem | 游戏系统界面 |
| `UIGameWorldMap` | Game/GameWorldMap | 世界地图界面 |
| `UIGameConversation` | Game/GameConversation | 对话界面 |
| `UICommonLoading` | Game/CommonLoading | 通用加载界面 |
| `UICommonMask` | Game/CommonMask | 通用遮罩 |
| `UIScreenLock` | Overlay | 屏幕锁定 |

### 3.2 弹窗UI (UITypeEnum.Dialog)

| UI名称 | 路径 | 说明 |
|--------|------|------|
| `UIDialogNormal` | Dialog | 普通确认弹窗 |
| `UIDialogSelect` | Dialog | 选择弹窗 |
| `UIDialogSelectColor` | Dialog | 颜色选择弹窗 |
| `UIDialogSelectItem` | Dialog | 道具选择弹窗 |
| `UIDialogSelectCreature` | Dialog | 生物选择弹窗 |
| `UIDialogRename` | Dialog | 重命名弹窗 |
| `UIDialogBossShow` | Dialog | Boss展示弹窗 |
| `UIDialogCreatureShow` | Dialog | 生物展示弹窗 |
| `UIDialogPortalDetails` | Dialog | 传送门详情弹窗 |

### 3.3 气泡UI (UITypeEnum.Popup)

| UI名称 | 路径 | 说明 |
|--------|------|------|
| `UIPopupItemInfo` | Popup/ItemInfo | 道具信息气泡 |
| `UIPopupCreatureCardDetails` | Popup | 生物卡片详情 |
| `UIPopupAbyssalBlessingInfo` | Popup | 深渊祝福详情 |
| `UIPopupDoomCouncilMainDetails` | Popup | 终焉议会详情 |
| `UIPopupPortalDetails` | Popup | 传送门详情 |
| `UIPopupResearchInfo` | Popup | 研究详情 |
| `UIPopupText` | Popup/Text | 文本气泡 |

### 3.4 提示UI (UITypeEnum.Toast)

| UI名称 | 路径 | 说明 |
|--------|------|------|
| `UIToastNormal` | Toast | 普通提示 |

### 3.5 通用组件UI (Common)

| UI名称 | 路径 | 说明 |
|--------|------|------|
| `UIViewItemBackpack` | Common/Backpack | 背包道具项 |
| `UIViewItemBackpackList` | Common/Backpack | 背包列表 |
| `UIViewItemEquip` | Common/ItemEquip | 装备项 |
| `UIViewCreatureCardItem` | Common/CreatureCard | 生物卡片项 |
| `UIViewCreatureCardList` | Common/CreatureCard | 生物卡片列表 |
| `UIViewCreatureCardDetails` | Common/CreatureCard | 生物卡片详情 |
| `UIViewBasePortalItem` | Common/BasePortal | 传送门项 |
| `UIViewBaseResearchItem` | Common/BaseResearch | 研究项 |
| `UIViewStoreItem` | Common/Store | 商店道具项 |
| `UIViewBuffShowItem` | Common/Buff | Buff展示项 |
| `UIViewColorShow` | Common/Other | 颜色展示 |
| `UIViewAbyssalBlessingInfoContent` | Common/AbyssalBlessing | 深渊祝福内容 |
| `UIViewBaseInfoContent` | Common/BaseInfo | 基础信息内容 |

---

## 四、UI管理器

### 4.1 UIHandler 核心API

```csharp
// 单例访问
UIHandler.Instance

// 打开UI
T OpenUI<T>(Action<T> actionBeforeOpen = null, int layer = -1, string uiNameIn = null, UITypeEnum uiType = UITypeEnum.UIBase)

// 关闭UI
void CloseUI<T>(int layer = -1)
void CloseUI(string uiName, int layer = -1)
void CloseAllUI()

// 获取UI
T GetUI<T>(int layer = -1, string uiNameIn = null, UITypeEnum uiType = UITypeEnum.UIBase)
BaseUIComponent GetOpenUI(int layer = -1)
string GetOpenUIName()

// 打开并关闭其他
T OpenUIAndCloseOther<T>(Action<T> actionBeforeOpen = null, int layer = -1, string uiNameIn = null)

// 刷新UI
void RefreshUI(string uiName, int layer = -1)
void RefreshUI()
void RefreshAllUI()

// 弹窗
T ShowDialog<T>(DialogBean dialogBean)

// Toast提示
T ToastHint<T>(string hintContent)
T ToastHint<T>(string hintContent, float showTime)
T ToastHint<T>(Sprite toastIconSp, string hintContent)

// 气泡
T ShowPopup<T>(PopupBean popupData)
PopupShowView ShowPopup(PopupBean popupData)
void HidePopup(PopupEnum popupType)

// 屏幕锁定
void ShowScreenLock()
void HideScreenLock()
```

### 4.2 使用示例

```csharp
// 打开UI
UIHandler.Instance.OpenUI<UIBaseMain>();

// 打开UI并设置数据
UIHandler.Instance.OpenUI<UIBaseMain>((ui) =>
{
    ui.SetData(data);
});

// 关闭UI
UIHandler.Instance.CloseUI<UIBaseMain>();

// 显示弹窗
DialogBean dialogData = new DialogBean("确认删除？", "删除后将无法恢复", "确定", "取消");
dialogData.actionSubmit = (dialog, data) =>
{
    // 确认操作
};
UIHandler.Instance.ShowDialog<UIDialogNormal>(dialogData);

// Toast提示
UIHandler.Instance.ToastHint<UIToastNormal>("保存成功！");

// 屏幕锁定
UIHandler.Instance.ShowScreenLock();
```

---

## 五、现有UI清单

### 5.1 UI脚本目录结构

```
Assets/Scripts/Component/UI/
├── Game/                              # 游戏主UI
│   ├── MainStart/
│   ├── MainCreate/
│   ├── MainLoad/
│   ├── MainMaker/
│   ├── BaseMain/
│   ├── BaseCore/
│   ├── BasePortal/
│   ├── BaseResearch/
│   ├── FightMain/
│   ├── FightSettlement/
│   ├── FightAbyssalBlessing/
│   ├── CreatureManager/
│   ├── CreatureChange/
│   ├── CreatureSacrifice/
│   ├── CreatureVat/
│   ├── LineupManager/
│   ├── GashaponMachine/
│   ├── GashaponBreak/
│   ├── RewardSelect/
│   ├── DoomCouncil/
│   ├── GameSetting/
│   ├── GameSystem/
│   ├── GameWorldMap/
│   ├── GameConversation/
│   ├── CommonLoading/
│   └── CommonMask/
├── Dialog/                            # 弹窗UI
│   ├── UIDialogNormal.cs
│   ├── UIDialogSelect.cs
│   ├── UIDialogSelectColor.cs
│   ├── UIDialogSelectItem.cs
│   ├── UIDialogSelectCreature.cs
│   ├── UIDialogRename.cs
│   ├── UIDialogBossShow/
│   ├── UIDialogCreatureShow.cs
│   └── UIDialogPortalDetails.cs
├── Popup/                             # 气泡UI
│   ├── ItemInfo/
│   ├── Text/
│   ├── UIPopupAbyssalBlessingInfo.cs
│   ├── UIPopupCreatureCardDetails.cs
│   ├── UIPopupDoomCouncilMainDetails.cs
│   ├── UIPopupPortalDetails.cs
│   └── UIPopupResearchInfo.cs
└── Common/                            # 通用组件
    ├── Backpack/
    ├── ItemEquip/
    ├── CreatureCard/
    ├── BasePortal/
    ├── BaseResearch/
    ├── Store/
    ├── Buff/
    ├── AbyssalBlessing/
    ├── BaseInfo/
    └── Other/
```

### 5.2 框架层UI组件

```
Assets/FrameWork/Scripts/Component/UI/
├── ScrollGrid/                        # 滚动网格组件
│   ├── ScrollGridBaseContent.cs
│   ├── ScrollGridCell.cs
│   ├── ScrollGridHorizontal.cs
│   └── ScrollGridVertical.cs
├── DialogView.cs                      # 弹窗基类
├── PopupShowView.cs                   # 气泡基类
├── ToastView.cs                       # 提示基类
├── SelectView.cs                      # 选择器组件
├── SelectColorView.cs                 # 颜色选择器
├── CartogramBarView.cs                # 柱状图组件
├── ProgressView.cs                    # 进度条组件
├── DropdownView.cs                    # 下拉框组件
├── RadioButtonView.cs                 # 单选按钮组件
├── RadioGroupView.cs                  # 单选组组件
├── LongPressButton.cs                 # 长按按钮组件
├── ButtonExtendView.cs                # 扩展按钮组件
├── LineView.cs                        # 连线组件
├── UITextLanguageView.cs              # 多语言文本组件
├── AudioView.cs                       # 音频控制组件
├── CursorView.cs                      # 光标组件
└── ...
```

---

## 六、新增UI规范

### 6.1 命名规范

| 类型 | 前缀 | 示例 |
|------|------|------|
| 普通UI | `UI` | `UIBaseMain`, `UIFightMain` |
| 弹窗 | `UIDialog` | `UIDialogNormal`, `UIDialogSelect` |
| 气泡 | `UIPopup` | `UIPopupItemInfo` |
| 提示 | `UIToast` | `UIToastNormal` |
| 组件 | `UIView` | `UIViewCreatureCardItem` |

### 6.2 创建步骤

1. **使用编辑器工具创建脚本**
   - 菜单：`Custom/工具弹窗/UI脚本创建`
   - 或点击Toolbar上的`UI脚本创建`按钮
   - 选择脚本类型：UI / View / Dialog / Popup / Toast

2. **Prefab放置路径**
   - 普通UI：`Assets/Prefabs/UI/Game/`
   - 弹窗：`Assets/Prefabs/UI/Dialog/`
   - 气泡：`Assets/Prefabs/UI/Popup/`
   - 提示：`Assets/Prefabs/UI/Toast/`

3. **脚本放置路径**
   - 普通UI：`Assets/Scripts/Component/UI/Game/[模块名]/`
   - 弹窗：`Assets/Scripts/Component/UI/Dialog/`
   - 气泡：`Assets/Scripts/Component/UI/Popup/[子目录]/`
   - 组件：`Assets/Scripts/Component/UI/Common/[子目录]/`

4. **更新本文档**
   - 在对应分类下添加新UI信息
   - 更新目录结构
   - 更新最后更新时间

### 6.3 代码模板

#### 普通UI模板

```csharp
using UnityEngine;
using UnityEngine.UI;

public class UIExample : BaseUIView
{
    // UI控件（命名规范：ui_xxx）
    public Button ui_Submit;
    public Text ui_Title;
    
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
    }

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_Submit)
        {
            OnClickSubmit();
        }
    }

    private void OnClickSubmit()
    {
        // 按钮点击逻辑
    }
}
```

#### 弹窗模板

```csharp
using UnityEngine;

public class UIDialogExample : DialogView
{
    public override void SetData(DialogBean dialogData)
    {
        base.SetData(dialogData);
        // 自定义数据设置
    }

    public override void SubmitOnClick()
    {
        base.SubmitOnClick();
        // 确认逻辑
    }
}
```

---

## 七、UI编辑器工具

### 7.1 BaseUICreateWindow

**打开方式：**
- 菜单：`Custom/工具弹窗/UI脚本创建`
- Toolbar：`UI脚本创建`按钮

**功能：**
- 自动生成UI脚本
- 自动根据Prefab命名生成类名
- 自动添加脚本组件到Prefab
- 支持UI/View/Dialog/Popup/Toast/Common多种类型

**脚本模板路径：**
- UI模板：`Assets/FrameWork/Editor/ScriptsTemplates/UI_BaseUI.txt`
- View模板：`Assets/FrameWork/Editor/ScriptsTemplates/UI_BaseUIView.txt`
- Dialog模板：`Assets/FrameWork/Editor/ScriptsTemplates/UI_BaseUIDialog.txt`
- Popup模板：`Assets/FrameWork/Editor/ScriptsTemplates/UI_BaseUIPopup.txt`
- Toast模板：`Assets/FrameWork/Editor/ScriptsTemplates/UI_BaseUIToast.txt`

### 7.2 InspectorBaseUIComponent

- 自动绑定UI控件（命名规则：`ui_XXX`）
- 支持Text, Button, Image, Slider等常用组件
- 支持自定义组件类型

---

## 更新记录

| 日期 | 更新内容 | 更新人 |
|------|----------|--------|
| 2026-04-10 | 创建UI框架文档 | - |

---

*文档结束 - UI详细API请参考 [ProjectDocs.md](ProjectDocs.md)*
