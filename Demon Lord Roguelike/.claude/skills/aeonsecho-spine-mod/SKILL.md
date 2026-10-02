---
name: aeonsecho-spine-mod
description: AeonsEchoSpine Mod（回响幻化药）数据生成流程指南。使用此SKILL当需要为该Mod新增/重建幻化药道具、重新扫描AeonsEcho spine资源套装、重新生成ItemsInfo/多语言JsonText、重新构建与部署Mod时。记录资源约定、道具ID规则、other_data键值格式（show_res/ui_show_res/ui_show_data/show_data/world_data）、幻化药测试面板调参写回、生成脚本与一键构建的完整步骤。目前仅适用于AeonsEchoSpine这一个Mod，其他Mod流程可能不同。
watched_files:
  - Assets/Scripts/Bean/Game/CreatureBeanPartial.cs
  - Assets/Scripts/Bean/MVC/Game/ItemsInfoBean.cs
  - Assets/Scripts/Bean/MVC/Game/ItemsInfoBeanPartial.cs
  - Assets/Scripts/Component/Handler/CreatureHandler.cs
  - Assets/Scripts/Component/Handler/SpineHandler.cs
  - Assets/Scripts/Utils/GameUIUtil.cs
  - Assets/FrameWork/Scripts/Bean/BaseBean.cs
  - Assets/FrameWork/Editor/Base/Window/ExcelEditorWindow.cs
  - Assets/FrameWork/Scripts/Utils/ExcelUtil.cs
  - Assets/Scripts/Enums/ItemsEnum.cs
  - .claude/scripts/gen_aeonsecho_spine_mod.py
---

# AeonsEchoSpine Mod（回响幻化药）数据生成流程

## Mod 概述

- **Mod 名**：`AeonsEchoSpine`（= 主游戏 `Mods/` 下的目录名，ModManager 按目录名发现/加载）
- **内容**：417 个幻化药道具（ItemTypeEnum.TransformPotion=18）+ AeonsEcho 系列 spine 资源
- **MOD 项目**：独立于主游戏的 Unity 工程（示例路径 `E:\Unity\DemonLordRoguelikeMod\DemonLordRoguelikeMod\DemonLordRoguelikeMod`，**以实际机器路径为准**），负责存放 spine 源资源与构建 Addressables 产物
- **主项目**：Demon Lord Roguelike（本仓库），负责消费 Mod（幻化解析、道具合并、UI 展示）

## 资源约定（MOD 项目侧）

```
MOD项目/Assets/ModResource/Spine/AeonsEcho/
├── 1101/                          - 资源套装（数字文件夹名=套装号）
│   ├── Chess/                     - 基础 spine（世界/战斗/普通卡片形象=show 段），每个文件夹一个变体
│   ├── Chess_s01/                 - Chess 变体（s01/s02...）
│   ├── Avator_lv1/                - ui_show 高清图（详情UI形象=ui_show 段），每个文件夹一个变体
│   ├── Avator_lv1a/               - Avator 变体（lv1/lv1a/lv2/lv2a/lv3...）
│   └── ...
├── 851101/  (仅 Chess → 每 Chess 出 1 个道具，只设 show 段)
├── 4001/    (仅 Avator_lv1 → 出 1 个道具，只设 ui_show 段=详情UI幻化)
├── 5001/    (Chess + Secretary → 笛卡尔积，Secretary 同 Avator 走 ui_show)
├── 5002/    (仅 Secretary → 只设 ui_show 段)
├── 6001/    (仅 Elf → 同 Avator，只设 ui_show 段)
└── 2001/    (Chess + Avator×3 + AVG → AVG 同 Avator 走 ui_show)
```

