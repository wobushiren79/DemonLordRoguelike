---
name: aseprite-ai-artist
description: 使用 Aseprite AI Artist 的原生像素网格直接绘制、修复和检查项目 Aseprite 图层。适用于小尺寸头像、头部五官、轮廓描边和其他逐像素调整。
---

# Aseprite AI Artist

本项目通过 npm 安装官方 `@pebbly/aseprite-ai-artist`，版本固定在 `package-lock.json`。本地依赖缺失时，在本目录运行 `npm ci --ignore-scripts --no-audit --no-fund`。

官方工作流由 MCP 提供。使用 `skill://draw` 在原生网格绘制，`skill://fix` 读取并修改指定图层区域，`skill://review` 检查结果。客户端无法读取 MCP 资源时，直接读包内 `skills/draw/SKILL.md`、`skills/fix/SKILL.md`、`skills/review/SKILL.md` 与 `docs/TOOLS.md`。包目录为 `node_modules/@pebbly/aseprite-ai-artist/`。

`start-mcp.ps1` 使用项目的 Aseprite 定位脚本，启动官方无界面 MCP。先调用 `preflight` 验证 Aseprite，再从副本读取像素和编辑。身体部件交付继续遵循 [aseprite-body-parts](../aseprite-body-parts/SKILL.md) 的母版、帧、锚点和预览要求。

用字符网格绘制穿戴装备时，先按[披挂与双臂分层](../aseprite-equipment-from-preview/SKILL.md#披挂与双臂分层)确定后垂的布片和随臂移动的袖甲。根据实穿稿重画时先检查双臂，绘制后在预览副本分别抬臂复查。

## 原生网格绘制与修复

用 `draw` 或 `fix` 时，在字符网格或像素坐标上处理细线的 L 形拐角、双厚暗边、阶梯步长和连通处，保留有意的尖角、镂空与锚点连接。对照原稿的透明区域单独核对轮廓，另看明暗变化。按 `skill://review` 在 1× 和最近邻整数倍放大下检查实际保存的图层与合成。适用的线宽与检查方法见[完美像素检查](../aseprite-pixel-editor/SKILL.md#完美像素检查)。
