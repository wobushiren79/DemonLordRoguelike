---
name: fight-scene-integration
description: 将已有 Unity 场景预制体或建筑预制体接入魔王太嚣张的正式战斗场景。用户说「把这个 prefab 加进战斗场景」「给挑战、征服、无尽绑定或替换战斗地图」或要求修复已接入场景的道路、镜头、光照、屋檐闪烁和粒子比例时使用。覆盖 Addressables 登记、配置表导出、模式场地池、旧存档与画面验收。
watched_files:
  - Assets/LoadResources/FightScene/
  - Assets/AddressableAssetsData/AssetGroups/FightScene.asset
  - Assets/Data/Excel/excel_fight_scene[战斗场景].xlsx
  - Assets/Resources/JsonText/FightScene.txt
  - Assets/Data/Excel/excel_fight_type_infinite_info[战斗-无尽模式].xlsx
  - Assets/Resources/JsonText/FightTypeInfiniteInfo.txt
  - Assets/Scripts/Bean/Game/FightBeanForInfinite.cs
  - Assets/Scripts/Component/Handler/WorldHandler.cs
  - Assets/Scripts/Component/Manager/WorldManager.cs
---

# 预制场景接入战斗

## 1. 确认接入范围

1. 读项目 `CLAUDE.md`，确认源 prefab、目标模式、世界、难度及普通/BOSS 分支。区分「可从战斗测试工具加载」「加入现有场地池」「某模式专用」三种结果。终焉议会在 `WorldHandler` 走单独的 `LoadDoomCouncilScene`，先查实际入口。现有建筑已完成时，从它的正式 prefab 开始；还在参考图建模阶段时读 [场景制作流程](../blender-scene-from-reference/SKILL.md)。
2. 查源 prefab 的当前引用、模型依赖、地面高度、镜头可见范围，以及目标模式的道路数量和长度。先记录现有用户改动。编辑 Unity 序列化资源前，按 [连接流程](../unity-mcp-connection/SKILL.md) 确认实例和编辑器状态；通过 Unity 的 prefab/AssetDatabase API 修改并保存，回读确认。

3. **单张贴图缩小**。导入 3D 战斗场景前，先按材质与 UV 的实际用途区分独立贴图和图集。将多块纹理打包到同一张图供模型分区采样的图集（如 `embervault_pixel_atlas.png`）保留原始尺寸。独立贴图的源文件任一边超过 256 像素时，用项目的[像素图编辑器](../pixel-image-editor/SKILL.md)把宽高缩到原尺寸约 1/8。2048×2048 → 256×256，1024×1024 或 1028×1028 → 128×128，512×512 → 64×64。非方形贴图选接近目标且能保持原宽高比的网格档位。没有合适档位时先确认目标尺寸。仅盘点已有资源时，列出超限贴图的路径和尺寸，标明哪些是图集，待用户选定独立贴图后再处理。
   - 从 Unity 菜单 **Custom → 工具弹窗 → 像素图编辑器** 打开工具。步骤①载入源图并设置目标网格宽高，以磁盘源图尺寸计算目标。步骤②居中适配，选「最近邻颜色」，关闭「限制最终颜色数」，确认整张图没有裁切或补边后转换。步骤③以 **×1** 导出 PNG 并回读文件尺寸。
   - 替换已有 PNG 时保留路径和 .meta。用原材质复核透明度和数据通道，法线贴图保留 Normal map 导入类型。

## 2. 生成战斗 prefab 并登记

