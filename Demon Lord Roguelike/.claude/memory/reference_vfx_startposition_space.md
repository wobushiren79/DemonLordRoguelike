---
name: reference-vfx-startposition-space
description: VFX 图中链接 StartPosition 注入的 Position 块输入空间必须标 World(m_Space:1)，否则与 C# 世界注入叠加成双倍偏移
metadata:
  type: reference
---

# VFX 图 StartPosition 空间约定（2026-08-30 排查定案）

**规则**：任何通过 `effect_info` 的 `vector3_data = 'StartPosition:{StartPosition}'` 注入位置的特效，VFX 图内消费 StartPosition 的 **Position 块（`attribute: position`，guid a971fa2e110a0ac42ac1d8dae408704b）输入槽必须标 World（槽块 `m_MasterData.m_Space: 1`）**。

## 根因（生物1003火/1004冰球击中特效位置偏移）
- C# 链路：`BaseAttackMode.PlayEffectForHit` → `EffectHandler.ShowEnduringSingletonEffect` 做两件事：①特效实例 `transform.position = 命中点`（EffectHandler.cs:62）；②把命中点**世界坐标**经 `{StartPosition}` 注入 VFX 暴露参数（EffectHandler.cs:114-118，配置解析 EffectInfoBeanPartial.cs:76-99）。
- VFX_Boom_1/VFX_Boom_2 图里 StartPosition 直连 Position 块输入槽，**该槽空间是 Local（m_Space: 0）** → 粒子位置 = transform 局部坐标 = `transform.position + StartPosition` = **2×命中点**（Local 空间下世界注入值被当局部值再加一次 transform）。命中点离原点越远偏移越大。
- 对照：正常工作的 VFX_Slash_1（刀锋 400001/400002）/ VFX_Buff_1（加血 500001/500002）Position 槽均为 `m_Space: 1`（World）——注入世界坐标由 VFX 图按世界空间消费，正确。VFX_Explosion_1（300001，爆炸）原为 Local，同病（已一并修复）。

## 修复手法与判定
- 文本改 .vfx YAML：仅把"链接 StartPosition 的 position 槽块"内 `m_MasterData.m_Space: 0 → 1`；**不要动**未链接 StartPosition 的 position 槽（如 VFX_Boom_1 内 5476 块，它保持 0=局部原点，恰好=特效 transform 处）。
- 判定 slot 是否需改：它的 `m_LinkedSlots` 引用的输出槽 id 必须属于 `m_ExposedName: StartPosition` 参数容器。
- **VFX 图的 Position 块链接语义**：Boom 图还有 2 处 StartPosition 使用点（shape 球心 `arcSphere.transform.position`，m_Space: -1）——暂未改（被 position 块 Composition=Overwrite 覆盖/或属于旧实验结构）；若 Play 验证时发现"主体爆炸正确但仍有一圈粒子在 2× 位置"，需把 shape 球心槽也标 World 或从图中移除该链接。

## 代码侧补充（2026-08-30 同日第二改）
`EffectHandler.ShowEnduringSingletonEffect`：**带 VFX 组件的单例粒子一律不设置实例坐标**（`GetVisualEffect() == null` 才移动 transform 到 targetPos）——VFX 粒子位置由图自身逻辑/`{StartPosition}` 世界注入驱动，单例容器保持自身位置（"VFX 全局单例粒子位置不随播放位置变化"）；纯 PS 粒子仍靠 transform 定位。⚠️已知受影响：600001 魅惑=VFX_Buff_2 图无 StartPosition 注入、原靠 transform 移动，自此固定不跟随命中点——待图内补注入或另议。相关 agent 文档 system-effect.md 已同步。

## 验证状态
2026-08-30 已改 7 处（VFX_Boom_1 ×1、VFX_Boom_2 ×1、VFX_Explosion_1 ×5），UI 还原为 Unity 编辑器 VFX Graphs 内把 Position 块输入槽空间切到 World 即可（MCP 无法改 VFX 图内部 slot）。等用户 Play 验证（火/冰球命中点爆炸、爆炸落点、刀锋/加血回归）。
