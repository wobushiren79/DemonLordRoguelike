---
name: aeonsecho-spine-mod
description: AeonsEchoSpine Mod（回响幻化药）数据生成：AeonsEcho spine 资源套装扫描、幻化药道具（Chess×ui_show 笛卡尔积，ui_show=Avator/Secretary/Elf/AVG）JsonText 生成、Mod Addressables 一键构建与部署到主项目。当需要重建/新增该Mod的幻化药道具、调整资源约定与ID规则、执行生成脚本或构建部署流程时使用此 agent。触发关键词：AeonsEchoSpine、回响幻化药、幻化药mod、AeonsEcho、mod道具生成。
tools: Read, Write, Edit, Glob, Grep, Bash
watched_files:
  - Assets/Scripts/Bean/Game/CreatureBeanPartial.cs
  - Assets/Scripts/Bean/MVC/Game/ItemsInfoBean.cs
  - Assets/Scripts/Bean/MVC/Game/ItemsInfoBeanPartial.cs
  - Assets/Scripts/Component/Handler/CreatureHandler.cs
  - Assets/Scripts/Component/Handler/SpineHandler.cs
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
- **`MOD项目/Assets/Editor/AeonsEchoSpineModBuilder.cs`** — 菜单「工具/Mod/AeonsEchoSpine/一键构建」：同步 `Mod_AeonsEchoSpine` 分组条目（Address=资产名，**PackSeparately 每资源一个 bundle 按需加载**）→ SkeletonData 缩放双向校准（`ApplyChessSkeletonDataScale`：Chess 系=`ChessSkeletonDataScale=0.002f` 非 ui_show_spine 显示 ×1/5；ui_show 系 Avator/Secretary/Elf/AVG 主动复位=`UIShowSkeletonDataScale=0.01f`，幂等）→ 隔离构建(本组残留false自愈+0条目中止)（临时禁用其他分组）→ Profile 输出到 `Mods/AeonsEchoSpine` → 保留 JsonText 清理旧产物 → 构建 → 恢复分组 → catalog 三件套从 `Library/com.unity.addressables/aa/Windows` 拷进 Mod 目录

