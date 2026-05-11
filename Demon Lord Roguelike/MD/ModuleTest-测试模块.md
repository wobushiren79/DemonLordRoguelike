# 测试模块 (Test Module) 分析文档

## 一、模块概述

测试模块为游戏开发提供了一套完整的测试工具和测试场景，支持在 Unity Editor 和运行时对游戏的各个子系统进行快速验证和调试。涵盖战斗场景测试、卡片测试、基地测试、奖励选择测试、终焉议会测试、NPC 创建测试和研究 UI 测试。

---

## 二、核心组件

### 2.1 LauncherTest（测试启动器）

**文件**: `Scripts/Game/Launcher/LauncherTest.cs`

测试场景的入口启动器，继承 `BaseLauncher`。在启动时会自动初始化大量测试数据，包括生物、道具、魔晶、声望和解锁状态。

**自动初始化的测试数据**:

| 数据 | 说明 |
|------|------|
| 用户自身生物 | 使用 NPC ID `1010010001` 创建 |
| 背包生物 | 50 个随机生物（ID 2002），随机品质/星级/等级 |
| 阵容生物 | 自动加入阵容 1 |
| 魔晶 | 99999 |
| 声望 | 1000 |
| 道具 | 基础道具 10100001~10100004 |
| 解锁数据 | 全部解锁（含研究满级） |

**测试入口方法**:

| 方法 | 说明 |
|------|------|
| `StartForFightSceneTest(FightBean)` | 进入战斗场景测试 |
| `StartForCardTest(FightCreatureBean)` | 打开卡片测试 UI |
| `StartForBaseTest(CreatureBean)` | 进入基地场景测试 |
| `StartForRewardSelect(RewardSelectTestData)` | 打开奖励选择测试 |
| `StartForDoomCouncil(long billId)` | 进入终焉议会测试 |
| `StartNpcCreate()` | 打开 NPC 创建测试 UI |
| `StartForResearchUI()` | 打开研究 UI 测试 |

---

### 2.2 GameTestEditor（测试编辑器扩展）

**文件**: `Editor/GameTestEditor.cs` + `Editor/GameTestEditorPartial.cs`

自定义 Inspector 编辑器，为 `LauncherTest` 提供可视化的测试参数配置面板。

**测试场景类型** (`TestSceneTypeEnum`):

```csharp
public enum TestSceneTypeEnum
{
    None = 0,
    FightSceneTest = 1,     // 战斗场景测试
    CardTest = 2,           // 卡片效果测试
    Base = 3,               // 基地测试
    RewardSelect = 4,       // 奖励选择
    DoomCouncil = 5,        // 终焉议会
    NpcCreate = 6,          // NPC 创建
    ResearchUI = 7,         // 研究 UI
}
```

**战斗场景测试参数**:

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `testDataCardNum` | `int` | 20 | 初始生成的卡片数量 |
| `fightSceneId` | `int` | 1 | 战斗场景 ID |
| `fightCardId` | `string` | "2002" | 防守方卡片生物 ID，多个用逗号分隔 |
| `fightSceneRoadNum` | `int` | 1 | 道路数量 |
| `fightSceneRoadLength` | `int` | 10 | 道路长度 |
| `fightSceneAttackNum` | `int` | 2 | 进攻生物数量（波次） |
| `fightSceneAttackDelay` | `float` | 1 | 进攻间隔（秒） |
| `enemyIds` | `List<long>` | [1010010001] | 敌人 NPC ID 列表 |

**战斗 BUFF 测试参数**:

| 参数 | 类型 | 说明 |
|------|------|------|
| `attackModeAttackTestId` | `int` | 进攻方攻击模块测试 ID |
| `attackModeDefenseTestId` | `int` | 防守方攻击模块测试 ID |
| `buffSelfAttackTestId` | `string` | 进攻方携带的 BUFF ID |
| `buffSelfDefenseTestId` | `string` | 防守方携带的 BUFF ID |
| `buffTestId` | `string` | 全局攻击时触发的 BUFF ID |
| `abyssalBlessingIds` | `string` | 深渊馈赠 ID 列表，逗号分隔 |

