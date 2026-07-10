---
name: creature-system
description: Demon Lord Roguelike 游戏的生物系统开发指南。使用此SKILL当需要创建或修改生物实体、生物属性、生物创建/删除/管理、生物数据(CreatureBean)、生物稀有度/等级系统等，与 creature-card-system(卡片UI) 和 ai-system(AI行为) 互补。
watched_files:
  - Assets/Scripts/Component/Handler/CreatureHandler.cs
  - Assets/Scripts/Component/Manager/CreatureManager.cs
  - Assets/Scripts/Bean/Game/CreatureBean.cs
  - Assets/Scripts/Bean/Game/CreatureAttributeBean.cs
  - Assets/Scripts/Bean/Game/CreatureNpcBean.cs
  - Assets/Scripts/Bean/MVC/Game/CreatureInfoBean.cs
  - Assets/Scripts/Utils/CreatureUtil.cs
  - Assets/Scripts/Game/Fight/FightCreatureEntity.cs
  - Assets/Scripts/Game/Fight/FightCreatureEntityForAttack.cs
  - Assets/Scripts/Game/Fight/FightCreatureEntityForDefense.cs
  - Assets/Scripts/Game/Fight/FightCreatureEntityForDefenseCore.cs
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
    public int level;                        // 等级 (0-10)
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

> **NPC体型缩放**：`CreatureBean.bodySizeScale`(float, 默认1) 是模型体型倍率，仅 NPC 生效。来源为 `NpcInfo.body_size`(string) 配置：空/"0"=1倍、`"min,max"`(如"0.9,1.1")=区间随机、`"1.1"`=固定倍数；由 `NpcInfoBean.GetBodySizeRandomScale()` 解析，在 `CreatureBean.SetData(NpcInfoBean)` 创建时随机一次并缓存，渲染时 `CreatureHandler.SetCreatureData` 以 `localScale = size_spine × CreatureBean.GetBodySizeScale()`（带≤0回退1的保护）应用。普通生物倍率恒为1，行为不变。

> **等级升级机制见 [`sacrifice-system`](../sacrifice-system/SKILL.md) Skill**：生物的 `level`/`levelExp` 升级走"基地祭坛献祭"——经验达标后献祭祭品掷骰，成功才升级。升级**不再自动加属性**，而是按 `LevelInfo.attribute_point`(当前全等级配置5) 发放可分配点数，由玩家在 `UICreatureAddAttribute` 界面手动加点(HP/护甲每点+10、攻击/攻速每点+1，写入 `creatureAttribute.dicAttributeLevelUp`)。升级方法 `UpLevelForSacrifice`(返回本次加点数)/`CanUpLevel`/`IsMaxLevel` 在 `CreatureBeanPartial.cs`，单点增量 `CreatureUtil.GetAttributePointAddValue`，成功率公式在 `CreatureUtil`。

### 创建生物

```csharp
// 通过配置ID创建
CreatureBean creature = new CreatureBean(creatureId);

// 指定稀有度创建
CreatureBean creature = new CreatureBean(creatureId, rarity: 3);

// 指定全部参数
CreatureBean creature = new CreatureBean(creatureId, rarity: 5, level: 8);

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

// 魔力上限MP（仅战斗中有效 魔王创建魔物的资源池）：
// GetAttribute 的 switch 已含 MP/CMP 分支（CreatureBean.cs 为手写可改文件），直接走 GetAttribute 即可
float mp = creature.GetAttribute(CreatureAttributeTypeEnum.MP);
// 创建魔物消耗的魔力基础值配在 CreatureInfo.CMP（原 create_mp 字段已改名为 CMP），魔力恢复速度为 MPF（每秒恢复量）
// 取实际召唤耗魔走 creature.GetAttributeInt(CreatureAttributeTypeEnum.CMP)，勿直接读 CMP 字段。
// 该值 = 基础CMP + 基础CMP×(等级增加倍率+稀有度增加倍率)，再叠加自身/稀有度BUFF（如扭蛋 CMP 减益）。
// 等级增加倍率取 LevelInfo.CMP_rate（按 level），稀有度增加倍率取 RarityInfo.CMP_rate（按 rarity，N=0 依次+0.5），
// 两者求和由 CreatureBean.GetCreateMPAddRate() 提供（level 0/越界记0，rarity≤0视为N）。

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
    RCD = 8,               // 冷却缩减（实为复活CD）
    HPRegeneration = 11,   // HP 恢复
    MP = 20,               // 魔力
    CMP,                   // 召唤魔力消耗（基础值=CreatureInfo.CMP；GetAttribute(CMP)=基础CMP×(1+等级/稀有度增加倍率)，再经BUFF管线修正）
}
```

