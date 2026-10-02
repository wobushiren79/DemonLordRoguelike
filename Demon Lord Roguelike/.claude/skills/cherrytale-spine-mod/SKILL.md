---
name: cherrytale-spine-mod
description: CherryTaleSpine Mod（樱桃幻化药）数据生成流程指南。使用此SKILL当需要为该Mod新增/重建幻化药道具、重新扫描CherryTale spine资源目录(扁平目录)、重新生成ItemsInfo/多语言JsonText、重新构建与部署Mod时。记录资源约定(扁平目录/皮肤三类出药规则/Eye+Mouth组合皮肤)、按动画拆药规则(排除Talk与_mark)、道具ID规则、other_data键值格式（ui_show_res/ui_show_data/ui_show_skin多皮肤「|」分隔/ui_show_idle_anim）、生成脚本与一键构建的完整步骤。目前仅适用于CherryTaleSpine这一个Mod，其他Mod流程可能不同。
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
  - .claude/scripts/gen_cherrytale_spine_mod.py
---

# CherryTaleSpine Mod（樱桃幻化药）数据生成流程

## Mod 概述

- **Mod 名**：`CherryTaleSpine`（= 主游戏 `Mods/` 下的目录名，ModManager 按目录名发现/加载）
- **内容**：1175 个幻化药道具（ItemTypeEnum.TransformPotion=18）+ CherryTale 系列 spine 资源（110 个 SkeletonData）
- **MOD 项目**：独立于主游戏的 Unity 工程（与其他 Spine Mod 同一 MOD 工程，示例路径 `E:\Unity\DemonLordRoguelikeMod\DemonLordRoguelikeMod\DemonLordRoguelikeMod`，**以实际机器路径为准**）
- **与其他 Mod 的核心差异**：① **皮肤三类出药规则**（仅default出default药 / 有01出01皮药 / 无01出「Eye_01+Mouth_01」组合皮药——**双角色骨架每个角色的眼/嘴各取一个**，`ui_show_skin` 键支持「|」分隔多皮肤叠加——全 Mod 体系首例组合皮肤）；② **按动画拆药**（全部动画各出 1 药，排除 Talk 与 _mark 差分）；③ 全部幻化药只改 ui_show（同 ArkRe/BrownDust 等）

## 资源约定（MOD 项目侧）

```
MOD项目/Assets/ModResource/Spine/CherryTale/
├── a001_01/                       - 角色目录（字母前缀+数字_数字，扁平结构无嵌套子目录）
│   ├── a001_01_SkeletonData.asset  ← 资源名=文件名去扩展名（保留 _SkeletonData）
│   ├── a001_01.json/.atlas.txt/.png/_Atlas.asset/_Material.mat[/_Material-Multiply.mat 等混合材质]
│   └── mark_Tex_01.png            - 孤儿贴图（不被任何图集/材质引用,不进bundle,无需处理）
├── a001_03/
├── story_hcg01/                   - 剧情CG目录（同构）
├── teaching_hcg01/                - 教学CG目录（双角色骨架）
└── ...（共 110 个目录）
```

- **目录组织**：扁平结构，每目录直接内含 **1 套** spine 导出物（无 ArkRe 的套装/子目录两层）；目录名形态多样（a001_01/b001_03/c001_01/da05_01/e001_01/f002_04/g025_03/wa02_01/wi01_01/story_free/story_hcgXX/teaching_hcgXX），**无统一命名模式**→套装号不取目录名，按目录自然序编号
- **资源名 = SkeletonData 资产文件名（不含扩展名，保留 `_SkeletonData` 后缀）**，如 `a001_01_SkeletonData`；构建器按此名登记 Addressables Address（`Path.GetFileNameWithoutExtension`），游戏侧 `SpineHandler.GetSkeletonDataAssetWithMod` 按此名精确匹配（大小写敏感）。**与其他 Mod 约定完全一致**
- 同名 spine json = 文件名去 `_SkeletonData.asset` + `.json`（读 skins/animations）
- **Spine JSON 格式**：真 4.3.26（2026-09-29 全量 110 个扫描确认），无需格式转换；scan 已内置旧 linkedmesh/分离约束数组校验告警（面向未来新增资源）
- **贴图必须 PMA（预乘 alpha）**：本 Mod 原始贴图曾为直通 alpha（172 材质全开 `_StraightAlphaInput`），**2026-09-29 已按 BrownDust 方案修复**（110 图集页 PNG `rgb*=a` 转 PMA + 172 材质关直通开关，临时脚本已删）；**新增资源入库时必须复查**：PNG 透明区纯黑、全部 `*.mat` 的 `_StraightAlphaInput: 0`（事故原理见 browndust-spine-mod SKILL「白边事故与 PMA 约定」；通用要求见 project_spine_mod_pma_requirement 记忆）

