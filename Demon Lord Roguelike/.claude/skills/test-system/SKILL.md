---
name: test-system
description: Demon Lord Roguelike 游戏的测试系统开发指南。使用此SKILL当需要创建或修改测试工具、测试UI、测试编辑器扩展、测试数据等，包括战斗场景测试、卡片测试、基地测试、奖励选择测试、终焉议会测试、NPC创建测试、研究UI测试等。
watched_files:
  - Assets/Scripts/Game/Launcher/LauncherTest.cs
  - Assets/Editor/GameTestEditor.cs
  - Assets/Editor/GameTestEditorPartial.cs
  - Assets/Scripts/Game/Logic/GameFightLogicTest.cs
  - Assets/Scripts/Bean/Game/FightBeanForTest.cs
  - Assets/FrameWork/Scripts/Component/UI/UITestConsole.cs
  - Assets/Scripts/Component/UI/Test/
  - Assets/Scripts/Component/UI/Game/BaseResearch/UIBaseResearchTest.cs
  - Assets/Scripts/Enums/GameStateEnum.cs
---

# 测试系统开发指南

## 核心概念

### 测试架构

```
LauncherTest                    - 测试启动器，初始化测试数据并提供测试入口
├── GameTestEditor              - Unity Inspector 扩展，可视化配置测试参数
├── GameFightLogicTest          - 测试战斗逻辑，支持循环重置
├── UITestConsole               - 游戏内日志控制台
└── 测试UI们
    ├── UITestBase              - GM工具面板
    ├── UITestCard              - 卡片显示参数校准
    ├── UITestNpcCreate         - NPC外观/属性/装备配置(预制版)
    ├── TestNpcCreateGUI        - NPC外观/属性/装备配置(纯IMGUI代码版，不依赖预制)
    └── UIBaseResearchTest      - 研究节点坐标配置
```

### 测试场景类型

```csharp
public enum TestSceneTypeEnum
{
    None = 0,
    NormalGame = 1,         // 正常游戏启动(走真实开始流程，免去切换 GameScene)
    FightSceneTest = 2,     // 战斗场景测试
    CardTest = 3,           // 卡片效果测试
    Base = 4,               // 基地测试
    RewardSelect = 5,       // 奖励选择
    DoomCouncil = 6,        // 终焉议会
    NpcCreate = 7,          // NPC创建
    ResearchUI = 8,         // 研究UI
    AbyssalBlessing = 9,    // 深渊馈赠UI
    CreatureSacrifice = 10, // 生物献祭升级测试
    CreatureVat = 11,       // 魔物进阶(生物升阶容器)测试
}
```

---

## 添加新的测试类型

### 1. 在枚举中添加类型

```csharp
// Assets/Scripts/Enums/GameStateEnum.cs
public enum TestSceneTypeEnum
{
    // ... 现有类型
    MyNewTest = 8,          // 新测试类型
}
```

### 2. 在编辑器中添加绘制方法

```csharp
// Assets/Editor/GameTestEditor.cs
public partial class GameTestEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // ...
        switch (launcher.testSceneType)
        {
            // ... 现有分支
            case TestSceneTypeEnum.MyNewTest:
                DrawMyNewTest();
                break;
        }
        // ...
    }

    private void DrawMyNewTest()
    {
        showMyNewTest = EditorGUILayout.Foldout(showMyNewTest, "🆕 新测试", true);
        if (!showMyNewTest) return;

        EditorGUI.indentLevel++;
        EditorGUILayout.Space(5);

        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("▶️ 开始新测试", GUILayout.Height(30)) && Application.isPlaying)
        {
            launcher.StartForMyNewTest(myTestParam);
        }
        GUI.backgroundColor = Color.white;

        // 参数配置
        EditorGUILayout.BeginVertical("box");
        myTestParam = EditorGUILayout.IntField(new GUIContent("测试参数", "参数说明"), myTestParam);
        EditorGUILayout.EndVertical();

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);
    }
}
```

