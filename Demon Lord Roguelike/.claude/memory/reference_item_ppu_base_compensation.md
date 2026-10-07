---
name: reference_item_ppu_base_compensation
description: 道具贴图 PPU 已统一为 16（原 100），世界空间道具图标统一走 IconHandler.SetItemIcon(SpriteRenderer 版)：内置 PPU=100 基准补偿，世界尺寸=targetSize÷100 与贴图 PPU 解耦
metadata:
  type: reference
---

道具（Items 图集）贴图的 PixelsPerUnit 已于 2026-10 从 100 统一改为 **16**。SpriteRenderer 的世界尺寸 = 像素 ÷ PPU × localScale，直接换图会放大 100/16 = 6.25 倍（箭矢弹道 204101、宝箱道具图标先后踩坑）。

**定案（2026-10-07 用户拍板）**：世界空间道具图标**统一走 `IconHandler.SetItemIcon`(SpriteRenderer 版） 一个入口**（独立的 `SetItemIconForAttackMode` 已删除），其缩放公式：

```
localScale = contain(targetSize) × ppuFix × scaleMul
contain    = targetSize>0 ? min(tX/宽px, tY/高px) : 1（≤0 = 按原始像素尺寸）
ppuFix     = sprite.pixelsPerUnit / IconHandler.ItemSpriteBasePPU(基准100)
```

效果：**世界尺寸 = targetSize ÷ 100，与贴图实际 PPU 解耦**——默认 100 = 1 世界单位，调用方用「100=1单位」的直觉尺度选值即可，不用关心贴图 PPU。

两个在役用法：

- **宝箱道具图标**（RewardSelectBoxComponent）：默认 targetSize=100 → 1 世界单位。
- **弹道换图**（武器 attack_mode_data 的 ShowSprite）：`HandleItemsInfoAttackModeData` 在**遍历解析完之后**统一调 `SetItemIcon(name, 0, sr, 0, 0, scaleMul=StartSize)` → 原始像素尺寸 × StartSize @PPU=100（箭 30px×1.25 → 0.375 单位，与 DSP 通道 0.3125 一致）。⚠️换图必须放最后调：GetSprite 是同步回调，若 ShowSprite 分支内先换图、StartSize 分支后再设 localScale 会互相覆盖。

不受影响、勿乱改的通道：

- **DSP 弹道批量渲染**（visual_name 通道，AttackModeInstanceRenderer）：尺寸 = 内置 Quad(1×1) × 桶材质 `_VertexScale` × 逐实例 visualScale(StartSize) × `_VertexScaleXY` 宽高比修正（像素 rect 归一化），全程与 PPU 无关。
- **UI Image**：尺寸由 RectTransform 决定，与 sprite PPU 无关（未用 SetNativeSize）。
- 生物身上装备显示走 spine 皮肤，不是 sprite 图标。

相关：[[reference_colored_icons]]（道具图标像素规范）；攻击模式双通道细节见 attack-mode-system skill「per-instance 视觉参数」条目。
