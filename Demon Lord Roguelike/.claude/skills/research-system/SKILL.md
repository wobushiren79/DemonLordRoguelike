---
name: research-system
description: Demon Lord Roguelike 游戏的研究模块(Research)系统开发指南。使用此SKILL当需要创建或修改基地研究界面、研究节点、研究分支(设施/强化/魔物)、研究等级、研究解锁条件、研究购买流程、解锁存档(UserUnlock)、连线绘制、ResearchInfo/UnlockInfo 配置等。
watched_files:
  - Assets/Scripts/Component/UI/Game/BaseResearch/
  - Assets/Scripts/Component/UI/Popup/UIPopupResearchInfo.cs
  - Assets/Scripts/Component/UI/Popup/UIPopupResearchInfoComponent.cs
  - Assets/Scripts/Bean/MVC/Game/ResearchInfoBean.cs
  - Assets/Scripts/Bean/MVC/Game/ResearchInfoBeanPartial.cs
  - Assets/Scripts/Bean/MVC/Game/UnlockInfoBean.cs
  - Assets/Scripts/Bean/Game/UserUnlockBean.cs
  - Assets/Data/Excel/excel_research_info[研究信息].xlsx
  - Assets/Data/Excel/excel_unlock_info[解锁信息].xlsx
---

# 研究模块系统开发指南

## 核心概念

研究模块是基地科技树系统，玩家在基地（`UIBaseCore`）通过消耗水晶逐级研究节点，解锁设施、属性强化与魔物相关功能。研究节点之间存在前置依赖关系，通过连线（`ui_Line`）可视化展示，并按 **设施 / 强化 / 魔物** 三大类型分页切换。

### 系统架构

```
UIBaseCore (基地主界面)
    │  点击「研究」按钮
    ▼
UIBaseResearch (研究主界面)
    │  Tab 切换 → ResearchInfoTypeEnum (Building / Strengthen / Creature)
    │  RadioGroup → InitResearchItems(type, isInitContentPos)
    │
    ├─► UIViewBaseResearchItem (单节点 View)
    │     ├─ SetState  → 已解锁/未解锁/已满级 三态切换图标与色调
    │     ├─ SetLevel  → 显示研究等级 (1/levelMax 时隐藏)
    │     ├─ OnClickForPay → 弹出确认 → 扣水晶 → AddUnlock → SaveUserData → 解锁动画
    │     └─ ui_BG_PopupButtonCommonView → 悬停弹 UIPopupResearchInfo
    │
    └─► CreateLine (前置连线)
          └─ 取 GetPreUnlockIdsForLine → 对每个前置节点画一条线
             (仅同 ResearchType 才画线；解锁/未解锁线着色不同)

UserUnlockBean (玩家解锁存档)
    │  AddUnlock(unlockId, level) → 触发 User_AddUnlock 事件
    │  CheckIsUnlock(...) 支持 long / long[] / UnlockEnum / 字符串表达式
    │  GetUnlockResearchLeveByXxx 三种 ID 输入
    │  GetUnlockPortalShowCount / GetUnlockLineupNum / ... 衍生数值
```

### 数据流转

```
Excel: excel_research_info[研究信息].xlsx
   │ Editor 导出
   ▼
ResearchInfo.json (LoadResources/Data)
   │ ResearchInfoCfg.GetInitData(fileName)
   ▼
ResearchInfoBean (运行时)
   │ ResearchInfoCfg.GetResearchInfoByType(type)
   │ ResearchInfoCfg.GetItemDataByUnlockId(unlockId)
   ▼
UIBaseResearch.InitResearchItems(type)
```

```
玩家点击购买
   │ OnClickForPay
   ▼
检查 unlockLevel < level_max
检查 CheckHasCrystal(payCrystal)
   │ 用户确认
   ▼
CheckHasCrystal(..., isAddCrystal: true)   // 扣水晶（仅改内存）
   ▼
targetLevel = level + 1                           // 本次解锁后的目标等级
delayComplete = IsBuildingShowUnlock ? 0.5f : 0   // 仅设施解锁才延迟
AnimForUnlock(targetLevel, actionComplete, delayComplete) // 先播节点解锁动画（放大+抖动）
   │ 动画播完后立刻 SetStateForLevel(targetLevel)：把本节点刷新为已解锁外观（图标+颜色）+播粒子
   │ ——此刻尚未 AddUnlock，研究 UI 仍可见，故「节点动画播完即显示已解锁」
   │ 设施解锁再延迟 0.5s（展示粒子）后执行 actionComplete，其他解锁立即执行：
   ▼
UserUnlockBean.AddUnlock(unlock_id, targetLevel)
   │ 触发 EventsInfo.User_AddUnlock（此时才切设施镜头/播设施出现动画/隐藏研究 UI）
SaveUserData()                             // 此刻才落盘（扣费+解锁一起持久化）
   ▼
InitResearchItems(researchInfoType)        // 整页刷新（重画连线）
```

