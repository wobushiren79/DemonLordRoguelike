---
name: putgirl-spine-mod
description: PutGirlSpine Mod（放置少女幻化药）数据生成流程指南。使用此SKILL当需要为该Mod新增/重建幻化药道具、重新扫描PutGirl spine资源目录(拼音目录)、重新生成ItemsInfo/多语言JsonText、重新构建与部署Mod时。记录资源约定(拼音目录/每个SkeletonData一个药/待机动画Stand已入主项目标准候选)、道具ID规则、other_data键值格式（ui_show_res/ui_show_data 仅详情UI幻化两键）、生成脚本与一键构建的完整步骤。目前仅适用于PutGirlSpine这一个Mod，其他Mod流程可能不同。
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
  - .claude/scripts/gen_putgirl_spine_mod.py
---

# PutGirlSpine Mod（放置少女幻化药）数据生成流程

## Mod 概述

- **Mod 名**：`PutGirlSpine`（= 主游戏 `Mods/` 下的目录名，ModManager 按目录名发现/加载）
- **内容**：332 个幻化药道具（ItemTypeEnum.TransformPotion=18）+ PutGirl（放置少女）系列 spine 资源（332 个 SkeletonData，每拼音目录恰好 1 个本体）
- **MOD 项目**：独立于主游戏的 Unity 工程（与其他 Spine Mod 同一 MOD 工程，示例路径 `E:\Unity\DemonLordRoguelikeMod\DemonLordRoguelikeMod\DemonLordRoguelikeMod`，**以实际机器路径为准**）
- **与其他 Mod 的核心差异**：① 资源按**拼音目录**组织（aboluo/amina/...，`^[a-z0-9_]+$`）；② 资源命名统一为 `<目录名>_skeleton_SkeletonData`；③ **待机动画=`Stand`**——主项目标准待机候选（excel_spine_animation_state id=10001）已于 2026-09-29 加入 `stand`（用户确认该名通用），全部骨架命中框架候选解析，**不生成 ui_show_idle_anim 键**（other_data 只有 ui_show_res/ui_show_data 两键，同 NikkeSpine 本体类资源）

## 资源约定（MOD 项目侧）

```
MOD项目/Assets/ModResource/Spine/PutGirl/
├── aboluo/                        - 拼音资源目录（小写字母/数字/下划线，332 个）
│   ├── aboluo_skeleton_SkeletonData.asset ← 本体，含同名 .json/.atlas.txt/.png/_Atlas.asset/_Material.mat（部分目录多页图集=多 png/多 mat）
│   └── ...
├── amina/
└── zzmxcl/
```

- **资源目录名**必须匹配 `^[a-z0-9_]+$`（脚本 `SET_DIR_PATTERN`），不匹配跳过；空目录（无 SkeletonData）跳过
- **资源名 = SkeletonData 资产文件名（不含扩展名，保留 `_SkeletonData` 后缀）**，如 `aboluo_skeleton_SkeletonData`；构建器按此名登记 Addressables Address，游戏侧 `SpineHandler.GetSkeletonDataAssetWithMod` 按此名精确匹配（大小写敏感）。**与其他 Mod 约定完全一致**
- 同名 spine json = 文件名去 `_SkeletonData.asset` + `.json`（如 `aboluo_skeleton.json`），生成脚本读其骨架高校准 ui_show_data
- **贴图必须 PMA**：2026-09-29 接入时已一次性转换（667 PNG rgb*=a + 698 材质 `_StraightAlphaInput: 0`，原始资源是直通 alpha，同 BrownDust/GirlWars 事故资源形态）；**新增资源入库必须复查** PNG 透明区纯黑 + 全部 `*.mat`（含多页图集）`_StraightAlphaInput: 0`（详见 `project_spine_mod_pma_requirement` 记忆 / browndust-spine-mod SKILL 事故记录）
- **Spine JSON 格式**：全部为真 4.3.26（2026-09-29 全量 332 个 JSON 扫描确认：无旧 linkedmesh、无顶层分离约束数组、全部含 `Stand` 动画、全部单皮肤），主项目 spine-csharp 4.3.39 直接兼容，**无需格式转换**。**新增资源时仍需校验**（脚本 scan 已内置校验告警）

