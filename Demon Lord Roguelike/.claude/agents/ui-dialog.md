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
- **UIDialogPortalDetails** - 传送门详情弹窗（征服难度选择：左右按钮切换难度，3 个 `UIViewDialogPortalDetailsItem` 分列 -400/0/400 显示上一个/当前/下一个难度，对象池 4 个 item 支持 0.6s 左右滑动动画（`AnimSwitchDifficulty` 调 `AnimSwitchOneItem` 逐个起步：位移 `DOAnchorPosX` 用 `Ease.OutBack`，透明度/缩放按切换前→后中心渐变；**池槽位必须按「切换后中心」的紧凑顺序 `newCenter-1/newCenter/newCenter+1→pool[0..2]`、滑出项放剩余槽位分配，与动画结束后 `RefreshItemsImmediate(newCenter)` 完全一致**，否则向右切换时中心 item 会在刷新瞬间被换成另一个正在缩小的对象，表现为「缩放最后一刻一瞬间放大」——勿改回按 `min(old,new)-1` 顺序分配）；中间难度限制在 `[1, GetUnlockGameWorldConquerDifficultyLevel]`，到边界点击时播放「试探 1/3 间距再弹回」的回弹动画，且向上越界时 Toast 提示「难度未解锁」(文本id 404)；item 显示星球图标+难度文本(文本id 403)，未解锁难度显示 Chain_1/Chain_2 锁链，玩家曾通关过该世界对应难度的征服模式(`UserAchievementBean.GetConquerCompleteCount(worldId, difficultyLevel) > 0`)则显示 `ui_Complete` 通关标记(`SetComplete` 在 `SetData` 里调用)，IconBG 背景色由难度表 `FightTypeConquerInfo.bg_color`(经 `GetBGColor()` 解析)按难度等级着色(绿→紫，由易到难)；非当前难度 item 的 CanvasGroup(`ui_UIViewDialogPortalDetailsItem`) 透明度降到 0.5(当前为 1)、缩放降到 0.6(当前为 1，`SetScale`/`DoScale` 作用于 item 根节点 `rectTransform.localScale`，常量 `scaleCurrent=1`/`scaleOther=0.6`，由 `GetItemScale` 按是否中心难度取值)，滑动时透明度与缩放同步渐变；item 的 IconBG 上挂 `PopupButtonCommonView`(`ui_IconBG_PopupButtonCommonView`)，与 `UIViewBasePortalItem` 一致，悬停时 `SetData((gameWorldInfo, gameWorldInfoRandom, difficultyLevel), PopupEnum.PortalDetails)` 弹出传送门详情气泡(名字/线路数/关卡数)，气泡按**各 item 自身难度**取数(征服模式经 `gameWorldInfoRandom.GetDifficultyRandom(difficultyLevel)` 读各档预生成的 roadNum/fightNum，无尽模式回退当前字段)，故 `SetData` 改为接收完整 `GameWorldInfoRandomBean`(而非仅 iconSeed)；**未解锁难度 item 不弹气泡**——`SetPopup(gameWorldInfo, gameWorldInfoRandom, isUnlock)` 在 `!isUnlock` 时改为 `SetData(null, PopupEnum.PortalDetails)` 清空 targetData，`PopupButtonCommonView` 悬停见 targetData 为 null 即跳过。item 根节点挂 `Animator`(控制器 `Assets/LoadResources/Anim/UI/UIViewDialogPortalDetailsItem.controller`，默认状态 Idle 循环播放 `UIViewDialogPortalDetailsItem.anim`，仅驱动子节点 `Icon` 的 `m_AnchoredPosition.y` 上下漂浮 ±6px/2s)，`TryApplyIdleAnimOffset()` 以 `Animator.Play("Idle",0,Random.Range(0,1))`+`Update(0f)` 给每个 item 设一次性随机起始相位，避免多个item漂浮同步；**该方法在 `OnEnable()` 调用并由 `Update()` 持续重试至落定(首次 OnEnable 可能在弹窗层级激活前、Animator 未初始化，需等 `gameObject.activeInHierarchy && idleAnimator.isActiveAndEnabled` 才能稳定求值)，且全程只成功应用一次(`idleOffsetApplied`)；同时开启 `idleAnimator.keepAnimatorStateOnDisable = true`，使难度切换反复 SetActive 关/开本 item 时 Animator 状态不被重置、漂浮无缝续播——勿在每次 OnEnable 重新随机/重播，否则切换难度时漂浮会被还原重播；相位 `idleAnimNormalizedOffset` 一次性确定后保持不变**。子 item 视图 `UIViewDialogPortalDetailsItem`/`.SetData`/`.SetPopup`/`.TryApplyIdleAnimOffset` 在 `Assets/Scripts/Component/UI/Dialog/PortalDetails/`）
- **UIDialogOrderFilter** - 排序筛选弹窗（分区段：名字模糊/等级区间/稀有度多选=筛选，战斗/其它=排序键多选；区段显隐由 listFilterType 推导；单个「确认」回传 `OrderFilterResultBean`）

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

通用「排序筛选」弹窗：在某个按钮处弹出，**按 5 个区段分组**（外层 `ContentShow` 竖排）：
- **条件区**（命中即置顶，**不删行、全部展示**）：`ContentName`（模糊名字输入框，大小写不敏感子串）、`ContentLevel`（左右两个整数输入构成等级区间，仅数字、≥0、左≤右）、`ContentRarity`（N/R/SR/SSR/UR/L 多选）。命中这些条件的数据排到列表前面，未命中的仍展示。
- **排序区**（多选排序键，按选择顺序定优先级 index0=主键，选中项上移到各自容器标题之后作优先级反馈）：`ContentData`「战斗」（伤害/击杀/承伤/经验）、`ContentOther`「其它」（阵容/同类）。

