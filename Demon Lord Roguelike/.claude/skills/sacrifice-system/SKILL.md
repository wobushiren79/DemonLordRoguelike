---
name: sacrifice-system
description: Demon Lord Roguelike 游戏的生物献祭升级系统开发指南。使用此SKILL当需要创建或修改生物献祭流程、献祭升级(等级提升)、献祭成功率公式(祭品数量/生物id/等级差修正 2^(祭品level-目标level))、保底机制、祭坛动画、献祭祭品消耗与装备退回、等级上限、献祭UI、升级手动加点UI(UICreatureAddAttribute)等，包括 CreatureSacrificeLogic 献祭逻辑、CreatureSacrificeBean 献祭数据、UICreatureSacrifice 献祭界面、UICreatureAddAttribute 升级加点界面、UICreatureManager 升级按钮、CreatureBean.UpLevelForSacrifice/CanUpLevel/IsMaxLevel/sacrificePityRate、CreatureUtil.GetSacrificeSuccessRate 成功率公式、CreatureUtil.GetAttributePointAddValue 单点增量、LevelInfo(level_exp/sacrifice_num/attribute_point) 等级配置、CreatureSacrifice_* 事件常量、UnlockEnum.Altar 祭坛解锁等。
watched_files:
  - Assets/Scripts/Game/Logic/CreatureSacrificeLogic.cs
  - Assets/Scripts/Bean/Game/CreatureSacrificeBean.cs
  - Assets/Scripts/Bean/Game/CreatureBean.cs
  - Assets/Scripts/Bean/Game/CreatureBeanPartial.cs
  - Assets/Scripts/Bean/Game/CreatureAttributeBean.cs
  - Assets/Scripts/Utils/CreatureUtil.cs
  - Assets/Scripts/Component/UI/Game/CreatureSacrifice/
  - Assets/Scripts/Component/UI/Game/CreatureAddAttribute/
  - Assets/Scripts/Component/UI/Game/CreatureManager/
  - Assets/Scripts/Bean/MVC/Game/LevelInfoBean.cs
  - Assets/Scripts/Bean/MVC/Game/LevelInfoBeanPartial.cs
  - Assets/Resources/JsonText/LevelInfo.txt
---

# 生物献祭升级系统 (Sacrifice System) 开发指南

## 核心概念

玩家在基地**祭坛 (Altar)** 上，把若干「祭品生物」献祭给一只「目标生物」，按**成功率掷骰**：成功则目标生物**升一级**并获得属性成长；失败则只消耗祭品并累积**保底成功率**。整个过程是「攒经验 → 满足升级门槛 → 献祭祭品掷骰 → 升级/保底」的链路。

- **升级双门槛**：① 目标生物当前经验 `levelExp` ≥ 下一级所需经验（决定能否**发起**献祭）；② 献祭祭品掷骰成功（决定本次是否**真的升级**）。
- **经验来源**：仅征服战斗胜利发放（普通关 `reward_exp` / BOSS 关 `reward_exp_boss`），见 `GameFightLogicConquer.AddLevelExpForLineupCreature`。满级生物不再累加经验。
- **失败不退经验**：升级失败只扣祭品，经验保留；只有升级成功才扣除本级所需经验。

### 解锁前置
献祭升级需要解锁祭坛：`UnlockEnum.Altar = 100100001`（`Assets/Scripts/Enums/GameStateEnum.cs`）。未解锁时 `UICreatureManager` 的献祭升级按钮恒隐藏；祭坛场景对象显隐见 `ScenePrefabForBase`（`isUnlockAltar`）。

### 祭品数量研究（增加献祭祭品上限）
献祭可选祭品上限可通过研究模块「设施」类节点提升：研究 `UnlockEnum.SacrificeNum = 100100002`（前置 = 开启献祭设施 `Altar`），`level_max=10`，每级 +1 祭品上限，水晶消耗 1000~10000（每级递增 1000）。上限计算统一走 `UserUnlockBean.GetUnlockSacrificeMax()` = `sacrificeMax`(基础 5) + 该研究等级，满级 5+10=15。配置见 `excel_research_info`/`excel_unlock_info`/`excel_language` 的 id=100100002 行。详见 research-system skill。

### 保底/不同id 成功率研究（影响成功率公式）
两个「设施」类研究节点(前置均 = 开启献祭设施 `Altar`，`level_max=10`，水晶消耗 `100,500,1000,5000,10000,20000,30000,40000,50000,100000`)直接决定成功率与保底：
- **`UnlockEnum.SacrificePityRate = 100100003`（献祭失败保底概率提升）**：献祭**失败**时按研究等级累积保底，每级 +5%（满级每次失败 +50%），衍生方法 `UserUnlockBean.GetUnlockSacrificeFailPityAddRate()` = 等级×0.05。**未解锁(0级)则失败不累积任何保底**（已去掉旧的 `rate*0.5` 保底逻辑）。
- **`UnlockEnum.SacrificeDifferentIdRate = 100100004`（不同魔物献祭成功率提升）**：**不同生物id** 祭品的单个成功率默认为 0，由该研究按等级给出，每级 +5%（满级 50%），衍生方法 `UserUnlockBean.GetUnlockSacrificeDifferentIdRate()` = 等级×0.05。**未解锁时不同id祭品成功率恒为 0**（已去掉旧的「不同id ×1/10」逻辑）。

