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

`source` 为调用者提供的工作副本，`frame` 为一开始检查确认的帧号。输出 `manifest.json` 和独立 RGBA PNG，包含画布、图层路径、原 Cel 偏移、尺寸、透明度及 Z 顺序。图层组用 `组名/层名` 表达；同名路径歧义时脚本停止。

## 新增一帧或局部替换

准备 JSON manifest，PNG 路径相对于 manifest 所在目录，不能包含父目录跳转。例子中的大小、层名、坐标和来源帧是示意，必须替换为实际 inspect 结果：

```json
{
  "width": 128,
  "height": 128,
  "mode": "append",
  "frame": 25,
  "duration": 0.8,
  "tag": "Equipment_Preview",
  "copy_cels": [
    { "layer": "Body", "from_frame": 7 },
    { "layer": "Head", "from_frame": 7 },
    { "layer": "Eye_1", "from_frame": 2 }
  ],
  "cels": [
    { "layer": "Equipment_Cape", "create": true, "stack_index": 1, "png": "cape.png", "x": 27, "y": 56 },
    { "layer": "Equipment_Hood", "create": true, "png": "hood.png", "x": 49, "y": 33, "z_index": 3 }
  ]
}
```

`append` 仅允许 `frame=原总帧数+1`，新帧先空白，再复制 `copy_cels` 指定身体/脸部，导入所有装备。列表必须包含实际需要的全部身体部件，不能照抄示意。

`replace` 用于已认可版本的现有装备帧，不增帧；仅列出要改的 Cel，未指定的透明度与 Z 顺序保留。已有 Cel 用新 `Image` 断开共享，避免链接帧被同步修改。新图层需 `create:true`，通用脚本只创建顶层图层；新建组应在明确需求下扩展 Lua。

```powershell
& $exe -b --script-param op=apply --script-param "src=$workingCopy" --script-param "out=$newOutput" --script-param "manifest=$manifest" --script '.claude/scripts/aseprite_equipment.lua' | Out-Host
& $exe -b --script-param op=verify --script-param "src=$original" --script-param "out=$newOutput" --script-param "through=$originalFrameCount" --script '.claude/scripts/aseprite_equipment.lua' | Out-Host
```

输出不能等于源路径，已存在输出不会覆盖。输入 PNG 用 `Image{fromFile=...}` 读取单图，避免 `app.open` 自动把连续编号 PNG 当成多帧序列。通用 JSON 需要 Aseprite 自带 `json.encode/decode`；若版本不支持，保留工作副本，按其 Lua API 调整，不写二进制替换文件。

## 只加强描边

先导出认可版本目标帧，再使用 PNG 工具生成局部 replacement manifest：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File '.claude/scripts/run-python.ps1' '.claude/scripts/aseprite_equipment_png.py' contour --manifest "$exportDir/manifest.json" --output-dir "$tempRoot/contours" --prefix 'Equipment_' --color '#111219' --min-luma 24 --keep-highlight 155
```

按实际图层前缀选择；可重复 `--exclude 层路径` 跳过匕首、链条、扣饰等精细部件。工具只改原 alpha 内四邻域暴露边缘，不改形状或内部像素，保留高于 `keep-highlight` 的亮点。它是描边起点，不能替代逐件视觉检查。内部接缝需要针对选定部件手工像素修整时，应单独记录范围，不全图锐化或量化。

## 装备帧身体核对

`aseprite_equipment.lua op=verify through=N` 只检查前 N 帧，不覆盖 N+1 的新增装备帧。要求身体不变时，另做以下只读验证；现有工具没有单独的 `verify_body` 操作，不要假设传入额外参数就已检查。

1. 制作前保存身体基准：用 `op=export` 导出已核对站姿的身体 Cel 和 manifest；若从不同源帧组合身体，记录每层实际来源帧。参考图中的身体不能作为此基准。
2. 保存后再次用 `op=export` 导出装备帧，到另一个空临时目录。按完整图层路径匹配预先记录的身体层，不能按 PNG 编号配对，也不能把所有新增层都当作身体。
3. 在本次验证脚本中逐项断言：身体 Cel 的存在性、原始宽高、画布 x/y、每个 RGBA 像素、opacity、zIndex 均与基准一致；核对图层可见性、opacity、blendMode 和相对顺序。导出 manifest 未提供的层属性/顺序需经官方 Lua 读取，不能漏项后声称全通过。
4. 另行执行旧帧 `verify`，复核母版 SHA-256，确认输出帧数符合 append/replace 模式。身体不变与衣物遮挡身体可以同时成立；不通过移动身体或替换脸部来补救帽口。
5. 若用 Python 合成图辅助适配，将其与正式原生导出核对：alpha 必须一致，alpha 非零位置的 RGBA 必须一致；全透明像素的隐藏 RGB 可忽略。该核对不能替代身体 Cel 比对或视觉验收。

## 最终导出

`--frame-range` 用从零开始的下标，必须放在输入文件前；Lua 帧号从一开始。

```powershell
$index = $equipmentFrame - 1
& $exe -b --frame-range "$index,$index" $newOutput --data "$tempRoot/render.json" --sheet "$tempRoot/native.png" | Out-Host
powershell.exe -NoProfile -ExecutionPolicy Bypass -File '.claude/scripts/run-python.ps1' '.claude/scripts/aseprite_equipment_png.py' preview --input "$tempRoot/native.png" --output $finalPreview --scale 6 --background '#4f5056'
```

省略 `--background` 保持透明。导出后检查实际 PNG 尺寸等于原画布乘倍率，不能意外导出全动画。PNG 工具依赖 Pillow，Lua 工具不依赖外部库。

## 保存原图参考截图

从用户提供或确认的装备预览原图读取像素。生图预览以用户选定的具体版本为准，从会话中的可读取原图或用户提供的同一张图获取像素。拼图先核对套装编号与所在格，按原图像素坐标裁出该套完整区域，检查帽饰、翅膀、披风或武器等边缘。单套参考可原样保存为 PNG。参考截图保留原始像素比例、颜色和背景，单独保存为 `角色_套装_参考.png`。三份成品最终并排放在[装备交付规则](../SKILL.md#正式交付路径)指定的 `装备/` 直属层。`preview` 命令用于从装备帧生成效果预览，参考截图直接来自原图。
