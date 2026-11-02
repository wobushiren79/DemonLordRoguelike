---
name: reference-creature-search-fight-type
description: 生物索敌阵营只读 attack_search_creature_fight_type 配置、与实际所在阵营无关——玩家生物（值=2 猎杀进攻方）直接改作敌人会索敌自己人打友军；正确做法=克隆生物行改值=1（先例 103106/103107）
metadata:
  type: reference
---

`AICreatureEntity.FindCreatureEntity`（AICreatureEntity.cs:89）索敌阵营 `searchCreatureFightType = creatureInfo.GetAttackSearchCreatureFightType()`——**只读生物配置，与生物实际所在阵营（FightAttack/FightDefense）无关**。玩家卡生物（如 2001 骷髅战士/2002 骷髅投手）该值=2（FightAttack，猎杀进攻方），是防守方设计；若作为进攻方生成（召唤物/暴力说服快照），AI 会搜 `LayerInfo.CreatureAtt`（**自己人**）——近战 AttackModeMelee 对 AI 传入的 attacked 直接 UnderAttack 不过层级，会真打友军。

**Why:** 2026-09-15 骷髅召唤师任务发现：全库所有敌人生物（10xxxx）都是独立行、配 attack_search_creature_fight_type=1，没有任何「敌方 NPC 复用玩家生物」的先例可抄。

**How to apply:** 一般把玩家生物改作敌人（或反之）时，**克隆生物行**（先例：103106/103107 克隆 2001/2002）只改 `attack_search_creature_fight_type`（敌人=1 索防守方）+ `creature_type=2` + 清 CMP/RCD/unlock_id/creature_random_id/details，皮肤/攻击方式/动画照抄，NPC 行改引用克隆生物 id，不要直接让一般敌人 NPC 引用玩家生物 id。

**随机议员例外：** 终焉议会随机议员按 [[reference-doom-council-rating-class-rule]] 必须直接复用现有扭蛋职业，不能套用上述克隆建议擅自改绑或破坏“职业×评级”模板。其「暴力说服」快照（玩家生物配置=2 当敌人用）仍存在同类索敌阵营隐患；如需修复，应单独调整 NPC 驱动生物的索敌阵营推导并回归测试，而不是违背议员职业复用规则。相关：[[reference-npc-attribute-zero-no-inherit]]
