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
- **UIDialogPortalDetails** - 传送门详情弹窗（征服难度选择：左右按钮切换难度，3 个 `UIViewDialogPortalDetailsItem` 分列 -400/0/400 显示上一个/当前/下一个难度，对象池 4 个 item 支持 0.6s 左右滑动动画（`AnimSwitchDifficulty` 调 `AnimSwitchOneItem` 逐个起步：位移 `DOAnchorPosX` 用 `Ease.OutBack`，透明度/缩放按切换前→后中心渐变；**池槽位必须按「切换后中心」的紧凑顺序 `newCenter-1/newCenter/newCenter+1→pool[0..2]`、滑出项放剩余槽位分配，与动画结束后 `RefreshItemsImmediate(newCenter)` 完全一致**，否则向右切换时中心 item 会在刷新瞬间被换成另一个正在缩小的对象，表现为「缩放最后一刻一瞬间放大」——勿改回按 `min(old,new)-1` 顺序分配）；中间难度限制在 `[1, GetUnlockGameWorldConquerDifficultyLevel]`，到边界点击时播放「试探 1/3 间距再弹回」的回弹动画，且向上越界时 Toast 提示「难度未解锁」(文本id 404)；item 显示星球图标+难度文本(文本id 403)，未解锁难度显示 Chain_1/Chain_2 锁链，玩家曾通关过该世界对应难度的征服模式(`UserAchievementBean.GetConquerCompleteCount(worldId, difficultyLevel) > 0`)则显示 `ui_Complete` 通关标记(`SetComplete` 在 `SetData` 里调用)，IconBG 背景色由难度表 `FightTypeConquerInfo.bg_color`(经 `GetBGColor()` 解析)按难度等级着色(绿→紫，由易到难)；非当前难度 item 的 CanvasGroup(`ui_UIViewDialogPortalDetailsItem`) 透明度降到 0.5(当前为 1)、缩放降到 0.6(当前为 1，`SetScale`/`DoScale` 作用于 item 根节点 `rectTransform.localScale`，常量 `scaleCurrent=1`/`scaleOther=0.6`，由 `GetItemScale` 按是否中心难度取值)，滑动时透明度与缩放同步渐变；item 的 IconBG 上挂 `PopupButtonCommonView`(`ui_IconBG_PopupButtonCommonView`)，与 `UIViewBasePortalItem` 一致，悬停时 `SetData((gameWorldInfo, gameWorldInfoRandom), PopupEnum.PortalDetails)` 弹出传送门详情气泡(名字/线路数/关卡数)，故 `SetData` 改为接收完整 `GameWorldInfoRandomBean`(而非仅 iconSeed)。item 根节点挂 `Animator`(控制器 `Assets/LoadResources/Anim/UI/UIViewDialogPortalDetailsItem.controller`，默认状态 Idle 循环播放 `UIViewDialogPortalDetailsItem.anim`，仅驱动子节点 `Icon` 的 `m_AnchoredPosition.y` 上下漂浮 ±6px/2s)，`RandomizeIdleAnimOffset()` 以 `Animator.Play("Idle",0,Random.Range(0,1))` 随机化起始相位，避免多个item漂浮完全同步；**该方法必须在 `OnEnable()`(item 随层级真正激活的一刻)里调用，而非 `SetData` 里——因为 `SetData`/`ShowItem` 可能在弹窗层级尚未激活时被调用，那时 Animator 无法求值，随机相位会被随后的激活 rebind(重置回默认 normalizedTime=0)吞掉导致所有 item 同步；放在 `OnEnable` 才能保证 Animator 已激活。Play 之后仍须紧跟 `idleAnimator.Update(0f)` 强制当帧求值固定随机相位——勿删此调用，也勿移回 `SetData`**。子 item 视图 `UIViewDialogPortalDetailsItem`/`.SetData`/`.SetPopup`/`.RandomizeIdleAnimOffset` 在 `Assets/Scripts/Component/UI/Dialog/PortalDetails/`）
- **UIDialogOrderFilter** - 排序筛选弹窗（在按钮处弹出，多选筛选类型+按选择顺序定优先级，正/倒序确认）

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

## 排序筛选弹窗 UIDialogOrderFilter 使用说明

通用「排序筛选」弹窗：在某个按钮处弹出，**可多选**筛选类型并**按选择顺序决定排序优先级**（index0=最高/主键），再点「正序 / 倒序」确认，结果通过回调交调用方排序；点击背景关闭。

```csharp
UIHandler.Instance.ShowDialogOrderFilter(
    ui_OrderBtn_Button.transform as RectTransform,          // 触发按钮：弹窗内容定位到它处
    OnConfirmOrderFilter,                                   // 确认回调
    listFilterType,                                         // 初始化时开放哪些筛选(null=全部)
    new List<OrderFilterTypeEnum>(currentFilterTypes));     // 默认已选(按优先级，可空)

// filterTypes 按优先级从高到低(index0=主键)；isAscending 作用于全部键的全局正/倒序
protected void OnConfirmOrderFilter(List<OrderFilterTypeEnum> filterTypes, bool isAscending) { /* 调用方据此排序 */ }
```

- 也可直接构造 `DialogOrderFilterBean` 走 `ShowDialogOrderFilter(bean)` 重载。
- `OrderFilterTypeEnum`（定义于 `GameStateEnum.cs`）：`Rarity=1 / Level=2 / Lineup=3 / Name=4 / Class=5`。
- `listFilterType` 即「**初始化时自定义开放哪些筛选**」；不在列表里的项隐藏，且会从默认选中里剔除。
- 相关文件：`UIDialogOrderFilter`、`DialogOrderFilterBean`、`UIViewDialogOrderFilterItem`（弹窗内筛选项）。参考接入：`UIViewCreatureCardList` 的 `OrderBtn`。

### ⚠️ 约定：筛选/排序按钮的 popup 详情统一用 UIText `2000014`

**凡是「打开 UIDialogOrderFilter 的筛选/排序按钮」，其 `PopupButtonCommonView` 悬浮详情一律使用多语言 `2000014`（`筛选排序` / `Sort & Filter`），不要再为每个按钮新建文本：**

```csharp
ui_OrderBtn_PopupButtonCommonView.SetData(TextHandler.Instance.GetTextById(2000014), PopupEnum.Text);
```

> 旧文本（稀有度=2000004 / 等级=2000005 / 阵容=2000006 / 名字=2000007 / 同类=2000011）现仅用于**弹窗内每个筛选项**的悬浮详情（见 `UIViewDialogOrderFilterItem.GetFilterDetail`），不再用于按钮本身。

## 约束

- 弹窗继承 DialogView，使用 `UIDialog` 前缀命名
- 弹窗数据通过 DialogBean 传递
- Prefab 放置在 `Assets/Prefabs/UI/Dialog/`
- 新增弹窗需更新 MD/ProjectUI.md 文档
