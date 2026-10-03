---
name: browndust-spine-mod
description: BrownDustSpine Mod（棕尘幻化药）数据生成流程指南。使用此SKILL当需要为该Mod新增/重建幻化药道具、重新扫描BrownDust spine资源目录(char_XXXXXX与illust/npc等)、重新生成ItemsInfo/多语言JsonText、重新构建与部署Mod时。记录资源约定(char目录+非char目录900001编号段)、按动画名拆药规则（idle/all/loop/cut 每个匹配动画一个药、四类独立判定不去重）、道具ID规则、other_data键值格式（ui_show_res/ui_show_data/ui_show_idle_anim）、生成脚本与一键构建的完整步骤。目前仅适用于BrownDustSpine这一个Mod，其他Mod流程可能不同。
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
  - .claude/scripts/gen_browndust_spine_mod.py
---

# BrownDustSpine Mod（棕尘幻化药）数据生成流程

## Mod 概述

- **Mod 名**：`BrownDustSpine`（= 主游戏 `Mods/` 下的目录名，ModManager 按目录名发现/加载）
- **内容**：791 个幻化药道具（ItemTypeEnum.TransformPotion=18）+ BrownDust 系列 spine 资源（290 个 SkeletonData）
- **MOD 项目**：独立于主游戏的 Unity 工程（与其他 Spine Mod 同一 MOD 工程，示例路径 `E:\Unity\DemonLordRoguelikeMod\DemonLordRoguelikeMod\DemonLordRoguelikeMod`，**以实际机器路径为准**）
- **与其他 Mod 的核心差异**：① 出药规则=**按动画名拆药**（idle/all/loop/cut 每个匹配动画一个药，四类独立判定不去重）；② 目录两类编号（char 目录数字 + 非 char 目录 900001 编号段）；③ 全部资源只有 default 一套皮肤不按皮肤拆药；④ 全部幻化药只改 ui_show（同 ArkRe/Nikke）

## 资源约定（MOD 项目侧）

```
MOD项目/Assets/ModResource/Spine/BrownDust/
├── char_000201/                   - 角色目录（char_+6位数字，共 199 个有效）
│   └── 000201/                    - 本体子目录，含完整 spine 导出物
│       └── 000201_SkeletonData.asset + .json/.atlas.txt/.png/_Atlas.asset/_Material.mat
├── char_000202/
│   ├── 000202/                    - 本体
│   └── 000202_cutscene/           - cutscene 特写子目录（可选），另一套独立骨架
├── illust_dating1/                - 非 char 目录（约会插画）：1 个 SkeletonData
├── illust_special7_1/             - 非 char 目录（特殊插画）
├── npc000001/                     - 非 char 目录（NPC）
└── storypack2_2/                  - 非 char 目录（剧情包）
```

- **char_XXXXXX 目录**：目录名匹配 `^char_(\d+)$`，数字部分（6 位）进道具 ID；`char_000402` 无 spine 资源跳过
- **非 char 目录**（illust_dating/illust_special/illust_talk/npc/specialIllust/storypack 等，共 66 个）：各 1 个 SkeletonData，**也出药**（2026-09-28 与用户确认），套装号=独立编号段 **900001 起按目录自然序递增分配**（`NON_CHAR_SET_NUM_BASE=900000`；char 数字最大 099999 不冲突；新增非 char 目录自然顺延、已有目录编号不变=id 不漂移）
- **资源名 = SkeletonData 资产文件名（不含扩展名，保留 `_SkeletonData` 后缀）**，如 `000202_cutscene_SkeletonData`；构建器按此名登记 Addressables Address，游戏侧 `SpineHandler.GetSkeletonDataAssetWithMod` 按此名精确匹配（大小写敏感）
- 同名 spine json = 文件名去 `_SkeletonData.asset` + `.json`
- **皮肤**：全量 290 个 JSON 扫描确认全部仅 `default` 一套皮肤（2026-09），不带 `ui_show_skin` 键
- **贴图必须 PMA（预乘 alpha）**：所有图集 PNG 透明区（alpha=0）必须为纯黑、材质 `Straight Alpha Texture` 开关必须关闭（`_StraightAlphaInput=0`）。**严禁直通 alpha 资源入库**——直通贴图依赖 `Spine/Skeleton` shader 的 `_STRAIGHT_ALPHA_INPUT` 变体，该变体是 `shader_feature`（构建裁剪），MOD 工程编辑器内按需编译显示正常，但 bundle 里只带默认 PMA 变体 → 主工程运行时回退默认变体 → 直通贴图被按 PMA 加算 → **全体白边**（2026-09-29 实发事故，详见「注意与边界」）。多页图集注意：次级页材质命名为 `{图集}_{页}.mat`（非 `*_Material.mat`），检查/批处理必须覆盖全部 `*.mat`
- **Spine JSON 格式**：真 4.3.x（与其他 Mod 同批），无旧格式转换需求；新增资源仍需校验旧 linkedmesh/分离约束数组（参照 aeonsecho-spine-mod SKILL）

