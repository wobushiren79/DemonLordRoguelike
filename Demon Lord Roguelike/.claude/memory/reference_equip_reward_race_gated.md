---
name: reference_equip_reward_race_gated
description: 通关奖励装备池=种族开关(EquipRewardXxx)门控+通用装备再经「已解锁可孕育职业可穿」过滤(IsEquipUsableByAnyCreature)；魔王专属只按形态过滤
metadata:
  type: reference
---

通关奖励装备池是**两层门控**（2026-10-08 起，推翻 2026-07「纯种族级门控」旧决议）：

**第一层：种族级开关（不变）**
- 奖励装备池 `RewardSelectBean.GetUnlockCreatureModelIdsForEquip()`(`RewardSelectBean.cs`) 遍历 `CreatureModelCfg`(种族外观模型，非部件表 CreatureModelInfoCfg)，用 **model.unlock_id** 判解锁；再按 `creature_model_id` 取该种族的道具。
- 每个种族模型的 `unlock_id` = 该种族的**装备奖励研究开关** `UnlockEnum.EquipRewardXxx`（`GameStateEnum.cs`）：Human=300100301 / Skeleton=300200301 / Slime=300300301 / Succubus=300400301 / Minotaur=300500301 / Goblin=300600301 / Orc=300700301。
- 种族武器道具统统绑种族模型（如骷髅系 `20xxxxx` 段绑 model 2），**数据层不区分职业**——所谓"骷髅魔法师的武器"只是"骷髅种族武器池"里的一件。

**第二层：已解锁可孕育职业可用性过滤（2026-10 新增，仅通用装备）**
- `CreateItemEquipCore` 过滤循环对**非魔王专属**装备追加 `IsEquipUsableByAnyCreature(listUnlockGashaponCreature, itemInfo)`：道具须至少能被一个「已解锁可孕育生物」`CanEquipItem`（装备类型+种族模组+武器类型三重匹配，见 `CreatureInfoBeanPartial.CanEquipItem`）才保留，否则剔除（过滤后空=回退魔晶）。
- 判定集 `UserUnlockBean.GetUnlockGashaponCreatureInfos()`：`creature_type==GashaponMachine(1)` 且职业研究 `unlock_id` 已解锁（与孕育池 `UIGashaponMachine.StartGashaponMachine` 同口径；商店配置覆盖全部 type=1 生物故等价）。
- 修复的问题：人类装备开关解锁后掉出**刀盾(11020021/11020022)/大盾(11000015)/长枪(11020016)/双手剑(11000012/11020014)** 等敌方专属武器皮肤——对应生物（持盾战士 101002 等）是 `creature_type=2` 敌人、永远无法孕育，掉了无人能穿。
- 副作用（用户已确认接受）：职业研究未解锁则其装备不掉（没解锁骷髅法师→不掉骷髅法杖）；一个可孕育职业都没解锁时通用装备全回退魔晶；**初始赠送的 3 魔物（骷髅战士×2+骷髅投手）不伴随职业研究解锁**，其装备在解锁对应研究前不掉落。
- **魔王专属不叠加此过滤**（仍只按 `IsEquipTypeMatchForDemonLord` 魔王形态过滤；魔王能穿即不算废物）；取不到解锁数据时不过滤（容错保持旧行为）。

**两套解锁平级独立(仍易混)**——同挂种族根(如骷髅 300200000)下：
- 「可获取骷髅装备」= 研究节点 300200301 = EquipRewardSkeleton，~10 魔晶，开**整个种族**武器掉落池（第一层）。
- 「解锁骷髅魔法师(火/冰)」= 研究节点 300200003/300200004(research_type=3 魔物分支)，各 500 魔晶，解锁**生物职业**（第二层过滤的判定依据）。
- 购买互不连带：`UIViewBaseResearchItem.OnClickForPay` → `AddUnlock(researchInfo.unlock_id,...)` 只写节点**自身** unlock_id。

**附带隐患(未处理)**：`GetUnlockCreatureModelIds`(`UserUnlockBean.cs`) 对 unlock_id==0 的 model 一律视为已解锁(`CheckIsUnlock` 约定 0=无需解锁)；CreatureModel 表里 57 个 Mod 模型 unlock_id=0，靠"无对应道具"被 `ContainsKeyForCreatureModelId` 过滤才不掉落——若某 Mod 模型日后配了道具，还需过第二层过滤（无可孕育生物能穿则剔除），双保险。相关：[[reference_portal_reward_pregen_research_gate]]。
