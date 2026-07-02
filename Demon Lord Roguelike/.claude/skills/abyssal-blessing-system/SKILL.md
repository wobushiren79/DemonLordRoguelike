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
AbyssalBlessingInfoBean     - 馈赠配置数据（id / icon_res / parent_id / level / buff_ids / name / details / remark / valid）；valid==0 的行被生成器过滤、运行时不存在
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

#### 馈赠类型：两个正交维度（`level`/`parent_id` 管"强度升级链"，`max_count` 管"一局获得次数"）

> ⚠️ 自 `max_count` 字段引入后，「强度」与「次数」彻底解耦：`level`/`parent_id` 只定义升级链，`max_count` 只定义一局可获得次数上限（`0`/留空=不限）。不要再用 `level=1` 单行族去表达"一次性"。

| 类型 | `level` | `parent_id` | `max_count` | 候选行为 | 等级角标 |
|------|---------|-------------|-------------|----------|----------|
| **可重复·不限** | `0` | `0` | `0` | 始终可出现，重复选取叠加（如"增殖"） | 不显示（`level<=0`） |
| **可重复·限 N 次** | `0` | `0` | `N(>0)` | 一局最多获得 N 次，达上限后不再出现 | 不显示（`level<=0`） |
| **一次性**（= N=1 特例） | `0` | `0` | `1` | 整局仅 1 次（如大力出奇迹/膘肥体壮/钢铁憨憨/急性子） | 不显示（`level<=0`） |
| **多级升级链** | `1..N` | 链式 | `0`(留空) | 每次只出现"已拥有族的下一级" | 显示 `LvN` |

- **次数门控**：`level<=0` 的候选资格 = `max_count<=0`（不限）**或** `GetAbyssalBlessingPickCount(族根) < max_count`。计数来自运行时容器——`level<=0` 选取**不走同族替换**（`IsLevelUp()=false`）、每次选取各新增一个实例，故"同族根实例数 == 已选取次数"，随 `ClearAbyssalBlessing` 清零、**无需存档**。判定落点 [UIFightAbyssalBlessing.IsCandidateEligible](../../../Assets/Scripts/Component/UI/Game/FightAbyssalBlessing/UIFightAbyssalBlessing.cs)；计数方法 [BuffHandler.GetAbyssalBlessingPickCount](../../../Assets/Scripts/Component/Handler/BuffHandler.cs)。
- **「一次性」统一用 `level=0 + max_count=1` 表达**（不再用 `level=1`），让 `level` 纯表强度链、`max_count` 纯表次数，单一真实源、避免双轨。升级链(`level>0`)的次数由链长自限，`max_count` 留 `0` 即可、对其无意义。
- `AbyssalBlessingInfoCfg.IsSingleLevelOnce(info)`（=`level==1` 且 `GetFamilyMaxLevel(族根)==1`）与 `GetFamilyMaxLevel` 仍保留，仅用于 UI 隐藏 `level>=1` 退化族的角标；可重复馈赠(`level<=0`)角标本就隐藏、不依赖它。`level=1` 单行族这种旧"一次性"写法虽仍可工作，但已**废弃**，新增一律走 `max_count`。
- BUFF 绑定：可重复馈赠按需绑**常驻型**(属性/条件/周期)或**即时型**(`BuffEntityInstant`+计数器，见下文"常驻可重复馈赠")；单体定向类(大力出奇迹等)绑常驻 `BuffEntityAttributeSingleTarget`。**max_count 次数门控只在候选层生效，与 BUFF 类型/叠加逻辑完全独立、互不影响**。

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
- **属性当场生效靠事件驱动刷新（勿删事件触发）**：`AddAbyssalBlessing` 末尾 `TriggerEvent(Buff_AbyssalBlessingChange)`，由 `GameFightLogic.EventForAbyssalBlessingChange` 监听并立即对**防守核心 + 全部普通防守生物**调用 `RefreshBaseAttribute`。属性类馈赠BUFF（含单体定向「随机一只属性翻倍」）只有在 `RefreshBaseAttribute` 时才会被算进 `dicAttribute`；征服模式**普通关卡→普通关卡**走 `ContinueNextLevelInSameScene`「保留防御生物 / 魔王 / BUFF 等所有现场状态」、不重载场景也不重算属性，若馈赠变化时不刷新，加成要等到下次重载场景（如**切到BOSS关**走 `StartNextGameForBoss` 重建生物实体触发 `RefreshBaseAttribute`）才生效——表现为"普通关卡选了不生效、切到BOSS才生效"的BUG。BuffHandler 只负责触发事件，刷新职责归属战斗逻辑。

### 3. 事件通信