## 道具生成规则（按动画名拆药，2026-09-28 与用户确认）

- **出药**：每个 SkeletonData 的动画列表按四类标签独立判定（**不去重**）：
  | 标签 | 匹配（动画名小写含子串） | 当前出药数 |
  |---|---|---|
  | idle 系 | `idle` | 302 |
  | all 系 | `all` | 58 |
  | loop 系 | `loop` | 63 |
  | cut 系 | `cut` | 368 |
  - 每个匹配动画各出 1 个幻化药，`ui_show_idle_anim` 键=对应动画名（保留大小写）
  - **同一动画多重匹配在各系分别出药**：如 `3_cut_idle`（含 idle+cut）出 idle 系药+cut 系药共 2 个、`cut_all`（含 all+cut）出 2 个
  - 骨架内序号排序 = idle 系（动画名自然序）→ all 系 → loop 系 → cut 系
  - **0 匹配动画的骨架**兜底出 1 个药（不带 idle_anim 键，静态显示）+警告——当前全量扫描**不存在**此类骨架（0 个）
- **道具自ID**：`18` + 套装号（char 的 6 位数字 / 非 char 900001+）+ `2位序号`（01 起）。套装内序号跨骨架连续（本体子目录→cutscene 子目录）。例：char_000202 → 1800020201（本体 idle）/1800020202~06（cutscene cut_1~cut_5）；illust_dating1 → 1890000101~03（idle1/idle2/idle3）
- **other_data 键值格式**（`&` 拆项、首个 `:` 拆键值，缺省键省略）——**只有 ui_show 系键**：
  ```
  ui_show_res:000202_cutscene_SkeletonData&ui_show_data:0.2735;0,0&ui_show_idle_anim:cut_1
  ```
  - `ui_show_res`：ui_show_spine 高清展示资源名
  - `ui_show_data`：详情UI尺寸 `scale;x,y`；生成器按 `645/骨架高` 校准（同骨架全部药共用校准值，手调按药 id 各自保留）；scan 全量重建时按 id 保留手调值（`--reset-layout` 强制重算）
  - `ui_show_idle_anim`：该药对应的目标动画名（按动画拆药必带；0 匹配兜底药省略）
- **name 自ID = 道具自ID**（`name[language]` 标记驱动 `CombineModReferenceIds`，与其他 Mod 同机制）
- **固定字段**：item_type=18、num_max=1、icon_res=`Item_Potion_1`、creature_model_id=0、reward_rarity=""、**source="1"**（征服模式奖励）
- **道具名**（带**动画名后缀**，玩家可区分同 char 的不同动画药）：cn「幻化药·棕尘char_000202-02 cut_1」/ tw「幻化藥·棕塵…」/ en「BrownDust Potion char_000202-02 cut_1」（12 语言全生成；0 匹配兜底药无后缀）
- **Excel 独立**（与 `TestTransformPotionGUI.GetModItemsExcelRelPath` 约定一致）：
  - `Assets/Data/Excel/excel_mod_items_info_browndustspine[Mod道具信息-BrownDustSpine].xlsx`
  - `Assets/Data/Excel/excel_mod_language_browndustspine[Mod多语言-BrownDustSpine].xlsx`

## 主项目配套代码（消费侧）

与其他 Spine Mod 完全共用，无任何 Mod 特化代码：