**奖励选择测试参数**:

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `rewardSelectRarity` | `RarityEnum` | N | 装备品质 |
| `rewardSelectAddAttribute` | `int` | 5 | 属性加成 |
| `rewardSelectCrystalNum` | `int` | 100 | 魔晶数量 |
| `rewardSelectCreateEquipNum` | `int` | 1 | 装备生成数量 |
| `rewardSelectCreateItemNum` | `int` | 3 | 道具生成总数 |
| `rewardSelectNumMax` | `int` | 1 | 最大选择次数 |
| `rewardSelectEquipDemonLordRate` | `float` | 0.1 | 魔王专属装备概率 |

**参数持久化**: 使用 `EditorPrefs` 保存所有测试参数，重启 Editor 后自动恢复。

---

### 2.3 GameFightLogicTest（测试战斗逻辑）

**文件**: `Scripts/Game/Logic/GameFightLogicTest.cs`

继承 `GameFightLogic`，用于测试战斗场景。在战斗结算时会自动重置进攻数据并重新开始战斗，方便循环测试。

**关键行为**:
- 结算时打开 `UIFightSettlement`
- 关闭结算界面后自动重置 `fightAttackData` 并重新进入战斗

---

### 2.4 FightBeanForTest（测试战斗数据）

**文件**: `Scripts/Bean/Game/FightBeanForTest.cs`

继承 `FightBean`，额外保存一份进攻数据的备份 `fightAttackDataRemark`，用于测试场景下循环重置战斗。

---

## 三、测试 UI 系统

### 3.1 UITestConsole（游戏内测试控制台）

**文件**: `FrameWork/Scripts/Component/UI/UITestConsole.cs`

基于 IMGUI 的游戏内调试控制台，捕获并显示 Unity 的日志输出。

