---
name: doom-council-system
description: Demon Lord Roguelike 游戏的终焉议会(DoomCouncil)系统开发指南。使用此SKILL当需要创建或修改终焉议会逻辑、议会实体效果、议会投票机制、议会UI等，包括DoomCouncilLogic、DoomCouncilBaseEntity、议会战斗模式(GameFightLogicDoomCouncil)、议会UI(UIDoomCouncilBill/Vote/VoteEnd)等。
watched_files:
  - Assets/Scripts/Game/DoomCouncil/
  - Assets/Scripts/Game/Logic/GameFightLogicDoomCouncil.cs
  - Assets/Scripts/Game/Logic/DoomCouncilLogic.cs
  - Assets/Scripts/Bean/Game/DoomCouncilBean.cs
  - Assets/Scripts/Bean/Game/FightBeanForDoomCouncil.cs
  - Assets/Scripts/Bean/Game/UserRelationshipBean.cs
  - Assets/Scripts/Bean/Game/CreatureBeanPartial.cs
  - Assets/Scripts/Bean/MVC/Game/NpcInfoBeanPartial.cs
  - Assets/Scripts/Bean/MVC/Game/DoomCouncilInfoBeanPartial.cs
  - Assets/Scripts/Enums/NpcEnum.cs
  - Assets/Scripts/Component/Game/Scene/ScenePrefabForDoomCouncil.cs
  - Assets/Scripts/Component/UI/Game/DoomCouncil/
  - Assets/Scripts/Component/UI/Popup/UIPopupDoomCouncilBillDetails.cs
---

# 终焉议会系统开发指南

> ⚠️ 部分历史小节（如「生成3个随机提案」「GenerateProposals」「UIDoomCouncilMain 旧描述」）与现行代码已有出入；**议员/投票/好感机制以下方「## 议员与投票态度系统（现行机制）」为准**。

## 议员与投票态度系统（现行机制）

### NPC 类型（`NpcTypeEnum`，`Assets/Scripts/Enums/NpcEnum.cs`）
- `Councilor = 2` **议会固定NPC**：固定装备/样貌；拥有按 npcId **持久化**的好感系统（默认仇恨）。
- `CouncilorRandom = 3` **议会随机NPC**：随机外貌（`creature_random_id`）；每场议会临时随机生成。
- 配置表 `NpcInfo` 为每种生物(CreatureInfo id 1001-7004，共30种)×评级1~5 各建一条 `npc_type=3` 行（30×5=150）；固定NPC为 `npc_type=2` 行。

### 议员生成（`DoomCouncilLogic.GenerateCouncilors`）
1. 议会人数：议案 `DoomCouncilInfo.council_num`（字符串 `"min,max"`）→ `GetRandomCouncilNum()` 区间随机。
2. 每席：随机一种生物 + 按权重随机评级（1~5 权重 50/30/15/10/5 归一化，`NpcInfoCfg.GetRandomCouncilorNpc()`）。
3. 整场 10% 概率出现 1 名固定NPC（`GetRandomFixedCouncilorNpc()`）。
4. **地区限制（region）**：两个抽取入口（随机/固定）及测试分流均先经 `NpcInfoCfg.FilterByCurrentRegion` 过滤——`NpcInfo.region` 列为空=不限语言；配置语言代码（如 `cn` 或 `cn,en`，代码见 `LanguageEnum`/`LanguageCfg.currentLanguage`）时仅当前语言在列表内才允许出现（判定 `NpcInfoBean.IsMatchCurrentRegion()`，逗号分隔多值、忽略大小写）。**目前仅议员生成使用此过滤**，初始创建/商店等其它 NPC 路径不受影响。
5. 议员显示名由 `CreatureBean.SetCouncilorDisplayName` 按 NPC 类型分流：**随机议员**走通用命名 `NpcInfoBean.GetCouncilorRandomDisplayName()`（评级称谓名：预备/列席/初级议员等，`DoomCouncilRatingsInfo.name`；评级配置缺失兜底多语言文本 UIText id=53016"随机议员"。**随机议员命名唯一入口**，测试工具等所有场景统一调它）；**固定议员(`NpcTypeEnum.Councilor`)**取其自身 NPC 名字（`NpcInfo.name`→`name_language`）。该显示名同时驱动对话弹窗(`UIGameConversation`)与详情面板(`UIViewCreatureCardDetails`)。固定NPC从 `UserRelationshipBean` 载入持久化好感。
6. **测试分流**：当 `DoomCouncilBean.isTestAllFixedCouncilor=true` 时，`GenerateCouncilors` 顶部直接返回 `GenerateAllFixedCouncilors()`——跳过随机人数/随机议员，把 `NpcInfoCfg.GetNpcInfosByType(NpcTypeEnum.Councilor)` 经地区过滤后的全部固定议员各生成 1 名（同样走上述显示名分流 + 载入持久化好感）。仅供测试查看所有固定议员，入口 `LauncherTest.StartForDoomCouncilAllFixed`。