### 3. 在 Partial 中添加参数和持久化

```csharp
// Assets/Editor/GameTestEditorPartial.cs
public partial class GameTestEditor
{
    // 新测试参数
    public int myTestParam = 0;
    private bool showMyNewTest = true;

    private void LoadAllPreferences()
    {
        // ...
        myTestParam = EditorPrefs.GetInt(PREFS_KEY_PREFIX + "myTestParam", 0);
        // ...
    }

    private void SaveAllPreferences()
    {
        // ...
        EditorPrefs.SetInt(PREFS_KEY_PREFIX + "myTestParam", myTestParam);
        // ...
    }
}
```

### 4. 在 LauncherTest 中添加入口方法

```csharp
// Assets/Scripts/Game/Launcher/LauncherTest.cs
public class LauncherTest : BaseLauncher
{
    /// <summary>
    /// 开始新测试
    /// </summary>
    public void StartForMyNewTest(int param)
    {
        // 实现测试入口逻辑
        // 例如：打开UI、进入场景等
    }
}
```

---

## 创建新的测试 UI

### 继承 BaseUIComponent

```csharp
// Assets/Scripts/Component/UI/Test/UITestMyNew.cs
public partial class UITestMyNew : BaseUIComponent
{
    public override void OpenUI()
    {
        base.OpenUI();
        GameControlHandler.Instance.manager.EnableAllControl(false);
    }

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_MyButton)
        {
            OnClickForMyAction();
        }
    }

    /// <summary>
    /// 设置测试数据
    /// </summary>
    public void SetData(MyTestData data)
    {
        // 初始化UI数据
    }
}
```

### 对应 Component 文件

```csharp
// Assets/Scripts/Component/UI/Test/UITestMyNewComponent.cs
public partial class UITestMyNew
{
    // 自动链接的UI控件
    protected Button ui_MyButton;
    protected InputField ui_MyInput;
}
```

---

## 常用测试数据初始化

### 创建测试生物

`UITestBase` 提供两个发生物按钮（逻辑见 `OnClickForAddAllCreature` / `OnClickForAddTestCreature`）：

- **添加所有生物** `ui_BtnAddAllCreature`：忽略输入框，遍历 `CreatureInfoCfg.GetAllData()` 全部生物各发 1 只，稀有度随机 1-6、等级固定 0，仅 `AddSkinForBase()`（**不授稀有度 BUFF**）。
- **添加测试生物** `ui_BtnAddTestCreature`：输入1=生物ID(必填)；输入2=稀有度1-6(空=随机)；输入3=等级(空=随机0-10)。生成后走**孕育同款随机稀有度 BUFF 逻辑** `CreatureBean.RandomRarityBuffForCreate()`（按稀有度逐级授予，存入 `dicRarityBuff`，与扭蛋同口径）。

```csharp
// 添加测试生物核心(OnClickForAddTestCreature)
// 稀有度: 输入2(夹紧1-6)或随机; 等级: 输入3(>=0)或随机0-10
CreatureBean creatureData = new CreatureBean(targetId);
creatureData.rarity = rarity;
creatureData.level = level;
creatureData.AddSkinForBase();
creatureData.RandomRarityBuffForCreate();   // 走孕育同款随机稀有度BUFF
userData.AddBackpackCreature(creatureData);
```

> 三个输入框提示**直接写死不走多语言**，由 `UITestBase.InitInputPlaceholder()` 在 `OpenUI` 设置；输入2/3 常驻显示，仅"添加测试生物"读取（其余功能只用输入1）。

### 添加用户资源

