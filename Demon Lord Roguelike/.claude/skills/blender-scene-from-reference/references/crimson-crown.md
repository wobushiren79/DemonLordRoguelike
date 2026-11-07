# 赤冠宫殿案例

## 构图与三维结构

参考机位是朝王座的正交俯视。以中轴地毯和逐层台阶确定大厅纵深，再对齐两排石柱、旗帜和彩窗。背墙、侧廊、入口门和屋顶分别成组。默认隐藏入口墙、屋顶和吊灯形成参考构图，外观镜头恢复这些分组，剖视机位可另隐藏近侧墙。原始源工程保留五台相机、23 种表面和 12 张打包纹理。

## 单图集的导出约定

赤冠宫殿整合为 512 × 1024 RGBA 图集。原贴图维持像素密度，边缘用循环像素留边。Unity FBX 在同一可见性分组中合网格并保留原始重复 UV，完整模型有 15 个网格、54,218 个非退化三角面和一个材质槽。

- `UV0` 是原始局部 UV。着色器使用 `frac(uv)` 在当前图集矩形内重复采样。
- `UV1` 留给 Unity 的光照贴图。`UV2` 是图集矩形左下坐标，`UV3` 是宽高，均归一化，同一三角面内恒定。
- PNG RGB 保存底色，Alpha 存 `255 - flags`。第 0–1 位表示自发光等级（0 无、1 彩窗、2 外焰、3 焰芯），第 2 位表示金属，第 3–4 位表示光滑度类别（0 通用贴图、1 地面、2 纯色）。数据由 `Assets/Shaders/Shader_Building_Atlas.shader` 解码。
- Unity 使用 Point、Clamp、无 Mip Maps、无压缩。Alpha Source 取输入图，关闭 Alpha Is Transparency，FBX 关闭网格压缩并保留后两套 UV。给 FBX 材质槽和两个 prefab 赋同一个 `Mat_Building_Palace_CrimsonCrown_1.mat`。
- Blender 图集工程用标准 UV 对重复边界切分，因此网格三角面更多。以 FBX 的几何统计评估 Unity 版本。

## 归档与重建

从 Unity 项目目录访问 `../资源/建筑/Building_Palace_CrimsonCrown_1/`。其中 `Building_Palace_CrimsonCrown_Source_1.blend` 用于编辑部件，`Building_Palace_CrimsonCrown_Atlas_1.blend` 用于查看单图集，`Previews/` 包含各机位原图和最近邻放大图。

[rebuild_crimson_crown_atlas.py](../scripts/rebuild_crimson_crown_atlas.py) 针对赤冠宫殿的材料和集合结构。将重建结果写到独立的 `Assets/Test` 验收目录。等用户明确确认这版可以替换后，再通过 Unity 重新导入到原 FBX 和贴图路径，以保留 `.meta` 与引用。Blender 4.5 LTS 的路径通过 PATH 或本机安装位置动态取得。

```powershell
$blender = (Get-Command blender -ErrorAction Stop).Source
$recipe = (Resolve-Path '.claude/skills/blender-scene-from-reference/scripts/rebuild_crimson_crown_atlas.py').Path
$archive = (Resolve-Path '../资源/建筑/Building_Palace_CrimsonCrown_1').Path
$source = Join-Path $archive 'Building_Palace_CrimsonCrown_Source_1.blend'
$review = 'Assets/Test/Building_Palace_CrimsonCrown_Review'
if (Test-Path -LiteralPath $review) { throw '验收目录已存在，请先确认该目录的版本或另取一个名称。' }
$output = Join-Path $review 'Source~'
$launch = 'import subprocess, sys; app, script, *args = sys.argv[1:]; sys.exit(subprocess.call([app, "--background", "--python-exit-code", "1", "--python", script, "--", *args]))'
& '.claude/scripts/run-python.ps1' -c $launch $blender $recipe --source $source --output $output
if ($LASTEXITCODE -ne 0) { throw '图集重建失败。' }
Move-Item -LiteralPath (Join-Path $output 'Building_Palace_CrimsonCrown_1.fbx'), (Join-Path $output 'palace_crimson_crown_atlas_1.png') -Destination $review
```

使用同一个 `$launch` 调用 [render_previews.py](../scripts/render_previews.py)。通过 `--blend` 指定 `Source~/` 下的图集版工程，通过 `--output` 指定验收目录下的 `Previews~/`，可重复传入 `--camera`。默认使用工程当前相机，必要时用 `--scale 50` 先出检查图。脚本沿用保存的场景布光和显隐，完整外观和剖视需要先在相应场景配置这些状态。
