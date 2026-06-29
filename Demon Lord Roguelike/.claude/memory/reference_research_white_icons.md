---
name: reference_research_white_icons
description: 研究图标 ui_research_* 的统一风格与 PixelLab 纯白16x16生成流水线、当前编号进度
metadata:
  type: reference
---

研究界面图标 `ui_research_N.png` 的风格 = **原生 16×16、纯白(255,255,255)剪影 + 透明负空间表现内部细节、无描边、单个粗壮居中符号**（参考 `Assets/LoadResources/Textures/Research/`）。用户多次要求"凑同款"，下面是已固化的生成流水线（与 [[feedback_pixellab_require_consent]]、[[feedback_pixellab_auto_download]] 配合）：

**生成流水线**（每次约 20 generations/批）：
1. 概念：Ultracode 下用 Workflow 多主题 lens 头脑风暴 → 去重策划 → 对抗审校，产出 N 个互不重复、适合做剪影的概念；务必把"已用/已弃/原始参考"全部塞进排除清单（已用约 200 个，越往后越要细分主题）。
2. PixelLab `create_1_direction_object`：`size=64`、`view=sidescroller`、`item_descriptions=[16个noun phrase]` → 一次出 16 个不同对象（frame i ↔ item_descriptions[i]，顺序一一对应）。**过量生成**(候选≈目标×1.4)优中选。异步返回，多批一起 fire 再轮询（60s 间隔规则）。
3. 帧 URL 模板：`https://backblaze.pixellab.ai/file/pixellab-characters/objects/be829c7e-0d14-482e-baef-ce1c6fe308b0/<OBJECT_ID>/rotations/frame_<n>.png`（项目段 be829c7e... 对本账号恒定）。
4. **后处理(关键)**：亮度阈值化——`亮像素(luminance≥115)→纯白`，`暗部/透明→透明`(负空间)，再按内容裁剪居中缩放到 16×16。**切忌**"所有不透明像素→白"，那会把骷髅眼眶/瞳孔等内部细节糊成一团。脚本见会话 scratchpad `to_white16b.py`。
5. 联系表(带编号)目测剔除：最常见的失败是**细长武器**(刀/剑/弩/弓)在 16px 太细读不出——优先砍；保留粗壮实心剪影。
6. 落盘：先写 `Assets/Out/Research/`（暂存，符合自动下载规则），用户再自行移入正式目录 `Assets/LoadResources/Textures/Research/` 并配像素导入设置(Sprite/Point/无压缩)。

**编号进度（截至 2026-06-29）**：`ui_research_1..200` 已存在并连号无缺口（原始 1–61；AI 生成 62–200）。正式目录现含 1–121，暂存 `Assets/Out/Research/` 含 122–200。**下一个新图标从 201 起编号**。