```
EventsInfo.Buff_AbyssalBlessingChange   // BUFF系统-深渊馈赠变化（参数 AbyssalBlessingEntityBean）
```

- **触发处**：`BuffHandler.AddAbyssalBlessing` 末尾。
- **监听处**：
  - `UIViewAbyssalBlessingInfoContent` → 在面板可见时刷新已选馈赠列表。
  - `GameFightLogic.EventForAbyssalBlessingChange` → 立即对防守核心 + 全部普通防守生物 `RefreshBaseAttribute`，使属性类馈赠（含单体定向翻倍）当场生效（不必等切BOSS关重载场景）。

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
├── 刷新按钮 ui_RefreshBtn/ui_RefreshBtnText     # 见下「馈赠刷新」
└── 跳过按钮

UIViewAbyssalBlessingInfoContent                # 战斗界面常驻列表（已选馈赠展示）
└── UIViewAbyssalBlessingInfoContentItem        # 单项（含 popup 触发）
    └── PopupButtonCommonView                    # 点击 → PopupEnum.AbyssalBlessingInfo

UIPopupAbyssalBlessingInfo : PopupShowCommonView # 馈赠详情气泡
```

### 6. 馈赠刷新（研究门控 + 征服run共享次数池）

选择界面右侧「刷新按钮」`ui_RefreshBtn`：点击消耗一次刷新次数、**重抽当前 3 个候选**（内部即 `SetData(actionForSelect, actionForSkip)` 重跑 `BuildCandidatePool`+`RollCandidates` 并重渲染，保留原选择/跳过回调）。

- **研究门控**：由**强化**分支研究节点 `UnlockEnum.AbyssalBlessingRefreshNum`(200500001, `level_max=5`) 解锁。未解锁(等级0)或非征服模式时整个刷新按钮隐藏——`RefreshRefreshBtnState()` 用 `UserUnlockBean.CheckIsUnlockAbyssalBlessingRefresh()` 判定，在每次 `SetDataInternal` 末尾调用。
- **次数作用域=整个征服run共享**：剩余次数池挂在 `FightBeanForConquer.abyssalBlessingRefreshUsedNum` 上，`GetAbyssalBlessingRefreshRemainNum()` = 研究上限(`GetUnlockAbyssalBlessingRefreshMax`) − 已用；`ReduceAbyssalBlessingRefreshNum()` 消耗。**新 run 随 `FightBeanForConquer` 重建自动归 0 回满**（无需存档、无显式 refill）。UI 经 `FightHandler.Instance.manager.GetCachedFightLogic().fightData as FightBeanForConquer` 取到该池；非征服(测试)取到 null → 按钮隐藏。
- **次数文本**：`ui_RefreshBtnText` 显示「刷新x{剩余}」，多语言 UIText id `4000018`（`string.Format(GetTextById(4000018), remain)`）。剩余为 0 时点击 Toast 提示「刷新次数已用完」(UIText 2007，与传送门刷新共用)、不刷新。
- **仿传送门刷新实现**：整套模式照搬 `UIBasePortal` 的 `PortalRefreshNum`(次数存 `UserTempBean`、研究等级=上限)；差异是本功能次数存 `FightBeanForConquer`(随 run 生灭)而非 `UserTempBean`。

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
| 第 1 行 | 字段名（导出 JSON 的 key） | `id` `icon_res` `parent_id` `level` `buff_ids` `name[language]` `details[language_1]` `remark` `valid` `max_count` |
| 第 2 行 | 字段类型（导出工具识别） | `long` `string` `long` `int` `string` `long` `long` `string` `int` `int` |
| 第 3 行 | 中文说明（仅给策划看） | `序号` `图标名字` `父级深渊馈赠ID` `等级` `buff_ids` `名字-中文` `描述中文` `备注` `是否有效(0无效1有效)` `一局最多可获得次数(0=不限)` |
| 第 4 行起 | 数据行 | 一行一个馈赠（一个等级 = 一行） |

各列说明：

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `id` | long | ✓ | 馈赠唯一 ID（当前真实数据为 **10 位**，如 `2000001005`，末 3 位=等级序号）。**同族不同等级用不同 id**。 |
| `icon_res` | string |  | 图标 Sprite 名（不带后缀），须存在于 `AtlasForAbyssalBlessing.spriteatlas`（该图集打包整个 `Assets/LoadResources/Textures/AbyssalBlessing/` 文件夹）。同族各级共用一张图标，命名 `abyssal_*`，当前所有族系均已配图。 |
| `parent_id` | long | ✓ | 父级馈赠 id。**族根填 `0`；2 级填 1 级的 id；3 级填 2 级的 id**……链表式逐级指向上一级。 |
| `level` | int | ✓ | **只表强度升级链**：`0`=不参与升级链（可重复类，次数由 `max_count` 控制）；`1..N` 链式=多级升级链。详见上文「馈赠类型」。 |
| `buff_ids` | string | ✓ | 这一级对应的 BUFF id 列表，多个用英文逗号 `,` 分隔。数值大小由 BUFF 决定，**不同等级指向不同 BUFF**。 |
| `name` | language | ✓ | 名字多语言文本 id（通常填**与该行 id 相同的值**）。 |
| `details` | language | ✓ | 描述多语言文本 id（通常也填与该行 id 相同的值；Bean 读取时取该 id 的第 2 列文本）。 |
| `remark` | string |  | 备注（仅文档用途，不影响逻辑）。建议写清"效果+等级"，如 `钱多多-20%（2级）`。 |
| `valid` | int | ✓ | 是否有效：`1`=有效，`0`=无效。**填 0 的行不会进入候选池（也无法被 `GetItemData` 查到，运行时彻底不存在）**，用于临时下线某馈赠而不删行。⚠️ 新增行务必填 `1`——JSON 缺省值为 0 会被当成无效。该过滤由生成器内置（`AbyssalBlessingInfo` 表含 `valid` 列即自动生成 `valid!=0` 过滤，详见 editor-extension-system SKILL）。 |
| `max_count` | int |  | **一局最多可获得（选取）次数**，仅对可重复馈赠(`level<=0`)生效：`0`/留空=不限（如"增殖"）；`N>0`=整局最多 N 次，达上限后该馈赠不再进入候选池。`1`=一次性。升级链(`level>0`)次数由链长自限，留 `0` 即可。门控在 [UIFightAbyssalBlessing.IsCandidateEligible]，计数 `BuffHandler.GetAbyssalBlessingPickCount(族根)`。 |

### 第 1 步：写入 Excel（用 excel-io / openpyxl）

**可重复·不限次馈赠**（不参与升级、可无限重复选取叠加，如"增殖"）：1 行，`parent_id=0`、`level=0`、`max_count=0`，`valid=1`。

**可重复·限次馈赠**（一局最多获得 N 次；`N=1` 即"一次性"，选后不再出现、不显示角标）：1 行，`parent_id=0`、`level=0`、`max_count=N`，`valid=1`。BUFF 绑常驻型即可。
```
# 一次性（N=1）：
id=2000005001, parent_id=0, level=0, buff_ids=3000600001, name=2000005001, details=2000005001, remark=一次性强力馈赠（一局限1次）, valid=1, max_count=1
# 一局最多 3 次：把上面 max_count 改成 3 即可
```

**多等级馈赠**（推荐写法，N 级 = N 行；下例新开一个"暴击"族，沿用 10 位 id 规律）：

```
id=2000004001, parent_id=0,          level=1, buff_ids=3000500001, name=2000004001, details=2000004001, remark=暴击率+5%（1级）,  valid=1
id=2000004002, parent_id=2000004001, level=2, buff_ids=3000500002, name=2000004002, details=2000004002, remark=暴击率+10%（2级）, valid=1
id=2000004003, parent_id=2000004002, level=3, buff_ids=3000500003, name=2000004003, details=2000004003, remark=暴击率+15%（3级）, valid=1
```

修改脚本参考（遵循项目 Excel 规则：仅用 openpyxl、UTF-8、保存前先备份、临时脚本用完即删；本环境 Python 命令为 `python3`）：

```python
import openpyxl
path = r"Assets/Data/Excel/excel_abyssal_blessing_info[深渊馈赠信息].xlsx"
wb = openpyxl.load_workbook(path)
ws = wb["AbyssalBlessingInfo"]          # 注意：工作表名为 AbyssalBlessingInfo，不是 Sheet1
# 列顺序：id, icon_res, parent_id, level, buff_ids, name, details, remark, valid
# ⚠️ 新增行必须按 id 升序插入到正确位置（参考 .claude/scripts/excel_add_row.py 的排序插入逻辑），
#    仅当新 id 比所有现有 id 都大时才允许落到表尾（如下例）。
# ⚠️ valid 必须填 1（有效），缺省 0 会被生成器过滤掉、运行时不存在。
ws.append([2000004001, "", 0,          1, "3000500001", 2000004001, 2000004001, "暴击率+5%（1级）",  1])
ws.append([2000004002, "", 2000004001, 2, "3000500002", 2000004002, 2000004002, "暴击率+10%（2级）", 1])
ws.append([2000004003, "", 2000004002, 3, "3000500003", 2000004003, 2000004003, "暴击率+15%（3级）", 1])
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