### 随机议员装备（NpcInfo.equip_random + 装备随机池/套装池）
- **配置链路（Excel 唯一真实源）**：`NpcInfo.equip_random` 列（格式 `装备池ID,稀有度1,稀有度2...`，如 `10000001,N,R`；稀有度按 `RarityEnum` 枚举名、多个等概率抽、**重复写可加权**；空=不随机）→ 引用 `CreatureRandomInfo` 的随机池行（`random_type=0` 皮肤池存 `skin_random_data`；`random_type=1` **散件池** `equip_random_data` 存 ItemsInfo 道具ID区间串，7 个物种池 id `10000001~70000001`=model_id×10^7+1；`random_type=2` **套装池** `equip_random_data` 改存 EquipSuitInfo 套装ID区间串，7 个物种池 id `10000002~70000002`=model_id×10^7+2）。套装表 `excel_equip_suit_info[装备套装]`（EquipSuitInfo）一行=一套手动精配（`creature_model_id` 物种0=通用 + `hat/clothes/pants/shoe/nose_ring/finger_ring/weapon` 7 槽位列0=空 + `remark` 套装名；套装id段 200001 起），解决散件随机衣裤不搭的问题。
- **随机议员按评级配稀有度**（150 条 npc_type=3 行已批量填充）：评级1→`N`、2→`N,R`、3→`R`、4→`R,SR`、5→`SR,SSR`；池按生物 model_id 对应**物种套装池**（x0000002 段，已全线从散件池切换）。
- **运行时**：`CreatureBean.SetData(NpcInfoBean)` 在 `InitEquip` 后追加 `InitRandomEquip(npcInfo)`，按池类型分支——散件池：池按 ItemType 分组（`GetAllRandomEquipData`），每槽位经 `CreatureInfoBean.CanEquipItem`（部位/种族模组/武器类型）过滤后**在「空+可装备道具」中等概率抽1个**（裸体率=1/(可装备数+1)），稀有度每件独立抽；套装池（`InitRandomEquipForSuit`）：`GetRandomEquipSuit` 筛整套可装备的套装（`EquipSuitInfoBean.CanEquipFor`，任一件穿不上整套剔除）等概率整套抽1套，**稀有度整套统一roll一次**。**固定装备 `equip_item_ids` 优先，随机只填空槽**；每件走 `EquipUtil.CreateEquipItemForNpc`（NPC随机装备场景：userType=0 不出魔王专属、加点按稀有度配置默认取值，底层收口 `CreateEquipItem`）。解析入口 `NpcInfoBean.GetEquipRandomPoolId()/GetEquipRandomRarities()`（带缓存）。该机制对所有 NPC 类型通用（任何 NPC 配了 equip_random 都会随机穿装）。
- **外观即装备皮肤**：`GetSkinData(isNeedEquip:true)` 自动挂装备 `creature_model_info_id` 皮肤（帽隐发/武器替换基础武器皮），议会场景/对话/详情面板零改动生效。注意随机议员 npc_type=3 行基础属性全 0，**装备属性是其暴力说服战的主要属性来源**。
- **暴力说服快照传递**：`FightBeanForDoomCouncil` 把 `doomCouncilData.listCouncilor` 放进 `FightAttackDetailsBean.creatureSnapshots`（与 npcIds 按下标对应）；`CreatureHandler.CreateAttackCreature(npcId..., creatureSnapshot)` 快照非空时直接用该 CreatureBean 创建（`GetFightCreatureData(creatureData,...)`）——**战斗中的议员与议会场景同一只（同皮肤/同装备/同属性）**，不再按 npcId 重建重抽。征服/其它战斗不传快照，行为不变。防御生物收集当前出战阵容（`userData.GetLineupCreature(userData.GetLineupFightIndex())`，与传送门详情弹窗选择的出战阵容同一口径，替代旧写死第 1 套）。
- **装备池编辑**：编辑器「游戏/皮肤随机池配置」(`SkinRandomEditorWindow`) 支持三模式——装备池([装备]前缀)候选取自 ItemsInfo、套装池([套装]前缀)候选取自 EquipSuitInfo 套装表、均按池物种(creature_model_id)过滤；套装内容本身由「游戏/装备套装配置」(`EquipSuitEditorWindow`) 编辑；保存按池类型写回对应列并同步导出 JSON。

### 投票态度（存于 `DoomCouncilBean.dicCouncilorAttitude`，Key=议员UUID，Value 0~100=投赞成概率；只与本场议案绑定，不放 CreatureBean、不入存档）
- `GenerateCouncilorAttitudes(list, success_rate)` 按议案通过率生成：**高态度(赞成)组人数 = 总数×通过率 → 随机 {75,100}**；其余低态度组 → 随机 {0,25}；再随机取全体 10% 覆盖为 50。
- 固定NPC再叠加好感修正 `(关系类型-3)×50`：仇恨-100 / 冷淡-50 / 中立0 / 友好+50 / 迷恋+100。
- ✅ 通过率与议员赞成意愿**正相关**：通过率越高 → 越多议员倾向赞成（如通过率 10% → 约 10% 议员高态度赞成）。此前"通过率越高低态度议员越多"的反向算法为 bug，已修正。

