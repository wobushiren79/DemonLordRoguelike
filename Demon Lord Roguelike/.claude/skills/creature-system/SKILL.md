---
name: creature-system
description: Demon Lord Roguelike 游戏的生物系统开发指南。使用此SKILL当需要创建或修改生物实体、生物属性、生物创建/删除/管理、生物数据(CreatureBean)、生物稀有度/星级/等级系统等，与 creature-card-system(卡片UI) 和 ai-system(AI行为) 互补。
watched_files:
  - Assets/Scripts/Component/Handler/CreatureHandler.cs
  - Assets/Scripts/Component/Manager/CreatureManager.cs
  - Assets/Scripts/Bean/Game/CreatureBean.cs
  - Assets/Scripts/Bean/Game/CreatureAttributeBean.cs
  - Assets/Scripts/Bean/Game/CreatureNpcBean.cs
  - Assets/Scripts/Bean/MVC/Game/CreatureInfoBean.cs
  - Assets/Scripts/Utils/CreatureUtil.cs
  - Assets/Scripts/Game/Fight/FightCreatureEntity.cs
  - Assets/Scripts/Enums/CreatureEnum.cs
  - Assets/Scripts/AI/Creature/AICreatureEntity.cs
---

# 生物系统开发指南

> 本 skill 关注生物实体、属性、创建/删除管理等核心系统。
> 生物卡片 UI 请使用 [creature-card-system](../creature-card-system/SKILL.md)，AI 行为请使用 [ai-system](../ai-system/SKILL.md)。

## 核心概念

### 生物数据体系

```
CreatureBean              - 生物完整数据（属性、装备、BUFF、外观等）
    ├── creatureInfoBean      - 生物配置信息（ID、名称、稀有度等）
    ├── creatureAttributeBean - 生物属性值（HP/DR/ATK/ASPD/MSPD等）
    ├── creatureSkinData      - 皮肤/外观数据
    ├── listEquip             - 装备列表
    └── listBuff              - BUFF 列表

CreatureInfoBean           - 生物配置表数据（来自 Excel/Cfg）
CreatureAttributeBean      - 生物运行时属性（计算 BUFF + 装备加成后）
FightCreatureEntity        - 战斗中生物实体（Spine动画、受击、移动）
```

### 生物分类

```csharp
CreatureFightTypeEnum
├── FightAttack = 1      // 进攻生物（主动前进攻击）
├── FightDefense = 2     // 防守生物（玩家放置）
└── FightDefenseCore = 3 // 核心生物（魔王，需保护）
```

---

## CreatureBean - 生物数据

**文件**: `Assets/Scripts/Bean/Game/CreatureBean.cs`

### 核心属性

```csharp
public class CreatureBean
{
    public string creatureUUId;              // 唯一ID (UUID)
    public long creatureId;                  // 配置表ID
    public string creatureName;             // 自定义名称
    public int rarity;                       // 稀有度 (1-6)
    public int starLevel;                    // 星级 (0-10)
    public int level;                        // 等级
    public long exp;                         // 经验值
    
    public CreatureAttributeBean baseAttribute;      // 基础属性
    public CreatureAttributeBean equipAttribute;     // 装备加成属性
    public CreatureAttributeBean buffAttribute;      // BUFF 加成属性
    
    public Dictionary<ItemTypeEnum, ItemBean> listEquip;  // 装备
    public List<BuffBean> listBuff;                        // BUFF 列表
    public Dictionary<long, long> creatureSkinData;        // 皮肤数据
    public CreatureInfoBean creatureInfoBean;              // 配置信息
    
    // 计算最终属性 = 基础 + 装备 + BUFF
    public CreatureAttributeBean GetFinalAttribute();
}
```

> **等级升级机制见 [`sacrifice-system`](../sacrifice-system/SKILL.md) Skill**：生物的 `level`/`levelExp` 升级走"基地祭坛献祭"——经验达标后献祭祭品掷骰，成功才升级并加属性（当前每级 +1 ATK，写入 `creatureAttribute.dicAttributeLevelUp`）。升级方法 `UpLevelForSacrifice`/`CanUpLevel`/`IsMaxLevel` 在 `CreatureBeanPartial.cs`，成功率公式在 `CreatureUtil`。