## 道具生成规则（皮肤三类 × 按动画拆药，2026-09-29 与用户确认）

- **皮肤三类出药**（读同名 json 的 skins 数组，`resolve_skin_key`）：
  | 分类 | 判定 | 数量 | ui_show_skin 键 |
  |---|---|---|---|
  | A | 仅 `default` 单皮肤 | 36 | **省略**（=骨架默认皮肤） |
  | C | 非 default 皮肤中含 `01`（其余 03/04/05/lv1~lv4 忽略） | 27 | `01` |
  | D | 无 `01`，部件组合皮肤 | 47 | `Eye_01|Mouth_01`（「|」分隔叠加） |
  - **D 类组合适配规则**：皮肤名以 `_01` 结尾且基名（`/` 分隔最后一段，小写）以 `eye`/`mouth` 结尾，**按基名分组、每组各取第一个**（单角色=1 Eye+1 Mouth；**双角色骨架每个角色的眼/嘴各取一个**，2026-09-29 与用户确认）；缺某部件则只用现有部件；两者皆无→不带皮肤键+警告
  - **D 类 6 个特殊骨架的解析结果**（scan 打印明细供审计）：a009_03=`AEye_01|BEye_01|Mouth_01`（双角色眼各取一个，嘴仅一套共用系列）、e001_01=`Eye/Eye_01|Mouth/Mouth_01`（斜杠皮肤名）、story_free=`Eyes/Eye_01|Mouth/Mouth_01`、story_hcg19=`Mouth_01`（无 Eye 系）、teaching_hcg01/02=`GEye_01|GMouth_01|HEye_01|HMouth_01`（双角色 G/H 眼嘴全取）
- **按动画拆药**：每个 SkeletonData 的全部动画各出 1 个幻化药，`ui_show_idle_anim` 键=动画名；**排除** `Talk` 动画（大小写不敏感精确匹配，全库 50 个）与 `_mark` 后缀差分动画（小写结尾判定，全库 397 个）；骨架内序号=动画名自然序；排除后无动画的骨架跳过+警告（当前不存在）
- **道具自ID**：`18` + `3位套装号`（目录自然序 001 起，当前 001~110）+ `2位序号`（01 起）。例：a001_01 → 1800101~1800108；a009_03 → 1801201~1801216；story_hcg19 → 1809601~1809614；teaching_hcg02 → 58 药（最大骨架，无溢出风险）
- **other_data 键值格式**（`&` 拆项、首个 `:` 拆键值，缺省键省略）——**只有 ui_show 系键**：
  ```
  ui_show_res:a009_03_SkeletonData&ui_show_data:0.1604;0,0&ui_show_skin:AEye_01|Mouth_01&ui_show_idle_anim:Idle_01
  ```
  - `ui_show_res`：ui_show_spine 高清展示资源名
  - `ui_show_data`：详情UI尺寸 `scale;x,y`；生成器按 `645/骨架高` 校准（同骨架全部药共用校准值，手调按药 id 各自保留）；scan 全量重建时按 id 保留手调值（`--reset-layout` 强制重算）
  - `ui_show_skin`：ui_show 资源内指定皮肤（可空：A 类省略；C 类=01；D 类=Eye|Mouth 组合，**「|」分隔多皮肤叠加**，消费侧拆分后走多皮肤换肤重载）
  - `ui_show_idle_anim`：该药对应的目标动画名（按动画拆药必带）