### 投票（`DoomCouncilLogic.StartVote`）
- 投票开始即调用 `scenePrefab.HideAllCouncilorAttitudeView()`：正式投票阶段隐藏所有议员意愿(Success)图标，玩家不再看到赞成意愿。
- 每名议员 `Random(0,100) < attitude ? 赞成 : 反对`；票数按评级 `DoomCouncilRatingsInfo.vote`。
- 旧逻辑（随机值 vs success_rate + 30% 睡觉）已移除。

### 贿赂（`UIGameConversation.ActionForItemSelectGift`）
- 送礼一次：态度 +10%（所有议员）。
- 固定NPC额外：好感 += `RarityInfo.item_add_relationship` 并持久化到 `UserRelationshipBean`，`SaveUserData()`。
- 之后调用 `DoomCouncilLogic.RefreshCouncilorView(uuid)` 刷新显示。
- 对话界面本身（台词配置/文本逐字动画/说话音效/点击跳过）归 **conversation-system** skill。

### 场景显示（`ScenePrefabForDoomCouncil`）
- 议员预制下 `Success` SpriteRenderer：用颜色表态度/意愿（0红/50白/100绿，`GetAttitudeColor`）；自由活动阶段可见，投票开始时由 `HideAllCouncilorAttitudeView()` 统一 `SetActive(false)` 隐藏。
- `Relationship` SpriteRenderer：仅固定NPC显示好感图标（`NpcRelationshipInfo.icon_res`，UI图集）。
- 坐标切换：固定NPC → `Relationship.x=-0.1`、`Success.x=0.1`（并排）；随机NPC → 隐藏 Relationship、`Success.x=0`（居中）。

### 好感持久化（`UserRelationshipBean`）
- `Dictionary<long npcId, int relationship>`，默认0=仇恨；区间映射见 `NpcRelationshipInfo`（仇恨0-100/冷淡101-200/中立201-300/友好301-400/迷恋401-500）。
- 独立存档 `UserRelationship_{slot}`，由 `UserDataService` Load/Save/Delete 注入落盘（参考 UserUnlock/UserAchievement 同款机制）。

### 相关配置表（Excel 为唯一真实源，改后需在 Unity 运行 ExcelEditorWindow 导出）
| 表 | 关键列 |
|----|--------|
| `excel_npc_info` NpcInfo | `npc_type`(2固定/3随机)、`creature_id`、`creature_random_id`、`councilor_ratings`、`name`、`body_size`(体型倍率: 空/0=1倍、"0.9,1.1"=区间随机、"1.1"=固定)、`equip_random`(随机装备: `装备池ID,稀有度...` 如 `10000001,N,R`，空=不随机)、`region`(地区限制: 空=不限语言、`cn` 或 `cn,en` 等语言代码=仅这些语言下出现，仅议员生成生效)、`rarity`(生物稀有度: 空/0=N、按RarityEnum值1=N~6=L；配置后创建NPC生物时写入 CreatureBean.rarity，仅设置稀有度不授稀有度BUFF；150条npc_type=3随机议员行已按评级填充：评级1-2→N、3-4→R、5→SR) |
| `excel_creature_random_info` CreatureRandomInfo | `random_type`(0皮肤池/1装备散件池/2套装池)、`skin_random_data`(皮肤池: CreatureModelInfo id区间串)、`equip_random_data`(散件池: ItemsInfo id区间串 / 套装池: EquipSuitInfo id区间串)、散件池id段 `10000001~70000001` / 套装池id段 `10000002~70000002`(已配给议员行) |
| `excel_equip_suit_info` EquipSuitInfo | `creature_model_id`(物种0=通用)、`hat/clothes/pants/shoe/nose_ring/finger_ring/weapon`(7槽位道具id,0=空)、`remark`(套装名) |
| `excel_doom_council_info` DoomCouncilInfo | `council_num`("min,max" 议会人数)、`success_rate` |
| `excel_doom_council_ratings_info` | `vote`(评级票数)、`name` |
| `excel_npc_relationship_info` | `relationship_min/max`、`relationship_type`、`icon_res` |
| `excel_rarity_info` RarityInfo | `item_add_relationship`(贿赂好感加成) |

---

## 议案(Bill)与效果实体（现行机制，以此为准）

> ⚠️ 下方「## 核心概念 / DoomCouncilLogic / GenerateProposals / DoomCouncilBaseEntity.ExecuteEffect」等历史小节多已过时，**议案展示与效果执行以本节为准**。

### 议案配置 `DoomCouncilInfo`（`excel_doom_council_info` / `DoomCouncilInfo.txt`）
字段：`success_rate`(通过率0~1, `>=1`直接通过不进议会) · `council_num`("min,max") · `cost_reputation`(消耗声望) · `cost_crystal`(消耗魔晶) · `icon_res` · `class_entity_name`(效果实体类名, 反射) · `class_entity_data`(效果参数字符串, 各实体自解析) · `unlock_id`(解锁ID) · `name`/`details`(文本id)。**无独立数值字段，效果参数全编码进 `class_entity_data`**。多语言真实源在 `excel_language` 的 `DoomCouncilInfo` 工作表(id/content_cn/content_en/content_1_cn/content_1_en)。