## 影响奖励系统的特殊馈赠（即时BUFF + 计数器模式）

部分特殊馈赠的效果**不是数值属性加成**，也**不在选取当下产生可见效果**，而是要在「征服 BOSS 通关领奖」时改变奖励生成参数。这类馈赠照搬「增殖」的**即时BUFF（`BuffEntityInstant`）模式**实现，关键点：

- 馈赠配置为 `level=0`、`parent_id=0` 的常驻可重复馈赠，`buff_ids` 指向一个 `class_entity` 为自定义 `BuffEntityInstant` 子类的 BUFF。
- 选取馈赠 → `BuffHandler.AddAbyssalBlessing` 解析 buff_ids → BUFF `SetData` 时立即 `TriggerBuffInstant`，在其中把加成**累加到 `FightBeanForConquer` 上的计数器字段**（不依赖 BUFF 在容器内常驻：`level=0` 选取**不走同族替换**、每次各新增一个即时 BUFF，触发即弃、加成已落到计数器，故可重复选取叠加）。
- 领奖时 `GameFightLogicConquer.ActionForUIFightSettlementNext` 读取计数器，调整 `RewardSelectBean` 的生成参数。

当前已实现的两个奖励类特殊馈赠：

| 馈赠 | id | buff_ids | class_entity | 计数器字段 | 领奖时效果 |
|------|----|----------|--------------|-----------|-----------|
| 奖励多多 | 1000002001 | 3000900001 | `BuffEntityInstantRewardMoreItem` | `FightBeanForConquer.rewardAddItemNum` | `RewardSelectBean.createItemNum += n`（领奖宝箱按奖励数量实时实例化，自动多出对应宝箱） |
| 再来一瓶 | 1000003001 | 3001000001 | `BuffEntityInstantRewardMoreSelect` | `FightBeanForConquer.rewardAddSelectNum` | `RewardSelectBean.selectNumMax += n`，随后裁剪到不超过 `listReward.Count`（超出的次数无对应宝箱可开，自然失效） |

