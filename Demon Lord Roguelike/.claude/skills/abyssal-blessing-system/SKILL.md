---
name: abyssal-blessing-system
description: Demon Lord Roguelike 游戏的深渊馈赠(AbyssalBlessing)系统开发指南。使用此SKILL当需要新增/修改深渊馈赠配置表、深渊馈赠 BUFF 加成、深渊馈赠等级链(parent_id/level 同族升级替换)、深渊馈赠 UI（选择界面/详情气泡/列表展示）、关卡间馈赠流程、深渊馈赠数据管理等，包括 AbyssalBlessingInfoBean、AbyssalBlessingEntityBean、UIFightAbyssalBlessing、UIViewAbyssalBlessingInfoContent、UIPopupAbyssalBlessingInfo、BuffHandler.AddAbyssalBlessing、AbyssalBlessingInfoCfg.GetFamilyRootId 族根回溯、GetAbyssalBlessingOwnedLevel 当前等级、Buff_AbyssalBlessingChange 事件等。
watched_files:
  - Assets/Scripts/Bean/Game/AbyssalBlessingEntityBean.cs
  - Assets/Scripts/Bean/MVC/Game/AbyssalBlessingInfoBean.cs
  - Assets/Scripts/Bean/MVC/Game/AbyssalBlessingInfoBeanPartial.cs
  - Assets/Scripts/Component/UI/Game/FightAbyssalBlessing/
  - Assets/Scripts/Component/UI/Common/AbyssalBlessing/
  - Assets/Scripts/Component/UI/Popup/UIPopupAbyssalBlessingInfo.cs
  - Assets/Scripts/Component/UI/Popup/UIPopupAbyssalBlessingInfoComponent.cs
  - Assets/Scripts/Component/Handler/BuffHandler.cs
  - Assets/Scripts/Component/Manager/BuffManager.cs
  - Assets/Data/Excel/excel_abyssal_blessing_info[深渊馈赠信息].xlsx
  - Assets/Resources/JsonText/AbyssalBlessingInfo.txt
  - Assets/Resources/JsonText/Language_AbyssalBlessingInfo_cn.txt
  - Assets/Resources/JsonText/Language_AbyssalBlessingInfo_en.txt
---

# 深渊馈赠系统开发指南

## 核心概念

深渊馈赠（Abyssal Blessing）是**征服模式**关卡间获得的全局增益系统：每通关一关后，玩家从随机生成的 3 个馈赠中选择 1 个（也可跳过），馈赠通过添加 BUFF 到防守核心（FightDefenseCore）作用于整局战斗，并在最终领奖后清空。

### 数据三件套

```
AbyssalBlessingInfoBean     - 馈赠配置数据（id / icon_res / parent_id / level / buff_ids / name / details / remark）
AbyssalBlessingEntityBean   - 馈赠运行时实例（abyssalBlessingUUID + AbyssalBlessingInfoBean，构造函数自动生成 UUID）
BuffInfoBean                - 馈赠绑定的 BUFF 配置（通过 buff_ids 关联，每个等级各自引用自己那级的 BUFF）
```

### 等级链是"配置表自身"的责任（核心，务必先读）

> ⚠️ 这是最容易踩错的点。深渊馈赠的"升级"**不是由 BUFF 的 buff_parent_id/buff_level 决定的**，
> 而是由**馈赠配置表自身的 `parent_id` + `level` 两列**以"链表"形式定义的。每个等级是一条**独立的配置行**，
> `buff_ids` 只是指向"这一级对应的 BUFF（决定数值大小）"。

链表式定义（"钱多多"族，取自 `AbyssalBlessingInfo.txt` 真实数据）：

| id | parent_id | level | buff_ids | remark |
|----|-----------|-------|----------|--------|
| 2000001001 | 0 | 1 | 3000200001 | 钱多多-每次击杀 10% 概率多掉一次魔晶（1级，族根） |
| 2000001002 | 2000001001 | 2 | 3000200002 | 钱多多-20%（2级） |
| 2000001003 | 2000001002 | 3 | 3000200003 | 钱多多-30%（3级） |
| 2000001004 | 2000001003 | 4 | 3000200004 | 钱多多-40%（4级） |
| 2000001005 | 2000001004 | 5 | 3000200005 | 钱多多-50%（5级） |