配置见 `excel_research_info`/`excel_unlock_info`/`excel_language` 的 id=100100003/100100004 行。详见 research-system skill。

## 系统架构

```
UICreatureManager (BaseUIComponent)   生物管理界面
    │  RefreshSacrificeButton(creatureData): 解锁祭坛且未满级时显示;经验达标则正常/经验未达标则置灰但仍可点击(SetSacrificeButtonGray: 图标 IconLevelUpSacrifice 是纯白剪影(sprite=ui_research_3,颜色来自特效材质 Mat_UICreatureManager_Sacrifice 的渐变/色相),灰度化对纯白无效→改为置灰时移除特效材质(image.material=null,去掉渐变)并压深灰 colorSacrificeButtonGray(0.35);恢复时还原 dicSacrificeOriginMat 缓存的原始特效材质并转白)。未解锁祭坛/无选中生物/已满级(IsMaxLevel)则隐藏
    │  点击 ui_BtnLevelUpSacrifice_Button → OnClickForCreatureSacrifice()
    │      → 若 !CanUpLevel(): ToastHintText(textId 61009「经验值未达到100%」, state0) 拦截返回
    │      → new CreatureSacrificeBean{ targetCreature = 选中生物 }
    │      → GameHandler.Instance.StartCreatureSacrifice(bean)
    ▼
GameHandler.StartCreatureSacrifice(CreatureSacrificeBean)   // GameHandler.cs
    │  if (manager.gameLogic == null || manager.gameLogic is not CreatureSacrificeLogic)
    │      manager.gameLogic = new CreatureSacrificeLogic();   // 必须带类型判定: 献祭 EndGame 回基地后可能被终焉议会触发覆盖成 DoomCouncilLogic,只判 ==null 会让 as 转型得到 null 而 NRE
    │  (manager.gameLogic as CreatureSacrificeLogic).creatureSacrificeData = bean; → PreGame()
    ▼
CreatureSacrificeLogic (BaseGameLogic)   献祭玩法逻辑
    │  PreGame → 注册事件 / InitSceneData(祭坛粒子+摄像机+目标生物模型) / StartGame
    │  StartGame → OpenUIAndCloseOther<UICreatureSacrifice>()
    │  EventForSelectCreature(选祭品) → GetFodderPositions 整圈平均分布(360°/count)并以祭坛正前方(-90°)居中摆放祭品模型(数量变化时整圈重新居中旋转)
    │  StartSacrifice(): ① CreatureUtil.GetSacrificeSuccessRate 算成功率 → ② 掷骰 isSuccess
    │                    → ③ 失败立即 SettleSacrificeData(false) 结算并落盘(防玩家中途退出规避祭品消耗/保底重刷)
    │                    → ④ 播放生物/粒子/摄像机动画 → ⑤ 动画结束回调 OnSacrificeAnimEnd(isSuccess)
    │  SettleSacrificeData(isSuccess): 仅数据结算(不做界面跳转/提示),返回加点数(失败=0)
    │      - 祭品: RemoveAllEquipToBackpack() 退装备 → RemoveBackpackCreature() 移除
    │      - 成功: attributePoint = targetCreature.UpLevelForSacrifice()(返回加点数,不再自动加属性) + 清空 sacrificePityRate
    │      - 失败: sacrificePityRate += GetUnlockSacrificeFailPityAddRate()(研究等级×5%,未解锁不累积)
    │      - 末尾落盘策略: 仅「失败」立即 GameDataHandler.SaveUserData();「成功」此处不落盘(只改内存),延后到加点确认后的 SaveAndEndGame,防玩家在加点界面强退时祭品已扣、加点未存造成亏损(测试模拟模式的不落盘由 GameDataManager.isTestSimulation 在存档层统一拦截,此处不再判断)
    │  OnSacrificeAnimEnd(isSuccess): 动画结束后的表现与收尾
    │      - 成功: SettleSacrificeData(true)(此时才升级,但不落盘) + 触发 Success; attributePoint>0→OpenAddAttributeUI(弹手动加点,确认后才落盘) / 无点数→SaveAndEndGame(落盘)
    │      - 失败: 数据已在动画前结算落盘,仅触发 Fail + EndGame() 返回 UICreatureManager
    ▼
UICreatureAddAttribute (BaseUIComponent)   升级加点界面(成功后弹出)
    │  SetData(targetCreature, totalPoint, onConfirm); 4 个 UIViewCreatureAddAttributeItem(HP/DR/ATK/ASPD) 左减右加
    │  单点增量 CreatureUtil.GetAttributePointAddValue(HP/DR +10, ATK/ASPD +1); 实时作用属性并 RefreshCard
    │  注: Item.RefreshNum 仅显示已分配「点数」(allocatedCount, 如 +1)，与单点实际增量解耦，各属性步进器统一显示点数
    │  ui_LimmitText 显示「剩余点数:{0}」(多语言 textId 61005, string.Format 填入 remainPoint); 剩余必须全部分配完(无 exit 退出按钮)
    │  点 ui_BtnConfirm 确认 → OnClickForConfirm(): 剩余>0 则 ToastHintText(textId 61004「请分配完所有属性点」)拦截;
    │      剩余=0 则弹 ShowDialogNormal 二次确认(content=textId 61006), 确认弹窗提交后才 onConfirm=SaveAndEndGame
    ▼
UICreatureSacrifice (BaseUIComponent)   献祭选择界面
    │  列表排除目标生物本身 + 已编入阵容的生物; 选择上限 = userUnlock.GetUnlockSacrificeMax()(基础5 + 研究等级)
    │  SetSuccessRate(GetCurrentSuccessRate()) 实时显示真实成功率
    │  点击开始 → 设置 creatureSacrificeData.fodderCreatures → Logic.StartSacrifice()
```

