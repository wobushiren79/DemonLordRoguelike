---
name: echocalypse-spine-mod
description: EchocalypseSpine Mod（绯色回响幻化药）数据生成流程指南。使用此SKILL当需要为该Mod新增/重建幻化药道具、重新扫描Echocalypse spine资源目录(knight/纯数字目录)、重新生成ItemsInfo/多语言JsonText、重新构建与部署Mod时。记录资源约定(数字目录/_bg_背景不出药不进包/重名冲突检测)、道具ID规则、other_data键值格式（ui_show_res/ui_show_data/ui_show_idle_anim 仅详情UI幻化三键）、idle通用检测规则、生成脚本与一键构建的完整步骤。目前仅适用于EchocalypseSpine这一个Mod，其他Mod流程可能不同。
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
  - .claude/scripts/gen_echocalypse_spine_mod.py
---

# EchocalypseSpine Mod（绯色回响幻化药）数据生成流程

## Mod 概述

- **Mod 名**：`EchocalypseSpine`（= 主游戏 `Mods/` 下的目录名，ModManager 按目录名发现/加载）
- **内容**：317 个幻化药道具（ItemTypeEnum.TransformPotion=18）+ Echocalypse 系列 spine 资源（317 个正常 SkeletonData 进包；284 个 `_bg_` 背景 SkeletonData 不出药也不进包）
- **MOD 项目**：独立于主游戏的 Unity 工程（与其他 Spine Mod 同一 MOD 工程，示例路径 `E:\Unity\DemonLordRoguelikeMod\DemonLordRoguelikeMod\DemonLordRoguelikeMod`，**以实际机器路径为准**）
- **与其他 Mod 的核心差异**：① **每个正常 SkeletonData 只出 1 个幻化药**（不按皮肤/动画拆药，同 Nikke/PutGirl）；② **名字带 `_bg_` 的是背景——不出药、构建器同步分组时也排除**（284 个无引用 bundle 不占 Mod 体积）；③ 全部幻化药只改 ui_show（同 Nikke/BrownDust 等）

## 资源约定（MOD 项目侧）

```
MOD项目/Assets/ModResource/Spine/Echocalypse/
└── knight/                        - 顶层分组（当前唯一）
    ├── 200003/                    - 角色目录（纯数字，6~8 位）
    │   ├── 200003_SkeletonData.asset  ← 资源名=文件名去扩展名（保留 _SkeletonData）
    │   ├── 200003.json/.atlas.txt/.png/_Atlas.asset/_Material.mat
    │   └── 200003_bg_*.*/...      - 同角色的背景资源（_bg_ 标记，不进包）
    ├── 200004/ ... 20700720/ 7010101/  - 共 323 目录（其中 6 个仅含 _bg_）
    └── 400020/                    - 曾含嵌套 400010（与 knight/400010 重名冲突，已删除，见下）
```

- **目录组织**：`knight/<纯数字目录>/<目录名>.完整spine导出物`，扁平一层；目录数字进道具 ID
- **资源名 = SkeletonData 资产文件名（不含扩展名，保留 `_SkeletonData` 后缀）**；构建器按此名登记 Addressables Address（`Path.GetFileNameWithoutExtension`），游戏侧 `SpineHandler.GetSkeletonDataAssetWithMod` 按此名精确匹配（大小写敏感）。**与其他 Mod 约定完全一致**
- **`_bg_` 背景资源**：文件名含 `_bg_` 的 SkeletonData（284 个）是角色配套背景插画——**生成器不出药、构建器 `SyncGroupEntries`/`ApplyUIShowSkeletonDataScale` 同步排除**（资产名含 `_bg_` 跳过）。未来若需背景入药，两侧同时放开
- **重名冲突检测**（2026-09-30 特例）：knight/400020 曾内嵌一套**不同的** 400010 资源（与 knight/400010 同名）——Addressables Address/ui_show_res 引用均按文件名，同名无法共存；与用户确认为**多余资源已删除**（12 文件）。生成器 scan 内置资源名冲突告警（`used_res_names` 查重，冲突跳过+警告）；未来冲突特例用脚本 `SKIP_PATHS` 常量登记跳过
- 同名 spine json = 文件名去 `_SkeletonData.asset` + `.json`（读 animations）
- **皮肤**：全量 317 个 JSON 扫描确认全部仅 `default` 一套皮肤（2026-09-30），不带 `ui_show_skin` 键
- **Spine JSON 格式**：真 4.3.26（2026-09-30 全量确认），无需格式转换；scan 已内置旧 linkedmesh/分离约束数组校验告警（面向未来新增资源）
- **贴图 PMA**：1081 张图集页 PNG 原生即为 PMA（透明区纯黑，2026-09-30 全量确认）；1234 个材质的 `_StraightAlphaInput` 曾全开（导入默认），**已全部置 0**（1233 修改 + 1 个随嵌套资源删除）；**新增资源入库时必须复查**两项（PNG 透明区纯黑 + 全部 `*.mat` 含 Multiply/Screen 混合材质 `_StraightAlphaInput: 0`，详见 project_spine_mod_pma_requirement 记忆与 browndust-spine-mod SKILL 白边事故节）

