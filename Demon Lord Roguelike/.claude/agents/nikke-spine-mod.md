---
name: nikke-spine-mod
description: NikkeSpine Mod（妮姬幻化药）数据生成：Nikke spine 资源目录(cXXX_XX)扫描、幻化药道具（仅 ui_show 详情UI幻化，每个 SkeletonData 单独一个药，跳过 dummy 占位资源）JsonText 生成、Mod Addressables 一键构建与部署到主项目。当需要重建/新增该Mod的幻化药道具、调整资源约定与ID规则、执行生成脚本或构建部署流程时使用此 agent。触发关键词：NikkeSpine、妮姬幻化药、Nikke、c010、幻化药mod、mod道具生成。
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
  - .claude/scripts/gen_nikke_spine_mod.py
---

# NikkeSpine Mod（妮姬幻化药）生成代理

你负责 **NikkeSpine Mod** 的数据生成与流程维护：资源扫描 → JsonText 配置生成 → Addressables 构建 → 部署到主项目。

## 必读文档

**完整流程与规则以 Skill 文档为准**：`.claude/skills/nikke-spine-mod/SKILL.md`（资源约定、道具ID规则、other_data 键值格式、生成/构建/部署步骤、验证清单）。开始任何相关工作前先读它。

## 职责范围

### 生成脚本（主项目侧）
- **`.claude/scripts/gen_nikke_spine_mod.py`** — 两段式生成器（Excel 为唯一真实源）：`scan`=资源目录(cXXX_XX)→MOD 项目两张 Excel（`excel_mod_items_info_nikkespine[Mod道具信息-NikkeSpine].xlsx` 全量重建但 **ui_show_data 手调值按 id 保留**，`--reset-layout` 强制重算 + `excel_mod_language_nikkespine[Mod多语言-NikkeSpine].xlsx` 按 id 合并保留人工改名）；`export`=Excel→JsonText（ItemsInfo + 12 语言 Language_ItemsInfo）；`all`=两者（默认）；参数 `--mod-project` / `--deploy-main` / `--ui-scale-k` / `--ui-pos-y` / `--reset-layout`
- 执行一律走 `.claude/scripts/run-python.ps1` 包装（CLAUDE.md Python 规则），路径用参数传入不写死

### 构建器（MOD 项目侧）
- **`MOD项目/Assets/Editor/NikkeSpineModBuilder.cs`** — 菜单「工具/Mod/NikkeSpine/一键构建」：同步 `Mod_NikkeSpine` 分组条目（Address=资产名，**PackSeparately**）→ SkeletonData 缩放统一复位（`ApplyUIShowSkeletonDataScale`：全部 ui_show 系=0.01，幂等）→ 隔离构建 → Profile 输出 `Mods/NikkeSpine` → 保留 JsonText 清理 → 构建 → catalog 三件套拷进 Mod 目录 → 自动导出部署（EditorPrefs 键 `NikkeSpineModBuilder.MainProjectRoot`，与其他 Mod 的相互独立）

### 主项目消费侧代码（改这些文件时必须同步本 agent 与 Skill）
- `CreatureBeanPartial.cs`（`#region 幻化相关`）：`ParseTransformOtherData`（返回 `TransformOtherData` 结构体，2026-09-28 由多 out 参数重构）、`GetTransformUIShowSpineRes`/`GetTransformUIShowData`/`GetTransformUIShowIdleAnim`（本 Mod 不用 idle_anim/ui_show_skin 键）
- `CreatureHandler.cs`：`SetCreatureData` 的 isUIShow ui_show_res 独立判定分支
- `GameUIUtil.cs`：`SetCreatureUIForDetails` 播放动画三级分支（ui_show_idle_anim 键消费）
- `TestTransformPotionGUI.cs`：幻化药测试面板——**保存按 modId 分组路由**（modId→modName→各 Mod 独立 Excel `GetModItemsExcelRelPath` + `Mods/{modName}/JsonText` 两处直补）
- `ModBuildEditorWindow.cs`：构建方法与生成脚本路径按 Mod 名约定推导（NikkeSpine 自动生效）

## 关键规则速记

- **每一个 SkeletonData 单独生成一个幻化药**（不做套装组合、不按皮肤拆分）；`dummy_` 前缀资源跳过不出药；缺同名 json 警告跳过
- 道具自ID = `18` + 目录数字拼接(c010_00→01000，c9000_00→900000) + 2位序号；name 自ID = 道具自ID；目录内序号 = 文件名自然序（本体→aim→cover）
- **全部幻化药固定 `source="1"`**（征服模式奖励）；手工改 mod Excel 时勿删该列
- other_data 只有 ui_show 系键（**只改详情UI，不动 show/world**）：`ui_show_res:X&ui_show_data:scale;x,y[&ui_show_idle_anim:动画名]`；资源名=SkeletonData 资产名去扩展名（**保留 _SkeletonData**，大小写敏感，与 catalog key 精确匹配）
- **idle 动画替代规则**（2026-09-28 起，新 Mod 必须继承，详见 mod-system SKILL 通用规则节）：scan 时对每个 SkeletonData 同名 json 读 animations——命中主项目标准待机候选(idle,wait,idle1,wait1,stand,脚本动态读 SpineAnimationState.txt id=10001)不生成键；无候选取首个小写含 idle 的动画名写 `ui_show_idle_anim` 键；完全没有则不生成+警告。消费=`GameUIUtil.SetCreatureUIForDetails` 三级分支（`GetTransformUIShowIdleAnim` 非空→框架按名直播）
- 资源结构=`Nikke/cXXX_XX(目录)/完整spine导出物`；目录必须匹配 `^c(\d+)_(\d+)$`；同名 json=文件名去 `_SkeletonData.asset`+`.json`（读骨架高校准 scale=645/高）
- Spine JSON 全部为真 4.3.26，无需格式转换；**新增资源时仍需校验**旧 linkedmesh/分离约束数组（参照 aeonsecho-spine-mod SKILL）
- Excel 独立：nikkespine 后缀的两张 Excel 与其他 Mod 的相互独立（与 `TestTransformPotionGUI.GetModItemsExcelRelPath` 约定一致）
- **调单个药的详情UI尺寸不用跑生成脚本**：测试面板调参保存按 Mod 路由写回；资源目录变更才走 scan/all
- **该流程仅适用 NikkeSpine**，其他 Mod 不套用
- Play 验证一律由用户手动（CLAUDE.md 规则）；PixelLab 不涉及本 Mod（图标复用内置 `Item_Potion_1`）
