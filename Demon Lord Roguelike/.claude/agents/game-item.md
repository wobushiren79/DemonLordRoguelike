---
name: game-item
description: 道具系统开发：道具创建/装备/使用、背包系统、道具商店、道具信息弹窗。
tools: Read, Write, Edit, Glob, Grep, Bash
watched_files:
  - Assets/Scripts/Bean/Game/ItemBean.cs
  - Assets/Scripts/Enums/ItemsEnum.cs
  - Assets/Scripts/Utils/ItemsUtil.cs
  - Assets/Scripts/Component/UI/Common/Item/
  - Assets/Scripts/Component/UI/Common/Backpack/
  - Assets/Scripts/Component/UI/Popup/ItemInfo/
---

# 道具系统 (Item System) 开发代理

你负责 [Scripts/](Assets/Scripts/) 中与道具相关的代码开发。

## 职责范围

### 道具数据
- **ItemBean / ItemBeanPartial** - 道具基础数据
- **ItemsEnum** - 道具枚举定义
- **ItemsInfoBean** - 道具配置信息（来自 Excel）

### 道具管理
- **ItemsUtil** - 道具工具类
- **GameDataHandler** / **GameDataManager** - 游戏数据处理（含道具持久化）

### 道具 UI（`Common/Item/`）
- **UIViewItem** - 道具项**基类**（公共字段 itemData + SetData/SetIcon/SetNum/SetItemBG/SetItemPopup/OnClickForButton；SetItemBG 按 itemData.rarity 用 RarityInfo.ui_board_color_item 给 ui_ItemBG 上色，空槽位/缺配置回退白色）
- **UIViewItemBackpack** - 背包道具项（`: UIViewItem`，加 creatureData + SetData(item,creature)）
- **UIViewItemEquip** - 装备项（`: UIViewItem`，加 itemTypeEnum + 空槽位占位图标/部位名）
- **UIViewItemBackpackList** - 背包列表（在 `Common/Backpack/`）
- **UIViewStoreItem** - 商店道具项
- **UIPopupItemInfo** - 道具信息气泡

### 道具相关 UI
- **UIDialogSelectItem** - 道具选择弹窗

## 关键文件

| 文件 | 路径 |
|------|------|
| ItemBean | Assets/Scripts/Bean/Game/ItemBean.cs |
| ItemsEnum | Assets/Scripts/Enums/ItemsEnum.cs |
| ItemsUtil | Assets/Scripts/Utils/ItemsUtil.cs |
| 道具项（基类+装备+背包） | Assets/Scripts/Component/UI/Common/Item/ |
| 背包列表 | Assets/Scripts/Component/UI/Common/Backpack/ |
| 道具信息气泡 | Assets/Scripts/Component/UI/Popup/ItemInfo/ |

## 约束

- 新增道具类型需在 ItemsEnum 中添加枚举
- 道具数据变更后需刷新相关 UI
- 道具弹出信息使用 Popup 类型 UI
