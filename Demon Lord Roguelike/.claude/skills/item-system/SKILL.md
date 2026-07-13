---
name: item-system
description: Demon Lord Roguelike 游戏的道具系统开发指南。使用此SKILL当需要创建或修改道具(Item)、装备(Equip)、背包(Backpack)相关的代码，包括道具数据结构、装备系统、背包管理、道具配置等。
watched_files:
  - Assets/Scripts/Enums/ItemsEnum.cs
  - Assets/Scripts/Bean/Game/ItemBean.cs
  - Assets/Scripts/Bean/MVC/Game/ItemsInfoBean.cs
  - Assets/Scripts/Bean/MVC/Game/ItemsTypeBean.cs
  - Assets/Scripts/Utils/ItemsUtil.cs
  - Assets/Scripts/Bean/MVC/UserDataBean.cs
  - Assets/Scripts/Bean/Game/CreatureBean.cs
  - Assets/Scripts/Component/UI/Common/Item/
  - Assets/Scripts/Component/UI/Common/Backpack/
---

# 道具系统开发指南

## 核心概念

### 道具数据结构

```
ItemBean          - 运行时道具实例数据（包含数量、品质、随机属性）
ItemsInfoBean     - 道具配置数据（来自配置表）
ItemsTypeBean     - 道具类型配置
```

### 道具类型枚举

```csharp
// 装备部位类型
ItemTypeEnum
├── Hat = 1          // 帽子
├── Clothes = 2      // 衣服
├── Pants = 3        // 裤子
├── Shoe = 4         // 鞋子
├── NoseRing = 5     // 鼻环
├── FingerRing = 6   // 指环
└── Weapon = 10      // 武器

// 武器子类型
ItemTypeWeaponEnum
├── Staff = 1        // 法杖
├── OneHanded = 2    // 单手剑
├── TwoHanded = 3    // 双手剑
├── SwordAndShield = 4 // 刀盾
├── GreatSword = 5   // 大剑
├── GreatShield = 6  // 大盾
├── Bow = 7          // 弓
└── Thrown = 8       // 投掷物

// 道具使用者类型
ItemUserTypeEnum
├── Default = 0      // 默认 所有生物可用
└── DemonLord = 1    // 魔王专属
```

## 创建新道具类型

### 1. 添加道具ID到枚举

```csharp
// Assets/Scripts/Enums/ItemsEnum.cs
public enum ItemIdEnum
{
    Crystal = 1,           // 魔晶
    // 新增道具ID
    NewItem = 100001,
}
```

### 2. 添加道具类型（如需新装备部位）

```csharp
// 在 ItemTypeEnum 中添加
public enum ItemTypeEnum
{
    // ... 现有类型
    NewEquipSlot = 7,  // 新装备部位
}
```

### 3. 配置道具数据

道具配置表字段（ItemsInfo）:
- `id` - 道具唯一ID
- `item_type` - 道具类型(1帽子 2衣服 3裤子 4鞋子 5鼻环 6戒指 10武器 1000魔晶)
- `item_weapon_type` - 武器类型（仅武器有效）
- `num_max` - 道具堆叠上限
- `creature_model_id` - 生物模型ID（装备外观）
- `creature_model_info_id` - 生物模型详细信息ID
- `icon_res` - 图标资源路径（格式 `名字` 或 `名字,图集Tag`，默认图集 Items）
- `icon_rotate_z` - 图标旋转角度
- `attack_mode_data` - 攻击模式数据
- `other_data` - 其他数据
- `name` - 文本表ID
- `remark` - 备注
- `reward_rarity` - **奖励可出稀有度白名单**（string，逗号分隔稀有度ID，空=全稀有度适配）

### reward_rarity 奖励稀有度白名单

`reward_rarity` 声明「本道具可作为哪些稀有度的奖励产出」：**空/未配置 = 全稀有度适配**（任意稀有度奖励都可能出）；配了(如 `5,6`)则**只在** UR/L 稀有度的奖励里出现。稀有度ID：N=1 R=2 SR=3 SSR=4 UR=5 L=6。

- 辅助方法在 [ItemsInfoBeanPartial.cs](Assets/Scripts/Bean/MVC/Game/ItemsInfoBeanPartial.cs)：
  - `GetRewardRarityList()` — 解析逗号串为 `List<int>`（结果缓存 `listRewardRarityCache`）。
  - `IsMatchRewardRarity(int rarity)` — 白名单为空→`true`（全适配）；否则须包含该稀有度。