- `parent_id == 0` 的那一行是**族根**（family root）。族根 `level` 可为 `1`（可升级族的 1 级），也可为 `0`（如"增殖" `1000001001`，`level==0` 表示**不参与升级链、可重复出现的常驻馈赠**）。
- `level == 2` 的 `parent_id` 指向 lv1 的 **id**；`level == 3` 的 `parent_id` 指向 lv2 的 **id**……**每行指向其上一级的 id**（不是都指向根）。
- `AbyssalBlessingInfoCfg.GetFamilyRootId(id)` 沿 `parent_id` 回溯到 `parent_id==0` 的根（防循环最多 64 层，结果缓存）。
- `AbyssalBlessingInfoBean.IsLevelUp()` = `level > 0`（`level==0` 即非升级类）。

> **id 命名约定**（看真实数据）：当前为 **10 位**，形如 `2000001005`，末 3 位是等级序号（`001`=1级…`005`=5级），同族各级仅末 3 位递增；不同族中间段不同。新增时沿用此规律，保证同族各级 id 连续、可读。

### 关键流程

```
[征服模式战斗结算]
     │ 非最后一关
     ▼
打开 UIFightAbyssalBlessing → SetData()
     │  RollCandidates(SHOW_NUM=3)：
     │  1. 全部配置按 GetFamilyRootId 分组成"族"
     │  2. 每族取"玩家当前拥有等级 + 1"那一行（未拥有则取族根）
     │  3. 已满级的族被排除；洗牌后取前 3 个
     ▼
玩家选择 / 跳过
     ├── 选择 → actionForSelect(info)
     │           ↓ (GameFightLogicConquer)
     │       FightBeanForConquer.AddAbyssalBlessing(info)
     │           ↓
     │       new AbyssalBlessingEntityBean(info) → BuffHandler.AddAbyssalBlessing(entity)
     │           ↓ GetFamilyRootId → RemoveAbyssalBlessingByRootId(同族旧级先移除)
     │           ↓ 解析 buff_ids(逗号分隔) 加到防守核心
     │       事件 Buff_AbyssalBlessingChange → UIViewAbyssalBlessingInfoContent 刷新
     │
     └── 跳过 → actionForSkip → 直接 StartNextGame
     │
     ▼ （所有关卡通关后，领奖结束）
BuffHandler.Instance.manager.ClearAbyssalBlessing()  // 清空所有馈赠
```

## 关键架构（必读）

### 1. 与 BUFF 系统的关系

深渊馈赠**本身不实现 BUFF 逻辑**，所有效果通过 `buff_ids` 字段引用 BuffInfo 配置实现。具体 BUFF 实体类型 / 触发逻辑 / 属性管线 / 堆叠策略请参考 `buff-system` SKILL。

馈赠 BUFF 在 BuffManager 中走**独立容器**，不与战斗生物 BUFF 混用：

| 容器 | 类型 | 用途 |
|------|------|------|
| `manager.dicFightCreatureBuffsActivie` | DictionaryList<string, List<BuffBaseEntity>> | 战斗生物 BUFF（key=creatureUUID） |
| `manager.dicAbyssalBlessingBuffsActivie` | DictionaryList<AbyssalBlessingEntityBean, List<BuffBaseEntity>> | 深渊馈赠 BUFF（key=馈赠实例） |

馈赠 BUFF 作用目标统一为**防守核心**（CreatureFightTypeEnum.FightDefenseCore），施加者与目标 UUID 都填核心 UUID（`BuffHandler.GetDefenseCoreUUID()`）。

### 2. 等级链替换机制（重点，按当前实现）

`BuffHandler.AddAbyssalBlessing` 的真实逻辑（简化）：

