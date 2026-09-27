---
name: arkre-spine-mod
description: ArkReSpine Mod（方舟幻化药）数据生成流程指南。使用此SKILL当需要为该Mod新增/重建幻化药道具、重新扫描ArkRe spine资源套装(HXXX目录)、重新生成ItemsInfo/多语言JsonText、重新构建与部署Mod时。记录资源约定(HXXX套装/子目录/多皮肤)、道具ID规则、other_data键值格式（ui_show_res/ui_show_data/ui_show_skin 仅详情UI幻化三键）、皮肤指定消费链路、生成脚本与一键构建的完整步骤。目前仅适用于ArkReSpine这一个Mod，其他Mod流程可能不同。
watched_files:
  - Assets/Scripts/Bean/Game/CreatureBeanPartial.cs
  - Assets/Scripts/Component/Handler/CreatureHandler.cs
  - Assets/FrameWork/Scripts/Component/Handler/SpineHandler.cs
  - Assets/Scripts/Component/UI/Test/TestTransformPotionGUI.cs
  - Assets/Editor/ModBuildEditorWindow.cs
  - Assets/Scripts/Bean/MVC/Game/ItemsInfoBean.cs
  - Assets/Scripts/Enums/ItemsEnum.cs
  - .claude/scripts/gen_arkre_spine_mod.py
---

# ArkReSpine Mod（方舟幻化药）数据生成流程

## Mod 概述

- **Mod 名**：`ArkReSpine`（= 主游戏 `Mods/` 下的目录名，ModManager 按目录名发现/加载）
- **内容**：1437 个幻化药道具（ItemTypeEnum.TransformPotion=18）+ ArkRe 系列 spine 资源（341 个 SkeletonData）
- **MOD 项目**：独立于主游戏的 Unity 工程（与 AeonsEchoSpine 同一 MOD 工程，示例路径 `E:\Unity\DemonLordRoguelikeMod\DemonLordRoguelikeMod\DemonLordRoguelikeMod`，**以实际机器路径为准**）
- **与 AeonsEchoSpine 的核心差异**：① 资源按 `HXXX` 套装目录组织；② **全部幻化药只改 ui_show（详情UI高清展示），不动 show/world**；③ 单资源多皮肤时每套具名皮肤单独出一个幻化药（`ui_show_skin` 键指定皮肤）

## 资源约定（MOD 项目侧）

```
MOD项目/Assets/ModResource/Spine/ArkRe/
├── H001/                          - 资源套装（H+数字目录名=套装号，当前 H001~H811 共 202 个，非连续）
│   ├── H001/                      - 子目录（=套装名），内含完整 spine 导出物
│   │   ├── H001_CG_H001_a_SkeletonData.asset  ← 资源名=文件名去扩展名（保留 _SkeletonData）
│   │   └── H001_CG_H001_a.json/.atlas.txt/.png/_Atlas.asset/_Material.mat
│   └── H001_S/                    - 子目录（套装名+后缀），另一个资源
│       └── H001_S_SkeletonData.asset + ...
├── H002/
└── ...
```

- **套装目录名**必须匹配 `^H(\d+)$`（脚本 `SET_DIR_PATTERN`），不匹配跳过
- **资源名 = SkeletonData 资产文件名（不含扩展名，保留 `_SkeletonData` 后缀）**，如 `H001_CG_H001_a_SkeletonData`；构建器按此名登记 Addressables Address（`Path.GetFileNameWithoutExtension`），游戏侧 `SpineHandler.GetSkeletonDataAssetWithMod` 按此名精确匹配（大小写敏感）。**与 AeonsEchoSpine 约定完全一致**
- 同名 spine json = 文件名去 `_SkeletonData.asset` + `.json`（如 `H001_CG_H001_a.json`），生成脚本读其 `skins` 数组取皮肤列表
- **Spine JSON 格式**：这批资源无 `spine` 版本头、无旧格式 linkedmesh/分离约束数组（2026-09 全量 341 个 JSON 扫描确认），无需 AeonsEchoSpine 那批「伪 4.2」的格式转换。**新增资源套装时仍需校验**：若出现旧 linkedmesh（含 `"parent"`）或顶层 `"ik"/"transform"/"path"` 约束数组，须先做转换（参照 aeonsecho-spine-mod SKILL「Spine JSON 格式陷阱」节）

