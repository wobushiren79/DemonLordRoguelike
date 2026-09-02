---
name: game-doom-council
description: 终焉议会系统开发：议会实体、投票机制、议会效果（更多水晶/经验/转生/改名/敌人强度/魔物降级·降稀有度/更多装备/更多魔王装备）。
tools: Read, Write, Edit, Glob, Grep, Bash
skill: doom-council-system
watched_files:
  - Assets/Scripts/Game/DoomCouncil/
  - Assets/Scripts/Game/Logic/DoomCouncilLogic.cs
  - Assets/Scripts/Component/UI/Game/DoomCouncil/
  - Assets/Scripts/Component/Game/Scene/ScenePrefabForDoomCouncil.cs
  - Assets/Scripts/Bean/Game/DoomCouncilBean.cs
  - Assets/Scripts/Bean/Game/UserRelationshipBean.cs
  - Assets/Scripts/Bean/MVC/Game/NpcInfoBeanPartial.cs
  - Assets/Scripts/Bean/MVC/Game/DoomCouncilInfoBeanPartial.cs
  - Assets/Scripts/Enums/NpcEnum.cs
---

# 终焉议会 (Doom Council) 开发代理

你负责 [Scripts/Game/DoomCouncil/](Assets/Scripts/Game/DoomCouncil/) 中的终焉议会系统开发。

## 职责范围

### 议会逻辑
- **DoomCouncilLogic** - 终焉议会逻辑，继承 BaseGameLogic
- **GameFightLogicDoomCouncil** - 终焉议会战斗模式

### 议会实体
- **DoomCouncilBaseEntity** - 议会实体基类。效果靠一组触发钩子驱动（非 `ExecuteEffect`）：`TriggerFirst`(首次添加/立即型) · `TriggerGameFightLogicDropAddCrystal` · `TriggerGameFightLogicAddExp` · `TriggerGameFightLogicEndGame`(返回true=效果失效出列) · `TriggerWorldEnterGameForBaseScene` · **`GetEnemyIntensityRate()`**(默认1，返回对下一场敌人 HP/护甲/攻击力的强度倍率)。⚠️ 征服模式的 `GameFightLogicEndGame` 消耗钩子实际由 `GameFightLogicConquer.EndGameAndReturnToBase` 显式分发（`GameFightLogic_EndGame` 事件在征服流程不触发，详见 skill）。
- **DoomCouncilEntityMoreCrystal** - 更多水晶效果
- **DoomCouncilEntityMoreExp** - 更多经验效果
- **DoomCouncilEntityReincarnation** - 转生效果
- **DoomCouncilEntityRename** - 改名效果
- **DoomCouncilEntityEnemyIntensity** - 「挑战更强/更弱的敌人」：`class_entity_data` 存倍率("2"翻倍强/"0.5"减半弱)，`GetEnemyIntensityRate()` 返回该倍率；`TriggerFirst` 返回 false 常驻议案列表，`TriggerGameFightLogicEndGame` 在**征服模式**战斗结束时返回 true 消耗移除。倍率经 `UserTempBean.GetEnemyIntensityRate()`(连乘所有在列议案) 在 `FightBeanForConquer.InitFightAttackData` 叠加到 `intensityRate`，作用于下一整场征服 run 所有关卡+BOSS。
- **DoomCouncilEntityCreatureLevelDown** - 「魔物等级下降/归0」：`class_entity_data` "1"=降1级(下限0)/"0"=归0；`TriggerFirst` 弹 `ShowDialogSelectCreature` 选1只背包魔物（选择窗经 `DialogSelectCreatureBean.filterCreature` 过滤，`level<=0` 已最低的魔物不进列表）→ 降级 + `levelExp`清零 + `creatureAttribute.dicAttributeLevelUp`清空(供重新分配，创建时加成不动) → 存档+Toast(3000006/已0级3000008)；立即型(返true)。
- **DoomCouncilEntityCreatureRarityDown** - 「魔物稀有度下降/归0」：`class_entity_data` "1"=降1级(下限N)/"0"=降到N级；同样弹魔物选择窗（`filterCreature` 过滤 `rarity<=(int)RarityEnum.N` 的魔物），降稀有度并移除 `dicRarityBuff` 中高于新稀有度档位的稀有度BUFF → 存档+Toast(3000007/已N级3000009)；立即型。
- **DoomCouncilEntityMoreEquip** - 「想要更多装备！」：无参数；常驻型，`TriggerGameFightLogicEndGame` 判征服模式结束消耗（输赢皆然）。效果为**查询式消费**：征服通关领奖时 `GameFightLogicConquer.ActionForUIFightSettlementNext` 经 `UserTempBean.HasDoomCouncilMoreEquip()` 检测在列 → `RewardSelectBean.CreateRewardListForConquerAllEquip` 将基础奖励重生成全装备（1保底装备+3魔晶→4件全装备；仅实领、传送门预览不同步）。
- **DoomCouncilEntityMoreDemonLordEquip** - 「想要更多魔王装备！」：同上但全装备且全为魔王专属（`CreateRewardListForConquerAllEquip(conquerInfo, isAllDemonLord: true)`，且该次领奖 `createEquipDemonLordRate=1` 让「奖励多多」追加件也全魔王）；与 MoreEquip 同时在列时**本议案优先**（严格上位），各自独立消耗。查询接口 `UserTempBean.HasDoomCouncilMoreDemonLordEquip()`（与 MoreEquip 查询共同收口私有泛型 `HasDoomCouncilEntity<T>()`）。