## 道具生成规则

- **组合**：目录内遍历 SkeletonData（文件名自然序），**每个 SkeletonData 出 1 个幻化药**；缺同名 json 的资源警告跳过（当前每目录恰好 1 个本体）
- **道具自ID**（Mod JsonText 内的原始 id，运行时由 BaseCfg.CombineModId 拼 modId 前缀）：`18` + `4位目录序号`（0001 起，按目录名自然序分配）+ `2位序号`（01 起，目录内按文件名自然序）。例：aboluo 目录（序号0001）→ 18000101。**注意**：新增目录插入自然序中间会重排后续目录的序号（同 OtherSpine 既有规则），重排后旧 id 指向的资源变——目录集有增删时务必全量重建并知会重新调参
- **other_data 键值格式**（`&` 拆项、首个 `:` 拆键值，缺省键省略）——**只有 ui_show 系两键**：
  ```
  ui_show_res:aboluo_skeleton_SkeletonData&ui_show_data:0.6089;0,0
  ```
  - `ui_show_res`：ui_show_spine 高清展示资源名（详情UI，isUIShow=true 时使用）
  - `ui_show_data`：详情UI尺寸 `scale;x,y`；生成器按 `645/骨架高` 校准 scale（与其他 Mod 同基准），默认位移 `0,0`；**scan 全量重建时按 id 保留手调值**（`--reset-layout` 可强制重算）
  - `ui_show_idle_anim`：**当前不生成**——全部 332 骨架含 `Stand`，已命中主项目标准待机候选（id=10001 候选 `idle,wait,idle1,wait1,stand`，2026-09-29 起）；scan 仍按 mod-system SKILL 通用规则动态检测兜底（无标准候选时自动取首个含 idle 动画名写键+统计）
- **name 自ID = 道具自ID**：指向 Mod 自带语言表同 id 行（`name[language]` 标记驱动 `CombineModReferenceIds`，与其他 Mod 同机制）
- **固定字段**：item_type=18、num_max=1、icon_res=`Item_TransformPotion_1`、creature_model_id=0、reward_rarity=""、**source="1"**（=ItemSourceEnum.ConquerReward 征服模式奖励）
- **道具名**：cn「幻化药·放置少女aboluo」/ tw「幻化藥·放置少女aboluo」/ en「PutGirl Potion aboluo」（12 语言全生成；2026-09-29 与用户确认「放置少女+拼音」方案，人工改名在语言 Excel 进行、scan 按 id 合并保留）
- **Excel 独立**（与 `TestTransformPotionGUI.GetModItemsExcelRelPath` 约定一致）：
  - `Assets/Data/Excel/excel_mod_items_info_putgirlspine[Mod道具信息-PutGirlSpine].xlsx`
  - `Assets/Data/Excel/excel_mod_language_putgirlspine[Mod多语言-PutGirlSpine].xlsx`

## 主项目配套代码（消费侧）

与其他 Spine Mod 完全共用，无任何 Mod 特化代码：

| 机制 | 位置 |
|------|------|
| other_data 键值解析 | `CreatureBeanPartial.ParseTransformOtherData`（`#region 幻化相关`，返回 `TransformOtherData` 结构体） |
| 高清展示资源（ui_show_res 键） | `CreatureBeanPartial.GetTransformUIShowSpineRes` → `CreatureHandler.SetCreatureData`（isUIShow 分支独立判定、不依赖 show_res） |
| 待机动画 | 本 Mod 不生成 idle 键——框架 `SpineAnimationStateCfg.CheckSpineAnim` 按 id=10001 候选（含 `stand`）大小写不敏感匹配骨架 `Stand` |
| 详情UI尺寸（ui_show_data 键） | `CreatureBeanPartial.GetTransformUIShowData` → `GameUIUtil.SetCreatureUIForDetails`；编辑器下测试覆盖层 `TransformPotionUITestOverride` 优先 |
| 调参预览+写回 | `TestTransformPotionGUI`（测试模式-卡片测试-Mod幻化药测试面板）：保存按 modId 分组路由——各 Mod 独立 Excel（`GetModItemsExcelRelPath` 约定自动覆盖 PutGirlSpine）+ `Mods/{modName}/JsonText/ItemsInfo.txt` 两处直补 + 会话内存 |
| Mod 道具/语言合并 | `BaseCfg.GetInitDataForMods` → id 拼接 + `BaseBean.CombineModReferenceIds` |
| Mod spine 资源加载 | `SpineHandler.GetSkeletonDataAssetWithMod` |
| Mod 构建工具 | `ModBuildEditorWindow`：下拉自动扫描（产物目录 ∪ `*ModBuilder.cs`），构建方法约定推导 `PutGirlSpineModBuilder.BuildMod`，生成脚本路径推导 `gen_putgirl_spine_mod.py`——**均按约定自动生效，无需改代码** |