> **为什么 AddUnlock/SaveUserData 推迟到动画后**：`AddUnlock` 会同步触发 `User_AddUnlock`，`ScenePrefabForBase.EventForUserAddUnlock` 会立刻切到设施镜头并隐藏研究 UI 播设施出现动画。若与节点解锁动画同时发生会互相冲突，故把 `AddUnlock`/`SaveUserData`/刷新放进 `AnimForUnlock` 的完成回调，确保「节点解锁动画 → 设施镜头切换+设施出现动画」顺序播放。扣费只改内存、落盘随回调里的 `SaveUserData` 一起完成，动画期间锁屏，中途异常退出则扣费与解锁均未持久化，数据保持一致。

> **为什么动画播完要先 `SetStateForLevel(targetLevel)`**：解锁数据要等到 `actionComplete` 里才 `AddUnlock`，而 `AddUnlock` 又会同步隐藏研究 UI 去播设施出现动画。若只靠回调里的 `InitResearchItems` 刷新本节点外观，玩家会看到「节点动画播完图标仍是未解锁占位（白）→ 设施动画播完才变已解锁」。因此在 `AnimForUnlock` 的 `OnComplete` 里、`AddUnlock` 之前，先用解锁后的 `targetLevel` 直接刷新本节点图标/颜色为已解锁外观，保证「节点动画一播完就显示已解锁」，再播设施动画。

---

## ResearchInfoBean - 研究配置

**文件**: `Assets/Scripts/Bean/MVC/Game/ResearchInfoBean.cs`（自动生成，禁止直接修改）
**扩展**: `Assets/Scripts/Bean/MVC/Game/ResearchInfoBeanPartial.cs`

### 字段说明

| 字段 | 类型 | 含义 |
|------|------|------|
| `id` | long | 研究节点主键（继承自 BaseBean） |
| `research_type` | int | 1=设施, 2=强化, 3=魔物 → `ResearchInfoTypeEnum` |
| `icon_res` | string | 图标资源 key（已解锁时使用；未解锁固定显示 `ui_unlock_1`） |
| `level_max` | int | 升级上限（1 表示纯解锁型节点；>1 为可升级节点） |
| `position_x / position_y` | float | 节点在 `ui_Content` 中的 anchoredPosition |
| `unlock_id` | long | 该研究对应的解锁 ID（写入 `UserUnlockBean` 的 key） |
| `pre_unlock_ids` | string | 前置解锁表达式，`,` 与、`|` 或、可嵌套 |
| `pay_crystal` | string | 水晶消耗（三种格式见下） |
| `name` | long | 名称多语言 textId（自动生成 `name_language` getter） |
| `remark` | string | 备注（不参与逻辑） |

### `pay_crystal` 三种格式

```csharp
// 1. 单值：仅 level_max=1 的节点
"100"  →  arrayPayCrystal = [100]

// 2. 逗号分隔：每级独立配置
"100,200,400"  →  arrayPayCrystal = [100, 200, 400]

// 3. 基础*倍率：按 level_max 自动生成阶梯
//    arrayPayCrystal[i] = base + (base * rate * i)
"100*2"  →  base=100, rate=2, itemPay=200
         →  arrayPayCrystal[0..level_max-1] = [100, 300, 500, ...]
```

### Partial 扩展方法

```csharp
// 前置解锁 ID 解析（用于连线绘制）
// 注意：仅用于连线，因此 OR 关系会被「拍平」成所有候选 ID 都画线
public List<long> GetPreUnlockIdsForLine();

// 研究类型枚举映射
public ResearchInfoTypeEnum GetResearchType();

// 获取指定等级的水晶价格
// researchLevel 越界会被钳制到 [1, arrayPayCrystal.Length]
public long GetPayCrystal(int researchLevel);
```

### Cfg 扩展方法

```csharp
// 按解锁 ID 查询研究（缓存字典）
public static ResearchInfoBean GetItemDataByUnlockId(long unlockId);

// 按研究类型查询研究列表（缓存字典）
public static List<ResearchInfoBean> GetResearchInfoByType(ResearchInfoTypeEnum type);
```

---

## UnlockInfoBean - 解锁条目配置

**文件**: `Assets/Scripts/Bean/MVC/Game/UnlockInfoBean.cs`

```csharp
public partial class UnlockInfoBean : BaseBean
{
    public int unlock_type;  // 0=研究, 1=扭蛋机
    public string remark;
}
```

> 此表的主键 `id` 必须与 ResearchInfo 的 `unlock_id` 对齐——它是解锁系统的"全局解锁字典"，研究只是其中一种来源。

---

## ResearchInfoTypeEnum - 研究类型

**文件**: `Assets/Scripts/Enums/GameStateEnum.cs`

```csharp
public enum ResearchInfoTypeEnum
{
    None = 0,
    Building = 1,    // 设施相关
    Strengthen = 2,  // 强化相关
    Creature = 3,    // 生物相关
}
```

## UnlockEnum - 关键解锁 ID

**文件**: `Assets/Scripts/Enums/GameStateEnum.cs`

定义了游戏中被代码硬引用的关键解锁 ID（如 `LineupNum`、`CreatureVat`、`GashaponRarityR` 等）。