### 默认议案与议案展示（现行机制）
- **议案列表非随机**：`UIDoomCouncilBill.InitData` 取 `DoomCouncilInfoCfg.GetAllArrayData()` **全部**行，仅按 `unlock_id` 用 `CheckIsUnlock` 过滤后平铺展示（无「随机抽N个」）。
- **默认议案 = `unlock_id` 留空/0**：`CheckIsUnlock(0)` 恒 true（约定0=无需解锁），故 `unlock_id` 空的议案「默认就有」。「更多水晶/更多经验/敌人更强/敌人更弱」均属此类。
- **需研究解锁的议案**：重命名魔物/魔王(unlock 100200002/3)、转生×7(300x00201)、魔物等级下降/归0(100200005/6)、魔物稀有度下降/归0(100200007/8，对应研究挂在终焉议会设施分支 100200001 下，归0级研究前置为对应的下降1级研究)、想要更多装备！(100200009)、想要更多魔王装备！(100200010，pre=100200009 子研究)，均挂 100200001 下。
- 提交流程 `UIViewDoomCouncilBillItem.OnClickForSubmit`：校验并扣 `cost_crystal`/`cost_reputation`(声望存 `UserDataBean.reputation`) → 二次确认 → `success_rate>=1` 直接 `userTempData.AddDoomCouncil`(不进议会场景)，否则 `GameHandler.StartDoomCouncil` 进投票。**投票计票通过(`StartVote` isPass 分支)或暴力说服战斗胜利(`GameFightLogicDoomCouncil.ActionForUIFightSettlementNext`)时补调 `AddDoomCouncil` 入列生效**（原漏接已修复，此前需投票的议案通过后从不生效）。