### 属性计算流程

```
基础属性 (baseAttribute)
    │  来自 CreatureInfoBean 配置 + 等级/稀有度加成
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

**文件**: `Assets/Scripts/Game/Fight/FightCreatureEntity.cs`（partial 拆分：通用部分在主文件；进攻生物专属在 `FightCreatureEntityForAttack.cs`；防守生物专属在 `FightCreatureEntityForDefense.cs`；魔王(防守核心)专属(魔力MPShow显示等)在 `FightCreatureEntityForDefenseCore.cs`）

### 核心职责

```csharp
// 普通C#类（非MonoBehaviour），partial 拆分为 主文件 + ForAttack + ForDefense + ForDefenseCore
public partial class FightCreatureEntity
{
    // === 数据（主文件） ===
    public GameObject creatureObj;                        // 生物游戏物体
    public FightCreatureBean fightCreatureData;           // 战斗生物数据（含 creatureData 原始生物数据）
    public AIBaseEntity aiEntity;                         // AI实体
    public CreatureFightStateEnum creatureFightState;     // 生物战斗状态
    public SkeletonAnimation creatureSkeletionAnimation;  // Spine 动画组件
    public SpriteRenderer creatureLifeShow;               // 血条（进度条材质 CheckDead 内刷新）

    // === 生命周期（主文件） ===
    public void SetData(GameObject creatureObj, FightCreatureBean fightCreatureData);  // 初始化（内部调用 SetDataForDefenseCore 挂接魔王MPShow）
    public void Destory(bool isPermanently);                                           // 删除

    // === 战斗交互（主文件） ===
    public void UnderAttack(BaseAttackMode attackMode);  // 受击
    public void RegainHP(BaseAttackMode attackMode);     // 回复HP
    public void RegainDR(BaseAttackMode attackMode);     // 回复护甲
    public void AddBuff(BaseAttackMode attackMode);      // 添加BUFF
    public void CheckDead(Action noDead, Action dead);    // 死亡检测（内置血条/护盾进度刷新）
    public void SetCreatureDead();                       // 设置死亡（分发到各类型partial的死亡意图切换）
    public bool IsDead();                                // 是否死亡
    public void DropCrystal(int state);                  // 掉落水晶（0所有 1仅进攻 2仅防守）

    // === 表现（主文件） ===
    public TrackEntry PlayAnim(SpineAnimationStateEnum anim, bool loop);  // 播放动画
    public void SetFaceDirection(Direction2DEnum direction);              // 设置朝向

    // === 进攻生物专属（FightCreatureEntityForAttack.cs） ===
    public void ChangeRoad(int targetRoadIndex);         // 换路（诱导）

