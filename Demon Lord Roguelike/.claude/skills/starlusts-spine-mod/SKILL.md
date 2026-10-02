---
name: starlusts-spine-mod
description: StarLustsSpine Mod（星欲幻化药）数据生成流程指南。使用此SKILL当需要为该Mod新增/重建幻化药道具、重新扫描StarLusts spine资源目录(角色数字目录/CG|HCG|Standard子目录)、重新生成ItemsInfo/多语言JsonText、重新构建与部署Mod时。记录资源约定(数字目录/纯数字皮肤1·2·3出药、无数字皮肤出default皮药、动画统一Idle、无idle跳过)、道具ID规则、other_data键值格式（ui_show_res/ui_show_data/ui_show_skin/ui_show_idle_anim 仅详情UI幻化四键）、生成脚本与一键构建的完整步骤。目前仅适用于StarLustsSpine这一个Mod，其他Mod流程可能不同。
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
  - .claude/scripts/gen_starlusts_spine_mod.py
---

# StarLustsSpine Mod（星欲幻化药）数据生成流程

## Mod 概述

- **Mod 名**：`StarLustsSpine`（= 主游戏 `Mods/` 下的目录名，ModManager 按目录名发现/加载）
- **内容**：320 个幻化药道具（ItemTypeEnum.TransformPotion=18）+ StarLusts 系列 spine 资源（144 个 SkeletonData 出药；1002_CG 场景CG跳过）
- **MOD 项目**：独立于主游戏的 Unity 工程（与其他 Spine Mod 同一 MOD 工程，示例路径 `E:\Unity\DemonLordRoguelikeMod\DemonLordRoguelikeMod\DemonLordRoguelikeMod`，**以实际机器路径为准**）
- **与其他 Mod 的核心差异**：① **皮肤规则=只出纯数字皮肤（1/2/3）的药，无数字皮肤出 default 皮药**（AVG/CoverUpMode 等具名皮肤一律排除）；② 动画统一用 Idle（无 idle 的骨架跳过不出药）；③ 全部幻化药只改 ui_show（同 Nikke/BrownDust 等）

## 资源约定（MOD 项目侧）

```
MOD项目/Assets/ModResource/Spine/StarLusts/
└── 01/                            - 角色目录（纯数字，2~4 位：01~1002）
    ├── CG/                        - CG 子目录（无数字皮肤→default 皮 1 药；动画 InteractiveMode/P1_Idle 等）
    │   └── 01_CG_SkeletonData.asset + .json/.atlas.txt/.png(可多页)/_Atlas.asset/_Material.mat
    ├── HCG/                       - HCG 子目录（部分角色有）
    └── Standard/                  - 立绘子目录（有数字皮肤 1/2/3→各出 1 药；动画精确 Idle）
        └── 01_Standard_SkeletonData.asset + ...
```

- **目录组织**：`<角色数字目录>/<CG|HCG|Standard 子目录>/<资源名>.完整spine导出物`；角色目录数字进道具 ID
- **资源名 = SkeletonData 资产文件名（不含扩展名，保留 `_SkeletonData` 后缀）**（全局唯一无重复，2026-09-30 确认）；构建器按此名登记 Addressables Address，游戏侧 `SpineHandler.GetSkeletonDataAssetWithMod` 按此名精确匹配（大小写敏感）
- 同名 spine json = 文件名去 `_SkeletonData.asset` + `.json`（读 skins/animations）
- **皮肤形态**（2026-09-30 全量普查）：88 个 Standard 骨架有纯数字皮肤 `1`/`2`/`3`（另有 `AVG`、`CoverUpMode_01/1` 等非数字皮肤=按规则排除）；57 个 CG 骨架无数字皮肤（`CoverUpMode_01`/`CoverUpMode_02`/`UncoverMode`）
- **Spine JSON 格式**：真 4.3.26（2026-09-30 全量确认），无需格式转换；scan 已内置旧格式校验告警
- **贴图必须 PMA**：原始 416 张图集页 PNG 全为直通 alpha、442 材质直通开关全开，**2026-09-30 已按 CherryTale 同方案修复**（PNG `rgb*=a` + 材质置 0，复检通过）；**新增资源入库时必须复查**（PNG 透明区纯黑 + 全部 `*.mat` 含多页图集材质 `_StraightAlphaInput: 0`，详见 project_spine_mod_pma_requirement 记忆）