- **消费点（唯一）**：[RewardSelectBean.cs](Assets/Scripts/Bean/Game/RewardSelectBean.cs) 的 `CreateItemEquip`。流程改为：**先**确定目标稀有度(`conquerInfo.reward_equip_rarity`/测试数据/默认)→按 `IsMatchRewardRarity(rarityItem)` 过滤道具池→从匹配道具中随机取一件；过滤后为空则回退发魔晶(与"无相关道具"一致)。**仅**作用于征服/传送门装备奖励池，扭蛋(生物)/其它路径不受影响。
- ⚠️ `ItemsInfoBean.cs` 自动生成且被 Hook 拦截；`reward_rarity` 列已加进 Excel，需在 Unity 对 ItemsInfo「生成 Entity」使 Bean 字段落地后代码才编译通过。

### 道具稀有度配置编辑器（游戏/道具稀有度配置）

菜单「游戏/道具稀有度配置」→ [ItemRarityConfigEditorWindow.cs](Assets/Editor/ItemRarityConfigEditorWindow.cs)，用于可视化配置 `reward_rarity`：

- 左侧**虚拟化列表**(仅渲染可视范围行 + 上下 `GUILayout.Space` 占位，`RowHeight=42`)列出全部道具，**图标懒加载**(`iconCache`，优先 `Assets/LoadResources/Textures/Items/{名}.png`，回退 `AssetDatabase.FindAssets`)，**同名道具相邻**(按名字→id 排序，同名分组交替底色)。
- 顶部支持按名字搜索、按 `item_type` 类型筛选、按**物种(生物模组)筛选**（`creature_model_id` → `CreatureModel.txt` 的 remark，0=通用；每行副标题显示 `id + 物种名 + 类型名(id)`）。
- **稀有度统计/筛选条**：每个稀有度显示「可产出该稀有度的道具数」，其中**全适配(空白名单)道具计入每个稀有度 +1**（与 `IsMatchRewardRarity` 语义一致）；点击某稀有度即一键筛选出所有「含该稀有度或全适配」的道具，再点或点「全部」取消。统计基于 `baseRows`（类型/物种/名字前置过滤内），数字不随稀有度筛选变化；稀有度筛选叠加在前置过滤之上。
- 右侧每行 6 个稀有度**枚举勾选**(N/R/SR/SSR/UR/L)，"清空"按钮=恢复全稀有度适配。
- 保存：`ExcelUtil.SetExcelData` 写回 Excel(唯一真实源) + 定向补丁 `ItemsInfo.txt` 的 `reward_rarity`(只改该字段，避开 `name[language]` 特殊处理)。名字取多语言 `Language_ItemsInfo_cn.txt`(id→content)回退 remark。

## 装备系统

### 装备道具到生物

```csharp
// 检查是否可装备
ItemsInfoBean itemInfo = ItemsInfoCfg.GetItemData(itemId);
bool canEquip = itemInfo.CanEquipForCreature(creatureInfo);

// 装备道具
CreatureBean creature = new CreatureBean(creatureId);
creature.ChangeEquip(ItemTypeEnum.Hat, itemBean);

// 卸下装备到背包
creature.RemoveAllEquipToBackpack();

// 获取装备属性加成
float bonus = creature.GetEquipAttribute(CreatureAttributeTypeEnum.ATK);
```

### 生物可装备类型配置

```csharp
// CreatureInfoBean 中配置
equip_items_type = "1,2,3,4,5,10"  // 可装备：帽子、衣服、裤子、鞋子、鼻环、武器
equip_items_weapon_type = 0        // 0表示可使用所有武器类型
```

### 能否装备的判定规则（CanEquipItem / CanEquipForCreature）

统一入口 `CreatureInfoBean.CanEquipItem(itemInfo)`（对称的 `ItemsInfoBean.CanEquipForCreature(creatureInfo)` 逻辑一致），按顺序做三重校验，全部通过才可装备：