> 两个馈赠图标当前均用 `ui_abyssalblessing_7`。新增同类「领奖参数型」馈赠时沿用此模式：写一个 `BuffEntityInstant` 子类累加到 `FightBeanForConquer` 计数器 → 在领奖初始化处读取应用；切勿用 BUFF 容器查询累计（即时 BUFF 触发即弃、容器内实例不承载累计数值，无法据此叠加）。详见 `fight-reward-system` skill 的领奖生成链路。

## 单体定向馈赠（随机一只防守生物属性/攻速翻倍）

另一类「可重复·限次(`level=0` + `max_count`)」馈赠的效果是**只作用于随机一只防守生物**（不是全体、也不是防守核心），如下 4 个（当前均 `max_count=1` 整局限 1 次）：

> 设计：这 4 个原为 `level=0`(可无限叠加，各锁定一只随机魔物)，现统一为 `level=0` + `max_count=1` —— **整局最多获得 1 次**（候选判定 `GetAbyssalBlessingPickCount(族根)` 达 `max_count` 后不再出现）。BUFF 仍是常驻单体定向型(非 Instant)，与次数门控**完全独立**；`level<=0` 故 UI 不显示等级角标。**想改一局可获次数只改 `max_count`**（如填 `3` 即可叠加锁定 3 只随机魔物），无需改 C# 代码。

| 馈赠 | id | buff_ids | BUFF class_entity | 效果 |
|------|----|----------|-------------------|------|
| 大力出奇迹 | 1000004001 | 3001100001 | `BuffEntityAttributeSingleTarget`(data=ATK,rate=1) | 随机一只防守魔物攻击力翻倍 |
| 膘肥体壮 | 1000005001 | 3001200001 | `BuffEntityAttributeSingleTarget`(data=HP,rate=1) | 随机一只防守魔物生命翻倍 |
| 钢铁憨憨 | 1000006001 | 3001300001 | `BuffEntityAttributeSingleTarget`(data=DR,rate=1) | 随机一只防守魔物护甲翻倍 |
| 急性子 | 1000007001 | 3001400001 | `BuffEntityAttributeAttackTimeSingleTarget`(rate=0.5) | 随机一只防守魔物攻速翻倍(攻击间隔减半) |

