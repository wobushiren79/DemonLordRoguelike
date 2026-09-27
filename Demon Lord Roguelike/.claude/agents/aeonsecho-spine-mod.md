---
name: aeonsecho-spine-mod
description: AeonsEchoSpine Mod（回响幻化药）数据生成：AeonsEcho spine 资源套装扫描、幻化药道具（Chess×ui_show 笛卡尔积，ui_show=Avator/Secretary/Elf/AVG）JsonText 生成、Mod Addressables 一键构建与部署到主项目。当需要重建/新增该Mod的幻化药道具、调整资源约定与ID规则、执行生成脚本或构建部署流程时使用此 agent。触发关键词：AeonsEchoSpine、回响幻化药、幻化药mod、AeonsEcho、mod道具生成。
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

**完整流程与规则以 Skill 文档为准**：`.claude/skills/aeonsecho-spine-mod/SKILL.md`（资源约定、道具ID规则、other_data 键值格式、生成/构建/部署步骤、验证清单）。开始任何相关工作前先读它。

## 职责范围

### 生成脚本（主项目侧）
- **`.claude/scripts/gen_aeonsecho_spine_mod.py`** — 两段式生成器（Excel 为唯一真实源）：`scan`=资源套装→MOD 项目两张 Excel（`Assets/Data/Excel/excel_mod_items_info[Mod道具信息].xlsx` 全量重建 + `excel_mod_language[Mod多语言].xlsx` 按 id 合并保留人工改名）；`export`=Excel→JsonText（ItemsInfo + 12 语言 Language_ItemsInfo）；`all`=两者（默认）；参数 `--mod-project` / `--deploy-main` / `--ui-scale-k` / `--ui-pos-y` / `--ui-chess-scale-k` / `--ui-chess-pos-y`（小卡 chess 尺寸校准）
- 执行一律走 `.claude/scripts/run-python.ps1` 包装（CLAUDE.md Python 规则），路径用参数传入不写死

### 构建器（MOD 项目侧）
- **`MOD项目/Assets/Editor/AeonsEchoSpineModBuilder.cs`** — 菜单「工具/Mod/AeonsEchoSpine/一键构建」：同步 `Mod_AeonsEchoSpine` 分组条目（Address=资产名，**PackSeparately 每资源一个 bundle 按需加载**）→ SkeletonData 缩放双向校准（`ApplyChessSkeletonDataScale`：Chess 系=`ChessSkeletonDataScale=0.002f` 非 ui_show_spine 显示 ×1/5；ui_show 系 Avator/Secretary/Elf/AVG 主动复位=`UIShowSkeletonDataScale=0.01f`，幂等）→ 隔离构建（临时禁用其他分组）→ Profile 输出到 `Mods/AeonsEchoSpine` → 保留 JsonText 清理旧产物 → 构建 → 恢复分组 → catalog 三件套从 `Library/com.unity.addressables/aa/Windows` 拷进 Mod 目录

### 主项目消费侧代码（改这些文件时必须同步本 agent 与 Skill）
- `CreatureBeanPartial.cs`（`#region 幻化相关`）：`GetTransformItemInfo`/`GetTransformSpineRes`/`GetTransformUIShowSpineRes`/`GetTransformUIShowData`/`GetTransformShowData`/`ParseTransformOtherData`（**6 出参**，2026-09-24 起含 `ui_show_skin` 键——ArkReSpine 多皮肤药用，本 Mod 不用）；两个尺寸 Get 在编辑器下测试覆盖层优先（见下）
- `TransformPotionUITestOverride.cs`（`#if UNITY_EDITOR` 整文件）：幻化药 UI 尺寸/位置测试覆盖层，key=幻化药完整id，被两个尺寸 Get 优先消费
- `TestTransformPotionGUI.cs`：Mod 幻化药测试面板（测试模式-卡片测试入口，四个页签：单个预览/小卡列表/大卡列表/场景列表；列表带 Mod筛选/横竖个数与间距可调(项目内 `ProjectSettings/TestTransformPotionLayout.json` 持久化随git共享)/网格右移避开面板/每项[还原][0,0][复制][粘贴]小按钮/拖拽4px阈值）——文本框+滑动条(缩放对数映射)+悬停滚轮缩放+拖拽改位置调 ui_show_data/show_data/world_data 实时预览，底栏「保存全部修改(N)」批量写回：**按 modId 分组路由**（2026-09-24 起，modId→modName→各 Mod 独立 Excel：AeonsEchoSpine=历史文件名 `excel_mod_items_info[Mod道具信息].xlsx`，后续 Mod=`excel_mod_items_info_{modName小写}[...]` 约定见 `GetModItemsExcelRelPath`；EPPlus 一次写盘，会话每文件首写前备份 `MOD项目/ExcelBackup/`）+ 直补 MOD 项目与主项目部署副本两处 `Mods/{modName}/JsonText/ItemsInfo.txt` + 当前会话内存（详见 Skill「调参写回」节）；`BuildOtherData` 保留 ui_show_skin 键、show_res 空时省略
- `ItemsInfoBean.cs`（自动生成，勿手改）：`CombineModReferenceIds` 重写由 `ExcelEditorWindow.CreateEntity` 按 Excel 列头 `name[language]` 标记产出；机制变更时需在 Unity 对 excel_items_info 重新「生成 Entity」
- `CreatureHandler.cs`：`SetCreatureData` 的 isUIShow ui_show_res 独立判定分支（有 ui_show_res 即替换详情UI形象并跳过原生物皮肤，不依赖 show_res——支持仅详情UI幻化道具；**例外**：配了 ui_show_skin 键的幻化药 hasTransform 时仍按名换肤 `SpineHandler.ChangeSkeletonSkin(Skeleton, string)`，ArkReSpine 多皮肤药专用，2026-09-24 起）+ world_data 世界显示注入（SkeletonAnimation 分支缩放=size_spine×体型×world倍率、spine 子节点 localPosition=偏移或归零恒管理防池化残留）
- `GameUIUtil.cs`：`SetCreatureUIForDetails` 的幻化自带尺寸分支（ui_show_data 键）、`SetCreatureUIForSimple` 的幻化小卡尺寸分支（show_data 键）
- `BaseBean.cs`：`CombineModReferenceIds` 钩子 + `BaseCfg.CombineModId`（public static）
- `ExcelEditorWindow.cs` / `ExcelUtil.cs`：列头标记（[language]/[mode_id]）的生成与剥离逻辑
- `ModBuildEditorWindow.cs`：生成脚本路径按 Mod 名推导（`GetGenScriptPath`：gen_{modName去Spine后缀小写}_spine_mod.py，2026-09-24 起）