各区段显隐由 `listFilterType` **推导**：含 `Name`→名字区、`Level`→等级区、`Rarity`→稀有度区、含战斗维度(Damage/Kill/DamageReceived/Exp)→战斗区、含 `Lineup/Class`→其它区；`listFilterType` 为空则全显。单个「确认」按钮（节点 `Submit`/文本 `TextSubmit`，绑定基类 `DialogView.ui_Submit`，走重写 `SubmitOnClick`）回传 **`OrderFilterResultBean`**；点击背景关闭。**无正/倒序概念，排序方向由各调用方自行固定**。

```csharp
UIHandler.Instance.ShowDialogOrderFilter(
    ui_OrderBtn_Button.transform as RectTransform,            // 触发按钮：弹窗内容定位到它处
    OnConfirmOrderFilter,                                     // 确认回调 Action<OrderFilterResultBean>
    listFilterType,                                           // 开放哪些维度(决定区段显隐;null=全显)
    new List<OrderFilterTypeEnum>(currentFilter.sortTypes),   // 默认已选排序键(回填)
    currentFilter.nameFilter,                                 // 默认名字(回填)
    currentFilter.levelMin, currentFilter.levelMax,           // 默认等级区间(0/int.MaxValue=不限,输入框留空)
    new List<RarityEnum>(currentFilter.rarities));            // 默认选中稀有度(回填)

// 回传统一结果 Bean,调用方据此【命中置顶 + 排序键次级排序,不删行全部展示】
protected void OnConfirmOrderFilter(OrderFilterResultBean result) {
    // result.sortTypes(排序键,index0主键) / result.nameFilter / result.levelMin/levelMax / result.rarities
    // 命中判定: result.MatchName(name) && MatchLevel(level) && MatchRarity(rarity) → OrderByDescending(命中) 把命中项置顶
}
```

- `OrderFilterResultBean`（`Assets/Scripts/Bean/UI/`）自带 `MatchName/MatchLevel/MatchRarity` 便捷判定（对应条件为空即恒命中），调用方别重复实现。**语义=命中即置顶、不删行、全部展示**（不是过滤隐藏）。
- `OrderFilterTypeEnum`（`GameStateEnum.cs`）：`Rarity=1 / Level=2 / Lineup=3 / Name=4 / Class=5 / Damage=6 / Kill=7 / DamageReceived=8 / Exp=9`。注意：`Name`/`Level`/`Rarity` 现在是**命中置顶**条件（不进 `sortTypes`），只有战斗(Data)+其它(Other)里的维度才是排序键。
- 各区段标题多语言：名字`2000019` / 等级`2000020` / 稀有度`2000018` / 战斗`2000021` / 其它`2000022`；名字输入占位复用「输入名字...」`302`，等级左右输入占位「低等级...」`2000023`/「高等级...」`2000024`；确认按钮复用`1000001`。阵容/同类排序项名已去「排序：」前缀（`2000006`=阵容、`2000011`=同类）。
- **调用方各取所需**（按数据可得性，统一「命中置顶+全部展示」）：生物 `UIViewCreatureCardList`（名字+等级+稀有度命中置顶 + Lineup/Class 次级排序，正序固定，主列表 `listCreatureDataAll` 全量、`OrderByDescending(IsMatch)` 重排不删行）；道具 `UIViewItemBackpackList`（名字+稀有度命中置顶，无等级、无排序键，次按稀有度倒序，装备资格 `FilterItems` 仍为第一阶段硬过滤）；战斗结算 `UIFightSettlement`（仅战斗维度排序，固定倒序，无名字/等级/稀有度）。
- 相关文件：`UIDialogOrderFilter`、`DialogOrderFilterBean`、`OrderFilterResultBean`、`UIViewDialogOrderFilterItem`（项视图，排序键模式/稀有度多选模式，名称内联在 `NameItem`，无 POPUP）。

### ⚠️ 约定：筛选/排序按钮的 popup 详情统一用 UIText `2000014`

**凡是「打开 UIDialogOrderFilter 的筛选/排序按钮」，其 `PopupButtonCommonView` 悬浮详情一律使用多语言 `2000014`（`筛选排序` / `Sort & Filter`），不要再为每个按钮新建文本：**

```csharp
ui_OrderBtn_PopupButtonCommonView.SetData(TextHandler.Instance.GetTextById(2000014), PopupEnum.Text);
```

> `UIViewDialogOrderFilterItem.GetFilterName` 仍用旧文本作排序项内联名 `NameItem`：当前实际生效的是**阵容=2000006 / 同类=2000011 / 伤害=50001 / 击杀=50002 / 承伤=50004 / 经验=50003**（战斗区/其它区的排序键）。其中「排序：稀有度=2000004 / 排序：等级=2000005 / 排序：名字=2000007」对应的排序项已被移除（名字/等级改输入框、稀有度改 `RarityInfo.name_language` 多选），这几个文本不再被引用。「正序=2000012 / 倒序=2000013」已删除。

## 约束

- 弹窗继承 DialogView，使用 `UIDialog` 前缀命名
- 弹窗数据通过 DialogBean 传递
- Prefab 放置在 `Assets/Prefabs/UI/Dialog/`
- 新增弹窗需更新 MD/ProjectUI.md 文档
