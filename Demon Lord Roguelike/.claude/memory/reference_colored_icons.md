---
name: reference_colored_icons
description: 彩色游戏图标(深渊馈赠/成就)32x32 ≤8色生成流水线与编号进度
metadata:
  type: reference
---

彩色像素图标(深渊馈赠 `ui_abyssalblessing_*`、成就 `ui_achievement_*`)风格 = **32×32、彩色、黑色描边、单个粗壮居中徽记、每张 ≤8 色**(参考现有 `Assets/LoadResources/Textures/AbyssalBlessing/` 与 `.../Achievement/`,均带描边+简单明暗;`icon_res` 字段引用,走 `AtlasForAbyssalBlessing`/`AtlasForAchievement` 图集)。与白色 [[reference_research_white_icons]] 同源但彩色。

**生成流水线**:
1. 概念:Ultracode 下用 Workflow 多主题 lens 风暴→去重策划→对抗审校,每个概念 desc 写明**主色/材质**(如"a glowing crimson clenched fist wreathed in orange fire, black outline"),排除已有图标题材。
2. 批量生成:PixelLab `create_1_direction_object` **size=32**(→一次出 **64** 个原生 32px 对象,cost≈20 gen/call,frame i↔item_descriptions[i]),base description="a colorful pixel-art game icon, single bold centered emblem, clean black outline, flat cel-shaded vibrant colors, on transparent background"。**质量很好,无需放大再缩**。小批量/预览可用 `create_map_object`(单个/调用,有 outline/shading 控制,但限流~4并发)。
3. **控色 ≤8**:下载后用 `quantize8.py`(中位切分量化,保留透明背景,黑描边自动成为其中一色)——硬保证 ≤8 色约束达标。
4. 下载 URL:`create_map_object` 用 `https://api.pixellab.ai/mcp/map-objects/<id>/download`(公开免鉴权);`create_1_direction_object` 帧用 backblaze `.../objects/be829c7e-.../<oid>/rotations/frame_<n>.png`。
5. 联系表(contact32.py,scale 3)目测优选,落盘 `Assets/Out/AbyssalBlessing/`、`Assets/Out/Achievement/`,用户再移入正式 `Assets/LoadResources/Textures/<子目录>/`。

**编号进度(截至 2026-06-29)**:
- 深渊馈赠 `ui_abyssalblessing_0..119`(原 0–14;AI 预览 15–19 用 create_map_object;AI 批量 20–119 用 create_1_direction_object)。**下一个从 120 起**。
- 成就 `ui_achievement_*`:原始为按类型命名(`_time`/`_kill`/`_clear`);AI 新增按下标 `ui_achievement_0..63`。**下一个从 64 起**。
- 正式目录现有:AbyssalBlessing 含 0–14,Achievement 含 time/kill/clear;AI 产出仍在 `Assets/Out/`,待移入。
