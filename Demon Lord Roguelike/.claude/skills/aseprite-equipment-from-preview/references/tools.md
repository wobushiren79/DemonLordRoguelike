# CLI、PNG 部件与 manifest

从 Unity 项目目录运行，所有路径由输入参数、`$env:TEMP` 和脚本位置推导，不写机器绝对路径。Python 统一经过 `.claude/scripts/run-python.ps1`。`run-aseprite.ps1 -WhichOnly` 动态返回 Aseprite 路径。

## 导出母版素模供生图参考

按 [Aseprite 基础检查](../../aseprite-pixel-editor/SKILL.md) 找到素模帧并核对全身可见。使用下文「最终导出」的单帧命令，将输入换成原母版，帧号换成已核对的素模帧，输出到临时目录。直接用合成 PNG 作为 ChatGPT 生图的参考。需要放大时，用同节的 `preview` 命令做最近邻整数放大。若母版没有完整的单帧素模，导出实际身体 Cel，并按原画布坐标与遮挡顺序合成参考 PNG，保持母版只读。

## 导出某帧的独立 Cel

```powershell
$exe = powershell.exe -NoProfile -ExecutionPolicy Bypass -File '.claude/scripts/run-aseprite.ps1' -WhichOnly
$tempRoot = Join-Path $env:TEMP ('aseprite-equipment-' + [guid]::NewGuid().ToString('N'))
$exportDir = Join-Path $tempRoot 'base'
New-Item -ItemType Directory -Path $exportDir | Out-Null
& $exe -b --script-param op=export --script-param "src=$source" --script-param "frame=$frame" --script-param "dir=$exportDir" --script '.claude/scripts/aseprite_equipment.lua' | Out-Host
```

`source` 为只读输入，`frame` 为检查确认的实际帧号，从 1 开始。输出 `manifest.json` 和独立 RGBA PNG，记录画布、图层路径、Cel 偏移、尺寸、透明度及 Z 顺序。图层组用 `组名/层名` 表达。同一完整路径对应多层时，导出的 Cel 还包含 `layer_indices`。

## 新增一帧或局部替换

准备 JSON manifest，PNG 路径相对于 manifest 所在目录，不能包含父目录跳转。以下以哥布林的帽子和上衣举例，画布、帧号、坐标、身体及装备层均以实际检查结果为准。

```json
{
  "width": 128,
  "height": 128,
  "mode": "append",
  "frame": 43,
  "duration": 0.8,
  "copy_cels": [
    { "layer": "Body", "from_frame": 7 },
    { "layer": "Head", "from_frame": 7 },
    { "layer": "Eye_1", "from_frame": 2 }
  ],
  "cels": [
    { "layer": "Clothes_1", "png": "shirt.png", "x": 57, "y": 97 },
    { "layer": "Hat_1", "png": "hat.png", "x": 62, "y": 84 }
  ]
}
```

`append` 每次在当前输入总帧数加一的位置插入空白帧。`copy_cels` 填写该姿势需要的所有身体和脸部 Cel，排除旧装备和其他参考内容。`cels` 在同一帧写入一整套装备，各部位使用已核实的原装备层，省略 `create` 和 `tag`。多套依次以上一轮输出为输入，始终写到独立的候选路径。记录已有标签的起止帧，若追加后原末帧的标签被延长，用官方 Lua 恢复其原有范围。

`replace` 只更新该套原帧的目标 Cel，未指定的透明度与 Z 顺序沿用原值。替换使用独立图像，避免修改与该 Cel 链接的其他帧。用户要求移除某件装备时，用官方 Lua 删除目标帧明确对应的 Cel，再回读其余部件。通用脚本其他用途仍可用 `create:true` 新建顶层图层。

`layer` 填完整路径。路径歧义时，`cels` 和 `copy_cels` 的对应条目附上 `layer_indices`。数组从根层开始，按 Aseprite Lua `sprite.layers` 和后续各级 `layer.layers` 的组内顺序，从 1 开始记每一级索引。例如 `组/子层` 的 `[5, 2]` 表示根列表第 5 层中的第 2 层。工具会校验索引与图层路径，同名层的索引须按当前输入的实际 Cel 和层序重新核对。

```powershell
& $exe -b --script-param op=apply --script-param "src=$workingCopy" --script-param "out=$newOutput" --script-param "manifest=$manifest" --script '.claude/scripts/aseprite_equipment.lua' | Out-Host
& $exe -b --script-param op=verify --script-param "src=$originalInput" --script-param "out=$newOutput" --script-param "through=$originalFrameCount" --script '.claude/scripts/aseprite_equipment.lua' | Out-Host
```

输出不能等于源路径，已存在输出不会覆盖。追加后用 `verify` 核对本批最初输入的全部原有帧。局部修整用 `verify` 核对目标帧前的原有帧，并另核对目标帧未修改的 Cel 和之后的全部原有帧。输入 PNG 用 `Image{fromFile=...}` 读取单图，避免打开连续编号 PNG 时被自动识别为序列。通用 JSON 需要 Aseprite 自带 `json.encode/decode`，其他版本按其官方 Lua API 处理。

