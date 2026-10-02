---
name: project_spine_mod_pma_requirement
description: 所有 Spine Mod 的图集贴图必须 PMA（预乘 alpha），直通 alpha 进 bundle 后会在主工程出现全体白边（BrownDust 2026-09-29 事故）
metadata:
  type: project
---

所有 Spine Mod（AeonsEcho/ArkRe/Nikke/BrownDust/GirlWars/CrossCore 及未来新增）的 spine 图集 PNG 必须是 **PMA（预乘 alpha，透明区 alpha=0 像素 RGB 纯黑）**，材质 `Straight Alpha Texture` 开关必须为关（`_StraightAlphaInput: 0`）。

**Why:** `Spine/Skeleton` shader 固定 PMA 混合（`Blend One OneMinusSrcAlpha`），直通贴图靠 `_STRAIGHT_ALPHA_INPUT` 关键字在片元补乘 alpha；该关键字是 `shader_feature`（构建裁剪），MOD 工程编辑器内按需编译显示正常，但 bundle 里 shader 只带默认 PMA 变体 → 主工程运行时回退 → 直通贴图被按 PMA 直接加算 → 半透明边缘颜色未经 alpha 衰减过曝 → **全体白色描边**。2026-09-29 BrownDust 实发此事故，修复=贴图 rgb*=a 转 PMA + 649 个材质关直通开关 + 重建；同日 CherryTale 接入时也发现全量直通（110 PNG + 172 材质），已按同方案预防性修复（未实发事故）；2026-09-30 Snowbreak（255 PNG/390 材质）与 StarLusts（416 PNG/442 材质）同为全量直通，同方案预防性修复。**注意反向案例**：2026-09-30 Echocalypse 接入时 1234 材质直通开关全开但 1081 张 PNG 原生已是 PMA——开关与贴图不匹配时**只需把关开置 0**（MOD 编辑器内预览暗边也消失），无需像素转换；检查务必先看 PNG 再定方案。

**How to apply:** 新增/接入任何 spine 资源到 MOD 工程时必查两项：① PNG 透明区纯黑（Python+zlib 抽样 alpha=0 像素 RGB，或贴图导入后目测边缘无暗/亮边）；② 该图集全部 `*.mat`（含多页图集的 `{图集}_{页}.mat`，不止 `*_Material.mat`）`_StraightAlphaInput: 0`。不符就先转 PMA 再构建。详见 [[.claude/skills/browndust-spine-mod/SKILL.md]]「注意与边界」。相关：[[reference_research_white_icons]]