```csharp
public void AddAbyssalBlessing(AbyssalBlessingEntityBean abyssalBlessingEntity)
{
    if (abyssalBlessingEntity?.abyssalBlessingInfo == null) return;
    AbyssalBlessingInfoBean info = abyssalBlessingEntity.abyssalBlessingInfo;
    long defenseCoreUUID = GetDefenseCoreUUID();

    // 等级链替换：同族（同 root）的旧馈赠先整条移除
    long rootId = AbyssalBlessingInfoCfg.GetFamilyRootId(info.id);
    RemoveAbyssalBlessingByRootId(rootId);

    // 记录实例 + 逐个解析 buff_ids（逗号分隔），全部加到防守核心
    manager.AddAbyssalBlessingEntity(abyssalBlessingEntity);
    foreach (string buffIdStr in info.buff_ids.Split(','))
    {
        if (long.TryParse(buffIdStr.Trim(), out long buffId))
        {
            var buffEntity = manager.GetBuffEntity(new BuffBean(buffId), defenseCoreUUID, defenseCoreUUID);
            manager.AddAbyssalBlessingBuff(abyssalBlessingEntity, buffEntity);
        }
    }

    EventHandler.Instance.TriggerEvent(EventsInfo.Buff_AbyssalBlessingChange, abyssalBlessingEntity);
}
```

**关键点**：
- 升级 = "先移除同族旧级，再加新级的 buff_ids"。同族识别靠 `GetFamilyRootId`，**不依赖 BUFF 的任何字段**。
- 选择界面（`UIFightAbyssalBlessing.RollCandidates`）通过 `BuffHandler.Instance.GetAbyssalBlessingOwnedLevel(rootId)` 得到玩家当前等级，只展示 `level == owned + 1` 的那一行 —— 玩家看到的就是"将要获得"的等级。
- `buff_ids` 是**纯逗号分隔字符串**，逐个直接当 BUFF id 用，没有任何"按 parent/level 解析下一级 BUFF"的逻辑（那是旧设计，已废弃）。

### 3. 事件通信

```
EventsInfo.Buff_AbyssalBlessingChange   // BUFF系统-深渊馈赠变化（参数 AbyssalBlessingEntityBean）
```

- **触发处**：`BuffHandler.AddAbyssalBlessing` 末尾。
- **监听处**：`UIViewAbyssalBlessingInfoContent` 注册 → 在面板可见时刷新列表。

### 4. 数据生命周期

| 时机 | 操作 |
|------|------|
| 关卡间选择馈赠 | `FightBeanForConquer.AddAbyssalBlessing()` → `BuffHandler.AddAbyssalBlessing()` |
| 切换关卡（进入战斗） | 馈赠 BUFF 留存（防守核心一直存在） |
| 通关全部关卡（领奖结束） | `BuffHandler.Instance.manager.ClearAbyssalBlessing()` 清空所有馈赠 |
| 战斗过程 | `BuffHandler.UpdateData` 同时驱动馈赠 BUFF 的 `UpdateBuffTime` |

⚠️ 馈赠 BUFF 不会随单关结束自动销毁，**ClearAbyssalBlessing 只能在最终领奖完成后调用**。

### 5. UI 组件层级

```
UIFightAbyssalBlessing                          # 关卡间选择界面（征服模式触发，3 选 1）
├── 候选列表                                     # RollCandidates 抽出的最多 3 个
│   └── UIViewFightAbyssalBlessingItem          # 单个馈赠候选项
└── 跳过按钮

UIViewAbyssalBlessingInfoContent                # 战斗界面常驻列表（已选馈赠展示）
└── UIViewAbyssalBlessingInfoContentItem        # 单项（含 popup 触发）
    └── PopupButtonCommonView                    # 点击 → PopupEnum.AbyssalBlessingInfo

UIPopupAbyssalBlessingInfo : PopupShowCommonView # 馈赠详情气泡
```

---

## 新增深渊馈赠配置（重点流程）

> Excel 是配置数据的**唯一真实源**。任何新增/修改/删除都必须落到 Excel，再用 Unity 编辑器导出 JSON；
> 只改 JSON 会在下次导出被覆盖丢失。

### 第 0 步：理解配置表结构

文件：`Assets/Data/Excel/excel_abyssal_blessing_info[深渊馈赠信息].xlsx`
工作表：**`AbyssalBlessingInfo`**（整本只有这一张数据表，不存在 `Sheet1`/`Sort Title`）。

`AbyssalBlessingInfo` 是项目通用的"三行表头"格式：