## 只加强描边

先导出认可版本的套装帧，再用 PNG 工具生成只含目标层的 `replace` manifest。

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File '.claude/scripts/run-python.ps1' '.claude/scripts/aseprite_equipment_png.py' contour --manifest "$exportDir/manifest.json" --output-dir "$tempRoot/contours" --layer 'Hat_1' --layer 'Clothes_1' --color '#111219' --min-luma 24 --keep-highlight 155
```

每个 `--layer` 填目标 Cel 的完整路径。若帧内的导出 manifest 有多个同名路径，先复制 manifest，按 `layer_indices` 为该路径只保留本次选中的一个条目，保持图片相对路径一致。多个同名层都要修整时，分别生成描边 PNG，再合并带 `layer_indices` 的条目。旧源文件仍可用 `--prefix` 按末级图层名的前缀匹配，也可重复 `--exclude 层路径` 排除指定层。工具只改原 alpha 内四邻域暴露边缘，保留形状、内部像素及高于 `keep-highlight` 的亮点。内部接缝按部件做局部像素修整，再逐件检查。

## 装备帧身体核对

`aseprite_equipment.lua op=verify through=N` 只检查前 N 帧，不覆盖 N+1 的新增装备帧。要求身体不变时，另做以下只读验证；现有工具没有单独的 `verify_body` 操作，不要假设传入额外参数就已检查。

1. 制作前保存身体基准：用 `op=export` 导出已核对站姿的身体 Cel 和 manifest；若从不同源帧组合身体，记录每层实际来源帧。参考图中的身体不能作为此基准。
2. 保存后再次用 `op=export` 导出装备帧，到另一个空临时目录。按预先记录的图层路径和同名层索引匹配各身体 Cel，独立核对装备 Cel 的归属和偏移。
3. 在本次验证脚本中逐项断言：身体 Cel 的存在性、原始宽高、画布 x/y、每个 RGBA 像素、opacity、zIndex 均与基准一致；核对图层可见性、opacity、blendMode 和相对顺序。导出 manifest 未提供的层属性/顺序需经官方 Lua 读取，不能漏项后声称全通过。
4. 另行执行旧帧 `verify`，复核母版 SHA-256，确认输出帧数符合 append/replace 模式。身体不变与衣物遮挡身体可以同时成立；不通过移动身体或替换脸部来补救帽口。
5. 若用 Python 合成图辅助适配，将其与正式原生导出核对：alpha 必须一致，alpha 非零位置的 RGBA 必须一致；全透明像素的隐藏 RGB 可忽略。该核对不能替代身体 Cel 比对或视觉验收。

## 最终导出

`--frame-range` 用从零开始的下标，放在输入文件前。将实际要显示的身体、面部和本套装备图层的唯一 CLI 名称列入 `$renderLayers`。重复传入 `--layer` 可在本次渲染显示默认隐藏的 `Hat_1`、`Clothes_1` 等层，并排除 `Show`、其他套装及辅助层。若 CLI 层名有歧义，在专用渲染副本中用官方 Lua 按 `layer_indices` 只开启选中层与必要父组，关闭其余层。用此副本作为 `$verifiedSource`，把 `$renderLayers` 设为空列表，以副本可见性和相同帧范围导出。

```powershell
$index = $equipmentFrame - 1
$layerOptions = @(foreach ($layer in $renderLayers) { '--layer'; $layer })
$native = Join-Path $tempRoot "native-$equipmentFrame.png"
$renderData = Join-Path $tempRoot "render-$equipmentFrame.json"
$finalPreview = Join-Path $deliveryDir "${masterName}_帧${equipmentFrame}_预览.png"
& $exe -b @layerOptions --frame-range "$index,$index" $verifiedSource --data $renderData --sheet $native | Out-Host
powershell.exe -NoProfile -ExecutionPolicy Bypass -File '.claude/scripts/run-python.ps1' '.claude/scripts/aseprite_equipment_png.py' preview --input $native --output $finalPreview --scale 6 --background '#4f5056'
```

`$verifiedSource` 是保存并回读的装备文件，`$masterName` 是母版的文件主名。新预览和替换预览都可先将 `$deliveryDir` 指向批次临时目录，核对后再与同帧参考图写入正式目录。省略 `--background` 保持透明。核对元数据只有一帧、原生 PNG 尺寸等于原画布、放大 PNG 尺寸等于原画布乘倍率，并确认应显示的装备均可见。PNG 工具依赖 Pillow，Lua 工具不依赖外部库。

## 保存原图参考截图

从用户提供或确认版本的装备预览原图读取像素。拼图先核对套装编号与所在格，按原图坐标裁出该套完整区域，检查帽饰、翅膀、披风或武器等边缘。单套参考可原样另存为 PNG。保留原始像素比例、颜色和背景，保存为 `<母版名>_帧<实际帧号>_参考.png`，与同帧效果预览和共用装备源文件一起放入[正式装备目录](../SKILL.md#正式交付路径)。