### 议案展示：全部平铺，非随机
`UIDoomCouncilBill.InitData` 取 `GetAllArrayData()` **全部**行，仅按 `unlock_id` 用 `userUnlock.CheckIsUnlock` 过滤后平铺（**无随机抽N/权重**）。
- **默认议案 = `unlock_id` 留空/0**：`CheckIsUnlock(0)` 恒 true（约定0=无需解锁）→「默认就有」。当前默认议案：更多水晶/更多经验/**挑战更强的敌人**/**挑战更弱的敌人**。
- **需研究解锁的议案**：重命名魔物/魔王(unlock 100200002/3) · 转生×7(unlock 300x00201) · **魔物等级下降/归0(unlock 100200005/6)** · **魔物稀有度下降/归0(unlock 100200007/8)**——后4条对应研究挂终焉议会设施分支(research_type=1) 100200001 下，"归0"研究前置为同族"下降1级"研究(100200006←100200005、100200008←100200007)。**想要更多装备！(unlock 100200009)** · **想要更多魔王装备！(unlock 100200010)**——研究同挂 100200001 下(100200009: pre=100200001、pay_crystal=100、position(-500,-900)；100200010 为其子研究: pre=100200009、pay_crystal=200、position(-700,-900))。

### 提交与效果执行
`UIViewDoomCouncilBillItem.OnClickForSubmit`：校验并扣 `cost_crystal`/`cost_reputation`(声望=`UserDataBean.reputation`, `CheckHasReputation`/`AddReputation`) → 二次确认 → `success_rate>=1` 直接 `userTempData.AddDoomCouncil`，否则 `GameHandler.StartDoomCouncil` 进议员投票。**投票议案的入列路径（原漏接已修复）**：投票计票通过(`DoomCouncilLogic.StartVote` isPass 分支)或未通过后「暴力说服」战斗胜利(`GameFightLogicDoomCouncil.ActionForUIFightSettlementNext`，两路径互斥)均会补调 `AddDoomCouncil`——此前两条路径均未调用，导致需投票的议案通过后从不生效。`UserTempBean.AddDoomCouncil` 反射 `class_entity_name` 建实体；`TriggerFirst()` 返 true=立即型(不入列)，返 false=常驻 `listDoomCouncilEntity`。

### `DoomCouncilBaseEntity` 触发钩子（非 `ExecuteEffect`）
`TriggerFirst` · `TriggerGameFightLogicDropAddCrystal` · `TriggerGameFightLogicAddExp` · `TriggerGameFightLogicEndGame`(返true=出列) · `TriggerWorldEnterGameForBaseScene` · **`GetEnemyIntensityRate()`**(默认1, 返回对下一场敌人 HP/护甲/攻击力的强度倍率)。分发在 `UserTempBean.TriggerDoomCouncil`，由 `GameHandler` 各时机 + `TriggerTypeDoomCouncilEntityEnum` 调用。**查询式消费**（不走事件分发）：`UserTempBean.GetEnemyIntensityRate()` 连乘 · `UserTempBean.HasDoomCouncilMoreEquip()` / `HasDoomCouncilMoreDemonLordEquip()` 检测「想要更多装备/魔王装备」议案在列（内部收口私有泛型 `HasDoomCouncilEntity<T>()`）。

> ⚠️ **`GameFightLogic_EndGame` 事件在征服模式流程中不会触发**（征服通关/失败统一收口于 `GameFightLogicConquer.EndGameAndReturnToBase`，全程不调 `EndGame()`/`ChangeGameState(End)`）。征服模式的 `GameFightLogicEndGame` 消耗钩子由 `EndGameAndReturnToBase`（回基地存档前）**显式调用** `TriggerDoomCouncil(...)` 分发——此刻 `GetGameLogic<GameFightLogic>()` 仍是本场 Conquer 逻辑，各实体判定成立。依赖该时机消耗的实体（更多水晶/经验/敌人强度/更多装备/更多魔王装备）实际都靠这一显式分发生效。

### 敌人更强/更弱议案（`DoomCouncilEntityEnemyIntensity`）
- 两条默认议案共用一个实体类，靠 `class_entity_data` 区分：`"2"`=翻倍强(通过率1/消耗声望1)、`"0.5"`=减半弱(通过率0.01/消耗声望100)。
- `GetEnemyIntensityRate()` 解析 `class_entity_data`(不变区域性)返回倍率；`TriggerFirst` 返 false 常驻；`TriggerGameFightLogicEndGame` 在**征服模式**(`gameFightType==Conquer`)战斗结束时返 true 消耗移除。
- 施加链路：`UserTempBean.GetEnemyIntensityRate()`(连乘所有在列议案) → `FightBeanForConquer.InitFightAttackData` 里 `intensityRate *= ...` → 敌人(含BOSS)生成时 `FightCreatureBean.RefreshBaseAttribute` 对 HP/护甲(DR)/攻击力(ATK) 整体相乘。**作用于下一整场征服 run 所有关卡+BOSS，run 结束消耗**（与「更多水晶/经验」同口径）。

