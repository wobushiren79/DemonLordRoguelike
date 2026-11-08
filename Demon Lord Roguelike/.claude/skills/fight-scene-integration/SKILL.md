---
name: fight-scene-integration
description: 将已有 Unity 场景预制体或建筑预制体接入魔王太嚣张的正式战斗场景。用户说「把这个 prefab 加进战斗场景」「给挑战、征服、无尽绑定或替换战斗地图」或要求修复已接入场景的道路、镜头、光照、屋檐闪烁和粒子比例时使用。覆盖 Addressables 登记、配置表导出、模式场地池、旧存档与画面验收。
watched_files:
  - Assets/LoadResources/FightScene/
  - Assets/AddressableAssetsData/AssetGroups/FightScene.asset
  - Assets/Data/Excel/excel_fight_scene[战斗场景].xlsx
  - Assets/Resources/JsonText/FightScene.txt
  - Assets/Scripts/Component/Handler/WorldHandler.cs
  - Assets/Scripts/Component/Manager/WorldManager.cs
---

# 预制场景接入战斗

## 1. 确认接入范围

1. 读项目 `CLAUDE.md`，确认源 prefab、目标模式、世界、难度及普通/BOSS 分支。区分「可从战斗测试工具加载」「加入现有场地池」「某模式专用」三种结果。终焉议会在 `WorldHandler` 走单独的 `LoadDoomCouncilScene`，先查实际入口。现有建筑已完成时，从它的正式 prefab 开始；还在参考图建模阶段时读 [场景制作流程](../blender-scene-from-reference/SKILL.md)。
2. 查源 prefab 的当前引用、模型依赖、地面高度、镜头可见范围，以及目标模式的道路数量和长度。先记录现有用户改动。编辑 Unity 序列化资源前，按 [连接流程](../unity-mcp-connection/SKILL.md) 确认实例和编辑器状态；通过 Unity 的 prefab/AssetDatabase API 修改并保存，回读确认。

## 2. 生成战斗 prefab 并登记

1. 按同类战斗 prefab 建立 `Assets/LoadResources/FightScene/FightScene_<主题>_<编号>.prefab`。把源建筑作为嵌套 prefab，用包装层处理朝向、等比缩放、位置、近景切口和该场地的灯光。检查预览专用相机、后处理、UI、灯光等是否会与运行时对象叠加。源模型确需修网格时，只改故障部位并保留原材质、UV、法线和实际需要的碰撞。
2. 顺着 `WorldHandler.LoadFightScene` 核对运行时摆放。普通战斗会重设根的位置及旋转，根 Z 由 `-(sceneRoadNumMax - sceneRoadNum) / 2f` 偏移；需要保留的美术摆放写在子层。道路由运行时另外加载和缩放。普通战斗分支会确保根节点有 `ScenePrefabForFight` 并调用 `InitSceneData`，场景实例的动态表现统一挂在该组件；其 `enableSkyboxMotion` 默认关闭，只有天空石台等明确需要动态天空盒的预制体才启用并配置曝光/偏航速度。以实际道路格、生成点和地面高度检查覆盖范围、穿模、悬空及 Z-fighting；多种路数都要检查，包含可支持的最大路数。
3. 用 Unity API 把包装 prefab 登记到 Addressables 的 `FightScene` 组，地址使用项目完整资源路径（含 `.prefab`）。核对 GUID 与地址唯一且对应同一资产，嵌套建筑、网格、材质、贴图及特效依赖可解析。不要为了此类加载另加一个 Build Settings 场景。

4. 终焉议会及暴力说服复用 Common 组的 DoomCouncilScene.prefab。建筑挂在议会场景根节点下，随战斗分支的根变换定位。DoomCouncilArchitectureView 根据主相机位置隐藏挡住画面的近侧围墙，东侧用低墙剖视。LoadDoomCouncilScene(forBattle: true) 在战斗入口打开门扇，普通议会保持关门。保留能覆盖门外出生点的正式地面。

## 3. 配表、绑定模式与兼容旧档

