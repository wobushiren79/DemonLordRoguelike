---
name: pixel-perfect-converter
description: Demon Lord Roguelike 的「像素完美转换器」编辑器工具开发指南。使用此 SKILL 当需要创建或修改 PixelPerfectConverterWindow（把 AI 伪像素画重采样为像素对齐图）的功能：三步式工作流(设置/定位转换/编辑导出)、5 种取色算法(最常用/偏亮/偏暗/平均/邻域)、画笔/橡皮/魔棒(洪水填充)编辑、撤销重做、调色板颜色替换(全局换色)、PNG 导出(x1/x4/x8 另存为/覆盖原图/同目录/按列×行拆分)、网格与背景明暗切换等。触发关键词：像素完美、Pixel Perfect、像素图转换工具、AI像素画对齐、颜色替换、调色板替换、PixelPerfectConverterWindow。
watched_files:
  - Assets/FrameWork/Editor/Base/Window/PixelPerfectConverterWindow.cs
---

# 像素完美转换器开发指南

## 概述

[PixelPerfectConverterWindow.cs](Assets/FrameWork/Editor/Base/Window/PixelPerfectConverterWindow.cs)
（命名空间 `PixelPerfectTool`，菜单 **Custom/工具弹窗/像素完美转换器**；另在 Project 窗口选中图片后右键 **Assets/像素完美转换器** 可直接用该图打开）是一个 `EditorWindow`，
忠实移植自开源网页工具 [Void8Bit / Pixel-Perfect-AI-Art-Converter](https://github.com/Void8Bit/Pixel-Perfect-AI-Art-Converter)。
它把 AI 生成的「看起来像像素画、实则没有像素对齐」的图，按网格重采样为真正逐像素对齐的像素图，并可编辑、导出。

> 区别于已删除的旧工具 PixelArtConverterWindow（量化+调色板一次性导出）：本工具是完整的三步式工作流，
> 且已把旧工具的「调色板颜色替换」能力整合进步骤③。

## 三步式工作流

### 步骤① 设置（`DrawStep1`）
- 网格宽/高下拉：档位 `kGridOptions` = `{16,32,48,64,80,96,112,128,256,512,1024}`，默认目标大小 `_gridWidth/_gridHeight` = **32×32**。
- 源图：拖拽区(`DrawSourceDropArea`)或 ObjectField，支持工程内 Texture / 外部图片文件。
- `LoadSource`：读可读像素(`ReadSourcePixels`，按文件解码，回退 RenderTexture) → 按 `kMaxImageSize`=1024 等比缩小 → 生成自上而下采样数组 `_srcTopDown` 与显示纹理 `_srcDisplayTex`。

### 右键快捷入口（`OpenFromSelection`）
- `[MenuItem("Assets/像素完美转换器")]`：在 Project 窗口选中一张 `Texture2D` 图片后右键，直接打开窗口并把该图设为源图、自动 `LoadSource` + `EnterStep2`，跳过步骤①手动选图。
- `OpenFromSelectionValidate`（`[MenuItem(..., true)]` 校验函数）：仅当 `Selection.activeObject is Texture2D` 时菜单项可用，否则置灰。

### 步骤② 定位与转换（`DrawStep2`）
- 预览格子尺寸 `_previewCellSize`(4~16)：画布每格的「画布像素」数（= canvas.width/gridWidth）。
- 源图缩放 `_zoomPercent`(10~300)：`_imageScale = 值/100`，以画布中心为锚（`ApplyZoom`）。
- 拖拽定位：`HandleStep2Drag` 把屏幕位移 / 显示缩放换算成 `_offsetX/_offsetY`。
- `RefitImage`：居中适配并把缩放滑杆复位到 100（与原工具一致）。
- 算法下方可设：相似度阈值 `_similarityThreshold`(0~100)、限制最终颜色数 `_limitColors` + `_maxColors`(1~256)。
- 选算法后 `Convert` 生成 `_pixels`（末尾按需 `QuantizeToMaxColors`）。

### 步骤③ 编辑与导出（`DrawStep3`）
- 三栏布局：左侧工具面板 + 中间实时编辑画布(`DrawEditCanvas`，渲染 Point 纹理 `_artTex`，带网格/笔刷高亮) + 右侧「最终效果图」(`DrawResultPreview`，同 `_artTex` 但**无网格/无高亮**，缩放 `_resultZoom`(1~20)，宽高超限时内部滚动)。
- 底部全宽「调色板/颜色替换」面板(`DrawPaletteSection`)。

## 5 种取色算法（`ConvMethod` / `Convert`）

| 枚举 | 含义 | 实现 |
| --- | --- | --- |
| `Most` | 最常用色 | `SampleMostUsed`：精确直方图 → 阈值 `_similarityThreshold` 欧氏距离聚类 → 主簇内最高频色 |
| `MostLight` | 最常用(偏亮) | `SampleWeighted`：亮度加权聚类，权重 `0.25+0.50*(亮度/255)`，用 `_similarityThreshold` |
| `MostDark` | 最常用(偏暗) | `SampleWeighted`：权重 `0.25+0.50*((255-亮度)/255)`，用 `_similarityThreshold` |
| `Average` | 平均色 | `SampleAverage`：格内非透明像素 RGB 均值（不用阈值） |
| `Neighbor` | 邻域色 | 在格子四周各外扩 25% 后做平均（不用阈值） |

- 亮度公式：`0.299*r + 0.587*g + 0.114*b`。
- 画布→源图坐标：`o = (canvasCoord - offset) / imageScale`，floor/ceil 后夹紧到源图范围；空区域置透明。

### 相似度阈值（可设置）

- `_similarityThreshold`（默认 `kDefaultSimilarityThreshold`=30）：相近色归并阈值（RGB 欧氏距离），在步骤②算法下方滑杆设置 0~100；越大越容易合并相近色。
- 仅 `Most`/`MostLight`/`MostDark` 聚类算法使用；`Average`/`Neighbor` 选中时该滑杆禁用。

### 最终颜色数量限制（可设置）

- `_limitColors` 开关 + `_maxColors`(1~256)：步骤②算法下方设置。
- 转换末尾 `Convert` 调用 `QuantizeToMaxColors(_maxColors)`：直方图 → 频率加权最远点采样选种子 → K-means 精化（代表色取簇内最常用色）→ 每个非透明像素映射到最近代表色，保证生成图不同颜色数 ≤ 上限；透明像素保持透明。
- 仅转换时生效；步骤③手动笔刷/调色板替换不强制此上限。

## 编辑工具（`EditTool`）

- **画笔/橡皮**：`PaintAt` 按 `_brushSize`(1~5) 方形涂格；橡皮写 `kTransparent`(a==0)。改色自动切回画笔。
- **魔棒**：`DoMagicWand` 收集笔刷区域内所有非透明色，逐色用 `FloodFill`(4 向，`ColorDistance` ≤ `_magicWandThreshold`(0~30)) 抹透明。
- **撤销/重做**：`SaveHistory`/`Undo`/`Redo`，`_historyStack`/`_redoStack` 存像素深拷贝；快捷键 Ctrl+Z / Ctrl+Y（`HandleShortcutKeys`）。
- **最近颜色**：`AddRecentColor`，最多 6 个，点击回填为画笔色。

## 调色板颜色替换（移植自旧 PixelArtConverterWindow 的核心能力）

- `ExtractPalette`：从 `_pixels` 提取不同颜色（按 RGBA、忽略透明），按使用量降序填充 `_palette`/`_paletteCounts`，并记录 `_pixelPaletteIndices`（每像素→调色板索引，-1=透明）。
- `ReplaceByIndex(i)`：把映射到索引 i 的**所有像素**整体替换为 `_palette[i]`（alpha≈0 视为透明）。**按索引替换，天然避免颜色碰撞**，可反复编辑同一槽位（含从透明改回有色）。
- UI：色块网格(`DrawPaletteCell`) 提供 ColorField(替换)、A0/A1(透明开关)、「笔刷」(取此色作画)、占比。
- 同步时机：转换、进入步骤③、撤销/重做、笔刷收笔、魔棒后自动 `ExtractPalette`；笔刷编辑中置 `_paletteDirty` 提示刷新；拖拽色块期间 `_palettePendingHistory`，松手时 `SaveHistory` 合并为一次历史。
- **调色板编辑松手时【不】调用 `ExtractPalette`**：因 `ExtractPalette` 跳过透明像素，若刚把某色设为透明就重建调色板，该色槽会因无像素而消失（旧 bug：设透明 0.几秒后颜色从列表消失）。不重建即让透明槽常驻，`_pixelPaletteIndices` 仍指向原槽，可再改回有色恢复像素。透明槽会在下次笔刷收笔/魔棒/撤销重做/手动刷新时才随重建移除。

## 导出（导出 PNG 卡片）

所有导出底层共用 `BuildRegionPng(col0,row0,col1,row1,scale)`：把像素区域（顶部为 row0）按 scale 放大编码为 PNG 字节（每格 scale×scale 实色块，透明格留空，自上而下数组翻成 Texture 自下而上）。

- **另存为（`ExportImage(scale)`）**：`SaveFilePanel` 选路径，固定 ×1/×4/×8 导出整图。
- **导出倍数（`_exportScale`，IntPopup 1/2/4/8）**：覆盖原图 / 同目录 / 拆分导出三者共用的放大倍数。
- **覆盖原图导出（`ExportOverwriteOriginal`）**：用当前像素图（×_exportScale）写回 `GetSourceFilePath()` 指向的原始文件；destructive，弹确认框；原图无磁盘文件时按钮禁用。
- **同原图目录导出（`ExportToSourceDir`）**：导出到原图所在目录，文件名 = `原图名_输出宽x高.png`。
- **拆分导出（`_splitCols`/`_splitRows`）**：按列×行把整图切块（边界用整数比例 `i*grid/n` 计算，容忍不能整除），核心 `ExportSplitCore(cols,rows,scale,dir,baseName)` 逐块导出 `baseName_r{行}_c{列}.png`。两个入口：`ExportSplit`（`SaveFilePanel` 选基名）、`ExportSplitToSourceDir`（导出到原图目录、基名取原图名，原图无文件时禁用）。
- **`GetSourceFilePath()`**：取原图磁盘绝对路径（优先 `_sourceExternalPath` 外部文件，其次工程内资源 `AssetDatabase.GetAssetPath`→`GetAbsolutePath`）；无文件返回 null（覆盖/同目录导出据此禁用）。
- 写盘后均 `AssetDatabase.Refresh`。

## 关键约定

- `_pixels`：扁平数组，索引 `row*gridWidth + col`，**row0 在顶部**；`alpha==0` = 透明（哨兵 `kTransparent`）。
- 自上而下数据 → Texture2D（自下而上）需翻转：见 `BuildArtTexture`、`LoadSource`、`BuildRegionPng`。
- 外观快捷键：Alt+B 切棋盘背景明暗(`_isDarkBackground`)，Alt+G 切网格线明暗(`_isDarkGrid`)。
- 颜色键：`PackRGB`/`UnpackRGB`(0xRRGGBB)、`PackRGBA`(0xRRGGBBAA)。

## 开发规范

- 编辑器脚本置于 `Editor/` 目录；所有方法/属性带 `/// <summary>` XML 注释并用 `#region` 分类。
- 修改 `PixelPerfectConverterWindow.cs`（命中 `watched_files`）时，必须同步更新本 SKILL 与 [pixel-perfect-converter](../../agents/pixel-perfect-converter.md) agent。
