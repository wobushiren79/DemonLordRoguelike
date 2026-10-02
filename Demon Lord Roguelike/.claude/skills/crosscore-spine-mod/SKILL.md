---
name: crosscore-spine-mod
description: CrossCoreSpine Mod（交错战线幻化药）数据生成流程指南。使用此SKILL当需要为该Mod新增/重建幻化药道具、重新扫描CrossCore spine资源目录(数字目录两层/命名目录一层混合)、重新生成ItemsInfo/多语言JsonText、重新构建与部署Mod时。记录资源约定(数字目录+命名目录/每个本体SkeletonData一个药/effect系特效背景层跳过)、道具ID规则(数字目录=目录数字/命名目录=9000001起编号段)、other_data键值格式（ui_show_res/ui_show_data/ui_show_idle_anim 仅详情UI幻化三键）、生成脚本与一键构建的完整步骤。目前仅适用于CrossCoreSpine这一个Mod，其他Mod流程可能不同。
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
  - .claude/scripts/gen_crosscore_spine_mod.py
---

# CrossCoreSpine Mod（交错战线幻化药）数据生成流程

## Mod 概述

- **Mod 名**：`CrossCoreSpine`（= 主游戏 `Mods/` 下的目录名，ModManager 按目录名发现/加载）
- **内容**：422 个幻化药道具（ItemTypeEnum.TransformPotion=18）+ CrossCore（交错战线）系列 spine 资源（656 个 SkeletonData，其中 234 个 effect 系特效/背景层不导出，422 个本体各出 1 药）
- **MOD 项目**：独立于主游戏的 Unity 工程（与 AeonsEchoSpine/ArkReSpine/NikkeSpine/BrownDustSpine/GirlWarsSpine 同一 MOD 工程，示例路径 `E:\Unity\DemonLordRoguelikeMod\DemonLordRoguelikeMod\DemonLordRoguelikeMod`，**以实际机器路径为准**）
- **与其他 Mod 的核心差异**：① 两种目录形态混合——**数字目录**（107 个，`10010`~`6003003` 不连续 5~7 位，下含一层 skin_*/break_*/synchro_* 子目录）+ **命名目录**（169 个，acheron/alps/cg00010_intoxicatedshadow_spine 等，直接放 spine 导出物）；② 背景层跳过规则 = **effect 系**（`_effect_B/F`、`_effectB/F`、`_effect _B`、`-effect B` 等变体全算），不是 GirlWars 的 Back/Front/BG；③ 待机动画走**标准 pick_idle_anim 规则**（用户规则「动作都用idle」：idle 本身是标准待机候选 #1，绝大多数药不写键、框架自动播 idle）；④ 全部幻化药只改 ui_show（详情UI高清展示），不动 show/world

## 资源约定（MOD 项目侧）

```
MOD项目/Assets/ModResource/Spine/CrossCore/
├── 10010/                            - 数字资源目录（107 个：10010~6003003 不连续）
│   ├── skin_alps03/                      ← 皮肤子目录（skin_* 196 个，另 break_* 54 / synchro_* 3 / 零星）
│   │   ├── 10010_skin_Alps03_SkeletonData.asset  ← 本体（含同名 .json/.atlas.txt/.png/_Atlas.asset/_Material.mat）
│   │   └── ...
│   ├── skin_alps04/
│   └── skin_alps05/
├── 10070/
│   ├── skin_cinnabar03b/ ...             ← 皮肤拆分子目录（skin_* 一员）
│   └── break_cinnabar/                   ← 含本体 + 10070_break_Cinnabar_effectB/F_SkeletonData（特效层跳过）
├── acheron/                          - 命名目录（169 个，角色名或 cgXXXXX_名字 形态）
│   ├── Acheron_character_SkeletonData.asset  ← 本体（注意：png/atlas 名是 Acheron.png，与 SkeletonData 不同名，正常）
│   └── Acheron_effect_B_SkeletonData.asset   ← 特效层（跳过，不导出）
├── skadi/
│   ├── Skadi_character_SkeletonData.asset    ← 本体
│   ├── Skadi_effectB_SkeletonData.asset      ← 特效层变体（无第二个下划线，同样跳过）
│   └── Skadi_effectF_SkeletonData.asset      ← 特效层变体（跳过）
├── cg00010_intoxicatedshadow_spine/    - CG 命名目录（单 SkeletonData，照常出药）
└── feili/                            - 只有散装素材无 SkeletonData → 整目录不出药
```