## 成功率公式（核心）

公式实现在 **`CreatureUtil.cs`**（`Assets/Scripts/Utils/CreatureUtil.cs`），UI 显示与 Logic 掷骰共用，避免重复。

### 单个祭品成功率
- **同 id 基础**：祭品 `creatureId == 目标 creatureId` 时 `baseSingleRate = 1 / sacrifice_num`（`sacrifice_num` 来自下一级 `LevelInfo` 配置，默认 5 → 单祭品 20%）
- **生物 id 不同**：单个成功率 = `differentIdRate`（来自研究 `UnlockEnum.SacrificeDifferentIdRate`，每级 5%，**未解锁恒为 0**）。⚠️ 旧的「不同id ×1/10」逻辑已删除
- **等级差修正（替代旧的稀有度判定，已不再判定稀有度）**：修正系数 = `Mathf.Pow(2, 祭品.level - 目标.level)`，即祭品每**高**目标 1 级 `×2`（高 2 级 `×4`…），每**低** 1 级 `×0.5`（低 2 级 `×0.25`…），**同级不变**；同 id 基础率与不同 id 研究率都叠加。⚠️ 目标等级取目标生物**当前** `level`（非下一级）

### 总成功率
- `祭品总成功率 = Σ 每个祭品成功率`
- **最终成功率 = Clamp01(sacrificePityRate 保底 + 祭品总成功率)**，封顶 100%
- 相同 id 同等级时，祭品数量达到 `sacrifice_num` 即累加到 100%（高等级祭品单只即可超 100%，统一截顶）

### 公式举例（以 sacrifice_num = 5 为例，同 id base = 20%；不同 id 研究 N 级 → 5%×N）
| 场景 | 单祭品成功率 |
|------|------------|
| 相同 id、相同等级 | 20%（数量达 5 个即 100%） |
| 不同 id（研究未解锁） | 0% |
| 不同 id（研究 3 级=15%） | 15% |
| 同 id、祭品低目标 1 级 | 20% × 0.5 = 10% |
| 同 id、祭品低目标 2 级 | 20% × 0.25 = 5% |
| 同 id、祭品高目标 1 级 | 20% × 2 = 40% |
| 同 id、祭品高目标 2 级 | 20% × 4 = 80% |

### 两个公开方法
- `CreatureUtil.GetSacrificeFoddersRate(target, listFodder, sacrificeNum, differentIdRate)` —— 仅祭品部分，**不含保底、不截顶**；`differentIdRate` 为单个不同 id 祭品成功率(由调用方读研究等级传入)
- `CreatureUtil.GetSacrificeSuccessRate(target, listFodder)` —— 内部取 `target.GetNextLevelInfo().sacrifice_num`、读 `GetUnlockSacrificeDifferentIdRate()` 得不同id率，叠加 `target.sacrificePityRate` 保底，`Clamp01` 截顶；**UI 与 Logic 都调用这个**

## 保底机制 (Pity)

字段：`CreatureBean.sacrificePityRate`（持久化，写在 `CreatureBeanPartial.cs`）。保底增量由研究 `UnlockEnum.SacrificePityRate` 决定（旧的 `rate*0.5` 逻辑已删除）。

