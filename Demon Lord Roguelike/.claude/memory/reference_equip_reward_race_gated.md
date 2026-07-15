---
name: reference_equip_reward_race_gated
description: 传送门/征服装备奖励是"种族级"门控(EquipRewardXxx开关)，与解锁具体生物职业平级独立解耦，勿误判为解锁bug
metadata:
  type: reference
---

传送门/征服奖励里的**装备道具是「种族级」门控，不是「具体生物级」**。经常被误报为"没解锁某生物却掉了它的装备"的 bug，实为设计如此。

**数据链**
- 奖励装备池 `RewardSelectBean.GetUnlockCreatureModelIdsForEquip()`(`RewardSelectBean.cs`) 遍历 `CreatureModelCfg`(种族外观模型，非部件表 CreatureModelInfoCfg)，用 **model.unlock_id** 判解锁；再按 `creature_model_id` 取该种族的道具。
- 每个种族模型的 `unlock_id` = 该种族的**装备奖励研究开关** `UnlockEnum.EquipRewardXxx`（`GameStateEnum.cs`）：Human=300100301 / Skeleton=300200301 / Slime=300300301 / Succubus=300400301 / Minotaur=300500301 / Goblin=300600301 / Orc=300700301。
- 骷髅系全部武器道具(`20xxxxx` 段)统统绑 model 2，**数据层不区分骷髅战士/投手/魔法师**——所谓"骷髅魔法师的武器"只是"骷髅种族武器池"里的一件。

**两套解锁平级独立(易混)**——同挂种族根(如骷髅 300200000)下：
- 「可获取骷髅装备」= 研究节点 300200301 = EquipRewardSkeleton，~10 魔晶，开**整个种族**武器掉落池。
- 「解锁骷髅魔法师(火/冰)」= 研究节点 300200003/300200004(research_type=3 魔物分支)，各 500 魔晶，只解锁**生物职业**本身。
- 购买互不连带：`UIViewBaseResearchItem.OnClickForPay` → `AddUnlock(researchInfo.unlock_id,...)` 只写节点**自身** unlock_id。故只买便宜的装备开关就会掉全套种族武器，哪怕没解锁对应生物——非 bug。

**术语校准**：区分「解锁生物职业」(300200003 等) vs 「种族装备奖励开关」(EquipRewardXxx=3002003_01)，两者解耦。2026-07 与用户确认后**维持现状**(种族级门控)，不改为按具体生物门控。

**附带隐患(未处理)**：`GetUnlockCreatureModelIds`(`UserUnlockBean.cs`) 对 unlock_id==0 的 model 一律视为已解锁(`CheckIsUnlock` 约定 0=无需解锁)；CreatureModel 表里 57 个 Mod 模型 unlock_id=0，靠"无对应道具"被 `ContainsKeyForCreatureModelId` 过滤才不掉落——若某 Mod 模型日后配了道具会被无条件掉落。相关：[[reference_portal_reward_pregen_research_gate]]。