- **数字目录名**必须匹配 `^(\d+)$`（脚本 `SET_DIR_PATTERN`），数字直接拼进道具 ID；**命名目录**（非纯数字）也出药，套装号走 9000001 起编号段
- **特效/背景层跳过规则**（用户规则 2026-09-29：「名字里带 _effect_ 的都是背景不用生成」，按意图覆盖全部 effect 变体）：资产名中 **effect 前有分隔符（`_`/`-`/空格）** 即视为特效层（脚本 `EFFECT_LAYER_PATTERN`、构建器 `IsEffectLayer`，两侧同规则）。实际命中形态：`_effect_B/F`（169 个，如 `Acheron_effect_B`）、`_effectB/F`（60 个，如 `Skadi_effectB`）、`_effect _B`（带空格，如 `Badlands_effect _B`）、`-effect B`/`-effectB`（如 `Rex-effect B`、`melody-effectB`），共 234 个。跳过=不出药+不同步 Addressables 分组不导出 bundle
- **资源名 = SkeletonData 资产文件名（不含扩展名，保留 `_SkeletonData` 后缀）**，如 `10010_skin_Alps03_SkeletonData`、`Acheron_character_SkeletonData`；构建器按此名登记 Addressables Address，游戏侧 `SpineHandler.GetSkeletonDataAssetWithMod` 按此名精确匹配（大小写敏感）。**与其他 Mod 约定完全一致**
- 同名 spine json = 文件名去 `_SkeletonData.asset` + `.json`（如 `Acheron_character.json`），生成脚本读其骨架高校准 ui_show_data；png/atlas 在命名目录常与 SkeletonData 不同名（`Acheron.png`），不影响（依赖由构建自动跟进）
- **Spine JSON 格式**：全部为真 4.3.26（2026-09-29 全量 423 个本体 JSON 扫描确认：无旧 linkedmesh、无顶层分离约束数组），主项目 spine-csharp 4.3.39 直接兼容，**无需格式转换**。脚本 scan 已内置格式校验（`check_spine_json_format`），新增资源若出现旧 linkedmesh（skins 内含 `"parent"`）或顶层 `"ik"/"transform"/"path"` 约束数组会告警，须先转换（参照 aeonsecho-spine-mod SKILL「Spine JSON 格式陷阱」节）
- **贴图必须 PMA（预乘 alpha）**：2026-09-29 首次接入全量校验——295 张本体图集 PNG 仅 2 张直通（`70400_skin_LycorisRadiata03c.png`、`CG0040_WholeFamilyHaveFun.png`，透明区非零 RGB 占比 5~6%），**全部 511 个材质 `_StraightAlphaInput=1`**（直通开关全开，PNG 已是 PMA 时仅编辑器内边缘偏暗、bundle 运行时无碍，但仍须关）。首次接入用一次性脚本 `TempCrossCorePmaFix`（菜单「工具/Mod/CrossCoreSpine/[一次性]PMA修正」，跑完即删）：PNG 抽样检测直通→rgb*=a 转 PMA（幂等）+ 全部非特效层材质关直通（`_StraightAlphaInput=0` + `DisableKeyword("_STRAIGHT_ALPHA_INPUT")`）。**新增资源入库时必须复查**两项：① PNG 透明区纯黑（Python+PIL 抽样 alpha=0 像素 RGB）；② 该图集全部 `*.mat` `_StraightAlphaInput: 0`。不符就先转 PMA 再构建（详见 browndust-spine-mod SKILL「注意与边界」事故记录 / 记忆 project_spine_mod_pma_requirement）