- 献祭**失败**：`sacrificePityRate = Clamp01(sacrificePityRate + GetUnlockSacrificeFailPityAddRate())`，每级研究 +5%，**未解锁(0级)则失败不累积保底**
- 下一次献祭：最终成功率 = `sacrificePityRate(保底) + 新祭品总成功率`，再 `Clamp01`
- 献祭**成功**：`sacrificePityRate = 0` 清零
- 保底随存档持久（`CreatureBean` 通过 Newtonsoft 序列化），`ClearTempData()` 会重置为 0

## 升级逻辑

实现于 `CreatureBeanPartial.cs`（**禁止改自动生成的 `CreatureBean.cs` 字段定义，扩展写 Partial**；但 `level`/`levelExp` 等数据字段位于 `CreatureBean.cs`，由项目惯例维护）：

| 方法 | 作用 |
|------|------|
| `GetNextLevelInfo()` | `LevelInfoCfg.GetItemData(level + 1)`，满级返回 null |
| `IsMaxLevel()` | 无下一级配置即满级（满级时**隐藏**升级按钮） |
| `CanUpLevel()` | 未满级 且 `levelExp >= 下一级 level_exp`（未满级下决定升级按钮是否置灰/能否发起献祭；满级另由 `IsMaxLevel()` 隐藏按钮） |
| `UpLevelForSacrifice()` | 经验清 0(不保留溢出余量) → `level++` → **返回本次可分配加点数**(`LevelInfo.attribute_point`，当前全等级配置为 5，`<=0` 兜底1)；不再自动加属性 |

> **升级属性成长（手动加点）**：升级**不再自动加属性**。`UpLevelForSacrifice()` 升级后返回下一级 `LevelInfo.attribute_point`(当前全等级配置为 5) 个可分配点数，由玩家在 `UICreatureAddAttribute` 界面手动分配到 HP/护甲/攻击/攻速。单点增量见 `CreatureUtil.GetAttributePointAddValue`（HP/DR 每点 +10、ATK/ASPD 每点 +1），写入 `creatureAttribute.dicAttributeLevelUp`。要调整每级点数改 Excel `attribute_point` 列；要调单点增量改 `GetAttributePointAddValue`。

### 属性体系
- `CreatureAttributeBean`（`Assets/Scripts/Bean/Game/CreatureAttributeBean.cs`）：`dicAttributeCreate`(创建随机) + `dicAttributeLevelUp`(升级加点)，两个字典均为 **public** 以保证 Newtonsoft 存档（私有字段默认不序列化）
- `AddAttribute(dic, type, addNum)` 支持**正负增量**(加点/减点共用)，clamp 到 ≥0；**已修复**首次加某属性时把第一次增量误置为0导致丢失的 bug
- `CreatureBean.GetAttribute()` 最终属性 = 基础(creatureInfo/npcInfo) + `creatureAttribute`(创建+升级) + 装备 + BUFF；**注意 `level` 本身不直接参与属性计算**，升级收益全靠 `AddAttributeForLevelUp` 写入

## 祭品处理

无论成功失败，`SettleSacrificeData` 都消耗祭品：
1. `fodder.RemoveAllEquipToBackpack()` —— 祭品身上装备**退回背包**（需求：成功/失败都退）
2. `userData.RemoveBackpackCreature(fodder)` —— 从背包(及阵容)移除祭品

祭品列表过滤(`UICreatureSacrifice.InitCreaturekData`)：① 目标生物本身排除(`creatureData == targetCreature` 按引用跳过)；② **已编入阵容的生物排除**(`userData.GetLinupIndex(creatureUUId) > 0` 跳过)。两项均在构建 `listCreatureData` 时做数据级过滤，故**默认所有排序下阵容魔物都不出现在祭品列表**。

## 等级上限

- `LevelInfo` 配置共 10 级 → 最高等级 10。满级时 `GetNextLevelInfo()` 返回 null。
- 满级后：升级按钮**隐藏**（`IsMaxLevel()` 为 true）；征服关卡不再累加经验（`GameFightLogicConquer` 中 `IsMaxLevel()` 跳过）。

## 等级配置 (LevelInfo)

| 字段 | 含义 | 备注 |
|------|------|------|
| `id` | 等级（=要升到的目标等级，1~10） | `GetItemData(level+1)` 取下一级 |
| `level_exp` | 升到该等级所需经验（字符串，`long.Parse`） | 单级增量；升级成功后 `levelExp` 清 0，不保留溢出余量 |
| `sacrifice_num` | 该等级所需祭品基础数量（决定 `baseSingleRate = 1/sacrifice_num`） | 默认 5 |
| `attribute_point` | 升级到该等级获得的可分配属性加点数（玩家在 `UICreatureAddAttribute` 手动分配） | 当前全等级配置为 5，`<=0` 时代码回退为 1 |
| `CMP_rate` | 魔力召唤增加倍率 | 召唤消耗随等级提升 |
| `level_color` | 等级字体颜色（十六进制，如 `#9CE07A`） | 1~10 级 10 种渐进色；0 级及无配置在代码回退白色 |

