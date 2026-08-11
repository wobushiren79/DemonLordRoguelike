---
name: ui-popup
description: 气泡UI和提示UI开发：PopupShowView/ToastView基类、道具信息气泡、生物详情气泡、文本气泡、Toast提示。
tools: Read, Write, Edit, Glob, Grep, Bash
watched_files:
  - Assets/Scripts/Component/UI/Popup/
  - Assets/Scripts/Component/UI/Toast/
  - Assets/Prefabs/UI/Popup/
  - Assets/Prefabs/UI/Toast/
---

# 气泡与提示 UI (Popup & Toast UI) 开发代理

你负责 [Scripts/Component/UI/Popup/](Assets/Scripts/Component/UI/Popup/) 和 [Scripts/Component/UI/Toast/](Assets/Scripts/Component/UI/Toast/) 中所有气泡与提示 UI 的开发。

## 职责范围

### 气泡 UI (Popup)
- **UIPopupItemInfo** - 道具信息气泡
- **UIPopupCreatureCardDetails** - 生物卡片详情
- **UIPopupAbyssalBlessingInfo** - 深渊祝福详情
- **UIPopupDoomCouncilBillDetails** - 终焉议会详情
- **UIPopupPortalDetails** - 传送门详情（继承 `PopupShowCommonView`，详见下方「UIPopupPortalDetails 结构」）
- **UIPopupResearchInfo** - 研究详情
- **UIPopupText** - 文本气泡

### 提示 UI (Toast)
- **UIToastNormal** - 普通提示

### 基类
- **PopupShowView** - 气泡基类
- **ToastView** - 提示基类

### 使用方式
```csharp
// 显示气泡
PopupBean popupData = new PopupBean(PopupEnum.ItemInfo, targetTransform);
UIHandler.Instance.ShowPopup<UIPopupItemInfo>(popupData);

// 隐藏气泡
UIHandler.Instance.HidePopup(PopupEnum.ItemInfo);

// Toast 提示
UIHandler.Instance.ToastHint<UIToastNormal>("保存成功！");
UIHandler.Instance.ToastHint<UIToastNormal>("内容", 3f);
```

## 代码模板（气泡）

```csharp
public class UIPopupExample : PopupShowView
{
    public override void SetData(PopupBean popupData) { base.SetData(popupData); }
}
```

## UIPopupItemInfo 结构（道具信息气泡）

[UIPopupItemInfo.cs](Assets/Scripts/Component/UI/Popup/ItemInfo/UIPopupItemInfo.cs) 的 `SetData` 依次调用 `SetNum` → `SetType` → `SetAttributes` → `SetJuiceExp`：

- **魔汁经验行（JuiceExpText）**：prefab [UIPopupItemInfo.prefab](Assets/Resources/UI/Popup/UIPopupItemInfo.prefab) Details 节点下新增 `JuiceExpText`（复制 RarityText 而来，sibling index 1 即 RarityText 之后，默认 SetActive(false)）；Component 新增字段 `public TextMeshProUGUI ui_JuiceExpText;`（AutoLinkUI 按名绑定：字段名 ui_JuiceExpText → 子物体 JuiceExpText）。
- **`SetJuiceExp(itemData, itemInfo)`**（SetData 末尾调用）：仅 `ItemTypeEnum.Juice`（魔汁）显示并填 textId **61017**「经验+{0}」格式化 `itemData.juicerExp`，其余道具隐藏；`ui_JuiceExpText` 为 null 时容错跳过（prefab 未配置该元素不报错）；魔汁 dicAttribute 为空故属性区自动隐藏，两者互斥不冲突。

## UIPopupPortalDetails 结构（传送门详情气泡）

[UIPopupPortalDetails.cs](Assets/Scripts/Component/UI/Popup/UIPopupPortalDetails.cs) 继承 `PopupShowCommonView`，`SetData(object data)` 接收 `(GameWorldInfoBean, GameWorldInfoRandomBean, int difficultyLevel)` 三元组，展示某难度下传送门的预生成信息。已从早期「`transform.GetChild(index)` + `Find("Title"/"Content")` 手动拼装」重构为 **AutoLinkUI 按名绑定的详情项 + 道具缓存池**：

- **五个详情项**（[Component](Assets/Scripts/Component/UI/Popup/UIPopupPortalDetailsComponent.cs) 中 `ui_UIViewPopupProtalDetailsItem_Name/_Level/_RoadNum/_FightNum/_RoadLength`，类型均为 `UIViewPopupPortalDetailsItem`）：名字 / 难度 / 线路数 / 关卡数 / 路径长度。每项调 `itemView.SetData(title, content, isShow)`；标题文本 id 依次为 411/415/412/413/414（415「难度」、414「路径长度」为新增文本；难度内容=`difficultyLevel`，仅征服模式显示）。征服模式按 `gameWorldInfoRandom.GetDifficultyRandom(difficultyLevel)` 取该难度预生成的 roadNum/fightNum/roadLength；无尽模式直接用字段值。
- **[UIViewPopupPortalDetailsItem](Assets/Scripts/Component/UI/Popup/PortalDetails/UIViewPopupPortalDetailsItem.cs)**（继承 `BaseUIView`）：`SetData(title, content, isShow)` —— `isShow==false` 时整行 `SetActive(false)` 隐藏；内部 `SetTitle`/`SetContent` 写 `ui_Title`/`ui_Content`。
- **奖励道具显示**：以 `ui_UIViewItem`（[UIViewItem](Assets/Scripts/Component/UI/Common/Item/UIViewItem.cs)，通用道具项 = 图标+数量+ItemInfo 气泡）为模板的缓存池 `listRewardItemPool`（池首项=模板，不足时 `Instantiate` 克隆到同一容器，多余项隐藏）。`RefreshRewardItems(listReward)` 按 `gameWorldInfoRandom.GetDifficultyReward(difficultyLevel)` 实时填充；每项 `itemView.SetData(itemBean)`。内容变化后 `LayoutRebuilder.ForceRebuildLayoutImmediate`（先 `ui_Items` 再 `rectTransform`）保证气泡尺寸与跟随定位正确。
- **研究门控**：线路数/关卡数/路径长度/奖励四项受「设施」研究门控，用 `userUnlock.CheckIsUnlock(UnlockEnum.PortalPreviewRoadNum / PortalPreviewFightNum / PortalPreviewRoadLength / PortalPreviewReward)` 判定，未解锁则该详情项整行隐藏（奖励区直接不显示）。**名字行、难度行始终显示**，不受门控。无尽模式（`GameFightTypeEnum.Infinite`）不展示难度/关卡数/路径长度/奖励（难度是征服模式专属概念）。

> `UIViewItem` 为 `Common/Item` 通用道具项基类，子类含 `UIViewItemBackpack`/`UIViewItemEquip`；点击命中即 `OnClickForSelect()`（子类重写触发各自选中事件）。

## 约束

- 气泡继承 PopupShowView，使用 `UIPopup` 前缀命名
- 提示继承 ToastView，使用 `UIToast` 前缀命名
- PopupBean 必须指定 PopupEnum 类型和目标 Transform
- Prefab 放置在 `Assets/Prefabs/UI/Popup/` 或 `Assets/Prefabs/UI/Toast/`