## 道具生成规则

- **组合**：顶层目录内递归遍历本体 SkeletonData（剔除特效层后按相对路径自然序），**每个本体出 1 个幻化药**；缺同名 json 的资源警告跳过。当前 422 个本体 = 422 个药
- **待机动画 = 标准 pick_idle_anim 规则**（用户规则「动作都用idle」，2026-09-29）：idle 本身是主项目标准待机候选（`idle,wait,idle1,wait1,stand`，`excel_spine_animation_state` id=10001）——**405 个骨架命中候选**（绝大多数靠精确 idle）→ **不生成键**，运行时框架候选解析自动播 idle；**9 个无候选但有含 idle 动画**（如 `10070_skin_Cinnabar03c` 的 `idle3`、`cg0078` 的 `idle2`）→ 生成 `ui_show_idle_anim` 键（保留原始大小写）；**8 个完全无 idle 动画**（只有 `click*`/`guochang`/`animation`，如 `30400_skin_Badlands05c`、`CG0078_TwoFairiesOFTheInkWish03`）→ 不生成键+警告（详情UI静态显示）。扫描结束打印 候选/替代/无idle 三档统计
- **道具自ID**（Mod JsonText 内的原始 id，运行时由 BaseCfg.CombineModId 拼 modId 前缀）：`18` + 套装号 + `2位序号`（01 起，套装内按相对路径自然序）。套装号：**数字目录=目录数字本身**（10010→`10010`、6003003→`6003003`，不定长）；**命名目录=9000001 起独立编号段按目录自然序分配**（`NAMED_SET_NUM_BASE=9000000`，9000001 > 数字目录最大值 6003003，物理防冲突；used_ids 另有冲突告警兜底）。例：目录 10010 第 1 个本体 → `181001001`；acheron（第 1 个命名目录）→ `18900000101`。套装内本体 >99 时 2 位序号溢出告警截断（当前最多 7，无风险）
- **编号段漂移警告**：命名目录编号按目录自然序分配，**新增命名目录会挤占后续编号导致 id 漂移**（同 id 不再等于同资源），ui_show_data 手调值按 id 保留会错位——新增命名目录后建议 `--reset-layout` 全量重算
- **other_data 键值格式**（`&` 拆项、首个 `:` 拆键值，缺省键省略）——**只有 ui_show 系键，无 show_res/show_data/world_data/ui_show_skin**：
  ```
  ui_show_res:10010_skin_Alps03_SkeletonData&ui_show_data:0.3152;0,0
  ui_show_res:Acheron_character_SkeletonData&ui_show_data:0.5304;0,0&ui_show_idle_anim:idle3   ← 仅替代动画资源带第三键
  ```
  - `ui_show_res`：ui_show_spine 高清展示资源名（详情UI，isUIShow=true 时使用）
  - `ui_show_data`：详情UI尺寸 `scale;x,y`；生成器按 `645/骨架高` 校准 scale（与其他 Mod 同基准），默认位移 `0,0`；**scan 全量重建时按 id 保留手调值**（`--reset-layout` 可强制重算）
  - `ui_show_idle_anim`：ui_show 骨架的替代待机动画名（仅 9 个无标准候选资源带，如 idle3；命中候选的 405 个不写=框架自动播 idle）