### 创建生物

```csharp
// 通过配置ID创建
CreatureBean creature = new CreatureBean(creatureId);

// 指定稀有度创建
CreatureBean creature = new CreatureBean(creatureId, rarity: 3);

// 指定全部参数
CreatureBean creature = new CreatureBean(creatureId, rarity: 5, starLevel: 3, level: 50);

// 从存档加载（已有UUID）
CreatureBean creature = new CreatureBean(creatureId, creatureUUId);
```

### 获取属性

```csharp
CreatureBean creature = GetCreature();

// 最终属性（基础 + 装备 + BUFF）
CreatureAttributeBean finalAttr = creature.GetFinalAttribute();

float hp = finalAttr.GetAttribute(CreatureAttributeTypeEnum.HP);
float atk = finalAttr.GetAttribute(CreatureAttributeTypeEnum.ATK);
float def = finalAttr.GetAttribute(CreatureAttributeTypeEnum.DR);
float aspd = finalAttr.GetAttribute(CreatureAttributeTypeEnum.ASPD);
float mspd = finalAttr.GetAttribute(CreatureAttributeTypeEnum.MSPD);

// 配置信息
CreatureInfoBean info = creature.creatureInfoBean;
string name = info.name_language;     // 本地化名称
string type = info.creatureType;      // 生物类型
long modelId = info.creatureModelId;  // 模型ID
```

---

## CreatureAttributeBean - 属性系统

**文件**: `Assets/Scripts/Bean/Game/CreatureAttributeBean.cs`

### 属性类型

```csharp
public enum CreatureAttributeTypeEnum
{
    None = 0,
    HP = 1,                // 生命值
    DR = 2,                // 防御
    ATK = 3,               // 攻击力
    ASPD = 4,              // 攻击速度
    MSPD = 5,              // 移动速度
    CRT = 6,               // 暴击率
    EVA = 7,               // 闪避率
    RCD = 8,               // 冷却缩减
    HPRegeneration = 11,   // HP 恢复
    MP = 20,               // 魔力
}
```

### 属性计算流程

```
基础属性 (baseAttribute)
    │  来自 CreatureInfoBean 配置 + 等级/星级/稀有度加成
    │
    ├── + 装备加成 (equipAttribute)
    │   来自 listEquip 中每个 ItemBean 的属性
    │
    ├── + BUFF 加成 (buffAttribute)
    │   来自 listBuff 中每个属性类 BUFF 的加成
    │
    ▼
最终属性 (GetFinalAttribute())
    用于战斗中的伤害计算、速度计算等
```

---

## FightCreatureEntity - 战斗生物实体

**文件**: `Assets/Scripts/Game/Fight/FightCreatureEntity.cs`

### 核心职责

```csharp
public class FightCreatureEntity : BaseMonoBehaviour
{
    public FightCreatureBean fightCreatureData;  // 战斗生物数据
    public CreatureBean creatureData;            // 原始生物数据
    public GameObject creatureObj;               // Spine 游戏对象
    public SkeletonAnimation skeletonAnimation;  // Spine 动画组件
    public GameObject hpBar;                     // 血条

    // === 生命周期 ===
    public void InitCreatureEntity(FightCreatureBean data);  // 初始化
    public void ClearData();                                  // 清理
    
    // === 战斗交互 ===
    public void UnderAttack(BaseAttackMode attackMode);  // 受击
    public void RegainHP(BaseAttackMode attackMode);     // 回复HP
    public void RegainDR(BaseAttackMode attackMode);     // 回复护甲
    public void AddBuff(BaseAttackMode attackMode);      // 添加BUFF
    public void CheckDead(Action noDead, Action dead);    // 死亡检测
    public void SetCreatureDead();                       // 设置死亡
    public bool IsDead();                                // 是否死亡
    public void DropCrystal(int state);                  // 掉落水晶
    
    // === 表现 ===
    public TrackEntry PlayAnim(SpineAnimationStateEnum anim, bool loop);  // 播放动画
    public void SetFaceDirection(Direction2DEnum direction);              // 设置朝向
    public void ShowHpBar();                                              // 显示血条
    public void HideHpBar();                                              // 隐藏血条
    public void UpdateHpBar();                                            // 更新血条
}
```

