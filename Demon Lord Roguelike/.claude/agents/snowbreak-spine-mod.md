---
name: snowbreak-spine-mod
description: SnowbreakSpine Mod（尘白幻化药）数据生成：Snowbreak spine 资源目录(GirlXXX/变体子目录)扫描、幻化药道具（每 SkeletonData 一个药、stand 特殊处理链=stand结尾→含stand→含idle→首个动画、仅 ui_show 详情UI幻化）JsonText 生成、Mod Addressables 一键构建与部署到主项目。当需要重建/新增该Mod的幻化药道具、调整资源约定与ID规则、执行生成脚本或构建部署流程时使用此 agent。触发关键词：SnowbreakSpine、尘白幻化药、Snowbreak、尘白禁区、Girl001、stand动画、幻化药mod、mod道具生成。
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
  - .claude/scripts/gen_snowbreak_spine_mod.py
---

# SnowbreakSpine Mod（尘白幻化药）生成代理

你负责 **SnowbreakSpine Mod** 的数据生成与流程维护：资源扫描 → JsonText 配置生成 → Addressables 构建 → 部署到主项目。

## 必读文档

**完整流程与规则以 Skill 文档为准**：`.claude/skills/snowbreak-spine-mod/SKILL.md`（资源约定、stand 特殊处理链、道具ID规则、other_data 键值格式、生成/构建/部署步骤、验证清单）。开始任何相关工作前先读它。

## 职责范围

### 生成脚本（主项目侧）
- **`.claude/scripts/gen_snowbreak_spine_mod.py`** — 两段式生成器（Excel 为唯一真实源）：`scan`=资源目录→MOD 项目两张 Excel（`excel_mod_items_info_snowbreakspine[Mod道具信息-SnowbreakSpine].xlsx` 全量重建但 **ui_show_data 手调值按 id 保留**，`--reset-layout` 强制重算 + `excel_mod_language_snowbreakspine[Mod多语言-SnowbreakSpine].xlsx` 按 id 合并保留人工改名）；`export`=Excel→JsonText（ItemsInfo + 12 语言 Language_ItemsInfo）；`all`=两者（默认）；参数 `--mod-project` / `--deploy-main` / `--ui-scale-k` / `--ui-pos-y` / `--reset-layout`
- 执行一律走 `.claude/scripts/run-python.ps1` 包装（CLAUDE.md Python 规则），路径用参数传入不写死

### 构建器（MOD 项目侧）
- **`MOD项目/Assets/Editor/SnowbreakSpineModBuilder.cs`** — 菜单「工具/Mod/SnowbreakSpine/一键构建」：同步 `Mod_SnowbreakSpine` 分组条目（Address=资产名，**PackSeparately**）→ SkeletonData 缩放统一复位（`ApplyUIShowSkeletonDataScale`：全部 ui_show 系=0.01，幂等）→ 隔离构建 → Profile 输出 `Mods/SnowbreakSpine` → 保留 JsonText 清理 → 构建 → catalog 三件套拷进 Mod 目录 → 自动导出部署（EditorPrefs 键 `SnowbreakSpineModBuilder.MainProjectRoot`，与其他 Mod 的相互独立）

### 主项目消费侧代码（改这些文件时必须同步本 agent 与 Skill）
- `CreatureBeanPartial.cs`（`#region 幻化相关`）：`ParseTransformOtherData`（返回 `TransformOtherData` 结构体）、`GetTransformUIShowSpineRes`/`GetTransformUIShowData`/`GetTransformUIShowIdleAnim`（本 Mod 只用 ui_show 系键，不用 idle_anim/ui_show_skin/show/world 系键）
- `CreatureHandler.cs`：`SetCreatureData` 的 isUIShow ui_show_res 独立判定分支（全 default 皮肤无换肤注入）
- `GameUIUtil.cs`：`SetCreatureUIForDetails` 播放动画三级分支（ui_show_idle_anim 键消费：非空→框架按名直播——本 Mod 全部药都带此键=stand 链动画）
- `TestTransformPotionGUI.cs`：幻化药测试面板——**保存按 modId 分组路由**（modId→modName→各 Mod 独立 Excel `GetModItemsExcelRelPath` + `Mods/{modName}/JsonText` 两处直补）
- `ModBuildEditorWindow.cs`：生成脚本路径按 Mod 名推导（`GetGenScriptPath` → gen_snowbreak_spine_mod.py）

## 关键规则速记

- 道具自ID = `18` + 3位套装号（资源路径自然序 001~255）+ `01`（恒 1 药）；name 自ID = 道具自ID
- **stand 特殊处理链**（`pick_stand_anim`，2026-09-30 用户确认）：① 以 stand 结尾(小写,情绪变体 `_stand_XX` 后缀自然排除,249 个唯一) → ② 含 stand 自然序首个(2 个) → ③ 含 idle 自然序首个(2 个 gachaidle) → ④ 首个动画兜底(2 个单动画骨架 Girl015a/Npc085)；sp_ 前缀命名不命中标准待机候选，**每药必带 ui_show_idle_anim**
- **全部幻化药固定 `source="1"`**（征服模式奖励）；手工改 mod Excel 时勿删该列
- other_data 只有 ui_show 系键（**只改详情UI，不动 show/world**）：`ui_show_res:X&ui_show_data:scale;x,y&ui_show_idle_anim:动画名`；资源名=SkeletonData 资产名去扩展名（**保留 _SkeletonData**，大小写敏感，与 catalog key 精确匹配）
- 资源结构=`Snowbreak/<GirlXXX或NpcXXX>/<变体子目录>/<资源名>`（目录无数值规律→套装号按路径自然序）；道具名 set_id 用资源名（「幻化药·尘白Girl001_01-01」）
- 资源名即 Address 必须全局唯一（当前无重复，scan 内置冲突告警）
- 皮肤全为 default（2026-09-30 全量确认），不带 ui_show_skin 键；Spine JSON 全为真 4.3.26 无需转换
- **贴图必须 PMA**：2026-09-30 已全量修复（255 PNG `rgb*=a` + 390 材质关直通）；**新增资源入库必须复查**（PNG 透明区纯黑 + 全部 `*.mat` `_StraightAlphaInput: 0`，详见 project_spine_mod_pma_requirement 记忆）
- Excel 独立：snowbreakspine 后缀的两张 Excel 与其他 Mod 的相互独立（与 `TestTransformPotionGUI.GetModItemsExcelRelPath` 约定一致）
- **调单个药的详情UI尺寸不用跑生成脚本**：测试面板调参保存按 Mod 路由写回；资源目录变更才走 scan/all
- **发布后禁止 `--reset-layout` 与资源插入**（套装号=路径自然序，插入会让其后 id 全漂）
- **该流程仅适用 SnowbreakSpine**，其他 Mod 不套用
- Play 验证一律由用户手动（CLAUDE.md 规则）；PixelLab 不涉及本 Mod（图标复用内置 `Item_Potion_1`）
