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
- **ItemBean / ItemBeanPartial** - 道具基础数据（运行时实例含 `rarity` 品质）
- **ItemsEnum** - 道具枚举定义
- **ItemsInfoBean** - 道具配置信息（来自 Excel）
  - `reward_rarity`（string，逗号分隔稀有度ID，空=全稀有度）：**奖励可出稀有度白名单**。空表示该道具在任意稀有度奖励中都可能产出；配了(如 `5,6`)则仅在 UR/L 稀有度的奖励里出现。辅助方法在 `ItemsInfoBeanPartial`：`GetRewardRarityList()`（解析缓存）、`IsMatchRewardRarity(int rarity)`（空白名单→true）。注意与 `ItemBean.rarity`（运行时实例品质）语义不同。
  - 消费点：`RewardSelectBean.CreateItemEquip`（征服/传送门装备奖励池）先定目标稀有度→按 `IsMatchRewardRarity` 过滤道具池→随机取一件；过滤后为空回退发魔晶。**仅**作用于装备奖励生成，扭蛋/其它路径不受影响。
  - 编辑工具：菜单「游戏/道具稀有度配置」（`Assets/Editor/ItemRarityConfigEditorWindow.cs`）——虚拟化列表(图标懒加载)列出所有道具、同名相邻，右侧稀有度枚举勾选，保存写 Excel + 定向补丁 `ItemsInfo.txt` 的 `reward_rarity`。顶部支持名字搜索 + `item_type` 类型筛选 + **物种(creature_model_id→CreatureModel remark，0=通用)筛选**。新增该列后需在 Unity 对 ItemsInfo「生成 Entity」使 Bean 字段生效。

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
| 道具配置Bean(含 reward_rarity 辅助) | Assets/Scripts/Bean/MVC/Game/ItemsInfoBeanPartial.cs |
| 道具稀有度配置编辑器 | Assets/Editor/ItemRarityConfigEditorWindow.cs |

## 约束

- 新增道具类型需在 ItemsEnum 中添加枚举
- 道具数据变更后需刷新相关 UI
- 道具弹出信息使用 Popup 类型 UI