- 每个变体文件夹内含完整 spine 导出物：`{套装号}_{变体名}_SkeletonData.asset` + `_Atlas.asset` + `.json/.atlas.txt/.png/.mat`
- **资源名 = SkeletonData 资产文件名（不含扩展名）**，如 `1101_Chess_s01_SkeletonData`；构建器按此名登记 Addressables Address，游戏侧 `SpineHandler.GetSkeletonDataAssetWithMod` 按此名精确匹配（大小写敏感）
- **ui_show 变体前缀 = `Avator`/`Secretary`/`Elf`/`AVG` 四种**（2026-09-22 起，同规则均走详情UI高清展示=ui_show_res/ui_show_data 键，脚本 `UI_SHOW_PREFIXES` 常量）；其他命名的文件夹不参与幻化药生成

## Spine JSON 格式陷阱（2026-09 已修复）

**这批资源是「伪 4.2」**：JSON 头写 `"spine": "4.2.43"`，内容实为 4.1 及更早的旧格式（源游戏资产经第三方转换，头部版本号不可信）。主项目运行时是 **spine-csharp 4.3.39**，遇旧格式报 `KeyNotFoundException: uvs` 或 `XX constraint not found`。已做两处格式转换（全量 429 个 JSON 均已通过真实 4.3.39 运行时离线解析验证）：

1. **linkedmesh**：旧 `parent` 键 → 新 `source` 键（14 文件 170 处）。
2. **约束数组**：旧的顶层分离 `ik`/`transform`/`path` 数组 → 新的统一 `constraints` 数组（389 文件 5833 条）。转换规则：transform 的 `target→source`、`local→localSource+localTarget`、`relative→additive`、合成六属性恒等 `properties` 映射（from X→to X，mix 走顶层键）；path 的 `target→slot`；ik 仅补 `type`；三类按旧 `order` 稳定排序合并后删 `order`。

**新增资源套装时**：勿信 JSON 头版本号，须先校验格式——旧 linkedmesh（含 `"parent"`）与旧约束（顶层 `"ik"/"transform"/"path"` 数组）需先做同样转换，否则运行时解析失败（典型报错：`uvs` 键缺失、`Transform/IK/Path constraint not found`）。主游戏自带 4.2.43 资源（Succubus 等）是真 4.2，可正常加载，不要被「同版本号」迷惑。

## 道具生成规则