### 魔物等级/稀有度下降议案（`DoomCouncilEntityCreatureLevelDown` / `DoomCouncilEntityCreatureRarityDown`）
- 两类各两条议案，靠 `class_entity_data` 区分：`"1"`=下降1级、`"0"`=归0（等级→0级；稀有度→N级即 `RarityEnum.N=1`）。四条均为 `success_rate=1`（直接通过不投票）、消耗声望1，且需对应研究解锁（unlock 100200005~100200008）。
- 流程（立即型，`TriggerFirst` 返 true）：`CloseAllUI` → `ShowDialogSelectCreature` 选 1 只背包魔物（`listBackpackCreature`，不含魔王）→ 执行降级 → `SaveUserData()` + Toast → `BackDoomCouncilBill`；取消/空选直接 `BackDoomCouncilBill`。
- **选择列表过滤**：经 `DialogSelectCreatureBean.filterCreature` 在弹窗 `SetData` 时筛掉已最低的魔物——等级议案 `creature.level > 0` 才进列表、稀有度议案 `creature.rarity > (int)RarityEnum.N` 才进列表（`UIDialogSelectCreature.SetData` 用 `FindAll` 生成新列表，不动背包原列表）。
- **降级**（LevelDown）：`levelExp` 清零 + `creatureAttribute.dicAttributeLevelUp` 清空（已分配升级属性点供重新分配；`dicAttributeCreate` 创建时加成不动）。
- **降稀有度**（RarityDown）：同步移除 `dicRarityBuff` 中 key 高于新稀有度档位的稀有度BUFF（与 `RandomRarityBuffForCreate` 逐级授予逻辑对称）。
- 边界（兜底）：已最低的魔物正常已被 `filterCreature` 过滤不出现在列表；若目标仍已 0 级 / 已 N 级（如数据异常）则不生效，Toast(3000008/3000009) 提示后返回（声望不退，与重命名取消同口径）。
- Toast 文本（UIText 表）：`3000006` 等级下降成功 · `3000007` 稀有度下降成功 · `3000008` 已是0级 · `3000009` 已是N级。

### 想要更多装备议案（`DoomCouncilEntityMoreEquip`）
- 议案 id `1000500001`：success_rate=0.5(走投票)、cost_reputation=1、unlock_id=100200009（研究「终焉议会议案：想要更多装备！」解锁）、icon_res=ui_icon_1、class_entity_data 空（无参数）。
- 效果：**下一次征服模式通关领奖时，宝箱基础奖励由「1装备+2魔晶」重生成变为「3件全装备」**（稀有度按 `reward_equip_rarity`、魔王专属10%、解锁池、生成不出装备兜底魔晶等规则不变）。
- 常驻型（`TriggerFirst` 返 false）；`TriggerGameFightLogicEndGame` 判 `gameFightType==Conquer` 返 true 消耗——**run 结束即消耗，输赢皆然**（与 EnemyIntensity 同口径）。
- 消费为**查询式**（仿 `GetEnemyIntensityRate`，不加事件枚举）：`GameFightLogicConquer.ActionForUIFightSettlementNext` 取预生成 `baseReward` 后，若 `UserTempBean.HasDoomCouncilMoreEquip()` 则用 `RewardSelectBean.CreateRewardListForConquerAllEquip(fightTypeConquerInfo)`(装备件数=总件数)重生成。**仅影响实领，传送门气泡预览不同步**（预生成冻结奖励不动）。

### 想要更多魔王装备议案（`DoomCouncilEntityMoreDemonLordEquip`）
- 议案 id `1000500002`：success_rate=0.5、cost_reputation=2、unlock_id=100200010（研究为 100200009 的子研究）、其余配置同上。
- 效果：**通关领奖宝箱基础奖励重生成变为「3件全魔王专属装备」**（`CreateRewardListForConquerAllEquip(conquerInfo, isAllDemonLord: true)`，`createEquipDemonLordRate` 拉满）；同时把该次领奖 `rewardSelectData.createEquipDemonLordRate=1`，**深渊馈赠「奖励多多」追加件也全魔王**，保证"全部"。
- **与「想要更多装备」同时在列时本议案优先**（严格上位：全装备且全魔王）；两议案各自独立在征服 EndGame 消耗（输赢皆然）。实体与 MoreEquip 平行独立（不继承，避免 `is T` 子类污染检测）。

---

## 核心概念

终焉议会是游戏的特殊战斗模式，在战斗结算后触发的议会投票环节，玩家通过选择议会提案来获得各种效果。

### 系统架构

