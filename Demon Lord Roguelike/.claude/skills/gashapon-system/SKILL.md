---
name: gashapon-system
description: Demon Lord Roguelike 游戏的孕育系统（又叫扭蛋系统 / Gashapon）开发指南。使用此SKILL当需要创建或修改孕育/扭蛋逻辑、扭蛋机商店、抽蛋随机生物、破蛋动画、稀有度随机、扭蛋配置表、孕育解锁条件、孕育UI等，包括 GashaponMachineLogic 扭蛋逻辑、GashaponMachineBean/GashaponItemBean 运行时数据、StoreGashaponMachineInfoBean 配置、UIGashaponMachine 扭蛋商店、UIGashaponBreak 破蛋界面、GashaponMachine_* 事件常量、UnlockEnum.GashaponMachine 解锁等。
watched_files:
  - Assets/Scripts/Game/Logic/GashaponMachineLogic.cs
  - Assets/Scripts/Bean/Game/GashaponMachineBean.cs
  - Assets/Scripts/Bean/Game/GashaponItemBean.cs
  - Assets/Scripts/Bean/MVC/Game/StoreGashaponMachineInfoBean.cs
  - Assets/Scripts/Bean/MVC/Game/StoreGashaponMachineInfoBeanPartial.cs
  - Assets/Scripts/Component/UI/Game/GashaponMachine/
  - Assets/Scripts/Component/UI/Game/GashaponBreak/
  - Assets/Scripts/Component/UI/Common/Store/UIViewStoreItemPartialGashaponMatchine.cs
  - Assets/Resources/JsonText/StoreGashaponMachineInfo.txt
---

# 孕育系统（扭蛋系统 / Gashapon）开发指南

> **命名说明**：游戏内功能名为「孕育」，代码与配置中一律用 **Gashapon / 扭蛋机** 命名。本文「孕育」「扭蛋」指同一系统。

## 核心概念

玩家在基地的孕育界面花费**魔晶 (crystal)** 选择一种生物族群（人类/骷髅/史莱姆/魅魔/牛头人/哥布林/兽人）并购买 1/5/10 个「蛋」，逐个点击破壳，破壳后随机获得一只该族群生物（随机皮肤 + 随机属性 + 随机稀有度），生物直接入账到玩家背包并立即落盘。可消耗魔晶**重置**重新抽，或**结束**返回基地。

### 解锁前置
孕育系统需要解锁：`UnlockEnum.GashaponMachine = 100400000`。入口按钮在 `UIBaseCore`（`ui_ViewBaseCoreItemFunction_Gashapon`），未解锁时隐藏；点击 `OnClickForGashapon` → `OpenUIAndCloseOther<UIGashaponMachine>()`。各扭蛋商品本身还有独立的 `pre_unlock_ids` 前置解锁。

## 系统架构

```
UIGashaponMachine (BaseUIComponent)  扭蛋商店列表
    │  InitStoreListData(): 遍历配置, 按 pre_unlock_ids 过滤已解锁项
    │  点击购买 → CheckHasCrystal(扣费) → StartGashaponMachine()
    │            → 解析 creature_ids / 收集随机生物数据
    │            → GameHandler.Instance.StartGashaponMachine(GashaponMachineBean)
    ▼
GameHandler.StartGashaponMachine(GashaponMachineBean)   // GameHandler.cs:58
    │  manager.gameLogic = new GashaponMachineLogic(); → PreGame()
    ▼
GashaponMachineLogic (BaseGameLogic)   扭蛋玩法逻辑
    │  PreGame → InitSceneData / InitGashaponMachineData(生成所有蛋并入账)
    │  StartGame → ProcessForShowAllEgg / ProcessForFocusEgg(聚焦第一个蛋)
    │  EventForEggBreak → AnimForEggBreak(破壳+显示生物) → UIGashaponBreak.InitForBreak
    │  EventForNextEgg / EventForReset(再扣费重抽) / EventForEnd
    ▼
UIGashaponBreak (BaseUIComponent)   破蛋交互界面
    │  状态机: Show(待破) → Break(显示生物卡) → End(总览+重置/结束)
    │  按钮通过 GashaponMachine_* 事件回传给 Logic
    ▼
GashaponItemBean (单个蛋)
    │  creatureData: CreatureBean   isBreak: bool
    │  构造时 RandomSkill / RandomAttribute / RandomRarity
    │
userData.AddBackpackCreature(creatureData) + SaveUserData()   // 入账并落盘
```