### 议员与投票态度系统（核心机制）
- **NPC 类型** `NpcTypeEnum`：`Councilor=2` 议会固定NPC（固定装备/样貌 + 独立持久化好感），`CouncilorRandom=3` 议会随机NPC（随机外貌、每场临时生成）。
- **议员生成**（`DoomCouncilLogic.GenerateCouncilors`）：议会人数在议案 `DoomCouncilInfo.council_num`("min,max") 区间随机；每席随机一种生物(CreatureInfo id 1001-7004) + 按权重随机评级(1~5: 50/30/15/10/5 归一化, `NpcInfoCfg.GetRandomCouncilorNpc`)；整场 10% 概率出现 1 名固定NPC(`GetRandomFixedCouncilorNpc`)。每生物×评级各一条 `npc_type=3` 的 NpcInfo 行(共30×5=150)。**地区限制**：随机/固定抽取及测试分流均先经 `NpcInfoCfg.FilterByCurrentRegion` 过滤（`NpcInfo.region` 列，空=不限语言；`cn` 或 `cn,en` 等语言代码=仅这些语言下出现，判定 `NpcInfoBean.IsMatchCurrentRegion()`；目前仅议员生成使用）。**测试分流**：`DoomCouncilBean.isTestAllFixedCouncilor=true` 时改走 `GenerateAllFixedCouncilors`——把地区过滤后的所有 `npc_type=2` 固定议员各生成1名(跳过随机)，入口 `LauncherTest.StartForDoomCouncilAllFixed`(编辑器"查看所有固定议员"按钮)。**随机议员稀有度**：150 条 `npc_type=3` 行的 `rarity` 列已按评级填充——评级1-2→N、3-4→R、5→SR（创建时写入 `CreatureBean.rarity`，仅设置稀有度不授稀有度BUFF）。
- **随机议员装备**（NpcInfo.equip_random 列 + CreatureRandomInfo 随机池）：150 条随机议员行已按"物种定**套装池**(model_id×10^7+2=10000002~70000002，已全线从散件池 x0000001 段切换)、评级定稀有度(1:N 2:N,R 3:R 4:R,SR 5:SR,SSR)"填充 `equip_random`(格式 `池ID,稀有度...` 重复写加权)。池按 `random_type` 分两种：散件池(type1, `equip_random_data`=ItemsInfo id区间串)、套装池(type2, =EquipSuitInfo 套装id区间串; 套装表 excel_equip_suit_info 一行=一套手动精配7槽位, id段200001起)。`CreatureBean.SetData(npcInfo)` → `InitRandomEquip` 按池类型分支：散件池按 ItemType 分组、每槽经 `CanEquipItem` 过滤后「空+可装备道具」等概率抽1(裸体率=1/(可装备数+1))、稀有度每件独立抽；套装池走 `InitRandomEquipForSuit`——筛整套可装备套装(`EquipSuitInfoBean.CanEquipFor`)等概率整套抽1、稀有度整套统一roll一次。两者均只填空槽(固定 equip_item_ids 优先)、走 `EquipUtil.CreateEquipItemForNpc`(普通使用者,加点按稀有度配置)。随机议员基础属性全0，装备属性是暴力说服战主要属性来源。
- **暴力说服快照传递**：`FightBeanForDoomCouncil` 将 `listCouncilor` 放入 `FightAttackDetailsBean.creatureSnapshots`(与 npcIds 按下标对应)，`CreatureHandler.CreateAttackCreature(..., creatureSnapshot)` 快照非空时直接用该 CreatureBean 创建——战斗中议员与议会场景**同一只**(同皮肤/同装备/同属性)，不再按 npcId 重建重抽。
- **投票态度**（存于 `DoomCouncilBean.dicCouncilorAttitude`，Key=议员UUID，Value 0~100=投赞成概率；态度只与本场议案绑定，不放 CreatureBean、不入存档）：`GenerateCouncilorAttitudes` 按议案 `success_rate` 生成——**高态度(赞成)组人数=总数×通过率→{75,100}**，其余低态度组→{0,25}，再随机取10%覆盖为50（通过率越高→越多议员倾向赞成，与通过率正相关）。固定NPC再叠加好感修正 `(关系类型-3)×50`(仇恨-100/冷淡-50/中立0/友好+50/迷恋+100)。
- **投票**（`StartVote`）：开始即调用 `scenePrefab.HideAllCouncilorAttitudeView()` 隐藏所有议员意愿(Success)图标；随后每名议员 `Random(0,100) < attitude ? 赞成 : 反对`，票数按评级 `DoomCouncilRatingsInfo.vote`（已移除旧的「随机 vs success_rate + 30%睡觉」逻辑）。
- **贿赂**（`UIGameConversation.ActionForItemSelectGift`）：送礼一次态度+10%；固定NPC额外加好感(按道具稀有度 `RarityInfo.item_add_relationship`)并持久化到 `UserRelationshipBean`，随即 `RefreshCouncilorView`。对话界面/台词/文本动画归 [game-conversation](.claude/agents/game-conversation.md)（conversation-system skill）。
- **场景显示**（`ScenePrefabForDoomCouncil`）：议员预制下 `Success` SpriteRenderer 用颜色表态度/意愿(0红/50白/100绿 `GetAttitudeColor`)，自由活动阶段可见、投票开始时由 `HideAllCouncilorAttitudeView` 隐藏；`Relationship` SpriteRenderer 显示固定NPC好感图标。固定NPC：Relationship.x=-0.1、Success.x=0.1；随机NPC：隐藏 Relationship、Success.x=0。
- **好感持久化**：`UserRelationshipBean`（按 npcId 存好感，默认0=仇恨）作为独立存档 `UserRelationship_{slot}`，经 `UserDataService` Load/Save/Delete 注入落盘。