- **name 自ID = 道具自ID**：指向 Mod 自带语言表同 id 行（`name[language]` 标记驱动 `CombineModReferenceIds`，与其他 Mod 同机制）
- **固定字段**：item_type=18、num_max=1、icon_res=`Item_Potion_1`、creature_model_id=0、reward_rarity=""、**source="1"**（=ItemSourceEnum.ConquerReward 征服模式奖励）
- **道具名**：cn「幻化药·交错战线10010-01」/ tw「幻化藥·交錯戰線10010-01」/ en「CrossCore Potion 10010-01」（12 语言全生成；套装标签=数字目录号或命名目录名原样；套装内本体 >1 时全部带 `-NN` 序号，单本体不带）
- **Excel 独立**（与 `TestTransformPotionGUI.GetModItemsExcelRelPath` 约定一致）：
  - `Assets/Data/Excel/excel_mod_items_info_crosscorespine[Mod道具信息-CrossCoreSpine].xlsx`
  - `Assets/Data/Excel/excel_mod_language_crosscorespine[Mod多语言-CrossCoreSpine].xlsx`

## 主项目配套代码（消费侧）

与其他 Spine Mod 完全共用，无任何 Mod 特化代码：

| 机制 | 位置 |
|------|------|
| other_data 键值解析 | `CreatureBeanPartial.ParseTransformOtherData`（`#region 幻化相关`，返回 `TransformOtherData` 结构体，含 idle_anim/ui_show_idle_anim 键） |
| 高清展示资源（ui_show_res 键） | `CreatureBeanPartial.GetTransformUIShowSpineRes` → `CreatureHandler.SetCreatureData`（isUIShow 分支独立判定、不依赖 show_res） |
| **替代待机动画（ui_show_idle_anim 键）** | `CreatureBeanPartial.GetTransformUIShowIdleAnim` → `GameUIUtil.SetCreatureUIForDetails` 播放动画三级分支：该键非空→框架层按名直播（绕过 GetAnimNameAppoint 防误用）；ui_show_res 非空→框架候选（本 Mod 405 个药走这里自动播 idle）；否则原链路 |
| 详情UI尺寸（ui_show_data 键） | `CreatureBeanPartial.GetTransformUIShowData` → `GameUIUtil.SetCreatureUIForDetails`；编辑器下测试覆盖层 `TransformPotionUITestOverride` 优先 |
| 调参预览+写回 | `TestTransformPotionGUI`（测试模式-卡片测试-Mod幻化药测试面板）：保存按 modId 分组路由——各 Mod 独立 Excel（`GetModItemsExcelRelPath` 约定自动覆盖 CrossCoreSpine）+ `Mods/{modName}/JsonText/ItemsInfo.txt` 两处直补 + 会话内存 |
| Mod 道具/语言合并 | `BaseCfg.GetInitDataForMods` → id 拼接 + `BaseBean.CombineModReferenceIds` |
| Mod spine 资源加载 | `SpineHandler.GetSkeletonDataAssetWithMod` |
| Mod 构建工具 | `ModBuildEditorWindow`：下拉自动扫描（产物目录 ∪ `*ModBuilder.cs`），构建方法约定推导 `CrossCoreSpineModBuilder.BuildMod`，生成脚本路径推导 `gen_crosscore_spine_mod.py`——**均按约定自动生效，无需改代码** |

## 生成流程（完整步骤）

### 1. 生成配置（在主项目侧执行，两段式：Excel 为唯一真实源）

```powershell
# 一律走 run-python.ps1 包装（路径动态化，不写死）：
# 资源变更后全量重建（扫描→Excel→JsonText）：
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".claude/scripts/run-python.ps1" `
    ".claude/scripts/gen_crosscore_spine_mod.py" all --mod-project "<MOD项目根目录>"
