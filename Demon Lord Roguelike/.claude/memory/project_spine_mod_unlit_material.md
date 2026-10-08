---
name: spine-mod-unlit-material
description: Spine Mod 材质曾用内置管线无光照 Spine/Skeleton 致战斗场景偏暗——2026-10-04 已修复：Other/AeonsEcho 全部普通页材质换成 URP 受光 sprite shader 并重建部署；新增资源入库必须执行同样替换
metadata:
  type: project
---

# Spine Mod 材质无光照 → 战斗场景偏暗（已修复，约定长期有效）

**现象**：OtherSpine 幻化生物在战斗场景（森林）里明显比主项目生物暗。

**根因**：两类生物材质 shader 光照模型不同——

- **主项目生物**（Goblin/Human 等全部 7 个 `Assets/LoadResources/Spine/Creature/*/[X]_Material.mat`）：自拷贝的 URP **受光** shader [Spine-Sprite-URP.shader](Assets/Shaders/Spine-Sprite-URP.shader)（内部名 `Universal Render Pipeline/Spine/Sprite`，GUID `9f253724b2d29a3438eeea48277c25cb`，与 urp-shaders 包内原版同 GUID），关键字 `_ALPHAPREMULTIPLY_ON + _FIXED_NORMALS_VIEWSPACE`、`_FixedNormal=(0,0,1,1)`、`_SrcBlend=1/_DstBlend=10`（PMA）——ForwardLit pass 吃场景平行光+环境光。
- **全部 Spine Mod 图集材质**（spine-unity 导入自动生成的默认值）：内置管线**无光照** `Spine/Skeleton`（GUID `1e8a610c9e01c3648bac42585e5fc676`）——完全不吃场景灯光，按贴图原色渲染。
- **场景侧**：森林 Day 细节件白色平行光**强度 1.5** + GameScene 默认 Flat 白环境光 → 受光生物 ≈ 贴图 ×1.4~1.9，Mod 生物恒定 ×1 → **暗 30%~50%**。

**修复（2026-10-04 已完成）**：

- Other（138 个）+ AeonsEcho（744 个）共 **882 个普通页材质**换成受光 shader：以主项目 `Goblin_Material.mat` 为模板整文件重写（仅替换 m_Name 与 _MainTex guid，保留 .meta）；GUID `9f253724...` 在 MOD 项目解析到包内 shader、在主项目解析到本地拷贝（同 GUID 双解析链均通）。
- **67 个混合页**（`-Multiply`/`-Screen` 后缀）**保持内置 `Spine/Skeleton-PMA-*` 不换**：sprite shader 无 Screen 混合关键字、Multiply 公式（`Blend Zero SrcColor`）与内置 PMA-Multiply 不等价。
- 两 Mod 已命令行批处理重建（`OtherSpineModBuilder.BuildMod` / `AeonsEchoSpineModBuilder.BuildMod`）并经各自 `gen_*_spine_mod.py export --deploy-main` 部署主项目（构建器的 EditorPrefs 主项目根未配置，自动部署跳过，需手工补部署）。

**长期约定**（已写入 other-spine-mod / aeonsecho-spine-mod 两个 SKILL 的资源约定节）：

- **新增资源/套装入库时，spine-unity 自动生成的材质必须批量换成受光 shader 才能构建入包**（否则偏暗复发）；模板化重写方法同上。
- 其余 10 个仅 ui_show 的 Mod 未换（UI 无场景光照、无收益；若未来改 show 段须同样处理）。

**构建排障经验**：Unity batchmode 启动后 CPU≈0 不一定是卡死——许可证 token 刷新会阻塞主线程数分钟（本机有 ULF 兜底，等会自愈，Other 首次误杀教训）；`-noUpm` 不可用于该项目构建（spine/Addressables 包不解析，编译全灭）。

**修复后新问题（2026-10-04 用户 Play 反馈，2026-10-05 已解决）**：换受光 shader 后基地/森林场景 Mod spine **过亮**。逐项排除配置差异后确认根因 = **贴图基准亮度差 × 场景光照增益**：

- 主项目生物贴图平均亮度 **58~70**（暗色系，原生适配受光环境）；Mod 二次元立绘贴图（Amelia 抽样）**112**（约 1.7~1.9×）。
- 基地（BaseScene.prefab）与森林白天（FightScene_Forest_1 Day）恰好都是**白色平行光 ×1.5** + Flat 白环境光 → 受光总增益 ≈1.9~2.3×；主项目显示 ≈110~160 正常，Mod 显示 ≈210+ 过曝。**只有 1.5 白平行光的场景明显**（沙漠/夜晚等低增益场景不突兀）。
- 数值自洽：修复前 Mod 无光照=112 vs 主项目受光≈120（用户报"暗"）；修复后 Mod=112×1.9≈210 vs 主项目≈120（用户报"太亮"）。