```csharp
public enum UnlockEnum : long
{
    CreatureVat = 100000000,           // 生物进阶
    CreatureVatAdd = 100000001,        // 生物进阶设置+1(可升级, 100000001~100000005 为其 Lv1~Lv5 解锁条目, 占满 1000 段前 6 个 id)
    CreatureVatBuffPreview = 100000006,// 生物进阶-进阶增益BUFF数值范围预览(设施段, pre=CreatureVat; 因 1~5 被 +1 的 Lv 占用故取 6)
    CreatureVatAddProgress = 100000007,// 生物进阶-魔晶加速研究(恒消耗1魔晶,研究等级=每次进度增加秒数=进度倍率; 0级隐藏加速按钮, level_max=5)
    CreatureVatMaterialNum = 100000008,// 生物进阶-素材魔物可选上限+1(每级+1, 基础5, level_max=5, 满级10)
    Altar = 100100001,                 // 祭坛
    SacrificeNum = 100100002,          // 增加献祭祭品数量(+1/级, level_max=10)
    SacrificePityRate = 100100003,     // 献祭失败保底概率提升(+5%/级, level_max=10)
    SacrificeDifferentIdRate = 100100004, // 不同魔物献祭成功率提升(+5%/级, level_max=10)
    DoomCouncil = 100200001,           // 终焉议会模块
    PortalShowNum = 100300001,         // 传送门显示数量
    PortalPreviewRoadNum = 100300002,    // 传送门详情预览-线路数
    PortalPreviewFightNum = 100300003,   // 传送门详情预览-关卡数
    PortalPreviewRoadLength = 100300004, // 传送门详情预览-路径长度
    PortalPreviewReward = 100300005,     // 传送门详情预览-奖励道具
    PortalRefreshNum = 100300006,        // 传送门刷新次数(研究等级=可用刷新次数上限,通关回满,level_max=10)
    GashaponMachine = 100400000,       // 解锁孕育
    GashaponRarityR = 100401000,       // 稀有度R
    GashaponRarityRRate = 100401001,   // 稀有度R +1%
    // ... 更多见源文件
    LineupCreatureAddNum = 200000001,  // 阵容生物上限+1
    LineupNum = 200100001,             // 解锁多阵容
    DropCrystalLifeTime = 200200001,   // 魔晶掉落物存在时长+5秒/级(level_max=6)
    DemonLordMPMax = 200300001,        // 魔王魔力上限+10/级(level_max=5)
    DemonLordMPF = 200400001,          // 魔王魔力恢复速度+1/秒/级(level_max=3)
    AbyssalBlessingRefreshNum = 200500001, // 深渊馈赠刷新次数(研究等级=单次征服run内可用刷新次数上限,level_max=5,新run自动回满)
    SpaceDash = 200600001,             // 空格突进(基地控制,level_max=3;1/2/3级向朝向突进1/2/3距离单位)
    SpaceDashCD = 200700001,           // 空格突进冷却缩减(子研究,前置=SpaceDash,level_max=4;默认3s每级-0.5最低1s)
    EquipRewardHuman = 300100301,      // 人类装备奖励
    EquipRewardSkeleton = 300200301,
}
```

> **何时需要新增 `UnlockEnum`？** 当 C# 代码需要直接判定/读取某个解锁等级时（例如 `GetUnlockLineupNum` 中读取 `LineupNum` 的研究等级）。纯前置依赖型节点不需要进入枚举，仅在 Excel 中维护即可。

### 设施分支(1000 段) — 生物进阶「进阶增益范围预览」节点

魔物进阶(孵化缸 `UICreatureVat`)的进阶详情面板里，每条候选稀有度 BUFF 的悬浮气泡(`UIViewCreatureVatAscendBuffItem`)默认展示 BUFF 效果描述、但数值占位参数(`{Percentage}` 等)统一用 `???` 替代（数值待进阶开始时随机确定）。新增设施研究节点 `CreatureVatBuffPreview`(unlock_id **100000006**, research 节点 id 100000003, `research_type=1`, `pre_unlock_ids=100000000` 即需先解锁进阶设施, `level_max=1`, `pay_crystal=500`)门控该数值范围预览：**解锁后** `???` 换成 `min~max` 范围（唯一随机值是进阶增益属性百分比 `{Percentage}` = `trigger_value_rate_min~trigger_value_rate`，按整数百分点；素材命中该 id 时下限抬高，与 `BuffBean.CreateRandomWithFloor` 同口径；`{Time_S}`/`{KillNum}` 等固定条件显示实际值）。落表同其他节点：`excel_unlock_info`(id=100000006,unlock_type=0) + `excel_research_info`(name textId 同 unlock_id=100000006) + 多语言 `excel_language` 的 `ResearchInfo` 工作表(id=100000006, cn「进阶增益范围预览」/en「Buff Range Preview」)。

> **1000 段 id 已用满前 6 个**：`100000000`=进阶设施、`100000001~100000005`=进阶设施+1 的 Lv1~Lv5 解锁条目，故本预览节点 unlock_id 从 `100000006` 起。UI 门控在 `UIViewCreatureVatAscendBuffItem.GetBuffContentForPreview` 用 `CheckIsUnlock(UnlockEnum.CreatureVatBuffPreview)`。

### 设施分支(1003 段) — 传送门详情预览 4 节点

传送门详情弹窗 `UIPopupPortalDetails` 的四项预览各由一个**设施研究节点**(`research_type=1`)门控：未解锁则该详情项整行隐藏（奖励区不显示），名字行始终显示。无尽模式不展示关卡数/路径长度/奖励。

