---
name: game-item
description: 道具系统开发：道具创建/装备/使用、背包系统、道具商店、道具信息弹窗。
tools: Read, Write, Edit, Glob, Grep, Bash
watched_files:
  - Assets/Scripts/Bean/Game/ItemBean.cs
  - Assets/Scripts/Enums/ItemsEnum.cs
  - Assets/Scripts/Utils/ItemsUtil.cs
  - Assets/Scripts/Component/UI/Common/Item/
  - Assets/Scripts/Component/UI/Common/ItemSelect/
  - Assets/Scripts/Component/UI/Common/Backpack/
  - Assets/Scripts/Component/UI/Popup/ItemInfo/
---

# 道具系统 (Item System) 开发代理

你负责 [Scripts/](Assets/Scripts/) 中与道具相关的代码开发。

## 职责范围

### 道具数据
- **ItemBean / ItemBeanPartial** - 道具基础数据（运行时实例含 `rarity` 品质；`juicerExp` 魔汁经验值，仅 Juice 类型有效，旧存档无此字段默认 0 兼容）
- **ItemsEnum** - 道具枚举定义（`ItemTypeEnum`：装备部位 Hat=1~Weapon=10 + **消耗品 Juice=11（魔汁，非装备）**；`ItemIdEnum.Juice = 200001`）
- **ItemsInfoBean** - 道具配置信息（来自 Excel）
  - `reward_rarity`（string，逗号分隔稀有度ID，空=全稀有度）：**奖励可出稀有度白名单**。空表示该道具在任意稀有度奖励中都可能产出；配了(如 `5,6`)则仅在 UR/L 稀有度的奖励里出现。辅助方法在 `ItemsInfoBeanPartial`：`GetRewardRarityList()`（解析缓存）、`IsMatchRewardRarity(int rarity)`（空白名单→true）。注意与 `ItemBean.rarity`（运行时实例品质）语义不同。
  - 消费点：`RewardSelectBean.CreateItemEquip`（征服/传送门装备奖励池）先定目标稀有度→按 `IsMatchRewardRarity` 过滤道具池→随机取一件；过滤后为空回退发魔晶。**仅**作用于装备奖励生成，扭蛋/其它路径不受影响。
  - 编辑工具：菜单「游戏/道具稀有度配置」（`Assets/Editor/ItemRarityConfigEditorWindow.cs`）——虚拟化列表(图标懒加载)列出所有道具、同名相邻，右侧稀有度枚举勾选，保存写 Excel + 定向补丁 `ItemsInfo.txt` 的 `reward_rarity`。顶部支持名字搜索 + `item_type` 类型筛选 + **物种(creature_model_id→CreatureModel remark，0=通用)筛选**。新增该列后需在 Unity 对 ItemsInfo「生成 Entity」使 Bean 字段生效。

### 道具管理
- **ItemsUtil** - 道具工具类
- **GameDataHandler** / **GameDataManager** - 游戏数据处理（含道具持久化）

### 道具 UI（`Common/Item/`）
- **UIViewItem** - 道具项**基类**（公共字段 itemData + SetData/SetIcon/SetNum/SetItemBG/SetItemPopup/OnClickForButton；SetItemBG 按 itemData.rarity 用 RarityInfo.ui_board_color_item 给 ui_ItemBG 上色，空槽位/缺配置回退白色）
- **UIViewItemBackpack** - 背包道具项（`: UIViewItem`，加 creatureData + SetData(item,creature)；右键经 `ui_UIViewItem` 同物体 Button 旁的 PopupButtonCommonView 转发（Awake `AddListenerForRightClick` → `EventForRightClick`，itemData 非空时触发 `EventsInfo.UIViewItemBackpack_OnRightClickSelect` 并 `ClearData` 隐藏悬浮详情；左键仍走 Button.onClick → `OnClickForSelect` → `UIViewItemBackpack_OnClickSelect`）
- **UIViewItemEquip** - 装备项（`: UIViewItem`，加 itemTypeEnum + 空槽位占位图标/部位名）
- **UIViewItemBackpackList** - 背包列表（在 `Common/Backpack/`）
- **UIViewStoreItem** - 商店道具项
- **UIPopupItemInfo** - 道具信息气泡

