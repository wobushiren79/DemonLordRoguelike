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
    │     ├─ OnClickForPay → 弹出确认 → 扣水晶 → AddUnlock → 解锁动画
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
CheckHasCrystal(..., isAddCrystal: true)   // 扣水晶
UserUnlockBean.AddUnlock(unlock_id, level+1)
   │ 触发 EventsInfo.User_AddUnlock
   ▼
AnimForUnlock → InitResearchItems(researchInfoType)  // 整页刷新（重画连线）
```

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
    CreatureVatAdd = 100000001,        // 生物进阶设置+1
    Altar = 100100001,                 // 祭坛
    SacrificeNum = 100100002,          // 增加献祭祭品数量(+1/级, level_max=10)
    DoomCouncil = 100200001,           // 终焉议会模块
    PortalShowNum = 100300001,         // 传送门显示数量
    GashaponMachine = 100400000,       // 解锁孕育
    GashaponRarityR = 100401000,       // 稀有度R
    GashaponRarityRRate = 100401001,   // 稀有度R +1%
    // ... 更多见源文件
    LineupCreatureAddNum = 200000001,  // 阵容生物上限+1
    LineupNum = 200100001,             // 解锁多阵容
    EquipRewardHuman = 300100301,      // 人类装备奖励
    EquipRewardSkeleton = 300200301,
}
```

> **何时需要新增 `UnlockEnum`？** 当 C# 代码需要直接判定/读取某个解锁等级时（例如 `GetUnlockLineupNum` 中读取 `LineupNum` 的研究等级）。纯前置依赖型节点不需要进入枚举，仅在 Excel 中维护即可。

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
// 新增/覆盖解锁
// - 若已存在：覆盖 unlockLevel
// - 若不存在：创建条目，并触发 EventsInfo.User_AddUnlock(unlockId)
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
public int GetUnlockLineupNum();                       // 1 + LineupNum 等级
public int GetUnlockLineupCreatureNum();               // 6 + LineupCreatureAddNum 等级
public int GetUnlockGameWorldConquerDifficultyLevel(long worldId);
public int GetUnlockCreatureVatNum();                  // 未解锁返回 0；已解锁返回 1+CreatureVatAdd
public int GetUnlockSacrificeMax();                    // 献祭祭品选择上限 = 5(sacrificeMax) + SacrificeNum 等级(满级 15)
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
public void SetState();   // 三态：未解锁(mask + ui_unlock_1) / 已解锁未满(白色) / 已满(紫色)
public void SetIcon(string iconRes);

// 购买流程：检查满级 → 检查水晶 → 弹 DialogNormal → 扣水晶 → AddUnlock → AnimForUnlock
public void OnClickForPay();

// 解锁动画：放大+抖动+缩回，结束后刷新整页（重画连线）
public void AnimForUnlock();
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
    userUnlock.AddUnlock(researchInfo.unlock_id, level + 1);
    AnimForUnlock();
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
| `EventsInfo.User_AddUnlock` | `UserUnlockBean.AddUnlock(...)` 新增解锁时 | `ScenePrefabForBase` 等 | 通知基地场景刷新对应解锁的内容（如打开新功能入口） |

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
3. **如需代码读取该研究等级**：在 `UnlockEnum` 中追加对应常量
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
9. **关闭时强制存档**：`CloseUI` 中调用 `SaveUserData()`，避免玩家解锁后未存档就退出
10. **解锁动画期间锁屏**：`AnimForUnlock` 调用 `UIHandler.ShowScreenLock()`，结束后 `HideScreenLock()`，防止动画中再次点击
11. **节点池复用**：`listResearchItemView` 不销毁仅 `SetActive(false)`，切 Tab 时复用；`ClearData(true)` 仅在 `CloseUI` 销毁
12. **测试模式坐标回写**：`UIBaseResearchTest.SaveResearchDataForTest` 强制 cast 为 `(int)`，Excel 中只存整数坐标