- **Excel 源表**：`Assets/Data/Excel/excel_level_info[等级信息].xlsx`（工作表 `LevelInfo`，数据从第 4 行起，前 3 行为字段名/类型/注释）
- **派生 JSON**：`Assets/Resources/JsonText/LevelInfo.txt`
- **配置 Bean**：`LevelInfoBean`（`LevelInfoBean.cs` 自动生成，被钩子保护禁止直接改；扩展/临时字段写 `LevelInfoBeanPartial.cs`）；访问类 `LevelInfoCfg : BaseCfg<long, LevelInfoBean>`，`fileName = "LevelInfo"`
- **等级颜色取色**：`LevelInfoCfg.GetLevelColor(level)`（`LevelInfoBeanPartial.cs`）—— 0 级及以下/无配置/解析失败回退 `Color.white`，1~10 级取 `level_color`。`UIViewCreatureCardDetails.SetLevelData` 用它给 `ui_LevelText` 着色。

> ⚠️ **`sacrifice_num` / `attribute_point` / `CMP_rate` / `level_color` 字段归属**：它们是 Excel 列，正确做法是在 Excel 加列后用 `ExcelEditorWindow` **重新生成 `LevelInfoBean.cs`**（生成器会写入该字段）。若因无法运行生成器而临时把字段放在 `LevelInfoBeanPartial.cs`，重新生成后必须删除 Partial 里的临时字段，避免与生成文件重复定义。**`attribute_point`/`CMP_rate` 已在生成的 `LevelInfoBean.cs` 中；当前 `level_color` 临时放在 `LevelInfoBeanPartial.cs`（Excel/JSON 已同步），待在 Unity 重新生成 Bean 后删除该临时字段。**

## 事件常量 (EventsInfo.cs · #region 生物献祭)

| 事件 | 触发 | 处理 |
|------|------|------|
| `CreatureSacrifice_SelectCreature` | UICreatureSacrifice 选择祭品变化 | Logic.EventForSelectCreature（摆放祭品模型） |
| `CreatureSacrifice_SacrificeSuccess` | OnSacrificeAnimEnd 成功 | Logic.EventForSacrificeSuccess（成功 Toast，textId 61007，state=1） |
| `CreatureSacrifice_SacrificeFail` | OnSacrificeAnimEnd 失败 | Logic.EventForSacrificeFail（失败 Toast，textId 61008，state=0） |

> ⚠️ **`UIHandler.ToastHintText(content, state)` 的 state 约定**：`0` = 失败（红色 `ui_other_3` 图标），`1` = 成功（绿色 `ui_other_6` 图标），默认 `0`。表达**成功/正向**反馈必须传 `1`，表达**失败/负向**反馈必须传 `0`，别传错图标。

## UI 结构

```
UICreatureManager (BaseUIComponent)   生物管理
    ├── ui_UIViewCreatureCardList            // 背包生物列表
    ├── ui_UIViewCreatureCardEquipDetails    // 选中生物详情+装备
    ├── ui_BtnLevelUpSacrifice_Button        // 献祭升级按钮(解锁祭坛且未满级时显示;经验未达标时置灰仍可点击→toast 61009;满级隐藏)
    ├── ui_BtnLevelUpSacrifice_PopupButtonCommonView  // 按钮气泡说明(textId 60000)
    └── ui_UIViewCreatureCardList 内每张卡片的 ui_SacrificeEffect  // 卡片上的"可献祭升级"高亮特效
                                              // 由 UIViewCreatureCardItem.SetSacrificeEffect 控制,
                                              // 仅 CardUseStateEnum.CreatureManager && 解锁祭坛 && CanUpLevel() 时显示
                                              // (判定与 RefreshSacrificeButton 一致, 材质 Mat_UIViewCreatureCardItem_Sacrifice.mat)

UICreatureSacrifice (BaseUIComponent)   献祭选择
    ├── ui_UIViewCreatureCardList            // 可选祭品列表(排除目标)
    ├── ui_UIViewCreatureCardDetails         // 目标生物展示
    ├── ui_LimmitText                        // 已选/上限(sacrificeMax)
    ├── ui_SuccessRateProgress / ui_SuccessRateText  // 实时成功率(进度条按成功率分5段变色)
    └── ui_BtnStart / ui_ViewExit

UICreatureAddAttribute (BaseUIComponent)   升级加点(成功后弹出)
    ├── ui_UIViewCreatureCardDetails              // 目标生物详情(实时刷新属性)
    ├── ui_UIViewCreatureAddAttributeItem_HP/_DR/_ATK/_ASPD  // 四个属性加点项
    │     └── UIViewCreatureAddAttributeItem(BaseUIView): ui_LeftButton(-1)/ui_RightButton(+1)/ui_Num(本次增加值)/ui_Icon(属性图标,预制体内配置)
    ├── ui_LimmitText                             // 剩余加点数量
    └── ui_BtnConfirm                             // 确认按钮(无 exit 退出按钮);剩余>0 ToastHint 拦截,剩余=0 弹二次确认弹窗(textId 61006)确认后保存
```