1. 按同类战斗 prefab 建立 `Assets/LoadResources/FightScene/FightScene_<主题>_<编号>.prefab`。把源建筑作为嵌套 prefab，用包装层处理朝向、等比缩放、位置、近景切口和该场地的灯光。检查预览专用相机、后处理、UI、灯光等是否会与运行时对象叠加。普通战斗默认使用游戏天空盒。源模型含预览用天空穹顶、球体或背景面片时，在战斗包装层移除或禁用这些对象，让镜头显示配置的天空盒。源模型确需修网格时，只改故障部位并保留原材质、UV、法线和实际需要的碰撞。
2. 顺着 `WorldHandler.LoadFightScene` 核对运行时摆放。普通战斗会重设根的位置及旋转，根 Z 由 `-(sceneRoadNumMax - sceneRoadNum) / 2f` 偏移；需要保留的美术摆放写在子层。道路由运行时另外加载和缩放。普通战斗分支会确保根节点有 `ScenePrefabForFight` 并调用 `InitSceneData`，场景实例的动态表现统一挂在该组件；其 `enableSkyboxMotion` 默认关闭，只有天空石台等明确需要动态天空盒的预制体才启用并配置曝光/偏航速度。以实际道路格、生成点和地面高度检查覆盖范围、穿模、悬空及 Z-fighting；多种路数都要检查，包含可支持的最大路数。
3. 用 Unity API 把包装 prefab 登记到 Addressables 的 `FightScene` 组，地址使用项目完整资源路径（含 `.prefab`）。核对 GUID 与地址唯一且对应同一资产，嵌套建筑、网格、材质、贴图及特效依赖可解析。不要为了此类加载另加一个 Build Settings 场景。

4. 终焉议会及暴力说服复用 Common 组的 `DoomCouncilScene.prefab`。建筑挂在议会场景根节点下，随战斗分支的根变换定位。`ScenePrefabForDoomCouncil` 根据主相机位置隐藏近侧围墙，保留原有投影，东侧用低墙剖视。两种模式均保持关门，普通议会的 `Board` 限制角色走路和突进。`LoadDoomCouncilScene(forBattle: true)` 调用 `SetForBattle` 切换讲台墙剖视规则。大厅缩短时从门端调整，保留讲台与后墙的原有间距。暴力说服使用单路 7 格部署区，议员以世界 X=8.1 为出生中心（±0.25 随机偏移），从末格与关门之间入场。议会分支用 `MaterialPropertyBlock` 为地毯上方的棋盘指定酒红 `#621C2E`、暖铜 `#C29158` 与 `0.35` 不透明度。

## 3. 配表、绑定模式与兼容旧档

1. 读 [配置表读写](../excel-io/SKILL.md)。通过项目 `.claude/scripts/run-python.ps1` 使用 openpyxl 修改 `Assets/Data/Excel/excel_fight_scene[战斗场景].xlsx` 的 `FightScene` 工作表。检查已用 ID 与编号规则，新增行按 ID 升序。`name_res` 填 prefab 文件名（含扩展名），加载时与 `PathInfo.FightScenePrefabPath` 拼成完整 Addressables 地址。`skybox_mat` 优先选用 `Skybox` 组中已登记的 `Assets/FrameWork/Materials/Skybox/` 材质，按构图填写 `skybox_rotate`。现有材质无法满足明确的美术需求时，优先把新天空做成兼容旋转参数的天空盒材质并登记到同组。按画面需要配置环境光、雾、景深、道路颜色/透明度和 `Details` 变体。`camera_move_limit` 用 `x:min,max&z:min,max` 配置实际相机世界 X/Z 范围，可按轴省略；默认 X=3~7、Z=-4.5~-1，竞技场 50001 配 `z:-4,-1`。
2. 按入口更新目标场地配置，核对 `FightBean.fightSceneId` 最终取值。挑战100勇士的 `FightTypeChallengeHundredInfo.fight_scene_ids` 使用逗号，征服的 `FightTypeConquerInfo.fight_scene_ids` 和 `fight_scene_boss_ids` 使用 `&`。无尽用 `FightTypeInfiniteInfo.fight_scene_id` 按世界和难度指定单一 `FightScene` ID，0 或留空回退同难度征服 BOSS 场地。按 [传送门流程](../portal-system/SKILL.md) 核对专用场地只由目标正式战斗入口选用。
3. 检查已有传送门。`FightBeanForInfinite.InitData` 在进场时优先读取当前无尽场地，因此新旧无尽传送门均使用专属配置，旧冻结值仍可用于回退。征服只懒补缺失场地，挑战100勇士通过 `GetChallengeHundredFightSceneId` / `IsMatchFightScene` 在冻结场地已移出配置池时重选。核对征服首战和决战、无尽测试入口以及无尽传送门的进场结果。
4. 用项目 Unity 导出器同步本次改动的表到 `Assets/Resources/JsonText/`，包括模式表；通过 `ExcelUtil.ExcelToJsonItem(FileInfo, jsonFolderPath)` 定向导出时，实际回读 JSON，防止导出静默失败或拼错中文文件名。核对两端的行、字段类型和 ID 都一致，清理此次临时备份。只修改数据行时沿用已有 Bean。

