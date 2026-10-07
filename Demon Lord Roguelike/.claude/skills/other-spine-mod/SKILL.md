---
name: other-spine-mod
description: OtherSpine Mod（Other幻化药）数据生成流程指南。使用此SKILL当需要为该Mod新增/重建幻化药道具、重新扫描Other spine资源目录(角色名目录)、重新生成ItemsInfo/多语言JsonText、重新构建与部署Mod时。记录资源约定(角色名目录/Avator=ui_show段不带=基础show段/前缀配对/多皮肤拆分)、道具ID规则、other_data键值格式（show_res/show_data/ui_show_res/ui_show_data/ui_show_skin）、生成脚本与一键构建的完整步骤。目前仅适用于OtherSpine这一个Mod，其他Mod流程可能不同。
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
  - .claude/scripts/gen_other_spine_mod.py
---

# OtherSpine Mod（Other幻化药）数据生成流程

## Mod 概述

- **Mod 名**：`OtherSpine`（= 主游戏 `Mods/` 下的目录名，ModManager 按目录名发现/加载）
- **内容**：104 个幻化药道具（ItemTypeEnum.TransformPotion=18）+ Other 系列 spine 资源（59 个角色名目录共 104 个 SkeletonData：57 基础 show + 47 Avator ui_show）
- **MOD 项目**：独立于主游戏的 Unity 工程（与 AeonsEchoSpine/ArkReSpine/NikkeSpine/BrownDustSpine/GirlWarsSpine 同一 MOD 工程，示例路径 `E:\Unity\DemonLordRoguelikeMod\DemonLordRoguelikeMod\DemonLordRoguelikeMod`，**以实际机器路径为准**）
- **与其他 Mod 的核心差异**：① 资源按**角色名目录**组织（Amelia/Luna/guanchazhe 等，字母/拼音，无数字套装号）；② **Avator 命名分段**：SkeletonData 名带 `Avator`=ui_show 详情UI段、不带=基础 show 段（世界/战斗/小卡），同目录配对=前缀匹配（`X_Avator`↔`X`）优先、**无前缀匹配的基础回落配对目录主 Avator**（base_key==目录名优先，如 Aoliweiya 1 Avator+2 基础 → A+Avator 与 B+Avator 各出药）；③ **多皮肤拆分**：Avator 骨架的每个具名皮肤（≠default）单独出药（`ui_show_skin` 键，ArkReSpine 同规则），**多皮肤时不生成默认 default 皮肤药**，仅 default 单皮肤才出 1 普通药；④ 本 Mod 药**同时改 show 与 ui_show**（配对药），不是仅详情UI幻化

## 资源约定（MOD 项目侧）

```
MOD项目/Assets/ModResource/Spine/Other/
├── Amelia/                          - 角色目录（角色名，59 个）
│   ├── Amelia_SkeletonData.asset         ← 基础 show（同名 .json/.atlas.txt/.png/_Atlas.asset/_Material*.mat 全套）
│   └── Amelia_Avator_SkeletonData.asset  ← ui_show 详情UI高清图（Avator 标记）
├── Anniboni/
│   └── Anniboni_Avator_SkeletonData.asset ← 仅 Avator（无基础）→ 仅详情UI幻化药
├── Baolilong/
│   └── Baolilong_SkeletonData.asset       ← 仅基础（无 Avator）→ 仅 show 段药
├── Aoliweiya/                         - 双基础单 Avator：Aoliweiya + Aoliweiya_ChongChong 均配对共享 Aoliweiya_Avator
└── Luna/                              - 双基础双 Avator：Luna↔Luna_Avator、Luna_2↔Luna_2_Avator（前缀配对）
```