## 道具生成规则（数字皮肤出药 + 动画统一 Idle，2026-09-30 与用户确认）

- **皮肤规则**：骨架的**纯数字皮肤**（`^\d+$`，当前全库恰为 1/2/3）各出 1 个幻化药（`ui_show_skin:1/2/3`）；**无数字皮肤的骨架用 default 皮肤出 1 药**（不带皮肤键）
  - 全库分布：88 个 Standard × 3 = **264 皮肤药**；57 个 CG × 1 − 1 跳过（1002_CG）= **56 default 皮药**；合计 **320 药**
- **动画统一用 Idle**（`pick_idle_anim` 通用规则）：精确 `Idle` 命中主项目标准待机候选（大小写不敏感）→ **省略键**（88 个 Standard）；否则首个含 idle 动画名写 `ui_show_idle_anim` 键（56 个 CG=`InteractiveMode/P1_Idle`/`InteractMode/P1_Idle` 等）；**完全无 idle 动画的骨架跳过不出药+警告**（当前仅 `1002_CG`=纯 Background/Crowd 场景CG，2026-09-30 与用户确认跳过）
- **道具自ID**：`18` + 角色数字目录（2~4 位）+ `2位序号`（01 起）；目录内序号排序 = 子目录（CG→HCG→Standard 自然序）→ 数字皮肤（数值升序）。例：01 → 180101（01_CG default）/ 180102~180104（01_Standard 皮肤 1/2/3）；1002 → 18100201
- **other_data 键值格式**（`&` 拆项、首个 `:` 拆键值，缺省键省略）——**只有 ui_show 系键**：
  ```
  ui_show_res:01_Standard_SkeletonData&ui_show_data:0.2134;0,0&ui_show_skin:1
  ui_show_res:01_CG_SkeletonData&ui_show_data:0.1913;0,0&ui_show_idle_anim:InteractiveMode/P1_Idle
  ```
  - `ui_show_res`：ui_show_spine 高清展示资源名
  - `ui_show_data`：详情UI尺寸 `scale;x,y`；生成器按 `645/骨架高` 校准（同骨架全部药共用校准值，手调按药 id 各自保留）；scan 全量重建时按 id 保留手调值（`--reset-layout` 强制重算）
  - `ui_show_skin`：数字皮肤名（仅皮肤药带；default 皮药省略=骨架默认皮肤）
  - `ui_show_idle_anim`：替代待机动画名（命中标准候选省略；CG 药=InteractiveMode/P1_Idle 等）
- **name 自ID = 道具自ID**（`name[language]` 标记驱动 `CombineModReferenceIds`，与其他 Mod 同机制）
- **固定字段**：item_type=18、num_max=1、icon_res=`Item_Potion_1`、creature_model_id=0、reward_rarity=""、**source="1"**（征服模式奖励）
- **道具名**：皮肤药带皮肤后缀——cn「幻化药·星欲01-02 1」/ tw「幻化藥·星欲…」/ en「StarLusts Potion 01-02 1」；default 皮药无后缀「幻化药·星欲01-01」（12 语言全生成）
- **Excel 独立**（与 `TestTransformPotionGUI.GetModItemsExcelRelPath` 约定一致）：
  - `Assets/Data/Excel/excel_mod_items_info_starlustsspine[Mod道具信息-StarLustsSpine].xlsx`
  - `Assets/Data/Excel/excel_mod_language_starlustsspine[Mod多语言-StarLustsSpine].xlsx`

## 主项目配套代码（消费侧）

与其他 Spine Mod 完全共用，无任何 Mod 特化代码：