| 行号 | 含义 | 内容 |
|------|------|------|
| 第 1 行 | 字段名（导出 JSON 的 key） | `id` `icon_res` `parent_id` `level` `buff_ids` `name[language]` `details[language_1]` `remark` |
| 第 2 行 | 字段类型（导出工具识别） | `long` `string` `long` `int` `string` `long` `long` `string` |
| 第 3 行 | 中文说明（仅给策划看） | `序号` `图标名字` `父级深渊馈赠ID` `等级` `buff_ids` `名字-中文` `描述中文` `备注` |
| 第 4 行起 | 数据行 | 一行一个馈赠（一个等级 = 一行） |

各列说明：

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `id` | long | ✓ | 馈赠唯一 ID（当前真实数据为 **10 位**，如 `2000001005`，末 3 位=等级序号）。**同族不同等级用不同 id**。 |
| `icon_res` | string |  | 图标 Sprite 名（不带后缀），须存在于 `AtlasForAbyssalBlessing.spriteatlas`（该图集打包整个 `Assets/LoadResources/Textures/AbyssalBlessing/` 文件夹）。同族各级共用一张图标，命名 `abyssal_*`，当前所有族系均已配图。 |
| `parent_id` | long | ✓ | 父级馈赠 id。**族根填 `0`；2 级填 1 级的 id；3 级填 2 级的 id**……链表式逐级指向上一级。 |
| `level` | int | ✓ | 等级。升级族从 `1` 起连续递增（1,2,3…）；`0` 表示不参与升级链的常驻/可重复馈赠。 |
| `buff_ids` | string | ✓ | 这一级对应的 BUFF id 列表，多个用英文逗号 `,` 分隔。数值大小由 BUFF 决定，**不同等级指向不同 BUFF**。 |
| `name` | language | ✓ | 名字多语言文本 id（通常填**与该行 id 相同的值**）。 |
| `details` | language | ✓ | 描述多语言文本 id（通常也填与该行 id 相同的值；Bean 读取时取该 id 的第 2 列文本）。 |
| `remark` | string |  | 备注（仅文档用途，不影响逻辑）。建议写清"效果+等级"，如 `钱多多-20%（2级）`。 |

### 第 1 步：写入 Excel（用 excel-io / openpyxl）

**单条常驻馈赠**（不参与升级、可重复出现，如"增殖"）：1 行，`parent_id=0`、`level=0`。

**多等级馈赠**（推荐写法，N 级 = N 行；下例新开一个"暴击"族，沿用 10 位 id 规律）：

```
id=2000004001, parent_id=0,          level=1, buff_ids=3000500001, name=2000004001, details=2000004001, remark=暴击率+5%（1级）
id=2000004002, parent_id=2000004001, level=2, buff_ids=3000500002, name=2000004002, details=2000004002, remark=暴击率+10%（2级）
id=2000004003, parent_id=2000004002, level=3, buff_ids=3000500003, name=2000004003, details=2000004003, remark=暴击率+15%（3级）
```

修改脚本参考（遵循项目 Excel 规则：仅用 openpyxl、UTF-8、保存前先备份、临时脚本用完即删；本环境 Python 命令为 `python3`）：

```python
import openpyxl
path = r"Assets/Data/Excel/excel_abyssal_blessing_info[深渊馈赠信息].xlsx"
wb = openpyxl.load_workbook(path)
ws = wb["AbyssalBlessingInfo"]          # 注意：工作表名为 AbyssalBlessingInfo，不是 Sheet1
# 列顺序：id, icon_res, parent_id, level, buff_ids, name, details, remark
# ⚠️ 新增行必须按 id 升序插入到正确位置（参考 .claude/scripts/excel_add_row.py 的排序插入逻辑），
#    仅当新 id 比所有现有 id 都大时才允许落到表尾（如下例）。
ws.append([2000004001, "", 0,          1, "3000500001", 2000004001, 2000004001, "暴击率+5%（1级）"])
ws.append([2000004002, "", 2000004001, 2, "3000500002", 2000004002, 2000004002, "暴击率+10%（2级）"])
ws.append([2000004003, "", 2000004002, 3, "3000500003", 2000004003, 2000004003, "暴击率+15%（3级）"])
wb.save(path)
```

