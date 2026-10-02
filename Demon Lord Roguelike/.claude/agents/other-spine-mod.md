---
name: other-spine-mod
description: OtherSpine Mod（Other幻化药）数据生成：Other spine 资源目录(角色名目录)扫描、幻化药道具（Avator=ui_show详情UI段/不带=基础show段、同目录前缀配对 X_Avator↔X、多皮肤Avator按具名皮肤拆药带 ui_show_skin 键）JsonText 生成、Mod Addressables 一键构建与部署到主项目。当需要重建/新增该Mod的幻化药道具、调整资源约定与ID规则、执行生成脚本或构建部署流程时使用此 agent。触发关键词：OtherSpine、Other幻化药、Other、幻化药mod、mod道具生成。
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
  - .claude/scripts/gen_other_spine_mod.py
---

# OtherSpine Mod（Other幻化药）生成代理

你负责 **OtherSpine Mod** 的数据生成与流程维护：资源扫描 → JsonText 配置生成 → Addressables 构建 → 部署到主项目。

## 必读文档

**完整流程与规则以 Skill 文档为准**：`.claude/skills/other-spine-mod/SKILL.md`（资源约定、道具ID规则、other_data 键值格式、生成/构建/部署步骤、验证清单）。开始任何相关工作前先读它。

## 职责范围

### 生成脚本（主项目侧）
- **`.claude/scripts/gen_other_spine_mod.py`** — 两段式生成器（Excel 为唯一真实源）：`scan`=资源目录(角色名目录)→MOD 项目两张 Excel（`excel_mod_items_info_otherspine[Mod道具信息-OtherSpine].xlsx` 全量重建但 **show_data/ui_show_data/world_data 三键手调值按「目录名/资源token」(remark 资源身份)保留**——按资源身份而非道具 id 保留，出药规则变化/资源增减致 id 漂移时也不贴错，2026-09-30 起 + **目录序号按目录名从 remark 回收保留**（新增取 max+1），`--reset-layout` 强制重算（含序号重排，仅首次/未发布用）+ `excel_mod_language_otherspine[Mod多语言-OtherSpine].xlsx` 按 id 合并保留人工改名）；`export`=Excel→JsonText（ItemsInfo + 12 语言 Language_ItemsInfo）；`all`=两者（默认）；参数 `--mod-project` / `--deploy-main` / `--ui-scale-k` / `--ui-pos-y` / `--show-scale-k` / `--show-pos-y` / `--reset-layout`
- 执行一律走 `.claude/scripts/run-python.ps1` 包装（CLAUDE.md Python 规则），路径用参数传入不写死

### 构建器（MOD 项目侧）
- **`MOD项目/Assets/Editor/OtherSpineModBuilder.cs`** — 菜单「工具/Mod/OtherSpine/一键构建」：同步 `Mod_OtherSpine` 分组条目（Address=资产名，**PackSeparately**，无背景层跳过）→ SkeletonData 缩放双向校准（`ApplySkeletonDataScale`：文件名带 Avator=ui_show 系 0.01、不带=基础 show 系 0.002，幂等）→ 隔离构建 → Profile 输出 `Mods/OtherSpine` → 保留 JsonText 清理 → 构建 → catalog 三件套拷进 Mod 目录 → 自动导出部署（EditorPrefs 键 `OtherSpineModBuilder.MainProjectRoot`，与其他 Mod 的相互独立）

### 主项目消费侧代码（改这些文件时必须同步本 agent 与 Skill）
- `CreatureBeanPartial.cs`（`#region 幻化相关`）：`ParseTransformOtherData`（返回 `TransformOtherData` 结构体）、`GetTransformSpineRes`/`GetTransformShowData`/`GetTransformUIShowSpineRes`/`GetTransformUIShowData`/`GetTransformUIShowSkin`/`GetTransformWorldData`/`GetTransformIdleAnim`/`GetTransformUIShowIdleAnim`
- `CreatureHandler.cs`：`SetCreatureData` 的 isUIShow ui_show_res 独立判定分支 + `hasTransform` 且皮肤名非空时 `ChangeSkeletonSkin` 按名整皮替换
- `GameUIUtil.cs`：`SetCreatureUIForDetails`（ui_show_data/待机动画三级分支）、`SetCreatureUIForSimple`（show_data）
- `TestTransformPotionGUI.cs`：幻化药测试面板——**保存按 modId 分组路由**（modId→modName→各 Mod 独立 Excel `GetModItemsExcelRelPath` + `Mods/{modName}/JsonText` 两处直补）；Excel 写盘用项目标准 `new ExcelPackage(FileInfo)` 模式（**严禁 `new FileStream + new ExcelPackage(fs)` 写模式**——本项目 EPPlus 下 Save() 无异常但未落盘，2026-09-30 两次"保存成功却被覆盖"事故的根因）+ 写后回读校验、保存失败弹 DisplayDialog 强提示、「已保存0个」按红字错误处理、未写盘成功的 Mod 组不清覆盖层（保留未保存修改供重试）；Excel/WPS 占用该 xlsx 仍会写盘失败，保存前须先关闭
- `ModBuildEditorWindow.cs`：构建方法与生成脚本路径按 Mod 名约定推导（OtherSpine→`OtherSpineModBuilder.BuildMod`/`gen_other_spine_mod.py` 自动生效）