1. **道具类型匹配**：道具 `ItemTypeEnum` 须在生物 `equip_items_type` 列表内。
2. **种族模组匹配**：装备 `creature_model_id` 为 0 表示通用装备（任何种族可装）；否则须与生物 `model_id` 相等（如人类不能装备史莱姆专属装备）。
3. **武器子类型匹配**（仅当道具为武器）：生物 `equip_items_weapon_type` 为 0 表示通配所有武器；否则须与武器 `item_weapon_type` 相等。

> `UIViewItemBackpackList.FilterItems` 与 `UICreatureManager.SetCreatureEquip` 均走此统一入口，故列表展示与装备操作的资格判断一致，改判定只需改这两个 Partial。

## 背包管理

```csharp
UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();

// 添加道具到背包
userData.AddBackpackItem(new ItemBean(itemId, num));
userData.AddBackpackItem(itemBean);

// 移除道具
userData.RemoveBackpackItem(itemBean);

// 访问背包(列表已包裹进 UserBackpackItemsBean，经访问器取列表)
List<ItemBean> backpack = userData.GetUserBackpackItemsData().listBackpackItems;
```

## 道具随机属性系统

道具创建时可生成随机属性，基于品质等级：

```csharp
ItemBean item = new ItemBean(itemId, num, rarity: 3, userType: 0);
item.InitRandomAttributeForCreate(addNum: 1);

// 获取属性
float hpBonus = item.GetAttribute(CreatureAttributeTypeEnum.HP);
```

属性加成规则:
- `userType=0` (默认): 随机 HP/DR/ATK/ASPD
- `userType=1` (魔王专属): 固定 MSPD/MP
- 属性条数 = 品质等级 (rarity)

## 武器攻击模式配置

武器攻击模式通过 `attack_mode_data` 字段配置:

```
ShowSprite=icon_name,VertexRotateAxis=0,0,-1,VertexRotateSpeed=10,UVRotateSpeed=0,StartPosition=0,0,0,StartSize=1
```

字段说明:
- `ShowSprite` - 攻击特效精灵
- `VertexRotateAxis` - 模型旋转轴
- `VertexRotateSpeed` - 模型旋转速度
- `UVRotateSpeed` - UV旋转速度
- `StartPosition` - 初始位置偏移
- `StartSize` - 初始大小

## 常用代码模板

### 创建带品质的装备

```csharp
public ItemBean CreateEquipment(long itemId, int rarity)
{
    var item = new ItemBean(itemId, 1, rarity);
    item.InitRandomAttributeForCreate(1);
    return item;
}
```

### 筛选背包道具

```csharp
// 按类型筛选
List<ItemBean> hats = userData.GetUserBackpackItemsData().listBackpackItems
    .Where(item => item.GetItemType() == ItemTypeEnum.Hat)
    .ToList();

// 按品质筛选
List<ItemBean> rareItems = userData.GetUserBackpackItemsData().listBackpackItems
    .Where(item => item.rarity >= (int)RarityEnum.SR)
    .ToList();
```

### 空列表提示

`UIViewItemBackpackList` 含一个 `UIViewNullText`（挂 `UITextLanguageView`+`TextMeshProUGUI`）。`SetData` 末尾调用 `RefreshNullText()`：过滤后 `listFilterItems` 为空时显示「没有相关道具」（UIText **2000015**），非空则隐藏。`textId` 在代码里设置（非 prefab 写死）。生物卡片列表 `UIViewCreatureCardList` 同理，空时显示「没有相关魔物」（UIText **2000016**）。

### 排序筛选弹窗（含道具类型筛选）

`UIViewItemBackpackList` 的排序按钮走通用 [order-filter-system](../order-filter-system/SKILL.md)（`UIHandler.ShowDialogOrderFilter`）。道具列表开放 **名字模糊 + 稀有度多选**；**当有生物上下文**（`creatureData` 非空，如生物管理界面 `UICreatureManager`）时额外开放 **道具类型多选**（`OrderFilterTypeEnum.ItemType`），选项即该魔物的 `GetEquipItemsType()`（预制预留 5 项，可装备类型 <5 隐藏多余项）；无生物上下文（如 `UIDialogSelectItem`）不显示该维度。均为「命中即置顶」语义（`OrderFilterResultBean.MatchName/MatchRarity/MatchItemType`），**不删行**、次按稀有度倒序。

### 装备比较

