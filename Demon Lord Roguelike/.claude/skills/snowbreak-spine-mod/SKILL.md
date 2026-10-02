---
name: snowbreak-spine-mod
description: SnowbreakSpine Mod（尘白幻化药）数据生成流程指南。使用此SKILL当需要为该Mod新增/重建幻化药道具、重新扫描Snowbreak spine资源目录(GirlXXX/变体子目录)、重新生成ItemsInfo/多语言JsonText、重新构建与部署Mod时。记录资源约定(变体子目录/每骨架一药)、stand特殊处理链(stand结尾→含stand→含idle→首个动画)、道具ID规则、other_data键值格式（ui_show_res/ui_show_data/ui_show_idle_anim 仅详情UI幻化三键）、生成脚本与一键构建的完整步骤。目前仅适用于SnowbreakSpine这一个Mod，其他Mod流程可能不同。
watched_files:
  - Assets/Scripts/Bean/Game/CreatureBeanPartial.cs
  - Assets/Scripts/Component/Handler/CreatureHandler.cs
  - Assets/Scripts/Component/Handler/SpineHandler.cs
  - Assets/Scripts/Utils/GameUIUtil.cs
  - Assets/FrameWork/Scripts/Component/Handler/SpineHandler.cs
  - Assets/Scripts/Component/UI/Test/TestTransformPotionGUI.cs
  - Assets/Editor/ModBuildEditorWindow.cs
  - Assets/Scripts/Bean/MVC/Game/ItemsInfoBean.cs
  - Assets/Scripts/Enums/ItemsEnum.cs
  - .claude/scripts/gen_snowbreak_spine_mod.py
---

# SnowbreakSpine Mod（尘白幻化药）数据生成流程

## Mod 概述

- **Mod 名**：`SnowbreakSpine`（= 主游戏 `Mods/` 下的目录名，ModManager 按目录名发现/加载）
- **内容**：255 个幻化药道具（ItemTypeEnum.TransformPotion=18）+ Snowbreak 系列 spine 资源（255 个 SkeletonData）
- **MOD 项目**：独立于主游戏的 Unity 工程（与其他 Spine Mod 同一 MOD 工程，示例路径 `E:\Unity\DemonLordRoguelikeMod\DemonLordRoguelikeMod\DemonLordRoguelikeMod`，**以实际机器路径为准**）
- **与其他 Mod 的核心差异**：① **待机动画=stand 特殊处理链**（资源动画均带 `sp_` 前缀不命中标准待机候选，键必写）；② 每个 SkeletonData 只出 1 个幻化药；③ 全部幻化药只改 ui_show（同 Nikke/BrownDust 等）

## 资源约定（MOD 项目侧）

```
MOD项目/Assets/ModResource/Spine/Snowbreak/
└── Girl001/                       - 角色目录（GirlXXX / NpcXXX）
    ├── 01/                        - 变体子目录（01/02/a/a_03/b/b_lv2/Girl001 等形态多样）
    │   ├── Girl001_01_SkeletonData.asset  ← 资源名=文件名去扩展名（保留 _SkeletonData）
    │   └── Girl001_01.json/.atlas.txt/.png(sp_girl001_01.png)/_Atlas.asset/_Material.mat
    ├── 02/ ... Girl001/ b_lv2/
    └── Girl015/a/                 - 单动画骨架（仅 sp_girl015a_gacha）
    └── Npc085/Npc085/             - NPC 骨架（仅 sp_npc085 单动画）
```