## 关键规则速记

- **Avator 分段**：SkeletonData 名带 `Avator`=ui_show 详情UI段、不带=基础 show 段；**同目录配对=前缀匹配优先（`X_Avator`↔`X`），无前缀匹配的基础回落配对目录主 Avator**（base_key==目录名优先；如 Aoliweiya 1 Avator+2 基础 → A+Avator 与 B+Avator 各出药，一个 Avator 可被多基础共享）；每个基础出药，未被配对的 Avator 出仅详情UI幻化药
- **多皮肤拆分**：Avator 骨架 skins 每个具名皮肤（≠default）出 1 药带 `ui_show_skin` 键；**多皮肤时不生成默认 default 皮肤药**，仅 default 单皮肤才出 1 普通药；当前 32 配对普通药 + 13 仅基础 + 4 仅 Avator + 55 皮肤药 = 104 药（12 个多皮肤 Avator 全是表情皮肤 angry/wait/weixiao 等）
- **无背景层跳过**：`Kuluoxierback` 是角色背面图正常出药（与 GirlWarsSpine 的 Back/Front/BG 跳过规则相反，勿混用）
- **待机动画走通用 idle 检测**（mod-system SKILL）：当前 104 骨架全命中标准候选（idle/wait/idle1/wait1/stand，其中 13 个 Avator 待机=wait）均不带 idle 键；无标准候选时自动取首个含 idle 动画名写 idle_anim/ui_show_idle_anim 键
- 道具自ID = `18` + 4位目录序号(按目录名自然序分配,重建时回收保留) + 2位序号(01 起)；name 自ID = 道具自ID
- **全部幻化药固定 `source="1"`**（征服模式奖励）；手工改 mod Excel 时勿删该列
- other_data 键：`show_res/show_data(3159/高校准,0,-120)` + `ui_show_res/ui_show_data(645/高校准,0,0)` + 皮肤药 `ui_show_skin`；资源名=SkeletonData 资产名去扩展名（**保留 _SkeletonData**，大小写敏感，与 catalog key 精确匹配）；**仅基础药无 ui_show 段键，但详情UI回落 show 形象仍消费 ui_show_data**（消费与有无 ui_show_res 无关）——测试面板大卡列表/单个预览可手调补上（标签标注（无Avator)，键缺失时首次调整基线取卡片图标当前显示值防跳变，2026-09-30 起），scan 重建按「目录名/资源token」同机制保留（append_item 兜底，同 world_data）
- Spine JSON 全部为真 4.3.26（2026-09-29 全量 104 个扫描确认），无需格式转换；**新增资源时仍需校验**旧 linkedmesh/分离约束数组（脚本 scan 已内置校验告警）
- **贴图必须 PMA**：本 Mod 已确认（81 材质全 `_StraightAlphaInput: 0` + PNG 抽样透明区纯黑）；**新增资源入库必须复查**（详见 project_spine_mod_pma_requirement 记忆）
- Excel 独立：otherspine 后缀的两张 Excel 与其他 Mod 的相互独立（与 `TestTransformPotionGUI.GetModItemsExcelRelPath` 约定一致）
- **调单个药的尺寸不用跑生成脚本**：测试面板调参保存按 Mod 路由写回（三尺寸键）；资源目录变更才走 scan/all
- **发布后禁止 `--reset-layout`**（目录序号重排会让已发道具 id 漂移、存档幻化串角色）
- **该流程仅适用 OtherSpine**，其他 Mod 不套用
- Play 验证一律由用户手动（CLAUDE.md 规则）；PixelLab 不涉及本 Mod（图标复用内置 `Item_TransformPotion_1`）
