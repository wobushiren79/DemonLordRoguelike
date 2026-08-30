---
name: feedback-story-highlight-overlay
description: 故事演出目标高亮 UV 换算的相机约定：UI Canvas 是 ScreenSpaceOverlay 时必须传 null 相机，用主相机投影会得到天文数字屏幕坐标导致高亮洞错位
metadata:
  type: feedback
---

故事演出目标高亮（UIGameConversation.SetStoryHighlight / WorldPointToMaskUV）的 UV 换算相机约定：

- 本项目 UI Canvas（UIBase 容器等）实际渲染模式是 **ScreenSpaceOverlay**（`BaseUIManager` 只给容器赋值 `canvas.worldCamera` 字段，**并没有把 renderMode 设为 ScreenSpaceCamera**，worldCamera 不参与渲染）。
- Overlay Canvas 下 UI 元素的世界坐标就是**像素平面**（如 `(1920,0,0)`），`RectTransformUtility.WorldToScreenPoint` / `ScreenPointToLocalPointInRectangle` 的相机参数**必须传 null**（UGUI 约定：世界点即像素平面，无投影相机）。
- 若误传主相机（`Camera.main`，含 CameraManager.uiCamera 的兜底实现）做三维投影，屏幕坐标会变成 371962 这类天文数字 → 换算后高亮洞会错位到屏幕中心附近且被尺寸下限 clamp 到 0.03——表现是"整屏压暗但目标区域没有透亮、中心出现一个小亮块"。

**Why:** `UIGameConversation.cs` 初版高亮按 ScreenSpaceCamera 假设传了 uiCamera（=Camera.main），而实际是 Overlay；静态看 BaseUIManager 代码容易误判成 ScreenSpaceCamera（它确实赋了 worldCamera），只有实测 Editor 状态（renderMode=ScreenSpaceOverlay）才暴露。

**How to apply:** 写 UI 坐标换算时先取所属 Canvas 的 renderMode：Overlay → 相机传 null；ScreenSpaceCamera → 用 canvas.worldCamera。场景世界物体（Bounds/demon/crystal）投影屏幕才用 mainCamera 做第一段投影。运行时诊断可打印 `mask.canvas.renderMode` 与 `uiMaskWorldCorner → screen`（正常应为像素值，异常值如 371962 即中招）。修复后实现见 `UIGameConversation.GetMaskUVCamera()`（Overlay 返回 null / ScreenSpaceCamera 返回 worldCamera）。相关：[[story-highlight-flicker-fix]]（亮→亮切换防闪烁与高亮动画）。
