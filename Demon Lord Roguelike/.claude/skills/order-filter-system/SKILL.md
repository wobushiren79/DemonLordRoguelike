---
name: order-filter-system
description: Demon Lord Roguelike 游戏的排序筛选弹窗(OrderFilter/筛选/排序)系统开发指南。使用此SKILL当需要创建或修改列表的筛选排序功能、给某个列表接入「筛选排序」按钮、新增筛选/排序维度、调整区段(名字模糊/等级区间/稀有度多选/战斗数据排序键/其它排序键)、「命中即置顶 + 排序键次级排序」语义、弹窗按钮定位等,包括 UIDialogOrderFilter、UIViewDialogOrderFilterItem、DialogOrderFilterBean、OrderFilterResultBean(MatchName/MatchLevel/MatchRarity)、OrderFilterTypeEnum、UIHandler.ShowDialogOrderFilter、调用方(生物卡列表/背包列表/战斗结算)接入模式等。
watched_files:
  - Assets/Scripts/Component/UI/Dialog/UIDialogOrderFilter.cs
  - Assets/Scripts/Component/UI/Dialog/UIDialogOrderFilterComponent.cs
  - Assets/Scripts/Component/UI/Dialog/OrderFilter/
  - Assets/Scripts/Bean/UI/DialogOrderFilterBean.cs
  - Assets/Scripts/Bean/UI/OrderFilterResultBean.cs
---

# 排序筛选弹窗系统开发指南

## 核心概念

一个**通用**的列表「筛选 + 排序」弹窗:任意列表在排序按钮处弹出它,玩家配置条件后,弹窗回传一个 `OrderFilterResultBean`,由**调用方自己**据此重排列表。弹窗只负责收集条件、不碰列表数据。

两类语义(贯穿全系统,务必区分):

- **命中即置顶条件**(名字 / 等级 / 稀有度):**不删行、全部展示**,命中项浮到列表最前,未命中项排其后。判定用 `OrderFilterResultBean.Match*`。
- **排序键**(战斗数据 / 其它):**多选**,按**选择顺序定优先级**(index0=主键),作为命中分组后的**次级排序**。

```
列表 View(生物卡/背包/战斗结算)
  │ 点排序按钮 → 组装 listFilterType + 回填当前条件
  ▼
UIHandler.ShowDialogOrderFilter(targetButton, onConfirm, listFilterType, selectFilterTypes, name, levelMin, levelMax, rarities)
  ▼
UIDialogOrderFilter (DialogView, 点背景关闭)
  ├─ 名字区  ContentName   (Name)        模糊输入
  ├─ 等级区  ContentLevel  (Level)       左/右整数→区间[min,max]
  ├─ 稀有度区 ContentRarity (Rarity)      6 档多选
  ├─ 战斗区  ContentData   (Damage/Kill/DamageReceived/Exp)  排序键多选→选中上移到最前
  └─ 其它区  ContentOther  (Lineup/Class)                    排序键多选→保持原始顺序(不上移)
  │ 各项 = UIViewDialogOrderFilterItem(SetData 排序键模式 / SetDataForRarity 稀有度模式)
  ▼ 点确认 Confirm()
OrderFilterResultBean { sortTypes(优先级), nameFilter, levelMin, levelMax, rarities }
  ▼ actionForConfirm 回传
调用方 RefreshFilterSortList():OrderByDescending(IsMatch).ThenBy(每个 sortType 的键选择器)
```

## 维度速查(OrderFilterTypeEnum,[GameStateEnum.cs](Assets/Scripts/Enums/GameStateEnum.cs))

| 枚举 | 值 | 区段 | 语义 | 项名多语言id |
|---|---|---|---|---|
| Rarity | 1 | 稀有度区 | 命中置顶(多选) | 2000004 |
| Level | 2 | 等级区 | 命中置顶(区间) | 2000005 |
| Lineup | 3 | 其它区 | 排序键(阵容序) | 2000006 |
| Name | 4 | 名字区 | 命中置顶(模糊) | 2000007 |
| Class | 5 | 其它区 | 排序键(同id归并) | 2000011 |
| Damage | 6 | 战斗区 | 排序键 | 50001 |
| Kill | 7 | 战斗区 | 排序键 | 50002 |
| DamageReceived | 8 | 战斗区 | 排序键 | 50004 |
| Exp | 9 | 战斗区 | 排序键 | 50003 |