```
DoomCouncilLogic                    - 终焉议会逻辑（继承 BaseGameLogic）
    │  管理议会状态、提案生成、投票处理
    │
GameFightLogicDoomCouncil           - 终焉议会战斗模式
    │  在 Settlement 阶段触发议会流程
    │
DoomCouncilBaseEntity               - 议会效果实体基类
    ├── DoomCouncilEntityMoreCrystal    - 更多水晶
    ├── DoomCouncilEntityMoreExp        - 更多经验
    ├── DoomCouncilEntityReincarnation  - 转生
    └── DoomCouncilEntityRename         - 改名
    │
DoomCouncilBean                      - 议会配置数据
```

### 议会流程

```
战斗结算 (Settlement)
    │
    ▼
议会开启 (DoomCouncilLogic.StartGame)
    │  生成 3 个随机提案
    │
    ▼
投票阶段 (UIDoomCouncilVote)
    │  玩家选择一个提案
    │
    ▼
提案执行
    │  应用议会效果
    │
    ▼
议会结算 (UIDoomCouncilVoteEnd)
    │  显示结果
    │
    ▼
返回基地 或 下一关
```

---

## DoomCouncilLogic - 议会逻辑

**文件**: `Assets/Scripts/Game/DoomCouncil/DoomCouncilLogic.cs`

### 核心方法

```csharp
public class DoomCouncilLogic : BaseGameLogic
{
    public DoomCouncilBean currentCouncil;  // 当前议会数据
    public List<DoomCouncilBean> proposals; // 提案列表

    // 准备议会（生成提案）
    public override void PreGame();

    // 开始议会（打开 UI）
    public override void StartGame();

    // 处理玩家投票
    public void HandleVote(int selectedProposalIndex);

    // 应用议会效果
    public void ApplyCouncilEffect(DoomCouncilBean proposal);

    // 结束议会
    public override void EndGame();
}
```

### 提案生成逻辑

```csharp
public void GenerateProposals()
{
    proposals = new List<DoomCouncilBean>();
    
    // 从配置表随机抽取 3 个议会提案
    var allCouncils = DoomCouncilCfg.GetAllArrayData();
    var randomCouncils = RandomTools.GetRandomItems(allCouncils, 3);
    
    foreach (var council in randomCouncils)
    {
        proposals.Add(council);
    }
}
```

---

## GameFightLogicDoomCouncil - 议会战斗模式

**文件**: `Assets/Scripts/Game/Logic/GameFightLogicDoomCouncil.cs`

### 核心特性

终焉议会模式的战斗在结算时会触发议会流程。

```csharp
public class GameFightLogicDoomCouncil : GameFightLogic
{
    public DoomCouncilLogic doomCouncilLogic;

    public override void ChangeGameState(GameStateEnum gameState)
    {
        base.ChangeGameState(gameState);
        switch (gameState)
        {
            case GameStateEnum.Settlement:
                // 战斗结算后进入议会
                StartDoomCouncil();
                break;
        }
    }

    private void StartDoomCouncil()
    {
        // 创建议会逻辑
        doomCouncilLogic = new DoomCouncilLogic();
        doomCouncilLogic.PreGame();
        doomCouncilLogic.StartGame();
    }

    public void OnDoomCouncilComplete()
    {
        // 议会结束后继续战斗流程
        // 根据议会效果应用增益
        // 进入下一关 或 返回基地
    }
}
```

---

## DoomCouncilBaseEntity - 议会效果实体

**文件**: `Assets/Scripts/Game/DoomCouncil/DoomCouncilBaseEntity.cs`

### 基类设计

```csharp
public abstract class DoomCouncilBaseEntity
{
    public DoomCouncilBean councilData;

    // 初始化
    public virtual void InitData(DoomCouncilBean data);

    // 执行议会效果
    public abstract void ExecuteEffect();

    // 获取效果描述
    public abstract string GetEffectDescription();
}
```

### 已有效果类型

#### 更多水晶 (DoomCouncilEntityMoreCrystal)

```csharp
public class DoomCouncilEntityMoreCrystal : DoomCouncilBaseEntity
{
    public override void ExecuteEffect()
    {
        var userData = GameDataHandler.Instance.manager.GetUserData();
        long crystalAmount = councilData.effectValue;
        userData.AddCrystal(crystalAmount);
    }
}
```

#### 更多经验 (DoomCouncilEntityMoreExp)

```csharp
public class DoomCouncilEntityMoreExp : DoomCouncilBaseEntity
{
    public override void ExecuteEffect()
    {
        var userData = GameDataHandler.Instance.manager.GetUserData();
        long expAmount = councilData.effectValue;
        userData.exp += expAmount;
    }
}
```

#### 转生 (DoomCouncilEntityReincarnation)

```csharp
public class DoomCouncilEntityReincarnation : DoomCouncilBaseEntity
{
    public override void ExecuteEffect()
    {
        // 重置生物等级但保留部分属性加成
        // 增加转生计数
    }
}
```

#### 改名 (DoomCouncilEntityRename)

```csharp
public class DoomCouncilEntityRename : DoomCouncilBaseEntity
{
    public override void ExecuteEffect()
    {
        // 打开改名 UI
        UIHandler.Instance.ShowDialog<UIDialogRename>(renameData);
    }
}
```