- **组合**：套装内 Chess×ui_show 笛卡尔积。如套装有 Chess/Chess_s01 + Avator_lv1/Avator_lv2 → 4 个道具；**仅 Chess** → 每个 Chess 单独出 1 个道具（只设 show 段）；**仅 ui_show**（Avator/Secretary/Elf/AVG，2026-09-22 起）→ 每个 ui_show 变体单独出 1 个道具（只设 ui_show 段=详情UI幻化：详情UI 显示高清形象，世界/战斗/小卡回落原生物形象）；两类都没有 → 跳过该套装
- **道具自ID**（Mod JsonText 内的原始 id，运行时由 BaseCfg.CombineModId 拼 modId 前缀）：`18`(道具类型) + `套装号` + `2位序号`(01 起)。混合套装序号排序 = **ui_show 前缀组（Avator→Secretary→Elf→AVG）→ Chess → 组内变体自然序**（单组套装与旧版 Chess×Avator 顺序完全一致；新前缀组的组合整体排后，旧套装 id→组合映射不漂移，2026-09-22 起）。例：套装 1101 → 18110101~18110120；套装 851101 → 1885110101
- **other_data 键值格式**：按 `&` 拆项、每项以第一个 `:` 拆 key/value（与主项目 attack_mode other_data 同规约），**缺省键省略**。完整形态：
  ```
  show_res:1101_Chess_SkeletonData&ui_show_res:1101_Avator_lv1_SkeletonData&ui_show_data:0.1919;0,0&show_data:3.75;0,-120
  ```
  - `show_res`：默认展示形象资源名（**可空**：套装无 Chess 时省略=仅详情UI幻化道具，此时世界/战斗/小卡显示原生物形象；show=游戏默认展示，即原 chess 概念，2026-09-21 由 chess_res 改名）
  - `ui_show_res`：ui_show_spine 高清展示资源名（详情UI，isUIShow=true 时使用；可空；**消费侧独立判定、不依赖 show_res**——`CreatureHandler.SetCreatureData` 的 isUIShow 分支只查本键；ui_show=详情UI高清展示，即原 avator 概念，2026-09-21 由 avator_res 改名）
  - `ui_show_data`：详情UI尺寸 `scale;x,y`（格式同 `CreatureModelBean.ui_data_b`）；生成器按 `645/Avator骨架高` 校准 scale（2026-09 起由 430 基准放大 1.5 倍），默认位移 `0,0`（原为 `0,-215`）
  - `show_data`：默认展示小卡UI尺寸 `scale;x,y`（格式同 `CreatureModelBean.ui_data_s`，2026-09-21 由 ui_chess_data 改名）；生成器按 `3159/默认展示骨架高` 校准 scale（3159=842.4×2.5×1.5：主游戏人形骨架 ui_data_s 基准 2.5 放大 1.5 倍，2026-09 起），默认位移 `0,-120`
  - `world_data`：世界显示尺寸/偏移 `scale;x,y`（x=横向偏移，y=竖向抬升；**战斗/基地/议会等世界空间 SkeletonAnimation 显示消费**，2026-09-21 新增）；**无骨架校准来源，默认不生成该键**——手调入口=主项目幻化药测试面板场景列表/单个预览场景组；**scan 全量重建时按 id 保留手调值**（脚本 `read_preserved_layout_data`，2026-09-27 起扩展为 show_data/ui_show_data/world_data 三键同保，`--reset-layout` 可强制重算），不会被校准覆盖
  - `idle_anim` / `ui_show_idle_anim`：show / ui_show 骨架的替代待机动画名（**idle 动画替代规则**，2026-09-28 起，详见 mod-system SKILL 通用规则节）：对应骨架动画列表命中主项目标准待机候选（`idle,wait,idle1,wait1,stand`）时省略=走框架候选解析；无标准候选时取首个小写含 `idle` 的动画名（如 Elf 系=`idle_emo1`，当前仅 26 个段次命中替代）；完全没有含 idle 动画则不生成该键+警告
  - 无 ui_show 形态示例：`show_res:851101_Chess_SkeletonData&show_data:3.4439;0,-120`
  - 仅详情UI幻化形态示例（套装无 Chess）：`ui_show_res:6001_Elf_SkeletonData&ui_show_data:0.1828;0,0`
  - 2026-09-21 起由旧位置段格式（`chessRes,avatorRes|uiData|chessUiData`）改为键值格式；旧数据用生成脚本 `migrate` 子命令一次性迁移（幂等，数值原样保留）
- **name 自ID = 道具自ID**（约定）：指向 Mod 自带语言表同 id 行；运行时拼接由 **Excel 列头 `name[language]` 标记驱动**——`ExcelEditorWindow.CreateEntity` 生成 `ItemsInfoBean.cs` 时自动产出 `CombineModReferenceIds` 重写（无标记/0 值不拼接）。**Mod 道具不支持复用主游戏 textId**（会被拼接后查不到），空文本用 name=0
- **固定字段**：item_type=18、num_max=1（不堆叠）、icon_res=`Item_TransformPotion_1`（复用主游戏内置图标）、creature_model_id=0、reward_rarity=""（消耗品不进装备奖励池——奖励生成按 creature_model_id 过滤，0 型道具天然不进池）、**source="1"（=ItemSourceEnum.ConquerReward 征服模式奖励：征服通关领奖随机一个魔晶位替换为池内随机幻化药，见 fight-reward-system；ITEM_COLUMNS 与 compute_items 均已带 source 列，scan 全量重建不会丢）**
- **道具名**：cn「幻化药·回响{套装}-{序号}」/ tw「幻化藥·迴響…」/ 其他语言「Echo Potion …」（12 语言全生成）