- **资源目录名无格式要求**（角色名即目录名，直接参与备注与命名）；空目录不出药
- **不分段跳过规则**：本 Mod **无** Back/Front/BG 背景层跳过（`Kuluoxierback` 是角色背面图不是背景层，正常出药——与 GirlWarsSpine 的跳过规则相反，勿混用）
- **资源名 = SkeletonData 资产文件名（不含扩展名，保留 `_SkeletonData` 后缀）**；构建器按此名登记 Addressables Address，游戏侧 `SpineHandler.GetSkeletonDataAssetWithMod` 按此名精确匹配（大小写敏感）。**与其他 Mod 约定完全一致**
- **配对规则**：Avator 名截到 `_Avator` 前 = 基础名（`Amelia_Avator`→`Amelia`、`Luna_2_Avator`→`Luna_2`），同目录内前缀匹配优先；**无前缀匹配的基础回落配对目录主 Avator**（base_key==目录名优先，否则排序首个），一个 Avator 可被多个基础共享配对（2026-09-29 与用户确认）
- 同名 spine json = 文件名去 `_SkeletonData.asset` + `.json`，生成脚本读其骨架高校准 show_data/ui_show_data、读 skins 拆皮肤药、读 animations 做 idle 检测
- **Spine JSON 格式**：全部为真 4.3.26（2026-09-29 全量 104 个 JSON 扫描确认：无旧 linkedmesh、无顶层分离约束数组），主项目 spine-csharp 4.3.39 直接兼容，**无需格式转换**。脚本 scan 已内置格式校验（`check_spine_json_format`），新增资源若出现旧 linkedmesh（skins 内含 `"parent"`）或顶层 `"ik"/"transform"/"path"` 约束数组会告警，须先转换（参照 aeonsecho-spine-mod SKILL「Spine JSON 格式陷阱」节）
- **贴图 PMA 已确认**（2026-09-29：81 个材质全部 `_StraightAlphaInput: 0`，PNG 抽样透明区纯黑），无需转换。**新增资源入库时必须复查**两项：① PNG 透明区纯黑；② 该图集全部 `*.mat` `_StraightAlphaInput: 0`（详见 browndust-spine-mod SKILL 白边事故记录 / 记忆 project_spine_mod_pma_requirement）
- **材质 shader 约定（2026-10-04 起）**：图集**普通页**材质统一为 URP 受光 `Universal Render Pipeline/Spine/Sprite`（GUID `9f253724b2d29a3438eeea48277c25cb`，与主项目生物材质同一 shader；MOD 项目经 `com.esotericsoftware.spine.urp-shaders` 包解析，主项目运行时解析到本地拷贝 `Assets/Shaders/Spine-Sprite-URP.shader`），关键字 `_ALPHAPREMULTIPLY_ON + _FIXED_NORMALS_VIEWSPACE`、`_FixedNormal=(0,0,1,1)`、`_SrcBlend=1/_DstBlend=10`——全属性以主项目 `Goblin_Material.mat` 为模板。**混合页**（`-Multiply`/`-Screen`/`-Additive` 后缀）**保持内置管线 `Spine/Skeleton-PMA-*` 不换**（sprite shader 无 Screen 关键字、Multiply 公式不等价）。**原因**：spine-unity 图集导入自动生成的材质默认是内置无光照 `Spine/Skeleton`——不吃战斗场景灯光（森林 Day 平行光 1.5 + 环境光），战斗/基地场景比主项目生物**暗 30~50%**（2026-10-04 偏暗事故根因，详见记忆 project_spine_mod_unlit_material）。**新增资源入库时自动生成的材质必须批量换成受光 shader 才能构建入包**：以 Goblin_Material.mat 为模板整文件重写（仅替换 m_Name 与 _MainTex guid，保留 .meta）

## 道具生成规则