实现要点（详见 `buff-system` skill「单体定向深渊馈赠」）：
- 这些 BUFF 实现标记接口 `IBuffSingleTarget`(只暴露 `SingleTargetCreatureUUId`)，`SetData` 时用 `fightData.GetRandomDefenseCreatureUUId()`(实例方法在 `FightBean` 上) 从 `dlDefenseCreatureData` **随机锁定一只生物 UUID**；`ClearData` 归还对象池时清空。
- 属性类由 `FightCreatureBean.CollectFromBuffList` 按 UUID 过滤（`SingleTargetCreatureUUId != 本生物` 则跳过，只对锁定生物 emit modifier）；攻速类由 `BuffHandler.ChangeAttackTimeDataForBuff` 扫描馈赠池按同一 UUID 比对。
- **复制魔物(增殖)不继承单体定向**：`BuffEntityInstantCloneDefenseCreature` 克隆出的魔物是**新 UUID**，与单体定向馈赠锁定的原魔物 UUID 不匹配，故**不显示也不继承**针对原魔物的单体定向馈赠；克隆体只继承「作用于全体防守生物」的馈赠(靠 `trigger_creature_type` 过滤、与 UUID 无关，新魔物 `RefreshBaseAttribute` 时自动收集)。这是预期行为。
- **关键安全约束**：`dlDefenseCreatureData` 的 `CreatureBean` 与玩家**存档共享引用**，故**绝不能改 `creatureAttribute`**（会污染永久存档）；本方案只改运行时计算出的 `dicAttribute`/攻击时间，征服全通关领奖后随 `ClearAbyssalBlessing` 清空。
- 图标 `ui_abyssalblessing_11~14`（PixelLab 32px 描边图）。

> 与「领奖参数型」(即时BUFF+计数器)的区别：单体定向用的是**常驻属性/攻速BUFF**(非Instant)，靠 UUID 过滤限定到单只生物、由 `RefreshBaseAttribute` 重算生效，无需计数器。
> ⚠️ 属性类(ATK/HP/DR 翻倍)依赖 `dicAttribute` 重算，故选取时由 `Buff_AbyssalBlessingChange` 事件 → `GameFightLogic.EventForAbyssalBlessingChange` **立即刷新**已在场的防守核心与全部防守生物（否则普通关卡内选取不生效，须等下次场景重载/切BOSS关才重算——已修复）。攻速类(急性子)由 `ChangeAttackTimeDataForBuff` 每次攻击实时缩放，不依赖刷新；被一并刷新也无副作用。

### 战斗卡片展示「作用于本魔物的馈赠」

战斗卡片 `UIViewCreatureCardItemForFight` 会在卡面上展示**实际作用于该魔物**的深渊馈赠图标(`ui_AbyssalBlessingContent` + `ui_AbyssalBlessingItem` 缓存池，详见 creature-card-system skill)。判定统一走 **`AbyssalBlessingUtil.IsAbyssalBlessingTargetCreature(buff, creatureData, FightDefense)`**(在 `Assets/Scripts/Utils/AbyssalBlessingUtil.cs`)，口径与属性/攻速管线一致：

- **三连判定**：① `trigger_creature_type` 过滤(None 或 == 本生物战斗类型)；② 单体定向过滤(`SingleTargetCreatureUUId == 本魔物 UUID`)；③ 仅 `IAttributeModifierSource`/`BuffEntityAttributeAttackTime` 类(会改属性/攻速)才算作用于生物。
- **会显示**：全体防守加成(强身健体/伤害性极强/唯快不破/坚不可摧/时光沙漏，每张防守卡各一份) + 定向到本魔物的(大力出奇迹/膘肥体壮/钢铁憨憨/急性子，按锁定 UUID 精确匹配)。
- **不显示**：作用敌方进攻生物的(慢条斯理 `trigger=FightAttack`)、只作用防守核心的、掉落类(钱多多 `BuffEntityConditional`)、奖励类(奖励多多/再来一瓶 `BuffEntityInstant`)、复制类(增殖 `BuffEntityInstant`)——它们不改本魔物数值。
- 卡片监听 `Buff_AbyssalBlessingChange` 刷新；克隆体卡片经 `Buff_DefenseCreatureAdd` 新建时 `SetData` 内即刷新——克隆体新 UUID 故只显示全体馈赠、不显示原魔物的单体定向馈赠。

### 卡片详情 popup 的属性数值同步

战斗中点开魔物卡牌的详情 popup(`UIViewCreatureCardDetails`)展示 HP/DR/ATK/ASPD 时，必须用 **`CreatureBean.GetAttribute(类型, includeAbyssalBlessing: true)`** 才会反映深渊馈赠（含单体定向翻倍）；漏传 `true` 会导致详情面板数值与场上实际值不符（典型症状：「大力出奇迹」生效后场上伤害翻倍但详情攻击力没变）。

