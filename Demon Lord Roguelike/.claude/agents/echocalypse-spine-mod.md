---
name: echocalypse-spine-mod
description: EchocalypseSpine Mod（绯色回响幻化药）数据生成：Echocalypse spine 资源目录(knight/纯数字目录)扫描、幻化药道具（每 SkeletonData 一个药、idle 通用检测规则、_bg_ 背景不出药不进包、仅 ui_show 详情UI幻化）JsonText 生成、Mod Addressables 一键构建与部署到主项目。当需要重建/新增该Mod的幻化药道具、调整资源约定与ID规则、执行生成脚本或构建部署流程时使用此 agent。触发关键词：EchocalypseSpine、绯色回响幻化药、Echocalypse、绯色回响、200003、幻化药mod、mod道具生成。
tools: Read, Write, Edit, Glob, Grep, Bash
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

# EchocalypseSpine Mod（绯色回响幻化药）生成代理

你负责 **EchocalypseSpine Mod** 的数据生成与流程维护：资源扫描 → JsonText 配置生成 → Addressables 构建 → 部署到主项目。

## 必读文档

**完整流程与规则以 Skill 文档为准**：`.claude/skills/echocalypse-spine-mod/SKILL.md`（资源约定、道具ID规则、other_data 键值格式、生成/构建/部署步骤、验证清单）。开始任何相关工作前先读它。

## 职责范围

### 生成脚本（主项目侧）
- **`.claude/scripts/gen_echocalypse_spine_mod.py`** — 两段式生成器（Excel 为唯一真实源）：`scan`=资源目录→MOD 项目两张 Excel（`excel_mod_items_info_echocalypsespine[Mod道具信息-EchocalypseSpine].xlsx` 全量重建但 **ui_show_data 手调值按 id 保留**，`--reset-layout` 强制重算 + `excel_mod_language_echocalypsespine[Mod多语言-EchocalypseSpine].xlsx` 按 id 合并保留人工改名）；`export`=Excel→JsonText（ItemsInfo + 12 语言 Language_ItemsInfo）；`all`=两者（默认）；参数 `--mod-project` / `--deploy-main` / `--ui-scale-k` / `--ui-pos-y` / `--reset-layout`
- 执行一律走 `.claude/scripts/run-python.ps1` 包装（CLAUDE.md Python 规则），路径用参数传入不写死

### 构建器（MOD 项目侧）
- **`MOD项目/Assets/Editor/EchocalypseSpineModBuilder.cs`** — 菜单「工具/Mod/EchocalypseSpine/一键构建」：同步 `Mod_EchocalypseSpine` 分组条目（Address=资产名，**PackSeparately**，**资产名含 `_bg_` 的背景排除**）→ SkeletonData 缩放统一复位（`ApplyUIShowSkeletonDataScale`：全部 ui_show 系=0.01，幂等，同跳过 `_bg_`）→ 隔离构建 → Profile 输出 `Mods/EchocalypseSpine` → 保留 JsonText 清理 → 构建 → catalog 三件套拷进 Mod 目录 → 自动导出部署（EditorPrefs 键 `EchocalypseSpineModBuilder.MainProjectRoot`，与其他 Mod 的相互独立）

### 主项目消费侧代码（改这些文件时必须同步本 agent 与 Skill）
- `CreatureBeanPartial.cs`（`#region 幻化相关`）：`ParseTransformOtherData`（返回 `TransformOtherData` 结构体）、`GetTransformUIShowSpineRes`/`GetTransformUIShowData`/`GetTransformUIShowIdleAnim`（本 Mod 只用 ui_show 系键，不用 idle_anim/ui_show_skin/show/world 系键）
- `CreatureHandler.cs`：`SetCreatureData` 的 isUIShow ui_show_res 独立判定分支（全 default 皮肤无换肤注入）
- `GameUIUtil.cs`：`SetCreatureUIForDetails` 播放动画三级分支（ui_show_idle_anim 键消费：非空→框架按名直播=idle_A 三药；空→框架候选=314 药的 idle）
- `TestTransformPotionGUI.cs`：幻化药测试面板——**保存按 modId 分组路由**（modId→modName→各 Mod 独立 Excel `GetModItemsExcelRelPath` + `Mods/{modName}/JsonText` 两处直补）
- `ModBuildEditorWindow.cs`：生成脚本路径按 Mod 名推导（`GetGenScriptPath`：gen_{modName去Spine后缀小写}_spine_mod.py → gen_echocalypse_spine_mod.py）

## 关键规则速记

- 道具自ID = `18` + 目录数字(6~8位) + `01`（每目录恒 1 骨架；多骨架时按资源名自然序递增）；name 自ID = 道具自ID
- **每骨架 1 药**，待机动画走 mod-system 通用 idle 检测（`pick_idle_anim`+`load_std_idle_candidates` 读 SpineAnimationState.txt id=10001）：命中候选(idle,wait,idle1,wait1,stand)省略键（314 个），否则首个含 idle 动画名写 `ui_show_idle_anim`（3 个 idle_A=20500050/20700600/20700720）
- **`_bg_` 背景双侧排除**：生成器（`BG_MARK` 跳过 284 个不出药）+ 构建器（`BgMark` 不进分组不打 bundle）；放开需两侧同改
- **全部幻化药固定 `source="1"`**（征服模式奖励）；手工改 mod Excel 时勿删该列
- other_data 只有 ui_show 系键（**只改详情UI，不动 show/world**）：`ui_show_res:X&ui_show_data:scale;x,y[&ui_show_idle_anim:动画名]`；资源名=SkeletonData 资产名去扩展名（**保留 _SkeletonData**，大小写敏感，与 catalog key 精确匹配）
- **资源名即 Address 必须全局唯一**：scan 内置冲突告警；knight/400020 曾嵌套不同的 400010（与 knight/400010 重名）=多余资源已删（2026-09-30 用户确认）；未来特例用脚本 `SKIP_PATHS` 登记
- 目录名必须**纯数字**才分配套装号（非数字目录跳过+告警）
- **贴图 PMA**：1081 页 PNG 原生 PMA 已确认；1234 材质直通开关已全部置 0；**新增资源入库必须复查**（PNG 透明区纯黑 + 全部 `*.mat` 含 Multiply/Screen 混合材质 `_StraightAlphaInput: 0`，详见 project_spine_mod_pma_requirement 记忆）
- Spine JSON 全为真 4.3.26 无需转换；scan 已内置旧格式校验告警（面向未来新增）
- 皮肤全为 default（2026-09-30 全量确认），不带 ui_show_skin 键
- Excel 独立：echocalypsespine 后缀的两张 Excel 与其他 Mod 的相互独立（与 `TestTransformPotionGUI.GetModItemsExcelRelPath` 约定一致）
- **调单个药的详情UI尺寸不用跑生成脚本**：测试面板调参保存按 Mod 路由写回；资源目录变更才走 scan/all
- **发布后禁止 `--reset-layout` 与目录改名**（套装号=目录数字，改动即 id 漂移）
- **该流程仅适用 EchocalypseSpine**，其他 Mod 不套用
- Play 验证一律由用户手动（CLAUDE.md 规则）；PixelLab 不涉及本 Mod（图标复用内置 `Item_Potion_1`）