## 4. 检查画面与控制

| 症状或检查点 | 做法 |
|---|---|
| 沙地、墙面有细亮线 | 先在同机位分别关道路渲染、可疑模型投影和主光阴影，找出是哪一层产生亮线。若来自装饰洞顶或岩肋的实时阴影缝，只关相关渲染器的 Cast Shadows，再调主光、补光与环境光；保留必要的场景和角色投影。 |
| 屋檐、砖面闪烁 | 检查同段内外表面、相邻段端面及双面材质。用网格平面与投影重叠验证共面，再收边或去掉重复面。保留镜头能看到的切口端面，验收缩进后有无明显缝隙。 |
| 火盆、壁炬需要动态火焰 | 先找已有 prefab，例如 `Effect_Fire_7`。需要替换模型火焰时，仅拆旧火焰与火星几何，保留火具、炭块和合适的灯光。按火具的实际锚点挂实例，并核对发射朝向。 |
| 粒子缩小后变细、变长或散开 | 读取实例最终 `lossyScale`，包含外层战斗 prefab 的缩放。对 `Effect_Fire_7` 的等比 `Hierarchy` 缩放，世界空间主火、火星和烟要用**共享原版值 × 最终比例**计算重力及速度上限；初速度已随层级缩放，局部空间 Glow 不做这项补偿。只覆盖各使用场景的实例，源建筑与战斗包装层比例不同时分别核对。用相同随机种子、相机和多个时间点对比；其它粒子先实测模块行为再确定缩放规则。 |
| 镜头到边界露出切口或模型背面 | 在 `FightScene.camera_move_limit` 按 `x:min,max&z:min,max` 配置实际相机世界范围，代码按跟随偏移换算锚点限位。开场及 WASD、鼠标拖拽共用 `ClampCameraTargetPosition`；只收窄目标场地，不写道路数特判。检查四周、切口以及近景遮挡。 |
| 光照或景深与预览不同 | 用运行时的环境光、道路材质参数、雾、镜头和 Volume 设置作对照。优先按场景配表调节；道路透明度来自 `road_alpha` 经 MaterialPropertyBlock 设置的 `_Alpha`，空值或 0 回退为 0.5。检查前后景层次以及全排单位、网格和技能提示的辨识度，退场后检查全局渲染设置是否还原。 |

## 5. 验收与交付

- **编辑模式**：回读 prefab、Addressables、Excel 和导出 JSON。核对目标模式/难度、普通及 BOSS 分支、有效与失效的旧冻结场地。检查依赖、相机锚点、边界画面和粒子表现。用战斗机位确认天空盒在开阔处和镜头边界均可见，核对预览背景模型的显隐。如改了 C#，检查 Unity 编译与 Console。编辑模式截图或模拟要注明覆盖范围。
- **实际战斗**：按项目规则由用户手动 Play。给出可操作的复测路径，覆盖实际入口、新旧传送门、可支持的最大路数、镜头移动、放置及刷怪、退场重进。没有 Play 反馈时，把实际运行效果标为待验收。
- **收尾**：在 Unity 项目目录运行 `powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".claude/scripts/check-watched.ps1" -Json`，对未跟踪的新资源补查 `watched_files`，同步受影响的项目指南。说明新增场地 ID、绑定范围、实际通过的检查与待复测项。

需要查此次实际接入的参数、诊断过程或复测方法时，读 [Embervault 接入案例](references/embervault.md)。

## 平原场地布置

