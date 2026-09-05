# 测试模块 (Test Module) 分析文档

## 一、模块概述

测试模块为游戏开发提供了一套完整的测试工具和测试场景，支持在 Unity Editor 和运行时对游戏的各个子系统进行快速验证和调试。涵盖战斗场景测试、卡片测试、基地测试、奖励选择测试、终焉议会测试和研究 UI 测试；NPC 创建编辑已迁移为非运行态编辑器工具（见 3.4 节）。

---

## 二、核心组件

### 2.1 LauncherTest（测试启动器）

**文件**: `Scripts/Game/Launcher/LauncherTest.cs`

测试场景的入口启动器，继承 `BaseLauncher`。在启动时会自动初始化大量测试数据，包括生物、道具、魔晶、声望和解锁状态。

**自动初始化的测试数据**:

| 数据 | 说明 |
|------|------|
| 用户自身生物 | 使用 NPC ID `1010010001` 创建 |
| 背包生物 | 50 个随机生物（ID 2002），随机品质/等级（0-10） |
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
    NormalGame = 1,         // 正常游戏启动
    FightSceneTest = 2,     // 战斗场景测试
    CardTest = 3,           // 卡片效果测试
    Base = 4,               // 基地测试
    RewardSelect = 5,       // 奖励选择
    DoomCouncil = 6,        // 终焉议会
    ResearchUI = 8,         // 研究 UI（7 为已删除的 NPC创建，号段留空防存档错位）
    AbyssalBlessing = 9,    // 深渊馈赠UI
    CreatureSacrifice = 10, // 生物献祭升级测试
    CreatureVat = 11,       // 魔物进阶(生物升阶容器)测试
    CreatureJuicer = 12,    // 魔汁机(魔物回收)测试
    EffectTest = 13,        // 粒子特效测试
    ConversationTest = 14,  // 对话系统测试
    StoryTest = 15,         // 故事演出测试
}
```

**战斗场景测试参数**:

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `testDataCardNum` | `int` | 20 | 初始生成的卡片数量 |
| `fightSceneId` | `int` | 1 | 战斗场景 ID |
| `fightCardIds` | `List<long>` | [2002] | 防守方卡片生物 ID 列表（每行手动输入或下拉选择已有生物[带中文名]，数量超列表时循环） |
| `fightSceneRoadNum` | `int` | 1 | 道路数量 |
| `fightSceneRoadLength` | `int` | 10 | 道路长度 |
| `fightSceneAttackNum` | `int` | 2 | 进攻生物数量（波次） |
| `fightSceneAttackDelay` | `float` | 1 | 进攻间隔（秒） |
| `enemyIds` | `List<long>` | [1010010001] | 敌人 NPC ID 列表（每行手动输入或下拉选择已有 NPC[带中文名]） |

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
### 3.4 NpcCreateEditorWindow（NPC 创建编辑器窗口，非运行态）

**文件**: `Editor/NpcCreateEditorWindow.cs` + 5 个 partial（`.List/.Edit/.Appearance/.Preview/.Save`），**菜单**: `游戏/NPC创建编辑`

非运行态的 NPC 创建/修改/删除工具（Play 模式的 UITestNpcCreate/TestNpcCreateGUI 已删除并入本工具），覆盖全字段编辑：

| 功能 | 说明 |
|------|------|
| NPC 列表 | 搜索/npc_type/稀有度筛选/排序（左栏） |
| 新建 NPC | 建议 id（maxId+1）+模板复制+中文名（textId 约定==NPC id，写语言表 NpcInfo sheet 的 content_cn，其他语种 Excel 补录） |
| 删除 NPC | 内存登记、保存时才双表删行（业务表+语言表） |
| 全字段编辑 | id/名字/生物/npc_type/稀有度/等级/议会评级/属性七项/body_size/称号/额外技能(带校验)/地区/头像(带预览)/备注 |
| 外观编辑 | 随机皮肤池/固定皮肤按部位/逐部位调色 RGB(A)+16 色盘/随机装备池+稀有度勾选/固定装备按槽位（候选面板带图集图标） |
| Spine 预览 | 参考模型（生物2001)+目标双模型（可关参考模型），动画列表/播放控制/滚轮缩放/拖拽平移；驱动模式拷贝改造自 SpineWindow 动画预览页签 |
| 保存 | 校验（错误阻断/警告可过）→变更摘要→Excel 占用探测→EPPlus 双表写回→ExcelToJsonItem 重导 JSON→清 Cfg 静态缓存 |

**编辑器安全约束**：编辑目标是 JSON 深拷贝的编辑副本（快照判脏、切换/新建/刷新前三选保护），绝不直改 Cfg 缓存 Bean；编辑器内零运行时单例访问（禁止 `new CreatureBean(npcInfo)`、禁止 `*_language` 属性、禁止 UIHandler 弹窗——详见 `.claude/skills/editor-extension-system` 的 NpcCreateEditorWindow 章节）。

---

### 3.5 UIBaseResearchTest（研究 UI 测试扩展）

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
| NPC 创建编辑器窗口（非运行态） | `Assets/Editor/NpcCreateEditorWindow.cs` + 5 个 partial |
| 测试基础 UI | `Assets/Scripts/Component/UI/Test/UITestBase.cs` |
| 卡片测试 UI | `Assets/Scripts/Component/UI/Test/UITestCard.cs` |
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

### 5.3 NPC 创建编辑（编辑器版，无需运行）

1. 菜单 `游戏/NPC创建编辑` 打开窗口（无需 Play）
2. 左侧列表搜索/筛选/选中 NPC，或底部「＋ 新建NPC」（建议 id + 模板复制 + 中文名）
3. 中栏编辑基础字段/属性/外观，右栏 Spine 双模型实时预览（可切换动画）
4. 改完点工具栏「💾 保存」，确认变更摘要后写回 Excel 并自动重导 JSON
5. 删除：选中后点「－ 删除选中」登记，保存时生效（列表再点一次置灰项可撤销登记）
