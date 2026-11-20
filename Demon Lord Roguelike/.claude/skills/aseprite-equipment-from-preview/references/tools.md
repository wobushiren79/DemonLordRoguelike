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

按[选帧规则](../SKILL.md#选帧和装备图层)从第 1 帧开始检查所有穿戴装备层的实际 Cel，找到最早可用帧后用 `replace` 补位。一批多套依次以上一轮的输出为输入，每次重新核对占位。局部修整同样用 `replace`，只指定已有套装帧的目标 Cel。

补位时，`frame` 填选中的已有帧，`cels` 只列该套装备及确实缺失的身体或脸部 Cel。需要补齐的身体部件从核对过的源帧导出 PNG，按原 `x`、`y`、`opacity` 和 `z_index` 填入 `cels`。保留目标帧已有的身体和其他 Cel。`replace` 模式不处理 `copy_cels`，会写入列出的 Cel，生成 manifest 前须确认所有待填项在目标帧为空。

没有可用帧时，用 `append` 在末尾新增一帧。以下以哥布林的帽子和上衣举例，`frame` 取当前帧数加一，画布、坐标和图层以实际检查结果为准。PNG 路径相对于 manifest 所在目录，不能包含父目录跳转。

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

`append` 的 `copy_cels` 填写该姿势所需的身体和脸部 Cel，排除旧装备和其他参考内容。补位和追加都用 `cels` 在同一帧写入整套装备，各部位使用已核实的原装备层，省略 `create` 和 `tag`。记录已有标签的起止帧，若追加后原末帧的标签被延长，用官方 Lua 恢复其原有范围。

修整已有套装时，`replace` 使用独立图像替换指定的 Cel，避免修改与其链接的其他帧。未指定的透明度与 Z 顺序沿用原值。用户要求移除某件装备时，用官方 Lua 删除目标帧明确对应的 Cel，再回读其余部件。通用脚本其他用途仍可用 `create:true` 新建顶层图层。

`layer` 填完整路径。路径歧义时，`cels` 和 `copy_cels` 的对应条目附上 `layer_indices`。数组从根层开始，按 Aseprite Lua `sprite.layers` 和后续各级 `layer.layers` 的组内顺序，从 1 开始记每一级索引。例如 `组/子层` 的 `[5, 2]` 表示根列表第 5 层中的第 2 层。工具会校验索引与图层路径，同名层的索引须按当前输入的实际 Cel 和层序重新核对。

```powershell
& $exe -b --script-param op=apply --script-param "src=$workingCopy" --script-param "out=$newOutput" --script-param "manifest=$manifest" --script '.claude/scripts/aseprite_equipment.lua' | Out-Host
& $exe -b --script-param op=verify --script-param "src=$originalInput" --script-param "out=$newOutput" --script-param "through=$originalFrameCount" --script '.claude/scripts/aseprite_equipment.lua' | Out-Host
```

输出不能等于源路径，已存在输出不会覆盖。仅追加时用 `verify` 核对本批最初输入的全部原有帧。补位或修整时，用 `verify` 核对第一处改动前的帧，逐帧比对其余原有帧的未改 Cel、帧号与时长。补位帧还须核对本套及补齐身体以外的空 Cel 保持为空。输入 PNG 用 `Image{fromFile=...}` 读取单图，避免打开连续编号 PNG 时被自动识别为序列。通用 JSON 需要 Aseprite 自带 `json.encode/decode`，其他版本按其官方 Lua API 处理。

## Cel 的 Z 顺序

Cel 属性里的 Z 对应 Lua 的 `zIndex` 与 manifest 的 `z_index`，表示该 Cel 在当前帧相对默认图层堆叠的绘制顺序偏移。`0` 按图层顺序绘制，正值向前，负值向后。所有魔物的套装装备均沿现有图层顺序绘制，新装备省略 `z_index`。通用工具在替换已有 Cel 时省略该字段会保留原值。修整或迁移旧套装时，对涉及的旧装备 Cel 在 `replace` 中显式填 `"z_index": 0`，或直接用官方 Lua 设置 `cel.zIndex = 0`。只处理目标装备 Cel，保留非装备 Cel 的属性。保存后回读目标 Z 和原图层顺序，再原生导出对应帧的效果图。[Aseprite Cel.zIndex](https://aseprite.org/api/cel#celzindex)

## 只加强描边

先导出认可版本的套装帧，再用 PNG 工具生成只含目标层的 `replace` manifest。

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File '.claude/scripts/run-python.ps1' '.claude/scripts/aseprite_equipment_png.py' contour --manifest "$exportDir/manifest.json" --output-dir "$tempRoot/contours" --layer 'Hat_1' --layer 'Clothes_1' --color '#111219' --min-luma 24 --keep-highlight 155
```

每个 `--layer` 填目标 Cel 的完整路径。若帧内的导出 manifest 有多个同名路径，先复制 manifest，按 `layer_indices` 为该路径只保留本次选中的一个条目，保持图片相对路径一致。多个同名层都要修整时，分别生成描边 PNG，再合并带 `layer_indices` 的条目。旧源文件仍可用 `--prefix` 按末级图层名的前缀匹配，也可重复 `--exclude 层路径` 排除指定层。工具只改原 alpha 内四邻域暴露边缘，保留形状、内部像素及高于 `keep-highlight` 的亮点。内部接缝按部件做局部像素修整，再逐件检查。

## 装备帧身体核对

历史套装有时也给同帧的身体 Cel 加了排序偏移。要恢复该套的原生层序时，先与母版同部件对照，再清零确认只为这套临时设置的偏移。核对身体像素、位置和其他帧，并重新导出合成预览。

`aseprite_equipment.lua op=verify through=N` 检查前 N 帧整体是否保持不变。补位或追加帧的身体 Cel 单独按以下方式验收。

1. 制作前用 `op=export` 导出目标帧原有身体 Cel 和 manifest。补齐或追加所需的 Cel 另从核对过的站姿源帧导出，记录每层实际来源帧。参考图中的身体不能作为此基准。
2. 保存后再次用 `op=export` 导出装备帧，到另一个空临时目录。按预先记录的图层路径和同名层索引匹配各身体 Cel，独立核对装备 Cel 的归属和偏移。
3. 在本次验证脚本中逐项断言：身体 Cel 的存在性、原始宽高、画布 x/y、每个 RGBA 像素、opacity、zIndex 均与基准一致；核对图层可见性、opacity、blendMode 和相对顺序。导出 manifest 未提供的层属性/顺序需经官方 Lua 读取，不能漏项后声称全通过。
4. 另行核对原有帧未改 Cel 与时长，复核母版 SHA-256，确认补位与修整保留总帧数，追加只增加本批实际使用的新帧。需要调整帽口时修整装备本身。
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

从用户提供或确认的装备预览原图读取像素。核对套装编号与所在格或实际轮廓，按原图坐标裁出完整区域，检查帽饰、翅膀、披风和武器等边缘。单套原图可直接另存。将保留原尺寸、像素、色彩和背景的 PNG 裁片写入本批临时目录，作为取色基准和压缩输入。重复制作同一参考时从原图重新裁切或沿用原始裁片。

## 压缩参考 PNG

新交付的参考图默认等比用 Lanczos 缩小，优先保存为索引色 PNG。重制时从 Git 取同帧最近已提交的缩略图作为样本。新增帧先找同角色同类装备中构图比例相近的已提交参考，再找其他角色同类装备中比例相近的参考。记录样本路径和版本。工具读取样本的宽、高和实际字节数，从能放进该尺寸框的最大等比尺寸开始，优先保留细节，逐档减少色数，仍超出字节预算才缩小尺寸。透明图在索引色无法保全缩放后 alpha 时可交付达标的 RGBA PNG。字节预算取**该套未缩放裁片 PNG 的实际字节数除以 10（向下取整）**和**历史缩略图的实际字节数**中的较小值。确无历史样本时，仅采用原裁片的字节预算并在交付时注明。压缩采用独立的候选路径，输入使用原始裁片。

```powershell
$candidate = Join-Path $tempRoot "reference-$equipmentFrame.png"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File '.claude/scripts/run-python.ps1' '.claude/scripts/aseprite_equipment_png.py' compress-reference --input $referenceCropPng --output $candidate --thumbnail-like $historicalThumbnailPng
```

核对成功报告的 `input_bytes`、`budget_bytes` 和 `output_bytes`，确认输出字节数不超过预算，并回读正式图核对宽、高分别在所选历史样本的范围内。回读候选确认编码与 `palette_colors` 相符，`null` 表示透明图的 RGBA 候选。尺寸须与 `width`、`height` 一致，宽高比保持在整数像素的舍入范围内。用同一套的原始裁片对照构图、装备轮廓、细节和透明边缘。通过后保存为 `<母版名>_帧<实际帧号>_参考.png`，与同帧效果预览和共用装备源文件一起写入[正式装备目录](../SKILL.md#正式交付路径)，回读确认正式文件与候选一致。

极小裁片或无法保全透明信息时，工具可能非零退出且没有输出。保留原始裁片，报告该套未能达到的字节目标。需要原像素时走下方的无损方式。

## 无损优化参考 PNG

用户要求保留原像素时，可用工具重压缩静态 PNG 的像素数据，并移除可省的备注、软件信息和修改时间。工具保留尺寸、全部像素样本、透明度、色彩和方向相关数据，以及来源和版权等文字信息。无损方式按实际收益交付。

```powershell
$candidate = Join-Path $tempRoot 'reference-optimized.png'
powershell.exe -NoProfile -ExecutionPolicy Bypass -File '.claude/scripts/run-python.ps1' '.claude/scripts/aseprite_equipment_png.py' optimize-reference --input $referencePng --output $candidate
```

候选路径须与输入不同且尚不存在。工具回读比对无误才写入更小版本，否则按原字节输出。用报告的 `input_bytes`、`output_bytes` 和 `saved_bytes` 核对体积。只有 `saved_bytes` 大于 `0` 才采用候选，并沿用正式参考图的帧号文件名。
