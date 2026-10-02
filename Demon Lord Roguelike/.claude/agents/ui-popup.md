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
- **PopupShowView** - 气泡基类（已内建出现/消失动画：开关字段 `isAnimForShow`/`isAnimForHide`/`isAnimWithFade`，virtual 方法 `AnimForShow()`/`AnimForHide(onComplete)`/`ShowWithAnim()`/`HideWithAnim()`——DOScale 0→1 OutBack 弹出 / →0 InBack 缩回+可选淡出(代码 GetOrAdd CanvasGroup)，全部 unscaled，只做 scale+fade 无位移与每帧位置弹簧共存；隐藏中再次 Show 会 Kill 隐藏 Tween 重播出现动画(中断恢复)，Hide 有 `isHidingForAnim` 防重入、播完才真正隐藏。子类可在 Awake 置开关=false 回到瞬显瞬隐，或 override 定制动画）
- **ToastView** - 提示基类

### 使用方式
```csharp
// 显示气泡（默认带出现动画，经 PopupShowView.ShowWithAnim 收口）
PopupBean popupData = new PopupBean(PopupEnum.ItemInfo, targetTransform);
UIHandler.Instance.ShowPopup<UIPopupItemInfo>(popupData);

// 隐藏气泡（默认带消失动画，经 PopupShowView.HideWithAnim 收口）
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

[UIPopupItemInfo.cs](Assets/Scripts/Component/UI/Popup/ItemInfo/UIPopupItemInfo.cs) 的 `SetData` 依次调用 `SetNum` → `SetType` → `SetAttributes` → `SetJuiceExp` → `SetTransformPreview`：

- **魔汁经验行（JuiceExpText）**：prefab [UIPopupItemInfo.prefab](Assets/Resources/UI/Popup/UIPopupItemInfo.prefab) Details 节点下新增 `JuiceExpText`（复制 RarityText 而来，sibling index 1 即 RarityText 之后，默认 SetActive(false)）；Component 新增字段 `public TextMeshProUGUI ui_JuiceExpText;`（AutoLinkUI 按名绑定：字段名 ui_JuiceExpText → 子物体 JuiceExpText）。
- **`SetJuiceExp(itemData, itemInfo)`**：仅 `ItemTypeEnum.Juice`（魔汁）显示并填 textId **61017**「经验+{0}」格式化 `itemData.juicerExp`，其余道具隐藏；`ui_JuiceExpText` 为 null 时容错跳过（prefab 未配置该元素不报错）；魔汁 dicAttribute 为空故属性区自动隐藏，两者互斥不冲突。
- **幻化药形象预览区（TransformPreviewContent）**（2026-10-02 起）：prefab Details 节点末位 `TransformPreviewContent`（460x300，默认 SetActive(false)），下挂 `ShowArea`/`UIShowArea`（各 230x300，VLG 排版 childControl/forceExpand 全 0=只排列不控尺寸），区内 = 标签 `ShowLabel`/`UIShowLabel`（230x60）+ **两层裁切容器** `ShowRect` → `ShowRectContent`（均 230x240，Image alpha=0 纯载体 + RectMask2D，spine 超出框部分像素级裁切）→ spine 节点 `ShowSpine`/`UIShowSpine`（230x240 居中锚定，SkeletonGraphicExtend + SkeletonAnimation 配对，SkeletonGraphicDefault 材质）——**spine 矩形由布局固定，代码不得改其 sizeDelta/anchoredPosition**。Component 字段：`ui_TransformPreviewContent`/`ui_ShowArea`/`ui_UIShowArea`/`ui_ShowLabel`/`ui_UIShowLabel`/`ui_ShowSpine`/`ui_UIShowSpine`（spine 两字段类型 `SkeletonGraphicExtend`）。
- **`SetTransformPreview(itemInfo)`**（SetData 末尾调用）：仅幻化药（TransformPotion 且 other_data 非空）显示预览区，经 `CreatureBean.ParseTransformOtherData` 解析后两段独立显隐——show 段有 show_res 键才显示（标签 61022「战斗形象」，`SetSkeletonDataAsset` + `PlayAnim(animNameAppoint: idle_anim)`）；ui_show 段有 ui_show_res 键才显示（标签 61023「详情形象」，+ ui_show_skin 换肤「|」多皮肤叠加、无皮肤键时 `SetSkin(null)+SetupPoseSlots` 防复用残留 + ui_show_idle_anim）。预览尺寸（`ApplyTransformPreviewSize`）：**走 `GameUIUtil.ApplyCardIconSizeFit` 卡片图标尺寸等比适配**——预览 spine 节点与卡片 ui_Icon 同 pivot(0.5,0)+父容器中心锚定（挂 ShowRectContent 绝对定位，不受 LayoutGroup 覆写），show 段传 `cardContentHeightForS`、ui_show 段传 `cardContentHeightForB`，尺寸键 scale;pos 同乘「预览框高/卡片标准容器高」系数，还原小卡/大卡显示效果；Skeleton.X/Y 清零防历史方案残留。注意：直接套尺寸键不乘系数会把骨架推出框外被 RectMask2D 裁光（OtherSpine 药 ui_show pos.y=-226 曾因此全空白）。

## UIPopupPortalDetails 结构（传送门详情气泡）

[UIPopupPortalDetails.cs](Assets/Scripts/Component/UI/Popup/UIPopupPortalDetails.cs) 继承 `PopupShowCommonView`，`SetData(object data)` 接收 `(GameWorldInfoBean, GameWorldInfoRandomBean, int difficultyLevel)` 三元组，展示某难度下传送门的预生成信息。已从早期「`transform.GetChild(index)` + `Find("Title"/"Content")` 手动拼装」重构为 **AutoLinkUI 按名绑定的详情项 + 道具缓存池**：

- **五个详情项**（[Component](Assets/Scripts/Component/UI/Popup/UIPopupPortalDetailsComponent.cs) 中 `ui_UIViewPopupProtalDetailsItem_Name/_Level/_RoadNum/_FightNum/_RoadLength`，类型均为 `UIViewPopupPortalDetailsItem`）：名字 / 难度 / 线路数 / 关卡数 / 路径长度。每项调 `itemView.SetData(title, content, isShow)`；标题文本 id 依次为 411/415/412/413/414（415「难度」、414「路径长度」为新增文本；难度内容=`difficultyLevel`，征服/无尽均显示（`isShowDifficulty=非挑战100勇士`），挑战100勇士隐藏）。征服模式按 `gameWorldInfoRandom.GetDifficultyRandom(difficultyLevel)` 取该难度预生成的 roadNum/fightNum/roadLength；无尽模式道路数/长度按难度从 `listDifficultyRandom` 直接 `Find`（不走 `GetDifficultyRandom` 懒生成，避免连带生成征服通关奖励）。**挑战100勇士(ChallengeHundred)分支**：难度行与关卡数行隐藏(`isShowFightNum && !isChallengeHundred`，单关恒1无信息量)，线路数量/路径长度行保留(研究门控照旧)。
- **[UIViewPopupPortalDetailsItem](Assets/Scripts/Component/UI/Popup/PortalDetails/UIViewPopupPortalDetailsItem.cs)**（继承 `BaseUIView`）：`SetData(title, content, isShow)` —— `isShow==false` 时整行 `SetActive(false)` 隐藏；内部 `SetTitle`/`SetContent` 写 `ui_Title`/`ui_Content`。
- **奖励道具显示**：以 `ui_UIViewItem`（[UIViewItem](Assets/Scripts/Component/UI/Common/Item/UIViewItem.cs)，通用道具项 = 图标+数量+ItemInfo 气泡）为模板的缓存池 `listRewardItemPool`（池首项=模板，不足时 `Instantiate` 克隆到同一容器，多余项隐藏）。征服模式 `RefreshRewardItems(listReward)` 只取 `gameWorldInfoRandom.GetDifficultyReward(difficultyLevel)` 的首箱保底位(`listReward[0]`)组单件列表填充（其余可选箱不预览）；**挑战100勇士改取 `gameWorldInfoRandom.GetChallengeHundredReward()` 全量3件传入**（预览=实领，通关3箱3抽全手动开，无首箱保底）。每项 `itemView.SetData(itemBean)`。内容变化后 `LayoutRebuilder.ForceRebuildLayoutImmediate`（先 `ui_Items` 再 `rectTransform`）保证气泡尺寸与跟随定位正确。
- **研究门控**：线路数/关卡数/路径长度/奖励四项受「设施」研究门控，用 `userUnlock.CheckIsUnlock(UnlockEnum.PortalPreviewRoadNum / PortalPreviewFightNum / PortalPreviewRoadLength / PortalPreviewReward)` 判定，未解锁则该详情项整行隐藏（奖励区直接不显示）。**名字行、难度行始终显示**，不受门控。无尽模式（`GameFightTypeEnum.Infinite`）难度行现也显示（`isShowDifficulty=非挑战100勇士`，原「无尽隐藏难度行」描述已过时），但关卡数/路径长度/奖励行维持对无尽隐藏（`isShowFightNum=非无尽` 统一门控这三行；无尽无关卡数/通关奖励概念）；挑战100勇士隐藏难度/关卡数两行。

> `UIViewItem` 为 `Common/Item` 通用道具项基类，子类含 `UIViewItemBackpack`/`UIViewItemEquip`；点击命中即 `OnClickForSelect()`（子类重写触发各自选中事件）。

## 约束

- 气泡继承 PopupShowView，使用 `UIPopup` 前缀命名
- 提示继承 ToastView，使用 `UIToast` 前缀命名
- PopupBean 必须指定 PopupEnum 类型和目标 Transform
- Prefab 放置在 `Assets/Prefabs/UI/Popup/` 或 `Assets/Prefabs/UI/Toast/`
