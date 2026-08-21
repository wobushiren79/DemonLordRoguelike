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
  - Assets/Scripts/Bean/MVC/Game/CreatureInfoBeanPartial.cs
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

> **体型缩放**：`CreatureBean.bodySizeScale`(float, 默认1) 是模型体型倍率，两条来源共用同一套解析规则（空/"0"=1倍、`"min,max"`如"0.9,1.1"=区间随机、`"1.1"`=固定倍数）：NPC 读 `NpcInfo.body_size` + `NpcInfoBean.GetBodySizeRandomScale()`（`SetData(NpcInfoBean)`）；按 creatureId 创建的生物（扭蛋/建号等）读 `CreatureInfo.body_size` + `CreatureInfoBean.GetBodySizeRandomScale()`（`SetData(long creatureId)`）。均在创建时随机一次并缓存，渲染时 `CreatureHandler.SetCreatureData` 以 `localScale = size_spine × CreatureBean.GetBodySizeScale()`（带≤0回退1的保护）应用。未配置 body_size 的生物倍率恒为1，行为不变。CreatureInfo 侧另有区间解析 `CreatureInfoBean.GetBodySizeRange(out min, out max)`（供创建界面身高滑条取上下限，`GetBodySizeRandomScale` 已改为基于它随机）。

> **NPC 稀有度**：NPC 创建（`SetData(NpcInfoBean)`）稀有度取自 `NpcInfo.rarity` 列（int，空/0=N，按 RarityEnum 值 1=N~6=L），未配置时维持旧行为全 N。仅写入 `CreatureBean.rarity`（详情显示/CMP 倍率等读 `GetRarityValue()` 的链路自动生效），**不授予稀有度 BUFF**——稀有度 BUFF 仍仅孕育扭蛋（`GashaponItemBean`）与测试添加（`UITestBase`）经 `RandomRarityBuffForCreate` 发放。