- `GetAttribute(true)` 内部经 `CreatureBean.GetAbyssalBlessingChangeAttribute`，该方法对全局池每个 BUFF 先用 **`AbyssalBlessingUtil.IsAbyssalBlessingTargetCreature(buff, this, creatureFightType=FightDefense)`** 做与战斗管线一致的三连过滤，再 `ChangeData` 叠加——故单体定向馈赠**只对被锁定 UUID 的那只魔物翻倍**，不会无差别加到所有卡（缺此过滤即为该 bug 根因）。
- 战斗实体走的是另一条链路(`FightCreatureBean.RefreshBaseAttribute` 经 `ModifierPipeline`)，两条链路对单个翻倍馈赠结果一致；非战斗场景馈赠池已 `ClearAbyssalBlessing` 清空，详情面板传 `true` 无副作用。

## 动态数值馈赠（加成率随战况实时变化：都是兄弟/杀红了眼）

又一类馈赠的加成率**不是配置写死的固定值**，而是随运行时战况（场上魔物数量 / 累计击杀数）**每次重算属性时动态计算**——作用于**全体防守魔物**（`trigger_creature_type=1`，不含核心）。它们的 BUFF 继承抽象基类 `BuffEntityAttributeDynamicRate : BuffEntityAttribute`（重写 `CollectModifiers`+`ChangeData` 用 `GetDynamicRate()` 替代配置固定率，仅走 PercentAdd，用于 ATK/DR/HP；详见 `buff-system` SKILL「动态率深渊馈赠」）。

| 族（3 属性各一族） | class_entity（通用功能类） | 公式（rate=每次重算属性时算） | 每级 trigger_value_rate |
|------|--------------|------|------|
| 都是兄弟·攻击/护甲/生命 | `BuffEntityAttributeScaleByDefenseCount`（data=ATK/DR/HP；通用"随场上存活防守魔物数缩放"，当前用于本馈赠，可被其它同功能馈赠复用） | `(场上存活防守魔物数 N - 1) × trigger_value_rate`；N 数 `dlDefenseCreatureEntity.List` 中 `!IsDead()`，N≤1 为 0（减 1 扣自身） | 0.01~0.05（每只 +1%~5%，5 级） |
| 杀红了眼·攻击/护甲/生命 | `BuffEntityAttributeScaleByKillCount`（data=ATK/DR/HP；通用"随本局累计击杀数缩放"，当前用于本馈赠，可被其它同功能馈赠复用） | `fightRecordsData.totalKillNumForDef × trigger_value_rate`（仅魔物击杀，征服 run 内跨关卡累积至 BOSS 关不重置） | 0.01~0.05（每只 +1%~5%，5 级） |

**id 段（6 族，每族 5 级共 30 行）**：

| 族 | 馈赠 id | BUFF id | 图标 |
|----|---------|---------|------|
| 都是兄弟·攻击 | 2000008001~005 | 3001500001~005 | ui_abyssalblessing_120 |
| 都是兄弟·护甲 | 2000009001~005 | 3001600001~005 | ui_abyssalblessing_121 |
| 都是兄弟·生命 | 2000010001~005 | 3001700001~005 | ui_abyssalblessing_122 |
| 杀红了眼·攻击 | 2000011001~005 | 3001800001~005 | ui_abyssalblessing_123 |
| 杀红了眼·护甲 | 2000012001~005 | 3001900001~005 | ui_abyssalblessing_124 |
| 杀红了眼·生命 | 2000013001~005 | 3002000001~005 | ui_abyssalblessing_125 |

- 共性：多级升级链 `level=1~5`、`parent_id` 链式、`trigger_creature_type=1`、`valid=1`、`max_count=0`。BUFF 的 `name`/`content` 指向本族根 BUFF id、语言用 `{Percentage}` 占位；馈赠语言逐级写死数值。
- **当前 id 进度**：深渊馈赠 BUFF 段（`buff_type=3`）最大 → `3002000005`（族号用到 20）；馈赠多级 id 最大 → `2000013005`（族号用到 13）；彩色图标编号 → 已到 125（下一个 126）。

**广播重算（关键，动态率靠它生效，事件驱动）**：rate 变化后必须重算 `dicAttribute` 才生效。为此在高频事件按守卫广播全体防守魔物重算：