- 选择上限：`UserUnlockBean.GetUnlockSacrificeMax()` = `UserLimmitBean.sacrificeMax`（基础默认 5）+ 「增加祭品数量」研究等级（`UnlockEnum.SacrificeNum = 100100002`，研究 `level_max=10`，满级即 5+10=15）。`UICreatureSacrifice` 的 `RefreshUI`/`EventForCardClickSelect` 两处上限判定都走此方法，不要再直接读 `limmitData.sacrificeMax`
- 成功率显示：`SetSuccessRate(GetCurrentSuccessRate())`，`GetCurrentSuccessRate` 调 `CreatureUtil.GetSacrificeSuccessRate`；进度条颜色按成功率分5段（`GetSuccessRateColor` 现仅转发 `ColorUtil.GetProgressColor`：0-20%红`#C0392B`、20-40%橙`#E67E22`、40-60%黄`#F1C40F`、60-80%浅绿`#2ECC71`、80-100%蓝`#3498DB`，配色单一真实源在 ColorUtil，与孵化缸进阶BUFF概率共用），随 `DOColor`/`DOFillAmount` 0.5s 同步渐变
- 升级经验条：`UIViewCreatureCardDetails.SetLevelData(level, levelExp)`，用 `LevelInfoCfg.GetItemData(level+1).level_exp` 算百分比

## 接入 / 修改流程

### 调成功率公式
改 `CreatureUtil.GetSacrificeFoddersRate` / `GetSacrificeSuccessRate`（同id基础率、不同id研究率应用方式、等级差修正 `2^(祭品level-目标level)`、保底叠加方式）。UI 与 Logic 自动同步，无需两处改。

### 调升级加点（点数 / 单点增量 / UI）
- **每级获得点数**：改 Excel `excel_level_info` 的 `attribute_point` 列（唯一真实源，当前全等级配置为 5），再 `ExcelEditorWindow` 导出 JSON。`UpLevelForSacrifice()` 读该列返回点数（`<=0` 回退 1）。
- **单点增量**（每点加多少 HP/护甲/攻击/攻速）：改 `CreatureUtil.GetAttributePointAddValue(type)`。
- **加点界面**：`UICreatureAddAttribute`（`Assets/Scripts/Component/UI/Game/CreatureAddAttribute/`）。`SetData(creature, totalPoint, onConfirm)`；`OnItemChangeForAttribute` 校验剩余点数后增减并实时 `RefreshCard`；`RefreshLimmit` 显示「剩余点数:{0}」(textId 61005)；**已去掉 exit 退出按钮，改由 `ui_BtnConfirm` 确认**：`OnClickForConfirm` 剩余>0 时 ToastHintText(textId 61004)拦截，剩余=0 时 `ShowDialogNormal` 弹二次确认弹窗(content=textId 61006「确认按当前方案分配属性点吗？…」)，确认弹窗提交后才触发 `onConfirm`(=`SaveAndEndGame`) 保存。
- **可加点的属性种类**：在预制体里增删 `UIViewCreatureAddAttributeItem` 项并在 `InitItems()` 里对应 `SetData(attributeType, ...)`。

### 调祭品基础数量 / 等级经验
改 Excel `excel_level_info` 的 `sacrifice_num` / `level_exp` 列（唯一真实源），再 `ExcelEditorWindow` 导出 JSON。新增等级行按 id 升序插入。

### 调失败惩罚 / 保底
保底增量由研究 `UnlockEnum.SacrificePityRate` 驱动：改 `UserUnlockBean.GetUnlockSacrificeFailPityAddRate()`(每级系数)或 `CreatureSacrificeLogic.SettleSacrificeData`(累积/清零方式、是否退经验、祭品是否返还) + `CreatureBean.sacrificePityRate` 处理。

> ⚠️ **落盘时机(成功/失败分两条分支)**：
> - **失败**：`StartSacrifice` 掷骰判定失败后，会在动画播放前就调用 `SettleSacrificeData(false)` 结算并立即 `SaveUserData()` 落盘，防止玩家在动画期间强退、规避祭品消耗与保底累积后重新献祭。
> - **成功**：结算 (`SettleSacrificeData(true)`) 延后到动画结束的 `OnSacrificeAnimEnd` 里执行，且**成功时 `SettleSacrificeData` 内部不落盘**(只改内存：扣祭品+升级)。真正落盘统一延后到玩家在加点界面 `UICreatureAddAttribute` 确认后回调的 `SaveAndEndGame`(无加点数时直接 `SaveAndEndGame`)。这样玩家在加点界面强退则本次成功结算全部不写盘、祭品自然恢复，**避免祭品已扣、加点未存的亏损**。
> 改结算时机/落盘逻辑须同时顾及这两条分支：失败落盘在 `SettleSacrificeData` 内(`!isSuccess` 条件)，成功落盘在 `SaveAndEndGame`。