## 主项目配套代码（消费侧）

| 机制 | 位置 |
|------|------|
| other_data 键值解析 | `CreatureBeanPartial.ParseTransformOtherData`（`#region 幻化相关`，`&` 拆项 + `:` 拆键值；返回 `TransformOtherData` 结构体，2026-09-28 由多 out 参数重构，含 `idle_anim`/`ui_show_idle_anim` 键=show/ui_show 骨架替代待机动画） |
| 基础形象（show_res 键） | `CreatureBeanPartial.GetTransformSpineRes` |
| 高清展示（ui_show_res 键） | `CreatureBeanPartial.GetTransformUIShowSpineRes` → `CreatureHandler.SetCreatureData`（isUIShow 分支**独立判定、不依赖 show_res**：有 ui_show_res 即替换详情UI形象并跳过原生物皮肤，2026-09-22 起支持仅详情UI幻化道具；**例外**：配了 ui_show_skin 键的幻化药在 hasTransform 时仍按名换肤 `SpineHandler.ChangeSkeletonSkin(Skeleton, string)`——ArkReSpine 多皮肤药专用，2026-09-24 起；皮肤串支持「|」分隔多皮肤，拆分后多个改调 params 叠加重载——CherryTaleSpine 组合皮药用，2026-09-29 起） |
| 详情UI尺寸（ui_show_data 键） | `CreatureBeanPartial.GetTransformUIShowData` → `GameUIUtil.SetCreatureUIForDetails`（替代原生物 ui_data_b）；编辑器下测试覆盖层 `TransformPotionUITestOverride` 优先 |
| 小卡UI尺寸（show_data 键） | `CreatureBeanPartial.GetTransformShowData` → `GameUIUtil.SetCreatureUIForSimple`（替代原生物 ui_data_s）；编辑器下测试覆盖层 `TransformPotionUITestOverride` 优先 |
| 尺寸/位置测试覆盖层 | `Assets/Scripts/Bean/Game/TransformPotionUITestOverride.cs`（`#if UNITY_EDITOR` 整文件，打包无）：key=幻化药完整id 的「scale;x,y」覆盖值×3段，被上面三个 Get 优先消费；`GetAllDirtyIds` 供批量保存 |
| 世界显示尺寸/偏移（world_data 键） | `CreatureBeanPartial.GetTransformWorldData` → `CreatureHandler.SetCreatureData`（SkeletonAnimation 分支：缩放=size_spine×体型×world倍率，spine 节点 localPosition=偏移或归零恒管理防池化残留；**仅 hasTransform[有 show_res] 时消费**，无 show_res 的详情UI幻化药写了也不生效——测试面板场景段对此类药已禁调，2026-10-01 起）；编辑器下测试覆盖层优先；战斗受击抖动基准=`FightCreatureEntity.AnimForAnimForUnderAttackShake` 按偏移复位（同样带 show_res 门控，2026-10-01 起） |
| 替代待机动画（idle_anim/ui_show_idle_anim 键，2026-09-28 起） | show 段=`CreatureBeanPartial.GetTransformIdleAnim` → 游戏层 `SpineHandler.GetAnimNameAppoint`（Idle 分支幻化时优先按名直播，缺省交框架候选）；ui_show 段=`GetTransformUIShowIdleAnim` → `GameUIUtil.SetCreatureUIForDetails` 播放动画三级分支（非空→框架按名直播；ui_show_res 非空→框架候选；否则原链路） |
| 调参预览+写回 | `Assets/Scripts/Component/UI/Test/TestTransformPotionGUI.cs`（测试模式-卡片测试-Mod幻化药测试面板，四个页签）：单个预览/小卡列表/大卡列表/场景列表，详见下节 |

## 调参写回（幻化药测试面板，2026-09-21 新增；同日开始支持列表批量与 world_data）