| UnlockEnum | unlock_id | 预览项 | 详情项(View) | 备注 |
|------------|-----------|--------|--------------|------|
| `PortalPreviewRoadNum` | 100300002 | 线路数 | RoadNum | |
| `PortalPreviewFightNum` | 100300003 | 关卡数 | FightNum | 无尽模式不展示 |
| `PortalPreviewRoadLength` | 100300004 | 路径长度 | RoadLength | UI 文本 id 414；无尽模式不展示 |
| `PortalPreviewReward` | 100300005 | 奖励道具 | 奖励缓存池(`ui_UIViewItem` 模板) | 无尽模式不展示 |

> 同段 `PortalShowNum`(100300001) 为传送门显示数量、`PortalRefreshNum`(100300006) 为传送门刷新次数（研究等级=可用刷新次数上限，`level_max=10`，每通关一次世界回满；`UIBasePortal` 用 `CheckIsUnlockPortalRefresh` 门控刷新按钮显隐、`GetUnlockPortalRefreshMax` 取上限），均属同一 1003 设施段。上述节点均落在 `excel_research_info`(各一条 `research_type=1` 节点) + `excel_unlock_info`(`unlock_type=0`，`id` 同 `unlock_id`) + 多语言 `Language_ResearchInfo`(节点名 textId)。

**UI 门控范例**（`UIPopupPortalDetails`）：用 `UserUnlock.CheckIsUnlock(UnlockEnum)` 决定每个详情项是否显示，未解锁整行隐藏：

```csharp
var userUnlock = GameDataHandler.Instance.manager.GetUserData().GetUserUnlockData();
// 每项调用 UIViewPopupPortalDetailsItem.SetData(title, content, isShow)
// isShow = 是否解锁对应设施研究 (无尽模式额外把关卡数/路径长度/奖励压成 false)
ui_RoadNum.SetData(title, content, userUnlock.CheckIsUnlock(UnlockEnum.PortalPreviewRoadNum));
ui_RoadLength.SetData(title, content, userUnlock.CheckIsUnlock(UnlockEnum.PortalPreviewRoadLength));
// 奖励区：仅当 CheckIsUnlock(UnlockEnum.PortalPreviewReward) 时才生成奖励道具图标，否则整区隐藏
```

> `UIPopupPortalDetails` 详细改造（AutoLink 的 4 个 `UIViewPopupPortalDetailsItem`、奖励缓存池、预生成奖励来源 `GameWorldInfoRandomBean.GetDifficultyReward`）属传送门/征服模块，本 Skill 仅覆盖"研究门控"这一面。

---

## UserUnlockBean - 玩家解锁存档

**文件**: `Assets/Scripts/Bean/Game/UserUnlockBean.cs`

### 数据结构

```csharp
public Dictionary<long, UserUnlockInfoBean> unlockInfoData;  // key = unlock_id

public class UserUnlockInfoBean
{
    public long unlockId;
    public int unlockLevel;  // 默认 1；可升级研究存储当前等级
}
```

### 解锁操作

```csharp
// 新增/覆盖解锁（只要解锁状态或等级真正变化都触发 EventsInfo.User_AddUnlock）
// - 若不存在：按传入 unlockLevel 创建条目，并触发 User_AddUnlock(unlockId)
// - 若已存在且等级变化：覆盖 unlockLevel，并触发 User_AddUnlock(unlockId)
// - 若已存在且等级未变：不做任何事（避免重复触发）
// ⚠ 可升级解锁(如 CreatureVatAdd 容器+1)后续升级也会发事件 → 驱动 ScenePrefabForBase 刷新/出现动画；
//   若只在「首次新增」才发事件，升级出的新容器要重进游戏才显示（历史 bug）
public void AddUnlock(long unlockId, int unlockLevel = 1);
```

### 解锁检测（多种重载）

```csharp
// 单 ID（unlockId == 0 视为始终解锁）
public bool CheckIsUnlock(long unlockId);

// 枚举
public bool CheckIsUnlock(UnlockEnum unlockEnum);

// long[] 与判定
public bool CheckIsUnlock(long[] unlockIds);

// 字符串表达式：',' = AND, '|' = OR, 可嵌套
//   "1"          → 1 解锁
//   "1,2"        → 1 且 2
//   "1|2"        → 1 或 2
//   "1,2|3,4"    → 1 且 (2 或 3) 且 4
public bool CheckIsUnlock(string unlockStr);

// 统计 long[] 中解锁了几个（用于进度展示）
public int CheckIsUnlockNum(long[] unlockIds);
```

### 研究等级获取

```csharp
public int GetUnlockResearchLeveByUnlockEnum(UnlockEnum unlockEnum);
public int GetUnlockResearchLeveByUnlockId(long unlockId);
public int GetUnlockResearchLeveByResearchId(long researchId);
public int GetUnlockResearchLevelByResearchInfo(ResearchInfoBean researchInfo);
```

### 解锁衍生数值（业务方法）