```csharp
UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();

// 添加魔晶
userData.AddCrystal(99999);

// 添加声望
userData.AddReputation(1000);

// 添加道具(OnClickForAddItem)：输入空=遍历所有道具，输入=仅该道具ID；
// 两者都对每一种稀有度(N~L 共6级)各生成一个，走征服奖励装备生成单一真实源
for (int rarity = (int)RarityEnum.N; rarity <= (int)RarityEnum.L; rarity++)
{
    ItemBean rewardItem = RewardSelectBean.CreateEquipItemForReward(itemId, rarity);
    userData.AddBackpackItem(rewardItem);
}

// 添加生物
userData.AddBackpackCreature(creatureData);
userData.AddLineupCreature(lineupId, creatureData.creatureUUId);
```

### 解锁所有内容

```csharp
var userUnlockData = userData.GetUserUnlockData();
var allUnlockInfo = UnlockInfoCfg.GetAllArrayData();
allUnlockInfo.ForEach((index, value) =>
{
    var researchInfo = ResearchInfoCfg.GetItemDataByUnlockId(value.id);
    if (researchInfo == null)
    {
        userUnlockData.AddUnlock(value.id);
    }
    else
    {
        userUnlockData.AddUnlock(value.id, researchInfo.level_max);
    }
});
```

### 解锁所有世界征服难度

`UITestBase` 的 `ui_BtnWorldDifHalf` / `ui_BtnWorldDif` 两个按钮分别解锁所有世界的「一半难度(向上取整)」/「全部难度」，逻辑见 `OnClickForUnlockWorldDifficulty(bool isHalf)`：

```csharp
// 难度 = conquerDifficultyMax(基础) + 解锁ID(unlock_id_conquer_difficulty_level)对应的研究等级
int conquerDifficultyBase = userData.GetUserLimmitData().conquerDifficultyMax;
foreach (var itemData in GameWorldInfoCfg.GetAllData())
{
    long unlockId = itemData.Value.unlock_id_conquer_difficulty_level;      // 为0跳过(无可解锁难度)
    int configDifficultyMax = FightTypeConquerInfoCfg.GetMaxLevel(itemData.Key); // 该世界配置最高难度
    int targetDifficulty = isHalf ? Mathf.CeilToInt(configDifficultyMax / 2f) : configDifficultyMax;
    int needUnlockLevel = targetDifficulty - conquerDifficultyBase;          // ≤0跳过(基础已覆盖)
    userUnlockData.AddUnlock(unlockId);                 // 先确保条目存在
    userUnlockData.AddUnlock(unlockId, needUnlockLevel); // 再覆盖解锁等级
}
```

> 注意：`UserUnlockBean.AddUnlock(id, level)` 现已支持**新建时即按传入 level 创建**，故上面的「先建后设」两次调用已非必需，单次 `AddUnlock(unlockId, needUnlockLevel)` 即可（源码仍保留两次调用，结果等价无害，未一并改动）。

---

## 测试模拟不落盘（通用机制）

**"读真实存档 → 内存模拟 → 不写回"是献祭升级测试、魔物进阶测试共用的统一机制**，单一真实源是 `GameDataManager.isTestSimulation`：

- **单一开关**：`GameDataManager.isTestSimulation`（游戏层 partial）。为 `true` 时 `SaveUserData` **一律不落盘**（在 `SaveUserData(UserDataBean)` 入口统一 `return`），任何存档路径（含 UI 直接存档、结算存档、乃至进入基地时的附带存档）都自动跳过。
- **谁置位/复位**：`LauncherTest.StartForCreatureSacrificeTest` / `StartForCreatureVatTest` 在 `SetUserData(真实存档)` 之后立即 `isTestSimulation = true`。复位统一在 **`WorldHandler.EnterMainForBaseScene`**（`ClearUserData` 旁）`= false`——它是"回到真实主菜单"的唯一收口点（`LauncherGame.Launch` 启动、游戏内 `UIGameSystem` 返回主菜单、`StartForNormalGame` 都经它），随后读档/新建再 `EnterGameForBaseScene` 进正式游玩即恢复落盘。**测试入口走 `EnterGameForBaseScene` 直接进场、不经 `EnterMainForBaseScene`**，故测试标记不会被误清；正式游戏则总会先过一次主菜单而复位。正式游戏流程永不置 true。
- **各功能不再各自判断**：`UICreatureVat` 的开始/完成存档、`CreatureSacrificeLogic` 的失败落盘与 `SaveAndEndGame` 都**直接调 `SaveUserData()`**（不再写 `if (!isTestMode)`），测试拦截统一在存档层完成。
- **献祭手动成功率**：`CreatureSacrificeLogic.StartSacrifice` 读全局 `isTestSimulation && useManualSuccessRate` 决定是否用手动值掷骰（原 `CreatureSacrificeBean.isTestMode` 已删除，仅保留献祭专属的 `useManualSuccessRate`/`manualSuccessRate`）。
- **好处**：既是"通用测试数据"，又比逐处 `if(!isTestMode)` 更稳——多一条存档路径也不会漏，还顺带堵住了模拟测试期间基地附带存档误写真实档的隐患。