- 守卫（**热路径 O(1)，勿在死亡等高频点遍历池**）：动态率场景用 **`BuffHandler.Instance.HasDynamicRateAbyssalBlessing()`**——读 `BuffManager.hasDynamicRateAbyssalBlessing` 缓存布尔，仅当本局带了动态馈赠才广播，避免普通对局无谓开销。该缓存**单调置位**：`BuffHandler.AddAbyssalBlessing` 选取到含动态率BUFF的馈赠时直接置 `true`（一整局只增不减——馈赠不会中途删除，升级替换同族仍带动态BUFF），仅 `BuffManager.ClearAbyssalBlessing`（全通关领奖后）复位为 false。故**无需**重新遍历整池、也无独立 Refresh 方法。
- 重算入口 **`GameFightLogic.RefreshAllDefenseCreatureAttribute()`**（public）：刷新防守核心 + 全部防守魔物 `RefreshBaseAttribute`（由原 `EventForAbyssalBlessingChange` 的循环抽出，后者改为调它）。
- 两处广播落点：① `GameFightLogic.EventForGameFightLogicCreatureDeadEnd` 中——魔物死亡（都是兄弟 N 减少）/敌人死亡（杀红了眼击杀数增加）都重算，且**该重算放在 `CheckGameEnd()` 之前**（先处理死亡带来的属性变化，再检测游戏结束）；② `CreatureHandler.CreateDefenseCreatureEntity` 末尾（加完生物 BUFF 后）**推送新事件** `EventsInfo.GameFightLogic_DefenseCreatureCreate`（参数 FightCreatureEntity），由 `GameFightLogic.EventForDefenseCreatureCreate` 监听→按守卫重算，使「都是兄弟」随 N 增大即时生效（CreatureHandler 只负责生成、推事件，重算职责归 GameFightLogic）。二者均先过 `HasDynamicRateAbyssalBlessing()` 守卫。

> 动态率馈赠继承 `BuffEntityAttribute`（`IAttributeModifierSource`）且作用全体防守（`trigger_creature_type=1`），故天然被战斗卡片展示判定（`AbyssalBlessingUtil.IsAbyssalBlessingTargetCreature`）与卡片详情 `GetAttribute(true)` 通过，无需改判定逻辑。

## 关键文件速查

