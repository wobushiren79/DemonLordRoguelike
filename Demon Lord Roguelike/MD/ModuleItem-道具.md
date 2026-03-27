# 道具模块 (Item Module) 分析文档

## 一、模块概述

道具模块负责游戏中所有道具的定义、存储、背包管理、装备穿戴以及UI展示。涵盖装备类道具（帽子、衣服、裤子、鞋子、鼻环、武器）和特殊货币类道具（魔晶）。

---

## 二、核心数据结构

### 2.1 ItemBean（道具实例）

**文件**: `Bean/Game/ItemBean.cs` + `ItemBeanPartial.cs`

道具的运行时实例，代表玩家持有的一个具体道具。

| 字段 | 类型 | 说明 |
|------|------|------|
| `itemId` | `long` | 道具配置ID，对应ItemsInfoBean.id |
| `itemNum` | `int` | 堆叠数量 |
| `rarity` | `int` | 道具品质（对应RarityEnum） |
| `dicAttribute` | `Dictionary<CreatureAttributeTypeEnum, float>` | 装备属性加成（HP/DR/ATK/ASPD） |

**关键方法**:
- `AddRandomAttributeForCreate(int addNum)` — 创建时随机分配属性（HP+10 / DR+10 / ATK+1 / ASPD+1）
- `AddAttribute(type, value)` — 增加指定属性
- `GetAttribute(type)` — 获取指定属性值
- `GetItemType()` — 通过关联的 `ItemsInfoBean` 获取道具类型枚举
- `GetRarityEnum()` — 获取稀有度枚举

**Partial扩展** (`ItemBeanPartial.cs`):
- 懒加载 `itemsInfo` 属性，通过 `ItemsInfoCfg.GetItemData(itemId)` 从配置表获取静态信息
- 标记 `[JsonIgnore]` 避免序列化

### 2.2 ItemsInfoBean（道具配置表）

**文件**: `Bean/MVC/Game/ItemsInfoBean.cs` + `ItemsInfoBeanPartial.cs`

JSON配置表数据，继承 `BaseBean`，由 `ItemsInfoCfg` 管理。

| 字段 | 类型 | 说明 |
|------|------|------|
| `item_type` | `int` | 道具类型（1帽子 2衣服 3裤子 4鞋子 5鼻环 10武器 1000魔晶） |
| `num_max` | `int` | 单组堆叠上限 |
| `creature_model_id` | `long` | 关联的生物模组ID |
| `creature_model_info_id` | `long` | 关联的生物模组详细信息ID |
| `icon_res` | `string` | 图标资源名 |
| `icon_rotate_z` | `float` | 图标旋转角度 |
| `attack_mode_data` | `string` | 攻击模式参数串 |
| `name` | `long` | 多语言文本ID |
| `remark` | `string` | 备注 |

**Partial扩展** (`ItemsInfoBeanPartial.cs`):
- `GetItemType()` — 返回 `ItemTypeEnum` 枚举
- `HandleItemsInfoAttackModeData(BaseAttackMode)` — 解析 `attack_mode_data` 字符串，配置武器攻击模式的精灵图、旋转轴/速度、位置、大小等shader参数
- `dicDataForCreatureModel` — 按生物模型ID索引道具数据的二级缓存

**配置管理器** `ItemsInfoCfg`:
- 继承 `BaseCfg<long, ItemsInfoBean>`，从 `"ItemsInfo"` JSON文件加载
- `GetItemData(long key)` — 按ID查询
- `GetAllData()` / `GetAllArrayData()` — 全量查询
- `GetDataByCreatureModelId(long)` — 按生物模型查询关联道具
- `ContainsKeyForCreatureModelId(long)` — 判断是否有该模型的道具

### 2.3 ItemsTypeBean（道具类型配置表）

**文件**: `Bean/MVC/Game/ItemsTypeBean.cs` + `ItemsTypeBeanPartial.cs`

| 字段 | 类型 | 说明 |
|------|------|------|
| `icon_res` | `string` | 类型图标资源名 |
| `name` | `long` | 多语言文本ID |

**配置管理器** `ItemsTypeCfg`:
- 支持 `GetItemData(ItemTypeEnum)` 按枚举查询

---

## 三、枚举定义

**文件**: `Enums/ItemsEnum.cs` + `Enums/GameStateEnum.cs`

### ItemIdEnum（特殊道具ID）
```csharp
Crystal = 1  // 魔晶
```

