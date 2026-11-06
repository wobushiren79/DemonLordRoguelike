---
name: blender-scene-from-reference
description: 从参考图或场景预览图重建可编辑的 Blender 三维建筑、室内或游戏场景，渲染像素风或 HD2D 预览，并按需制作 Unity FBX、共享图集、URP 材质和预制体。先在 Assets/Test 交付验收，用户确认后按项目规范入库。
watched_files:
  - Assets/Shaders/Shader_Building_Atlas.shader
  - .claude/skills/blender-scene-from-reference/scripts/
---

# 从参考图制作 Blender 场景

交付真实三维几何和可编辑源工程，内容只保留场景。剔除参考图中的人物和 UI，包括血条、按钮、菜单和界面浮字。根据用户要求选择仅制作 Blender 场景、附带模型和预览，或继续接入 Unity。

## 参考图与白模

1. 查看参考图的原始分辨率。记录取景、透视或正交、遮挡关系、结构层级、主材质和色调。存在多个视角时先找共同的比例基准。单图未展示的区域按同一建筑语言补全，在交付中指出关键推断。
2. 先用地面、墙体、柱、楼梯、家具占位建立白模，设置单位和主相机，渲染小图核对轮廓、负空间和构图，再制作装饰。按功能建集合，保持屋顶、前墙等遮挡结构可单独显隐。剖视通过切换集合实现，完整导出应恢复这些集合。
3. 在 Blender 中构建实体，给侧面、背面和遮挡结构补几何。参考图中的人物或 UI 遮挡了场景时，根据周围结构补齐被遮挡的部分。为建筑结构与装饰明确坐标基准和 UV。参考图用于对照构图与设计，交付图从实际模型渲染。

## 贴图、预览与导出

- 像素风贴图先在目标像素网格绘制，Blender 贴图节点使用 Closest。保留原贴图密度和重复比例。可在低分辨率渲染后用最近邻放大，预览可交付参考机位、剖视、细节和完整外观。
- 在源工程中保存可编辑部件、命名相机和必要灯光，打包贴图。用 [render_previews.py](scripts/render_previews.py) 批量渲染已配置的相机。不同镜头需要不同遮挡或布光时，在 Blender 中先配置各自场景状态。
- Unity FBX 导出只选网格，默认按项目尺度 1 m / 单位，确认 Y 向上、正向、枢轴及模型尺寸。大型静态场景按可见性和碰撞用途合网格，保留需要独立切换的分组。
- 单图集先收集纹理、纯色和不同的表面参数，再决定 UV 方案。可直接烘 UV 时优先复用项目现有的 URP 着色器。重复贴图会显著增加切缝三角面时，可用项目的 `Game/Building/Atlas` 在图集区域内重复采样。使用前阅读 [赤冠宫殿案例](references/crimson-crown.md) 的 UV 和通道约定。

## 验收版交付

1. 模型制作完成后，为本次交付创建独立的 `Assets/Test/<模型名>/`。把 FBX、贴图、材质、prefab、验收场景以及本次新建的着色器和其他依赖集中在这个目录。将 Blender 工程放在 `Source~/`，将渲染预览放在 `Previews~/`，通过文件链接提供给用户，避免 Unity 重复导入。
2. 涉及 Unity 编辑器时按 [unity-mcp-connection](../unity-mcp-connection/SKILL.md) 处理。在验收目录内导入模型和贴图，按用途配置材质、FBX 材质重映射、碰撞体和 prefab。单图集需核对 Point、Mip Maps、压缩、Alpha 用途和 UV 通道。将建筑预制体与拍摄用相机、全局灯光和后处理分开。
3. 完成验收检查后，给出验收目录及预览或 prefab 的位置，停在此步等待用户明确确认当前版本可以入库。若收到修改意见，在同一验收目录内迭代。目录应能整套删除，正式资源及其他验收项目应独立于它。

## 用户确认后入库

只有用户明确确认当前版本可以入库后，才迁移资源、归档 Blender 工程和预览，或修改正式场景、配置与 Addressables。用户对当前验收版回复「没问题」或「可以入库」即可作为确认。先检查同类项目资源的名称和层级，再按以下目录归类。

| 类型 | 目录与命名 |
| --- | --- |
| FBX | `Assets/LoadResources/Fbx/Building/<类型>/`，沿用同类模型的名称 |
| 贴图 | `Assets/LoadResources/Textures/Building/<类型>/`，使用小写下划线分隔 |
| 材质 | `Assets/LoadResources/Materials/Building/`，例如 `Mat_Building_<类型>_<名称>_1.mat` |
| Prefab | `Assets/LoadResources/Prefabs/<类型>/`，例如 `Building_<类型>_<名称>_1.prefab` |
| Blender 和预览 | 仓库根的 `资源/建筑/<项目名>/`，源工程和预览按名称、机位区分 |

用 Unity 资源移动 API 保留 GUID。用户指定游戏放置目标或加载入口时，按已有系统接入场景或 Addressables。

## 验收与清理

- 检查 Blender 源工程、渲染预览、导出的模型和 Unity 验收资产，确认每项交付都只包含场景，人物和 UI 均已移除。
- 在 Blender 重新打开源工程，检查已打包贴图、参考和补全区域、网格和材质数量。回导 FBX 后核对尺度、分组、UV 和三角面；对照实际渲染检查贴图错位、烘焙接缝、透明和发光。
- 在 Unity 编辑器核对 FBX 和 prefab 的模型、材质和图集依赖，以及完整/剖视的分组状态。检查着色器和 Console。区分导入验证、编辑器画面和 Play 验证，后者由用户手动完成。
- 入库后检查归档产物可打开、正式资源无验收目录依赖，再清理本次验收目录。若用户放弃当前模型，可直接删除该模型的独立验收目录。代用户清理时，范围限定为用户指定的模型目录。

通过项目的 `.claude/scripts/run-python.ps1` 调用 Python，包括驱动 Blender 的 Python 脚本。启动范例及宫殿的可重建脚本见 [案例](references/crimson-crown.md)。