## 数据类型

### 运行时数据（`Assets/Scripts/Bean/Game/`，非自动生成可直接改）
- **GashaponMachineBean** —— 本次抽蛋上下文
  - `gashaponNum` 蛋数量(=buy_num)、`payCrystal` 单次魔晶价(重置时复用)、`listCreatureRandomData` 可抽生物列表
  - `GashaponMachineCreatureStruct { creatureId; randomCreatureMode }` 单个可抽生物 + 随机皮肤映射
- **GashaponItemBean** —— 单个蛋
  - `creatureData: CreatureBean`、`isBreak: bool`
  - 构造 `GashaponItemBean(creatureId, gashaponMachineCreature)` 内部依次：`RandomSkill()` 随机皮肤 → `RandomAttribute()` 随机属性 → `RandomRarity()` 随机稀有度
  - **随机属性共用逻辑**：`RandomAttribute()` 委托 `CreatureBean.RandomAttributeForCreate(userData)`（位于 `CreatureBeanPartial.cs`，点数取 `UserLimmitBean.gashaponRandomAttributeNum`，基础值默认5，`<=0` 不加点）
  - **初始魔物固定属性（不再随机）**：新建存档赠送的 3 个初始魔物（`UIMainCreate.OnClickForCreate`）改用 `CreatureBean.FixedAttributeForCreate(userData, attributeType)` —— 点数预算同样取 `UserLimmitBean.gashaponRandomAttributeNum`，但全部固定堆到单一属性：`NpcId1→HP`、`NpcId2→DR`、`NpcId3→ASPD`（底层 `CreatureAttributeBean.AddFixedAttributeForCreate`，单点增量复用 `CreatureUtil.GetAttributePointAddValue`：HP/DR 每点+10、ASPD 每点+1，故 5 点 = HP+50 / DR+50 / ASPD+5）；注意该场景存档尚未 `SetUserData`，需显式传入新建的 `UserDataBean`
  - **稀有度随机**：依次判定 UR→SSR→SR→R→N，每档由 `UnlockEnum.GashaponRarity*` / `GashaponRarity*Rate` 控制是否开放与成功率；命中后通过 `BuffTypeEnum.CreatureRarity*` 从对应 `buff_type`(11=R/12=SR/13=SSR) 的 BUFF 池随机抽 1 条叠加给生物（存入 `CreatureBean.dicRarityBuff`），高稀有度会叠加各低档各 1 条（如 SSR 给 R+SR+SSR 各 1）
  - **稀有度 BUFF 生成两级收口**：`RandomRarity()` 定好稀有度后调用 `CreatureBean.RandomRarityBuffForCreate()`（`CreatureBeanPartial.cs`）——按稀有度从 R 档逐级授予（高稀有度累积各低档各 1 条，存入 `dicRarityBuff`）；单档「按稀有度取对应 buff_type 池随机抽 1 条」再走 `BuffUtil.CreateRandomRarityBuff(rarityEnum)`（`Assets/Scripts/Utils/BuffUtil.cs`），与**魔物进阶（UICreatureVat）共用同一规则**（进阶另有素材 BUFF 加成走 `BuffUtil.CreateAscendRarityBuff`）。`RandomRarityBuffForCreate` 同时被**测试面板 `UITestBase.OnClickForAddTestCreature`** 复用，保证测试生物与孕育同口径。原 `GashaponItemBean.RandomRarityBuff(RarityEnum)` 已删除（详见 buff-system / utils-system skill）
  - **稀有度 BUFF 分档设计规则（buff_type 11/12/13，硬约束）**——每档效果性质必须归对档位（生成入口只按 buff_type 取池随机、不校验性质，靠本规则人工保证）：
    - **R(11)=纯属性 BUFF**：只做属性数值加/减益、常驻无触发。现池含 HP/DR/ATK 增益(+10~20%)、ASPD 攻速增益(+50~100%)、RCD 召唤CD减益、CMP 召唤魔力消耗减益(均 -25~50%，负 rate)；可扩展 MSPD/CRT/EVA（**无 HPRegeneration 生命回复属性**，实际枚举 index11=MPF 魔法回复、且游戏无被动回血刻）。类统一 `BuffEntityAttribute`。RCD/CMP 减益：RCD 走 `GetAttribute(RCD, true)`（含深渊馈赠按需叠加，原 GetRCD 已并入）、CMP 走 `GetAttribute(CMP)`（基础CMP×(1+等级/稀有度增加倍率)后再叠加BUFF；`GetAttributeInt(CMP)` 为 int 封装）。
    - **SR(12)=条件/周期被动触发 BUFF**：累计造成/受到伤害、击杀数、血量阈值或按周期触发后生效；类 `BuffEntityConditional*`(非死亡)/`Periodic*`/`Pecurrent`。
    - **SSR(13)=特殊类 BUFF**：死亡重生/死亡反击/死亡区域治疗/克隆增殖/生成改变水晶掉落等质变效果；类 `BuffEntityConditionalDead*`/`BuffEntityInstant*` 等。
    - 高稀有度累积低档：SSR 生物 = R+SR+SSR 各 1、SR 生物 = R+SR 各 1。新增稀有度 BUFF 需在 `excel_buff_info` + 多语言 `excel_language[BuffInfo]` 配表；R 属性类自动进池，SR/SSR 需配对应实体类。**完整分档表与设计自检见 buff-system SKILL「扭蛋/稀有度 BUFF 分档设计规则」（单一真实源）**