### 主项目消费侧代码（改这些文件时必须同步本 agent 与 Skill）
- `CreatureBeanPartial.cs`（`#region 幻化相关`）：`GetTransformItemInfo`/`GetTransformSpineRes`/`GetTransformUIShowSpineRes`/`GetTransformUIShowData`/`GetTransformShowData`/`GetTransformIdleAnim`/`GetTransformUIShowIdleAnim`/`ParseTransformOtherData`（返回 `TransformOtherData` 结构体，2026-09-28 由多 out 参数重构——新增键=结构体加字段+解析加 case，调用点零改动；含 `idle_anim`/`ui_show_idle_anim` 键=show/ui_show 骨架替代待机动画）；两个尺寸 Get 在编辑器下测试覆盖层优先（见下）
- `TransformPotionUITestOverride.cs`（`#if UNITY_EDITOR` 整文件）：幻化药 UI 尺寸/位置测试覆盖层，key=幻化药完整id，被两个尺寸 Get 优先消费
- `TestTransformPotionGUI.cs`：Mod 幻化药测试面板（测试模式-卡片测试入口，四个页签：单个预览/小卡列表/大卡列表/场景列表；列表带 Mod筛选/横竖个数与间距可调(项目内 `ProjectSettings/TestTransformPotionLayout.json` 持久化随git共享)/网格右移避开面板/每项[还原][0,0][复制][粘贴]小按钮(场景列表另有「⇪粘贴剪贴板到全部场景项」一键套用剪贴板 world_data 到当前筛选全部项, 2026-10-08起)/拖拽4px阈值）——文本框+滑动条(缩放对数映射)+悬停滚轮缩放+拖拽改位置调 ui_show_data/show_data/world_data 实时预览，底栏「保存全部修改(N)」批量写回：**按 modId 分组路由**（2026-09-24 起，modId→modName→各 Mod 独立 Excel：AeonsEchoSpine=历史文件名 `excel_mod_items_info[Mod道具信息].xlsx`，后续 Mod=`excel_mod_items_info_{modName小写}[...]` 约定见 `GetModItemsExcelRelPath`；EPPlus 一次写盘，会话每文件首写前备份 `MOD项目/ExcelBackup/`（滚动复用 .bak.1~3 只留最近 3 份））+ 直补 MOD 项目与主项目部署副本两处 `Mods/{modName}/JsonText/ItemsInfo.txt` + 当前会话内存（详见 Skill「调参写回」节）；`BuildOtherData` 保留 ui_show_skin 键、show_res 空时省略
- `ItemsInfoBean.cs`（自动生成，勿手改）：`CombineModReferenceIds` 重写由 `ExcelEditorWindow.CreateEntity` 按 Excel 列头 `name[language]` 标记产出；机制变更时需在 Unity 对 excel_items_info 重新「生成 Entity」
- `CreatureHandler.cs`：`SetCreatureData` 的 isUIShow ui_show_res 独立判定分支（有 ui_show_res 即替换详情UI形象并跳过原生物皮肤，不依赖 show_res——支持仅详情UI幻化道具；**例外**：配了 ui_show_skin 键的幻化药 hasTransform 时仍按名换肤 `SpineHandler.ChangeSkeletonSkin(Skeleton, string)`，ArkReSpine 多皮肤药专用，2026-09-24 起；皮肤串支持「|」分隔多皮肤，拆分后多个改调 params 叠加重载——CherryTaleSpine 组合皮药用，2026-09-29 起）+ world_data 世界显示注入（SkeletonAnimation 分支缩放=size_spine×体型×world倍率、spine 子节点 localPosition=偏移或归零恒管理防池化残留）
- `GameUIUtil.cs`：`SetCreatureUIForDetails` 的幻化自带尺寸分支（ui_show_data 键）+ 播放动画三级分支（ui_show_idle_anim 键消费，2026-09-28 起）、`SetCreatureUIForSimple` 的幻化小卡尺寸分支（show_data 键）
- `SpineHandler.cs`（游戏层）：`GetAnimNameAppoint` 的幻化守卫分支——show 段幻化骨架按 Idle/Walk/Attack/Dead 四状态各查幻化药映射动画键（idle_anim/walk_anim/attack_anim/dead_anim，`GetTransformIdleAnim`/`GetTransformWalkAnim`/`GetTransformAttackAnim`/`GetTransformDeadAnim` 非空优先按名直播，缺省交框架候选解析；四状态分支 2026-10-02 起，此前仅 Idle 例外）
- `BaseBean.cs`：`CombineModReferenceIds` 钩子 + `BaseCfg.CombineModId`（public static）
- `ExcelEditorWindow.cs` / `ExcelUtil.cs`：列头标记（[language]/[mode_id]）的生成与剥离逻辑
- `ModBuildEditorWindow.cs`：生成脚本路径按 Mod 名推导（`GetGenScriptPath`：gen_{modName去Spine后缀小写}_spine_mod.py，2026-09-24 起）

## 关键规则速记