- **目录组织**：`GirlXXX或NpcXXX/<变体子目录>/<资源名>.完整spine导出物`；目录名无数值规律 → 套装号按**资源路径自然序** 001 起顺序编号
- **资源名 = SkeletonData 资产文件名（不含扩展名，保留 `_SkeletonData` 后缀）**（全局唯一无重复，2026-09-30 确认）；构建器按此名登记 Addressables Address，游戏侧 `SpineHandler.GetSkeletonDataAssetWithMod` 按此名精确匹配（大小写敏感）
- 同名 spine json = 文件名去 `_SkeletonData.asset` + `.json`（读 animations）；图集 PNG 名与目录不同名（sp_girlXXX 系），不影响流程
- **皮肤**：全量 255 个 JSON 扫描确认全部仅 `default` 一套皮肤（2026-09-30），不带 `ui_show_skin` 键
- **Spine JSON 格式**：真 4.3.26（2026-09-30 全量确认），无需格式转换；scan 已内置旧格式校验告警
- **贴图必须 PMA**：原始 255 张图集页 PNG 全为直通 alpha、390 材质直通开关全开，**2026-09-30 已按 CherryTale 同方案修复**（PNG `rgb*=a` + 材质置 0，复检通过）；**新增资源入库时必须复查**（PNG 透明区纯黑 + 全部 `*.mat` 含 Multiply/Screen 混合材质 `_StraightAlphaInput: 0`，详见 project_spine_mod_pma_requirement 记忆）

## 道具生成规则（每骨架 1 药 + stand 特殊处理链，2026-09-30 与用户确认）

- **出药**：每个 SkeletonData 出 **1 个**幻化药；待机动画按 **stand 特殊处理链**（`pick_stand_anim`）检出并写 `ui_show_idle_anim` 键（sp_ 前缀命名不命中标准候选 `idle,wait,idle1,wait1,stand`，故**每药必带**）：
  | 链级 | 规则 | 命中数 | 例 |
  |---|---|---|---|
  | ① | **以 stand 结尾**（小写；情绪变体 `_stand_haixiu` 等后缀自然排除；全库无多义） | 249 | `sp_girl001_01_stand` |
  | ② | 含 stand（自然序首个） | 2 | `sp_girl010a_04_gachastand_01`、`sp_girl017a_02_gachastand_a` |
  | ③ | 含 idle（自然序首个） | 2 | `sp_girl011a_03_gachaidle`、`sp_girl014a_04_gachaidle` |
  | ④ | 首个动画（单动画骨架兜底） | 2 | `sp_girl015a_gacha`、`sp_npc085` |
- **道具自ID**：`18` + `3位套装号`（资源路径自然序 001~255）+ `01`（恒 1 药）。例：Girl001/01 → 1800101；Npc085 → 1825501
- **other_data 键值格式**（`&` 拆项、首个 `:` 拆键值，缺省键省略）——**只有 ui_show 系键**：
  ```
  ui_show_res:Girl001_01_SkeletonData&ui_show_data:0.2134;0,0&ui_show_idle_anim:sp_girl001_01_stand
  ```
  - `ui_show_res`：ui_show_spine 高清展示资源名
  - `ui_show_data`：详情UI尺寸 `scale;x,y`；生成器按 `645/骨架高` 校准；scan 全量重建时按 id 保留手调值（`--reset-layout` 强制重算）
  - `ui_show_idle_anim`：stand 链检出的待机动画名（每药必带）
- **name 自ID = 道具自ID**（`name[language]` 标记驱动 `CombineModReferenceIds`，与其他 Mod 同机制）
- **固定字段**：item_type=18、num_max=1、icon_res=`Item_TransformPotion_1`、creature_model_id=0、reward_rarity=""、**source="1"**（征服模式奖励）
- **道具名**：cn「幻化药·尘白Girl001_01-01」/ tw「幻化藥·塵白…」/ en「Snowbreak Potion Girl001_01-01」（12 语言全生成；set_id 用资源名而非序号，目录无数值规律时更可读）
- **Excel 独立**（与 `TestTransformPotionGUI.GetModItemsExcelRelPath` 约定一致）：
  - `Assets/Data/Excel/excel_mod_items_info_snowbreakspine[Mod道具信息-SnowbreakSpine].xlsx`
  - `Assets/Data/Excel/excel_mod_language_snowbreakspine[Mod多语言-SnowbreakSpine].xlsx`

## 主项目配套代码（消费侧）

与其他 Spine Mod 完全共用，无任何 Mod 特化代码：

