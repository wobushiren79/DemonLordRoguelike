---
name: reference_colored_icons
description: 彩色游戏图标(深渊馈赠/成就)32x32 ≤8色生成流水线与编号进度
metadata:
  type: reference
---

彩色像素图标(深渊馈赠 `ui_abyssalblessing_*`、成就 `ui_achievement_*`)风格 = **32×32、彩色、黑色描边、单个粗壮居中徽记、每张 ≤N 色**(参考现有 `Assets/LoadResources/Textures/AbyssalBlessing/` 与 `.../Achievement/`,均带描边+简单明暗;`icon_res` 字段引用,走 `AtlasForAbyssalBlessing`/`AtlasForAchievement` 图集)。与白色 [[reference_research_white_icons]] 同源但彩色。**⚠️ 单张颜色上限随需求变**:早期约定 ≤8,2026-07-12 用户收紧到 **≤6 色**——按当次要求调 `quantize` 的 colors 参数。

**生成流水线**:
1. 概念:多主题头脑风暴,每个概念 desc 写明**主色/材质**(如"a glowing crimson clenched fist wreathed in orange fire")。已用 2600+ 题材,后期靠"同物×5彩色变体"(红/蓝/绿/金/紫)扩量,仍出不同图;可接受重复,用户导入去重。
2. 批量生成:PixelLab `create_1_direction_object` **size=32**(→一次出 **64** 个原生 32px 候选,cost≈20 gen/call,frame i↔item_descriptions[i],给满 64 个 desc 最划算),base description="a colorful pixel-art game icon, single bold centered emblem, clean black outline, flat cel-shaded vibrant colors, on transparent background"。**质量极好,单批可用率近 64/64**,无需放大再缩。**CDN 传播延迟**同白色图:review 后等 **110s** 再下载基本免 404;偶发 404 再等 40s 重试(可用 curl 测 frame_0 的 HTTP 码确认)。
3. **控色 ≤N**:下载后用 `quantize6.py`(scratchpad,中位切分量化)——关键技巧:**先把透明区合成到黑底**(`Image.composite over black`)再量化,让透明背景与黑描边共用同一个"黑"调色板槽,不浪费槽位;量化后 `putalpha` 还原透明。硬保证不透明像素 ≤N 色达标(本轮 ≤6,抽样校验 max=6)。
4. 下载 URL:`create_1_direction_object` 帧用 backblaze `.../objects/be829c7e-.../<oid>/rotations/frame_<n>.png`(直接按 object_id 逐帧下,无需 select_object_frames);`create_map_object`(单个,有 outline/shading 控制,限流~4并发)用 `https://api.pixellab.ai/mcp/map-objects/<id>/download`。
5. 联系表(`contact.py`)目测优选,`save.py` 按编号顺序落盘 `Assets/Out/AbyssalBlessing/`、`Assets/Out/Achievement/`,用户再移入正式 `Assets/LoadResources/Textures/<子目录>/`。`process_batch.py` 一体完成 下载+量化;四脚本均在会话 scratchpad,随会话重建。

**编号进度(截至 2026-07-12)**:
- 深渊馈赠:正式目录 `ui_abyssalblessing_0..125`;暂存 `Assets/Out/AbyssalBlessing/` 新增 **`ui_abyssalblessing_126..2621`(2496 张,连号无缺口,≤6 色)** 待用户导入。**下一个从 2622 起**。
- 成就 `ui_achievement_*`:原始按类型命名(`_time`/`_kill`/`_clear`);AI 新增按下标 `ui_achievement_0..63`。**下一个从 64 起**(本轮未生成成就图,只做了研究+深渊)。
- 正式目录现有:AbyssalBlessing 含 0–125,Achievement 含 time/kill/clear + 0–63;新产出仍在 `Assets/Out/`,待移入。