### 配置数据（`StoreGashaponMachineInfoBean`，自动生成 → 扩展写 Partial）
| 字段 | 含义 | 示例 |
|------|------|------|
| `id` | 配置 ID（编码规则见下） | 10001 |
| `creature_ids` | 可抽生物 ID 集合，逗号分隔、`-` 表区间 | `"1001-1004"` |
| `buy_num` | 购买/蛋数量 | 1 / 5 / 10 |
| `pay_crystal` | 消耗魔晶 | 20 / 100 / 200 |
| `icon_res` | 图标，支持 `图标名,图集类型` 跨图集（孕育图标在 Research 图集） | `"ui_research_36,Research"` |
| `pre_unlock_ids` | 解锁前置（格式见下） | `"300100000,300100001\|300100002\|..."` |
| `name` | 多语言文本 ID | 10001 |
| `name_language` | 运行时解析文本（`[JsonIgnore]`，自动取） | "人类x1" |
| `remark` | 备注 | "人类" |

- **BeanPartial**：`StoreGashaponMachineInfoBeanPartial.cs` —— `GetCreatureIds()` 用 `creature_ids.SplitForListLong(',', '-')` 把区间字符串解析为 `List<long>`（带缓存）
- **Cfg 访问类**：`StoreGashaponMachineInfoCfg : BaseCfg<long, ...>`，`fileName = "StoreGashaponMachineInfo"`；`GetItemData(id)` / `GetAllData()`

### ID 编码规则
- **扭蛋配置 ID** `X000N`：`X`=族群(1人类/2骷髅/3史莱姆/4魅魔/5牛头人/6哥布林/7兽人)，`N`=档位(1=x1, 2=x5, 3=x10)。例：`10001`=人类x1，`70003`=兽人x10
- **生物 ID 区间(creature_ids)**：人类 1001-1004、骷髅 2001-2004、史莱姆 3001-3004、魅魔 4001-4005、牛头人 5001-5004、哥布林 6001-6005、兽人 7001-7004
- **pre_unlock_ids 解锁表达式**：逗号 `,` 分隔的是 **AND** 条件组；竖线 `|` 分隔的是组内 **OR** 选项。例 `"A,B|C|D"` = `A 且 (B 或 C 或 D)`。判定走 `userUnlock.CheckIsUnlock(...)`

## 配置表（Excel 是唯一真实源）

- **Excel 源表**：`Assets/Data/Excel/excel_store_gashaponmachine_info[商店-扭蛋机].xlsx`（工作表 `StoreGashaponMachineInfo`，数据从第 4 行起，前 3 行为表头/类型/注释）
- **派生 JSON**：`Assets/Resources/JsonText/StoreGashaponMachineInfo.txt`（由 `ExcelEditorWindow` 导出，理论可再生，改配置必须落到 Excel）
- 共 21 条：7 族群 × 3 档位