```csharp
public int GetUnlockPortalShowCount();                 // 3 + PortalShowNum 等级
public int GetUnlockPortalRefreshMax();                // 传送门刷新次数上限 = PortalRefreshNum 等级(未解锁0,满级10)
public bool CheckIsUnlockPortalRefresh();              // 是否解锁传送门刷新(等级>0,门控刷新按钮显隐)
public int GetUnlockAbyssalBlessingRefreshMax();       // 深渊馈赠刷新次数上限 = AbyssalBlessingRefreshNum 等级(未解锁0,满级5)；剩余次数池挂 FightBeanForConquer(整个征服run共享,新run自动回满)
public bool CheckIsUnlockAbyssalBlessingRefresh();     // 是否解锁深渊馈赠刷新(等级>0,门控 UIFightAbyssalBlessing 刷新按钮显隐)
public int GetUnlockLineupNum();                       // 1 + LineupNum 等级
public int GetUnlockLineupCreatureNum();               // 6 + LineupCreatureAddNum 等级
public int GetUnlockGameWorldConquerDifficultyLevel(long worldId);
public int GetUnlockCreatureVatNum();                  // 未解锁返回 0；已解锁返回 1+CreatureVatAdd
public int GetUnlockCreatureVatAddProgressLevel();     // 生物进阶魔晶加速研究等级(0~5)；恒消耗1魔晶,等级=每次进度增加秒数=进度倍率，0级隐藏加速按钮
public int GetUnlockCreatureVatMaterialMax();          // 进阶素材可选上限 = 5(creatureVatMaterialMax) + CreatureVatMaterialNum 等级(满级 10)
public int GetUnlockSacrificeMax();                    // 献祭祭品选择上限 = 5(sacrificeMax) + SacrificeNum 等级(满级 15)
public float GetUnlockSacrificeFailPityAddRate();      // 献祭失败保底增量 = SacrificePityRate 等级 × 5%(未解锁0,满级50%)
public float GetUnlockSacrificeDifferentIdRate();      // 单个不同id祭品成功率 = SacrificeDifferentIdRate 等级 × 5%(未解锁0,满级50%)
public float GetUnlockDropCrystalAddLifeTime();        // 魔晶掉落物额外存在时长 = DropCrystalLifeTime 等级 × 5秒(未解锁0,满级+30s)；在 FightCreatureEntity.DropCrystal 叠加到 FightDropCrystalBean.BASE_LIFE_TIME(30)
public float GetUnlockDemonLordMPMaxAddValue();        // 魔王魔力上限加成 = DemonLordMPMax 等级 × 10(未解锁0,满级+50)；在 FightCreatureBean.RefreshBaseAttribute 仅对 FightDefenseCore 叠加到 MP
public float GetUnlockDemonLordMPFAddValue();          // 魔王魔力恢复速度加成 = DemonLordMPF 等级 × 1/秒(未解锁0,满级+3/s)；同上叠加到 MPF
public int GetUnlockSpaceDashLevel();                  // 空格突进研究等级 = SpaceDash 等级(0=未解锁不可突进,1/2/3级=1/2/3距离单位)；由 ControlForGameBase 读取决定突进距离
public float GetUnlockSpaceDashCD();                   // 空格突进冷却(秒) = 3 - SpaceDashCD 等级×0.5(未解锁3s,每级-0.5,满级最低1s)；由 ControlForGameBase 读取决定突进CD
```

### 解锁列表

```csharp
public List<long> GetUnlockGameWorldIds();      // 默认包含 worldId=1，再遍历配置
public List<long> GetUnlockCreatureModelIds();  // 遍历生物模型，按 unlock_id 过滤
```

---

## UIBaseResearch - 研究主界面

**文件**: `Assets/Scripts/Component/UI/Game/BaseResearch/UIBaseResearch.cs`

继承 `BaseUIComponent`，实现 `IRadioGroupCallBack`，由三个 partial 文件组成：
- `UIBaseResearch.cs` — 业务逻辑（节点创建、连线、缩放、退出）
- `UIBaseResearchComponent.cs` — AutoLink 字段（`ui_Content`、`ui_Line`、`ui_TitleRadioGroup`...）
- `UIBaseResearchTest.cs` — 编辑器测试模式（节点拖拽位置写回 Excel）

### 核心字段（AutoLink）

```csharp
public RectTransform ui_Content;                             // 节点根容器（缩放对象）
public UIViewBaseResearchItem ui_UIViewBaseResearchItem;     // 节点 prefab
public RectTransform ui_Line;                                // 连线 prefab 容器
public Image ui_Line_Item;                                   // 连线 prefab
public Button ui_ViewExit;                                   // 退出按钮
public ParticleSystem ui_EffectUnlock;                       // 解锁粒子
public RadioButtonView ui_UIViewCommonLabel_Building;        // Tab: 设施
public RadioButtonView ui_UIViewCommonLabel_Strengthen;      // Tab: 强化
public RadioButtonView ui_UIViewCommonLabel_Creature;        // Tab: 魔物
public RadioGroupView ui_TitleRadioGroup;
public Button ui_TestSaveBtn;                                // 仅测试模式可见
```

### 关键方法