| 机制 | 位置 |
|------|------|
| other_data 键值解析 | `CreatureBeanPartial.ParseTransformOtherData`（`#region 幻化相关`，返回 `TransformOtherData` 结构体） |
| 高清展示资源（ui_show_res 键） | `CreatureBeanPartial.GetTransformUIShowSpineRes` → `CreatureHandler.SetCreatureData`（isUIShow 分支独立判定） |
| 替代待机动画（ui_show_idle_anim 键） | `CreatureBeanPartial.GetTransformUIShowIdleAnim` → `GameUIUtil.SetCreatureUIForDetails` 播放动画三级分支：该键非空→框架层按名直播——**本 Mod 全部药都带此键，详情UI 循环 stand 链动画** |
| 详情UI尺寸（ui_show_data 键） | `CreatureBeanPartial.GetTransformUIShowData` → `GameUIUtil.SetCreatureUIForDetails`；测试覆盖层优先 |
| 调参预览+写回 | `TestTransformPotionGUI`：保存按 modId 分组路由（结构体往返天然保留全部键） |
| Mod 道具/语言合并 | `BaseCfg.GetInitDataForMods` → id 拼接 + `BaseBean.CombineModReferenceIds` |
| Mod spine 资源加载 | `SpineHandler.GetSkeletonDataAssetWithMod` |
| Mod 构建工具 | `ModBuildEditorWindow`：下拉自动扫描，构建方法约定推导 `SnowbreakSpineModBuilder.BuildMod`，生成脚本路径推导 `gen_snowbreak_spine_mod.py`——**均按约定自动生效，无需改代码** |

## 生成流程（完整步骤）

### 1. 生成配置（在主项目侧执行，两段式：Excel 为唯一真实源）

```powershell
# 一律走 run-python.ps1 包装（路径动态化，不写死）：
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".claude/scripts/run-python.ps1" `
    ".claude/scripts/gen_snowbreak_spine_mod.py" all --mod-project "<MOD项目根目录>"
# 只改了 Excel 参数/改名后重导（不重新扫描资源）：
#   ... export --mod-project "<MOD项目根目录>"
```

**两段式流水线**（脚本 `.claude/scripts/gen_snowbreak_spine_mod.py`）：

- `scan`：扫描 Snowbreak 资源目录 → 重建/合并 **MOD 项目的两张 Excel**（道具表**全量重建**但 **ui_show_data 手调值按 id 保留**；语言表**按 id 合并保留人工改名**）；覆盖前自动备份到 `MOD项目/ExcelBackup/`；扫描结束打印 stand 链四级统计 + 资源名冲突告警（若有）
- `export`：读两张 Excel → 导出 JsonText

产出（写入 MOD 项目，属 Mod 包一部分）：
```
MOD项目/Mods/SnowbreakSpine/JsonText/
├── ItemsInfo.txt                    - 255 个幻化药道具
└── Language_ItemsInfo_{cn,en,jp,kr,tw,de,fr,ru,es,br,pl,tr}.txt
```

可调参数（仅 scan 生效）：`--ui-scale-k 645`、`--ui-pos-y 0`、`--reset-layout`。资源目录新增/变更后跑 `all`；只调道具参数/名字时改 Excel 后跑 `export`（**不要再 scan**）。

### 2. 构建 Addressables 产物

**方式一（推荐）：主项目「游戏/Mod构建工具」（ModBuildEditorWindow）一键完成**——「选择 Mod」下拉选中 `SnowbreakSpine`（构建器脚本存在即自动入列，构建方法/生成脚本按约定自动带出）。**前置：MOD 项目不能被另一个 Unity 实例打开（Temp/UnityLockfile 被进程占用才拦截；崩溃/强杀残留的锁文件由构建工具自动清理，无需手动删除）**。

**方式二：在 MOD 项目的 Unity 编辑器里执行**，菜单：**工具/Mod/SnowbreakSpine/一键构建(同步分组+构建)**（`MOD项目/Assets/Editor/SnowbreakSpineModBuilder.cs`）：

1. 扫描 `Assets/ModResource/Spine/Snowbreak` 全部 `SkeletonDataAsset` → 同步进 `Mod_SnowbreakSpine` 分组（Address=资产名；**PackSeparately** 每资源一个 bundle）
2. **SkeletonData 缩放统一复位**（`ApplyUIShowSkeletonDataScale`，幂等）：全部 ui_show 系，scale 统一 `0.01f`
3. 临时把其他分组 `IncludeInBuild=false`（隔离构建），构建后自动恢复
4. 新增/复用 Profile `SnowbreakSpine`，构建/加载路径指向 `Mods/SnowbreakSpine` 并设为激活
5. 清理旧产物（**保留 JsonText 子目录**）→ `BuildPlayerContent()` 构建
6. **构建后自动把 catalog 三件套从引擎默认输出目录拷进 Mod 目录**
7. **自动导出 Excel→JsonText 并部署到主项目**（`ExportJsonTextAndDeploy`；主项目根目录经菜单「工具/Mod/SnowbreakSpine/设置主项目根目录（自动导出部署用）」配置一次，EditorPrefs 键 `SnowbreakSpineModBuilder.MainProjectRoot` 独立于其他 Mod 的）
8. 产物：`Mods/SnowbreakSpine/` 下 `catalog.bin` + `catalog.hash` + `settings.json` + `*.bundle`（255 个）+ `JsonText/`

### 3. 部署到主项目（一键构建已自动完成；本步仅手工补部署时用）

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".claude/scripts/run-python.ps1" `
    ".claude/scripts/gen_snowbreak_spine_mod.py" export --mod-project "<MOD项目根目录>" --deploy-main "<主项目根目录>"