- **name 自ID = 道具自ID**（`name[language]` 标记驱动 `CombineModReferenceIds`，与其他 Mod 同机制）
- **固定字段**：item_type=18、num_max=1、icon_res=`Item_TransformPotion_1`、creature_model_id=0、reward_rarity=""、**source="1"**（征服模式奖励）
- **道具名**（带**动画名后缀**，同 BrownDust）：cn「幻化药·樱桃a001_01-01 Idle_lv1」/ tw「幻化藥·櫻桃…」/ en「Cherry Potion a001_01-01 Idle_lv1」（12 语言全生成）
- **Excel 独立**（与 `TestTransformPotionGUI.GetModItemsExcelRelPath` 约定一致）：
  - `Assets/Data/Excel/excel_mod_items_info_cherrytalespine[Mod道具信息-CherryTaleSpine].xlsx`
  - `Assets/Data/Excel/excel_mod_language_cherrytalespine[Mod多语言-CherryTaleSpine].xlsx`

## 主项目配套代码（消费侧）

与其他 Spine Mod 共用，**唯一新增 = ui_show_skin「|」多皮肤叠加**（2026-09-29 起，全 Mod 体系首例组合皮肤）：

| 机制 | 位置 |
|------|------|
| other_data 键值解析 | `CreatureBeanPartial.ParseTransformOtherData`（`#region 幻化相关`，返回 `TransformOtherData` 结构体；`uiShowSkin` 字段原样承载 `|` 分隔串） |
| 高清展示资源（ui_show_res 键） | `CreatureBeanPartial.GetTransformUIShowSpineRes` → `CreatureHandler.SetCreatureData`（isUIShow 分支独立判定） |
| **指定皮肤（ui_show_skin 键，含多皮肤）** | `CreatureBeanPartial.GetTransformUIShowSkin` → `CreatureHandler.SetCreatureData`：皮肤串按 `|` 拆分——**多个→`SpineHandler.ChangeSkeletonSkin(Skeleton, params string[])` 叠加换肤**（new Skin + AddSkin×N + SetSkin + SetupPoseSlots，未在组合内的部件自动回落骨架默认皮肤）；单个→原 `ChangeSkeletonSkin(Skeleton, string)` 整皮替换（ArkRe/Other/CherryTale C 类与 story_hcg19 单皮药行为不变） |
| 多皮肤换肤 API | `SpineHandler.ChangeSkeletonSkin(Skeleton skeleton, params string[] skinNames)` 重载（2026-09-29 新增；单皮肤缺失时 `SpineManager.GetSkeletonDataSkin` 报错并跳过该皮肤，其余照常叠加） |
| 替代待机动画（ui_show_idle_anim 键） | `CreatureBeanPartial.GetTransformUIShowIdleAnim` → `GameUIUtil.SetCreatureUIForDetails` 播放动画三级分支：该键非空→框架层按名直播——**本 Mod 全部药都带此键，详情UI 循环播放对应动画**（同 BrownDust） |
| 详情UI尺寸（ui_show_data 键） | `CreatureBeanPartial.GetTransformUIShowData` → `GameUIUtil.SetCreatureUIForDetails`；测试覆盖层优先 |
| 调参预览+写回 | `TestTransformPotionGUI`：保存按 modId 分组路由（结构体往返天然保留 `|` 皮肤串等全部键） |
| Mod 道具/语言合并 | `BaseCfg.GetInitDataForMods` → id 拼接 + `BaseBean.CombineModReferenceIds` |
| Mod spine 资源加载 | `SpineHandler.GetSkeletonDataAssetWithMod` |
| Mod 构建工具 | `ModBuildEditorWindow`：下拉自动扫描（产物目录 ∪ `*ModBuilder.cs`），构建方法约定推导 `CherryTaleSpineModBuilder.BuildMod`，生成脚本路径推导 `gen_cherrytale_spine_mod.py`——**均按约定自动生效，无需改代码** |

## 生成流程（完整步骤）

### 1. 生成配置（在主项目侧执行，两段式：Excel 为唯一真实源）