### 调不同 id 成功率
不同 id 祭品成功率由研究 `UnlockEnum.SacrificeDifferentIdRate` 驱动：改 `UserUnlockBean.GetUnlockSacrificeDifferentIdRate()`(每级系数)；公式如何应用(per-fodder/等级差修正叠加)改 `CreatureUtil.GetSacrificeFoddersRate`。

### 调祭坛动画 / 摄像机
改 `CreatureSacrificeLogic` 的 `AnimForCreatureObjSacrfice` / `AnimForSacrficeEffect` / `AnimForSacrficeCamera` / `SetAltarEffect`。祭坛对象 `scenePrefab.objBuildingAltar`，粒子 `VFX_LightFire*` / `MagicArray`。

### 调祭品站位
改 `CreatureSacrificeLogic.GetFodderPositions(count, centerPosition)`：沿整圈平均分布(相邻间隔 `360°/count`)，再以祭坛正前方 `centerAngle = -90°` 对整圈做居中偏移(`startAngle = centerAngle - step*(count-1)/2`，使角度中心落在 -90°)。`EventForSelectCreature` 每次选择变化都重算全部位置并经 `AnimForCreatureShow` 平滑移动，**数量变化时整圈重新居中旋转**(不会把第一个祭品固定钉在最下方)。`-90°` 经 `VectorUtil.GetCirclePosition` 映射为祭坛 -Z(屏幕最下方/朝镜头)。

> **基地控制屏蔽**：`UICreatureSacrifice.OpenUI` 与其它基地子界面一致调用 `GameControlHandler.Instance.SetBaseControl(false)`，确保献祭界面及随后 `StartSacrifice` 关闭全部 UI 播放动画期间都**无法控制角色移动**(此前仅依赖前一界面 UICreatureManager 关闭控制，测试入口直接进献祭时会漏关)。

### 测试献祭升级（不落盘）
测试模式 `TestSceneTypeEnum.CreatureSacrifice`：读取某个真实存档的数据对其中一只生物直接发起献祭，可手动控制成功率或用存档真实数据，**结果不写回真实存档**。详见 `test-system` Skill「献祭升级测试」。

- **不落盘统一为全局机制**：`GameDataManager.isTestSimulation` 为 `true` 时 `SaveUserData` 一律 no-op（献祭/进阶测试共用，见 `test-system` Skill「测试模拟不落盘通用机制」）。`CreatureSacrificeBean` 只保留献祭专属测试字段 `useManualSuccessRate`(是否覆盖)、`manualSuccessRate`(手动成功率 0~1)——原 `isTestMode` 已删。
- `CreatureSacrificeLogic.StartSacrifice`：`GameDataHandler.Instance.manager.isTestSimulation && useManualSuccessRate` 时用手动成功率掷骰，否则走 `CreatureUtil.GetSacrificeSuccessRate` 公式。
- `CreatureSacrificeLogic.SettleSacrificeData` / `SaveAndEndGame`：直接调 `SaveUserData()`（测试模拟下由存档层自动跳过），升级/祭品消耗只在内存生效。
- 入口 `LauncherTest.StartForCreatureSacrificeTest` 在 `SetUserData` 后置 `isTestSimulation=true`。
- 入口 `LauncherTest.StartForCreatureSacrificeTest(slot, uuid, useManualRate, manualRate)`：`UserDataService` 加载存档 → `SetUserData` 替换运行时数据 → 一次性 `World_EnterGameForBaseScene` 等基地就绪 → `GameHandler.StartCreatureSacrifice`。
- **目标生物须取自加载后的 `userData.GetUserBackpackCreatureData().listBackpackCreature` 同一引用**（UI 按引用排除目标）。

## 与其他系统的关系

- **生物系统**：`CreatureBean`(level/levelExp/rarity/creatureAttribute)、`CreatureInfoCfg`(基础属性)、`CreatureHandler.SetCreatureData`+`SpineHandler.PlayAnim`(模型展示)
- **战斗/征服系统**：`GameFightLogicConquer.AddLevelExpForLineupCreature` 发放经验（满级跳过）
- **道具系统**：`CreatureBean.RemoveAllEquipToBackpack()` 祭品装备退回 `userData.GetUserBackpackItemsData().listBackpackItems`
- **研究/解锁系统**：`UnlockEnum.Altar` 祭坛解锁；`ScenePrefabForBase` 据此显隐祭坛；`UnlockEnum.SacrificeNum` 研究等级经 `UserUnlockBean.GetUnlockSacrificeMax()` 提升祭品选择上限；`UnlockEnum.SacrificePityRate`(100100003)/`SacrificeDifferentIdRate`(100100004) 研究经 `GetUnlockSacrificeFailPityAddRate()`/`GetUnlockSacrificeDifferentIdRate()` 决定失败保底增量与不同id祭品成功率
- **存档系统**：`UserDataBean.GetUserBackpackCreatureData().listBackpackCreature` + `GameDataHandler.Instance.manager.SaveUserData()`
- **属性/BUFF 系统**：升级加点写 `creatureAttribute.dicAttributeLevelUp`，与稀有度 BUFF(`dicRarityBuff`)、装备属性在 `GetAttribute` 汇总