1. 读 [配置表读写](../excel-io/SKILL.md)。通过项目 `.claude/scripts/run-python.ps1` 使用 openpyxl 修改 `Assets/Data/Excel/excel_fight_scene[战斗场景].xlsx` 的 `FightScene` 工作表。检查已用 ID 与编号规则，新增行按 ID 升序。`name_res` 填 prefab 文件名（含扩展名）；加载时与 `PathInfo.FightScenePrefabPath` 拼成完整 Addressables 地址。沿用同类配置的必要字段，确认 `skybox_mat` 可以加载；按画面需要配置环境光、雾、景深、道路颜色/透明度和 `Details` 变体。
2. 按入口更新目标场地池，核对 `FightBean.fightSceneId` 最终取值。挑战100勇士的 `FightTypeChallengeHundredInfo.fight_scene_ids` 使用逗号；征服的 `FightTypeConquerInfo.fight_scene_ids` 和 `fight_scene_boss_ids` 使用 `&`。无尽当前使用同难度征服 BOSS 场地池；需要为两种模式指定不同场地时，先核对是否有独立配置入口，再按 `CLAUDE.md` 的约定处理需要调整配置来源的变更。按 [传送门流程](../portal-system/SKILL.md) 查清实际调用链，专用场地只进入授权的入口和分支。
3. 检查已生成传送门会不会继续读旧冻结值。若需要让旧档迁移，仅在场地缺失或已移出目标行场地池时重选并写回，保留仍有效的冻结值、道路、难度、敌人和奖励。挑战100勇士已有 `GetChallengeHundredFightSceneId` / `IsMatchFightScene`；征服和无尽当前只懒补非正数 ID，有效性迁移需另行处理。模式分流时，核对预生成、缺值补齐、无冻结数据兜底是否读同一个目标池，同时验证首战和推进到 BOSS 关两种入口。
4. 用项目 Unity 导出器同步本次改动的表到 `Assets/Resources/JsonText/`，包括模式表；通过 `ExcelUtil.ExcelToJsonItem(FileInfo, jsonFolderPath)` 定向导出时，实际回读 JSON，防止导出静默失败或拼错中文文件名。核对两端的行、字段类型和 ID 都一致，清理此次临时备份。只修改数据行时沿用已有 Bean。

## 4. 检查画面与控制

| 症状或检查点 | 做法 |
|---|---|
| 沙地、墙面有细亮线 | 先在同机位分别关道路渲染、可疑模型投影和主光阴影，找出是哪一层产生亮线。若来自装饰洞顶或岩肋的实时阴影缝，只关相关渲染器的 Cast Shadows，再调主光、补光与环境光；保留必要的场景和角色投影。 |
| 屋檐、砖面闪烁 | 检查同段内外表面、相邻段端面及双面材质。用网格平面与投影重叠验证共面，再收边或去掉重复面。保留镜头能看到的切口端面，验收缩进后有无明显缝隙。 |
| 火盆、壁炬需要动态火焰 | 先找已有 prefab，例如 `Effect_Fire_7`。需要替换模型火焰时，仅拆旧火焰与火星几何，保留火具、炭块和合适的灯光。按火具的实际锚点挂实例，并核对发射朝向。 |
| 粒子缩小后变细、变长或散开 | 读取实例最终 `lossyScale`，包含外层战斗 prefab 的缩放。对 `Effect_Fire_7` 的等比 `Hierarchy` 缩放，世界空间主火、火星和烟要用**共享原版值 × 最终比例**计算重力及速度上限；初速度已随层级缩放，局部空间 Glow 不做这项补偿。只覆盖各使用场景的实例，源建筑与战斗包装层比例不同时分别核对。用相同随机种子、相机和多个时间点对比；其它粒子先实测模块行为再确定缩放规则。 |
| 镜头到边界露出切口或模型背面 | 从实际跟随偏移换算锚点限位和相机世界坐标。开场及 WASD、鼠标拖拽共用同一个限制；按目标场地与路数收窄，避免影响别的战斗。检查四周、切口以及近景遮挡。 |
| 光照或景深与预览不同 | 用运行时的环境光、道路材质参数、雾、镜头和 Volume 设置作对照。优先按场景配表调节；道路透明度来自 `road_alpha` 经 MaterialPropertyBlock 设置的 `_Alpha`，空值或 0 回退为 0.5。检查前后景层次以及全排单位、网格和技能提示的辨识度，退场后检查全局渲染设置是否还原。 |