- `FightScene` ID `40001–40004` 共用 `FightScene_Plain_1.prefab`。地面、远山、树、岩石、草、特效与详情节点在根下沿 X 正方向偏移 `0.85`，使它们一起贴合地形。运行时道路按自身世界坐标生成。
- `InitFightSceneCamera(fightData)` 让平原原始开场目标取 X=5、Z=3，再经 `ClampCameraTargetPosition` 应用 `camera_move_limit`；默认配置下锚点保持 `(5,0,3)`，实际相机世界 X=5、Z=-5，整体画面相对默认 X=3 向左移 2 个场景单位。
- `Tree` 的 15 株近中景树和 `Ridge Tree Line` 的 139 株远树都嵌套深青绿版 `Building_Tree_2D_7_1`，使用独立材质 `Mat_Building_Tree_7_1` 和贴图 `tree_128_7_1.png`。原有 150 株在保留树脚位置的前提下等比放大 1.5 倍，另外在开场镜头左右边缘的道路外各补两株，沿纵深错开。`West Fill` 与 `East Fill` 和原林带前后交错，`Central Hill` 的 7 株树沿中央坡面分布，远树关闭投影。白天平原可见整条林带，其余变体使用各自配置的雾距。
- `Rock` 下的 18 个无碰撞实例使用 `Building_Rock_Plain_1–3`，其中 5 块大岩石和 2 组碎石分布在远坡，沿坡面微倾斜。源 OBJ 位于 `Assets/LoadResources/Fbx/Building/Rock/`，分别为贴地主岩、楔形小岩和碎石组，裸岩和苔藓各有一个材质槽。`Wild Grass` 将 351 张草卡烘焙到 `Mesh_Plain_WildGrass_1`，共用现有 `grass_1.png` 和 `FrameWork/URP/GrassWindSway`，在路边与坡上成簇分布。
- `Ground/Ground_1` 的显示表面由 `Mat_GroundTerrain_1` 按高度图在 GPU 位移。摆放景物时，以网格顶点采样 `TerrainHeightmap_2.png` 并在相应三角面内插值，再按预制体枢轴和材质偏移对齐树根、岩底和草卡两侧根脚。岩石最靠近道路的边界局部 Z 约 `7.80`，草簇布置在最大 6 路及出入口之外。
- 地表底图为 `ground_8_256x256.png`，使用偏青绿的连续短草，保留少量低对比的小露土擦痕。`_BaseMap` 平铺 `(12,-12)`、偏移 `(0.25,0.50)`。Unity 导入为 256×256，使用镜像环绕、三线性过滤、MipMap 和 8 倍各向异性。
- 白天和白天花海（`40001/40003`）主光强度为 `0.99`，色温为 `6500 K`，阴影强度为 `0.88`，俯角为 `43°`。地表材质的 `_BaseColor` 为 `(0.75, 0.74, 0.78, 1)`。白天道路色为 `#30481C` / `#57752E`，透明度为 `0.28`。夜晚和夜晚花海（`40002/40004`）使用灰绿道路色 `#243B25` / `#4D6941`，透明度为 `0.29`。
- 四种平原变体均使用原景深 `Bokeh/180/12`。后续平原光照和地表调色沿用这组景深做对照。在 `TestScene` 的 `Launcher` 选普通战斗测试 `[40001] 平原_1`，用 6 路、长度 10 手动 Play，检查地表亮度、景深和棋盘格辨识度。
- 白天花海 `40003` 使用线性雾 `#F2E8FB`（22–100）。夜晚花海 `40004` 使用线性雾 `#101A30`（14–65），体积雾密度为 `0.035`。雾参数按 `FightScene` 行配置。
- `Details/DayFlowerSea/FlowerSea` 使用「团簇分布」布置 40,000 朵花，`scaleRange` 为 `0.35–0.35`。默认 80 个团簇，世界半径随机区间 3.5–6.5，团心聚集程度 0.45，散花比例 15%（团内约 34,000 朵、每团平均约 425 朵）；这些参数只在团簇模式显示，改动会自动重新生成。手动 Play 时从开场机位和移动机位检查大团簇、团间空隙与远景覆盖。`Details/NightFlowerSea/FlowerSea` 使用 `68×68` 随机抖动网格布置 9,248 朵花，新增团簇字段保留组件默认值但不生效。可通过 `FlowerSeaInstanceRenderer` 的「分布方式」切换随机抖动网格、均匀错列与团簇分布。
- 白天花海的 `Details/DayFlowerSea/PetalDrift` 嵌套 `Effect_Leaves_1.prefab`，覆盖实例颜色、尺寸、发射范围和漂移速度，将灰白像素图集染成浅粉到奶白的花瓣。预热后以每秒 9 片循环发射，沿战场横向慢漂并渐隐。手动 Play 进入 `40003`，检查近景花簇与角色比例、花海覆盖和飘散粒子的辨识度。