    // === 魔王专属（FightCreatureEntityForDefenseCore.cs） ===
    public MeshRenderer creatureMPShow;                  // 魔力条（MeshRenderer+Quad 新版圆形 MeshProgressBar 材质）
    public TMPro.TextMeshPro creatureMPText;             // 魔力文本（当前/上限）
    public void RefreshMPShow();                         // 刷新魔力显示
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

> **`CreateDefenseCreatureEntity` 末尾推送新事件（事件驱动，不再直接重算）**：加完新防守魔物的 BUFF 后 `EventHandler.Instance.TriggerEvent(EventsInfo.GameFightLogic_DefenseCreatureCreate, fightCreatureEntity)`——CreatureHandler 只负责生成、推事件；由 `GameFightLogic.EventForDefenseCreatureCreate` 监听后按守卫 `BuffHandler.Instance.HasDynamicRateAbyssalBlessing()` 重算全体防守属性，供动态数值馈赠「都是兄弟」（加成率随场上魔物数 N 变化）在放置/增殖新魔物、N 增大时即时生效。守卫仅当馈赠池含指定类型/子类 BUFF 才广播，普通对局无开销。重算职责归 GameFightLogic。详见 abyssal-blessing-system SKILL。

### CreatureManager（生物管理器）

**文件**: `Assets/Scripts/Component/Manager/CreatureManager.cs`

```csharp
// 管理生物对象池
// 管理生物数据缓存
// 管理生物外观资源
```

#### 场上魔物描边高亮预览

`CreatureHandler.ShowCreatureOutlinePreview(FightCreatureEntity)` / `HideCreatureOutlinePreview()` → `CreatureManager` 同名方法。悬停已上场魔物卡牌时高亮对应场上魔物。**职责拆分**：`CreatureManager` 只负责懒加载共享单例预览预制 + 取组件（`GetCreatureSpineOutlineFollow`）；**显示/材质/逐帧跟随全部逻辑在 [CreatureSpineOutlineFollow](Assets/Scripts/Game/Fight/CreatureSpineOutlineFollow.cs) 组件里**（`Show` / `Hide`）。

- 预制 `FightCreature_OutlinePreview.prefab`（由 `FightCreature_SelectPreview` 复制，Spine 的 MeshRenderer 挂亮蓝 OutlineOnly 描边材质 `MatSpriteCreatureOutline.mat`，二者 Addressable 地址=路径）。`CreatureManager` 懒加载它并 `AddComponent<CreatureSpineOutlineFollow>` 到 Spine 节点。
- `CreatureSpineOutlineFollow.Awake` 从自身渲染器 `sharedMaterial`（即预制描边材质，须在 `SetCreatureData` 替换前）克隆出运行时材质实例 `matOutline`；`OnDestroy` 释放。**描边颜色由材质资源决定，不在代码里写死**（想调色直接改 `.mat`）。
- `Show`：`CreatureHandler.SetCreatureData` 灌目标骨骼（同一生物→同一骨架，逐帧骨骼复制才对应得上；切换生物才重建）→ `RefreshMaterial` 套描边材质 → 初始贴合位置/大小/朝向 → `SetTarget` → 激活根节点。
- **逐帧跟随动画**：订阅自身 `SkeletonAnimation.UpdateLocal`（在"应用动画后、算世界变换前"触发），逐根把目标骨骼的本地 SRT（X/Y/Rotation/ScaleX/ScaleY/ShearX/ShearY）复制过来，使描边轮廓跟上目标正在播放的动画；`LateUpdate` 逐帧同步根位置与 Spine `localScale`（含左右翻转）。`Hide`→`ClearTarget` + 根 `SetActive(false)`（`OnDisable` 退订）。**不是定格首帧**——早期定格首帧会导致目标播动画时描边脱节。
- 描边经 `SkeletonAnimation.CustomMaterialOverride` 把目标图集材质替换为 `matOutline`，`_MainTex` 填目标图集纹理，排序置目标后一层。
- **为何用描边而非 Rim 边缘光**：生物材质用固定法线 `_FIXED_NORMALS_VIEWSPACE`(法线恒正对相机)，Rim 公式 `(1-dot(法线,视线))^power` 恒≈0，平面精灵上 Rim 不可见，故改用 OutlineOnly 真描边。

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
// 规则：同 id 祭品基础成功率 = 1/sacrificeNum；
//       不同 id = differentIdRate(研究 SacrificeDifferentIdRate 等级×5%,未解锁0)；
//       等级差修正(替代稀有度判定)：×Mathf.Pow(2, 祭品.level-目标当前level)，高1级×2/低1级×0.5/同级×1（同id/不同id均叠加）；全部累加
float GetSacrificeFoddersRate(CreatureBean targetCreature, List<CreatureBean> listFodder, int sacrificeNum, float differentIdRate);

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
| 战斗生物实体(通用) | `Assets/Scripts/Game/Fight/FightCreatureEntity.cs` |
| 战斗生物实体(进攻) | `Assets/Scripts/Game/Fight/FightCreatureEntityForAttack.cs` |
| 战斗生物实体(防守) | `Assets/Scripts/Game/Fight/FightCreatureEntityForDefense.cs` |
| 战斗生物实体(魔王) | `Assets/Scripts/Game/Fight/FightCreatureEntityForDefenseCore.cs` |
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
