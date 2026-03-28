---
name: item-system
description: Demon Lord Roguelike 游戏的道具系统开发指南。使用此SKILL当需要创建或修改道具(Item)、装备(Equip)、背包(Backpack)相关的代码，包括道具数据结构、装备系统、背包管理、道具配置等。
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
// Assets/Scrpits/Enums/ItemsEnum.cs
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
- `item_type` - 道具类型(1帽子 2衣服 3裤子 4鞋子 5鼻环 10武器 1000魔晶)
- `item_weapon_type` - 武器类型（仅武器有效）
- `num_max` - 道具堆叠上限
- `creature_model_id` - 生物模型ID（装备外观）
- `creature_model_info_id` - 生物模型详细信息ID
- `icon_res` - 图标资源路径
- `icon_rotate_z` - 图标旋转角度
- `attack_mode_data` - 攻击模式数据
- `name` - 文本表ID
- `remark` - 备注

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

## 背包管理

```csharp
UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();

// 添加道具到背包
userData.AddBackpackItem(new ItemBean(itemId, num));
userData.AddBackpackItem(itemBean);

// 移除道具
userData.RemoveBackpackItem(itemBean);

// 访问背包
List<ItemBean> backpack = userData.listBackpackItems;
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
List<ItemBean> hats = userData.listBackpackItems
    .Where(item => item.GetItemType() == ItemTypeEnum.Hat)
    .ToList();

// 按品质筛选
List<ItemBean> rareItems = userData.listBackpackItems
    .Where(item => item.rarity >= (int)RarityEnum.SR)
    .ToList();
```

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

## 相关事件

```csharp
// 背包道具变化
EventHandler.Instance.TriggerEvent(EventsInfo.Backpack_Item_Change);

// 背包道具点击
EventsInfo.UIViewItemBackpack_OnClickSelect
```

## 文件位置速查

| 功能 | 文件路径 |
|------|----------|
| 道具枚举 | `Assets/Scrpits/Enums/ItemsEnum.cs` |
| 道具数据Bean | `Assets/Scrpits/Bean/Game/ItemBean.cs` |
| 道具配置Bean | `Assets/Scrpits/Bean/MVC/Game/ItemsInfoBean.cs` |
| 道具类型配置 | `Assets/Scrpits/Bean/MVC/Game/ItemsTypeBean.cs` |
| 道具工具类 | `Assets/Scrpits/Utils/ItemsUtil.cs` |
| 背包管理 | `Assets/Scrpits/Bean/MVC/UserDataBean.cs` |
| 生物装备 | `Assets/Scrpits/Bean/Game/CreatureBean.cs` |
| UI背包组件 | `Assets/Scrpits/Component/UI/Common/Backpack/` |