在主项目里预览并调整幻化药的小卡/详情UI/世界显示的尺寸与位置，无需离开 Play 模式即可写回本 Mod（**只改 ui_show_data/show_data/world_data 三键，show_res/ui_show_res 等其余键原样保留**）：

- **四个页签**：①单个预览=下拉选药+◀▶左右快速切换（含无幻化项，两端停住） + 小卡/大卡/场景对比 + 三组文本框精输与滑动条粗调（缩放对数映射 0.01~20；位置 UI±600 取整/世界±2 两位小数）；②小卡列表=列×行网格真实卡片（show_data）；③大卡列表=列×行网格真实详情卡**卡面模式**（SetData 后隐藏属性/好感/装备/BUFF/MP/备注等详情区块，只留底板+肖像+名字+稀有度+职业+等级；ui_show_data，仅基础药无 ui_show_res=详情UI回落 show 形象同样可调，标签标注（无Avator)，2026-09-30 起）；④场景列表=世界空间 spine 一排（world_data）。列表分页（◀▶+跳页）+ **Mod 筛选**（全部/游戏本地/各已加载 Mod）+ **横竖个数与间距步进可调**（布局行，**持久化到项目内 `ProjectSettings/TestTransformPotionLayout.json`，随 git 全队共享**）；网格整体右移避开左侧面板；每项下方名字标签带 ●=未保存修改，另有 **[还原]=单段恢复配置值、[0,0]=位置归零、[复制]/[粘贴]=参数快速套用（静态剪贴板）** 小按钮，拖拽带 4px 阈值防误触。
- **悬停交互**（列表与单个预览通吃）：鼠标移到目标卡片/模型上 → 滚轮等比改缩放（×1.05/格）、左键拖拽改位置（卡片按 Canvas 单位取整/场景按相机视场换算世界单位保留 2 位小数）；拖拽期间只直改显示不打断动画。
- **预览**：调参写入 `TransformPotionUITestOverride` 覆盖层 → 三个 Get 优先返回 → 所有走真实显示链的 UI/世界显示立即生效；场景 spine 为「根节点摆放 + Renderer 子节点承载缩放/偏移」结构（与游戏内实体一致）。
- **保存链路**（面板底栏「保存全部修改（N)」按钮，`SaveAllOverrides` 批量版）：Mod 项目根读 `EditorPrefs["ModBuildEditorWindow.ModProjectPath"]`（即 Mod构建工具里配的路径）→ **按 modId 分组路由**（2026-09-24 起，modId→modName 经 `ModManager.GetModJsonTextFileInfos`，各 Mod 独立处理）：① EPPlus 直写该 Mod 的道具 Excel（唯一真实源；路径按 `GetModItemsExcelRelPath` 约定——AeonsEchoSpine=`Assets/Data/Excel/excel_mod_items_info[Mod道具信息].xlsx` 历史文件名，后续 Mod=`excel_mod_items_info_{modName小写}[Mod道具信息-{modName}].xlsx`，如 ArkReSpine；按自ID=`完整id % 10^14` 定位行，**全部修改一次打开一次写盘**；**每次 Play 会话每文件首次写入前自动备份到 `MOD项目/ExcelBackup/`（滚动复用 .bak.1~3，只留最近 3 份）**；文件被 Excel/WPS 占用会红字提示）→ ② 直补两处 JsonText（`MOD项目/Mods/{modName}/JsonText/ItemsInfo.txt` + 主项目部署副本 `Mods/{modName}/JsonText/ItemsInfo.txt`，按 `"id":<自ID>,` 锚点定位批量替换 other_data 值）→ ③ 当前会话内存 `ItemsInfoCfg` 同步并清覆盖层。内置幻化药（无 modId 前缀）跳过并计数提示。**不用再跑 export/部署管线**；下次正常跑 export 以 Excel 为准重新生成也不冲突。
- **护栏**：参数非法红字拦截；全部调参与保存逻辑 `#if UNITY_EDITOR`；「清空未保存修改」一键丢弃全部覆盖。
- 保存只影响三个尺寸键；若资源套装变更（新增/删除套装），仍须走正常 `scan`/`all` 生成流程（**布局三键 show_data/ui_show_data/world_data 手调值默认按 id 保留**，新增资源按骨架校准默认值；需强制全部重算时加 `--reset-layout`，2026-09-27 起）。
| Mod 道具/语言合并 | `BaseCfg.GetInitDataForMods` → id 拼接 + `BaseBean.CombineModReferenceIds` 钩子（由生成器按列头 `name[language]` 标记自动重写进 `ItemsInfoBean.cs`） |
| Mod spine 资源加载 | `SpineHandler.GetSkeletonDataAssetWithMod`（assetName 命中已加载 Mod 的 catalog key 时走 Mod 路径） |