### ItemTypeEnum（道具类型）
```csharp
Hat = 1,       // 帽子
Clothes = 2,   // 衣服
Pants = 3,     // 裤子
Shoe = 4,      // 鞋子
NoseRing = 5,  // 鼻环
Weapon = 10,   // 武器
Crystal = 1000 // 魔晶（特殊货币）
```

### RarityEnum（稀有度）
```csharp
N = 1, R = 2, SR = 3, SSR = 4, UR = 5, L = 6
```

### ItemInfoAttackModeDataEnum（武器攻击模式参数）
```csharp
ShowSprite,       // 展示的精灵图片
VertexRotateAxis, // 模型旋转角度（如0,0,-1）
VertexRotateSpeed,// 模型旋转速度
UVRotateSpeed,    // UV旋转速度
StartPosition,    // 起始位置偏移
StartSize         // 起始大小缩放
```

---

## 四、背包系统

### 4.1 数据存储

**文件**: `Bean/MVC/UserDataBean.cs`

用户数据中通过 `List<ItemBean> listBackpackItems` 存储所有背包道具。

### 4.2 背包操作接口（UserDataBean）

| 方法 | 说明 |
|------|------|
| `AddBackpackItem(ItemBean)` | 直接添加道具实例（不堆叠） |
| `AddBackpackItem(long itemId, int num)` | 按ID和数量添加，自动堆叠（尊重 `num_max` 上限） |
| `AddBackpackItemForSpecial(long, int)` | 特殊道具处理（魔晶转为货币） |
| `RemoveBackpackItem(ItemBean)` | 从背包移除道具 |

### 4.3 堆叠逻辑

`AddBackpackItem(long, int)` 的堆叠流程：
1. 查询 `ItemsInfoCfg` 获取 `num_max`（单组上限，默认至少1）
2. 遍历已有同ID道具，优先填充未满的堆
3. 剩余数量创建新的道具堆（每堆不超过 `num_max`）
4. 触发 `Backpack_Item_Change` 全局事件

### 4.4 事件通知

| 事件名 | 触发时机 |
|--------|----------|
| `Backpack_Item_Change` | 背包道具增删时触发 |
| `UIViewItemBackpack_OnClickSelect` | 背包道具格子被点击时触发 |
| `UIViewItemEquip_OnClickSelect` | 装备槽位被点击时触发 |

---

## 五、装备系统

### 5.1 装备数据存储

**文件**: `Bean/Game/CreatureBean.cs`

每个生物通过 `Dictionary<ItemTypeEnum, ItemBean> dicEquipItemData` 存储当前装备，以道具类型为Key，每个槽位最多1件装备。

### 5.2 装备操作接口（CreatureBean）

| 方法 | 说明 |
|------|------|
| `InitEquip(NpcInfoBean)` / `InitEquip(List<long>)` | 根据NPC配置初始化装备 |
| `ChangeEquip(ItemTypeEnum, ItemBean, out ItemBean)` | 换装，返回被替换的旧装备 |
| `RemoveAllEquipToBackpack()` | 卸下所有装备归还背包 |
| `GetEquip(ItemTypeEnum)` | 获取指定槽位装备 |
| `GetEquipAttribute(CreatureAttributeTypeEnum)` | 汇总所有装备的指定属性加成 |
| `ClearEquip()` | 清空所有装备（不归还背包） |

### 5.3 装备换装流程

在 `UICreatureManager` 中的完整流程：

**穿戴装备** (`SetCreatureEquip`):
1. 从背包移除选中道具 → `userData.RemoveBackpackItem(itemData)`
2. 替换生物装备 → `creatureData.ChangeEquip(itemType, itemData, out beforeItem)`
3. 旧装备返回背包 → `userData.AddBackpackItem(beforeItem)`
4. 刷新UI

**卸载装备** (`UnloadCreatureEquip`):
1. 从生物卸载 → `creatureData.ChangeEquip(itemType, null, out beforeItem)`
2. 旧装备返回背包 → `userData.AddBackpackItem(beforeItem)`
3. 刷新UI

---

## 六、UI层结构

### 6.1 背包道具展示

| 组件 | 文件路径 | 职责 |
|------|----------|------|
| `UIViewItemBackpack` | `UI/Common/Backpack/UIViewItemBackpack.cs` | 单个背包道具格子：展示图标、数量、弹窗、点击事件 |
| `UIViewItemBackpackList` | `UI/Common/Backpack/UIViewItemBackpackList.cs` | 背包道具列表：虚拟滚动（ScrollGridCell）、排序（按稀有度/名字） |