## 5. 验收与交付

- **编辑模式**：回读 prefab、Addressables、Excel 和导出 JSON；核对目标模式/难度、普通及 BOSS 分支、有效与失效的旧冻结场地。检查依赖、相机锚点、边界画面和粒子表现；如改了 C#，检查 Unity 编译与 Console。编辑模式截图或模拟要注明覆盖范围。
- **实际战斗**：按项目规则由用户手动 Play。给出可操作的复测路径，覆盖实际入口、新旧传送门、可支持的最大路数、镜头移动、放置及刷怪、退场重进。没有 Play 反馈时，把实际运行效果标为待验收。
- **收尾**：在 Unity 项目目录运行 `powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".claude/scripts/check-watched.ps1" -Json`，对未跟踪的新资源补查 `watched_files`，同步受影响的项目指南。说明新增场地 ID、绑定范围、实际通过的检查与待复测项。

需要查此次实际接入的参数、诊断过程或复测方法时，读 [Embervault 接入案例](references/embervault.md)。

## 红冠宫殿决战场地

- `FightScene` ID `30002`，Addressables 地址 `Assets/LoadResources/FightScene/FightScene_Palace_CrimsonCrown_1.prefab`。内嵌 `Building_Palace_CrimsonCrown_Cutaway_1`，子层位置 `(5.5, -0.061, 4.72)`、Y 轴旋转 `180°`、缩放 `(0.68, 0.58, 0.58)`，保留战斗所需的横向间距。两侧柱心局部 X 为 `±10.17`，柱本地缩放 `(0.75, 1, 0.85)`，柱旗与柱身留约 `0.013` 战斗场景单位。用角色站姿约 1 单位校准家具，长椅、花盆和王座等比世界缩放分别为 `0.45`、`0.43`、`0.36`，对应长椅坐面高约 `0.41`、王座高约 `1.58`，王座底面贴台顶。
- 战斗版 12 座烛台共用 `Assets/LoadResources/Fbx/Building/Palace/CrimsonCrown_BattleCandelabrum.asset`。用图集 UV2 去掉原网格的 186 个火焰三角面，补同图集金色轴杆连接中间烛座。烛台等比世界缩放 `0.30`，烛芯高约 `0.69`，第二排局部 Z `8`，第三排仍落在台阶前平地。36 个烛芯各自嵌套 `Effect_Fire_7`，只开启主火和 Glow。火焰局部缩放 `0.19`，主火世界空间重力和速度上限按最终 `0.057` 比例补偿。烛台点光强度 `0.55`、范围 `2.3`。
- 战斗平地的地毯保留红色织纹和两侧金边，移除中央花纹。`CrimsonCrown_BattleRoyalDais.asset` 把大厅局部高度 `0.074` 的两张地毯三角面分到独立材质槽，使用 `Mat_Building_Palace_CrimsonCrown_CombatCarpet_1` 和 `palace_crimson_crown_combat_carpet_1.png`。台阶、王座台及碰撞体继续使用原资源。
- 世界 1 的征服难度 1–10 使用 BOSS 池 `30001&30002`，无尽按同难度复用。新随机时两座宫殿等概率，已冻结的场地继续沿用。`30002` 使用 `Skybox_6`、环境光 `#99847A`、道路透明度 `0.1`，场地雾和体积雾留空。
- 已在编辑模式按战斗根节点偏移回读 2–6 路的地面和出生区，核对 8 面柱旗、王座底面、12 座烛台的连接件与 36 个火焰实例。实机验收可在 `TestScene` 的 `Launcher` 选战斗测试场景 `[30002]`，手动 Play 查看角色与王座比例、烛火、放置、出怪和镜头边界，再用新征服传送门验证最后一关切换。