## 生成流程（完整步骤）

### 0. 前置（仅机制变更时需要）：重新生成 ItemsInfo Entity

Mod 道具 `name` 的 modId 拼接代码在 `ItemsInfoBean.cs` 里**由生成器产出**（列头 `name[language]` 标记驱动）。若 `ItemsInfoBean.cs` 还没有 `CombineModReferenceIds` 重写（如本机制刚上线/刚加标记列），先在主项目 Unity 里对 `excel_items_info[道具信息].xlsx` 执行「生成 Entity」（ExcelEditorWindow），确认生成结果包含：

```csharp
public override void CombineModReferenceIds(int modId)
{
    if (name > 0) name = ItemsInfoCfg.CombineModId(modId, name);
}
```

### 1. 生成配置（在主项目侧执行，两段式：Excel 为唯一真实源）

```powershell
# 一律走 run-python.ps1 包装（路径动态化，不写死）：
# 资源变更后全量重建（扫描→Excel→JsonText）：
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".claude/scripts/run-python.ps1" `
    ".claude/scripts/gen_aeonsecho_spine_mod.py" all --mod-project "<MOD项目根目录>"
# 只改了 Excel 参数/改名后重导（不重新扫描资源）：
#   ... export --mod-project "<MOD项目根目录>"
```

**两段式流水线**（脚本 `.claude/scripts/gen_aeonsecho_spine_mod.py`）：

- `scan`：扫描 AeonsEcho 资源套装 → 重建/合并 **MOD 项目的两张 Excel**（可用 Excel/WPS 直接打开查看、调整参数）；**idle 动画检测**（2026-09-28 起：show 段 Chess 骨架→`idle_anim` 键、ui_show 段变体骨架→`ui_show_idle_anim` 键，无标准待机候选时取首个含 idle 动画名，扫描结束打印 命中/替代/无idle 统计）：
  - `Assets/Data/Excel/excel_mod_items_info[Mod道具信息].xlsx` — 道具配置（3 行表头：列名/类型/说明，与主项目 excel_items_info 同布局，`name[language]` 标记列）；**全量重建**，覆盖前自动备份到 `MOD项目/ExcelBackup/`（Assets 之外，符合 Excel 备份清理规则；滚动复用 .bak.1~3，只留最近 3 份）
  - `Assets/Data/Excel/excel_mod_language[Mod多语言].xlsx` — 道具名多语言（id + content_{12语言}）；**按 id 合并保留人工改名**，新增道具补默认名、失效 id 清理；某语言留空 = 英文兜底
- `export`：读两张 Excel → 导出 JsonText
- `migrate`：道具表 other_data 旧位置段格式 → 键值格式的一次性迁移（幂等：已是键值格式的行跳过；数值原样保留，有改动才备份写回）。仅 2026-09-21 格式切换时使用，新跑的 scan 直接产键值格式，正常流程不再需要

产出（写入 MOD 项目，属 Mod 包一部分）：
```
MOD项目/Mods/AeonsEchoSpine/JsonText/
├── ItemsInfo.txt                    - 341 个幻化药道具
└── Language_ItemsInfo_{cn,en,jp,kr,tw,de,fr,ru,es,br,pl,tr}.txt
```

可调参数（仅 scan 生效）：`--ui-scale-k 645`（详情UI缩放校准常数，=原430基准×1.5）、`--ui-pos-y 0`（详情UI默认Y偏移）、`--ui-chess-scale-k 3159`（小卡UI缩放校准常数，=842.4×2.5×1.5）、`--ui-chess-pos-y -120`（小卡UI默认Y偏移）。资源套装新增/变更后跑 `all`；只调道具参数/名字时改 Excel 后跑 `export`（**不要再 scan，否则道具表被重建覆盖**）。

### 2. 构建 Addressables 产物

**方式一（推荐）：主项目「游戏/Mod构建工具」（ModBuildEditorWindow）一键完成**——只开主项目即可：「选择 Mod」下拉选中 `AeonsEchoSpine`（已构建过的自动入列，构建方法按约定自动带出，2026-09-27 起）→ ⚡ 一键构建+移动，或分步：⓪ 仅导出配置（Excel→JsonText 秒级，改道具参数/改名后用）/ ① 批量模式（-batchmode -executeMethod）调 MOD 项目 Unity 执行下方一键构建同款方法 / ② export 导出 JsonText + 移动部署（详见 editor-extension-system skill「Mod 构建工具」；**生成脚本路径按 Mod 名自动推导** `gen_{modName去Spine后缀小写}_spine_mod.py`，2026-09-24 起——AeonsEchoSpine→gen_aeonsecho_spine_mod.py、ArkReSpine→gen_arkre_spine_mod.py）。

**方式二：在 MOD 项目的 Unity 编辑器里执行**，菜单：**工具/Mod/AeonsEchoSpine/一键构建(同步分组+构建)**（`MOD项目/Assets/Editor/AeonsEchoSpineModBuilder.cs`）：

1. 扫描 `Assets/ModResource/Spine/AeonsEcho` 全部 `SkeletonDataAsset` → 同步进 `Mod_AeonsEchoSpine` 分组（先清空再全量加，Address=资产名；旧空分组 `Mod_AeonsEcho` 自动改名复用）；**打包模式 PackSeparately**（每个 SkeletonData 一个 bundle——幻化药使用时按需加载单个资源，避免 PackTogether 单大 bundle 首次全量加载慢）
2. **SkeletonData 缩放双向校准**（`ApplyChessSkeletonDataScale`，幂等可重跑）：Chess 系 `SkeletonDataAsset.scale` 统一设为 `ChessSkeletonDataScale=0.002f`（默认导入 0.01 × 0.2 = 非 ui_show_spine 显示大小 ×1/5）；**ui_show 系（Avator/Secretary/Elf/AVG 目录）主动复位为 `UIShowSkeletonDataScale=0.01f`**——详情UI尺寸由道具 other_data 的 ui_show_data 键控制（历史构建曾把 Secretary/Elf 误设为 0.002，校准会自动改回，2026-09-22 起）
3. 临时把其他分组 `IncludeInBuild=false`（隔离构建，只出本 Mod 的 bundle），构建后自动恢复
4. 新增/复用 Profile `AeonsEchoSpine`，构建/加载路径指向 `Mods/AeonsEchoSpine` 并设为激活
5. 清理旧产物（**保留 JsonText 子目录**）→ `BuildPlayerContent()` 构建
6. **构建后自动把 `catalog.bin`/`catalog.hash`/`settings.json` 从引擎默认输出目录（`Library/com.unity.addressables/aa/Windows`，catalog 不跟随分组 BuildPath）拷进 Mod 目录**——手工补拷时来源也是这里
7. **自动导出 Excel→JsonText 并部署到主项目**（`ExportJsonTextAndDeploy`：调主项目 `run-python.ps1` + `gen_aeonsecho_spine_mod.py export --deploy-main`，导出逻辑单一真实源在 python 脚本、C# 只调用不重复实现；主项目根目录经菜单「工具/Mod/AeonsEchoSpine/设置主项目根目录（自动导出部署用）」配置一次（EditorPrefs 持久化），未配置/脚本缺失/失败只告警不影响构建产物）——防「只构建不导出」导致 JsonText 缺失
8. 产物：`Mods/AeonsEchoSpine/` 下 `catalog.bin` + `catalog.hash` + `settings.json` + `*.bundle` + `JsonText/`

另有「仅同步分组条目」菜单用于先检查条目列表再构建。

### 3. 部署到主项目（一键构建已自动完成；本步仅手工补部署时用）

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".claude/scripts/run-python.ps1" `
    ".claude/scripts/gen_aeonsecho_spine_mod.py" export --mod-project "<MOD项目根目录>" --deploy-main "<主项目根目录>"