### 6.2 装备槽位展示

| 组件 | 文件路径 | 职责 |
|------|----------|------|
| `UIViewItemEquip` | `UI/Common/ItemEquip/UIViewItemEquip.cs` | 装备槽位：展示当前装备或空槽位占位图标、点击卸装 |

### 6.3 道具弹窗

| 组件 | 文件路径 | 职责 |
|------|----------|------|
| `UIPopupItemInfo` | `UI/Popup/ItemInfo/UIPopupItemInfo.cs` | 道具详情弹窗：展示图标和名字 |

### 6.4 道具选择弹窗

| 组件 | 文件路径 | 职责 |
|------|----------|------|
| `UIDialogSelectItem` | `UI/Dialog/UIDialogSelectItem.cs` | 道具选择对话框：列出背包道具，支持丢弃/送礼操作 |
| `DialogSelectItemBean` | `Bean/UI/DialogSelectItemBean.cs` | 选择对话框参数：丢弃/送礼回调 |

### 6.5 商店道具

| 组件 | 文件路径 | 职责 |
|------|----------|------|
| `UIViewStoreItem` | `UI/Common/Store/UIViewStoreItem.cs` | 商店道具展示：图标、名字、价格、购买 |
| `UIViewStoreItemPartialGashaponMatchine` | `UI/Common/Store/UIViewStoreItemPartialGashaponMatchine.cs` | 扭蛋机商品扩展 |

### 6.6 生物管理界面

| 组件 | 文件路径 | 职责 |
|------|----------|------|
| `UICreatureManager` | `UI/Game/CreatureManager/UICreatureManager.cs` | 生物管理主界面：整合背包列表、装备详情、穿戴/卸载流程 |

---

## 七、图标系统

**文件**: `Component/Handler/IconHandler.cs`

| 方法 | 说明 |
|------|------|
| `SetItemIcon(long itemId, Image)` | 根据道具ID查配置表，加载图标到Image |
| `SetItemIcon(long itemId, SpriteRenderer)` | 同上，目标为SpriteRenderer |
| `SetItemIcon(string iconName, float rotateZ, Image)` | 从SpriteAtlas加载并设置旋转 |
| `SetItemIconForAttackMode(string, SpriteRenderer)` | 为攻击模式设置武器精灵 |

图标资源通过 `SpriteAtlasTypeEnum.Items` 图集管理。

---

## 八、关联系统

### 8.1 扭蛋系统关联

**文件**: `Bean/Game/GashaponItemBean.cs`

扭蛋出的是生物（CreatureBean），不是道具。但共享稀有度系统（RarityEnum）。GashaponItemBean负责：
- 随机皮肤 → `RandomSkill()`
- 随机属性 → `RandomAttribute()`
- 随机稀有度 → `RandomRarity()`（按解锁等级概率决定 N/R/SR/SSR/UR）
- 稀有度BUFF → 根据品质随机附加Buff

### 8.2 生物卡片系统关联

**文件**: `Bean/Game/CreatureCardItemBean.cs`

UI层的生物卡片数据容器：
- `creatureData` — 生物数据引用
- `cardUseState` — 卡片用途（展示/管理/战斗等）
- `cardState` — 卡片状态（UI展示用）

---

## 九、数据流总结

```
[JSON配置文件]
    │
    ▼
ItemsInfoCfg (静态配置缓存) ◄─── ItemsInfoBean (道具定义)
ItemsTypeCfg (类型配置缓存) ◄─── ItemsTypeBean (类型定义)
    │
    ▼
ItemBean (道具运行时实例)
    │  ├── itemsInfo → 懒加载关联ItemsInfoBean
    │  └── dicAttribute → 随机属性加成
    │
    ├──► UserDataBean.listBackpackItems (背包存储)
    │       ├── AddBackpackItem() → 堆叠入库
    │       └── RemoveBackpackItem() → 移除
    │
    └──► CreatureBean.dicEquipItemData (装备存储)
            ├── ChangeEquip() → 穿戴/替换
            ├── GetEquipAttribute() → 属性汇总
            └── RemoveAllEquipToBackpack() → 批量卸装

[UI层]
UIViewItemBackpackList → UIViewItemBackpack → 点击事件
UIViewItemEquip → 装备槽展示/卸装
UICreatureManager → 协调背包与装备交互
UIPopupItemInfo → 道具详情弹窗
UIDialogSelectItem → 道具选择(丢弃/送礼)
```