- **组合**（目录内出药顺序，序号 01 起）：
  1. 每个基础 SkeletonData 出药（目录同名基础排首位=主形象）：配对 Avator 仅默认皮肤 → 出 1 个普通药（show+ui_show 双段键）；配对 Avator 有多皮肤 → **不出默认皮肤药**，每具名皮肤 1 药（双段键 + `ui_show_skin`）；无 Avator → 仅 show 段药（详情UI回落 show 形象，ui_show_data 可经测试面板手调补上，2026-09-30 起 scan 同机制保留）
  2. 未被任何基础配对的 Avator（4 个）→ 仅详情UI幻化药（只有 ui_show 段键；多皮肤同样不出默认皮肤药）
  - 当前构成：32 配对普通药 + 13 仅基础 + 4 仅 Avator + 55 皮肤药 = **104 药**（12 个多皮肤 Avator 全部是表情皮肤：angry/happy/wait/weixiao 等）
- **待机动画走 mod-system SKILL 通用 idle 检测**（`load_std_idle_candidates`/`pick_idle_anim`）：当前 104 个骨架**全部命中**标准待机候选（`idle,wait,idle1,wait1,stand`——其中 13 个 Avator 待机=wait，主项目 `excel_spine_animation_state` id=10001 候选已含 wait，无需改表），均不生成 idle_anim/ui_show_idle_anim 键；未来新增资源无标准候选时自动取首个含 idle 动画名写键（show 段→`idle_anim`、ui_show 段→`ui_show_idle_anim`）
- **道具自ID**：`18` + `4位目录序号`（0001 起，按目录名自然序分配）+ `2位序号`（01 起，目录内出药顺序）。例：Amelia(0001) → `18000101`；Luna(0032) → `18003201`/`18003202`。**目录序号 scan 重建时按目录名从旧道具表 remark 回收保留**（新增目录取 max+1，不复用已释放号，防存档 id 串目录；`--reset-layout` 会连序号一起重排——仅首次/未发布时使用）
- **other_data 键值格式**（`&` 拆项、首个 `:` 拆键值，缺省键省略）：
  ```
  配对药：show_res:Amelia_SkeletonData&show_data:0.8415;0,-120&ui_show_res:Amelia_Avator_SkeletonData&ui_show_data:0.1719;0,0&show_brightness:0.5801
  皮肤药：配对药全部键 & ui_show_skin:angry（show_brightness 按同骨架同系数自动带上）
  仅基础：show_res:Baolilong_SkeletonData&show_data:4.833;0,-120&show_brightness:0.63
  （仅基础药无 ui_show 段键；详情UI回落 show 形象仍消费 ui_show_data——测试面板可手调补上，scan 按资源身份保留，同 world_data 机制，2026-09-30 起）
  仅详情：ui_show_res:Anniboni_Avator_SkeletonData&ui_show_data:0.1388;0,0
  （仅详情药无 show 段不进场景，**不写 show_brightness**）
  ```
  - `show_res`：默认展示形象资源名（世界/战斗/小卡；可空=仅详情UI幻化）
  - `show_data`：小卡UI尺寸 `scale;x,y`；生成器按 `3159/骨架高` 校准 scale（与 AeonsEchoSpine 同基准），默认位移 `0,-120`
  - `show_brightness`：**场景调暗系数** `(0,1] 浮点`（仅场景实例调暗，UI 不消费）——**scan 自动生成**：show 骨架图集全页 PNG 有效像素（alpha>25）Rec.601 平均亮度 `avg > 基准65` 才写键（`k=65/avg` 只压不提，round 4；基准 65=主项目生物贴图 Goblin 57.9/Skeleton 69.5/Succubus 67.5 口径，修基地/森林 1.5 白平行光下浅色立绘过曝）；**保留优先**（手调值按资源身份保留，**删键=下次 scan 强制重算**的逃生口；`--reset-layout` 连保留值一起清）；皮肤药与同骨架基础药同系数；PIL 缺失时跳过自动算（保留值仍透传）
  - `ui_show_res` / `ui_show_data`：详情UI高清展示资源名/尺寸（`645/骨架高` 校准，位移 `0,0`）；仅基础药无 Avator 不自动生成 ui_show_data，但详情UI回落 show 形象（`SetCreatureData` 的 isUIShow 分支）时该键**仍被 `SetCreatureUIForDetails` 消费（与有无 ui_show_res 无关）**，测试面板大卡列表/单个预览可手调补上
  - `ui_show_skin`：ui_show 资源内指定皮肤名（仅皮肤药带）
  - `world_data`：世界显示尺寸/偏移（无校准来源默认不生成，测试面板手调；scan 重建按「目录名/资源token」保留）
  - **scan 全量重建时 show_data/ui_show_data/world_data 三键手调值按「目录名/资源token」(remark 资源身份)保留**——按资源身份而非道具 id 保留，出药规则变化/资源增减导致 id 漂移时保留值也不会贴错道具（2026-09-30 起，此前按 id 保留在皮肤拆分规则变化时发生过移位；`--reset-layout` 强制重算）