### 议会数据
- **DoomCouncilBean** - 终焉议会配置数据

### 议会 UI
- **UIDoomCouncilMain** - 议会场景主界面（议会进行中替换 UIBaseMain；通过 `ui_SuccessText` 显示「当前议案通过率」，文案 UIText id `53014`，`MathUtil.GetPercentage(success_rate,2)` + `string.Format`；并挂与 UIBaseMain 同款按键提示组 `ui_UIViewPressControlForGameBase`，议会场景走同一 `ControlForGameBase`，E/Space 显隐逻辑自动生效）
- **UIDoomCouncilBill** - 终焉议会议案选择界面
- **UIDoomCouncilVote** - 终焉议会投票界面
- **UIDoomCouncilVoteEnd** - 终焉议会结算界面
- **UIPopupDoomCouncilBillDetails** - 终焉议会详情气泡

### 关键文件

| 文件 | 路径 |
|------|------|
| 议会逻辑(议员生成/态度/投票) | Assets/Scripts/Game/Logic/DoomCouncilLogic.cs |
| 议会实体基类(触发钩子+GetEnemyIntensityRate) | Assets/Scripts/Game/DoomCouncil/DoomCouncilBaseEntity.cs |
| 敌人更强/更弱议案实体 | Assets/Scripts/Game/DoomCouncil/DoomCouncilEntityEnemyIntensity.cs |
| 魔物等级下降/归0议案实体 | Assets/Scripts/Game/DoomCouncil/DoomCouncilEntityCreatureLevelDown.cs |
| 魔物稀有度下降/归0议案实体 | Assets/Scripts/Game/DoomCouncil/DoomCouncilEntityCreatureRarityDown.cs |
| 想要更多装备议案实体 | Assets/Scripts/Game/DoomCouncil/DoomCouncilEntityMoreEquip.cs |
| 想要更多魔王装备议案实体 | Assets/Scripts/Game/DoomCouncil/DoomCouncilEntityMoreDemonLordEquip.cs |
| 议案效果暂存与触发分发(含 GetEnemyIntensityRate 连乘) | Assets/Scripts/Bean/Game/UserTempBean.cs |
| 敌人强度倍率注入点(intensityRate) | Assets/Scripts/Bean/Game/FightBeanForConquer.cs (InitFightAttackData) |
| 议会 Bean | Assets/Scripts/Bean/Game/DoomCouncilBean.cs |
| 议会场景预制(议员/态度色/好感图标) | Assets/Scripts/Component/Game/Scene/ScenePrefabForDoomCouncil.cs |
| 议员态度/类型辅助 | Assets/Scripts/Bean/Game/CreatureBeanPartial.cs |
| 随机/固定议员抽取、地区过滤(region→FilterByCurrentRegion/IsMatchCurrentRegion)、NPC体型解析(body_size→GetBodySizeRandomScale) | Assets/Scripts/Bean/MVC/Game/NpcInfoBeanPartial.cs |
| 议会人数解析(council_num) | Assets/Scripts/Bean/MVC/Game/DoomCouncilInfoBeanPartial.cs |
| 固定NPC好感存档 | Assets/Scripts/Bean/Game/UserRelationshipBean.cs |
| 贿赂(态度/好感)（对话界面归 game-conversation） | Assets/Scripts/Component/UI/Game/GameConversation/UIGameConversation.cs |
| NPC枚举(类型/关系/投票) | Assets/Scripts/Enums/NpcEnum.cs |
| 议会主界面 | Assets/Scripts/Component/UI/Game/DoomCouncil/UIDoomCouncilMain.cs |
| 议会 UI | Assets/Scripts/Component/UI/Game/DoomCouncil/ |

## 约束

- 新增议会效果继承 DoomCouncilBaseEntity
- 议会逻辑通过事件与战斗系统通信
- 议会 UI 使用 UIDoomCouncil 前缀命名

## 关联 Skill

详细开发指南请参考: [doom-council-system](../skills/doom-council-system/SKILL.md)