## 道具生成规则

- **组合**：套装内遍历资源（子目录自然序）→ 读 skins：
  - **多皮肤资源**：每套**具名皮肤**（≠default）单独出 1 个幻化药，`other_data` 带 `ui_show_skin:皮肤名` 键；**default 皮肤跳过不出药**（2026-09-24 与用户确认）
  - **仅 default 皮肤资源**：出 1 个幻化药，不带皮肤键（骨架默认皮肤即目标外观）
  - 套装内序号排序 = 资源（子目录自然序）→ 皮肤（skins 数组顺序）
- **道具自ID**：`18` + 套装号（HXXX 的数字部分，当前 001~811 均 3 位）+ `2位序号`（01 起）。例：H001 → 1800101~1800108；H120 → 1812001。与 AeonsEchoSpine 的号段（18+4位起套装号，最短 18110101）不冲突；套装号超 3 位时脚本告警（当前最大 H811，安全）
- **other_data 键值格式**（`&` 拆项、首个 `:` 拆键值，缺省键省略）——**只有 ui_show 三键，无 show_res/show_data/world_data**：
  ```
  ui_show_res:H001_CG_H001_a_SkeletonData&ui_show_data:0.1787;0,0&ui_show_skin:LV1
  ```
  - `ui_show_res`：ui_show_spine 高清展示资源名（详情UI，isUIShow=true 时使用）
  - `ui_show_data`：详情UI尺寸 `scale;x,y`；生成器按 `645/骨架高` 校准 scale（与 AeonsEchoSpine 同基准），默认位移 `0,0`；**scan 全量重建时按 id 保留手调值**（脚本 `read_preserved_ui_show_data`：同 id=同资源同皮肤，骨架未变手调仍有效；新增资源按骨架校准；`--reset-layout` 可强制全部重算，2026-09-27 起）
  - `ui_show_skin`：ui_show 资源内指定皮肤名（可空：仅 default 皮肤的资源省略=骨架默认皮肤）
- **name 自ID = 道具自ID**：指向 Mod 自带语言表同 id 行（`name[language]` 标记驱动 `CombineModReferenceIds`，与 AeonsEchoSpine 同机制）
- **固定字段**：item_type=18、num_max=1、icon_res=`Item_TransformPotion_1`、creature_model_id=0、reward_rarity=""、**source="1"**（=ItemSourceEnum.ConquerReward 征服模式奖励）
- **道具名**：具名皮肤药带皮肤后缀——cn「幻化药·方舟H001-01 LV1」/ tw「幻化藥·方舟H001-01 LV1」/ en「Ark Potion H001-01 LV1」；单 default 皮肤药无后缀「幻化药·方舟H120-01」（12 语言全生成）
- **Excel 独立**：ArkReSpine 的道具/语言 Excel 与 AeonsEchoSpine 相互独立（文件名带 modName 小写后缀，与主项目 `TestTransformPotionGUI.GetModItemsExcelRelPath` 约定一致）：
  - `Assets/Data/Excel/excel_mod_items_info_arkrespine[Mod道具信息-ArkReSpine].xlsx`
  - `Assets/Data/Excel/excel_mod_language_arkrespine[Mod多语言-ArkReSpine].xlsx`

## 主项目配套代码（消费侧）