> **creatureInfo/creatureModel 为自校验懒加载缓存**（`CreatureBeanPartial.cs`）：getter 发现缓存 id 与当前 `creatureId`（或 `creatureInfo.model_id`）不一致时自动重新解析。`creatureId` 允许中途直接改写（终焉议会转生 `DoomCouncilEntityReincarnation` 直接赋值、`CreatureManager.GetCreatureData` 对象池复用后 `SetData`），**无需手动清缓存**；改动种类相关字段时禁止另设平行的配置缓存字段绕过自校验。

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
// GetAttribute 的 switch 已含 MP/CMP 分支（CreatureBean.cs 为手写可改文件），直接走 GetAttribute 即可；
// 魔王(IsDemonLord)的 MP/MPF 还会在 GetAttribute 内叠加强化研究加成(DemonLordMPMax 每级+10 / DemonLordMPF 每级+1/秒)，战斗与基地显示同一口径
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
    MP = 2,                // 魔力
    DR = 3,                // 防御
    ATK = 4,               // 攻击力
    MSPD = 5,              // 移动速度
    ASPD = 6,              // 攻击速度
    CRT = 7,               // 暴击率
    EVA = 8,               // 闪避率
    RCD = 9,               // 复活CD
    MPR = 10,              // 魔法回复%
    MPF = 11,              // 魔法回复
    CMP = 12,              // 召唤魔力消耗（仅作BUFF修正标签,非生物常驻战斗属性）
    CDMG = 13,             // 暴击伤害倍率（基础1.5=暴击伤害+50%；BUFF rate Flat累加调整，如6001哥布林刺客经BUFF 2000500001+0.5→2.0）
    // ⚠️枚举值即 excel_creature_attribute_type_info 表 id（属性中文名/颜色映射），新增属性只能追加到末尾，禁止中间插入
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
    public void UnderAttackNoDamage(BaseAttackMode attackMode); // 无伤害受击(纯DEBUFF触碰：只上BUFF+命中音，不掉血/不跳数字/不播受击特效/不进统计，无敌免疫；AttackModeOverlapNoDamage 专用)
    public void RegainHP(BaseAttackMode attackMode);     // 回复HP（治疗成功且真实回血>0 时播攻击模式配置的命中音效 sound_hit，治疗型 500001 配 sound_medicine_1=470001）
    public void RegainDR(BaseAttackMode attackMode);     // 回复护甲
    public void AddBuff(BaseAttackMode attackMode);      // 添加BUFF
    public void CheckDead(Action noDead, Action dead);    // 死亡检测（内置血条/护盾进度刷新）
    public void SetCreatureDead();                       // 设置死亡（分发到各类型partial的死亡意图切换）
    public bool IsDead();                                // 是否死亡
    public void DropCrystal(int state);                  // 掉落水晶（0所有 1仅进攻 2仅防守）

    // === 表现（主文件） ===
    public TrackEntry PlayAnim(SpineAnimationStateEnum anim, bool loop);  // 播放动画
    public void SetAnimTimeScale(float timeScale);                        // 设置动画播放速度(=SkeletonAnimation.timeScale, 与 PlayAnim 的 animSpeed 相乘叠加; SetData 里按 GameFightLogic.GetCurrentGameSpeed() 自动初始化, 全场切换由 GameFightLogic.SetGameSpeed→RefreshAllCreatureAnimTimeScale 驱动)
    public void SetFaceDirection(Direction2DEnum direction);              // 设置朝向（内置去重：目标 localScale.x 符号与当前相等则直接 return，不重复写 transform.localScale；翻转对 Spine creatureSkeletionAnimation.transform.localScale.x 取正负。惠及所有调用方，尤其防守生物每攻击循环转身校准）

    // === 进攻生物专属（FightCreatureEntityForAttack.cs） ===
    public void ChangeRoad(int targetRoadIndex);         // 换路（诱导）

    // === 魔王专属（FightCreatureEntityForDefenseCore.cs） ===
    public MeshRenderer creatureMPShow;                  // 魔力条（MeshRenderer+Quad 新版圆形 MeshProgressBar 材质）
    public TMPro.TextMeshPro creatureMPText;             // 魔力文本（当前/上限）
    public void RefreshMPShow();                         // 刷新魔力显示

    // === 魔王专属-深渊馈赠环绕图标(GPU单Mesh)（FightCreatureEntityForDefenseCore.cs） ===
    public void InitAbyssalBlessingOrbit();              // 初始化（SetDataForDefenseCore 调用，仅魔王执行；Find 预制下已配好的 AbyssalBlessingOrbit 节点）
    public void RefreshAbyssalBlessingOrbit();           // 对账刷新（Buff_AbyssalBlessingChange 事件经 GameFightLogic 调用；新馈赠补建/消失移除）
}
```

> 环绕图标实现形态：魔王预制 `FightCreature_DefCore_1` 下的 `AbyssalBlessingOrbit` 节点（MeshRenderer/MeshFilter/材质球编辑器配好）+ 单 Mesh 装 N 个图标 quad，shader 为框架层 `FrameWork/URP/MeshOrbit`（`Assets/FrameWork/Shader/URP/Shader_Mesh_Orbit.shader`）；公转/浮动/入场缩放全在 vertex shader 按 `_Time.y` 匀速计算（不随游戏倍速），1 drawcall、每帧 CPU 零开销；代码只按馈赠列表重建 mesh 顶点（UV 指向图集 `sprite.textureRect`）并经 MaterialPropertyBlock 写 `_MainTex`/`_OrbitCount`（不污染共享材质资产）；mesh 静态共享跨局复用。ZWrite+AlphaClip 保证环绕到魔王身后被身体正确遮挡。

### 受击流程

```
UnderAttack(BaseAttackMode)
    │
    ├── 0. 无敌判定（FightCreatureBean.isInvincible,由SSR稀有度BUFF「真男人」BuffEntityConditionalInvincible驱动）
    │   └── 无敌中 → 跳0伤害字+播miss音效(复用闪避表现) → 结束（不掉血/不上受击BUFF/不播受击特效）
    │
    ├── 1. 闪避判定（EVA属性）
    │   └── 闪避成功 → 显示 MISS → 结束
    │
    ├── 2. 暴击判定（攻击者CRT属性快照）
    │   └── 暴击 → 伤害 ×= 攻击者暴击伤害倍率（CDMG属性快照attackerCDMG，默认1.5=+50%，可由BUFF调整）
    │
    ├── 3. 扣护甲（DR属性）
    │   └── 护甲 > 0 → 扣护甲，减伤（默认 ChangeDRAndHP 护甲吃满后溢出到血；
    │       若受击数据带 drDamageRate/hpDamageRate 分段倍率[≠1/1，来自攻击模式 other_data 键 dr_damage_rate/hp_damage_rate]则走串联破甲：
    │       护甲>0 只以 dr 倍率打甲不掉血、破甲击溢出不结转，破甲后只以 hp 倍率打血——如牛头人法师 101003/101004 配 dr2/hp0.5）
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