| 功能 | 文件路径 |
|------|----------|
| 馈赠配置 Bean（自动生成，禁改） | `Assets/Scripts/Bean/MVC/Game/AbyssalBlessingInfoBean.cs` |
| 馈赠配置扩展（IsLevelUp / GetFamilyRootId） | `Assets/Scripts/Bean/MVC/Game/AbyssalBlessingInfoBeanPartial.cs` |
| 馈赠运行时实例 | `Assets/Scripts/Bean/Game/AbyssalBlessingEntityBean.cs` |
| Excel 源表（数据在 AbyssalBlessingInfo 工作表） | `Assets/Data/Excel/excel_abyssal_blessing_info[深渊馈赠信息].xlsx` |
| 导出 JSON | `Assets/Resources/JsonText/AbyssalBlessingInfo.txt` |
| 多语言 | `Assets/Resources/JsonText/Language_AbyssalBlessingInfo_{cn,en}.txt` |
| BUFF 添加入口 | `Assets/Scripts/Component/Handler/BuffHandler.cs`（深渊馈赠 BUFF region：AddAbyssalBlessing / RemoveAbyssalBlessingByRootId / GetAbyssalBlessingOwnedLevel / GetDefenseCoreUUID；末尾仅触发 Buff_AbyssalBlessingChange，不直接刷新属性） |
| 馈赠变化刷新属性 | `Assets/Scripts/Game/Logic/GameFightLogic.cs`（EventForAbyssalBlessingChange：监听 Buff_AbyssalBlessingChange → 刷新防守核心 + 全部防守生物 RefreshBaseAttribute） |
| BUFF 容器 | `Assets/Scripts/Component/Manager/BuffManager.cs`（dicAbyssalBlessingBuffsActivie / AddAbyssalBlessingEntity / AddAbyssalBlessingBuff / ClearAbyssalBlessing） |
| 征服模式流程 | `Assets/Scripts/Game/Logic/GameFightLogicConquer.cs`（ActionForUIFightSettlementNext 读取奖励类馈赠计数器并应用到 RewardSelectBean） |
| 数据持有 | `Assets/Scripts/Bean/Game/FightBeanForConquer.cs`（AddAbyssalBlessing；奖励类馈赠计数器 rewardAddItemNum / rewardAddSelectNum） |
| 奖励类即时BUFF（奖励多多/再来一瓶） | `Assets/Scripts/Game/Buff/BuffEntity/Instant/BuffEntityInstantRewardMoreItem.cs` / `BuffEntityInstantRewardMoreSelect.cs` |
| 单体定向馈赠BUFF（大力出奇迹/膘肥体壮/钢铁憨憨/急性子） | `Assets/Scripts/Game/Buff/BuffEntity/Attribute/BuffEntityAttributeSingleTarget.cs` / `BuffEntityAttributeAttackTimeSingleTarget.cs` / `Assets/Scripts/Game/Buff/Interface/IBuffSingleTarget.cs`(接口，仅 SingleTargetCreatureUUId；不限深渊馈赠) |
| 动态率馈赠BUFF（当前用于 都是兄弟/杀红了眼） | `Assets/Scripts/Game/Buff/BuffEntity/Attribute/BuffEntityAttributeDynamicRate.cs`(抽象基类，GetDynamicRate 动态率) / `BuffEntityAttributeScaleByDefenseCount.cs`(通用"随场上魔物数缩放"，当前用于都是兄弟) / `BuffEntityAttributeScaleByKillCount.cs`(通用"随累计击杀数缩放"，当前用于杀红了眼) |
| 动态馈赠广播重算 | `Assets/Scripts/Component/Handler/BuffHandler.cs`(O(1) 缓存守卫 `HasDynamicRateAbyssalBlessing()` 读 `BuffManager.hasDynamicRateAbyssalBlessing`；缓存在 `AddAbyssalBlessing` 选取动态率馈赠时单调置 true、`ClearAbyssalBlessing` 复位) / `Assets/Scripts/Game/Logic/GameFightLogic.cs`(`RefreshAllDefenseCreatureAttribute()` 全体重算 + 死亡事件在 CheckGameEnd 前按守卫广播 + `EventForDefenseCreatureCreate` 监听放置事件按守卫广播) / `Assets/Scripts/Component/Handler/CreatureHandler.cs`(CreateDefenseCreatureEntity 末尾只推送 `GameFightLogic_DefenseCreatureCreate` 事件，不直接重算) |
| 随机锁定一只防守生物 | `Assets/Scripts/Bean/Game/FightBean.cs`(`GetRandomDefenseCreatureUUId()` 实例方法，从 dlDefenseCreatureData 随机取一只 UUID；BUFF 经 fightData 调用) |
| 馈赠作用判定工具 | `Assets/Scripts/Utils/AbyssalBlessingUtil.cs`(`IsAbyssalBlessingTargetCreature` 三连判定 + `CollectAbyssalBlessingEntityBean` 收集作用于某生物的馈赠实体，供卡片展示与属性/攻速管线统一口径) |
| 单体过滤落点 | `Assets/Scripts/Bean/Game/FightCreatureBean.cs`(CollectFromBuffList 按 SingleTargetCreatureUUId 比对) / `Assets/Scripts/Component/Handler/BuffHandler.cs`(ChangeAttackTimeDataForBuff 按 SingleTargetCreatureUUId 比对) |
| 复制魔物(增殖) | `Assets/Scripts/Game/Buff/BuffEntity/Instant/BuffEntityInstantCloneDefenseCreature.cs`(克隆新 UUID，仅继承全体馈赠不继承单体定向，触发 Buff_DefenseCreatureAdd) |
| 战斗卡片馈赠展示 | `Assets/Scripts/Component/UI/Common/CreatureCard/UIViewCreatureCardItemForFight.cs`(RefreshAbyssalBlessing 调 `AbyssalBlessingUtil.CollectAbyssalBlessingEntityBean(creatureData, FightDefense, 复用List)` 收集作用于本魔物的馈赠实体；收集逻辑已下沉到 Util，不再在 UI 层手写循环) |
| 选择界面 | `Assets/Scripts/Component/UI/Game/FightAbyssalBlessing/UIFightAbyssalBlessing.cs`（SetData / RollCandidates / OnClickForRefresh / RefreshRefreshBtnState 刷新按钮） |
| 刷新次数池 | `Assets/Scripts/Bean/Game/FightBeanForConquer.cs`（abyssalBlessingRefreshUsedNum / GetAbyssalBlessingRefreshRemainNum / ReduceAbyssalBlessingRefreshNum，整个征服run共享、新run自动回满） |
| 刷新研究上限 | `Assets/Scripts/Bean/Game/UserUnlockBean.cs`（GetUnlockAbyssalBlessingRefreshMax / CheckIsUnlockAbyssalBlessingRefresh，UnlockEnum.AbyssalBlessingRefreshNum=200500001 强化分支 level_max=5） |
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
- **可重复馈赠(`level<=0`)一局获得次数由 `max_count` 控制**：`0`/留空=不限；`N>0`=最多 N 次（门控 `GetAbyssalBlessingPickCount(族根) < max_count`，计数随 `ClearAbyssalBlessing` 清零）。"一次性"用 `level=0 + max_count=1`，**不要**再用已废弃的 `level=1` 单行族。升级链(`level>0`)不看 `max_count`。
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
9. **`max_count` 限次不生效** → ① 该馈赠须 `level<=0`（可重复类），升级链 `level>0` 不看 `max_count`；② `max_count` 默认/留空为 `0`=不限，限次要填 `>0`；③ **新增 `max_count` 列后必须在 Unity 跑「生成 Entity」让 `AbyssalBlessingInfoBean` 多出该字段、再「导出 JSON」**，否则代码 `info.max_count` 编译不过/读不到。