```csharp
public bool IsBetterEquipment(ItemBean newItem, ItemBean currentItem)
{
    if (currentItem == null) return true;
    if (newItem.rarity > currentItem.rarity) return true;
    // 自定义比较逻辑...
    return false;
}
```

## 道具项 UI 基类（UIViewItem）

道具格 UI 统一在 `Assets/Scripts/Component/UI/Common/Item/`，采用继承：

```
UIViewItem (基类 : BaseUIView)         公共 itemData + SetData/SetIcon/SetNum/SetItemBG/SetItemPopup/OnClickForButton
├── UIViewItemEquip   : UIViewItem     装备槽位：itemTypeEnum + SetData(ItemTypeEnum)，空槽位重写 SetIcon/SetItemPopup 显部位图标/名
└── UIViewItemBackpack : UIViewItem    背包格：creatureData + SetData(item,creature)，行为==基类
```

关键约定（改动这块务必遵守）：

- **自动绑定靠嵌套预制体**：`UIViewItem.prefab` 被嵌套进 Equip/Backpack 两个 prefab（命名 `UIViewItem`）。`AutoLinkUI` 递归按子物体名绑定，子类反射继承基类 `ui_*` 字段（`ui_ItemIcon`/`ui_ItemNum`/`ui_UIViewItem` popup），从嵌套预制体里取到引用。**嵌套物体名 `UIViewItem`/`ItemIcon`/`ItemNum` 不能改**。
- **点击按钮判定**：基类 `OnClickForButton` 用 `viewButton.gameObject == ui_UIViewItem.gameObject` 判定（按钮与 popup 同物体）。
- **`OnClickForSelect` 必须留在子类**：消费方用 `RegisterEvent<UIViewItemEquip>`/`<UIViewItemBackpack>` 按具体类型订阅，`TriggerEvent(..., this)` 的 `this` 必须是具体子类，不能上提到基类。
- **数量背景**：基类 Component 无 `ui_ItemNumBg`，`SetNum` 用 `ui_ItemNum.transform.parent`（即 ItemNumBg）开关。
- **稀有度底框**：基类含 `ui_ItemBG`（Image），`SetData` 里调 `SetItemBG(itemData)` 按稀有度上色——取 `RarityInfoCfg.GetItemData(itemData.rarity).ui_board_color_item`（道具专用**单色**，非 `ui_board_color` 逗号渐变）经 `ColorUtil.ParseHtmlString` 解析；`itemData==null`（空槽位）或缺配置回退 `Color.white`。`ui_ItemBG` 为 `null`（旧 prefab 未接）时直接跳过，向后兼容。
- **⚠️ 不要对子类 prefab 重跑 UI 自动生成工具**：工具不感知继承，会重生成 `UIViewItemEquipComponent`/`BackpackComponent` 带回 `ui_ItemIcon` 等公共字段，与基类字段重名隐藏。子类字段一律走继承（这两个 Component 文件已删除）。

## 相关事件

```csharp
// 背包道具变化
EventHandler.Instance.TriggerEvent(EventsInfo.Backpack_Item_Change);

// 道具点击（按具体子类类型订阅）
EventsInfo.UIViewItemBackpack_OnClickSelect   // RegisterEvent<UIViewItemBackpack>
EventsInfo.UIViewItemEquip_OnClickSelect      // RegisterEvent<UIViewItemEquip>
```

## 文件位置速查

| 功能 | 文件路径 |
|------|----------|
| 道具枚举 | `Assets/Scripts/Enums/ItemsEnum.cs` |
| 道具数据Bean | `Assets/Scripts/Bean/Game/ItemBean.cs` |
| 道具配置Bean | `Assets/Scripts/Bean/MVC/Game/ItemsInfoBean.cs` |
| 道具类型配置 | `Assets/Scripts/Bean/MVC/Game/ItemsTypeBean.cs` |
| 道具工具类 | `Assets/Scripts/Utils/ItemsUtil.cs` |
| 背包管理 | `Assets/Scripts/Bean/MVC/UserDataBean.cs` |
| 生物装备 | `Assets/Scripts/Bean/Game/CreatureBean.cs` |
| 道具项UI(基类/装备/背包) | `Assets/Scripts/Component/UI/Common/Item/` |
| UI背包列表组件 | `Assets/Scripts/Component/UI/Common/Backpack/` |
