---
name: reference_research_white_icons
description: 研究图标 ui_research_* 的统一风格规范、后处理要点与当前编号进度（原 PixelLab 生成流水线已弃用）
metadata:
  type: reference
---

研究界面图标 `ui_research_N.png` 的风格 = **原生 16×16、纯白(255,255,255)剪影 + 透明负空间表现内部细节、无描边、单个粗壮居中符号**（参考 `Assets/LoadResources/Textures/Research/`）。用户多次要求"凑同款"。

**再生成途径**：PixelLab 已停用（2026-10 起不再使用）。新图标用项目内 **PixelDa 编辑器工具**（菜单 Custom/AI/像素图生成，见 pixelda skill）出图，再按下述后处理规范统一风格。

**后处理要点（关键，2026-07 修正）**：正确做法是 **实心剪影挖黑**——`每个不透明像素→纯白`，**仅**把近黑像素(`luminance < dark_cut`，`dark_cut=35`)挖成透明(负空间：骷髅眼眶/内部线)，包围盒取所有不透明像素，裁剪居中缩放到 16×16(留 1px 边)。⚠️ 旧记忆的"亮度阈值化 luminance≥115→白"是**错的**——那只保留高光，剪影会碎成散点；`dark_cut` 太高(如 70)也会把内部阴影挖穿导致碎裂。

**选题经验**：细长武器(刀/剑/弩/戟/镰/链枷)在 16px 太细读不出、纯"胖圆X"概念会渲成无特征白饼——都不可用；保留有清晰轮廓特征的实心物体(头盔/盾/塔/桶/瓮/钟/砧/紧凑动物/建筑)。已用 570+ 题材，越往后越难找新的粗壮实心物体，可接受少量重复，用户导入时自己去重。

**落盘流程**：产出先放 `Assets/Out/Research/`（暂存），用户再自行移入正式目录 `Assets/LoadResources/Textures/Research/` 并配像素导入设置(Sprite/Point/无压缩)。

**编号进度（截至 2026-07-12）**：正式目录 `ui_research_1..200`；暂存 `Assets/Out/Research/` 新增 **`ui_research_201..577`(377 张，连号无缺口)** 待用户导入。**下一个新图标从 578 起编号**。全部 16×16 纯白剪影，抽样校验通过。相关：[[reference_colored_icons]]