### 第 2 步：配套 BUFF（每个等级的实际效果）

`buff_ids` 引用的 BUFF 必须在 `excel_buff_info` 中存在。**不同等级要配不同 BUFF（不同数值）**。
BUFF 的属性/条件/周期/堆叠等细节参考 `buff-system` SKILL。
> 注意：BUFF 自己的 `buff_parent_id` / `buff_level` 与馈赠升级链**无关**，深渊馈赠的升级只看馈赠表的 `parent_id`/`level`。

### 第 3 步：多语言文本

在 `excel_language[...]` 对应表中为 `name`/`details` 的文本 id 补中英文，导出生成
`Language_AbyssalBlessingInfo_cn.txt` / `_en.txt`。参考 `localization-system` SKILL。

### 第 4 步：图标资源

- 图标必须放入 `Assets/LoadResources/Textures/SpriteAtlas/AtlasForAbyssalBlessing.spriteatlas` 的 Packables。
- `icon_res` 填该图集中 Sprite 的名字（不带后缀）。
- 加载统一走 `IconHandler.Instance.SetAbyssalBlessingIcon(iconName, image)`（内部 `SpriteAtlasTypeEnum.AbyssalBlessing` → `AtlasForAbyssalBlessing`，找不到时 fallback `icon_unknow`）。**禁止用 `SetUIIcon`**。

### 第 5 步：导出 JSON（必须，在 Unity 中操作）

在 Unity 编辑器运行 `ExcelEditorWindow` 导出工具，重新生成
`Assets/Resources/JsonText/AbyssalBlessingInfo.txt`，否则配置不会生效。
> 若本次只改了 Excel 未能导出 JSON，必须在任务总结中提示用户："JSON 未同步，需在 Unity 编辑器运行配置导出工具重新生成"。

### 第 6 步：测试

用 `LauncherTest` 选 `TestSceneTypeEnum.AbyssalBlessing` 直接展示指定 id（见下文测试模式），验证：图标、名字、描述、同族升级替换是否正确。

---

## 使用深渊馈赠系统

### 触发选择界面（已由 GameFightLogicConquer 处理）

```csharp
var uiFightAbyssalBlessing = UIHandler.Instance.OpenUIAndCloseOther<UIFightAbyssalBlessing>();
uiFightAbyssalBlessing.SetData(
    actionForSelect: info => { /* 选择回调 */ },
    actionForSkip:   () => { /* 跳过回调 */ });
// SetData 内部调用 RollCandidates(SHOW_NUM=3)：按族取"当前等级+1"，洗牌取前 3
```

### 测试模式（指定 ID 直接展示，不随机）

```csharp
var ui = UIHandler.Instance.OpenUIAndCloseOther<UIFightAbyssalBlessing>();
ui.SetDataForTest(new long[] { 2000001001, 2000002001 },
    info => LogUtil.Log($"选 {info.id}"),
    () => LogUtil.Log("跳过"));
// 无效 id 自动跳过；与 SetData 共用渲染管线
```

- **Editor 入口**：`LauncherTest` 选 `TestSceneTypeEnum.AbyssalBlessing` → 填入 id 列表 → "打开深渊馈赠 UI"。

### 程序化添加馈赠（测试 / 特殊流程）

```csharp
AbyssalBlessingInfoBean info = AbyssalBlessingInfoCfg.GetItemData(2000001002);
AbyssalBlessingEntityBean entity = new AbyssalBlessingEntityBean(info);  // 构造函数自动生成 abyssalBlessingUUID
BuffHandler.Instance.AddAbyssalBlessing(entity);
// 内部会：① GetFamilyRootId → 移除同族旧级 ② 解析 buff_ids 加到防守核心 ③ 触发 Buff_AbyssalBlessingChange
```

### 查询已有馈赠

```csharp
// 全部馈赠实例
var all = BuffHandler.Instance.manager.dicAbyssalBlessingBuffsActivie;
foreach (var entity in all.ListKey) { /* entity.abyssalBlessingInfo ... */ }

// 查询某"族"当前拥有等级（0=未拥有）。注意传的是 root id
long rootId = AbyssalBlessingInfoCfg.GetFamilyRootId(someId);
int ownedLevel = BuffHandler.Instance.GetAbyssalBlessingOwnedLevel(rootId);
```