> 修改配置数据务必改 Excel；仅改 JSON 会在下次导出时被覆盖丢失。新增行按 id 升序插入。

## 事件常量 (EventsInfo.cs · #region 扭蛋机)

| 事件 | 触发 | 处理 |
|------|------|------|
| `GashaponMachine_ClickBreak` | UIGashaponBreak 破壳按钮 | Logic.EventForEggBreak |
| `GashaponMachine_ClickNext` | 下一个 | Logic.EventForNextEgg |
| `GashaponMachine_ClickReset` | 重置（再扣魔晶） | Logic.EventForReset |
| `GashaponMachine_ClickEnd` | 结束返回基地 | Logic.EventForEnd |
| `GashaponMachine_ClickShowAll` | 跳过逐个破壳（需解锁 `GashaponShowAll`） | Logic.EventForShowAll 一次性显示全部（点击时先显式播一次破壳音效） |

> UIGashaponBreak 还监听 `Creature_Rename`：当前展示生物被改名时刷新卡牌。

## 解锁枚举 (UnlockEnum · GameStateEnum.cs)

```
GashaponMachine        = 100400000   // 孕育主入口
GashaponShowAll        = 100400001   // 跳过逐个破壳
GashaponRarityR        = 100401000   GashaponRarityRRate   = 100401001
GashaponRaritySR       = 100402000   GashaponRaritySRRate  = 100402001
GashaponRaritySSR      = 100403000   GashaponRaritySSRRate = 100403001
GashaponRarityUR       = 100404000   GashaponRarityURRate  = 100404001
```
稀有度档位由研究系统解锁，`GashaponItemBean.RandomRarity()` 据此决定概率。每档命中率 = **起始 `rarityBaseRate`(常量,当前 10%) + 对应 `*Rate` 概率研究等级(每级+1%)**；即解锁档位即有 10% 起始概率，再靠概率研究节点叠加。研究节点 `100401001`/`100402001`/`100403001`(R/SR/SSR 概率+1%) 的 `level_max` 均为 50，故每档最高 10%+50%=60%。

## 多语言

- 文件：`Assets/Resources/JsonText/Language_StoreGashaponmachineInfo_cn.txt` / `_en.txt`
- 文本 ID 段：`10001~10003`(人类) `20001~20003`(骷髅) … `70001~70003`(兽人)
- 取值：`TextHandler.Instance.GetTextById("StoreGashaponMachineInfo", name)`（即 `name_language`）
- 源表为 `excel_language[多语言_FrameWork].xlsx` 的 `StoreGashaponmachineInfo` 工作表

## UI 结构

```
UIGashaponMachine (BaseUIComponent)   孕育商店
    ├── ui_List (ScrollGridVertical)            // 已解锁扭蛋项列表
    │     └── tempCell = UIViewStoreItem        // 通用商店项, 扭蛋逻辑在 Partial
    ├── ui_ViewExit (Button)
    ├── ui_ViewBaseInfoContent (UIViewBaseInfoContent)
    └── ui_NullText (UITextLanguageView)

UIGashaponBreak (BaseUIComponent)     破蛋交互
    ├── ui_BtnBreak / ui_BtnNext / ui_BtnReset / ui_BtnEnd / ui_BtnShowAll
    ├── ui_UIViewGashaponBreakItemShow (UIViewGashaponBreakItemShow)  // 当前生物卡
    ├── ui_ViewCreatureCardDetails (UIViewCreatureCardDetails)
    └── ui_AllList (RectTransform)              // End 状态总览所有蛋
```

