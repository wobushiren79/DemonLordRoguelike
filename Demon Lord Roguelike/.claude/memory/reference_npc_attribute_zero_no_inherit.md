---
name: reference-npc-attribute-zero-no-inherit
description: NPC 表属性填 0 ≠ 继承生物值——GetAttribute 对 HP/DR/ATK/ASPD/MSPD/MP 逐字使用 NPC 值（0 就是 0），仅 attack_search_range 有 0→继承回退；敌人 NPC 行属性必须显式填满
metadata:
  type: reference
---

`CreatureBean.GetAttribute`（CreatureBean.cs:703-719）对 HP/DR/ATK/ASPD/MSPD/MP 的逻辑是 `npcInfo != null ? npcInfo.X : creatureInfo.X`——**NPC 行存在即逐字使用，填 0 就是 0，不存在"0=继承生物值"的回退**。唯一例外是 `attack_search_range`：`AICreatureEntity.cs:86` 有 `(npcInfo != null && npcInfo.attack_search_range != 0) ? npcInfo : creatureInfo` 判断，填 0 才真继承。

**Why:** 2026-09-15 骷髅召唤师任务中，最初按"0=继承"理解给召唤物 NPC 行留了 0，被 Plan 代理读码推翻：若照此配置，召唤物会 0 攻 0 移速站桩。现有敌人 NPC 行（如 1010010001）全部显式重复填满生物属性，正是这个语义的佐证。初始赠送 NPC 1/2/3 属性为 0 是因为它们走 dicFixedAttribute 固定属性创建路径，不吃 NPC 字段。

**How to apply:** 给敌人/战斗单位新增 NPC 行时，HP/DR/ATK/ASPD/MSPD/MP 必须显式填目标值（想"继承"就把生物值抄一遍）；只有 attack_search_range 可以放心填 0 继承。改 AI 索敌范围时优先用 NPC 行覆盖而非改生物行。相关：[[reference-creature-search-fight-type]]、[[feedback-excel-id-sorted-insert]]