| 机制 | 位置 |
|------|------|
| other_data 键值解析 | `CreatureBeanPartial.ParseTransformOtherData`（`#region 幻化相关`，返回 `TransformOtherData` 结构体） |
| 高清展示资源（ui_show_res 键） | `CreatureBeanPartial.GetTransformUIShowSpineRes` → `CreatureHandler.SetCreatureData`（isUIShow 分支独立判定） |
| **替代待机动画（ui_show_idle_anim 键）** | `CreatureBeanPartial.GetTransformUIShowIdleAnim` → `GameUIUtil.SetCreatureUIForDetails` 播放动画三级分支：该键非空→框架层按名直播——**本 Mod 全部药（除 0 匹配兜底）都带此键，详情UI 循环播放对应 idle/all/loop/cut 动画** |
| 详情UI尺寸（ui_show_data 键） | `CreatureBeanPartial.GetTransformUIShowData` → `GameUIUtil.SetCreatureUIForDetails`；测试覆盖层优先 |
| 调参预览+写回 | `TestTransformPotionGUI`：保存按 modId 分组路由（结构体往返天然保留全部键） |
| Mod 道具/语言合并 | `BaseCfg.GetInitDataForMods` → id 拼接 + `BaseBean.CombineModReferenceIds` |
| Mod spine 资源加载 | `SpineHandler.GetSkeletonDataAssetWithMod` |
| Mod 构建工具 | `ModBuildEditorWindow`：下拉自动扫描（产物目录 ∪ `*ModBuilder.cs`），构建方法约定推导 `BrownDustSpineModBuilder.BuildMod`，生成脚本路径推导 `gen_browndust_spine_mod.py`——**均按约定自动生效，无需改代码** |

## 生成流程（完整步骤）

### 1. 生成配置（在主项目侧执行，两段式：Excel 为唯一真实源）

```powershell
# 一律走 run-python.ps1 包装（路径动态化，不写死）：
# 资源变更后全量重建（扫描→Excel→JsonText）：
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".claude/scripts/run-python.ps1" `
    ".claude/scripts/gen_browndust_spine_mod.py" all --mod-project "<MOD项目根目录>"
# 只改了 Excel 参数/改名后重导（不重新扫描资源）：
#   ... export --mod-project "<MOD项目根目录>"
```

**两段式流水线**（脚本 `.claude/scripts/gen_browndust_spine_mod.py`）：

- `scan`：扫描 BrownDust 资源目录（char + 非 char）→ 重建/合并 **MOD 项目的两张 Excel**（道具表**全量重建**但 **ui_show_data 手调值按 id 保留**；语言表**按 id 合并保留人工改名**）；覆盖前自动备份到 `MOD项目/ExcelBackup/`（滚动复用 .bak.1~3，只留最近 3 份）；扫描结束打印按动画拆药统计（idle/all/loop/cut 各出药数 + 0 匹配兜底数）
- `export`：读两张 Excel → 导出 JsonText

产出（写入 MOD 项目，属 Mod 包一部分）：
```
MOD项目/Mods/BrownDustSpine/JsonText/
├── ItemsInfo.txt                    - 791 个幻化药道具
└── Language_ItemsInfo_{cn,en,jp,kr,tw,de,fr,ru,es,br,pl,tr}.txt
```

可调参数（仅 scan 生效）：`--ui-scale-k 645`、`--ui-pos-y 0`、`--reset-layout`。资源目录新增/变更后跑 `all`；只调道具参数/名字时改 Excel 后跑 `export`（**不要再 scan**）。

### 2. 构建 Addressables 产物

**方式一（推荐）：主项目「游戏/Mod构建工具」（ModBuildEditorWindow）一键完成**——「选择 Mod」下拉选中 `BrownDustSpine`（构建器脚本存在即自动入列，构建方法/生成脚本按约定自动带出）。**前置：MOD 项目不能被另一个 Unity 实例打开（Temp/UnityLockfile 被进程占用才拦截；崩溃/强杀残留的锁文件由构建工具自动清理，无需手动删除）**。

**方式二：在 MOD 项目的 Unity 编辑器里执行**，菜单：**工具/Mod/BrownDustSpine/一键构建(同步分组+构建)**（`MOD项目/Assets/Editor/BrownDustSpineModBuilder.cs`）：

1. 扫描 `Assets/ModResource/Spine/BrownDust` 全部 `SkeletonDataAsset` → 同步进 `Mod_BrownDustSpine` 分组（Address=资产名；**PackSeparately** 每资源一个 bundle）
2. **SkeletonData 缩放统一复位**（`ApplyUIShowSkeletonDataScale`，幂等）：全部 ui_show 系，scale 统一 `0.01f`
3. 临时把其他分组 `IncludeInBuild=false`（隔离构建），构建后自动恢复；构建器含防空构建保险：本分组残留 false 自动恢复、分组 0 条目时中止构建（2026-10-02 空构建事故后加）
4. 新增/复用 Profile `BrownDustSpine`，构建/加载路径指向 `Mods/BrownDustSpine` 并设为激活
5. 清理旧产物（**保留 JsonText 子目录**）→ `BuildPlayerContent()` 构建
6. **构建后自动把 catalog 三件套从引擎默认输出目录拷进 Mod 目录**
7. **自动导出 Excel→JsonText 并部署到主项目**（`ExportJsonTextAndDeploy`：调主项目 `run-python.ps1` + `gen_browndust_spine_mod.py export --deploy-main`；主项目根目录经菜单「工具/Mod/BrownDustSpine/设置主项目根目录（自动导出部署用）」配置一次，EditorPrefs 键 `BrownDustSpineModBuilder.MainProjectRoot` 独立于其他 Mod 的）
8. 产物：`Mods/BrownDustSpine/` 下 `catalog.bin` + `catalog.hash` + `settings.json` + `*.bundle`（290 个）+ `JsonText/`

### 3. 部署到主项目（一键构建已自动完成；本步仅手工补部署时用）

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".claude/scripts/run-python.ps1" `
    ".claude/scripts/gen_browndust_spine_mod.py" export --mod-project "<MOD项目根目录>" --deploy-main "<主项目根目录>"