### 道具选项控件（`Common/ItemSelect/`）
- **UIViewItemSelect** - 道具选项通用控件（prefab `Resources/UI/Common/UIViewItemSelect.prefab`，内嵌送礼/丢弃/装备三个 `UIViewItemSelectChild` 按钮）：`SetData(actionForGift/Delete/Equip)` **传入回调即显示对应按钮、为空隐藏**，业务全由使用方回调处理；`ShowSelect(itemData, targetTF)` 记录选中道具并用 `UGUIUtil.GetRootPos` 把选项列表定位到目标处；点全屏透明背景或任意选项均 `CloseSelect`，点选项先关闭再以 `Action<ItemBean>` 回调。使用方：`UIDialogSelectItem`（按 Bean 回调显隐）、`UICreatureManager`（右键弹出，只显示装备+丢弃）

### 道具相关 UI
- **UIDialogSelectItem** - 道具选择弹窗（内嵌 `UIViewItemSelect`，选项显隐由 `DialogSelectItemBean` 回调是否传入决定）

### 魔汁（Juice，首个消耗品类道具）

道具类型不再只有装备部位 + 头像：**魔汁是首个消耗品**（`ItemTypeEnum.Juice = 11`，紧随 Weapon=10），由榨汁产出、对魔物使用加经验。

- **数据**：`ItemBean.juicerExp`（long 实例字段）存每个魔汁的经验值，榨汁时按投入魔物等级汇总写入（产出端 `CreatureJuicerLogic.SettleJuiceReward`，详见 juicer-system）；旧存档无此字段 JSON 反序列化默认 0 兼容。
- **配置**：excel_items_info 新行 id=200001（item_type=11、`num_max=1` 不堆叠——每个魔汁实例经验不同故不入堆、creature_model_id=0、icon_res=`Item_Juicer_1` 无图集后缀走默认 Items 图集、name textId=200001）。入账走 `userData.AddBackpackItem(itemBean)` 不堆叠重载（每个魔汁是独立 ItemBean）。
- **使用流程**（魔物管理页 `UICreatureManager`）：`EventForItemBackpackClickSelect` 点击分流——Juice 类型 → `UseJuiceItem(itemData)`（`#region 魔汁使用`），其余道具照旧 `SetCreatureEquip`。`UseJuiceItem`：无选中生物/魔王兜底返回（列表已隐藏魔汁）→ `IsMaxLevel()` 满级 Toast 61015 拦截 → `UIHandler.ShowDialogNormal` 确认框（textId 61014，格式化生物名+juicerExp）→ 确定回调：`creatureData.levelExp += juicerExp` → `RemoveBackpackItem` → `SaveUserData()` → 三连刷新（`SetCardDetails` 经验显示 + `RefreshSacrificeButton` 献祭按钮点亮 + `InitBackpackItemsData` 列表移除）。**经验只累计 levelExp 不自动升级**（沿用战斗结算加经验语义，升级仍走献祭 CanUpLevel/UpLevelForSacrifice）。
- **列表过滤**：`UIViewItemBackpackList.FilterItems` 保留条件 = `creatureInfo.CanEquipItem` 或（`GetItemType()==Juice` 且 `!creatureData.IsDemonLord()`）——选中魔王时魔汁在管理页列表隐藏，普通魔物可见；`UIDialogSelectItem`（creatureData=null）显示全部不受影响。
- **气泡**：`UIPopupItemInfo.SetJuiceExp`（SetData 末尾调用）——Juice 类型显示 `ui_JuiceExpText` 并填 textId 61017「经验+{0}」，其余道具隐藏；字段经 AutoLinkUI 按名绑定（prefab Details 节点下 `JuiceExpText`，默认隐藏），为 null 容错跳过；魔汁 dicAttribute 为空故属性区自动隐藏，两者互斥。
- **相关配置**：LevelInfo 新增 `juicer_exp` 列（1~10 级 = 同级升级经验 100%，另有 id=0 行=20=1级的20%）；excel_language UIText sheet 新增 61014/61015/61016/61017（12 语种），ItemsInfo sheet 新增 id=200001「魔汁」。

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