- **商店项复用**：`UIViewStoreItem` 是通用商店项，孕育专用逻辑写在 `UIViewStoreItemPartialGashaponMatchine.cs`（`SetData`→`SetName(name_language)`/`SetPrice(pay_crystal)`/`SetIcon(icon_res)`/`SetContentShow()`）。`SetIcon` 走 `IconHandler.SetUIIcon`，已支持 `图标名,图集类型` 跨图集加载
- **可抽生物概率弹窗**：`SetContentShow()` 给 `ui_ContentShow`（`PopupButtonCommonView`，悬浮触发 `PopupEnum.Text`）填入「每只可抽生物 + 各稀有度实际命中概率」文本，样式 `生物名:普通50% 稀有10%`，每个「稀有度名+百分比」整段用 TMP `<color>` 包裹（颜色取 `RarityInfo.ui_board_color` 的**主色**=逗号前首段，因该字段是「主色,暗色」渐变对）。概率来自 `GashaponItemBean.GetRarityProbabilityList()`：把顺序判定 UR→SSR→SR→R→N 换算成**真实命中概率**（普通 N=剩余补足，合计=1，仅已解锁档位+普通，顺序 普通→R→SR→SSR→UR）；与抽到哪只生物无关，所有生物共用同一份概率文本。**生物列表须与实际抽取一致**：跳过职业未解锁（`!userUnlock.CheckIsUnlock(creatureInfo.unlock_id)`）的生物——与 `UIGashaponMachine.StartGashaponMachine` 收集随机生物时同一过滤口径，否则会展示抽不到的生物
- **破蛋卡片**：`UIViewGashaponBreakItemShow.SetData(CreatureBean, CardUseStateEnum)`，`OnClickForRename` 打开重命名对话框

## 接入/修改流程

### 新增一个扭蛋族群或档位
1. **Excel**：`excel_store_gashaponmachine_info` 按 id 升序插入新行（creature_ids / buy_num / pay_crystal / icon_res / pre_unlock_ids / name）
2. **多语言**：`excel_language` 的 `StoreGashaponmachineInfo` 工作表加 name 文本（cn/en）
3. 在 Unity 运行 `ExcelEditorWindow` 导出，生成 `StoreGashaponMachineInfo.txt` 及对应 Language JSON
4. 如族群对应新生物，确保 `CreatureInfo` / `CreatureRandomInfo` 已配置（皮肤随机依赖 `creature_random_id`）
5. 如需新的研究解锁节点，配 `ResearchInfo` + `pre_unlock_ids`

### 改抽奖/稀有度逻辑
改 `GashaponItemBean`（RandomSkill/RandomAttribute/RandomRarity）与 `UnlockEnum.GashaponRarity*` 关联，**不要**改自动生成的 Bean。「按稀有度逐级授予稀有度 BUFF」已收口到 `CreatureBean.RandomRarityBuffForCreate()`（`RandomRarity` 定好稀有度后调用它，测试面板 `UITestBase` 也复用）；单档「按稀有度抽 1 条」再走 `BuffUtil.CreateRandomRarityBuff`，与魔物进阶共用。要改通用规则应改 `BuffUtil` / `RandomRarityBuffForCreate`，而非在 `GashaponItemBean` 内重新内联。
> 同一套 `GashaponRarity*`/`*Rate` 解锁门控还被**展示用**的 `GashaponItemBean.GetRarityProbabilityList()` 消费（孕育商店项概率弹窗）。改稀有度顺序/门控时，`RandomRarity`(实际抽取) 与 `GetRarityProbabilityList`(展示概率) **两处口径需同步**，否则弹窗显示与真实概率不符。

### 改破蛋流程/动画
改 `GashaponMachineLogic`（AnimForEggBreak / AnimForShowEgg / AnimForEggPunch / ProcessForFocusEgg）。蛋子物体名：`Egg_1`(壳) / `Renderer`(Spine)；破壳粒子走 `scenePrefab.effectEggBreak`（VFX，传 Color1/Color2）；破壳音效在 `AnimForEggBreak` 播 `AudioHandler.Instance.PlaySound(AudioEnum.sound_break_1)`；点击「跳过所有」(`EventForShowAll`) 会先显式再播一次 `sound_break_1`，因为随后逐帧连续破蛋会被 `AudioHandler` 的 0.1s 同音重复保护抑制（否则跳过时听不到破壳声）。**蛋出现（吐蛋）音效**：`AnimForShowEgg` 在蛋跳出（DOJump）时播 `AudioHandler.Instance.PlaySound(AudioEnum.sound_btn_31)`，每个蛋出现各播一次。