```powershell
# 一律走 run-python.ps1 包装（路径动态化，不写死）：
# 资源变更后全量重建（扫描→Excel→JsonText）：
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".claude/scripts/run-python.ps1" `
    ".claude/scripts/gen_cherrytale_spine_mod.py" all --mod-project "<MOD项目根目录>"
# 只改了 Excel 参数/改名后重导（不重新扫描资源）：
#   ... export --mod-project "<MOD项目根目录>"
```

**两段式流水线**（脚本 `.claude/scripts/gen_cherrytale_spine_mod.py`）：

- `scan`：扫描 CherryTale 资源目录 → 重建/合并 **MOD 项目的两张 Excel**（道具表**全量重建**但 **ui_show_data 手调值按 id 保留**；语言表**按 id 合并保留人工改名**）；覆盖前自动备份到 `MOD项目/ExcelBackup/`；扫描结束打印统计（皮肤 A/C/D 三类骨架数、排除 Talk/_mark 动画数、**D 类特殊组合皮肤解析明细**——非标准 `Eye_01|Mouth_01` 的全部列出供审计）
- `export`：读两张 Excel → 导出 JsonText

产出（写入 MOD 项目，属 Mod 包一部分）：
```
MOD项目/Mods/CherryTaleSpine/JsonText/
├── ItemsInfo.txt                    - 1175 个幻化药道具
└── Language_ItemsInfo_{cn,en,jp,kr,tw,de,fr,ru,es,br,pl,tr}.txt
```

可调参数（仅 scan 生效）：`--ui-scale-k 645`、`--ui-pos-y 0`、`--reset-layout`。资源目录新增/变更后跑 `all`；只调道具参数/名字时改 Excel 后跑 `export`（**不要再 scan**）。

### 2. 构建 Addressables 产物

**方式一（推荐）：主项目「游戏/Mod构建工具」（ModBuildEditorWindow）一键完成**——「选择 Mod」下拉选中 `CherryTaleSpine`（构建器脚本存在即自动入列，构建方法/生成脚本按约定自动带出）。**前置：MOD 项目不能被另一个 Unity 实例打开（Temp/UnityLockfile 被进程占用才拦截；崩溃/强杀残留的锁文件由构建工具自动清理，无需手动删除）**。

**方式二：在 MOD 项目的 Unity 编辑器里执行**，菜单：**工具/Mod/CherryTaleSpine/一键构建(同步分组+构建)**（`MOD项目/Assets/Editor/CherryTaleSpineModBuilder.cs`）：

1. 扫描 `Assets/ModResource/Spine/CherryTale` 全部 `SkeletonDataAsset` → 同步进 `Mod_CherryTaleSpine` 分组（Address=资产名；**PackSeparately** 每资源一个 bundle）
2. **SkeletonData 缩放统一复位**（`ApplyUIShowSkeletonDataScale`，幂等）：全部 ui_show 系，scale 统一 `0.01f`
3. 临时把其他分组 `IncludeInBuild=false`（隔离构建），构建后自动恢复
4. 新增/复用 Profile `CherryTaleSpine`，构建/加载路径指向 `Mods/CherryTaleSpine` 并设为激活
5. 清理旧产物（**保留 JsonText 子目录**）→ `BuildPlayerContent()` 构建
6. **构建后自动把 catalog 三件套从引擎默认输出目录拷进 Mod 目录**
7. **自动导出 Excel→JsonText 并部署到主项目**（`ExportJsonTextAndDeploy`：调主项目 `run-python.ps1` + `gen_cherrytale_spine_mod.py export --deploy-main`；主项目根目录经菜单「工具/Mod/CherryTaleSpine/设置主项目根目录（自动导出部署用）」配置一次，EditorPrefs 键 `CherryTaleSpineModBuilder.MainProjectRoot` 独立于其他 Mod 的）
8. 产物：`Mods/CherryTaleSpine/` 下 `catalog.bin` + `catalog.hash` + `settings.json` + `*.bundle`（110 个）+ `JsonText/`

### 3. 部署到主项目（一键构建已自动完成；本步仅手工补部署时用）

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".claude/scripts/run-python.ps1" `
    ".claude/scripts/gen_cherrytale_spine_mod.py" export --mod-project "<MOD项目根目录>" --deploy-main "<主项目根目录>"