```

## 验证清单（用户手动 Play，AI 不自动 Play）

1. 主项目 Unity 编译通过后，用 **LauncherTest** 场景启动
2. 测试发放道具：`userData.AddBackpackItem(new ItemBean(modId*100000000000000L + 1800101))`（modId 用 `ModHandler.Instance.manager.GetModId("SnowbreakSpine")` 取）
3. 魔物管理（UICreatureManager）对生物使用幻化药 → 确认框显示道具名「幻化药·尘白Girl001_01-01」
4. 确认后：**卡片详情 UI 显示高清形象并循环对应 stand 动画**（非静态）；**普通小卡/基地/战斗场景仍显示原生物形象**（本 Mod 药不动 show/world）
5. 抽查链级 ②③④ 的药：Girl010a_04（gachastand_01）、Girl011a_03（gachaidle）、Girl015a（gacha 单动画）、Npc085（sp_npc085 单动画）详情 UI 均应动态播放
6. 吃幻原药恢复原形象；切语言（如 en）道具名显示「Snowbreak Potion Girl001_01-01」
7. 也可用测试模式-卡片测试-Mod幻化药测试面板：Mod 筛选选 `SnowbreakSpine`，大卡列表逐页核对形象与动画

## 注意与边界

- **本流程仅适用于 SnowbreakSpine**：其他 Mod 资源约定与道具规则不同，不套用本文档
- **stand 链新增资源自动适配**：新骨架无需配置，scan 按四级链自动检出；出现多个「stand 结尾」动画时取自然序首个（当前全库无多义）
- 套装号按路径自然序分配：**新增目录/文件会使其后套装号顺延、道具 id 漂移**——发布后如需加资源，应排在末尾或接受 id 重排（当前 255 个一次性全量生成，未发布前无此顾虑）
- 资源名即 Address 必须全局唯一：scan 内置冲突告警（当前无重复）
- 幻化状态 `CreatureBean.transformItemId` 存拼接后完整 id：Mod 移除→配置查不到→详情UI 自动回落原形象，幻原药仍可清残留
- **JsonText 缺失排查**：同其他 Mod——诊断=`Mods/SnowbreakSpine/JsonText/ItemsInfo.txt` 是否存在；修复=补跑 `export --deploy-main`
- **调单个药的详情UI尺寸不用跑生成脚本**：测试面板拖拽/滚轮实时调，「保存全部修改」按 Mod 分组路由写回（只换 ui_show_data 键）；资源目录变更才走 scan/all