## 生成流程（完整步骤）

### 1. 生成配置（在主项目侧执行，两段式：Excel 为唯一真实源）

```powershell
# 一律走 run-python.ps1 包装（路径动态化，不写死）：
# 资源变更后全量重建（扫描→Excel→JsonText）：
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".claude/scripts/run-python.ps1" `
    ".claude/scripts/gen_putgirl_spine_mod.py" all --mod-project "<MOD项目根目录>"
# 只改了 Excel 参数/改名后重导（不重新扫描资源）：
#   ... export --mod-project "<MOD项目根目录>"
```

**两段式流水线**（脚本 `.claude/scripts/gen_putgirl_spine_mod.py`）：

- `scan`：扫描 PutGirl 资源目录 → 重建/合并 **MOD 项目的两张 Excel**（道具表**全量重建**但 **ui_show_data 手调值按 id 保留**；语言表**按 id 合并保留人工改名**）；覆盖前自动备份到 `MOD项目/ExcelBackup/`（滚动复用 .bak.1~3，只留最近 3 份）；**idle 动画检测**（每个 SkeletonData 的同名 json 读 animations → 命中标准候选省略键，无候选取首个含 idle 动画名写 `ui_show_idle_anim` 键，扫描结束打印 命中/替代/无idle 统计）
- `export`：读两张 Excel → 导出 JsonText

产出（写入 MOD 项目，属 Mod 包一部分）：
```
MOD项目/Mods/PutGirlSpine/JsonText/
├── ItemsInfo.txt                    - 332 个幻化药道具
└── Language_ItemsInfo_{cn,en,jp,kr,tw,de,fr,ru,es,br,pl,tr}.txt
```

可调参数（仅 scan 生效）：`--ui-scale-k 645`（详情UI缩放校准常数）、`--ui-pos-y 0`（详情UI默认Y偏移）、`--reset-layout`（强制重算手调值）。资源目录新增/变更后跑 `all`；只调道具参数/名字时改 Excel 后跑 `export`（**不要再 scan，否则道具表被重建覆盖**）。

### 2. 构建 Addressables 产物

**方式一（推荐）：主项目「游戏/Mod构建工具」（ModBuildEditorWindow）一键完成**——「选择 Mod」下拉选中 `PutGirlSpine`（构建器脚本存在即自动入列，构建方法按约定自动带出 `PutGirlSpineModBuilder.BuildMod`，生成脚本路径自动推导 `gen_putgirl_spine_mod.py`）。**前置：MOD 项目不能被另一个 Unity 实例打开（Temp/UnityLockfile 被进程占用才拦截；崩溃/强杀残留的锁文件由构建工具自动清理，无需手动删除）**。

**方式二：在 MOD 项目的 Unity 编辑器里执行**，菜单：**工具/Mod/PutGirlSpine/一键构建(同步分组+构建)**（`MOD项目/Assets/Editor/PutGirlSpineModBuilder.cs`）：

1. 扫描 `Assets/ModResource/Spine/PutGirl` 全部 `SkeletonDataAsset` → 同步进 `Mod_PutGirlSpine` 分组（Address=资产名；**PackSeparately** 每资源一个 bundle 按需加载）
2. **SkeletonData 缩放统一复位**（`ApplyUIShowSkeletonDataScale`，幂等）：PutGirl 全部资源都是 ui_show 系，scale 统一 `0.01f`
3. 临时把其他分组 `IncludeInBuild=false`（隔离构建），构建后自动恢复
4. 新增/复用 Profile `PutGirlSpine`，构建/加载路径指向 `Mods/PutGirlSpine` 并设为激活
5. 清理旧产物（**保留 JsonText 子目录**）→ `BuildPlayerContent()` 构建
6. **构建后自动把 catalog 三件套从引擎默认输出目录拷进 Mod 目录**
7. **自动导出 Excel→JsonText 并部署到主项目**（`ExportJsonTextAndDeploy`：调主项目 `run-python.ps1` + `gen_putgirl_spine_mod.py export --deploy-main`；主项目根目录经菜单「工具/Mod/PutGirlSpine/设置主项目根目录（自动导出部署用）」配置一次，EditorPrefs 键 `PutGirlSpineModBuilder.MainProjectRoot` 独立于其他 Mod 的）
8. 产物：`Mods/PutGirlSpine/` 下 `catalog.bin` + `catalog.hash` + `settings.json` + `*.bundle`（332 个）+ `JsonText/`

### 3. 部署到主项目（一键构建已自动完成；本步仅手工补部署时用）

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".claude/scripts/run-python.ps1" `
    ".claude/scripts/gen_putgirl_spine_mod.py" export --mod-project "<MOD项目根目录>" --deploy-main "<主项目根目录>"
