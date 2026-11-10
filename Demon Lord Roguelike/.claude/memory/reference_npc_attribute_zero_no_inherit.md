---
name: reference-npc-attribute-zero-no-inherit
description: NPC 的 HP/DR/ATK/ASPD/MSPD/MP 填 0 即使用 0，索敌范围和目标阵营填 0 时继承职业
metadata:
  type: reference
---

`CreatureBean.GetAttribute` 对 HP/DR/ATK/ASPD/MSPD/MP 采用 `npcInfo != null ? npcInfo.X : creatureInfo.X`，NPC 行存在且填 0 就使用 0。`AICreatureEntity.GetSearchParameters` 单独读取 `attack_search_range` 与 `attack_search_creature_fight_type`，这两项为空或 0 时继承职业配置。

**Why:** 2026-09-15 骷髅召唤师任务中，最初按"0=继承"理解给召唤物 NPC 行留了 0，被 Plan 代理读码推翻：若照此配置，召唤物会 0 攻 0 移速站桩。现有敌人 NPC 行（如 1010010001）全部显式重复填满生物属性，正是这个语义的佐证。初始赠送 NPC 1/2/3 属性为 0 是因为它们走 dicFixedAttribute 固定属性创建路径，不吃 NPC 字段。

**How to apply:** 给敌人或战斗单位新增 NPC 行时，HP/DR/ATK/ASPD/MSPD/MP 显式填目标值。索敌范围和目标阵营需与职业值不同时，用 NPC 同名配置列覆盖。相关：[[reference-creature-search-fight-type]]、[[feedback-excel-id-sorted-insert]]