| 机制 | 位置 |
|------|------|
| other_data 键值解析（含 ui_show_skin 键，6 出参） | `CreatureBeanPartial.ParseTransformOtherData`（`#region 幻化相关`） |
| 高清展示资源（ui_show_res 键） | `CreatureBeanPartial.GetTransformUIShowSpineRes` → `CreatureHandler.SetCreatureData`（isUIShow 分支独立判定、不依赖 show_res） |
| **指定皮肤（ui_show_skin 键）** | `CreatureBeanPartial.GetTransformUIShowSkin` → `CreatureHandler.SetCreatureData`：isUIShow 且有 ui_show_res 时取皮肤名，SkeletonAnimation/SkeletonGraphic 两分支在 `hasTransform` 且皮肤名非空时调 `SpineHandler.ChangeSkeletonSkin(Skeleton, string)` 按名整皮替换（**幻化换肤跳过规则的显式例外**；无皮肤键的幻化药行为不变=保持骨架默认皮肤） |
| 按名换肤 API | `SpineHandler.ChangeSkeletonSkin(Skeleton skeleton, string skinName)` 重载（FindSkin→SetSkin→SetupPoseSlots；皮肤缺失时 `SpineManager.GetSkeletonDataSkin` 报错并保持原皮肤） |
| 详情UI尺寸（ui_show_data 键） | `CreatureBeanPartial.GetTransformUIShowData` → `GameUIUtil.SetCreatureUIForDetails`；编辑器下测试覆盖层 `TransformPotionUITestOverride` 优先 |
| 调参预览+写回 | `TestTransformPotionGUI`（测试模式-卡片测试-Mod幻化药测试面板）：**保存按 modId 分组路由**——modId→modName（`ModManager.GetModJsonTextFileInfos`）→ 各 Mod 独立 Excel（`GetModItemsExcelRelPath` 约定）+ `Mods/{modName}/JsonText/ItemsInfo.txt` 两处直补 + 会话内存；`BuildOtherData` 保留 ui_show_skin 等键、show_res 空时省略 |
| Mod 道具/语言合并 | `BaseCfg.GetInitDataForMods` → id 拼接 + `BaseBean.CombineModReferenceIds`（同 AeonsEchoSpine） |
| Mod spine 资源加载 | `SpineHandler.GetSkeletonDataAssetWithMod`（同 AeonsEchoSpine） |

## 生成流程（完整步骤）

### 1. 生成配置（在主项目侧执行，两段式：Excel 为唯一真实源）

```powershell
# 一律走 run-python.ps1 包装（路径动态化，不写死）：
# 资源变更后全量重建（扫描→Excel→JsonText）：
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".claude/scripts/run-python.ps1" `
    ".claude/scripts/gen_arkre_spine_mod.py" all --mod-project "<MOD项目根目录>"
# 只改了 Excel 参数/改名后重导（不重新扫描资源）：
#   ... export --mod-project "<MOD项目根目录>"
```

**两段式流水线**（脚本 `.claude/scripts/gen_arkre_spine_mod.py`）：

- `scan`：扫描 ArkRe 资源套装 → 重建/合并 **MOD 项目的两张 Excel**（道具表**全量重建**但 **ui_show_data 手调值按 id 保留**；语言表**按 id 合并保留人工改名**）；覆盖前自动备份到 `MOD项目/ExcelBackup/`
- `export`：读两张 Excel → 导出 JsonText
- 无 migrate 子命令（本 Mod 自始即键值格式）

产出（写入 MOD 项目，属 Mod 包一部分）：
```
MOD项目/Mods/ArkReSpine/JsonText/
├── ItemsInfo.txt                    - 1437 个幻化药道具
└── Language_ItemsInfo_{cn,en,jp,kr,tw,de,fr,ru,es,br,pl,tr}.txt
```

可调参数（仅 scan 生效）：`--ui-scale-k 645`（详情UI缩放校准常数）、`--ui-pos-y 0`（详情UI默认Y偏移）。资源套装新增/变更后跑 `all`；只调道具参数/名字时改 Excel 后跑 `export`（**不要再 scan，否则道具表被重建覆盖**——ui_show_data 手调值虽按 id 保留，其余列改动会丢）。

### 2. 构建 Addressables 产物

**方式一（推荐）：主项目「游戏/Mod构建工具」（ModBuildEditorWindow）一键完成**——「选择 Mod」下拉直接选中 `ArkReSpine`（已构建过的 Mod 自动扫描进列表，构建方法按约定自动带出 `ArkReSpineModBuilder.BuildMod`，可按 Mod 覆盖；生成脚本路径按 Mod 名自动推导 `gen_arkre_spine_mod.py`）。

**方式二：在 MOD 项目的 Unity 编辑器里执行**，菜单：**工具/Mod/ArkReSpine/一键构建(同步分组+构建)**（`MOD项目/Assets/Editor/ArkReSpineModBuilder.cs`）：

1. 扫描 `Assets/ModResource/Spine/ArkRe` 全部 `SkeletonDataAsset` → 同步进 `Mod_ArkReSpine` 分组（Address=资产名；**PackSeparately** 每资源一个 bundle 按需加载）
2. **SkeletonData 缩放统一复位**（`ApplyUIShowSkeletonDataScale`，幂等）：ArkRe 全部资源都是 ui_show 系，scale 统一 `UIShowSkeletonDataScale=0.01f`——详情UI尺寸由道具 other_data 的 ui_show_data 键控制（无 AeonsEchoSpine 的 Chess 系 0.002 校准）
3. 临时把其他分组 `IncludeInBuild=false`（隔离构建），构建后自动恢复
4. 新增/复用 Profile `ArkReSpine`，构建/加载路径指向 `Mods/ArkReSpine` 并设为激活
5. 清理旧产物（**保留 JsonText 子目录**）→ `BuildPlayerContent()` 构建
6. **构建后自动把 catalog 三件套从引擎默认输出目录拷进 Mod 目录**
7. **自动导出 Excel→JsonText 并部署到主项目**（`ExportJsonTextAndDeploy`：调主项目 `run-python.ps1` + `gen_arkre_spine_mod.py export --deploy-main`；主项目根目录经菜单「工具/Mod/ArkReSpine/设置主项目根目录（自动导出部署用）」配置一次，EditorPrefs 键 `ArkReSpineModBuilder.MainProjectRoot` 独立于 AeonsEchoSpine 的）
8. 产物：`Mods/ArkReSpine/` 下 `catalog.bin` + `catalog.hash` + `settings.json` + `*.bundle`（341 个）+ `JsonText/`

### 3. 部署到主项目（一键构建已自动完成；本步仅手工补部署时用）

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".claude/scripts/run-python.ps1" `
    ".claude/scripts/gen_arkre_spine_mod.py" export --mod-project "<MOD项目根目录>" --deploy-main "<主项目根目录>"
