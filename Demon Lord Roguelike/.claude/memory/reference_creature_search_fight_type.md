---
name: reference-creature-search-fight-type
description: 索敌阵营参数优先读 NpcInfo，空或 0 时继承 CreatureInfo，同路回复通过阵营校验后选实际友军
metadata:
  type: reference
---

`AICreatureEntity.GetSearchParameters` 为所有搜索类型优先读取 `NpcInfo.attack_search_creature_fight_type` 的非零值，无 NPC 配置或该列为空/0 时继承 `CreatureInfo` 同名配置。普通搜索与指定目标复查共用此参数。`FindCreatureEntity` 接受 1（`FightDefense`）和 2（`FightAttack`），99（`FightDefenseCore`）等其他值返回空。

玩家职业（如 2001 骷髅战士和 2002 骷髅投手）默认配置 2。进攻 NPC 复用这些职业时，在 `NpcInfo` 配 1 即可索敌防守方。引用独立敌方职业且职业默认配置已是 1 的 NPC 可留空。玩家初始赠送 NPC 继续继承职业配置。

终焉议会随机议员按 [[reference-doom-council-rating-class-rule]] 复用扭蛋职业，暴力说服通过 NPC 配置索敌防守方。搜索类型仍取职业，搜索范围遵循 NPC 非零优先。无防守目标时沿用普通进攻生物的魔王核心兜底。相关：[[reference-npc-attribute-zero-no-inherit]]

**同路回复**通过索敌阵营校验后，`CreatureSearchType.RoadAllyHPOrDRNoMax` 按施法者实际阵营（`selfCreature.fightCreatureData.creatureFightType`）取同路友军列表。议会愈之魅魔作为进攻方时治疗同路受伤议员，并可在没有其他受伤友军时治疗自己。
