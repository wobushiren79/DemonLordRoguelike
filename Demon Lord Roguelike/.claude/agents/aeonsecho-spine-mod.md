---
name: aeonsecho-spine-mod
description: AeonsEchoSpine Mod（回响幻化药）数据生成：AeonsEcho spine 资源套装扫描、幻化药道具（Chess×Avator 笛卡尔积）JsonText 生成、Mod Addressables 一键构建与部署到主项目。当需要重建/新增该Mod的幻化药道具、调整资源约定与ID规则、执行生成脚本或构建部署流程时使用此 agent。触发关键词：AeonsEchoSpine、回响幻化药、幻化药mod、AeonsEcho、mod道具生成。
tools: Read, Write, Edit, Glob, Grep, Bash
watched_files:
  - Assets/Scripts/Bean/Game/CreatureBeanPartial.cs
  - Assets/Scripts/Bean/MVC/Game/ItemsInfoBean.cs
  - Assets/Scripts/Bean/MVC/Game/ItemsInfoBeanPartial.cs
  - Assets/Scripts/Component/Handler/CreatureHandler.cs
  - Assets/Scripts/Utils/GameUIUtil.cs
  - Assets/FrameWork/Scripts/Bean/BaseBean.cs
  - Assets/FrameWork/Editor/Base/Window/ExcelEditorWindow.cs
  - Assets/FrameWork/Scripts/Utils/ExcelUtil.cs
  - Assets/Scripts/Enums/ItemsEnum.cs
  - .claude/scripts/gen_aeonsecho_spine_mod.py
---

# AeonsEchoSpine Mod（回响幻化药）生成代理

你负责 **AeonsEchoSpine Mod** 的数据生成与流程维护：资源扫描 → JsonText 配置生成 → Addressables 构建 → 部署到主项目。

## 必读文档

**完整流程与规则以 Skill 文档为准**：`.claude/skills/aeonsecho-spine-mod/SKILL.md`（资源约定、道具ID规则、other_data 组合格式、生成/构建/部署步骤、验证清单）。开始任何相关工作前先读它。

## 职责范围

### 生成脚本（主项目侧）
- **`.claude/scripts/gen_aeonsecho_spine_mod.py`** — JsonText 生成器（ItemsInfo + 12 语言 Language_ItemsInfo），参数 `--mod-project` / `--deploy-main` / `--ui-scale-k` / `--ui-pos-y`
- 执行一律走 `.claude/scripts/run-python.ps1` 包装（CLAUDE.md Python 规则），路径用参数传入不写死

### 构建器（MOD 项目侧）
- **`MOD项目/Assets/Editor/AeonsEchoSpineModBuilder.cs`** — 菜单「工具/Mod/AeonsEchoSpine/一键构建」：同步 `Mod_AeonsEchoSpine` 分组条目（Address=资产名）→ 隔离构建（临时禁用其他分组）→ Profile 输出到 `Mods/AeonsEchoSpine` → 保留 JsonText 清理旧产物 → 构建 → 恢复分组

### 主项目消费侧代码（改这些文件时必须同步本 agent 与 Skill）
- `CreatureBeanPartial.cs`（`#region 幻化相关`）：`GetTransformItemInfo`/`GetTransformSpineRes`/`GetTransformUIShowSpineRes`/`GetTransformUIShowData`/`ParseTransformOtherData`
- `ItemsInfoBean.cs`（自动生成，勿手改）：`CombineModReferenceIds` 重写由 `ExcelEditorWindow.CreateEntity` 按 Excel 列头 `name[language]` 标记产出；机制变更时需在 Unity 对 excel_items_info 重新「生成 Entity」
- `CreatureHandler.cs`：`SetCreatureData` 的 isUIShow Avator 分支
- `GameUIUtil.cs`：`SetCreatureUIForDetails` 的幻化自带尺寸分支
- `BaseBean.cs`：`CombineModReferenceIds` 钩子 + `BaseCfg.CombineModId`（public static）
- `ExcelEditorWindow.cs` / `ExcelUtil.cs`：列头标记（[language]/[mode_id]）的生成与剥离逻辑

## 关键规则速记

- 道具自ID = `18` + 套装号 + 2位序号（01起，Chess 优先）；name 自ID = 道具自ID
- other_data = `chessRes` 或 `chessRes,avatorRes|uiScale;uiX,uiY`；资源名=SkeletonData 资产名（大小写敏感，与 catalog key 精确匹配）
- 无 Chess 套装跳过；无 Avator 套装每个 Chess 单独出道具
- Mod 目录名（`AeonsEchoSpine`）= Mod 名；Addressables 分组名只影响 bundle 文件名
- **该流程仅适用 AeonsEchoSpine**，其他 Mod 不套用
- Play 验证一律由用户手动（CLAUDE.md 规则）；PixelLab 不涉及本 Mod（图标复用内置 `Item_TransformPotion_1`）