```

## 验证清单（用户手动 Play，AI 不自动 Play）

1. 主项目 Unity 编译通过后，用 **LauncherTest** 场景启动（Mod 初始化已收口到 `BaseLauncher.Launch()` 首行 `InitializeAllModsSync()`）
2. 测试发放道具：`userData.AddBackpackItem(new ItemBean(modId*100000000000000L + 1800101))`（modId 用 `ModHandler.Instance.manager.GetModId("ArkReSpine")` 取）
3. 魔物管理（UICreatureManager）对生物使用幻化药 → 确认框显示道具名「幻化药·方舟H001-01 LV1」
4. 确认后：**卡片详情 UI 显示 ui_show 高清图的指定皮肤**（如 LV1_M）；**普通小卡/基地/战斗场景仍显示原生物形象**（本 Mod 药不动 show/world）
5. 同套装不同皮肤的药（如 H001-01 LV1 与 H001-02 LV1_M）详情 UI 形象应有差异
6. 吃幻原药恢复原形象；切语言（如 en）道具名显示「Ark Potion H001-01 LV1」
7. 也可用测试模式-卡片测试-Mod幻化药测试面板：Mod 筛选选 `ArkReSpine`，大卡列表逐页核对皮肤显示

## 注意与边界

- **本流程仅适用于 ArkReSpine**：其他 Mod 资源约定与道具规则可能不同，不套用本文档；AeonsEchoSpine 流程见 aeonsecho-spine-mod SKILL
- MOD 项目改动（资源/构建器脚本）不在本仓库 git 内，watched_files 只覆盖主项目侧消费代码与生成脚本
- 套装内组合数 >99 时 2 位序号溢出，生成器告警截断（当前最多 17 组合=H003，无风险）
- 幻化状态 `CreatureBean.transformItemId` 存拼接后完整 id：Mod 移除→配置查不到→详情UI 自动回落原形象（每 id 一次 LogError 属预期），幻原药仍可清残留
- **JsonText 缺失排查**：同 AeonsEchoSpine——Mod 目录只有 bundle+catalog 没有 JsonText 时，主游戏 Mod 初始化成功但道具合并不生效；诊断=`Mods/ArkReSpine/JsonText/ItemsInfo.txt` 是否存在；修复=补跑 `export --deploy-main`
- **调单个药的详情UI尺寸不用跑生成脚本**：测试面板拖拽/滚轮实时调，「保存全部修改」按 Mod 分组路由写回 arkrespine 的 Excel+两处 JsonText+内存（只换 ui_show_data 键，ui_show_skin 等其余键原样保留）；资源套装变更才走 scan/all 生成流程