| 功能 | 说明 |
|------|------|
| 快捷键 | `` ` ``（BackQuote）切换显示 |
| 摇一摇 | 移动端支持摇一摇打开（加速度 > 3） |
| 日志折叠 | 支持折叠重复消息 |
| 日志清理 | Clear 按钮清空日志 |
| 颜色区分 | Error=红色, Warning=黄色, Log=白色 |

---

### 3.2 UITestBase（测试基础 UI）

**文件**: `Scripts/Component/UI/Test/UITestBase.cs`

GM 工具面板，提供快速添加游戏资源的功能。

**功能按钮**:

| 按钮 | 功能 | 输入为空时的行为 |
|------|------|-----------------|
| 退出 | 返回 `UIBaseMain` | - |
| 添加魔晶 | 增加魔晶 | +999999 |
| 添加声望 | 增加声望 | +999999 |
| 添加道具 | 添加道具到背包 | 添加所有道具 |
| 添加所有生物 | 添加所有生物到背包 | - |
| 添加测试生物 | 添加指定 ID 的生物 | 提示输入生物 ID |
| 添加解锁 | 添加解锁数据 | 解锁所有 |

---

### 3.3 UITestCard（卡片测试 UI）

**文件**: `Scripts/Component/UI/Test/UITestCard.cs`

用于测试和校准生物卡片的 UI 显示参数，支持实时调整卡片图标大小和位置，并将结果保存到 Excel 配置表。

**可调参数**:

| 参数 | 说明 |
|------|------|
| 小卡尺寸/位置 | `ui_CreatureCardItem` 的图标缩放和锚点位置 |
| 大卡尺寸/位置 | `ui_ViewCreatureCardDetails` 的图标缩放和锚点位置 |
| 实体大小 | Spine 模型的缩放比例 |

**保存数据**: 点击"生成数据"按钮，自动将参数写入 `excel_creature_model[生物模型信息].xlsx`

---

### 3.4 UITestNpcCreate（NPC 创建测试 UI）

**文件**: `Scripts/Component/UI/Test/UITestNpcCreate.cs`

用于测试和配置 NPC 的外观、属性和装备，支持实时预览和保存到 Excel。

**功能**:

| 功能 | 说明 |
|------|------|
| 加载 NPC | 通过 NPC ID 加载基础数据 |
| 皮肤切换 | 为每个身体部位选择不同皮肤 |
| 装备切换 | 为每个装备槽选择不同装备 |
| 头发颜色 | 调整头发染色 |
| 属性编辑 | 实时修改 HP/DR/MP/ATK/ASPD/MSPD |
| 装备显隐 | 切换是否显示装备 |
| 保存数据 | 将外观、装备、属性保存到 `excel_npc_info[NPC信息].xlsx` |

**属性编辑字段**:

| 字段 | 说明 |
|------|------|
| HP | 生命值 |
| DR | 防御 |
| MP | 魔法值 |
| ATK | 攻击力 |
| ASPD | 攻击速度 |
| MSPD | 移动速度 |

---

### 3.5 UIViewTestIconShow（图标显示测试）

**文件**: `Scripts/Component/UI/Test/UIViewTestIconShow.cs`

测试 UI 中用于展示可选皮肤/装备图标的子组件。

---

### 3.6 UIBaseResearchTest（研究 UI 测试扩展）

**文件**: `Scripts/Component/UI/Game/BaseResearch/UIBaseResearchTest.cs`

`UIBaseResearch` 的 partial 扩展，添加测试模式支持。

| 功能 | 说明 |
|------|------|
| `SetDataForTest()` | 开启测试模式，显示保存按钮 |
| `SaveResearchDataForTest()` | 将研究节点的坐标保存到 `excel_research_info[研究信息].xlsx` |

---

## 四、文件位置速查

| 功能 | 文件路径 |
|------|----------|
| 测试启动器 | `Assets/Scripts/Game/Launcher/LauncherTest.cs` |
| 测试编辑器 | `Assets/Editor/GameTestEditor.cs` |
| 测试编辑器参数 | `Assets/Editor/GameTestEditorPartial.cs` |
| 测试战斗逻辑 | `Assets/Scripts/Game/Logic/GameFightLogicTest.cs` |
| 测试战斗数据 | `Assets/Scripts/Bean/Game/FightBeanForTest.cs` |
| 测试控制台 | `Assets/FrameWork/Scripts/Component/UI/UITestConsole.cs` |
| 测试基础 UI | `Assets/Scripts/Component/UI/Test/UITestBase.cs` |
| 卡片测试 UI | `Assets/Scripts/Component/UI/Test/UITestCard.cs` |
| NPC 创建测试 | `Assets/Scripts/Component/UI/Test/UITestNpcCreate.cs` |
| 图标显示测试 | `Assets/Scripts/Component/UI/Test/UIViewTestIconShow.cs` |
| 研究 UI 测试 | `Assets/Scripts/Component/UI/Game/BaseResearch/UIBaseResearchTest.cs` |
| 临时测试 | `Assets/Scripts/TestTemp.cs` |
| 测试场景 | `Assets/Scenes/TestScene.unity` |

---

## 五、使用流程

### 5.1 战斗场景测试

1. 打开 `TestScene`
2. 选中挂载 `LauncherTest` 的 GameObject
3. 在 Inspector 选择 `FightSceneTest`
4. 配置卡片数量、场景 ID、敌人 ID、BUFF 等参数
5. 点击 Play 运行
6. 点击"开始战斗测试"

### 5.2 卡片测试

1. 选择 `CardTest`
2. 输入生物 ID 或 NPC ID
3. 运行后点击"显示卡片"
4. 实时调整小卡/大卡的尺寸和位置
5. 点击"生成数据"保存到 Excel

### 5.3 NPC 创建测试

1. 选择 `NpcCreate`
2. 运行后进入 NPC 创建界面
3. 输入 NPC ID 加载数据
4. 切换皮肤、装备、调整属性
5. 点击保存写入 Excel