- **name 自ID = 道具自ID**：指向 Mod 自带语言表同 id 行（`name[language]` 标记驱动 `CombineModReferenceIds`，与其他 Mod 同机制）
- **固定字段**：item_type=18、num_max=1、icon_res=`Item_Potion_1`、creature_model_id=0、reward_rarity=""、**source="1"**（=ItemSourceEnum.ConquerReward 征服模式奖励）
- **道具名**：cn「幻化药·Other Amelia」/ tw「幻化藥·Other Amelia」/ en「Other Potion Amelia」（12 语言全生成）；目录内第 2 个起的基础药名字带 `-NN` 后缀（如「幻化药·Other Aoliweiya-02」）；皮肤药=基础名+皮肤名（如「幻化药·Other Asitelinna angry」——多皮肤目录只有皮肤药，无纯目录名药）
- **Excel 独立**（与 `TestTransformPotionGUI.GetModItemsExcelRelPath` 约定一致）：
  - `Assets/Data/Excel/excel_mod_items_info_otherspine[Mod道具信息-OtherSpine].xlsx`
  - `Assets/Data/Excel/excel_mod_language_otherspine[Mod多语言-OtherSpine].xlsx`

## 主项目配套代码（消费侧）

与其他 Spine Mod 完全共用，无任何 Mod 特化代码：