```csharp
// 打开/关闭：进入默认选中第一个 Tab；关闭时 SaveUserData
public override void OpenUI();
public override void CloseUI();

// 按类型初始化：清空并重建节点 + 连线
public void InitResearchItems(ResearchInfoTypeEnum type, bool isInitContentPos = false);

// 前置过滤：只有 pre_unlock_ids 全部解锁的节点才会被创建（满足才"亮"出来）
public bool CheckPreIsUnlock(ResearchInfoBean info);

// 节点池：复用 listResearchItemView
public void CreateResearchItem(int index, ResearchInfoBean info);

// 连线绘制：基于贝塞尔/直线长度+角度+共享 material 颜色着色
//   - 已解锁(isUnlockSelf): _Color2 = #3C0031, _WaveSpeed=1
//   - 未解锁:               _Color2 = #3C003100, _WaveSpeed=10
public void CreateLine(ResearchInfoBean targetInfo);

// 滚轮缩放（0.5 ~ 1.0）
public void ChangeContentSize(float changeSize);

// 解锁粒子动画
public void AnimForShowUnlockEffect(Vector2 position);

// 退出回基地
public void OnClickForExit();  // → UIBaseCore
```

### Tab 切换回调

```csharp
public void RadioButtonSelected(RadioGroupView rg, int position, RadioButtonView rb)
{
    if (rb == ui_UIViewCommonLabel_Building)
        InitResearchItems(ResearchInfoTypeEnum.Building, true);
    else if (rb == ui_UIViewCommonLabel_Strengthen)
        InitResearchItems(ResearchInfoTypeEnum.Strengthen, true);
    else if (rb == ui_UIViewCommonLabel_Creature)
        InitResearchItems(ResearchInfoTypeEnum.Creature, true);
}
```

---

## UIViewBaseResearchItem - 研究节点 View

**文件**: `Assets/Scripts/Component/UI/Game/BaseResearch/UIViewBaseResearchItem.cs`

```csharp
public void SetData(ResearchInfoBean researchInfo);

public void SetLevel();   // level == 0 || level == max 时隐藏数字
public void SetState();   // 读当前存档等级 → SetStateForLevel(currentLevel)
// 按指定等级刷新外观：三态 未解锁(mask + ui_unlock_1) / 已解锁未满(白色) / 已满(紫色)
// 抽出以便解锁动画播完、AddUnlock 之前就用 targetLevel 把本节点刷成已解锁外观
public void SetStateForLevel(int unlockLevel);
public void SetIcon(string iconRes);

// 购买流程：检查满级 → 检查水晶 → 弹 DialogNormal → 扣水晶 → AnimForUnlock(回调里 AddUnlock → SaveUserData → 刷新)
public void OnClickForPay();

// 解锁动画：放大+抖动+缩回；OnComplete 里先 SetStateForLevel(targetLevel) 把本节点刷成已解锁外观+播粒子，
// 再按 delayComplete 回调 actionComplete（由调用方提交解锁/刷新，从而在动画后才触发设施镜头切换+出现动画）
// delayComplete>0(仅设施解锁)时回调前延迟该秒数让粒子先展示，期间保持锁屏；其他解锁传0立即回调
public void AnimForUnlock(int targetLevel, Action actionComplete = null, float delayComplete = 0f);
```

### AutoLink 字段

```csharp
public Image ui_Icon;
public Button ui_BG_Button;
public PopupButtonCommonView ui_BG_PopupButtonCommonView;  // 悬停气泡触发器
public Image ui_Board;
public MaskUIView ui_UIViewBaseResearchItem_MaskUIView;     // 未解锁灰罩
public RectTransform ui_UIViewBaseResearchItem_RectTransform;
public TextMeshProUGUI ui_Level;
```

### 购买确认弹窗

```csharp
DialogBean dialogData = new DialogBean();
dialogData.content = string.Format(TextHandler.Instance.GetTextById(62001), payCrystal);
dialogData.actionSubmit = (view, data) =>
{
    if (!userData.CheckHasCrystal(payCrystal, isHint: true, isAddCrystal: true)) return;
    int targetLevel = level + 1;
    //先播节点解锁动画，播完即把本节点刷为已解锁外观(SetStateForLevel)，再提交解锁(触发设施镜头切换/出现动画)，避免与节点动画冲突
    AnimForUnlock(targetLevel, () =>
    {
        userUnlock.AddUnlock(researchInfo.unlock_id, targetLevel);
        GameDataHandler.Instance.manager.SaveUserData();   // 动画后才落盘(扣费+解锁一起)
        var targetUI = UIHandler.Instance.GetUI<UIBaseResearch>();
        targetUI.InitResearchItems(targetUI.researchInfoType);  // 整页刷新
    });
};
UIHandler.Instance.ShowDialogNormal(dialogData);
```

---

## UIPopupResearchInfo - 节点信息气泡

**文件**: `Assets/Scripts/Component/UI/Popup/UIPopupResearchInfo.cs`

继承 `PopupShowCommonView`，挂到节点的 `ui_BG_PopupButtonCommonView` 上，通过 `PopupEnum.ResearchInfo` 注册。

### SetData 流程

```csharp
public override void SetData(object data)
{
    researchInfo = (ResearchInfoBean)data;
    var userUnlock = userData.GetUserUnlockData();
    int currentLevel = userUnlock.GetUnlockResearchLevelByResearchInfo(researchInfo);
    long payCrystal = researchInfo.GetPayCrystal(currentLevel + 1);

    SetName(researchInfo.name_language);
    SetIcon(researchInfo.icon_res);
    SetPayCrystal(payCrystal);
    SetLevel(researchInfo.level_max, currentLevel);
    SetResearchState();    // 已满级时隐藏前置条件区
    RefreshUILayout();
}
```