```

校验 `Mods/AeonsEchoSpine/catalog.bin` 存在后，整体覆盖拷贝到 `主项目/Mods/AeonsEchoSpine/`（先删旧目录）。

## 验证清单（用户手动 Play，AI 不自动 Play）

1. 主项目 Unity 编译通过后，用 **LauncherTest** 场景启动（Mod 初始化已收口到 `BaseLauncher.Launch()` 首行 `InitializeAllModsSync()`，正式包 LauncherGame 同路径）
2. 测试发放道具：`userData.AddBackpackItem(new ItemBean(modId*100000000000000L + 18110101))`（modId 用 `ModHandler.Instance.manager.GetModId("AeonsEchoSpine")` 取——ModManager 无静态 Instance，通常首个新 Mod=1 或 2）
3. 魔物管理（UICreatureManager）对生物使用幻化药 → 确认框显示道具名「幻化药·回响1101-01」
4. 确认后：卡片详情 UI 显示 **UIShow 高清图**（比例正常不裁切）；普通小卡显示 **Show 默认展示形象**（尺寸坐标=道具 show_data 键，非原生物 ui_data_s）；基地/战斗场景显示 **Show 默认展示形象**
5. **仅详情UI幻化药**（仅 ui_show 套装的道具，如 18600101 Elf）：详情 UI 显示高清形象，普通小卡/基地/战斗场景仍显示**原生物形象**
6. 吃幻原药恢复原形象；切语言（如 en）道具名显示「Echo Potion 1101-01」
7. 移除 `Mods/AeonsEchoSpine` 目录重启 → 已幻化生物自动回落原形象（每 id 一次 LogError 属预期），幻原药仍可清残留

## 注意与边界

- **本流程仅适用于 AeonsEchoSpine**：其他 Mod（Nikke/BrownDust 等分组）资源约定与道具规则可能不同，不套用本文档
- MOD 项目改动（资源/构建器脚本）不在本仓库 git 内，watched_files 只覆盖主项目侧消费代码与生成脚本
- 套装内组合数 >99 时 2 位序号溢出，生成器会告警截断（当前最多 20 组合，无风险）
- 幻化状态 `CreatureBean.transformItemId` 存的是拼接后的完整 id（含 modId 前缀）：Mod 移除→id 查不到配置→自动回落原形象；Mod 重装且 modId 不变（持久化映射）→自动恢复
- **JsonText 缺失排查**：Mod 目录被整体删除、或只跑了一键构建没跑 export 时，产物只有 bundle+catalog 没有 JsonText——此时主游戏 Mod 初始化**成功**（catalog 正常加载）但道具/语言合并不生效（典型症状：幻化药下拉/GM Mod 区为空、无报错日志）。诊断=`Mods/AeonsEchoSpine/JsonText/ItemsInfo.txt` 是否存在；修复=补跑 `export --deploy-main`（Excel 是唯一真实源，导出后还需同步到打包输出目录的 Mods）。**一键构建已内置自动导出+部署**（ExportJsonTextAndDeploy，需先用菜单设置主项目根目录），该问题不会再因漏跑 export 复现