| 机制 | 位置 |
|------|------|
| other_data 键值解析 | `CreatureBeanPartial.ParseTransformOtherData`（`#region 幻化相关`，返回 `TransformOtherData` 结构体，含 idle_anim/ui_show_idle_anim/ui_show_skin 键） |
| 基础形象（show_res 键） | `CreatureBeanPartial.GetTransformSpineRes` → `CreatureHandler.SetCreatureData` |
| 高清展示资源（ui_show_res 键） | `CreatureBeanPartial.GetTransformUIShowSpineRes` → `CreatureHandler.SetCreatureData`（isUIShow 分支独立判定、不依赖 show_res） |
| 指定皮肤（ui_show_skin 键） | `CreatureBeanPartial.GetTransformUIShowSkin` → `CreatureHandler.SetCreatureData`：`hasTransform` 且皮肤名非空时 `SpineHandler.ChangeSkeletonSkin(Skeleton, string)` 按名整皮替换（**皮肤串支持「|」分隔多皮肤**——拆分后多个改调 params 叠加重载，2026-09-29 起，CherryTaleSpine 组合皮药用，本 Mod 单皮肤名不受影响） |
| 详情UI尺寸（ui_show_data 键） | `CreatureBeanPartial.GetTransformUIShowData` → `GameUIUtil.SetCreatureUIForDetails`（**消费不看有无 ui_show_res**——仅基础药详情UI回落 show 形象同样生效）；编辑器下测试覆盖层 `TransformPotionUITestOverride` 优先 |
| 小卡UI尺寸（show_data 键） | `CreatureBeanPartial.GetTransformShowData` → `GameUIUtil.SetCreatureUIForSimple`；覆盖层同优先 |
| 世界显示尺寸（world_data 键） | `CreatureBeanPartial.GetTransformWorldData` → `CreatureHandler.SetCreatureData`（SkeletonAnimation 分支） |
| 场景调暗系数（show_brightness 键） | `CreatureBeanPartial.GetTransformShowBrightness` → `CreatureHandler.SetCreatureData`（SkeletonAnimation 分支，world_data 注入同位）→ 游戏层 `SpineHandler.ApplySceneDimOverride`/`ClearSceneDimOverride`：`CustomMaterialOverride` 把场景实例普通页图集材质换成克隆调暗材质（`_COLOR_ADJUST`+`_Brightness`，HSV 只缩 V 不碰 alpha，PMA 安全；混合页 -Multiply/-Screen 跳过；无键/幻原药恢复=按值识别 Remove 清除，对象池安全；**UI SkeletonGraphic 不受影响**；`_COLOR_ADJUST` 变体剥离保险=`Assets/Resources/Materials/SpineSpriteURP_DimDummy.mat`） |
| 替代待机动画（idle_anim/ui_show_idle_anim 键） | show 段=`GetTransformIdleAnim`→游戏层 `SpineHandler.GetAnimNameAppoint`；ui_show 段=`GetTransformUIShowIdleAnim`→`GameUIUtil.SetCreatureUIForDetails` 三级分支（本 Mod 当前全部骨架命中标准候选，无药带键） |
| 调参预览+写回 | `TestTransformPotionGUI`（测试模式-卡片测试-Mod幻化药测试面板）：大卡列表/单个预览对**仅基础药（无 Avator）同样可调**（详情UI回落 show 形象，调的是 ui_show_data 键；无键时首次调整基线取卡片图标当前显示值防跳变，2026-09-30 起）；**场景列表支持亮度调参**（Alt+滚轮 或 项下亮度滑动条调 show_brightness，标签显「亮xx%」+滑动条实时数值，「亮原」专用按钮清覆盖回配置键值；「还原」按钮只管 world_data，2026-10-05 起）与**测试场景加载**（下拉选择真实场景 prefab 还原光照：首项基地档（销毁 ScenePrefabForBase 业务组件+白环境光+纯色深底）+ FightSceneCfg 各行变体（天空盒/雾/环境光/Details 显隐），体积雾/景深不还原；卸载/关面板自动还原环境含相机 clearFlags，2026-10-05 起）；保存按 modId 分组路由——各 Mod 独立 Excel（`GetModItemsExcelRelPath` 约定自动覆盖 OtherSpine）+ `Mods/{modName}/JsonText/ItemsInfo.txt` 两处直补 + 会话内存 |
| Mod 道具/语言合并 | `BaseCfg.GetInitDataForMods` → id 拼接 + `BaseBean.CombineModReferenceIds` |
| Mod spine 资源加载 | `SpineHandler.GetSkeletonDataAssetWithMod` |
| Mod 构建工具 | `ModBuildEditorWindow`：下拉自动扫描（产物目录 ∪ `*ModBuilder.cs`），构建方法约定推导 `OtherSpineModBuilder.BuildMod`，生成脚本路径推导 `gen_other_spine_mod.py`——**均按约定自动生效，无需改代码** |

## 生成流程（完整步骤）

### 1. 生成配置（在主项目侧执行，两段式：Excel 为唯一真实源）

```powershell
# 一律走 run-python.ps1 包装（路径动态化，不写死）：
# 资源变更后全量重建（扫描→Excel→JsonText）：
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".claude/scripts/run-python.ps1" `
    ".claude/scripts/gen_other_spine_mod.py" all --mod-project "<MOD项目根目录>"