## 玫瑰宫殿决战场地

- `FightScene` ID `30002`，Addressables 地址 `Assets/LoadResources/FightScene/FightScene_Palace_CrimsonCrown_1.prefab`。内嵌 `Building_Palace_CrimsonCrown_Cutaway_1`，子层位置 `(5.5, -0.0435, 4.72)`、Y 轴旋转 `180°`、缩放 `(0.68, 0.58, 0.58)`，保留战斗所需的横向间距。两侧柱心局部 X 为 `±10.17`，柱本地缩放 `(0.75, 1, 0.85)`，柱旗与柱身留约 `0.013` 战斗场景单位。用角色站姿约 1 单位校准家具，长椅、花盆和王座等比世界缩放分别为 `0.45`、`0.43`、`0.36`，对应长椅坐面高约 `0.41`、王座高约 `1.58`，王座底面贴台顶。
- 战斗版 12 座烛台共用 `Assets/LoadResources/Fbx/Building/Palace/CrimsonCrown_BattleCandelabrum.asset`。用图集 UV2 去掉原网格的 186 个火焰三角面，补同图集金色轴杆连接中间烛座。烛台等比世界缩放 `0.30`，烛芯高约 `0.69`，第二排局部 Z `8`，第三排仍落在台阶前平地。36 个烛芯各自嵌套 `Effect_Fire_7`，只开启主火和 Glow。火焰局部缩放 `0.19`，主火世界空间重力和速度上限按最终 `0.057` 比例补偿。其中 10 座带烛光。大厅 6 盏点光强度为 `1.02`、范围 `2.65`，王座台 4 盏为 `1.4`、范围 `3.15`，在地毯两侧和王座台形成局部暖光。
- 战斗平地的地毯保留红色织纹和两侧金边，移除中央花纹。`CrimsonCrown_BattleRoyalDais.asset` 把大厅局部高度 `0.074` 的两张地毯三角面分到独立材质槽，使用 `Mat_Building_Palace_CrimsonCrown_CombatCarpet_1` 和 `palace_crimson_crown_combat_carpet_1.png`。战斗包装层整体上移后，地毯世界 Y 约为 -0.00058，贴近魔物落子平面 Y=0，并低于棋盘格平面 Y=0.0001。两侧石砖保留原有高度差。台阶、王座台及碰撞体继续使用原资源。
- 战斗预制体将暖色主光 `Warm evening key` 的世界欧拉角设为 `(48°, 28°, 0°)`。主光强度为 `0.58`，冷色补光强度为 `0.17`，让窗边、柱廊和大厅中轴保留明暗层次。参考森林白天场地，阴影朝棋盘深处投射。地毯已贴近落子平面时，用同一机位对比主光方向，区分地面落差与影子在画面中的分离。
- 世界 1 的征服难度 1–10 使用 BOSS 池 `30001&30002`。新随机时两座宫殿等概率，已冻结的场地继续沿用。`30002` 使用 `Skybox_6`、环境光 `#776468`、道路透明度 `0.1`、景深 `Bokeh/280/12`，场地雾和体积雾留空。
- 已在编辑模式按战斗根节点偏移回读 2–6 路的地面和出生区，核对 8 面柱旗、王座底面、12 座烛台的连接件与 36 个火焰实例。实机验收可在 `TestScene` 的 `Launcher` 选战斗测试场景 `[30002]`，手动 Play 查看角色与王座比例、烛火、放置、出怪和镜头边界，再用新征服传送门验证最后一关切换。

