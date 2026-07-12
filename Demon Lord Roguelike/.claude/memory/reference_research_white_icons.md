---
name: reference_research_white_icons
description: 研究图标 ui_research_* 的统一风格与 PixelLab 纯白16x16生成流水线、当前编号进度
metadata:
  type: reference
---

研究界面图标 `ui_research_N.png` 的风格 = **原生 16×16、纯白(255,255,255)剪影 + 透明负空间表现内部细节、无描边、单个粗壮居中符号**（参考 `Assets/LoadResources/Textures/Research/`）。用户多次要求"凑同款"，下面是已固化的生成流水线（与 [[feedback_pixellab_require_consent]]、[[feedback_pixellab_auto_download]] 配合）：

**生成流水线**（每次约 20 generations/批）：
1. 概念：多主题头脑风暴，产出适合做剪影的概念；把已用题材塞进排除清单（已用 570+ 个，越往后越难找新的粗壮实心物体，可接受少量重复，用户导入时自己去重）。
2. PixelLab `create_1_direction_object`：`size=64`、`view=sidescroller`、`item_descriptions=[16个noun phrase]` → 一次出 16 个候选（frame i ↔ item_descriptions[i]）。异步返回，多批一起 fire 再轮询。**CDN 有传播延迟**：对象状态已 review 但 backblaze 帧可能仍 404，等 **110s** 再下载可基本免重试；仍 404 就再等 40s 重试。
3. 帧 URL 模板：`https://backblaze.pixellab.ai/file/pixellab-characters/objects/be829c7e-0d14-482e-baef-ce1c6fe308b0/<OBJECT_ID>/rotations/frame_<n>.png`（项目段 be829c7e... 对本账号恒定）。可直接按 object_id 逐帧下载，**无需先 select_object_frames**，也无需读 get_object 的长输出（省 token）。
4. **后处理(关键，2026-07 修正)**：正确做法是 **实心剪影挖黑**——`每个不透明像素→纯白`，**仅**把近黑像素(`luminance < dark_cut`，`dark_cut=35`)挖成透明(负空间：骷髅眼眶/内部线)，包围盒取所有不透明像素，裁剪居中缩放到 16×16(留 1px 边)。⚠️ 旧记忆的"亮度阈值化 luminance≥115→白"是**错的**——那只保留高光，剪影会碎成散点；`dark_cut` 太高(如 70)也会把内部阴影挖穿导致碎裂。脚本 `process_batch.py`(scratchpad，含下载+挖黑一体) 的 `to_white16(dark_cut=35)`。
5. 联系表(`contact.py`,带编号)目测剔除：**细长武器**(刀/剑/弩/戟/镰/链枷)在 16px 太细读不出、纯"胖圆X"概念(fat/round blob)会渲成无特征白饼——都砍；保留有清晰轮廓特征的实心物体(头盔/盾/塔/桶/瓮/钟/砧/紧凑动物/建筑)。单批可用率约 9~12/16。
6. 落盘：`save.py` 按编号顺序拷进 `Assets/Out/Research/`（暂存），用户再自行移入正式目录 `Assets/LoadResources/Textures/Research/` 并配像素导入设置(Sprite/Point/无压缩)。

**编号进度（截至 2026-07-12）**：正式目录 `ui_research_1..200`；暂存 `Assets/Out/Research/` 新增 **`ui_research_201..577`(377 张，连号无缺口)** 待用户导入。**下一个新图标从 578 起编号**。全部 16×16 纯白剪影，抽样校验通过。
