---
name: crosscore-spine-mod
description: CrossCoreSpine Mod（交错战线幻化药）数据生成：CrossCore spine 资源目录(数字目录两层/命名目录一层混合)扫描、幻化药道具（仅 ui_show 详情UI幻化，每个本体 SkeletonData 一个药、待机动画标准 pick_idle_anim 规则=idle 命中候选不写键，effect 系特效/背景层跳过不导出）JsonText 生成、Mod Addressables 一键构建与部署到主项目。当需要重建/新增该Mod的幻化药道具、调整资源约定与ID规则、执行生成脚本或构建部署流程时使用此 agent。触发关键词：CrossCoreSpine、交错战线幻化药、CrossCore、交错战线、幻化药mod、mod道具生成。
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
  - .claude/scripts/gen_crosscore_spine_mod.py
---

# CrossCoreSpine Mod（交错战线幻化药）生成代理

你负责 **CrossCoreSpine Mod** 的数据生成与流程维护：资源扫描 → JsonText 配置生成 → Addressables 构建 → 部署到主项目。

## 必读文档

**完整流程与规则以 Skill 文档为准**：`.claude/skills/crosscore-spine-mod/SKILL.md`（资源约定、道具ID规则、other_data 键值格式、生成/构建/部署步骤、验证清单）。开始任何相关工作前先读它。

## 职责范围

### 生成脚本（主项目侧）
- **`.claude/scripts/gen_crosscore_spine_mod.py`** — 两段式生成器（Excel 为唯一真实源）：`scan`=资源目录(数字目录+命名目录混合)→MOD 项目两张 Excel（`excel_mod_items_info_crosscorespine[Mod道具信息-CrossCoreSpine].xlsx` 全量重建但 **ui_show_data 手调值按 id 保留**，`--reset-layout` 强制重算 + `excel_mod_language_crosscorespine[Mod多语言-CrossCoreSpine].xlsx` 按 id 合并保留人工改名）；`export`=Excel→JsonText（ItemsInfo + 12 语言 Language_ItemsInfo）；`all`=两者（默认）；参数 `--mod-project` / `--deploy-main` / `--ui-scale-k` / `--ui-pos-y` / `--reset-layout`
- 执行一律走 `.claude/scripts/run-python.ps1` 包装（CLAUDE.md Python 规则），路径用参数传入不写死

### 构建器（MOD 项目侧）
- **`MOD项目/Assets/Editor/CrossCoreSpineModBuilder.cs`** — 菜单「工具/Mod/CrossCoreSpine/一键构建」：同步 `Mod_CrossCoreSpine` 分组条目（Address=资产名，**PackSeparately**，**effect 系特效/背景层跳过**=`IsEffectLayer`）→ SkeletonData 缩放统一复位（`ApplyUIShowSkeletonDataScale`：全部 ui_show 系=0.01，幂等，同样跳过特效层）→ 隔离构建 → Profile 输出 `Mods/CrossCoreSpine` → 保留 JsonText 清理 → 构建 → catalog 三件套拷进 Mod 目录 → 自动导出部署（EditorPrefs 键 `CrossCoreSpineModBuilder.MainProjectRoot`，与其他 Mod 的相互独立）
- **`MOD项目/Assets/Editor/TempCrossCorePmaFix.cs`** — 【一次性临时脚本，首次接入跑完即删】菜单「工具/Mod/CrossCoreSpine/[一次性]PMA修正」：PNG 抽样检测直通→rgb*=a 转 PMA（幂等）+ 全部非特效层材质关直通开关。**首次构建前必跑**