# 只改了 Excel 参数/改名后重导（不重新扫描资源）：
#   ... export --mod-project "<MOD项目根目录>"
```

**两段式流水线**（脚本 `.claude/scripts/gen_crosscore_spine_mod.py`）：

- `scan`：扫描 CrossCore 资源目录 → 重建/合并 **MOD 项目的两张 Excel**（道具表**全量重建**但 **ui_show_data 手调值按 id 保留**；语言表**按 id 合并保留人工改名**）；覆盖前自动备份到 `MOD项目/ExcelBackup/`（滚动复用 .bak.1~3，只留最近 3 份）；**idle 动画检测**（标准 pick_idle_anim 规则，扫描结束打印 候选/替代/无idle 统计）；**Spine JSON 格式校验**（旧 linkedmesh/分离约束数组告警）
- `export`：读两张 Excel → 导出 JsonText

产出（写入 MOD 项目，属 Mod 包一部分）：
```
MOD项目/Mods/CrossCoreSpine/JsonText/
├── ItemsInfo.txt                    - 422 个幻化药道具
└── Language_ItemsInfo_{cn,en,jp,kr,tw,de,fr,ru,es,br,pl,tr}.txt
```

可调参数（仅 scan 生效）：`--ui-scale-k 645`（详情UI缩放校准常数）、`--ui-pos-y 0`（详情UI默认Y偏移）、`--reset-layout`（强制重算手调值）。资源目录新增/变更后跑 `all`；只调道具参数/名字时改 Excel 后跑 `export`（**不要再 scan，否则道具表被重建覆盖**——ui_show_data 手调值虽按 id 保留，其余列改动会丢）。

### 2. 构建 Addressables 产物

**方式一（推荐）：主项目「游戏/Mod构建工具」（ModBuildEditorWindow）一键完成**——「选择 Mod」下拉选中 `CrossCoreSpine`（构建器脚本存在即自动入列，构建方法按约定自动带出 `CrossCoreSpineModBuilder.BuildMod`，生成脚本路径自动推导 `gen_crosscore_spine_mod.py`）。**前置：MOD 项目不能被另一个 Unity 实例打开（Temp/UnityLockfile 被进程占用才拦截；崩溃/强杀残留的锁文件由构建工具自动清理，无需手动删除）**。

**方式二：在 MOD 项目的 Unity 编辑器里执行**，菜单：**工具/Mod/CrossCoreSpine/一键构建(同步分组+构建)**（`MOD项目/Assets/Editor/CrossCoreSpineModBuilder.cs`）：

1. 扫描 `Assets/ModResource/Spine/CrossCore` 全部 `SkeletonDataAsset` → **剔除 effect 系特效/背景层** → 同步进 `Mod_CrossCoreSpine` 分组（Address=资产名；**PackSeparately** 每资源一个 bundle 按需加载）
2. **SkeletonData 缩放统一复位**（`ApplyUIShowSkeletonDataScale`，幂等，同样跳过特效层）：CrossCore 全部资源都是 ui_show 系，scale 统一 `0.01f`——详情UI尺寸由道具 other_data 的 ui_show_data 键控制
3. 临时把其他分组 `IncludeInBuild=false`（隔离构建），构建后自动恢复
4. 新增/复用 Profile `CrossCoreSpine`，构建/加载路径指向 `Mods/CrossCoreSpine` 并设为激活
5. 清理旧产物（**保留 JsonText 子目录**）→ `BuildPlayerContent()` 构建
6. **构建后自动把 catalog 三件套从引擎默认输出目录拷进 Mod 目录**
7. **自动导出 Excel→JsonText 并部署到主项目**（`ExportJsonTextAndDeploy`：调主项目 `run-python.ps1` + `gen_crosscore_spine_mod.py export --deploy-main`；主项目根目录经菜单「工具/Mod/CrossCoreSpine/设置主项目根目录（自动导出部署用）」配置一次，EditorPrefs 键 `CrossCoreSpineModBuilder.MainProjectRoot` 独立于其他 Mod 的）
8. 产物：`Mods/CrossCoreSpine/` 下 `catalog.bin` + `catalog.hash` + `settings.json` + `*.bundle`（422 个）+ `JsonText/`

**首次构建前必做**：先跑「工具/Mod/CrossCoreSpine/[一次性]PMA修正(PNG转PMA+材质关直通)」（`TempCrossCorePmaFix.cs`），确认日志后删除该临时脚本——否则材质直通开关全开（编辑器内边缘偏暗），且 2 张直通 PNG 可能白边。

### 3. 部署到主项目（一键构建已自动完成；本步仅手工补部署时用）

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".claude/scripts/run-python.ps1" `
    ".claude/scripts/gen_crosscore_spine_mod.py" export --mod-project "<MOD项目根目录>" --deploy-main "<主项目根目录>"