- 道具自ID = `18` + 套装号 + 2位序号（01起）；name 自ID = 道具自ID。混合套装序号排序 = ui_show 前缀组(Avator→Secretary→Elf→AVG)→Chess→组内变体自然序（单组套装与旧版 Chess×Avator 顺序一致；新前缀组整体排后，旧 id→组合映射不漂移）
- **全部幻化药固定 `source="1"`**（=ItemSourceEnum.ConquerReward 征服模式奖励：征服通关领奖随机一个魔晶位替换为池内随机幻化药，详见 fight-reward-system / item-system）——生成脚本 `ITEM_COLUMNS` 与 `compute_items` 已带 source 列，scan 全量重建不会丢；手工改 mod Excel 时勿删该列
- other_data 键值格式（& 拆项、: 拆键值，缺省键省略）三种形态：①完整 `show_res:X&ui_show_res:X&ui_show_data:scale;x,y&show_data:scale;x,y[&world_data:scale;x,y][&idle_anim:X][&ui_show_idle_anim:X][&walk_anim:X][&attack_anim:X][&dead_anim:X]`；②仅 Chess `show_res:X&show_data:scale;x,y[&world_data:...][&idle_anim:X][&walk_anim:X][&attack_anim:X][&dead_anim:X]`；③仅 ui_show（详情UI幻化，套装无 Chess）`ui_show_res:X&ui_show_data:scale;x,y[&ui_show_idle_anim:X]`（show_res 可空，世界/小卡回落原生物形象）（show_data=小卡 chess 尺寸，`GetTransformShowData` → `GameUIUtil.SetCreatureUIForSimple` 替代原生物 ui_data_s；world_data=世界显示尺寸/偏移 `scale;x,y` x=横向/y=竖向，`GetTransformWorldData` → `CreatureHandler.SetCreatureData` 缩放乘算+spine子节点位置恒管理，无校准来源、scan 重建按 id 保留手调值）；资源名=SkeletonData 资产名（大小写敏感，与 catalog key 精确匹配）
- **idle 动画替代规则**（2026-09-28 起，新 Mod 必须继承，详见 mod-system SKILL 通用规则节）：scan 时对出药 SkeletonData 同名 json 读 animations——命中主项目标准待机候选(idle,wait,idle1,wait1,stand,脚本动态读 SpineAnimationState.txt id=10001)不生成键；无候选取首个小写含 idle 的动画名：show 段 Chess 骨架写 `idle_anim` 键、ui_show 段变体骨架写 `ui_show_idle_anim` 键；完全没有则不生成+警告。消费：show 段=游戏层 `SpineHandler.GetAnimNameAppoint`（Idle 分支 `GetTransformIdleAnim` 优先）；详情UI=`GameUIUtil.SetCreatureUIForDetails` 三级分支（`GetTransformUIShowIdleAnim` 非空→框架按名直播）
- **attack 动画替代规则**（2026-10-02 起，用户拍板 attack→skill 或 skill1）：scan 时对 Chess 骨架检测主项目标准攻击候选(attack,attack1,脚本动态读 SpineAnimationState.txt id=30001)——命中不生成键；无候选按 `skill1`→`skill` 顺序取首个大小写不敏感全等命中动画名写 `attack_anim` 键（仅 show 段 Chess 检测）；都没有则不生成+警告（当前仅 5001_Chess 1 段次，56 段次命中替代）。**dead 动画替代规则·方案B**（2026-10-02 起，用户拍板）：无标准死亡候选(dead,dead1,die,id=40001)时有 `attacked` 写 `dead_anim:attacked`（受击抖一下再消失）、否则回退该骨架实际待机动画名（当前 65 段次=attacked×10/idle×55）。消费：游戏层 `SpineHandler.GetAnimNameAppoint` 幻化守卫分支已扩展为 Idle/Walk/Attack/Dead 四状态各查映射键（`GetTransformWalkAnim`/`GetTransformAttackAnim`/`GetTransformDeadAnim`）。**Walk 走映射键**（2026-10-09 起，用户拍板：无 walk 候选时用 idle 映射）：scan 检测主项目标准移动候选(walk,walk1,move,move1,move2,脚本动态读 SpineAnimationState.txt id=20001)——命中省略；无候选回退该骨架实际待机动画名（标准待机候选原始名→首个小写含 idle 的动画名）写 `walk_anim` 键；连 idle 都没有则不生成+警告（当前 379 个 Chess 段次=63 命中标准候选/316 替代 idle/0 无可用替代；2026-10-02 的 id=20001 候选扩容 move1,move2 保留作候选层）
- 套装组合规则：Chess×ui_show 笛卡尔积；仅 Chess→每个 Chess 单独出道具（只 show 段）；仅 ui_show→每个变体单独出道具（只 ui_show 段）；两类都没有→跳过。ui_show 前缀=脚本 `UI_SHOW_PREFIXES`（Avator/Secretary/Elf/AVG），其他命名文件夹不参与生成
- **调单个药的尺寸/位置不用跑生成脚本**：测试模式-卡片测试-Mod幻化药测试面板里拖拽/滚轮实时调，「保存全部修改」按 modId 分组路由写回本 Mod 的 Excel+两处 JsonText+内存（只换 ui_show_data/show_data/world_data 三键，其余键原样保留）；资源套装变更才走 scan/all 生成流程（scan 默认按 id 保留三键手调值、新增资源按骨架校准，`--reset-layout` 强制全部重算，2026-09-27 起）
- Mod 目录名（`AeonsEchoSpine`）= Mod 名；Addressables 分组名只影响 bundle 文件名
- **该流程仅适用 AeonsEchoSpine**，其他 Mod 不套用
- Play 验证一律由用户手动（CLAUDE.md 规则）；PixelLab 不涉及本 Mod（图标复用内置 `Item_Potion_1`）