## 道具生成规则（每骨架 1 药 + idle 通用检测，2026-09-30 与用户确认）

- **出药**：每个正常 SkeletonData 出 **1 个**幻化药；待机动画用 idle——走 **mod-system 通用 idle 检测规则**（`pick_idle_anim`）：命中主项目标准待机候选（`idle,wait,idle1,wait1,stand`，脚本动态读 `SpineAnimationState.txt` id=10001）→ **省略键**（走框架候选解析）；否则取首个含 idle 动画名写 `ui_show_idle_anim` 键
  - 全库分布：**314 个** 精确 `idle`（命中候选省略键）+ **3 个** `idle_A`（20500050/20700600/20700720，写 `ui_show_idle_anim:idle_A`）；无 idle 骨架 0 个
- **道具自ID**：`18` + 目录数字（6~8 位）+ `2位序号`（每目录恒 1 骨架=恒 `01`；目录内多骨架时按资源名自然序递增）。例：200003 → 1820000301；20500050 → 182050005001；7010101 → 187010100101
- **other_data 键值格式**（`&` 拆项、首个 `:` 拆键值，缺省键省略）——**只有 ui_show 系键**：
  ```
  ui_show_res:20500050_SkeletonData&ui_show_data:0.3057;0,0&ui_show_idle_anim:idle_A
  ```
  - `ui_show_res`：ui_show_spine 高清展示资源名
  - `ui_show_data`：详情UI尺寸 `scale;x,y`；生成器按 `645/骨架高` 校准；scan 全量重建时按 id 保留手调值（`--reset-layout` 强制重算）
  - `ui_show_idle_anim`：替代待机动画名（仅 idle_A 三药带；命中标准候选的药省略=框架候选解析）
- **name 自ID = 道具自ID**（`name[language]` 标记驱动 `CombineModReferenceIds`，与其他 Mod 同机制）
- **固定字段**：item_type=18、num_max=1、icon_res=`Item_Potion_1`、creature_model_id=0、reward_rarity=""、**source="1"**（征服模式奖励）
- **道具名**：cn「幻化药·绯色回响200003-01」/ tw「幻化藥·緋色迴響200003-01」/ en「Echocalypse Potion 200003-01」（12 语言全生成）
- **Excel 独立**（与 `TestTransformPotionGUI.GetModItemsExcelRelPath` 约定一致）：
  - `Assets/Data/Excel/excel_mod_items_info_echocalypsespine[Mod道具信息-EchocalypseSpine].xlsx`
  - `Assets/Data/Excel/excel_mod_language_echocalypsespine[Mod多语言-EchocalypseSpine].xlsx`

## 主项目配套代码（消费侧）

与其他 Spine Mod 完全共用，无任何 Mod 特化代码：