**解决方案（用户拍板：配置驱动 + 只 OtherSpine，2026-10-05 已实施）**：幻化药 other_data 新增 **`show_brightness` 键**（场景亮度系数，带键才生效；**弃用 hasTransform 一刀切**）——

- **配置侧**：gen_other_spine_mod.py 的 scan 自动生成（show 骨架图集全页 PNG alpha>25 有效像素 Rec.601 均值 avg>65 才写 `k=65/avg` 只压不提；基准 65=主项目三生物口径）；`PRESERVED_KEYS`（原 LAYOUT_KEYS）挂键后手调值按资源身份保留、**删键=强制重算**；4 个仅 Avator 药不写键（无 show 段）。100/104 药已写键（k 0.38~0.63）。
- **运行时**：`CreatureBeanPartial.GetTransformShowBrightness`（≈1/≤0/>2/解析失败=无键——域 (0,2]，1=原亮度，<1调暗/>1调亮，2026-10-08 起上限 1→2 放开调亮，shader `_Brightness` 属性域 Range(0,2) 原生支持，生成端仍只压不提、>1 只能测试面板手调）→ `CreatureHandler.SetCreatureData` SkeletonAnimation 分支 → 游戏层 `SpineHandler.ApplySceneDimOverride`/`ClearSceneDimOverride`：`CustomMaterialOverride` 把场景实例普通页材质换成克隆亮暗材质（`_COLOR_ADJUST`+`_Brightness`，HSV 只缩 V 不碰 alpha，PMA 安全；缓存 key=(原材质,系数千分位)；混合页跳过；清除按值识别 Remove 不 Clear——防误删描边键；**UI SkeletonGraphic 零影响**）。
- **TestTransformPotionGUI.BuildOtherData 必加拼接行**——面板「保存全部修改」是结构体往返重组，不加会把键从 Excel/JsonText×2/内存四处吃掉（新键四处同步铁律：TransformOtherData 字段+Parse case+BuildOtherData 拼接+gen 脚本 PRESERVED_KEYS）。
- **⚠️ bundle 内嵌 shader 变体裁剪坑（2026-10-05 实战：调暗"完全不生效"的根因）**：mod bundle 的材质引用 **bundle 内嵌 shader 副本**（Addressables 隐式打包依赖），构建时按包内材质实际引用裁剪 shader_feature 变体——运行时 `EnableKeyword("_COLOR_ADJUST")` 静默失效（关键字在材质上显示已启用、但变体不存在，渲染回退无该分支）。**修复=克隆材质时把 shader 换成主项目资产**（`GetSceneDimShader()` 经 `Resources/Materials/SpineSpriteURP_DimDummy.mat` 取——dummy 保变体），编辑器和 PC 包统一。**教训：凡"运行时给 bundle 材质 EnableKeyword 开新关键字"都踩此坑；诊断法=插桩打印渲染器 sharedMaterials 落地状态 + MCP 运行时查材质 shader/keyword/属性三连**。另：spine-unity 4.3 `SkeletonAnimation` 与 `SkeletonRenderer` 已分离，`CustomMaterialOverride` 须经 `((SkeletonRenderer)sa.Renderer)` 访问（直接 `.CustomMaterialOverride` 编译不过）。
- **键取图集资产权威清单**：覆盖键取 `SkeletonDataAsset.atlasAssets[*].Materials`（普通页），**不读渲染器当前 sharedMaterials**——SetSkeletonDataAsset 后渲染器材质可能未刷新，键错配则覆盖静默不生效。
- **变体剥离保险**：`_COLOR_ADJUST` 全项目无引用、`m_PreloadedShaders` 空 → `Assets/Resources/Materials/SpineSpriteURP_DimDummy.mat`（三关键字 dummy）保主项目 shader 变体。**已实锤一次**（2026-10-05）：用户 14:23 打的包早于 dummy 创建（15:04）→ 包里无变体 → 调暗静默失效偏亮；**凡涉及运行时新关键字的改动，必须重打包验证，且打包时间要晚于全部保险资产落地**。
- **bundle 无需重建**：亮度键只走 Excel→JsonText，export --deploy-main 即生效（调系数只需改 Excel+export，秒级）。
- 已知取舍（R7）：k 按 1.9 增益上限归一，低增益场景（沙漠/夜晚）调暗后略偏暗，Play 确认。

相关：[[project_spine_mod_pma_requirement]]（同批材质的另一项约定：PMA）。