```

## 验证清单（用户手动 Play，AI 不自动 Play）

1. 主项目 Unity 编译通过后，用 **LauncherTest** 场景启动
2. 测试发放道具：`userData.AddBackpackItem(new ItemBean(modId*100000000000000L + 1800020101))`（modId 用 `ModHandler.Instance.manager.GetModId("BrownDustSpine")` 取）
3. 魔物管理（UICreatureManager）对生物使用幻化药 → 确认框显示道具名「幻化药·棕尘char_000201-01 idle」
4. 确认后：**卡片详情 UI 循环播放对应动画**（idle 药=待机循环、cut 药=cut 特写循环）；**普通小卡/基地/战斗场景仍显示原生物形象**（本 Mod 药不动 show/world）
5. 同 char 不同动画的药（如 char_000202-02 cut_1 与 -03 cut_2）详情 UI 播放不同 cut 动画；多 idle 药（illust_dating1-01 idle1 / -02 idle2 / -03 idle3）应有差异
6. 吃幻原药恢复原形象；切语言（如 en）道具名显示「BrownDust Potion char_000202-02 cut_1」
7. 也可用测试模式-卡片测试-Mod幻化药测试面板：Mod 筛选选 `BrownDustSpine`，大卡列表逐页核对形象与动画

## 注意与边界

- **本流程仅适用于 BrownDustSpine**：其他 Mod 资源约定与道具规则不同，不套用本文档
- **白边事故与 PMA 约定**（2026-09-29）：本 Mod 原始贴图是**直通 alpha**（其他 Mod 均为 PMA），spine-unity 导入时自动开了材质 `Straight Alpha Texture`（`_STRAIGHT_ALPHA_INPUT` 关键字）——MOD 工程内显示正常，但构建进 bundle 后该 `shader_feature` 变体未随包编译，主工程运行时回退默认 PMA 变体，直通贴图被按 PMA 直接加算 → 详情UI 全体白色描边。**修复=全部贴图 rgb*=a 转 PMA + 全部 649 个材质关直通开关后重建**（转换用临时批量脚本 `TempBrownDustPmaConvert.ConvertAll` 跑两轮：首轮只覆盖 `*_Material.mat` 漏掉多页图集的 `{图集}_{页}.mat`，次轮改按"开关仍开"过滤全部 `*.mat` 幂等补齐，已删）。**新增资源入库时必须复查**：PNG 透明区纯黑（可用 Python+zlib 抽样 alpha=0 像素 RGB）、全部 `*.mat` 的 `_StraightAlphaInput: 0`
- MOD 项目改动（资源/构建器脚本）不在本仓库 git 内，watched_files 只覆盖主项目侧消费代码与生成脚本
- **双重匹配药语义注意**：同一动画的 idle 系药与 cut 系药播放效果完全相同（都循环该动画），属规则设计（用户确认不去重），数量约 27 对
- 套装内组合数 >99 时 2 位序号溢出，生成器告警截断（当前最多 ~25 组合，无风险）
- 幻化状态 `CreatureBean.transformItemId` 存拼接后完整 id：Mod 移除→配置查不到→详情UI 自动回落原形象，幻原药仍可清残留
- **JsonText 缺失排查**：同其他 Mod——Mod 目录只有 bundle+catalog 没有 JsonText 时，主游戏 Mod 初始化成功但道具合并不生效；诊断=`Mods/BrownDustSpine/JsonText/ItemsInfo.txt` 是否存在；修复=补跑 `export --deploy-main`
- **调单个药的详情UI尺寸不用跑生成脚本**：测试面板拖拽/滚轮实时调，「保存全部修改」按 Mod 分组路由写回（只换 ui_show_data 键）；资源目录变更才走 scan/all