```

## 验证清单（用户手动 Play，AI 不自动 Play）

1. 主项目 Unity 编译通过后，用 **LauncherTest** 场景启动（Mod 初始化已收口到 `BaseLauncher.Launch()` 首行 `InitializeAllModsSync()`）
2. 测试发放道具：`userData.AddBackpackItem(new ItemBean(modId*100000000000000L + 18000101))`（modId 用 `ModHandler.Instance.manager.GetModId("PutGirlSpine")` 取）
3. 魔物管理（UICreatureManager）对生物使用幻化药 → 确认框显示道具名「幻化药·放置少女aboluo」
4. 确认后：**卡片详情 UI 显示 ui_show 高清图**（待机应为 `Stand` 动画循环）；**普通小卡/基地/战斗场景仍显示原生物形象**（本 Mod 药不动 show/world）
5. 不同拼音目录的药详情 UI 形象应有差异；吃幻原药恢复原形象；切语言（如 en）道具名显示「PutGirl Potion aboluo」
6. 也可用测试模式-卡片测试-Mod幻化药测试面板：Mod 筛选选 `PutGirlSpine`，大卡列表逐页核对形象显示

## 注意与边界

- **本流程仅适用于 PutGirlSpine**：其他 Mod 资源约定与道具规则可能不同，不套用本文档；各 Mod 流程见各自 SKILL
- MOD 项目改动（资源/构建器脚本）不在本仓库 git 内，watched_files 只覆盖主项目侧消费代码与生成脚本
- 幻化状态 `CreatureBean.transformItemId` 存拼接后完整 id：Mod 移除→配置查不到→详情UI 自动回落原形象（每 id 一次 LogError 属预期），幻原药仍可清残留
- **JsonText 缺失排查**：同其他 Mod——Mod 目录只有 bundle+catalog 没有 JsonText 时，主游戏 Mod 初始化成功但道具合并不生效；诊断=`Mods/PutGirlSpine/JsonText/ItemsInfo.txt` 是否存在；修复=补跑 `export --deploy-main`
- **调单个药的详情UI尺寸不用跑生成脚本**：测试面板拖拽/滚轮实时调，「保存全部修改」按 Mod 分组路由写回 putgirlspine 的 Excel+两处 JsonText+内存（只换 ui_show_data 键）；资源目录变更才走 scan/all 生成流程
- **目录序号重排风险**：道具自ID 含「按目录名自然序分配的 4 位目录序号」——新增/删除目录会改变后续目录的序号，旧 id 指向的资源随之变化；目录集有增删时务必全量重建（`all`），并知会 ui_show_data 手调值需重核
- **「Material is missing texture」排查**：同 NikkeSpine 实战记录（见 nikke-spine-mod SKILL 注意与边界）——bundle/源png 大小比 <0.5 即缺贴图，对受害 png 强制重导后重跑一键构建