### 文本 ID 约定

| 文本 ID | 含义 |
|---------|------|
| `1003001` | `"等级 {0}"` 格式 |
| `1003002` | `"已满级"` |
| `62001` | `"消耗 {0} 水晶确认购买?"` |

---

## UIBaseResearchTest - 编辑器测试模式

**文件**: `Assets/Scripts/Component/UI/Game/BaseResearch/UIBaseResearchTest.cs`

允许在游戏内调整节点坐标后一键回写 Excel，仅在 `SetDataForTest()` 后才显示保存按钮。

```csharp
public void SetDataForTest()
{
    isTest = true;
    SetData();
    ui_TestSaveBtn.gameObject.SetActive(true);
}

public void SaveResearchDataForTest()
{
    var listData = new List<ExcelChangeData>();
    foreach (var item in listResearchItemView)
    {
        long id = item.researchInfo.id;
        var pos = item.rectTransform.anchoredPosition;
        listData.Add(new ExcelChangeData(id, "position_x", $"{(int)pos.x}"));
        listData.Add(new ExcelChangeData(id, "position_y", $"{(int)pos.y}"));
    }
    ExcelUtil.SetExcelData("Assets/Data/Excel/excel_research_info[研究信息].xlsx",
                           "ResearchInfo", listData);
}
```

> **入口**: `LauncherTest.cs:147` 通过 `UIHandler.Instance.OpenUIAndCloseOther<UIBaseResearch>()` 打开后调用 `SetDataForTest()`。

---

## 事件清单

| 事件常量 | 触发位置 | 监听位置 | 用途 |
|----------|----------|----------|------|
| `EventsInfo.User_AddUnlock` | `UserUnlockBean.AddUnlock(...)` 新增解锁**或等级变化**时 | `ScenePrefabForBase`（全工程唯一监听者） | 通知基地场景刷新对应解锁的内容（建筑出现动画/打开新功能入口）；非建筑解锁在 `EventForUserAddUnlock` 中直接 return，无副作用 |

---

## 常见任务模板

### 1. 新增一个研究节点

#### 步骤
1. **在 `excel_unlock_info[解锁信息].xlsx` 添加解锁条目**：分配唯一 `id`，设 `unlock_type=0`
2. **在 `excel_research_info[研究信息].xlsx` 添加研究记录**：
   - `unlock_id` 指向上一步分配的 ID
   - `research_type` 选 1/2/3
   - `pre_unlock_ids` 写前置（可空）
   - `pay_crystal` 用三种格式之一
   - `level_max` 决定可升级次数
   - `position_x / position_y` 通过 `UIBaseResearchTest` 在游戏中拖完保存
   - 节点名 `name` textId 在 `excel_research_info` 的多语言表 `Language_ResearchInfo` 中补齐（如四个传送门预览节点）
3. **如需代码读取该研究等级/判定解锁**：在 `UnlockEnum` 中追加对应常量（如设施段 `PortalPreview*`，UI 用 `CheckIsUnlock(UnlockEnum)` 门控显示）
4. **如需衍生数值**：在 `UserUnlockBean` 的"解锁数值获取"区域追加方法
5. **配置表导出**：通过 `ExcelEditorWindow` 重新导出 JSON

### 2. 新增一个 UnlockEnum 衍生数值方法

```csharp
// 在 UserUnlockBean.cs 「解锁数值获取」#region 中追加
/// <summary>
/// 获取 XXX 数量
/// 基础数量 N + 对应研究等级
/// </summary>
public int GetUnlockXxxCount()
{
    return BASE_COUNT + GetUnlockResearchLeveByUnlockEnum(UnlockEnum.Xxx);
}
```

### 3. 检测多条件解锁

```csharp
// 表达式：A 且 (B 或 C)
bool unlocked = userUnlock.CheckIsUnlock($"{(long)UnlockEnum.A},{(long)UnlockEnum.B}|{(long)UnlockEnum.C}");

// 或者用 long[] 全部 AND
bool allUnlocked = userUnlock.CheckIsUnlock(new long[] { idA, idB, idC });
```

### 4. 监听解锁事件刷新 UI

```csharp
public override void RegisterEvent()
{
    base.RegisterEvent();
    EventHandler.Instance.RegisterEvent<long>(EventsInfo.User_AddUnlock, OnUserAddUnlock);
}

public override void UnRegisterEvent()
{
    base.UnRegisterEvent();
    EventHandler.Instance.UnRegisterEvent<long>(EventsInfo.User_AddUnlock, OnUserAddUnlock);
}

private void OnUserAddUnlock(long unlockId)
{
    // 根据 unlockId 决定是否需要刷新本 UI
    if (unlockId == (long)UnlockEnum.Xxx) RefreshUI();
}
```

### 5. 程序化解锁（如测试/作弊）

```csharp
var userUnlock = GameDataHandler.Instance.manager.GetUserData().GetUserUnlockData();
userUnlock.AddUnlock((long)UnlockEnum.LineupNum, 3);  // 直接给到 3 级
GameDataHandler.Instance.manager.SaveUserData();
```

---

## 文件位置速查