### 受击流程

```
UnderAttack(BaseAttackMode)
    │
    ├── 1. 闪避判定（EVA属性）
    │   └── 闪避成功 → 显示 MISS → 结束
    │
    ├── 2. 暴击判定（CRT属性）
    │   └── 暴击 → 伤害 *= 暴击倍率
    │
    ├── 3. 扣护甲（DR属性）
    │   └── 护甲 > 0 → 扣护甲，减伤
    │
    ├── 4. 扣血量（HP属性）
    │   └── HP -= 最终伤害
    │
    ├── 5. 触发 BUFF（受击/死亡）
    │
    ├── 6. 更新血条
    │
    └── 7. 检查死亡
        ├── HP <= 0 → SetCreatureDead()
        │   ├── 播放死亡动画
        │   ├── 触发死亡 BUFF
        │   ├── 掉落水晶
        │   └── 触发死亡事件
        └── HP > 0 → 播放受击动画
```

---

## CreatureHandler / CreatureManager

### CreatureHandler（生物处理器）

**文件**: `Assets/Scripts/Component/Handler/CreatureHandler.cs`

```csharp
// 创建进攻生物
CreatureHandler.Instance.CreateAttackCreature(attackDetailData, roadNum);

// 创建防守生物（预览/实体）
GameObject previewObj = CreatureHandler.Instance.CreateDefenseCreature(creatureData);
CreatureHandler.Instance.CreateDefenseCreatureEntity(previewObj, creatureData, position);

// 移除生物
CreatureHandler.Instance.RemoveFightCreatureEntity(entity, creatureFightType);

// 获取生物
FightCreatureEntity entity = CreatureHandler.Instance.GetFightCreatureEntity(creatureUUId);
```

### CreatureManager（生物管理器）

**文件**: `Assets/Scripts/Component/Manager/CreatureManager.cs`

```csharp
// 管理生物对象池
// 管理生物数据缓存
// 管理生物外观资源
```

---

## CreatureInfoBean - 生物配置

**文件**: `Assets/Scripts/Bean/MVC/Game/CreatureInfoBean.cs`

### 配置字段

```csharp
public class CreatureInfoBean : BaseBean
{
    public long id;                          // 生物ID
    public long name;                        // 名称文本ID
    public long creatureType;                // 生物类型
    public long rarity;                      // 稀有度
    public long creatureModelId;             // 模型ID（Spine资源）
    public long creatureModelInfoId;         // 模型详细信息ID
    public string equipItemsType;            // 可装备类型 "1,2,3,4,5,10"
    public long equipItemsWeaponType;        // 可装备武器类型（0=全部）
    public long baseHP;                      // 基础HP
    public long baseDR;                      // 基础防御
    public long baseATK;                     // 基础攻击
    public float baseASPD;                   // 基础攻速
    public float baseMSPD;                   // 基础移速
    public long cost;                        // 召唤消耗
    // ... 更多配置
    
    [JsonIgnore]
    public string name_language { get; }  // 本地化名称
}
```

---

## CreatureUtil - 生物工具

**文件**: `Assets/Scripts/Utils/CreatureUtil.cs`

```csharp
// 获取生物皮肤类型的多语言显示名称
string GetCreatureSkinTypeEnumName(CreatureSkinTypeEnum creatureSkinType);

// === 生物献祭升级（详见 sacrifice-system Skill）===

// 计算一批祭品对目标生物的"献祭成功率(祭品部分,不含保底)"
// 规则：单个相同 id+相同稀有度祭品基础成功率 = 1/sacrificeNum；
//       id 不同 ×1/10；稀有度每低一级再 ×1/10（可叠加）；全部累加
float GetSacrificeFoddersRate(CreatureBean targetCreature, List<CreatureBean> listFodder, int sacrificeNum);

// 计算目标生物本次献祭的最终成功率（保底 sacrificePityRate + 祭品，统一 Clamp01）
float GetSacrificeSuccessRate(CreatureBean targetCreature, List<CreatureBean> listFodder);
```

> 注：稀有度颜色、模型/Spine 资产名、装备校验等并不在 `CreatureUtil` 内，分别由各自的 Handler/工具承担，勿在此查找。

