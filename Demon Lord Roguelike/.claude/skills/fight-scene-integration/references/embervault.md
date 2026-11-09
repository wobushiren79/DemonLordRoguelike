# Embervault 竞技场接入案例

以下为当时的配置和验收证据。再次修改该场地前，先回读当前资产和配置；接入别的场地时按实测确定数值。

| 项目 | 当时的处理 |
|---|---|
| 资源入口 | `Building_Arena_Embervault_1.prefab` 嵌套在 `FightScene_Arena_Embervault_1.prefab`。战斗包装层使用子级位移 `(5.5, -0.05, 3.5)` 和等比缩放 `0.5`，隐藏挡住镜头的近侧围墙、基础、看台与拱廊。Addressables 组是 `FightScene`，地址是 `Assets/LoadResources/FightScene/FightScene_Arena_Embervault_1.prefab`。 |
| 模式绑定 | 场地 ID `50001`。挑战100勇士的普通及真勇士共 28 行都指向该场地，道路保持 5×10。旧档场地不再符合配置池时，只迁移场地 ID。 |
| 灯光与景深 | 对照关投影的画面，定位为 `06 Bedrock Vault` 下洞顶与四组岩肋的阴影缝。关闭这五个渲染器的投影，重配暖主光与补光。`FightScene` 行环境光为 `#C5AD92`，景深为 `mode:Bokeh&length:185&aperture:5.6`；用战斗镜头验证中央战斗带与远近景的清晰度。 |
| 屋檐 | 双面图集材质下，16 段 `Cornice` 底带与 `Masonry` 同面，相邻屋檐端面也互叠。仅收短底带，在墙顶上方留 `0.004` 源模型单位，去掉重复端面，保留近侧切口可见的端面。用三角面共面及投影面积检查复核。 |
| 镜头 | `FightScene` 行 `50001` 配置 `camera_move_limit=z:-4,-1`，将实际相机世界 Z 限为 -4~-1，X 沿用默认 3~7。`CMFollow` 的 X/Z 偏移为 0/-8，对应锚点 X=3~7、Z=4~7；开场、WASD 和右键拖拽统一走 `ControlForGameFight.ClampCameraTargetPosition`，所有路数一致。 |

## 火焰实例如何按比例保持原状

1. 从火具网格中移除烘进模型的火焰、火星三角面，保留火盆、托盘和炭块，接入已有 `Assets/FrameWork/Prefabs/Effect/ParticleSystem/Fire/Effect_Fire_7.prefab`。保留共享资产；用嵌套实例覆盖摆放与必要参数。清理替换下来的独立资源前先查依赖。
2. 当时源建筑内壁炬和火盆实例分别缩放到 `0.37`、`0.50`。战斗包装再缩 `0.5` 后，实际比例是 `0.185`、`0.25`。按各上下文的最终比例分别覆盖 `MainModule.gravityModifierMultiplier` 和 `LimitVelocityOverLifetimeModule.limitMultiplier`，每次从共享源值计算，避免在已有覆盖值上再次相乘。只处理 World 模拟的主火、Sparkles、Smoke；Local 模拟的 Glow 保持其原行为。
3. 在编辑模式中用相同随机种子和相机模拟原版与缩放实例。可用 `ParticleSystemRenderer.BakeMesh` 比较多个时间点的粒子顶点，将缩放版顶点转换到同一原点并除以比例后对照。此案例中，单改 Transform 会把火焰和烟拖长，连初速度一起缩则过短；只补偿重力与限速后，两个上下文的四个比例均通过三帧对照。进入实际战斗后还要观察火焰是否贴合器皿、有没有被场景深度淡出。