## 关键规则速记

- 道具自ID = `18` + 套装号 + 2位序号（01起）；name 自ID = 道具自ID。混合套装序号排序 = ui_show 前缀组(Avator→Secretary→Elf→AVG)→Chess→组内变体自然序（单组套装与旧版 Chess×Avator 顺序一致；新前缀组整体排后，旧 id→组合映射不漂移）
- **全部幻化药固定 `source="1"`**（=ItemSourceEnum.ConquerReward 征服模式奖励：征服通关领奖随机一个魔晶位替换为池内随机幻化药，详见 fight-reward-system / item-system）——生成脚本 `ITEM_COLUMNS` 与 `compute_items` 已带 source 列，scan 全量重建不会丢；手工改 mod Excel 时勿删该列
- other_data 键值格式（& 拆项、: 拆键值，缺省键省略）三种形态：①完整 `show_res:X&ui_show_res:X&ui_show_data:scale;x,y&show_data:scale;x,y[&world_data:scale;x,y]`；②仅 Chess `show_res:X&show_data:scale;x,y[&world_data:...]`；③仅 ui_show（详情UI幻化，套装无 Chess）`ui_show_res:X&ui_show_data:scale;x,y`（show_res 可空，世界/小卡回落原生物形象）（show_data=小卡 chess 尺寸，`GetTransformShowData` → `GameUIUtil.SetCreatureUIForSimple` 替代原生物 ui_data_s；world_data=世界显示尺寸/偏移 `scale;x,y` x=横向/y=竖向，`GetTransformWorldData` → `CreatureHandler.SetCreatureData` 缩放乘算+spine子节点位置恒管理，无校准来源、scan 重建按 id 保留手调值）；资源名=SkeletonData 资产名（大小写敏感，与 catalog key 精确匹配）
- 套装组合规则：Chess×ui_show 笛卡尔积；仅 Chess→每个 Chess 单独出道具（只 show 段）；仅 ui_show→每个变体单独出道具（只 ui_show 段）；两类都没有→跳过。ui_show 前缀=脚本 `UI_SHOW_PREFIXES`（Avator/Secretary/Elf/AVG），其他命名文件夹不参与生成
- **调单个药的尺寸/位置不用跑生成脚本**：测试模式-卡片测试-Mod幻化药测试面板里拖拽/滚轮实时调，「保存全部修改」按 modId 分组路由写回本 Mod 的 Excel+两处 JsonText+内存（只换 ui_show_data/show_data/world_data 三键，其余键原样保留）；资源套装变更才走 scan/all 生成流程（scan 默认按 id 保留三键手调值、新增资源按骨架校准，`--reset-layout` 强制全部重算，2026-09-27 起）
- Mod 目录名（`AeonsEchoSpine`）= Mod 名；Addressables 分组名只影响 bundle 文件名
- **该流程仅适用 AeonsEchoSpine**，其他 Mod 不套用
- Play 验证一律由用户手动（CLAUDE.md 规则）；PixelLab 不涉及本 Mod（图标复用内置 `Item_TransformPotion_1`）
