---
name: aseprite-ai-artist
description: 使用 Aseprite AI Artist 的原生像素网格直接绘制、修复和检查项目 Aseprite 图层。适用于小尺寸头像、头部五官、轮廓和其他逐像素调整。
---

# Aseprite AI Artist

本项目通过 npm 安装官方 `@pebbly/aseprite-ai-artist`，版本固定在 `package-lock.json`。本地依赖缺失时，在本目录运行 `npm ci --ignore-scripts --no-audit --no-fund`。

官方工作流由 MCP 提供。使用 `skill://draw` 在原生网格绘制，`skill://fix` 读取并修改指定图层区域，`skill://review` 检查结果。客户端无法读取 MCP 资源时，直接读包内 `skills/draw/SKILL.md`、`skills/fix/SKILL.md`、`skills/review/SKILL.md` 与 `docs/TOOLS.md`。包目录为 `node_modules/@pebbly/aseprite-ai-artist/`。

`start-mcp.ps1` 使用项目的 Aseprite 定位脚本，启动官方无界面 MCP。先调用 `preflight` 验证 Aseprite，再从副本读取像素和编辑。身体部件交付继续遵循 [aseprite-body-parts](../aseprite-body-parts/SKILL.md) 的母版、帧、锚点和预览要求。