### 清空馈赠

```csharp
// 只在征服全通关 + 领奖结束后调用
BuffHandler.Instance.manager.ClearAbyssalBlessing();
```

### 监听馈赠变化

```csharp
this.RegisterEvent<AbyssalBlessingEntityBean>(
    EventsInfo.Buff_AbyssalBlessingChange, EventForAbyssalBlessingChange);

public void EventForAbyssalBlessingChange(AbyssalBlessingEntityBean entity) { /* 刷新 UI */ }
```

## UI 开发要点

### 候选项（UIViewFightAbyssalBlessingItem）

每个等级是独立配置，候选项直接展示该候选的 `name_language` / `details_language` 与 `icon_res` 即可，**无需再解析"下一级 BUFF"**（RollCandidates 已经把"当前等级+1"那一行选出来了）：

```csharp
public void SetData(AbyssalBlessingInfoBean info)
{
    this.abyssalBlessingInfo = info;
    SetName(info.name_language);
    SetDetails(info.details_language);
    IconHandler.Instance.SetAbyssalBlessingIcon(info.icon_res, ui_Icon);
}
```

### 战斗内常驻列表（UIViewAbyssalBlessingInfoContent）

拉取 `manager.dicAbyssalBlessingBuffsActivie.ListKey`，按 key 复用/实例化 `UIViewAbyssalBlessingInfoContentItem`，并注册 `Buff_AbyssalBlessingChange` 按需刷新。

### 详情气泡（UIPopupAbyssalBlessingInfo）

继承 `PopupShowCommonView`，`SetData` 接收 `AbyssalBlessingEntityBean`，从 `abyssalBlessingInfo` 显示图标/名字/详情。由 `PopupButtonCommonView.SetData(entity, PopupEnum.AbyssalBlessingInfo)` 触发。

### 图标加载统一入口

```csharp
IconHandler.Instance.SetAbyssalBlessingIcon(info.icon_res, ui_Icon);
```

## 关键文件速查

| 功能 | 文件路径 |
|------|----------|
| 馈赠配置 Bean（自动生成，禁改） | `Assets/Scripts/Bean/MVC/Game/AbyssalBlessingInfoBean.cs` |
| 馈赠配置扩展（IsLevelUp / GetFamilyRootId） | `Assets/Scripts/Bean/MVC/Game/AbyssalBlessingInfoBeanPartial.cs` |
| 馈赠运行时实例 | `Assets/Scripts/Bean/Game/AbyssalBlessingEntityBean.cs` |
| Excel 源表（数据在 AbyssalBlessingInfo 工作表） | `Assets/Data/Excel/excel_abyssal_blessing_info[深渊馈赠信息].xlsx` |
| 导出 JSON | `Assets/Resources/JsonText/AbyssalBlessingInfo.txt` |
| 多语言 | `Assets/Resources/JsonText/Language_AbyssalBlessingInfo_{cn,en}.txt` |
| BUFF 添加入口 | `Assets/Scripts/Component/Handler/BuffHandler.cs`（深渊馈赠 BUFF region：AddAbyssalBlessing / RemoveAbyssalBlessingByRootId / GetAbyssalBlessingOwnedLevel / GetDefenseCoreUUID） |
| BUFF 容器 | `Assets/Scripts/Component/Manager/BuffManager.cs`（dicAbyssalBlessingBuffsActivie / AddAbyssalBlessingEntity / AddAbyssalBlessingBuff / ClearAbyssalBlessing） |
| 征服模式流程 | `Assets/Scripts/Game/Logic/GameFightLogicConquer.cs` |
| 数据持有 | `Assets/Scripts/Bean/Game/FightBeanForConquer.cs`（AddAbyssalBlessing） |
| 选择界面 | `Assets/Scripts/Component/UI/Game/FightAbyssalBlessing/UIFightAbyssalBlessing.cs`（SetData / RollCandidates） |
| 选择项 | `Assets/Scripts/Component/UI/Game/FightAbyssalBlessing/UIViewFightAbyssalBlessingItem.cs` |
| 常驻列表 | `Assets/Scripts/Component/UI/Common/AbyssalBlessing/UIViewAbyssalBlessingInfoContent.cs` |
| 常驻项 | `Assets/Scripts/Component/UI/Common/AbyssalBlessing/UIViewAbyssalBlessingInfoContentItem.cs` |
| 详情气泡 | `Assets/Scripts/Component/UI/Popup/UIPopupAbyssalBlessingInfo.cs` |
| 图集 | `Assets/LoadResources/Textures/SpriteAtlas/AtlasForAbyssalBlessing.spriteatlas` |
| 图集枚举 | `Assets/FrameWork/Scripts/Enums/BaseGameEnum.cs`（SpriteAtlasTypeEnum.AbyssalBlessing） |
| 图标加载入口 | `Assets/Scripts/Component/Handler/IconHandler.cs`（SetAbyssalBlessingIcon） |