```

## 验证清单（用户手动 Play，AI 不自动 Play）

1. 主项目 Unity 编译通过后，用 **LauncherTest** 场景启动（Mod 初始化已收口到 `BaseLauncher.Launch()` 首行 `InitializeAllModsSync()`）
2. 测试发放道具：`userData.AddBackpackItem(new ItemBean(modId*100000000000000L + 181001001))`（modId 用 `ModHandler.Instance.manager.GetModId("CrossCoreSpine")` 取）
3. 魔物管理（UICreatureManager）对生物使用幻化药 → 确认框显示道具名「幻化药·交错战线10010-01」
4. 确认后：**卡片详情 UI 显示 ui_show 高清图且播放 idle 待机动画**；**普通小卡/基地/战斗场景仍显示原生物形象**（本 Mod 药不动 show/world）
5. **核对无白边**（PMA 修正生效；若全体白色描边=贴图直通 alpha 混入，按「贴图必须 PMA」节复查）
6. 吃幻原药恢复原形象；切语言（如 en）道具名显示「CrossCore Potion 10010-01」
7. 也可用测试模式-卡片测试-Mod幻化药测试面板：Mod 筛选选 `CrossCoreSpine`，大卡列表逐页核对形象显示（重点核对 8 个无 idle 药=静态显示、9 个替代动画药播 idle2/idle3 等）

## 注意与边界

- **本流程仅适用于 CrossCoreSpine**：其他 Mod 资源约定与道具规则可能不同，不套用本文档；AeonsEchoSpine/ArkReSpine/NikkeSpine/BrownDustSpine/GirlWarsSpine 流程见各自 SKILL
- MOD 项目改动（资源/构建器脚本）不在本仓库 git 内，watched_files 只覆盖主项目侧消费代码与生成脚本
- 幻化状态 `CreatureBean.transformItemId` 存拼接后完整 id：Mod 移除→配置查不到→详情UI 自动回落原形象（每 id 一次 LogError 属预期），幻原药仍可清残留
- **JsonText 缺失排查**：同其他 Mod——Mod 目录只有 bundle+catalog 没有 JsonText 时，主游戏 Mod 初始化成功但道具合并不生效；诊断=`Mods/CrossCoreSpine/JsonText/ItemsInfo.txt` 是否存在；修复=补跑 `export --deploy-main`
- **调单个药的详情UI尺寸不用跑生成脚本**：测试面板拖拽/滚轮实时调，「保存全部修改」按 Mod 分组路由写回 crosscorespine 的 Excel+两处 JsonText+内存（只换 ui_show_data 键）；资源目录变更才走 scan/all 生成流程
- **「Material is missing texture」排查**：与 NikkeSpine 同款——主项目运行报此错时按序排查：① UnityPy 看 bundle 内有无 Texture2D；② bundle 哈希大小与上次构建一致=可复现；③ 多半是 MOD 项目 Library 的 PNG 导入 artifact 损坏（编辑器里同样显示不出）；④ 修复=对受害 png 强制重导（`AssetDatabase.ImportAsset(path, ForceSynchronousImport|ForceUpdate)`）再重跑一键构建；⑤ `GetDependencies` 走查仍含坏 png（GUID 引用在），构建不报错、静默打出无贴图 bundle，只能靠结果校验（bundle/源png 大小比 <0.5 即缺贴图）（详见 nikke-spine-mod SKILL 2026-09-29 实战记录）