## 数据流与存档

- **货币**：魔晶 `UserDataBean.crystal`；扣费 `userData.CheckHasCrystal(num, isHint:true, isAddCrystal:true)`（购买在 UIGashaponMachine，重置在 Logic.EventForReset）
- **入账**：`userData.AddBackpackCreature(itemGashapon.creatureData)` → `GameDataHandler.Instance.manager.SaveUserData()` 立即落盘（生成蛋时即入账，破壳只是展示）
- **生物来源标记**：`CreatureTypeEnum.GashaponMachine = 1`

## 与其他系统的关系

- **生物系统**：`CreatureInfoCfg` / `CreatureRandomInfoCfg`（随机皮肤）/ `CreatureModelCfg`（Spine 模型尺寸）；`CreatureHandler.SetCreatureData` + `SpineHandler.PlayAnim` 展示
- **研究/解锁系统**：`UnlockEnum.GashaponMachine` 主入口 + `GashaponRarity*` 稀有度档位 + 每商品 `pre_unlock_ids` + 生物职业 `creatureInfo.unlock_id`
- **BUFF 系统**：稀有度 BUFF `BuffTypeEnum.CreatureRarity*`，存 `CreatureBean.dicRarityBuff`；逐级授予走 `CreatureBean.RandomRarityBuffForCreate()`，单档生成口径统一走 `BuffUtil.CreateRandomRarityBuff`（与魔物进阶共用，见 buff-system / utils-system skill）
- **商店 UI**：复用 `UIViewStoreItem`
- **存档系统**：`UserDataBean.AddBackpackCreature()` / `GetUserBackpackCreatureData().listBackpackCreature` + `GameDataManager.SaveUserData()`

## 注意事项

- **孕育 = 扭蛋**：代码/配置全部用 Gashapon 命名，UI 文案才叫「孕育」
- **生成即入账**：`InitGashaponMachineData` 一次性生成全部蛋并 `AddBackpackCreature`+落盘；破壳/下一个仅为展示，不再二次入账。重置 `EventForReset` 会**再次扣魔晶**重抽
- **Bean 改写规则**：`StoreGashaponMachineInfoBean.cs` 自动生成，扩展写 `StoreGashaponMachineInfoBeanPartial.cs`；`GashaponMachineBean`/`GashaponItemBean` 是手写运行时类可直接改
- **配置改 Excel**：`excel_store_gashaponmachine_info` 是唯一真实源，只改 JSON 会被导出覆盖
- **图标跨图集**：`icon_res` 用 `ui_xxx,图集类型`（如 `,Research`）可从非 UI 图集加载，解析在 `IconHandler.ParseIconName`

## 参考文件

| 模块 | 路径 |
|------|------|
| 扭蛋逻辑 | `Assets/Scripts/Game/Logic/GashaponMachineLogic.cs` |
| 启动入口 | `Assets/Scripts/Component/Handler/GameHandler.cs`（`StartGashaponMachine`） |
| 抽蛋上下文 | `Assets/Scripts/Bean/Game/GashaponMachineBean.cs` |
| 单蛋数据 | `Assets/Scripts/Bean/Game/GashaponItemBean.cs` |
| 配置 Bean | `Assets/Scripts/Bean/MVC/Game/StoreGashaponMachineInfoBean.cs` |
| Bean 扩展 | `Assets/Scripts/Bean/MVC/Game/StoreGashaponMachineInfoBeanPartial.cs` |
| 扭蛋商店 UI | `Assets/Scripts/Component/UI/Game/GashaponMachine/` |
| 破蛋 UI | `Assets/Scripts/Component/UI/Game/GashaponBreak/` |
| 商店项扭蛋扩展 | `Assets/Scripts/Component/UI/Common/Store/UIViewStoreItemPartialGashaponMatchine.cs` |
| 配置 JSON | `Assets/Resources/JsonText/StoreGashaponMachineInfo.txt` |
| 多语言 JSON | `Assets/Resources/JsonText/Language_StoreGashaponmachineInfo_{cn,en}.txt` |
| Excel 源表 | `Assets/Data/Excel/excel_store_gashaponmachine_info[商店-扭蛋机].xlsx` |