| 机制 | 位置 |
|------|------|
| other_data 键值解析 | `CreatureBeanPartial.ParseTransformOtherData`（`#region 幻化相关`，返回 `TransformOtherData` 结构体） |
| 高清展示资源（ui_show_res 键） | `CreatureBeanPartial.GetTransformUIShowSpineRes` → `CreatureHandler.SetCreatureData`（isUIShow 分支独立判定） |
| 替代待机动画（ui_show_idle_anim 键） | `CreatureBeanPartial.GetTransformUIShowIdleAnim` → `GameUIUtil.SetCreatureUIForDetails` 播放动画三级分支：该键非空→框架层按名直播（idle_A 三药）；空→框架候选解析（314 药的 idle） |
| 详情UI尺寸（ui_show_data 键） | `CreatureBeanPartial.GetTransformUIShowData` → `GameUIUtil.SetCreatureUIForDetails`；测试覆盖层优先 |
| 调参预览+写回 | `TestTransformPotionGUI`：保存按 modId 分组路由（结构体往返天然保留全部键） |
| Mod 道具/语言合并 | `BaseCfg.GetInitDataForMods` → id 拼接 + `BaseBean.CombineModReferenceIds` |
| Mod spine 资源加载 | `SpineHandler.GetSkeletonDataAssetWithMod` |
| Mod 构建工具 | `ModBuildEditorWindow`：下拉自动扫描（产物目录 ∪ `*ModBuilder.cs`），构建方法约定推导 `EchocalypseSpineModBuilder.BuildMod`，生成脚本路径推导 `gen_echocalypse_spine_mod.py`——**均按约定自动生效，无需改代码** |

## 生成流程（完整步骤）

### 1. 生成配置（在主项目侧执行，两段式：Excel 为唯一真实源）

```powershell
# 一律走 run-python.ps1 包装（路径动态化，不写死）：
# 资源变更后全量重建（扫描→Excel→JsonText）：
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".claude/scripts/run-python.ps1" `
    ".claude/scripts/gen_echocalypse_spine_mod.py" all --mod-project "<MOD项目根目录>"
# 只改了 Excel 参数/改名后重导（不重新扫描资源）：
#   ... export --mod-project "<MOD项目根目录>"
```

**两段式流水线**（脚本 `.claude/scripts/gen_echocalypse_spine_mod.py`）：

- `scan`：扫描 Echocalypse 资源目录 → 重建/合并 **MOD 项目的两张 Excel**（道具表**全量重建**但 **ui_show_data 手调值按 id 保留**；语言表**按 id 合并保留人工改名**）；覆盖前自动备份到 `MOD项目/ExcelBackup/`；扫描结束打印统计（_bg_ 跳过数/重名冲突跳过数/idle 检测三档计数 + 全部警告）
- `export`：读两张 Excel → 导出 JsonText

产出（写入 MOD 项目，属 Mod 包一部分）：
```
MOD项目/Mods/EchocalypseSpine/JsonText/
├── ItemsInfo.txt                    - 317 个幻化药道具
└── Language_ItemsInfo_{cn,en,jp,kr,tw,de,fr,ru,es,br,pl,tr}.txt
```

可调参数（仅 scan 生效）：`--ui-scale-k 645`、`--ui-pos-y 0`、`--reset-layout`。资源目录新增/变更后跑 `all`；只调道具参数/名字时改 Excel 后跑 `export`（**不要再 scan**）。

### 2. 构建 Addressables 产物

**方式一（推荐）：主项目「游戏/Mod构建工具」（ModBuildEditorWindow）一键完成**——「选择 Mod」下拉选中 `EchocalypseSpine`（构建器脚本存在即自动入列，构建方法/生成脚本按约定自动带出）。**前置：MOD 项目不能被另一个 Unity 实例打开（Temp/UnityLockfile 被进程占用才拦截；崩溃/强杀残留的锁文件由构建工具自动清理，无需手动删除）**。

**方式二：在 MOD 项目的 Unity 编辑器里执行**，菜单：**工具/Mod/EchocalypseSpine/一键构建(同步分组+构建)**（`MOD项目/Assets/Editor/EchocalypseSpineModBuilder.cs`）：

1. 扫描 `Assets/ModResource/Spine/Echocalypse` 全部 `SkeletonDataAsset` → 同步进 `Mod_EchocalypseSpine` 分组（Address=资产名；**PackSeparately** 每资源一个 bundle；**资产名含 `_bg_` 的背景资源排除**，日志打印排除数）
2. **SkeletonData 缩放统一复位**（`ApplyUIShowSkeletonDataScale`，幂等，同样跳过 `_bg_`）：全部 ui_show 系，scale 统一 `0.01f`
3. 临时把其他分组 `IncludeInBuild=false`（隔离构建），构建后自动恢复；构建器含防空构建保险：本分组残留 false 自动恢复、分组 0 条目时中止构建（2026-10-02 空构建事故后加）
4. 新增/复用 Profile `EchocalypseSpine`，构建/加载路径指向 `Mods/EchocalypseSpine` 并设为激活
5. 清理旧产物（**保留 JsonText 子目录**）→ `BuildPlayerContent()` 构建
6. **构建后自动把 catalog 三件套从引擎默认输出目录拷进 Mod 目录**
7. **自动导出 Excel→JsonText 并部署到主项目**（`ExportJsonTextAndDeploy`：调主项目 `run-python.ps1` + `gen_echocalypse_spine_mod.py export --deploy-main`；主项目根目录经菜单「工具/Mod/EchocalypseSpine/设置主项目根目录（自动导出部署用）」配置一次，EditorPrefs 键 `EchocalypseSpineModBuilder.MainProjectRoot` 独立于其他 Mod 的）
8. 产物：`Mods/EchocalypseSpine/` 下 `catalog.bin` + `catalog.hash` + `settings.json` + `*.bundle`（317 个）+ `JsonText/`

### 3. 部署到主项目（一键构建已自动完成；本步仅手工补部署时用）

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".claude/scripts/run-python.ps1" `
    ".claude/scripts/gen_echocalypse_spine_mod.py" export --mod-project "<MOD项目根目录>" --deploy-main "<主项目根目录>"