## 献祭升级测试 (CreatureSacrifice)

`TestSceneTypeEnum.CreatureSacrifice` —— 读取某个**真实存档**的数据，对其中一只生物直接发起献祭升级，便于验证成功率公式/升级成长/保底等，且**不会把结果写回真实存档**（依赖上面的[测试模拟不落盘通用机制](#测试模拟不落盘通用机制)）。

### 流程

```
GameTestEditor.DrawCreatureSacrificeTest()                     // Inspector 配置
    │  存档槽位(1~3，与游戏一致：UserData_1/2/3) → 「加载存档生物」→ LoadSacrificeTestCreatures()
    │      用 UserDataService.ChangeSlot(slot).Load(false) 读存档
    │      把背包生物列表(GetUserBackpackCreatureData().listBackpackCreature)填进目标生物下拉(uuid + 显示名)
    │  目标生物下拉 / 手动成功率开关 + 成功率 Slider(0~1)
    │  ▶️ 开始 → launcher.StartForCreatureSacrificeTest(slot, uuid, useManualRate, manualRate)
    ▼
LauncherTest.StartForCreatureSacrificeTest(...)                // Assets/Scripts/Game/Launcher/LauncherTest.cs
    │  ① UserDataService 重新加载该槽位存档为 UserDataBean
    │  ② 按 uuid 在背包生物列表中定位目标生物(必须同一引用)
    │  ③ GameDataHandler.manager.SetUserData(userData) + isTestSimulation=true  // 存档替换为运行时数据并开测试模拟
    │  ④ 注册一次性 World_EnterGameForBaseScene 回调
    │  ⑤ WorldHandler.EnterGameForBaseScene(userData)   // 进入基地(含祭坛)
    ▼
回调触发(基地场景就绪) → 注销自身 → 构建 CreatureSacrificeBean{ targetCreature,
    useManualSuccessRate, manualSuccessRate } → GameHandler.StartCreatureSacrifice(bean)
```

### 关键点

- **使用存档真实数据**：献祭流程内所有数据都走 `GameDataHandler.manager.GetUserData()`，因此把存档 `SetUserData` 进运行时即可让祭品列表、保底、目标生物属性全部来自该存档。
- **目标生物同一引用**：`UICreatureSacrifice.InitCreaturekData` 用 `creatureData != targetCreature` 按引用排除目标，故 `StartForCreatureSacrificeTest` 必须从加载后的 `userData.GetUserBackpackCreatureData().listBackpackCreature` 中按 uuid 取出**同一引用**。
- **手动 vs 真实成功率**：`CreatureSacrificeBean` 保留 `useManualSuccessRate/manualSuccessRate`（`isTestMode` 已删，测试判定改读全局 `isTestSimulation`）；`CreatureSacrificeLogic.StartSacrifice` 在 `isTestSimulation && useManualSuccessRate` 时用手动值掷骰，否则走 `CreatureUtil.GetSacrificeSuccessRate` 公式。
- **不落盘**：由[测试模拟不落盘通用机制](#测试模拟不落盘通用机制)统一保证；`CreatureSacrificeLogic` 的失败落盘与 `SaveAndEndGame` 直接调 `SaveUserData()`（测试模拟下自动 no-op），升级/祭品消耗只在内存生效，退出测试即丢弃。
- **场景依赖**：献祭需要基地场景的祭坛，故必须先 `EnterGameForBaseScene` 再发起；用一次性 `World_EnterGameForBaseScene` 事件等待场景就绪。

## 魔物进阶测试 (CreatureVat)

`TestSceneTypeEnum.CreatureVat` —— 读取某个**真实存档**的数据，覆盖「解锁的升阶容器(VAT)数量」与「魔晶加速等级」后进入基地，直接打开魔物进阶 UI(`UICreatureVat`)，便于验证进阶流程/加速/BUFF 增益等，且**全程内存模拟，不会写回真实存档**（依赖[测试模拟不落盘通用机制](#测试模拟不落盘通用机制)）。

### 流程

```
GameTestEditor.DrawCreatureVatTest()                           // Inspector 配置
    │  存档槽位(1~3) / 解锁VAT数量 IntSlider(1~6) / 解锁加速等级 IntSlider(0~5)  // 均自由选择,拉满=全解锁(默认拉满)
    │  ▶️ 开始 → launcher.StartForCreatureVatTest(slot, vatNum, addProgressLevel)  // 直接传具体值
    ▼
LauncherTest.StartForCreatureVatTest(...)                      // Assets/Scripts/Game/Launcher/LauncherTest.cs
    │  ① UserDataService 加载该槽位存档为 UserDataBean
    │  ② GameDataHandler.manager.SetUserData(userData) + isTestSimulation=true  // 存档替换为运行时数据并开测试模拟
    │  ③ 覆盖解锁(传入值按配置 level_max 钳制):AddUnlock(CreatureVat)
    │             + AddUnlock(CreatureVatAdd, 目标数量-基础creatureVatMax)
    │             + AddUnlock(CreatureVatAddProgress, 加速等级)  // 均按目标值覆盖,含降级/置0
    │  ④ 注册一次性 World_EnterGameForBaseScene 回调
    │  ⑤ WorldHandler.EnterGameForBaseScene(userData)
    ▼
回调触发(基地场景就绪) → 注销自身 → UIHandler.OpenUIAndCloseOther<UICreatureVat>()  // 不落盘由全局标记统一拦截
```

### 关键点

- **VAT数量 = 基础 `creatureVatMax`(默认1) + `CreatureVatAdd` 研究等级**，且受 `CheckIsUnlock(UnlockEnum.CreatureVat)` 门控。故测试须先 `AddUnlock(CreatureVat)`，再把 `CreatureVatAdd` 等级覆盖为 `目标数量 - creatureVatMax`。滑条拉满=全解锁，数量 = `creatureVatMax + CreatureVatAdd.level_max`(当前=1+5=6)。
- **加速等级 = `CreatureVatAddProgress` 研究等级**(0=锁定，隐藏加速按钮；等级=每次加速推进秒数)。滑条拉满=全解锁=该研究 `level_max`(当前=5)。传 0 即测试「加速未解锁」态。
- **自由选择而非勾选**：编辑器两项都是 `EditorGUILayout.IntSlider`（VAT 1~6、加速 0~5，默认取上限=全解锁），直接把**具体数值**传给 `LauncherTest`；`LauncherTest` 再用 `ResearchInfoCfg.GetItemDataByUnlockId(...).level_max` `Mathf.Clamp` 钳制，容错编辑器常量与配置的漂移。
- **不落盘(全程模拟)**：由[测试模拟不落盘通用机制](#测试模拟不落盘通用机制)统一保证——`UICreatureVat` 的开始/完成进阶直接 `SaveUserData()`（测试模拟下自动 no-op），无需自身测试标记；被动 tick 与魔晶加速本就不存档。
- **无需选目标生物**：进阶目标/素材魔物都在 `UICreatureVat` 内选择，故编辑器只需选存档，不像献祭测试那样预加载生物下拉。

## 正常游戏启动 (NormalGame)

`TestSceneTypeEnum.NormalGame` —— 在测试场景(TestScene)里直接走与正式 `LauncherGame` 完全一致的真实开始流程，免去每次手动切到 `GameScene` 再运行。

### 流程

```
GameTestEditor.DrawNormalGameTest()              // Inspector 一个「▶️ 正常启动游戏」按钮
    ▼
LauncherTest.StartForNormalGame()                // Assets/Scripts/Game/Launcher/LauncherTest.cs
    ▼
WorldHandler.EnterMainForBaseScene()             // 与 LauncherGame.Launch() 调用同一入口
    │  清理运行时数据/BUFF/UserData → 加载基地场景 → VolumeHandler 初始化
    ▼
打开主菜单 UIMainStart + 播放主界面音乐
```

### 关键点

- **复用正式入口**：直接调 `WorldHandler.Instance.EnterMainForBaseScene()`，和 [LauncherGame.cs](Assets/Scripts/Game/Launcher/LauncherGame.cs) 内的调用一致，不另写流程，避免测试与正式流程分叉。
- **InitTestData 会被清掉**：`LauncherTest.Launch()` 仍会执行 `InitTestData()` 预填测试数据，但 `EnterMainForBaseScene()` 内部 `GameDataHandler.ClearUserData()` 会清除它——这正是真实开始流程(从存档/新游戏进入)应有的行为。若某模式需保留测试数据，应改走 `EnterGameForBaseScene` 而非此模式。
- **无参数**：该模式无任何 Inspector 配置项，故 `GameTestEditorPartial` 仅新增折叠状态字段 `showNormalGameTest`，无需 EditorPrefs 持久化参数。

## 保存数据到 Excel

测试工具支持将调整后的数据直接写回 Excel 配置表（仅限 Editor 环境）。

```csharp
#if UNITY_EDITOR
List<ExcelChangeData> listData = new List<ExcelChangeData>
{
    new ExcelChangeData(id, "field_name", "value"),
    new ExcelChangeData(id, "field_name2", "value2"),
};
ExcelUtil.SetExcelData("Assets/Data/Excel/excel_xxx[xxx].xlsx", "SheetName", listData);
#endif
```

---

## 测试控制台使用

### 在游戏场景中查看日志

`UITestConsole` 会自动捕获所有 `Debug.Log` / `LogUtil.Log` 输出，按 `` ` `` 键（BackQuote）切换显示。

### 关键配置

| 字段 | 默认值 | 说明 |
|------|--------|------|
| `toggleKey` | `KeyCode.BackQuote` | 切换显示快捷键 |
| `shakeToOpen` | `true` | 是否支持摇一摇打开 |
| `shakeAcceleration` | `3f` | 摇一摇触发加速度阈值 |
| `restrictLogCount` | `false` | 是否限制日志数量 |
| `maxLogs` | `1000` | 最大日志数量 |

---

## 文件位置速查

| 功能 | 文件路径 |
|------|----------|
| 测试启动器 | `Assets/Scripts/Game/Launcher/LauncherTest.cs` |
| 测试编辑器 | `Assets/Editor/GameTestEditor.cs` + `GameTestEditorPartial.cs` |
| 测试战斗逻辑 | `Assets/Scripts/Game/Logic/GameFightLogicTest.cs` |
| 测试战斗数据 | `Assets/Scripts/Bean/Game/FightBeanForTest.cs` |
| 测试控制台 | `Assets/FrameWork/Scripts/Component/UI/UITestConsole.cs` |
| 测试基础 UI | `Assets/Scripts/Component/UI/Test/UITestBase.cs` + `UITestBaseComponent.cs` |
| 卡片测试 UI | `Assets/Scripts/Component/UI/Test/UITestCard.cs` + `UITestCardComponent.cs` |
| NPC 创建测试（预制版） | `Assets/Scripts/Component/UI/Test/UITestNpcCreate.cs` + `UITestNpcCreateComponent.cs`（`LauncherTest.StartNpcCreate` 打开预制 UI） |
| NPC 创建测试（GUI版） | `Assets/Scripts/Component/UI/Test/TestNpcCreateGUI.cs`（纯 IMGUI 代码 UI + 代码生成场景 Spine 预览 + 文本列出卡片数据，不依赖任何预制；`LauncherTest.StartNpcCreateGUI` 挂到空物体 `NpcCreateGUI` 上启动；两版并存，编辑器面板各有一个按钮） |
| 图标显示测试 | `Assets/Scripts/Component/UI/Test/UIViewTestIconShow.cs` + `UIViewTestIconShowComponent.cs` |
| 研究 UI 测试 | `Assets/Scripts/Component/UI/Game/BaseResearch/UIBaseResearchTest.cs` |
| 终焉议会测试入口 | `Assets/Scripts/Game/Launcher/LauncherTest.cs`（`StartForDoomCouncil` 正常随机议员；`StartForDoomCouncilAllFixed` 直接载入所有固定议员，标记 `DoomCouncilBean.isTestAllFixedCouncilor=true`） |
| 终焉议会测试 UI | `Assets/Editor/GameTestEditor.cs`（`DrawDoomCouncilTest`：▶️ 开始终焉议会 / ▶️ 查看所有固定议员 两个按钮 + 加载议案名字） |
| 献祭升级测试入口 | `Assets/Scripts/Game/Launcher/LauncherTest.cs`（`StartForCreatureSacrificeTest`） |
| 献祭升级测试 UI | `Assets/Editor/GameTestEditor.cs`（`DrawCreatureSacrificeTest`/`LoadSacrificeTestCreatures`） |
| 献祭测试数据字段 | `Assets/Scripts/Bean/Game/CreatureSacrificeBean.cs`（`useManualSuccessRate`/`manualSuccessRate`；`isTestMode` 已删，改读全局标记） |
| 测试模拟不落盘总开关 | `Assets/Scripts/Component/Manager/GameDataManager.cs`（`isTestSimulation` + `SaveUserData` 拦截） |
| 测试模拟标记复位点 | `Assets/Scripts/Component/Handler/WorldHandler.cs`（`EnterMainForBaseScene` 内 `isTestSimulation=false`） |
| 魔物进阶测试入口 | `Assets/Scripts/Game/Launcher/LauncherTest.cs`（`StartForCreatureVatTest`） |
| 魔物进阶测试 UI | `Assets/Editor/GameTestEditor.cs`（`DrawCreatureVatTest`） |
| 正常游戏启动入口 | `Assets/Scripts/Game/Launcher/LauncherTest.cs`（`StartForNormalGame`） |
| 正常游戏启动 UI | `Assets/Editor/GameTestEditor.cs`（`DrawNormalGameTest`） |
| 测试场景 | `Assets/Scenes/TestScene.unity` |

---

## 注意事项

1. **Editor 依赖**: `GameTestEditor` 和 Excel 保存功能仅在 `UNITY_EDITOR` 下可用，打包后不会生效。
2. **运行时检查**: 编辑器中的"开始"按钮都检查了 `Application.isPlaying`，必须在运行模式下才能执行。
3. **参数持久化**: 测试参数通过 `EditorPrefs` 保存，跨项目不会共享，重装 Unity 会丢失。
4. **战斗循环**: `GameFightLogicTest` 在结算后会自动重置进攻数据并重新开始，注意避免无限循环导致内存泄漏。
5. **日志性能**: 大量日志会影响性能，生产环境应禁用 `UITestConsole` 或设置 `restrictLogCount = true`。