> **`CreateDefenseCreatureEntity` 末尾推送新事件（事件驱动，不再直接重算）**：加完新防守魔物的 BUFF 后 `EventHandler.Instance.TriggerEvent(EventsInfo.GameFightLogic_DefenseCreatureCreate, fightCreatureEntity)`——CreatureHandler 只负责生成、推事件；由 `GameFightLogic.EventForDefenseCreatureCreate` 监听后按守卫 `BuffHandler.Instance.HasDynamicRateAbyssalBlessing()` 重算全体防守属性，供随魔物数缩放类动态率馈赠（加成率随场上魔物数 N 变化；曾用于「都是兄弟」，现役无配置、机制留存）在放置/增殖新魔物、N 增大时即时生效。守卫仅当馈赠池含指定类型/子类 BUFF 才广播，普通对局无开销。重算职责归 GameFightLogic。详见 abyssal-blessing-system SKILL。

> **`CreateDefenseCreatureEntity` 同UUID已死实体预清理**：Add 进主列表前，若存在同 UUID 且 `IsDead()` 的旧实体（DeadRebirth 重生替换场景），先按实例移出——防 `DictionaryList.Add` 同 key 静默失败导致新实体成幽灵（不可命中/清场泄漏）。

> **`RemoveFightCreatureEntity` 防守分支两大改动**：①移除改为**按实例**——新 `FightBean.RemoveDefenseCreature(FightCreatureEntity)`（`DictionaryList.RemoveByValue`）；`FightBean.RemoveDefenseCreatureByPos` **已删除**（按 positionCreate 首匹配会误删同格生物）。②卡片进 Rest(CD) 的条件改为「场上已无该 UUID 存活实体」——DeadRebirth 重生替换场景新实体在场，卡片保持 Fighting 不进 CD；根治"重生后卡片照进CD、CD结束可再放同UUID生物导致 Add 静默失败生成幽灵实体"的缺陷（重生机制详见 buff-system SKILL「死亡重生」）。

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

> **`attack_search_back`(int 0/1) — 防守生物转身攻击身后开关**：配置列在 `excel_creature_info`（插在 `attack_search_time` 之后），JSON 导出到 `CreatureInfo.txt`。手写辅助方法 `CreatureInfoBeanPartial.IsAttackSearchBack()`（返回 `attack_search_back == 1`，紧邻 `GetCreatureSearchType()`）。开启后防守生物正面无目标时转身攻击身后（范围同正面），身后清空/超范围转回正面。首用者骷髅战士 `id=2001`。搜索/转身逻辑详见 [ai-system](../ai-system/SKILL.md)「防守生物转身攻击身后」。

> **`charge_attack`(int 0/1) — 冲锋自爆型生物开关**：配置列在 `excel_creature_info`，自动生成到 `CreatureInfoBean.charge_attack`；手写辅助方法 `CreatureInfoBeanPartial.IsChargeAttack()`（返回 `charge_attack == 1`）。冲锋自爆语义：放卡后立即向 +X 冲锋并释放原占位格（`FightCreatureBean.isPositionReleased` 置位，占位查询跳过已释放实体见 game-fight-system SKILL），前方 0.5 遇敌/冲到路尽头/被打死时在死亡位置原地自爆（AoE），死亡后卡片走 RCD 回手。当前唯一配置生物：**6003 哥布林敢死队**（attack_mode=300002 爆炸、attack_search_range=0.5 触发距离、attack_search_time=0.1、RCD=60）。死亡引爆实现见 game-fight-core agent（`FightCreatureEntityForDefense.CreateAttackModeForDeadExplosion`），重生联动（死亡地点重生再冲锋）见 buff-system SKILL「死亡重生」。

> **`details[language_1]`(long) — 生物详情描述（攻击方式说明）**：配置列在 `excel_creature_info` 末尾（col45），textId=生物自身 id；文本本体在 `excel_language` 的 CreatureInfo 工作表 `content_1_*` 12 语种列。自动生成 `CreatureInfoBean.details` + `details_language`（contentIndex=1，带 LanguageCache）。仅 id 1001~7004 的 30 个可招募生物已配；`details=0`/文本为空时详情面板隐藏整个说明区块（消费方 `UIViewCreatureCardDetails.SetRenmark`，见 creature-card-system）。与 AchievementInfoBean 的 `details[language_1]` 同构。