## 约束

- **Excel 是唯一真实源**：任何馈赠配置变更必须改 Excel（`AbyssalBlessingInfo` 工作表），再用 Unity 编辑器导出 JSON。仅改 JSON 会被下次导出覆盖。
- **`AbyssalBlessingInfoBean.cs` 是自动生成的**，禁止直接修改；扩展逻辑写到 `AbyssalBlessingInfoBeanPartial.cs`。
- **升级链由馈赠表 `parent_id`+`level` 定义，不是 BUFF 字段**。每个等级是一条独立配置行，`buff_ids` 只决定该级的效果数值。
- **`parent_id` 链表式逐级指向上一级 id**（2 级指向 1 级、3 级指向 2 级），**不是都指向根**；`level` 从 1 起连续递增。链断裂会导致 `RollCandidates` 取不到下一级、该族卡住。
- **不要直接写 `manager.dicAbyssalBlessingBuffsActivie`**，必须经过 `BuffHandler.AddAbyssalBlessing`，否则跳过同族替换与事件通知。
- **馈赠目标固定为防守核心**（CreatureFightTypeEnum.FightDefenseCore），施加者也是核心 UUID。
- **`ClearAbyssalBlessing` 只能在征服全通关 + 领奖结束后调用**（已在 `ActionForUIRewardSelectEnd` 处理），中途调用会丢失玩家选择。
- **选择界面随机展示 SHOW_NUM=3**：每族只出"当前等级+1"那一行，已满级族不出；候选数不足 3 时按实际数量展示。
- **馈赠图标必须放入 `AtlasForAbyssalBlessing.spriteatlas`**，加载只能走 `IconHandler.Instance.SetAbyssalBlessingIcon`。

## 常见坑

1. **新增馈赠游戏里查不到** → 多半只改了 JSON 未改 Excel（下次导出被覆盖），或改了 Excel 没跑导出工具。统一从 Excel 改 + 导出 JSON。
2. **数据写错工作表** → 工作表名为 `AbyssalBlessingInfo`（`wb["AbyssalBlessingInfo"]`），不存在 `Sheet1`/`Sort Title` 页。
3. **升级链断裂某族卡住** → 检查 `parent_id` 是否逐级指向上一级 id、`level` 是否从 1 连续递增；`RollCandidates` 找不到 `level==owned+1` 的行就不会再出该族。
4. **升级时旧级没被移除/数值叠加** → 确认走的是 `BuffHandler.AddAbyssalBlessing`（内部 `GetFamilyRootId` + `RemoveAbyssalBlessingByRootId`），而不是直接塞容器。
5. **误以为靠 BUFF 的 buff_parent_id 升级** → 那是旧设计已废弃；深渊馈赠升级只看馈赠表 `parent_id`/`level`。
6. **`GetAbyssalBlessingOwnedLevel` 传错参数** → 必须传**族根 id**（`GetFamilyRootId` 得到），不是任意等级的 id。
7. **跨关切换馈赠被清空** → `ClearAbyssalBlessing` 提前调用（仅最终领奖才能调）。
8. **馈赠图标显示"未知"** → Sprite 未加入 `AtlasForAbyssalBlessing.spriteatlas`，或代码错用 `SetUIIcon`。统一走 `SetAbyssalBlessingIcon`。