# 只改了 Excel 参数/改名后重导（不重新扫描资源）：
#   ... export --mod-project "<MOD项目根目录>"
```

**两段式流水线**（脚本 `.claude/scripts/gen_other_spine_mod.py`）：

- `scan`：扫描 Other 资源目录 → 重建/合并 **MOD 项目的两张 Excel**（道具表**全量重建**但 **布局三键与 show_brightness 手调值按「目录名/资源token」(remark 资源身份)保留**——id 漂移不贴错 + **目录序号按目录名回收保留**；语言表**按 id 合并保留人工改名**）；覆盖前自动备份到 `MOD项目/ExcelBackup/`（滚动复用 .bak.1~3，只留最近 3 份）；**idle 动画检测**（标准候选命中→不生成键；无候选→首个含 idle 动画名写键，扫描结束打印统计）；**场景调暗键自动生成**（show 骨架图集全页平均亮度 >65 → 写 `show_brightness=65/avg` 只压不提，保留优先、删键强制重算，扫描结束打印写键数+最亮 top10）；**Spine JSON 格式校验**（旧 linkedmesh/分离约束数组告警）
- `export`：读两张 Excel → 导出 JsonText

产出（写入 MOD 项目，属 Mod 包一部分）：
```
MOD项目/Mods/OtherSpine/JsonText/
├── ItemsInfo.txt                    - 104 个幻化药道具
└── Language_ItemsInfo_{cn,en,jp,kr,tw,de,fr,ru,es,br,pl,tr}.txt
```

可调参数（仅 scan 生效）：`--ui-scale-k 645`（详情UI缩放校准常数）、`--ui-pos-y 0`、`--show-scale-k 3159`（小卡UI缩放校准常数）、`--show-pos-y -120`、`--reset-layout`（强制重算布局+目录序号重排，**仅首次/未发布时用**）。资源目录新增/变更后跑 `all`；只调道具参数/名字时改 Excel 后跑 `export`（**不要再 scan，否则道具表被重建覆盖**——布局三键与目录序号虽保留，其余列改动会丢）。

### 2. 构建 Addressables 产物

**方式一（推荐）：主项目「游戏/Mod构建工具」（ModBuildEditorWindow）一键完成**——「选择 Mod」下拉选中 `OtherSpine`（构建器脚本存在即自动入列，构建方法按约定自动带出 `OtherSpineModBuilder.BuildMod`，生成脚本路径自动推导 `gen_other_spine_mod.py`）。**前置：MOD 项目不能被另一个 Unity 实例打开（Temp/UnityLockfile 被进程占用才拦截；崩溃/强杀残留的锁文件由构建工具自动清理，无需手动删除）**。

**方式二：在 MOD 项目的 Unity 编辑器里执行**，菜单：**工具/Mod/OtherSpine/一键构建(同步分组+构建)**（`MOD项目/Assets/Editor/OtherSpineModBuilder.cs`）：

1. 扫描 `Assets/ModResource/Spine/Other` 全部 `SkeletonDataAsset` → 同步进 `Mod_OtherSpine` 分组（Address=资产名；**PackSeparately** 每资源一个 bundle 按需加载；**无背景层跳过**）
2. **SkeletonData 缩放双向校准**（`ApplySkeletonDataScale`，幂等）：文件名带 Avator 的 ui_show 系=`0.01f`（详情UI尺寸由 ui_show_data 键控制）、不带的基础 show 系=`0.002f`（非 ui_show_spine 显示 ×1/5，与 AeonsEchoSpine Chess 同值）
3. 临时把其他分组 `IncludeInBuild=false`（隔离构建），构建后自动恢复；构建器含防空构建保险：本分组残留 false 自动恢复、分组 0 条目时中止构建（2026-10-02 空构建事故后加）
4. 新增/复用 Profile `OtherSpine`，构建/加载路径指向 `Mods/OtherSpine` 并设为激活
5. 清理旧产物（**保留 JsonText 子目录**）→ `BuildPlayerContent()` 构建
6. **构建后自动把 catalog 三件套从引擎默认输出目录拷进 Mod 目录**
7. **自动导出 Excel→JsonText 并部署到主项目**（`ExportJsonTextAndDeploy`：调主项目 `run-python.ps1` + `gen_other_spine_mod.py export --deploy-main`；主项目根目录经菜单「工具/Mod/OtherSpine/设置主项目根目录（自动导出部署用）」配置一次，EditorPrefs 键 `OtherSpineModBuilder.MainProjectRoot` 独立于其他 Mod 的）
8. 产物：`Mods/OtherSpine/` 下 `catalog.bin` + `catalog.hash` + `settings.json` + `*.bundle`（104 个）+ `JsonText/`

**方式三（AI 批处理，2026-09-29 首次构建即用此法）**：直接命令行批量模式（等价于方式一的内部命令；MOD 项目需无锁）：
```powershell
& "<Unity.exe路径>" -batchmode -projectPath "<MOD项目根目录>" -executeMethod OtherSpineModBuilder.BuildMod -quit -logFile "<日志路径>"
```
注意：PowerShell `&` 调用可能立即返回而 Unity 继续跑——须按 PID `Wait-Process` 或盯日志尾部「构建完成」字样再验产物。

### 3. 部署到主项目（一键构建已自动完成；本步仅手工补部署时用）

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".claude/scripts/run-python.ps1" `
    ".claude/scripts/gen_other_spine_mod.py" export --mod-project "<MOD项目根目录>" --deploy-main "<主项目根目录>"