---

## DoomCouncilBean - 议会配置

**文件**: `Assets/Scripts/Bean/Game/DoomCouncilBean.cs`

### 配置字段

```csharp
public class DoomCouncilBean : BaseBean
{
    public long id;                    // 议会ID
    public long name;                  // 议会名称文本ID
    public long content;               // 议会描述文本ID
    public string class_name;          // 效果实体类名（反射创建）
    public long effectValue;           // 效果数值
    public string effectParam;         // 效果扩展参数
    public int weight;                 // 权重（影响出现概率）
    public int unlockCondition;        // 解锁条件
    public string icon;                // 图标资源

    [JsonIgnore]
    public string name_language { get; }   // 本地化名称

    [JsonIgnore]
    public string content_language { get; } // 本地化描述
}
```

---

## 议会 UI

### UIDoomCouncilMain - 议会场景主界面

**文件**: `Assets/Scripts/Component/UI/Game/DoomCouncil/UIDoomCouncilMain.cs`

议会进行中的常驻主界面，由 `DoomCouncilLogic.StartGame()` 与 `ActionForCouncilorConversationEnd()` 通过 `OpenUIAndCloseOther<UIDoomCouncilMain>()` 打开，**替换基地通用的 `UIBaseMain`**。

- `ui_SuccessText`：显示「当前议案通过率」，文案取 UIText id `53014`（`当前议案通过率:{0}%`），通过 `string.Format(GetTextById(53014), MathUtil.GetPercentage(doomCouncilData.doomCouncilInfo.success_rate, 2))` 填充。
- 通过率数据来源：`GameHandler.Instance.manager.GetGameLogic<DoomCouncilLogic>().doomCouncilData.doomCouncilInfo.success_rate`。
- `ui_UIViewPressControlForGameBase`：与 UIBaseMain 同款基础操作按键提示组（W/A/S/D 移动、E 互动、Space 突进+CD 遮罩；议会场景同样走 `ControlForGameBase`，故 E/Space 显隐逻辑自动生效，详见 ui-components 的 UIViewPressControlForGameBase）。

### UIDoomCouncilBill - 议会议案选择界面

**文件**: `Assets/Scripts/Component/UI/Game/DoomCouncil/UIDoomCouncilBill.cs`

议会议案（提案）列表、标题等界面元素。

### UIDoomCouncilVote - 议会投票界面

**文件**: `Assets/Scripts/Component/UI/Game/DoomCouncil/UIDoomCouncilVote.cs`

```csharp
public class UIDoomCouncilVote : BaseUIView
{
    public List<UIViewDoomCouncilOption> ui_Options;  // 议会选项列表

    public override void OpenUI()
    {
        base.OpenUI();
        // 显示 3 个提案选项
    }

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        // 处理玩家选择
        // 调用 doomCouncilLogic.HandleVote(index)
    }
}
```

### UIDoomCouncilVoteEnd - 议会结算界面

**文件**: `Assets/Scripts/Component/UI/Game/DoomCouncil/UIDoomCouncilVoteEnd.cs`

显示投票结果和应用的效果。

### UIPopupDoomCouncilBillDetails - 议会详情气泡

**文件**: `Assets/Scripts/Component/UI/Popup/UIPopupDoomCouncilBillDetails.cs`

悬浮显示的议会详情信息。`SetSuccessRate(rate)` 除填充文本(id 53003)外，`ui_SuccessRate.color` 按 `ColorUtil.GetProgressColor(rate)`（rate 0~1）分段着色（0-20红/20-40橙/40-60黄/60-80浅绿/80-100蓝，与献祭成功率同口径）。

---

## 创建新的议会效果

### 步骤

#### 1. 创建效果实体类

```csharp
// Assets/Scripts/Game/DoomCouncil/DoomCouncilEntityNewEffect.cs
public class DoomCouncilEntityNewEffect : DoomCouncilBaseEntity
{
    public override void ExecuteEffect()
    {
        var userData = GameDataHandler.Instance.manager.GetUserData();
        // 实现自定义效果
        // 例如：给所有生物增加属性
        foreach (var creature in userData.listCreature)
        {
            // 增加生物属性
        }
    }

    public override string GetEffectDescription()
    {
        return "给所有生物增加永久属性加成";
    }
}
```

#### 2. 在配置表中添加

```
// DoomCouncil 配置表
id | name | content | class_name | effectValue | weight
---|------|---------|------------|-------------|-------
10001 | 新效果名称ID | 描述ID | DoomCouncilEntityNewEffect | 100 | 10
```

#### 3. 如果有关联 UI，创建对应 UI 组件

```csharp
// 如果新效果需要特殊 UI 展示
public class UIDoomCouncilNewEffect : BaseUIComponent
{
    public void SetData(DoomCouncilBean data)
    {
        // 自定义 UI 展示
    }
}
```

---

## 常用代码模板

### 触发议会流程