| 功能 | 文件路径 |
|------|----------|
| 研究主界面（逻辑） | `Assets/Scripts/Component/UI/Game/BaseResearch/UIBaseResearch.cs` |
| 研究主界面（AutoLink） | `Assets/Scripts/Component/UI/Game/BaseResearch/UIBaseResearchComponent.cs` |
| 研究主界面（测试模式） | `Assets/Scripts/Component/UI/Game/BaseResearch/UIBaseResearchTest.cs` |
| 研究节点 View | `Assets/Scripts/Component/UI/Game/BaseResearch/UIViewBaseResearchItem.cs` |
| 研究节点 View（AutoLink） | `Assets/Scripts/Component/UI/Game/BaseResearch/UIViewBaseResearchItemComponent.cs` |
| 研究气泡 | `Assets/Scripts/Component/UI/Popup/UIPopupResearchInfo.cs` |
| 研究气泡（AutoLink） | `Assets/Scripts/Component/UI/Popup/UIPopupResearchInfoComponent.cs` |
| 研究配置 Bean | `Assets/Scripts/Bean/MVC/Game/ResearchInfoBean.cs` |
| 研究配置 Partial | `Assets/Scripts/Bean/MVC/Game/ResearchInfoBeanPartial.cs` |
| 解锁配置 Bean | `Assets/Scripts/Bean/MVC/Game/UnlockInfoBean.cs` |
| 玩家解锁存档 | `Assets/Scripts/Bean/Game/UserUnlockBean.cs` |
| 研究/解锁枚举 | `Assets/Scripts/Enums/GameStateEnum.cs` |
| 气泡枚举 | `Assets/Scripts/Enums/PopupEnum.cs` |
| 研究 Excel | `Assets/Data/Excel/excel_research_info[研究信息].xlsx` |
| 解锁 Excel | `Assets/Data/Excel/excel_unlock_info[解锁信息].xlsx` |

---

## 注意事项

1. **Bean 自动生成不可改**：`ResearchInfoBean.cs` 和 `UnlockInfoBean.cs` 修改会被覆盖；扩展统一写入 `*Partial.cs`
2. **Excel 必须用 openpyxl**：通过 `.claude/scripts/excel_*.py` 工具操作 `.xlsx`，禁止 pandas/xlrd
3. **解锁 ID ≠ 研究 ID**：`UserUnlockBean.unlockInfoData` 的 key 是 `unlock_id`，所有解锁检测都用 `unlock_id`
4. **前置 OR 关系连线会全部画出**：`GetPreUnlockIdsForLine` 把 `|` 拍平为多个 ID，因此一个 OR 节点会画到所有候选父节点的线；这是有意的视觉提示
5. **跨类型连线被吞掉**：`CreateLine` 中前置节点类型不同时跳过连线，但 **解锁条件仍生效**（`CheckPreIsUnlock` 依然检查全部前置）
6. **节点创建是按"已解锁前置"过滤的**：未达到前置的节点根本不会出现在界面上（"隐藏式"科技树，而非"灰色锁定"）
7. **缩放范围固定 0.5 ~ 1.0**：通过滚轮调节 `ui_Content.localScale`，按帧 ×`Time.deltaTime`×`SpeedForChangeContentSize`
8. **退出固定跳基地**：`OnClickForExit` → `UIBaseCore`，不要改成 GoBack/通用退出
9. **解锁动画后才落盘 + 关闭兜底存档**：`OnClickForPay` 确认后先扣水晶(仅改内存)再播 `AnimForUnlock`，**动画完成回调里**才 `AddUnlock` + `SaveUserData()` 落盘扣费与解锁并刷新页面；`CloseUI` 中再调一次 `SaveUserData()` 作为兜底。动画期间锁屏，中途异常退出则扣费与解锁均未持久化，数据一致
10. **解锁动画与设施出现动画串行 + 全程锁屏**：`AddUnlock` 会同步触发 `User_AddUnlock` → `ScenePrefabForBase.EventForUserAddUnlock` 切设施镜头/隐藏研究 UI/播设施出现动画；为避免与节点解锁动画冲突，把 `AddUnlock` 推迟到 `AnimForUnlock` 完成回调里，保证「节点解锁动画 → 设施镜头切换+出现动画」顺序播放。设施出现动画期间（仅当解锁发生在研究界面时）`EventForUserAddUnlock` 会 `ShowScreenLock()`，待出现动画+停留 1s 结束、还原镜头与研究 UI 后才 `HideScreenLock()`，故从节点解锁动画到设施出现动画结束全程锁屏不可操作
11. **解锁动画期间锁屏**：`AnimForUnlock` 开始即 `UIHandler.ShowScreenLock()`，并一直锁到动画播完后的 0.5s 粒子展示窗口结束（`DOVirtual.DelayedCall(0.5f)` 里才 `HideScreenLock()` 并执行回调）；紧接着回调里 `AddUnlock` 触发的设施出现动画又由 `EventForUserAddUnlock` 续上锁屏（见第 10 条），防止动画/粒子展示/设施出现期间再次点击或重复购买
12. **节点池复用**：`listResearchItemView` 不销毁仅 `SetActive(false)`，切 Tab 时复用；`ClearData(true)` 仅在 `CloseUI` 销毁
13. **测试模式坐标回写**：`UIBaseResearchTest.SaveResearchDataForTest` 强制 cast 为 `(int)`，Excel 中只存整数坐标