```

## 验证清单（用户手动 Play，AI 不自动 Play）

1. 主项目 Unity 编译通过后，用 **LauncherTest** 场景启动
2. 测试发放道具：`userData.AddBackpackItem(new ItemBean(modId*100000000000000L + 1820000301))`（modId 用 `ModHandler.Instance.manager.GetModId("EchocalypseSpine")` 取）
3. 魔物管理（UICreatureManager）对生物使用幻化药 → 确认框显示道具名「幻化药·绯色回响200003-01」
4. 确认后：**卡片详情 UI 显示高清形象并循环 idle 动画**；**普通小卡/基地/战斗场景仍显示原生物形象**（本 Mod 药不动 show/world）
5. idle_A 三药（20500050/20700600/20700720，自ID 182050005001/182070060001/182070072001）详情 UI 应循环 idle_A 动画而非静态
6. 吃幻原药恢复原形象；切语言（如 en）道具名显示「Echocalypse Potion 200003-01」
7. 也可用测试模式-卡片测试-Mod幻化药测试面板：Mod 筛选选 `EchocalypseSpine`，大卡列表逐页核对形象与动画

## 注意与边界

- **本流程仅适用于 EchocalypseSpine**：其他 Mod 资源约定与道具规则不同，不套用本文档
- **`_bg_` 背景双侧排除**：生成器（`BG_MARK`）与构建器（`BgMark`）都按资产名含 `_bg_` 跳过——未来新增背景资源自动排除无需改动；若要背景入药须两侧同时放开
- **资源名即 Address**：新增资源文件名必须全局唯一（scan 内置冲突告警）；目录名必须纯数字才分配套装号（非数字目录跳过+记入跳过清单）
- **发布后禁止 `--reset-layout` 与目录改名**（套装号=目录数字，改动即道具 id 漂移、存档幻化串角色）
- 套装内组合数 >99 时 2 位序号溢出告警截断（当前每目录恒 1 骨架，无风险）
- 幻化状态 `CreatureBean.transformItemId` 存拼接后完整 id：Mod 移除→配置查不到→详情UI 自动回落原形象，幻原药仍可清残留
- **JsonText 缺失排查**：同其他 Mod——Mod 目录只有 bundle+catalog 没有 JsonText 时，主游戏 Mod 初始化成功但道具合并不生效；诊断=`Mods/EchocalypseSpine/JsonText/ItemsInfo.txt` 是否存在；修复=补跑 `export --deploy-main`
- **调单个药的详情UI尺寸不用跑生成脚本**：测试面板拖拽/滚轮实时调，「保存全部修改」按 Mod 分组路由写回（只换 ui_show_data 键）；资源目录变更才走 scan/all