```

## 验证清单（用户手动 Play，AI 不自动 Play）

1. 主项目 Unity 编译通过后，用 **LauncherTest** 场景启动（Mod 初始化已收口到 `BaseLauncher.Launch()` 首行 `InitializeAllModsSync()`）
2. 测试发放道具：`userData.AddBackpackItem(new ItemBean(modId*100000000000000L + 18000101))`（modId 用 `ModHandler.Instance.manager.GetModId("OtherSpine")` 取）
3. 魔物管理（UICreatureManager）对生物使用幻化药 → 确认框显示道具名「幻化药·Other Amelia」
4. 确认后（配对药）：**卡片详情 UI 显示 Avator 高清图**（待机走框架候选，wait/idle 自动命中）；**普通小卡/基地/战斗场景显示基础 show 形象**（本 Mod 药会改 show，与 GirlWarsSpine 仅详情UI 不同）
5. **皮肤药**（如「幻化药·Other Asitelinna angry」）：详情UI 显示对应皮肤；同角色不同皮肤药之间应有差异（该角色没有默认皮肤药属预期）
6. **仅详情UI幻化药**（Anniboni 等 4 个）：详情UI 显示高清形象，普通小卡/基地/战斗场景仍显示原生物形象
7. **核对无白边/黑边**（PMA 已确认；若出现描边异常按「贴图 PMA」节复查）
8. 吃幻原药恢复原形象；切语言（如 en）道具名显示「Other Potion Amelia」
9. 也可用测试模式-卡片测试-Mod幻化药测试面板：Mod 筛选选 `OtherSpine`，大卡/小卡/场景列表逐页核对

## 注意与边界

- **本流程仅适用于 OtherSpine**：其他 Mod 资源约定与道具规则可能不同，不套用本文档；AeonsEchoSpine/ArkReSpine/NikkeSpine/BrownDustSpine/GirlWarsSpine 流程见各自 SKILL
- **与 GirlWarsSpine 的关系**：两者相互独立（GirlWarsSpine=Painting_xxx 纯数字目录仅详情UI幻化；OtherSpine=角色名目录 show+ui_show 双段）；用户确认（2026-09-29）这批 Avator 资源另起新 Mod 而非并入 GirlWarsSpine
- MOD 项目改动（资源/构建器脚本）不在本仓库 git 内，watched_files 只覆盖主项目侧消费代码与生成脚本
- 幻化状态 `CreatureBean.transformItemId` 存拼接后完整 id：Mod 移除→配置查不到→详情UI 自动回落原形象（每 id 一次 LogError 属预期），幻原药仍可清残留
- **目录序号稳定性**：发布后新增角色目录时序号取 max+1 追加（remark 回收机制），切勿用 `--reset-layout`（会把序号重排导致已发道具 id 漂移、存档幻化串角色）
- **JsonText 缺失排查**：同其他 Mod——Mod 目录只有 bundle+catalog 没有 JsonText 时，主游戏 Mod 初始化成功但道具合并不生效；诊断=`Mods/OtherSpine/JsonText/ItemsInfo.txt` 是否存在；修复=补跑 `export --deploy-main`
- **调单个药的尺寸不用跑生成脚本**：测试面板拖拽/滚轮实时调，「保存全部修改」按 Mod 分组路由写回 otherspine 的 Excel+两处 JsonText+内存（只换 show_data/ui_show_data/world_data 三键，ui_show_skin 等其余键原样保留）；**保存后必须确认绿色「✓ 已保存N个（Excel✓ + JsonText×2/2 + 当前会话✓）」且无弹窗**——2026-09-30 两次事故根因：面板写 Excel 用了「`new FileStream` + `new ExcelPackage(fs)` + `Save()`」模式，**在本项目 EPPlus 版本下无异常但文件完全未落盘**（mtime/内容均不变，PowerShell 加载同一 DLL 实测证实；全项目仅这一处写路径用该模式，ExcelUtil.SetExcelData 与全部编辑器工具均用 `new ExcelPackage(FileInfo)`），于是显示"Excel✓"实际未写入，随后一键构建 `export` 用旧 Excel 重出 JsonText，表现为手调值被覆盖；已修复为 FileInfo 写模式 + **写后回读校验**（比对每行 other_data，不符即弹窗报错且保留覆盖层可重试），另有失败弹 `DisplayDialog` 强提示、「已保存0个」按红字处理；Excel/WPS 占用该 xlsx 仍会写盘失败（弹窗提示），保存前请先关闭；资源目录变更才走 scan/all 生成流程
- **「Material is missing texture」排查**：与 NikkeSpine 同款——主项目运行报此错时按序排查：① UnityPy 看 bundle 内有无 Texture2D；② bundle 哈希大小与上次构建一致=可复现；③ 多半是 MOD 项目 Library 的 PNG 导入 artifact 损坏；④ 修复=对受害 png 强制重导再重跑一键构建；⑤ `GetDependencies` 走查仍含坏 png（GUID 引用在），构建不报错、静默打出无贴图 bundle，只能靠结果校验（bundle/源png 大小比 <0.5 即缺贴图）（详见 nikke-spine-mod SKILL 2026-09-29 实战记录）
- **「构建+部署都跑了但改动没生效」排查**（2026-09-30 Hinagiku 脸部修复未生效事故）：先看 MOD 项目 `Mods/OtherSpine/` 产物时间戳是否新于资源修改时间——不新=根本没重建（②/⓪ 只部署不构建）；新但内容不对=看 `%TEMP%\ModBuildEditorWindow_build.log` 命令行的 `-executeMethod` 类名是否与所选 Mod 一致（当日事故根因：Mod构建工具窗口重开只恢复下拉选中、不加载按 Mod 构建方法，方法字段停留全局默认 `AeonsEchoSpineModBuilder.BuildMod`，选 OtherSpine 实建 AeonsEchoSpine，随后一键流程的部署步骤又把 OtherSpine 旧产物重拷一遍——已修复：恢复选中态同步重载按 Mod 方法 + 偏离约定构建前告警，详见 editor-extension-system skill）。编辑器里 MOD 项目显示正常不代表 bundle 新——编辑器直接读 `Assets/` 源资源，不走 bundle
