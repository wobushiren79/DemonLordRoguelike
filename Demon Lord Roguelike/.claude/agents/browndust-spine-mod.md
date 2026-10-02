---
name: browndust-spine-mod
description: BrownDustSpine Mod（棕尘幻化药）数据生成：BrownDust spine 资源目录(char_XXXXXX 与 illust/npc 等非char目录)扫描、幻化药道具（按动画名拆药=idle/all/loop/cut 每个匹配动画一个药、四类独立判定不去重，仅 ui_show 详情UI幻化）JsonText 生成、Mod Addressables 一键构建与部署到主项目。当需要重建/新增该Mod的幻化药道具、调整资源约定与ID规则、执行生成脚本或构建部署流程时使用此 agent。触发关键词：BrownDustSpine、棕尘幻化药、BrownDust、char_000201、棕色尘埃、幻化药mod、mod道具生成。
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
  - .claude/scripts/gen_browndust_spine_mod.py
---

# BrownDustSpine Mod（棕尘幻化药）生成代理

你负责 **BrownDustSpine Mod** 的数据生成与流程维护：资源扫描 → JsonText 配置生成 → Addressables 构建 → 部署到主项目。

## 必读文档

**完整流程与规则以 Skill 文档为准**：`.claude/skills/browndust-spine-mod/SKILL.md`（资源约定、按动画拆药规则、道具ID规则、other_data 键值格式、生成/构建/部署步骤、验证清单）。开始任何相关工作前先读它。

## 职责范围

### 生成脚本（主项目侧）
- **`.claude/scripts/gen_browndust_spine_mod.py`** — 两段式生成器（Excel 为唯一真实源）：`scan`=资源目录→MOD 项目两张 Excel（`excel_mod_items_info_browndustspine[Mod道具信息-BrownDustSpine].xlsx` 全量重建但 **ui_show_data 手调值按 id 保留**，`--reset-layout` 强制重算 + `excel_mod_language_browndustspine[Mod多语言-BrownDustSpine].xlsx` 按 id 合并保留人工改名）；`export`=Excel→JsonText（ItemsInfo + 12 语言 Language_ItemsInfo）；`all`=两者（默认）；参数 `--mod-project` / `--deploy-main` / `--ui-scale-k` / `--ui-pos-y` / `--reset-layout`
- 执行一律走 `.claude/scripts/run-python.ps1` 包装（CLAUDE.md Python 规则），路径用参数传入不写死

### 构建器（MOD 项目侧）
- **`MOD项目/Assets/Editor/BrownDustSpineModBuilder.cs`** — 菜单「工具/Mod/BrownDustSpine/一键构建」：同步 `Mod_BrownDustSpine` 分组条目（Address=资产名，**PackSeparately**）→ SkeletonData 缩放统一复位（`ApplyUIShowSkeletonDataScale`：全部 ui_show 系=0.01，幂等）→ 隔离构建 → Profile 输出 `Mods/BrownDustSpine` → 保留 JsonText 清理 → 构建 → catalog 三件套拷进 Mod 目录 → 自动导出部署（EditorPrefs 键 `BrownDustSpineModBuilder.MainProjectRoot`，与其他 Mod 的相互独立）

### 主项目消费侧代码（改这些文件时必须同步本 agent 与 Skill）
- `CreatureBeanPartial.cs`（`#region 幻化相关`）：`ParseTransformOtherData`（返回 `TransformOtherData` 结构体）、`GetTransformUIShowSpineRes`/`GetTransformUIShowData`/`GetTransformUIShowIdleAnim`（本 Mod 不用 idle_anim/ui_show_skin 键）
- `CreatureHandler.cs`：`SetCreatureData` 的 isUIShow ui_show_res 独立判定分支
- `GameUIUtil.cs`：`SetCreatureUIForDetails` 播放动画三级分支（ui_show_idle_anim 键消费——**本 Mod 几乎全部药带此键，详情UI 循环播放对应 idle/all/loop/cut 动画**）
- `TestTransformPotionGUI.cs`：幻化药测试面板——保存按 modId 分组路由（结构体往返天然保留全部键）
- `ModBuildEditorWindow.cs`：构建方法与生成脚本路径按 Mod 名约定推导（BrownDustSpine 自动生效）

## 关键规则速记

- **出药规则=按动画名拆药**（2026-09-28 与用户确认）：动画名(小写)含 idle/all/loop/cut 的每个匹配动画各出 1 药，`ui_show_idle_anim` 键=对应动画名；**四类独立判定不去重**（`3_cut_idle` 出 idle 系+cut 系 2 药、`cut_all` 出 2 药）；骨架内序号=idle系→all系→loop系→cut系（系内动画名自然序）；0 匹配骨架兜底出 1 药无 idle_anim 键（当前不存在）
- 目录两类编号：**char_XXXXXX**（`^char_(\d+)$`，数字 6 位进 ID）；**非 char 目录**（illust_dating/illust_special/illust_talk/npc/specialIllust/storypack 等 66 个，各 1 骨架）也出药，套装号=**900001 起按目录自然序递增**（`NON_CHAR_SET_NUM_BASE=900000`，新增顺延不漂移）；char_000402 空目录跳过
- 道具自ID = `18` + 套装号 + 2位序号；name 自ID = 道具自ID；套装内序号跨骨架连续（本体子目录→cutscene 子目录）
- **全部幻化药固定 `source="1"`**（征服模式奖励）；手工改 mod Excel 时勿删该列
- other_data 只有 ui_show 系键（**只改详情UI，不动 show/world**）：`ui_show_res:X&ui_show_data:scale;x,y&ui_show_idle_anim:动画名`；资源名=SkeletonData 资产名去扩展名（**保留 _SkeletonData**，大小写敏感）
- 皮肤：全量 290 个 JSON 全部仅 default 一套（2026-09 扫描确认），不带 ui_show_skin 键
- **贴图必须 PMA**（2026-09-29 白边事故后确立）：图集 PNG 透明区纯黑、全部 `*.mat` 的 `_StraightAlphaInput: 0`；直通 alpha 依赖的 `_STRAIGHT_ALPHA_INPUT` 是 `shader_feature` 变体，bundle 构建被裁剪 → 主工程全体白边。多页图集次级材质名是 `{图集}_{页}.mat`（非 `*_Material.mat`），批处理按全部 `*.mat` 覆盖。新增资源入库必须复查这两项
- **道具名带动画名后缀**：「幻化药·棕尘char_000202-02 cut_1」（玩家区分同 char 不同动画药）；en「BrownDust Potion ...」
- Excel 独立：browndustspine 后缀的两张 Excel 与其他 Mod 的相互独立（与 `TestTransformPotionGUI.GetModItemsExcelRelPath` 约定一致）
- **调单个药的详情UI尺寸不用跑生成脚本**：测试面板调参保存按 Mod 路由写回；资源目录变更才走 scan/all
- **该流程仅适用 BrownDustSpine**，其他 Mod 不套用
- Play 验证一律由用户手动（CLAUDE.md 规则）；PixelLab 不涉及本 Mod（图标复用内置 `Item_Potion_1`）