| 机制 | 位置 |
|------|------|
| other_data 键值解析 | `CreatureBeanPartial.ParseTransformOtherData`（`#region 幻化相关`，返回 `TransformOtherData` 结构体） |
| 高清展示资源（ui_show_res 键） | `CreatureBeanPartial.GetTransformUIShowSpineRes` → `CreatureHandler.SetCreatureData`（isUIShow 分支独立判定） |
| 指定皮肤（ui_show_skin 键） | `CreatureBeanPartial.GetTransformUIShowSkin` → `CreatureHandler.SetCreatureData`：皮肤名非空时 `SpineHandler.ChangeSkeletonSkin(Skeleton, string)` 按名整皮替换（数字皮肤单名，不涉 `|` 组合语法） |
| 替代待机动画（ui_show_idle_anim 键） | `CreatureBeanPartial.GetTransformUIShowIdleAnim` → `GameUIUtil.SetCreatureUIForDetails` 播放动画三级分支：该键非空→框架层按名直播（CG 药）；空→框架候选解析（Standard 药的 Idle） |
| 详情UI尺寸（ui_show_data 键） | `CreatureBeanPartial.GetTransformUIShowData` → `GameUIUtil.SetCreatureUIForDetails`；测试覆盖层优先 |
| 调参预览+写回 | `TestTransformPotionGUI`：保存按 modId 分组路由（结构体往返天然保留全部键） |
| Mod 道具/语言合并 | `BaseCfg.GetInitDataForMods` → id 拼接 + `BaseBean.CombineModReferenceIds` |
| Mod spine 资源加载 | `SpineHandler.GetSkeletonDataAssetWithMod` |
| Mod 构建工具 | `ModBuildEditorWindow`：下拉自动扫描，构建方法约定推导 `StarLustsSpineModBuilder.BuildMod`，生成脚本路径推导 `gen_starlusts_spine_mod.py`——**均按约定自动生效，无需改代码** |

## 生成流程（完整步骤）

### 1. 生成配置（在主项目侧执行，两段式：Excel 为唯一真实源）

```powershell
# 一律走 run-python.ps1 包装（路径动态化，不写死）：
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".claude/scripts/run-python.ps1" `
    ".claude/scripts/gen_starlusts_spine_mod.py" all --mod-project "<MOD项目根目录>"
# 只改了 Excel 参数/改名后重导（不重新扫描资源）：
#   ... export --mod-project "<MOD项目根目录>"
```

**两段式流水线**（脚本 `.claude/scripts/gen_starlusts_spine_mod.py`）：

- `scan`：扫描 StarLusts 资源目录 → 重建/合并 **MOD 项目的两张 Excel**（道具表**全量重建**但 **ui_show_data 手调值按 id 保留**；语言表**按 id 合并保留人工改名**）；覆盖前自动备份到 `MOD项目/ExcelBackup/`；扫描结束打印统计（数字皮肤药/default皮药/无idle跳过/idle 两档计数 + 全部警告）
- `export`：读两张 Excel → 导出 JsonText

产出（写入 MOD 项目，属 Mod 包一部分）：
```
MOD项目/Mods/StarLustsSpine/JsonText/
├── ItemsInfo.txt                    - 320 个幻化药道具
└── Language_ItemsInfo_{cn,en,jp,kr,tw,de,fr,ru,es,br,pl,tr}.txt
```

可调参数（仅 scan 生效）：`--ui-scale-k 645`、`--ui-pos-y 0`、`--reset-layout`。资源目录新增/变更后跑 `all`；只调道具参数/名字时改 Excel 后跑 `export`（**不要再 scan**）。

### 2. 构建 Addressables 产物

**方式一（推荐）：主项目「游戏/Mod构建工具」（ModBuildEditorWindow）一键完成**——「选择 Mod」下拉选中 `StarLustsSpine`（构建器脚本存在即自动入列，构建方法/生成脚本按约定自动带出）。**前置：MOD 项目不能被另一个 Unity 实例打开（Temp/UnityLockfile 被进程占用才拦截；崩溃/强杀残留的锁文件由构建工具自动清理，无需手动删除）**。

**方式二：在 MOD 项目的 Unity 编辑器里执行**，菜单：**工具/Mod/StarLustsSpine/一键构建(同步分组+构建)**（`MOD项目/Assets/Editor/StarLustsSpineModBuilder.cs`）：

1. 扫描 `Assets/ModResource/Spine/StarLusts` 全部 `SkeletonDataAsset` → 同步进 `Mod_StarLustsSpine` 分组（Address=资产名；**PackSeparately** 每资源一个 bundle）
2. **SkeletonData 缩放统一复位**（`ApplyUIShowSkeletonDataScale`，幂等）：全部 ui_show 系，scale 统一 `0.01f`
3. 临时把其他分组 `IncludeInBuild=false`（隔离构建），构建后自动恢复
4. 新增/复用 Profile `StarLustsSpine`，构建/加载路径指向 `Mods/StarLustsSpine` 并设为激活
5. 清理旧产物（**保留 JsonText 子目录**）→ `BuildPlayerContent()` 构建
6. **构建后自动把 catalog 三件套从引擎默认输出目录拷进 Mod 目录**
7. **自动导出 Excel→JsonText 并部署到主项目**（`ExportJsonTextAndDeploy`；主项目根目录经菜单「工具/Mod/StarLustsSpine/设置主项目根目录（自动导出部署用）」配置一次，EditorPrefs 键 `StarLustsSpineModBuilder.MainProjectRoot` 独立于其他 Mod 的）
8. 产物：`Mods/StarLustsSpine/` 下 `catalog.bin` + `catalog.hash` + `settings.json` + `*.bundle`（144 个，含未出药的 1002_CG——资源进包但无药引用，属无害冗余）+ `JsonText/`

### 3. 部署到主项目（一键构建已自动完成；本步仅手工补部署时用）

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".claude/scripts/run-python.ps1" `
    ".claude/scripts/gen_starlusts_spine_mod.py" export --mod-project "<MOD项目根目录>" --deploy-main "<主项目根目录>"