> **`creature_layer`(string) — 生物分层（搜索隐身 + 显示前置，双重语义）**：配置列在 `excel_creature_info`，值为 Unity Layer 名（`CreatureDef_Front`/`CreatureAtt_Front`/`CreatureDef_Back`/`CreatureAtt_Back`，见 [LayerInfo.cs](Assets/Scripts/Common/LayerInfo.cs)），空 = 默认层（CreatureDef/CreatureAtt）。生效代码在 `CreatureHandler.GetFightCreatureObj`：把生物根 GameObject.layer 改为配置层（**BoxCollider 挂在预制根节点，随根一并改层**），Front 层还会把 Spine 节点 Z 前移 0.1（渲染显示在其他魔物前面）。
> - **Front 层 = 故意让敌对方物理搜索搜不到（设计而非 bug）**：索敌/攻击命中的物理搜索 mask 写死 `1 << CreatureDef` / `1 << CreatureAtt`（[FightCreatureSearchUtil.cs](Assets/Scripts/Utils/FightCreatureSearchUtil.cs) `FindCreatureEntity`、[BaseAttackMode.cs](Assets/Scripts/Game/Fight/AttackMode/BaseAttackMode.cs) 攻击层 mask），Front 层不在 mask 内 → 敌人不会发现/攻击它。典型使用者**烂泥史莱姆(id=3003, attack_mode=400001)**：敌人从它身上走过会被减速，但敌人不把它当攻击目标——"地面附着型"魔物靠移出敌方搜索层实现"只影响敌人、不被敌人当目标"。**禁止以"修复索敌失效/打不到"为由扩大搜索 mask 或改回 layer**（会破坏该设计）。
> - 只要"重叠时显示在前"而不想影响索敌的需求，**不要复用 Front 层**（会连带搜索隐身），应走渲染侧方案（如 Spine MeshRenderer.sortingOrder）。
> - `creature_layer_find`（"生物优先级搜寻"）字段在配置里存在但**代码未接线**，勿假设它生效。

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

> **生物快照创建**：`FightAttackDetailsBean.creatureSnapshots`（与 `npcIds` 按下标对应，元素可为空）非空时，`CreateAttackCreature(npcId..., creatureSnapshot)` 直接用该 CreatureBean 创建（同皮肤/同装备/同属性），不再按 npcId 重建——首用于终焉议会暴力说服战斗（议员与议会场景同一只）。

> **NPC 随机装备**：NPC 创建（`CreatureBean.SetData(NpcInfoBean)`）在 `InitEquip` 后追加 `InitRandomEquip(npcInfo)`——NPC 配置了 `NpcInfo.equip_random`（`池ID,稀有度...`）时按池类型分支：散件池（`random_type=1`，`equip_random_data` 按 ItemType 分组）每槽经 `CanEquipItem` 过滤后「空+可装备道具」等概率抽 1 件（裸体率=1/(可装备数+1)）、稀有度每件独立抽；套装池（`random_type=2`，池内为 EquipSuitInfo 套装id）走 `InitRandomEquipForSuit`——`GetRandomEquipSuit` 筛整套可装备（`EquipSuitInfoBean.CanEquipFor`）的套装等概率整套抽 1 套、**稀有度整套统一 roll 一次**。两者均只填空槽（固定装备优先）、走 `EquipUtil.CreateEquipItemForNpc` 生成器(NPC随机装备场景)。首用于终焉议会随机议员（详见 doom-council-system / item-system）。

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
| 战斗生物实体(魔王:MPShow/死亡意图/深渊馈赠环绕图标) | `Assets/Scripts/Game/Fight/FightCreatureEntityForDefenseCore.cs` |
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
5. **皮肤来源与默认武器兜底**: 皮肤在创建时由 `AddSkinForBase()` 写入 `dicSkinData` 并随存档持久化（改配置不回填旧生物）。例外是默认武器：`GetSkinData` 装配时发现皮肤数据与装备栏都无武器、且当前配置 `equip_item_base_weapon≠0`，会按当前配置兜底补入默认武器皮肤（仅 showType=0 且 isNeedWeapon=true），旧存档生物因此自动拿上当前默认武器；装备栏有武器时装备皮肤仍顶替基础武器。
6. **与 creature-card-system 的边界**: 本 skill 负责生物实体/属性/数据，creature-card-system 负责卡片 UI 展示/交互。
7. **与 ai-system 的边界**: 本 skill 负责生物实体管理，ai-system 负责生物的行为决策（状态机）。