---

## 常用代码模板

### 创建进攻生物

```csharp
// 在 GameFightLogic 中
public void SpawnAttackCreature(AttackDetailData detailData)
{
    int roadNum = fightData.sceneRoadNum;
    CreatureHandler.Instance.CreateAttackCreature(detailData, roadNum);
}
```

### 创建防守生物（玩家操作）

```csharp
// 选中卡片 → 预览 → 放置
public void CreateDefensePreview(CreatureBean creatureData)
{
    // 创建预览对象
    GameObject previewObj = CreatureHandler.Instance.CreateDefenseCreature(creatureData);
    // 预览跟随鼠标...
}

public void PlaceDefenseCreature(Vector3 worldPos, int roadIndex, int posInRoad)
{
    // 放置并创建实体
    CreatureHandler.Instance.CreateDefenseCreatureEntity(previewObj, creatureData, worldPos);
}
```

### 获取生物最终属性

```csharp
public float GetCreatureAttack(CreatureBean creature)
{
    var finalAttr = creature.GetFinalAttribute();
    return finalAttr.GetAttribute(CreatureAttributeTypeEnum.ATK);
}
```

### 修改生物属性

```csharp
public void BuffCreatureHP(CreatureBean creature, float bonusHP)
{
    var baseAttr = creature.baseAttribute;
    float currentHP = baseAttr.GetAttribute(CreatureAttributeTypeEnum.HP);
    baseAttr.SetAttribute(CreatureAttributeTypeEnum.HP, currentHP + bonusHP);
}
```

### 稀有度相关

```csharp
public enum RarityEnum
{
    Common = 1,      // 普通 (白色)
    Uncommon = 2,    // 非凡 (绿色)
    Rare = 3,        // 稀有 (蓝色)
    Epic = 4,        // 史诗 (紫色)
    Legendary = 5,   // 传说 (橙色)
    Mythic = 6,      // 神话 (红色)
}

// 获取稀有度颜色
Color rarityColor = CreatureUtil.GetRarityColor(creature.rarity);
```

---

## 文件位置速查

| 功能 | 文件路径 |
|------|----------|
| 生物数据Bean | `Assets/Scripts/Bean/Game/CreatureBean.cs` |
| 生物属性Bean | `Assets/Scripts/Bean/Game/CreatureAttributeBean.cs` |
| 生物配置Bean | `Assets/Scripts/Bean/MVC/Game/CreatureInfoBean.cs` |
| 生物NPC数据 | `Assets/Scripts/Bean/Game/CreatureNpcBean.cs` |
| 战斗生物实体 | `Assets/Scripts/Game/Fight/FightCreatureEntity.cs` |
| 生物处理器 | `Assets/Scripts/Component/Handler/CreatureHandler.cs` |
| 生物管理器 | `Assets/Scripts/Component/Manager/CreatureManager.cs` |
| 生物工具 | `Assets/Scripts/Utils/CreatureUtil.cs` |
| 生物枚举 | `Assets/Scripts/Enums/CreatureEnum.cs` |
| 生物AI基类 | `Assets/Scripts/AI/Creature/AICreatureEntity.cs` |
| 生物AI意图 | `Assets/Scripts/AI/Creature/` |
| 属性类型枚举 | `Assets/Scripts/Enums/GameStateEnum.cs` (CreatureAttributeTypeEnum) |

---

## 注意事项

1. **UUID 唯一性**: 每个 CreatureBean 的 creatureUUId 是全局唯一的，使用 GUID 生成。
2. **属性计算顺序**: 基础属性 → 装备加成 → BUFF 加成，计算最终属性时按此顺序。
3. **死体回收**: 死亡生物的资源需要回收，FightCreatureEntity 使用对象池管理。
4. **Spine资源**: 不同生物可能使用同一个 Spine 资源（如所有史莱姆共用模型），仅皮肤不同。
5. **与 creature-card-system 的边界**: 本 skill 负责生物实体/属性/数据，creature-card-system 负责卡片 UI 展示/交互。
6. **与 ai-system 的边界**: 本 skill 负责生物实体管理，ai-system 负责生物的行为决策（状态机）。