```

## 验证清单（用户手动 Play，AI 不自动 Play）

1. 主项目 Unity 编译通过后，用 **LauncherTest** 场景启动
2. 测试发放道具：`userData.AddBackpackItem(new ItemBean(modId*100000000000000L + 180102))`（modId 用 `ModHandler.Instance.manager.GetModId("StarLustsSpine")` 取）
3. 魔物管理（UICreatureManager）对生物使用幻化药 → 确认框显示道具名「幻化药·星欲01-02 1」
4. 确认后：**卡片详情 UI 显示高清形象的皮肤 1 并循环 Idle**；**普通小卡/基地/战斗场景仍显示原生物形象**（本 Mod 药不动 show/world）
5. **皮肤三档核对**：同角色 01 的皮肤 1/2/3 三药（180102/180103/180104）详情 UI 应显示不同皮肤；01_CG 药（180101）显示 default 皮并循环 P1_Idle
6. 吃幻原药恢复原形象；切语言（如 en）道具名显示「StarLusts Potion 01-02 1」
7. 也可用测试模式-卡片测试-Mod幻化药测试面板：Mod 筛选选 `StarLustsSpine`，大卡列表逐页核对形象/皮肤/动画

## 注意与边界

- **本流程仅适用于 StarLustsSpine**：其他 Mod 资源约定与道具规则不同，不套用本文档
- **数字皮肤判定严格 `^\d+$`**：`AVG`、`CoverUpMode_01/1` 等含字母/斜杠皮肤一律不出药；未来若出现 `4`、`5` 数字皮肤会自动出药（数值升序续排，序号顺延致后序 id 漂移——发布后出现新数字皮肤时需注意）
- 角色目录名必须**纯数字**才分配套装号（非数字目录跳过+记入跳过清单）
- **无 idle 骨架一律跳过不出药**：当前仅 1002_CG（纯场景CG）；未来新增场景骨架自动跳过+警告，无需配置
- **发布后禁止 `--reset-layout` 与目录改名**（套装号=目录数字，改动即道具 id 漂移、存档幻化串角色）
- 套装内组合数 >99 时 2 位序号溢出告警截断（当前每目录最多 4 组合，无风险）
- 幻化状态 `CreatureBean.transformItemId` 存拼接后完整 id：Mod 移除→配置查不到→详情UI 自动回落原形象，幻原药仍可清残留
- **JsonText 缺失排查**：同其他 Mod——诊断=`Mods/StarLustsSpine/JsonText/ItemsInfo.txt` 是否存在；修复=补跑 `export --deploy-main`
- **调单个药的详情UI尺寸不用跑生成脚本**：测试面板拖拽/滚轮实时调，「保存全部修改」按 Mod 分组路由写回（只换 ui_show_data 键，皮肤/idle 等其余键原样保留）；资源目录变更才走 scan/all
