---
name: reference_colored_icons
description: 彩色游戏图标尺寸、限色规范与编号进度。正式深渊馈赠图标为 16×16，成就与深渊暂存素材沿用 32×32 流程。
metadata:
  type: reference
---

深渊馈赠 `ui_abyssalblessing_*` 和成就 `ui_achievement_*` 使用彩色、黑色描边、居中构图和简明明暗。各自通过 `icon_res` 引用 `AtlasForAbyssalBlessing` 和 `AtlasForAchievement` 中的 Sprite。单张颜色上限按任务要求，2026-07-12 的约定为 **≤6 色**。

**正式深渊馈赠图标**（2026-11-10）：`Assets/LoadResources/Textures/AbyssalBlessing/` 的 127 张 PNG 均为原生 **16×16**。原 32×32 的 114 张参考用户选定的金币第二版重绘，保留原图主体和各自色系，使用较大的色块、清晰描边与二值透明。原位更新 PNG 并保留 `.meta`，以维持文件名、Sprite GUID 和图集引用。成就与 `Assets/Out/AbyssalBlessing/` 的暂存素材沿用 32×32 流程。

新图标先按[像素图生成方式](../../CLAUDE.md#像素图生成方式)确定流程。选择生图后转换时，可用项目内 **PixelDa 编辑器工具**（菜单 Custom/AI/像素图生成，见 pixelda skill）出图。PixelLab 已于 2026-10 停用。

**控色 ≤N 技巧**：下载/出图后做中位切分量化——关键：**先把透明区合成到黑底**(`Image.composite over black`)再量化,让透明背景与黑描边共用同一个"黑"调色板槽,不浪费槽位;量化后 `putalpha` 还原透明。硬保证不透明像素 ≤N 色达标。

**选题经验**：每个概念写明**主色/材质**(如"炽红握拳缠绕橙焰")。已用 2600+ 题材,后期靠"同物×5彩色变体"(红/蓝/绿/金/紫)扩量,仍出不同图;可接受重复,用户导入去重。

**落盘流程**：产出按编号顺序落盘 `Assets/Out/AbyssalBlessing/`、`Assets/Out/Achievement/`（暂存），用户再移入正式 `Assets/LoadResources/Textures/<子目录>/`。

**编号进度(截至 2026-07-12)**:
- 深渊馈赠:正式目录 `ui_abyssalblessing_0..125`;暂存 `Assets/Out/AbyssalBlessing/` 新增 **`ui_abyssalblessing_126..2621`(2496 张,连号无缺口,≤6 色)** 待用户导入。**下一个从 2622 起**。
- 成就 `ui_achievement_*`:原始按类型命名(`_time`/`_kill`/`_clear`);AI 新增按下标 `ui_achievement_0..63`。**下一个从 64 起**。
- 正式目录现有：AbyssalBlessing 共 127 张（114 张编号图标与 13 张具名图标），Achievement 含 time/kill/clear + 0–63。新产出仍在 `Assets/Out/` 待移入。