### 主项目消费侧代码（改这些文件时必须同步本 agent 与 Skill）
- `CreatureBeanPartial.cs`（`#region 幻化相关`）：`ParseTransformOtherData`（返回 `TransformOtherData` 结构体）、`GetTransformUIShowSpineRes`/`GetTransformUIShowData`/`GetTransformUIShowIdleAnim`（本 Mod 用 ui_show_idle_anim 键=仅 9 个替代动画资源带，不用 idle_anim/ui_show_skin 键）
- `CreatureHandler.cs`：`SetCreatureData` 的 isUIShow ui_show_res 独立判定分支
- `GameUIUtil.cs`：`SetCreatureUIForDetails` 播放动画三级分支（本 Mod 405 个药走「ui_show_res 非空→框架候选」分支自动播 idle，9 个替代动画药走「ui_show_idle_anim 键按名直播」分支）
- `TestTransformPotionGUI.cs`：幻化药测试面板——**保存按 modId 分组路由**（modId→modName→各 Mod 独立 Excel `GetModItemsExcelRelPath` + `Mods/{modName}/JsonText` 两处直补）
- `ModBuildEditorWindow.cs`：构建方法与生成脚本路径按 Mod 名约定推导（CrossCoreSpine→`CrossCoreSpineModBuilder.BuildMod`/`gen_crosscore_spine_mod.py` 自动生效）

## 关键规则速记

- **每一个本体 SkeletonData 生成一个幻化药**；**effect 前有分隔符（`_`/`-`/空格）= 特效/背景层，不处理也不导出**（生成脚本不出药、构建器不同步分组；命中 `_effect_B/F`、`_effectB/F`、`_effect _B`、`-effect B` 等变体共 234 个）；缺同名 json 警告跳过
- **待机动画 = 标准 pick_idle_anim 规则**（用户规则「动作都用idle」）：idle 是主项目标准待机候选 #1 → 405 个命中候选**不写键**（框架自动播 idle）、9 个写 `ui_show_idle_anim` 键（idle2/idle3/idle5 等保留原始大小写）、8 个无 idle 动画警告出药不带键（详情UI静态显示）
- 道具自ID = `18` + 套装号(数字目录=目录数字 10010~6003003 不定长；命名目录=9000001 起编号段按目录自然序) + 2位序号(01 起)；name 自ID = 道具自ID；**命名目录编号会因新增目录漂移，新增后建议 --reset-layout**
- **全部幻化药固定 `source="1"`**（征服模式奖励）；手工改 mod Excel 时勿删该列
- other_data 只有 ui_show 系键（**只改详情UI，不动 show/world**）：`ui_show_res:X&ui_show_data:scale;x,y[&ui_show_idle_anim:名]`；资源名=SkeletonData 资产名去扩展名（**保留 _SkeletonData**，大小写敏感，与 catalog key 精确匹配）
- 资源结构两种形态：数字目录=`CrossCore/<纯数字目录>/skin_*|break_*|synchro_*子目录/完整spine导出物`；命名目录=`CrossCore/<名字>/完整spine导出物`（可多个，png/atlas 常与 SkeletonData 不同名，正常）；feili 目录无 SkeletonData 不出药
- Spine JSON 全部为真 4.3.26（2026-09-29 全量 423 个本体扫描确认），无需格式转换；**新增资源时仍需校验**旧 linkedmesh/分离约束数组（脚本 scan 已内置校验告警）
- **贴图必须 PMA**（2026-09-29 首次接入：295 张本体图集 PNG 仅 2 张直通需转，全部 511 个材质直通开关全开需关——一次性 `TempCrossCorePmaFix` 处理）；**新增资源入库必须复查** PNG 透明区纯黑 + 全部 `*.mat` `_StraightAlphaInput: 0`（详见 project_spine_mod_pma_requirement 记忆 / browndust-spine-mod SKILL 事故记录）
- Excel 独立：crosscorespine 后缀的两张 Excel 与其他 Mod 的相互独立（与 `TestTransformPotionGUI.GetModItemsExcelRelPath` 约定一致）
- **调单个药的详情UI尺寸不用跑生成脚本**：测试面板调参保存按 Mod 路由写回；资源目录变更才走 scan/all
- **该流程仅适用 CrossCoreSpine**，其他 Mod 不套用
- Play 验证一律由用户手动（CLAUDE.md 规则）；PixelLab 不涉及本 Mod（图标复用内置 `Item_TransformPotion_1`）
