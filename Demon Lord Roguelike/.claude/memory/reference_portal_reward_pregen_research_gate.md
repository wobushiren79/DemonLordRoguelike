---
name: reference_portal_reward_pregen_research_gate
description: 传送门世界奖励预生成(冻结)+预览即实领+解锁重生成, 及 UIPopupPortalDetails 四项预览的设施研究门控
metadata:
  type: reference
---

传送门(Portal)世界奖励现在是「创建传送门时预生成并冻结」的，UIPopupPortalDetails 气泡按难度**预览即实领**，且预览项受设施研究门控。跨 5 个模块，单看任一处不易看全。

**奖励预生成与冻结（预览即实领）**
- `GameWorldDifficultyRandomBean.listReward`(+`rewardUnlockSign`) 在 `CreateDifficultyRandom` 创建传送门时按难度一次性随出并冻结（`GameWorldInfoBeanPartial.cs`）。
- `GameWorldInfoRandomBean.GetDifficultyReward(difficulty)` 取预生成奖励；**列表为空(老存档) 或 解锁了新魔物掉落致装备奖励池签名变化(`rewardUnlockSign != RewardSelectBean.GetConquerEquipPoolSign()`) 时重新生成并刷新签名**。这就是「解锁后重新生成奖励」的实现（无事件、访问时自愈）。
- 通关 BOSS 领奖 `GameFightLogicConquer.ActionForUIFightSettlementNext` 消费这份预生成奖励：`InitDataForReward(baseReward, fightTypeConquerInfo, rewardAddItemNum)`；深渊馈赠「奖励多多」额外件数(魔晶)追加在基础奖励**之后**，`selectNumMax` 钳制到 `listReward.Count`。组成与旧逻辑等价(1装备+(2+extra)魔晶)。

**RewardSelectBean = 奖励生成单一真实源**（`RewardSelectBean.cs`）
- 私有 `CreateItemEquip/CreateItemCrystal` 改吃 `FightTypeConquerInfoBean`(不再吃 FightBean)；私有 `InitRewardList(conquerInfo,testData)` 收口。
- 新增：`InitData(FightTypeConquerInfoBean)`、静态 `CreateRewardListForConquer(conquerInfo)`、`InitDataForReward(baseReward,conquerInfo,extraItemNum)`、静态 `GetUnlockCreatureModelIdsForEquip()`/`GetConquerEquipPoolSign()`。
- 原 `InitData(FightBean,testData)` 与测试模式 `InitData(null,testData)`(LauncherTest) 行为不变。

**UIPopupPortalDetails 四项预览的设施研究门控**
- 弹窗从 `transform.GetChild+Find` 重构为 AutoLinkUI 按名绑定的 4 个 `UIViewPopupPortalDetailsItem`(Name/RoadNum/FightNum/**RoadLength 新增**, 文本 id 411/412/413/**414**) + `ui_UIViewItem` 模板缓存池(奖励道具)。
- 4 项受设施研究门控(`UnlockEnum.PortalPreviewRoadNum=100300002 / PortalPreviewFightNum=100300003 / PortalPreviewRoadLength=100300004 / PortalPreviewReward=100300005`)，`UserUnlock.CheckIsUnlock(UnlockEnum)` 判定，**未解锁整行隐藏**；名字行始终显示；无尽模式不展示关卡数/路径长度/奖励。
- 配置：`excel_research_info`(research_type=1 设施, 坐标 x=400,y=-200~-800) + `excel_unlock_info`(unlock_type=0) + 多语言 `Language_ResearchInfo`/`Language_UIText`(414)。**改的是 Excel，JSON 需在 Unity 运行 ExcelEditorWindow 全部导出才生效**(见 [[reference_language_excel_source]])。

详见 conquer-system / fight-reward-system / research-system 三个 SKILL。相关：[[reference_boss_skill_attack_mode_ext]]。
