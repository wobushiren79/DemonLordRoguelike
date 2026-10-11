---
name: reference_colored_icons
description: 彩色游戏图标(深渊馈赠/成就)32x32 ≤N色风格规范、控色技巧与编号进度（原 PixelLab 生成流水线已弃用）
metadata:
  type: reference
---

彩色像素图标(深渊馈赠 `ui_abyssalblessing_*`、成就 `ui_achievement_*`)风格 = **32×32、彩色、黑色描边、单个粗壮居中徽记、每张 ≤N 色**(参考现有 `Assets/LoadResources/Textures/AbyssalBlessing/` 与 `.../Achievement/`,均带描边+简单明暗;`icon_res` 字段引用,走 `AtlasForAbyssalBlessing`/`AtlasForAchievement` 图集)。与白色 [[reference_research_white_icons]] 同源但彩色。**⚠️ 单张颜色上限随需求变**:早期约定 ≤8,2026-07-12 用户收紧到 **≤6 色**——按当次要求调量化的 colors 参数。

**再生成途径**：PixelLab 已停用（2026-10 起不再使用）。新图标用项目内 **PixelDa 编辑器工具**（菜单 Custom/AI/像素图生成，见 pixelda skill）出图。

**控色 ≤N 技巧**：下载/出图后做中位切分量化——关键：**先把透明区合成到黑底**(`Image.composite over black`)再量化,让透明背景与黑描边共用同一个"黑"调色板槽,不浪费槽位;量化后 `putalpha` 还原透明。硬保证不透明像素 ≤N 色达标。

**选题经验**：每个概念写明**主色/材质**(如"炽红握拳缠绕橙焰")。已用 2600+ 题材,后期靠"同物×5彩色变体"(红/蓝/绿/金/紫)扩量,仍出不同图;可接受重复,用户导入去重。

**落盘流程**：产出按编号顺序落盘 `Assets/Out/AbyssalBlessing/`、`Assets/Out/Achievement/`（暂存），用户再移入正式 `Assets/LoadResources/Textures/<子目录>/`。

**编号进度(截至 2026-07-12)**:
- 深渊馈赠:正式目录 `ui_abyssalblessing_0..125`;暂存 `Assets/Out/AbyssalBlessing/` 新增 **`ui_abyssalblessing_126..2621`(2496 张,连号无缺口,≤6 色)** 待用户导入。**下一个从 2622 起**。
- 成就 `ui_achievement_*`:原始按类型命名(`_time`/`_kill`/`_clear`);AI 新增按下标 `ui_achievement_0..63`。**下一个从 64 起**。
- 正式目录现有:AbyssalBlessing 含 0–125,Achievement 含 time/kill/clear + 0–63;新产出仍在 `Assets/Out/`,待移入。