> 区段显隐由 `listFilterType` 推导:含某维度才显示其所属区段;传 `null`/空则全部显示。区段归属硬编码在 [UIDialogOrderFilter.dataTypes/otherTypes](Assets/Scripts/Component/UI/Dialog/UIDialogOrderFilter.cs#L17-L20) 与各 `InitXxxSection`。

## 给一个列表接入筛选排序

参照 [UIViewCreatureCardList](Assets/Scripts/Component/UI/Common/CreatureCard/UIViewCreatureCardList.cs) 的 5 步:

1. **持有当前条件**:`protected OrderFilterResultBean currentFilter = new OrderFilterResultBean();`(空=无条件)。
2. **排序按钮**:`OnClickForButton` 里调自己的 `ShowOrderFilterDialog()`;按钮的 `PopupButtonCommonView` 悬浮详情统一用多语言 **2000014**(筛选排序)。
3. **弹窗**:`UIHandler.Instance.ShowDialogOrderFilter(按钮RectTransform, OnConfirm, listFilterType, currentFilter.sortTypes, currentFilter.nameFilter, levelMin, levelMax, currentFilter.rarities)`,其中 `listFilterType` 只列出本列表开放的维度。
4. **确认回调**:`OnConfirm(result)` → `currentFilter = result ?? new OrderFilterResultBean()` → `RefreshFilterSortList()`。
5. **重排(不删行)**:
   ```
   var ordered = listData.OrderByDescending(x => IsMatch(x, filter));      // 命中置顶
   foreach (var t in filter.sortTypes) ordered = ordered.ThenBy(KeySel(t)); // 排序键次级、index0最先
   ```
   `IsMatch = filter.MatchName(名字) && filter.MatchLevel(等级) && filter.MatchRarity(稀有度)`;`KeySel` 把每个 `OrderFilterTypeEnum` 映射到该数据类型的可比较键(见调用方各自的 `GetOrderKeySelector`)。

## 新增一个维度的改动清单

1. [OrderFilterTypeEnum](Assets/Scripts/Enums/GameStateEnum.cs#L225) 加枚举值。
2. 决定区段归属:加入 `UIDialogOrderFilter.dataTypes` 或 `otherTypes`(排序键类),或新建区段(命中置顶类,仿 `InitNameSection`/`InitLevelSection`/`InitRaritySection` + 对应 `ui_Content*` + `Confirm()` 汇总)。
3. 战斗/其它区新增项:在 `InitSortSections` 里 `RegisterSortItem(新枚举, ui_新项, listFilterType)`。
4. [UIViewDialogOrderFilterItem.GetFilterName](Assets/Scripts/Component/UI/Dialog/OrderFilter/UIViewDialogOrderFilterItem.cs#L85) 加多语言分支。
5. 每个调用方的 `GetOrderKeySelector`(排序键)或 `OrderFilterResultBean.Match*`(命中置顶)加对应逻辑;并把新维度加进各列表的 `listFilterType`。
6. 多语言文本走 [localization-system](.claude/skills/localization-system/SKILL.md);区段显隐/上移行为见下「关键约定」。

## 关键约定

- **选中上移**:`RefreshSortItemOrder` → `ReorderSelectedFirst` 把已选排序键上移到容器最前表达优先级。**仅战斗区**这样做;**其它区保持原始(预制体)顺序**(只刷新布局,不上移)。这只影响视觉,`sortTypes` 的优先级语义不变。
- **弹窗定位**:`RefreshDialogContentPosition` 按鼠标所在屏幕象限设 `pivot`、对齐到触发按钮;`isDestroyBG=true` 点背景即关。
- **等级输入**:左不大于右、负数夹 0,输入完成与确认时各兜底一次(`OnLevelLeftEndEdit`/`OnLevelRightEndEdit`/`Confirm`)。
- **现有调用方**:生物卡列表 [UIViewCreatureCardList](Assets/Scripts/Component/UI/Common/CreatureCard/UIViewCreatureCardList.cs)、背包列表 [UIViewItemBackpackList](Assets/Scripts/Component/UI/Common/Backpack/UIViewItemBackpackList.cs)、战斗结算 [UIFightSettlement](Assets/Scripts/Component/UI/Game/FightSettlement/UIFightSettlement.cs)。

## 文件速查

| 文件 | 职责 |
|---|---|
| [UIDialogOrderFilter.cs](Assets/Scripts/Component/UI/Dialog/UIDialogOrderFilter.cs) | 弹窗逻辑:区段初始化/显隐、选中切换、上移、确认汇总、定位 |
| [UIDialogOrderFilterComponent.cs](Assets/Scripts/Component/UI/Dialog/UIDialogOrderFilterComponent.cs) | AutoLinkUI 字段(ui_ContentName/Level/Rarity/Data/Other 及各项/输入框) |
| [OrderFilter/UIViewDialogOrderFilterItem.cs](Assets/Scripts/Component/UI/Dialog/OrderFilter/UIViewDialogOrderFilterItem.cs) | 单项:排序键/稀有度双模式、选中勾、点击回传、项名多语言 |
| [DialogOrderFilterBean.cs](Assets/Scripts/Bean/UI/DialogOrderFilterBean.cs) | 入参:listFilterType/默认值/actionForConfirm |
| [OrderFilterResultBean.cs](Assets/Scripts/Bean/UI/OrderFilterResultBean.cs) | 回传结果 + Match* 便捷判定 |
| [UIHandler.ShowDialogOrderFilter](Assets/Scripts/Component/Handler/UIHandler.cs) | 弹窗入口(Bean 版 + 便捷参数版重载) |
| UIDialogOrderFilter.prefab / OrderFilter/UIViewDialogOrderFilterItem.prefab | 弹窗与单项预制体 |