## 天空石台无尽场地

- `FightScene` ID `60001`，Addressables 地址 `Assets/LoadResources/FightScene/FightScene_SkywardTerrace_1.prefab`。世界 1 无尽难度 2–10 的 `fight_scene_id` 均为 `60001`，可从战斗模式编辑器的无尽页签调整。
- 用 `TestScene` 的 `Launcher` 选择无尽模式测试难度，手动 Play 检查场地加载、道路覆盖、放置和刷怪。正式入口可用已有无尽传送门验证进场场地，并用征服传送门确认普通关和决战仍从各自场地池选取。

## 荒野峡谷普通关场地

- `FightScene` ID `70001`（白天）和 `70002`（夜晚）共用 Addressables 地址 `Assets/LoadResources/FightScene/FightScene_WildernessCanyon_1.prefab`。内嵌 `Scene_WildernessCanyon_1`，子层平移 `(5.5, -0.005, 3.5)`，Y 轴旋转 `+5°`，使远处峡口对准道路中线。`Ground_Placement` 填补 `Ground_Arena` 的中央开口，渲染器和碰撞体都保留。
- 战斗包装层移除了嵌套模型的 `Sky_Dome`。白天配置 `details=Day`，由 `Details/Day/Lights` 提供暖主光和冷补光，使用 `Skybox_2`、环境光 `#8CA1AB`、线性雾 `#9EB5B3`（42–108），道路色为 `#7D7253` / `#B4A57B`。
- 夜晚配置 `details=Night`，由 `Details/Night/Moonlights` 提供冷色月光与补光。`Moonlight` 强度为 `0.4`，`Night Canyon Bounce` 为 `0.14`，沿用白天灯组的朝向和阴影设置。夜晚使用 `Skybox_6`、环境光 `#879AC0`、线性雾 `#101A30`（30–95）及虫鸣 `2000001`。道路色为 `#5F5960` / `#938879`，体积雾和景深字段留空。两种变体的天空盒旋转均为 `-15,0,0`，道路透明度均为 `0.28`。
- `Ground_Arena` 和 `Ground_Placement` 共用战斗专用材质 `Mat_FightScene_WildernessCanyon_Sand_1`。`Shader_WildernessCanyonSand_1` 在 URP Lit 受光前用较粗的底色 mip 识别暗沟，提浅其低频底色，保留原砂粒、裂纹、平铺接缝和法线。崖壁投影仍由 Lit 处理。
- 白天与夜晚共用根节点 `Effect` 下的 `Effect_SandDust_1` 和 `Effect_Weather_Windlines_2`。沙粒位于 `(5.5, 1.5, 3.5)`，风线从 `(20, 3, 3.5)` 发射，沿用沙漠实例的旋转、半径和砂色覆盖，从右向左扫过战场。
- 草丛和碎石的位置修正在战斗预制体的嵌套实例上。用 `Ground_Arena` / `Ground_Placement` 三角面测草根和石底的地表高度，草根稍入地，碎石保留自然埋底。用道具网格顶点和面心检查崖壁及草与石的穿插，露出崖壁的道具就近挪到空砂地，复核 2–6 路的道路及出生区净空。
- 世界 1 征服难度 5（`1000005`）的普通关场地池包含夜晚 `70002`。新生成该难度场地时抽取并冻结，已有冻结场地继续使用。原场地已在编辑模式按 2–6 路及战斗根节点偏移采样道路和出生区。实机可在 `TestScene` 的 `Launcher` 中选择普通战斗测试 `[70002] 荒野峡谷_1_夜晚`，用 6 路、长度 10 检查月光、峡口轮廓、棋盘格、角色辨识度、镜头边界、放置和出怪。白天对照场景为 `[70001] 荒野峡谷_1`。
