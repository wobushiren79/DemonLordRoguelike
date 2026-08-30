---
name: story-highlight-flicker-fix
description: 故事演出高亮"亮→亮"切换防闪烁与出现动画的实现约定：对话步骤间复用 UIGameConversation 不重走 OpenUI、ApplyStoryHighlight 状态机化（亮→亮位置插值、首现淡入）
metadata:
  type: project
---

故事演出目标高亮（UIGameConversation.MaskTarget）的闪烁与动画约定（2026-08 定稿）：

1. **亮→亮切换闪烁根因**：`PlayTalkOnce` 原来每句 `OpenUI`/点击回调 `CloseUI`（关闭重开有约 1 帧间隔），且 `OpenUI` 内含 `HideStoryHighlight`（SetActive(false)→true 让压暗"消失一瞬再淡入"）。修复：对话步骤内连播/相邻对话步骤间**复用同一 UIGameConversation 实例**（`StoryManager.storyConversationUI`，PlayTalkOnce 判定 `gameObject.activeInHierarchy` 时不重走 OpenUI——OpenUI 的 Hide 防残留语义只保留给非故事入口：首句/议会交谈等）。
2. **关闭时机**：非对话步骤（镜头/等待等）进入前 `CloseStoryConversationUI()`（在 `PlayStoryAsync` 的步骤循环里做，**不能放 ExecuteStep 末尾**——async 并发步骤会打断开着的前一句对话）；故事收尾 `FinishStory` 兜底关闭。
3. **动画状态机（ApplyStoryHighlight）**：`wasActive = ui_MaskTarget.gameObject.activeSelf`（SetActive(true) 之前取值）。true（亮→亮）= alpha 恒定（压暗不闪），材质 `_Center/_Size` 从旧值 **0.18s DOVctor 插值**到新目标（洞移动的可见出现动画；快速连点 Kill 重启）；false（首现/无亮→有亮）= mask alpha 从 0 **DOFade 0.12s 淡入**（`SetUpdate(true)`，战斗 timeScale=0 下照常）。
4. **动画收口**：`HideStoryHighlight`/`CloseUI`/`OnDestroy` 都 Kill `highlightFadeTween`+`highlightMoveTween` 并复位 color（防残留 tween 拉 alpha 造成闪动/访问已销毁材质）。

**Why:** 用户感知的"闪"本质是"压暗恒定态被破坏一帧"；"没动画"本质是只改位置不动 alpha。位置插值 + alpha 恒定的组合同时满足"切换不闪"与"每次切换都有出现动画"。

**How to apply:** 后续调动画时长/曲线改 `ApplyStoryHighlight` 里 0.18s/0.12s 即可；新增 UI 高亮类功能时遵循"Overlay 相机传 null"（[[feedback-story-highlight-overlay]]）与"复用实例不重走 OpenUI"两个约定。
