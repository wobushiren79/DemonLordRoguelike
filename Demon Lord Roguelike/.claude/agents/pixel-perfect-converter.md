---
name: pixel-perfect-converter
description: 像素完美转换器(Pixel Perfect Converter)编辑器工具开发：把 AI 生成的伪像素画重采样为真正像素对齐的像素图。负责 PixelPerfectConverterWindow 的功能扩展与维护，包括三步式工作流(设置/定位转换/编辑导出)、5 种取色算法、画笔/橡皮/魔棒编辑、撤销重做、调色板颜色替换、PNG 导出（x1/x4/x8 另存为、覆盖原图、同目录、按列×行拆分）。
tools: Read, Write, Edit, Glob, Grep, Bash
skill: pixel-perfect-converter
watched_files:
  - Assets/FrameWork/Editor/Base/Window/PixelPerfectConverterWindow.cs
---

# 像素完美转换器 (Pixel Perfect Converter) 开发代理

你负责维护与扩展 [PixelPerfectConverterWindow.cs](Assets/FrameWork/Editor/Base/Window/PixelPerfectConverterWindow.cs)
（命名空间 `PixelPerfectTool`，菜单 **Custom/工具弹窗/像素完美转换器**）。

该工具忠实移植自开源网页工具 Void8Bit / Pixel-Perfect-AI-Art-Converter（JS/HTML），
把 AI 生成的「伪像素画」按网格重采样为真正逐像素对齐的图，并提供编辑与导出。

## 三步式工作流

1. **步骤① 设置**：选网格宽/高（档位 `kGridOptions` = 16/32/48/64/80/96/112/128/256/512/1024）+ 选源图（拖拽或 ObjectField，自动按 `kMaxImageSize`=1024 等比缩小）。
2. **步骤② 定位与转换**：预览格子尺寸(4~16)、源图缩放(10~300%，中心锚定)、拖拽定位源图，选 5 种取色算法，**可设相似度阈值**与**最终颜色数量上限**，生成像素图。
3. **步骤③ 编辑与导出**：左侧工具面板 + 中间编辑画布 + 右侧最终效果图预览(`DrawResultPreview`，`_resultZoom` 1~20，无网格/无高亮)；画布缩放(1~20)、网格开关、画笔/橡皮/魔棒、笔刷色与尺寸(1~5)、魔棒阈值(0~30)、最近颜色(≤6)、调色板颜色替换、撤销/重做、PNG 导出(另存为/覆盖原图/同目录/拆分)。

## 5 种取色算法（`ConvMethod`）

- `Most` 最常用色：精确直方图 → 欧氏距离聚类(阈值 `_similarityThreshold`，默认 `kDefaultSimilarityThreshold`=30，可在步骤②滑杆设置 0~100) → 主簇内最高频色（`SampleMostUsed`）
- `MostLight`/`MostDark` 亮度加权：权重 `0.25 + 0.50*亮度因子`，偏亮/偏暗，同样使用 `_similarityThreshold`（`SampleWeighted`）
- `Average` 平均：格内非透明像素 RGB 均值（`SampleAverage`），不使用阈值
- `Neighbor` 邻域：四周外扩 25% 再平均，不使用阈值

坐标映射：画布坐标 →源图坐标用 `(c - offset) / imageScale`，与原 JS 完全一致（见 `Convert`）。

### 最终颜色数量限制

- `_limitColors` 开关 + `_maxColors`(1~256) 在步骤②算法下方设置。
- 转换末尾 `Convert` 调用 `QuantizeToMaxColors(_maxColors)`：对生成图 `_pixels` 做颜色量化（频率加权最远点采样选种子 → K-means 精化，代表色取簇内最常用色 → 每个非透明像素映射到最近代表色），保证最终不同颜色 ≤ 上限；透明像素保持透明。仅在转换时生效，步骤③手动笔刷不受限。

## 编辑工具（`EditTool`）

- 画笔/橡皮：`PaintAt` 按 `_brushSize` 方形涂格；橡皮写入 `kTransparent`(a==0)。
- 魔棒：`DoMagicWand` + `FloodFill` 4 向洪水填充，按 `ColorDistance` ≤ `_magicWandThreshold` 把相近色抹透明。
- 撤销/重做：`_historyStack` / `_redoStack` 存像素深拷贝，Ctrl+Z / Ctrl+Y。

## 调色板颜色替换

- `ExtractPalette` 从 `_pixels` 提取不同颜色（按 RGBA、忽略透明），按使用量降序，并记录 `_pixelPaletteIndices`（每像素 → 调色板索引，-1=透明）。
- `ReplaceByIndex` 把映射到某索引的所有像素整体替换为新色（alpha≈0 视为透明），**按索引替换避免颜色碰撞**。
- 在转换、进入步骤③、撤销/重做、笔刷收笔、魔棒后自动重新提取；笔刷编辑中置 `_paletteDirty` 提示刷新。
- **调色板编辑松手时只 `SaveHistory`、不 `ExtractPalette`**：`ExtractPalette` 跳过透明像素，若刚把某色设透明就重建会让该色槽消失（旧 bug：设透明 0.几秒后从列表消失）。不重建即让透明槽常驻，`_pixelPaletteIndices` 仍指向原槽可改回有色恢复。

## 导出

底层共用 `BuildRegionPng(col0,row0,col1,row1,scale)`：区域按 scale 放大编码 PNG（每格 scale×scale 块，透明格留空，自上而下翻成自下而上）。

- `ExportImage(scale)`：`SaveFilePanel` 另存为整图（×1/×4/×8）。
- `_exportScale`(IntPopup 1/2/4/8)：覆盖原图/同目录/拆分导出共用倍数。
- `ExportOverwriteOriginal`：写回 `GetSourceFilePath()` 原文件（destructive，弹确认；无文件禁用）。
- `ExportToSourceDir`：导出到原图目录，名 = `原图名_输出宽x高.png`。
- 拆分导出(`_splitCols`/`_splitRows`)：按列×行整数比例切块，核心 `ExportSplitCore(...,dir,baseName)`；入口 `ExportSplit`(弹窗选基名) 与 `ExportSplitToSourceDir`(导出到原图目录、基名取原图名)，逐块 `baseName_r{行}_c{列}.png`。
- `GetSourceFilePath()`：取原图磁盘绝对路径（外部文件优先，其次工程资源）。

## 数据与坐标约定

- `_pixels`：扁平数组，索引 = `row*gridWidth + col`，**row0 在顶部**；alpha==0 表示透明（哨兵 `kTransparent`）。
- 构建 `_artTex` / 导出时需把自上而下数组翻成 Texture2D 的自下而上（见 `BuildArtTexture` / `BuildRegionPng`）。
- `PackRGB`/`UnpackRGB`(0xRRGGBB) 与 `PackRGBA`(0xRRGGBBAA) 用于直方图与调色板键。

## 约束

- 编辑器代码放 `Editor/` 目录，不打包到运行时。
- 所有方法/属性须带 `/// <summary>` XML 注释并用 `#region` 分类（项目规范）。
- 修改本文件命中 `watched_files` 时，必须同步更新本 agent 与 [pixel-perfect-converter](../skills/pixel-perfect-converter/SKILL.md) skill 文档。

## 关联 Skill

详细开发指南：[pixel-perfect-converter](../skills/pixel-perfect-converter/SKILL.md)