```csharp
// 在 GameFightLogicDoomCouncil 中
public override void ChangeGameState(GameStateEnum gameState)
{
    switch (gameState)
    {
        case GameStateEnum.Settlement:
            if (ShouldTriggerDoomCouncil())
            {
                TriggerDoomCouncil();
            }
            else
            {
                base.ChangeGameState(gameState);
            }
            break;
    }
}

private bool ShouldTriggerDoomCouncil()
{
    // 每 N 波战斗后触发一次议会
    return (fightData.currentWave % 5 == 0);
}

private void TriggerDoomCouncil()
{
    doomCouncilLogic = new DoomCouncilLogic();
    doomCouncilLogic.PreGame();
    UIHandler.Instance.OpenUI<UIDoomCouncilBill>();
}
```

### 应用议会效果

```csharp
public void ApplyCouncilEffect(DoomCouncilBean proposal)
{
    // 反射创建效果实体
    var effectType = Type.GetType(proposal.class_name);
    var effectEntity = Activator.CreateInstance(effectType) as DoomCouncilBaseEntity;
    
    if (effectEntity != null)
    {
        effectEntity.InitData(proposal);
        effectEntity.ExecuteEffect();
        
        // 显示效果应用 UI
        UIHandler.Instance.OpenUI<UIDoomCouncilVoteEnd>((ui) =>
        {
            ui.SetData(proposal);
        });
        
        // 保存数据
        GameDataHandler.Instance.manager.SaveUserData();
    }
}
```

### 获取议会提案列表

```csharp
public List<DoomCouncilBean> GetRandomProposals(int count = 3)
{
    var allCouncils = DoomCouncilCfg.GetAllArrayData();
    
    // 过滤已解锁的
    var unlockedCouncils = allCouncils.Where(c => IsCouncilUnlocked(c.id)).ToList();
    
    // 按权重随机抽取
    return RandomTools.GetRandomItemsByWeight(unlockedCouncils, c => c.weight, count);
}
```

---

## 文件位置速查

| 功能 | 文件路径 |
|------|----------|
| 议会逻辑 | `Assets/Scripts/Game/Logic/DoomCouncilLogic.cs` |
| 议会效果基类 | `Assets/Scripts/Game/DoomCouncil/DoomCouncilBaseEntity.cs` |
| 更多水晶效果 | `Assets/Scripts/Game/DoomCouncil/DoomCouncilEntityMoreCrystal.cs` |
| 更多经验效果 | `Assets/Scripts/Game/DoomCouncil/DoomCouncilEntityMoreExp.cs` |
| 转生效果 | `Assets/Scripts/Game/DoomCouncil/DoomCouncilEntityReincarnation.cs` |
| 改名效果 | `Assets/Scripts/Game/DoomCouncil/DoomCouncilEntityRename.cs` |
| 魔物等级下降/归0效果 | `Assets/Scripts/Game/DoomCouncil/DoomCouncilEntityCreatureLevelDown.cs` |
| 魔物稀有度下降/归0效果 | `Assets/Scripts/Game/DoomCouncil/DoomCouncilEntityCreatureRarityDown.cs` |
| 想要更多装备效果 | `Assets/Scripts/Game/DoomCouncil/DoomCouncilEntityMoreEquip.cs` |
| 想要更多魔王装备效果 | `Assets/Scripts/Game/DoomCouncil/DoomCouncilEntityMoreDemonLordEquip.cs` |
| 议会配置Bean | `Assets/Scripts/Bean/Game/DoomCouncilBean.cs` |
| 议会战斗数据 | `Assets/Scripts/Bean/Game/FightBeanForDoomCouncil.cs` |
| 装备随机池解析(皮肤/装备散件/套装池) | `Assets/Scripts/Bean/MVC/Game/CreatureRandomInfoBeanPartial.cs` |
| 议会战斗模式 | `Assets/Scripts/Game/Logic/GameFightLogicDoomCouncil.cs` |
| 议会场景主界面(替换UIBaseMain) | `Assets/Scripts/Component/UI/Game/DoomCouncil/UIDoomCouncilMain.cs` |
| 议会议案选择界面 | `Assets/Scripts/Component/UI/Game/DoomCouncil/UIDoomCouncilBill.cs` |
| 议会投票界面 | `Assets/Scripts/Component/UI/Game/DoomCouncil/UIDoomCouncilVote.cs` |
| 议会结算界面 | `Assets/Scripts/Component/UI/Game/DoomCouncil/UIDoomCouncilVoteEnd.cs` |
| 议会详情气泡 | `Assets/Scripts/Component/UI/Popup/UIPopupDoomCouncilBillDetails.cs` |

---

## 注意事项

1. **反射创建**: 议会效果实体通过反射创建，class_name 必须与类名完全一致。
2. **数据保存**: 应用议会效果后必须保存用户数据（SaveUserData）。
3. **议会触发时机**: 议会通常在战斗结算阶段（Settlement）触发，具体触发条件可配置。
4. **提案随机性**: 生成提案时使用权重随机，确保提案多样性。
5. **UI 状态管理**: 议会 UI 是独立的 UI 流程，需要正确处理打开/关闭时机。