## 注意事项

- **双门槛**：经验门槛只控制能否"发起"献祭；真正升级靠祭品掷骰成功。两者不要混淆。
- **失败只扣祭品**：经验不退，保底累积；成功才扣经验、清保底。
- **目标生物与祭品引用一致**：UI 选中的祭品、目标生物与 `userData.GetUserBackpackCreatureData().listBackpackCreature` 是**同一引用**，原地升级/移除即生效。
- **掷骰时机**：`StartSacrifice` 动画播放**前**就定好 `isSuccess`。失败在动画前即 `SettleSacrificeData(false)` 落实**并落盘**；成功在动画结束回调 `OnSacrificeAnimEnd` 里 `SettleSacrificeData(true)` 落实但**不落盘**(只改内存)，落盘延后到加点界面确认后的 `SaveAndEndGame`，避免动画中途数据被改、也避免加点未完成就写盘。
- **属性序列化**：`CreatureAttributeBean` 两个字典必须 public（或 `[JsonProperty]`），否则升级加的属性存盘丢失。
- **配置改 Excel**：`excel_level_info` 是唯一真实源，只改 JSON 会被导出覆盖。
- **Bean 改写规则**：`LevelInfoBean.cs` 自动生成，扩展写 Partial；`CreatureSacrificeBean`/`CreatureAttributeBean` 是手写运行时类可直接改。
- **Toast 多语言**：成功/失败提示当前为硬编码中文（留有 `TODO`），后续接 `TextHandler.GetTextById`。
- **返回界面索引钳制**：献祭后祭品从背包移除导致生物数量减少，`UICreatureManager.selectCreatureIndex` 可能越界。`InitCreaturekData` 已在 `SetData` 后把它钳到 `[0, count-1]`（数量为 0 时置 0 且不再 `GetItemData`），避免重开管理界面时 `GetItemData` 越界报「超过下标」错误。注意它仍可能指向位移后的另一只生物（仅保证不越界，不保证选中同一只）。

## 参考文件

| 模块 | 路径 |
|------|------|
| 献祭逻辑 | `Assets/Scripts/Game/Logic/CreatureSacrificeLogic.cs` |
| 启动入口 | `Assets/Scripts/Component/Handler/GameHandler.cs`（`StartCreatureSacrifice`） |
| 献祭数据 | `Assets/Scripts/Bean/Game/CreatureSacrificeBean.cs` |
| 生物数据/升级 | `Assets/Scripts/Bean/Game/CreatureBean.cs` · `CreatureBeanPartial.cs`（`UpLevelForSacrifice`/`CanUpLevel`/`IsMaxLevel`/`sacrificePityRate`） |
| 生物属性 | `Assets/Scripts/Bean/Game/CreatureAttributeBean.cs` |
| 成功率公式 / 单点增量 | `Assets/Scripts/Utils/CreatureUtil.cs`（`GetSacrificeSuccessRate`/`GetSacrificeFoddersRate`/`GetAttributePointAddValue`） |
| 献祭 UI | `Assets/Scripts/Component/UI/Game/CreatureSacrifice/` |
| 升级加点 UI | `Assets/Scripts/Component/UI/Game/CreatureAddAttribute/`（`UICreatureAddAttribute` + `UIViewCreatureAddAttributeItem`） |
| 升级按钮 UI | `Assets/Scripts/Component/UI/Game/CreatureManager/UICreatureManager.cs`（`RefreshSacrificeButton`） |
| 经验发放 | `Assets/Scripts/Game/Logic/GameFightLogicConquer.cs`（`AddLevelExpForLineupCreature`） |
| 等级配置 Bean | `Assets/Scripts/Bean/MVC/Game/LevelInfoBean.cs` · `LevelInfoBeanPartial.cs` |
| 等级配置 JSON | `Assets/Resources/JsonText/LevelInfo.txt` |
| 等级 Excel 源表 | `Assets/Data/Excel/excel_level_info[等级信息].xlsx` |
| 祭坛解锁 | `Assets/Scripts/Enums/GameStateEnum.cs`（`UnlockEnum.Altar = 100100001`） |
| 事件常量 | `Assets/Scripts/Common/EventsInfo.cs`（#region 生物献祭） |
