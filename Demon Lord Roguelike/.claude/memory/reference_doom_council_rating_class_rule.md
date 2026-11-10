---
name: reference-doom-council-rating-class-rule
description: 随机议员模板按现有扭蛋职业×评级1~5全展开，每个(creature_id,councilor_ratings)唯一；固定议员例外，普攻继承职业，HP/DR/ATK按职业基础值×评级
metadata:
  type: reference
---

# 随机议员「职业 × 评级」模板规则

## 长期不变量

随机议员（`NpcTypeEnum.CouncilorRandom = 3`）的 `NpcInfo` 模板按“**现有扭蛋职业 × 议员评级1~5**”全展开；按物种观察，等价于“**物种 × 该物种现有全部扭蛋职业 × 评级1~5**”。每个 `(creature_id, councilor_ratings)` 组合必须恰好存在一条 `npc_type=3` 行。

- 职业由 `creature_id` 决定，评级由 `councilor_ratings` 决定；**评级不决定职业，同一物种也不只绑定一个代表职业**。同一职业的五个评级共用 `creature_id` 是预期结构，不是错绑。
- 例如人类评级1不是一条，而是 `(1001,1)`、`(1002,1)`、`(1003,1)`、`(1004,1)` 四条；人类完整矩阵为4职业×5评级=20条。
- 复用现有扭蛋职业，**不为议员另建 `CreatureInfo` 职业，也不另设“评级→职业/攻击方式”映射**。
- 后续新增一个扭蛋职业，必须同步补齐该职业评级1~5的五条随机议员模板；删除职业则同步删除其全部评级行。修改职业绑定或评级时也必须保持全组合与组合唯一，不能只增删某一档。
- 具体职业ID、职业池和当前总数以 Excel/扭蛋配置为真实源，本记忆不维护另一份职业映射表。

## 攻击继承与抽取边界

- 暴力说服沿用议会议员快照，未指定额外攻击时，普攻读取快照中 `creatureInfo.attack_mode`，沿用 `creature_id` 的扭蛋职业。议员的 `NpcInfo.attack_search_creature_fight_type=1` 指向防守方，`attack_search_range=0` 时继承职业的索敌范围。
- **现阶段基础属性与评级倍率规则**：随机议员的 `HP/DR/ATK = 对应职业 CreatureInfo 基础值 × councilor_ratings`，1~5 阶分别为 1~5 倍。`MP/ASPD/MSPD` 沿用职业基础值。后续调整职业基础属性时，按该公式同步此职业全部 5 条随机议员模板。倍率直接写在配置值里，NPC 基础属性填 0 即使用 0（见 [[reference-npc-attribute-zero-no-inherit]]）。装备、评级稀有度等按议员自身规则叠加。固定议员独立手配。
- `GetRandomCouncilorNpc` 正常先抽职业，再按权重抽评级，并精确查找 `(creature_id,rating)`；缺组合时回退到同职业任意评级只是运行时容错，**不能作为配置缺行的依据**。
- 本规则只约束随机议员模板。固定议员（`npc_type=2`）是独立手配角色，可有评级6，不要求按物种或评级展开，也不参与随机议员模板计数。
- 地区过滤等运行时筛选只决定当前可用模板，不改变全组合配置要求。

## 当前状态快照（2026-11-02）

当前有29个扭蛋职业，随机议员为29×5=145条，145个 `(creature_id,councilor_ratings)` 组合均唯一；Excel 与导出 JSON 的职业×评级矩阵组合已核对一致。2026-11-02已在 Excel 源表按“职业基础值×评级”写入 `HP/DR/ATK`，其余三项基础属性保持职业值。职业数量变化后更新本快照，不改变上述长期规则。

相关：[[reference-creature-search-fight-type]]、[[reference-npc-attribute-zero-no-inherit]]