```

## 验证清单（用户手动 Play，AI 不自动 Play）

1. 主项目 Unity 编译通过后，用 **LauncherTest** 场景启动
2. 测试发放道具：`userData.AddBackpackItem(new ItemBean(modId*100000000000000L + 1800101))`（modId 用 `ModHandler.Instance.manager.GetModId("CherryTaleSpine")` 取）
3. 魔物管理（UICreatureManager）对生物使用幻化药 → 确认框显示道具名「幻化药·樱桃a001_01-01 Idle_lv1」
4. 确认后：**卡片详情 UI 循环播放对应动画**；**普通小卡/基地/战斗场景仍显示原生物形象**（本 Mod 药不动 show/world）
5. **皮肤三类核对**：A 类药（如 a001_01 系）显示骨架默认皮肤；C 类药（如 a001_03 系）显示 `01` 皮肤（与 default 应有明显差异）；D 类药（如 a004_03 系）显示 **Eye+Mouth 组合皮肤**（眼/嘴部件替换，其余部件保持默认）
6. **特殊骨架核对**：a009_03 系药=A/B 双角色眼各换+Mouth 组合；story_hcg19 系药=仅 Mouth 单皮肤；teaching_hcg01/02 系药=G/H 双角色眼嘴全换（4 皮肤叠加）
7. 同骨架不同动画的药（如 a001_01-01 Idle_lv1 与 -05 Touch_lv1）详情 UI 播放不同动画
8. 吃幻原药恢复原形象；切语言（如 en）道具名显示「Cherry Potion a001_01-01 Idle_lv1」
9. 也可用测试模式-卡片测试-Mod幻化药测试面板：Mod 筛选选 `CherryTaleSpine`，大卡列表逐页核对形象/皮肤/动画

## 注意与边界

- **本流程仅适用于 CherryTaleSpine**：其他 Mod 资源约定与道具规则不同，不套用本文档
- **PMA 约定**：2026-09-29 已完成全量修复（同 BrownDust 方案）；新增资源入库时必须复查 PNG 透明区纯黑 + 全部 `*.mat`（含 `*_Material-Multiply.mat` 等混合材质）`_StraightAlphaInput: 0`，否则详情UI 白边
- **`ui_show_skin` 的 `|` 多皮肤语法目前仅 CherryTaleSpine D 类药使用**：ArkRe/Other 的单皮肤名药不受影响（拆分后长度=1 走原整皮替换分支）
- **D 类组合皮肤=部件叠加**：Eye/Mouth 是覆盖在 default 基底上的部件皮肤（非整皮），消费侧 params 重载 `new Skin + AddSkin×N` 后未命中部件自动回落骨架默认皮肤——**禁止**改用 `ChangeSkeletonSkin(Skeleton, string)` 逐个皮肤调用（后者是整皮替换，后调会覆盖先调）
- 套装号按目录自然序分配：**新增目录会使其后目录套装号顺延、道具 id 漂移**——发布后如需加资源，应把新目录排在末尾或接受 id 重排（当前 110 目录一次性全量生成，未发布前无此顾虑）
- 套装内组合数 >99 时 2 位序号溢出，生成器告警截断（当前最大 58=teaching_hcg02，无风险）
- 幻化状态 `CreatureBean.transformItemId` 存拼接后完整 id：Mod 移除→配置查不到→详情UI 自动回落原形象，幻原药仍可清残留
- **JsonText 缺失排查**：同其他 Mod——Mod 目录只有 bundle+catalog 没有 JsonText 时，主游戏 Mod 初始化成功但道具合并不生效；诊断=`Mods/CherryTaleSpine/JsonText/ItemsInfo.txt` 是否存在；修复=补跑 `export --deploy-main`
- **调单个药的详情UI尺寸不用跑生成脚本**：测试面板拖拽/滚轮实时调，「保存全部修改」按 Mod 分组路由写回（只换 ui_show_data 键，`|` 皮肤串等其余键原样保留）；资源目录变更才走 scan/all
