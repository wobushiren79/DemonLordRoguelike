---
name: portal-system
description: Demon Lord Roguelike 游戏的传送门(Portal)系统开发指南。使用此SKILL当需要创建或修改基地地图的传送门世界选择/生成、传送门随机数据(世界类型随机：①先挑战100勇士(研究 ChallengeHundredShowRate 概率出现,独立难度链 GetUnlockChallengeHundredDifficultyLevel,抽行两段判定:BOSS率=基础10%+研究 ChallengeHundredBossRate(100300009)等级×10%满级50%,命中在BOSS行内随机/否则普通行内随机,冻结配置行+宝箱奖励(普通3箱/BOSS挑战翻倍:装备6件·魔晶x2)) →②无尽概率=基础10%+研究 InfiniteShowRate(100300008) 等级×10%(需至少一个无尽难度已解锁(GetUnlockInfiniteDifficultyLevel>=2)) →③落回征服；挑战100勇士/无尽全场各只刷1个,已存在则跳过该模式判定、难度、道路数/道路长度/关卡数预生成、奖励预生成)、传送门数量(研究 PortalShowNum)、地图刷新/位置避重叠/图标按模式(程序化星球按iconSeed生成:征服=圆形/无尽=倒三角/100勇士=正方形)、悬停详情气泡(UIPopupPortalDetails 四项预览+奖励, 受设施研究门控; 挑战100勇士难度行显示难度N（勇士/真勇士）、隐藏关卡数行,奖励全量预览(普通3箱/BOSS装备6箱); 无尽显示难度/线路数行,隐藏关卡数/路径长度/奖励行)、进入确认(401征服/419无尽/416挑战100勇士)与难度选择对话框(UIDialogPortalDetails 7+1池左右滑动/点击item直切; 无尽难度2起 difficultyMin=2; 挑战100勇士分支=难度选择器+来袭魔物列表)、点击进入世界→生成 FightBean→进战斗场景等，包括 UIBasePortal、UIViewBasePortalItem、UIDialogPortalDetails、UIPopupPortalDetails、GameWorldInfoBean、GameWorldInfoRandomBean/GameWorldDifficultyRandomBean、FightTypeChallengeHundredInfoBean(Partial)、FightBeanForChallengeHundred、FightTypeInfiniteInfoBean(Partial)、FightBeanForInfinite、excel_game_world_info、excel_fight_type_challenge_hundred_info、excel_fight_type_infinite_info。注意：进入后的征服战斗逻辑见 conquer-system，奖励生成规则见 fight-reward-system，研究节点/解锁机制见 research-system。
watched_files:
  - Assets/Scripts/Component/UI/Game/BasePortal/UIBasePortal.cs
  - Assets/Scripts/Component/UI/Game/BasePortal/UIViewBasePortalItem.cs
  - Assets/Scripts/Component/UI/Dialog/UIDialogPortalDetails.cs
  - Assets/Scripts/Component/UI/Dialog/PortalDetails/UIViewDialogPortalDetailsItem.cs
  - Assets/Scripts/Component/UI/Dialog/PortalDetails/UIViewDialogPortalDetailsCreatureItem.cs
  - Assets/Scripts/Component/UI/Dialog/PortalDetails/UIViewDialogPortalDetailsCreatureItemComponent.cs
  - Assets/Scripts/Component/UI/Popup/UIPopupPortalDetails.cs
  - Assets/Scripts/Component/UI/Popup/PortalDetails/UIViewPopupPortalDetailsItem.cs
  - Assets/Scripts/Bean/MVC/Game/GameWorldInfoBean.cs
  - Assets/Scripts/Bean/MVC/Game/GameWorldInfoBeanPartial.cs
  - Assets/Scripts/Bean/MVC/Game/FightTypeChallengeHundredInfoBean.cs
  - Assets/Scripts/Bean/MVC/Game/FightTypeChallengeHundredInfoBeanPartial.cs
  - Assets/Scripts/Bean/Game/FightBeanForChallengeHundred.cs
  - Assets/Scripts/Bean/MVC/Game/FightTypeInfiniteInfoBean.cs
  - Assets/Scripts/Bean/MVC/Game/FightTypeInfiniteInfoBeanPartial.cs
  - Assets/Scripts/Bean/Game/FightBeanForInfinite.cs
  - Assets/Data/Excel/excel_game_world_info[游戏世界信息].xlsx
  - Assets/Resources/JsonText/GameWorldInfo.txt
  - Assets/Data/Excel/excel_fight_type_challenge_hundred_info[战斗-挑战100勇士].xlsx
  - Assets/Resources/JsonText/FightTypeChallengeHundredInfo.txt
  - Assets/Data/Excel/excel_fight_type_infinite_info[战斗-无尽模式].xlsx
  - Assets/Resources/JsonText/FightTypeInfiniteInfo.txt
  - Assets/Editor/FightModeEditorTabChallengeHundred.cs
---

# 传送门 (Portal) 系统开发指南

## 核心概念

传送门系统是**基地地图的"世界选择/进入"层**——玩家在基地地图上看到若干随机生成的传送门(每个=一个世界实例)，悬停看详情、点击选难度并进入对应战斗模式。它**只负责"选哪局、进哪局"**，进入后的战斗由 `conquer-system`(征服) / `game-fight-logic`(无尽等) 接管。

- **传送门 = 一个世界随机实例**：`GameWorldInfoRandomBean`（运行时随机数据），引用一张世界配置 `GameWorldInfoBean`。
- **世界类型随机**：`SetGameFightTypeRandom(worldId, listExistWorld)` 判定顺序——**①先 roll `ChallengeHundred`**（是魔王就挑战100勇士，单关100只怪；出现概率=研究 `UnlockEnum.ChallengeHundredShowRate`(100300007) 等级×10%，难度上限取独立难度链 `GetUnlockChallengeHundredDifficultyLevel`(worldId)，命中且有匹配配置行才生效）；**②再 roll `Infinite`**（无尽，概率=**基础10% + 研究 `UnlockEnum.InfiniteShowRate`(100300008) 等级×10%**（9级满级=100%）：前提该世界**至少一个无尽难度已解锁** `GetUnlockInfiniteDifficultyLevel(worldId) >= 2`，`Random.Range(0,100) < GetUnlockInfiniteShowRate()` 命中）；**③其余落回 `Conquer`**（默认）。**挑战100勇士/无尽全场各只刷 1 个**：`listExistWorld`（= `UserTempBean.listPortalWorldInfoRandomData`）已存在该模式世界则跳过对应判定（旧存档存量多个不清理，仅不再新增）。旧的"Conquer/Infinite 均匀随机列表"已删除。
- **数量受研究控制**：地图上传送门个数 = `UserUnlockBean.GetUnlockPortalShowCount()`(基础 `portalShowMax` + 研究 `UnlockEnum.PortalShowNum` 等级)。
- **数据缓存在临时存档**：`UserTempBean.listPortalWorldInfoRandomData`，打开地图不重新洗牌，只按数量缺口补足；点"刷新"（需研究 `PortalRefreshNum` 解锁、有次数限制、通关回满）才清空重洗。

### 系统架构

```
UIBasePortal (基地地图, BaseUIComponent)
    │  OpenUI→SetBasePortalCamera + InitMap()
    │  InitMap：先显示已缓存世界(listPortalWorldInfoRandomData)，再按 GetUnlockPortalShowCount() 缺口补足
    │  CreateRandomPortalWorld：随机已解锁世界id(GetUnlockGameWorldIds) → SetGameFightTypeRandom(传已存在世界列表, 挑战100勇士/无尽全场各只刷1个) → 随机地图位置(避重叠) → iconSeed
    ▼
UIViewBasePortalItem (单个传送门, BaseUIView)
    │  图标按模式(程序化星球按iconSeed生成:征服=圆形/无尽=倒三角/挑战100勇士=正方形, ui_Icon 为 Image)/缓慢绕中心旋转/出现动画
    │  悬停 ui_BG → PopupEnum.PortalDetails → UIPopupPortalDetails(详情气泡)
    │  点击 ui_BG → OnClickForEnterWorld
    ▼
OnClickForEnterWorld
    │  确认对话框(文本401征服/419无尽/416挑战100勇士) + 难度选择 UIDialogPortalDetails(挑战100勇士分支=难度选择器+来袭魔物列表)
    │  确认 → ShowMask → 按 gameFightType 造 FightBeanForConquer / FightBeanForInfinite / FightBeanForChallengeHundred
    ▼
WorldHandler.EnterGameForFightScene(fightData)  → 进入战斗(交给 conquer-system / game-fight-logic)
```

## 数据模型

### GameWorldInfoBean（世界配置, 自动生成, 禁改）
`Assets/Scripts/Bean/MVC/Game/GameWorldInfoBean.cs`（Cfg.fileName = `GameWorldInfo`）

| 字段 | 含义 |
|------|------|
| `icon_res` | 图标资源名(为空则用 iconSeed 程序化生成行星贴图) |
| `unlock_id` | 世界本身解锁id |
| `unlock_id_infinite` | 无尽模式**研究起始解锁id**(无尽有难度概念：难度N(2~10)对应解锁id=起始id+(N-2)，第一难度没有无尽模式；0=该世界未配置无尽研究；任意一个无尽难度解锁后该世界才可能刷出无尽) |
| `unlock_id_challenge_hundred` | **挑战100勇士难度研究起始解锁id**：难度1默认可用；难度N(2~10)对应解锁id=起始id+(N-2)（world1=100310160）；0=该世界未配置100勇士难度链，难度恒为1（字段暂在 `GameWorldInfoBeanPartial` 临时 region，重新生成 Entity 后删除） |
| `unlock_id_conquer_difficulty_level` | 征服难度起始解锁id |
| `unlock_id_quick_attack` | 加快进攻节奏(Quick)研究起始解锁id(0=该世界无此类研究, 实际id=起始id+难度-1) |
| `unlock_id_speed2` | 2倍速游戏研究起始解锁id(0=该世界无此类研究, 实际id=起始id+难度-1) |
| `unlock_id_speed0` | 0倍速游戏研究起始解锁id(0=该世界无此类研究, 实际id=起始id+难度-1) |
| `map_pos` | 地图坐标(配置) |
| `name` | 名字多语言id(`name_language` 带缓存) |

### GameWorldInfoRandomBean（传送门运行时随机数据, 手写, 可改）
`Assets/Scripts/Bean/MVC/Game/GameWorldInfoBeanPartial.cs` — 一个传送门一份，存 `UserTempBean.listPortalWorldInfoRandomData`。

- 字段：`worldId`、`gameFightType`、`roadNum`/`roadLength`/`fightNum`(当前难度的值)、`uiPosition`(Vector2Bean 规避 Newtonsoft 递归)、`iconSeed`、`difficultyLevel`、`listDifficultyRandom`(各难度预生成数据：道路/关卡/战斗场景/奖励)；ChallengeHundred 专用：`listChallengeHundredRandom`(逐难度预生成的 `GameWorldChallengeHundredRandomBean`：难度/冻结行id/道路/战斗场景/3箱奖励/解锁签名)、`challengeHundredRowId`(当前活动难度的冻结配置行id，0=未冻结)、`listRewardChallengeHundred`(当前活动难度的通关宝箱奖励：普通3箱/BOSS挑战装备6箱)、`rewardUnlockSignChallengeHundred`(当前活动难度生成奖励时的装备池解锁签名)。
- `SetGameFightTypeRandom(worldId)`：**判定顺序——①先 roll ChallengeHundred 出现概率**(`UserUnlockBean.GetUnlockChallengeHundredShowRate()`=研究 `UnlockEnum.ChallengeHundredShowRate`(100300007) 等级×10%)，命中后按**100勇士独立难度链**取上限 `GetUnlockChallengeHundredDifficultyLevel(worldId)`，抽配置行 `FightTypeChallengeHundredInfoCfg.GetRandomRow(unlockDifficultyMax, bossRate)` 为**两段判定**：先按 BOSS 挑战出现概率 `GetUnlockChallengeHundredBossRate()`(=**基础10%**+研究 `UnlockEnum.ChallengeHundredBossRate`(100300009) 等级×10%，4级满级=50%) 判定普通/BOSS，再在命中类型行内等概率随机(命中类型该难度无匹配行落回另一类型，全无匹配返回 null 落回原随机)，抽中行才生成为该模式并直接调 `SetRandomDataForChallengeHundred`；**②再 roll 无尽**(概率=**基础10%+研究 `UnlockEnum.InfiniteShowRate`(100300008) 等级×10%**(9级满级=100%)：前提该世界**至少一个无尽难度已解锁** `GetUnlockInfiniteDifficultyLevel(worldId) >= 2`，`Random.Range(0,100) < GetUnlockInfiniteShowRate()` 命中) → `SetRandomDataForInfinite`；**③其余落回征服** → 调 `SetRandomData`。旧的"Conquer/Infinite 均匀随机列表"已删除。
- `SetRandomDataForConquer`：创建时把 `1~已解锁最高难度` 逐档随机出 `GameWorldDifficultyRandomBean`(道路数/长度/关卡数 + **普通关/BOSS关战斗场景冻结** + **预生成奖励**)缓存进 `listDifficultyRandom`，默认难度取已解锁最高。
- `SetRandomDataForInfinite`：按无尽已解锁难度(2~`GetUnlockInfiniteDifficultyLevel(worldId)`)**逐难度预生成 `listDifficultyRandom`**——每档走 `CreateInfiniteDifficultyRandom`：roadNum/roadLength 取自同难度征服行 `road_num`/`road_length` 区间(切换难度直接取用保证恒定)；`fightSceneId`/`bossFightSceneId` 取同难度征服行场景池冻结(无尽每轮都是BOSS，实际用 `bossFightSceneId`)；`fightNum=1` 占位(无尽无关卡数概念)；`listReward` 留空(无尽无通关奖励)；默认难度=最高已解锁无尽难度并 `SetDifficultyLevel` 同步。
- `GetInfiniteDifficultyRandom(D)`：取该难度道路数据；**已解锁但未预生成**(传送门创建后研究新解锁的无尽难度)时懒补齐并冻结，未解锁/无配置返回 null。`SetDifficultyLevel` 无尽分支与气泡均走此方法(不走 `GetDifficultyRandom`，避免连带生成征服通关奖励)。
- `SetRandomDataForChallengeHundred(challengeHundredInfo)`：按 `1~GetUnlockChallengeHundredDifficultyLevel(worldId)` **逐难度预生成** `listChallengeHundredRandom`——最高难度沿用传入行(不匹配则重抽)，其余难度用 `GetRandomRow(level, bossRate)`；每档冻结配置行 id、`roadNum`/`roadLength`(行区间 x或x-y)、战斗场景 `fightSceneId`(行 `fight_scene_ids` 随机其一)、3箱奖励(`CreateRewardListForChallengeHundred(行, level)`，装备池空=全魔晶，否则全装备；**BOSS挑战翻倍：装备3箱→6件、魔晶单箱数量x2**)；`fightNum=1`；默认 `difficultyLevel`=最高已解锁100勇士难度并 `SetDifficultyLevel` 同步活动字段。
- `EnsureChallengeHundredRandomData()`：旧存档 `listChallengeHundredRandom` 为空时懒重建——按旧 `challengeHundredRowId` 保留当前难度(夹到新难度范围)、补齐其他已解锁难度；`GetChallengeHundredRandom(D)` 首次访问也会触发 Ensure。
- `CreateChallengeHundredRandomByLevel(D)`(**研究解锁补生成**, 2026-10-21)：`GetChallengeHundredRandom(D)` 在列表里找不到该档时调用——逐档 `CheckChallengeHundredUnlock(worldId,D)` 仅补已解锁难度、按当前 `GetUnlockChallengeHundredBossRate()` 抽行、`CreateChallengeHundredRandom` 生成(含3箱奖励)后加入列表冻结；未解锁/无匹配行返回 null。**修复场景**：传送门创建后才研究解锁难度N → 气泡不显示该档奖励/道路、`SetDifficultyLevel(N)` 不生效(战斗仍用旧档冻结行/结算按难度N取不到奖励)。
- `SetDifficultyLevel` 的 ChallengeHundred 分支：按目标难度从 `listChallengeHundredRandom` 同步 `challengeHundredRowId/roadNum/roadLength/fightNum/listRewardChallengeHundred/rewardUnlockSignChallengeHundred`(缺失已解锁档由 `GetChallengeHundredRandom` 懒补齐)；战斗/结算/气泡均读这些活动字段。
- `GetChallengeHundredRandom(D)` / `GetChallengeHundredReward(D)` / `IsChallengeHundredBossChallenge(D)`：按指定难度取该档预生成数据/奖励/BOSS类型(缺失已解锁档懒补齐，未解锁返回 null)；无参版本委托当前活动难度，旧存档列表为空时回退原有活动字段。
- `SetRandomData` switch 有 ChallengeHundred **防御性 case**：正常由 `SetGameFightTypeRandom` 直接调 `SetRandomDataForChallengeHundred`；走到 switch 说明只有冻结行id(如旧数据重放)，改调 `EnsureChallengeHundredRandomData()` 懒重建并保留当前难度，失败 LogError。
- `SetDifficultyLevel(level)`：切换难度，把 `roadNum/roadLength/fightNum` 同步为该难度缓存值(气泡、战斗都读这些字段)。**无尽分支**：走 `GetInfiniteDifficultyRandom(level)` 同步 roadNum/roadLength(无关卡数)，**不走 `GetDifficultyRandom`**——那会连带生成征服通关奖励；研究新解锁但未预生成的难度由该方法懒补齐(返回 null 时保留当前值)。
- `GetDifficultyRandom(level)`：取某难度数据(缺失懒生成并缓存)。
- `GetDifficultyReward(level)`：取该难度**预生成奖励**(预览即实领)；空或解锁池签名变化时重新生成，见下。
- `GetFightSceneId(level, isBoss)`：取本世界指定难度**已冻结的战斗场景**(2026-10-22 起，场地不再每次进战斗重随)——Conquer/Infinite 走 `GetDifficultyRandom`/`GetInfiniteDifficultyRandom` 的 `fightSceneId`/`bossFightSceneId`，ChallengeHundred 走 `GetChallengeHundredRandom(D).fightSceneId`(按冻结行随机)；老存档场景字段为0时按配置懒补齐并写回(补齐后同样固定)，无冻结数据(如无尽测试入口未解锁难度)时按配置随机兜底。三个 FightBean 的 `fightSceneId` 全部改为此方法取值。

### GameWorldDifficultyRandomBean（单难度随机数据）
每难度一份：`difficultyLevel`、`roadNum`、`roadLength`、`fightNum`、`fightSceneId`(普通关冻结场景)、`bossFightSceneId`(BOSS关冻结场景，无尽每轮用)、`listReward`(预生成通关奖励)、`rewardUnlockSign`(生成时的装备奖励池解锁签名)、**`listEnemyTypeId`/`listEnemyBossTypeId`**(2026-10：按征服行 `enemy_type_num`/`enemy_boss_type_num` 冻结的敌人种类，空列表=未配置整池全随机；**null=老存档未生成**，由 `GameWorldInfoRandomBean.GetDifficultyEnemyTypeIdList(D,isBoss)` 懒补齐写回，勿把字段初始化为空列表)、**`listEnemyBossSpawnId`**(BOSS 出场阵容：每个元素=一只 BOSS，长度=BOSS 数量，世界生成时由 `attack_boss_num` × BOSS 种类随出；战斗出怪与弹窗去重展示共用，null=老存档由 `GetDifficultyEnemyBossSpawnIdList(D)` 懒补齐)。无尽复用同一缓存但只填道路/场景，奖励与敌人种类/阵容留空。

### FightTypeChallengeHundredInfoBean（挑战100勇士配置, Bean 自动生成禁改 / Partial 手写扩展）
`Assets/Scripts/Bean/MVC/Game/FightTypeChallengeHundredInfoBean(Partial).cs`（Cfg.fileName = `FightTypeChallengeHundredInfo`，一行一难度，当前10行）。

- Partial 扩展：`GetEnemyIdList()`/`GetRandomEnemyId()`(enemy_ids ,分隔；生成100只怪时每只独立从列表随机抽取)、`GetDifficultyLevelList()`/`IsMatchDifficulty(level)`(本行是否可被指定难度抽中，即 difficulty_levels 含该难度)、`GetRandomFightScene()`(fight_scene_ids 随机其一；只在传送门创建/老存档懒补齐时调用一次，结果冻结进 `GameWorldChallengeHundredRandomBean.fightSceneId`)、`GetRandomRoadNum()`/`GetRandomRoadLength()`(x或x-y区间)、`GetRandomRewardCrystal()`(x固定或x-y随机，解析失败返回0)、`GetIntensityRate()`(attack_intensity_baserate 强度倍率，敌人HP/护甲/攻击力×该值，≤0按1)。
- Cfg 扩展：`GetMatchRows(unlockDifficultyMax)`(指定难度可抽中的全部行)、`GetRandomRow(unlockDifficultyMax, bossRate)`(**两段随机**：先按 bossRate(百分数0~100) 判定普通/BOSS挑战，再在命中类型(challenge_type)的行内等概率随机其一；命中类型无匹配行落回另一类型，**全无匹配返回 null，调用方需落回默认模式**)、`GetMaxDifficultyLevel()`(2026-10 新增：扫描全部行 `difficulty_levels` 取最高，供弹窗未解锁预览)。

### FightTypeInfiniteInfoBean（无尽模式配置, Bean 自动生成禁改 / Partial 手写扩展）
`Assets/Scripts/Bean/MVC/Game/FightTypeInfiniteInfoBean(Partial).cs`（Cfg.fileName = `FightTypeInfiniteInfo`，一行=一个世界一个无尽难度，当前 9 行=世界1难度2~10）。

- 字段：`world_id` / `level`(难度2~10，无尽无难度1) / `round_intensity_addrate`(每轮强度倍率：每轮怪物HP/护甲/攻击力×该值^(轮-1)，含BOSS；第1轮恒为1；1=不递增) / `remark`。
- Partial 扩展：`GetRoundIntensityRate(round)`(轮次强度倍率=`round_intensity_addrate^(round-1)`，仅无尽表自身乘区，不含征服基础倍率与终焉议会加成；配置非法≤0按1)。
- Cfg 扩展：`GetItemData(worldId, difficultyLevel)`(指定世界指定无尽难度配置行，无配置返回 null)、`GetMaxLevel(worldId)`(配置表中最高无尽难度，供传送门详情弹窗展示未解锁预览，无配置返回0)。
- 战斗侧消费：`FightBeanForInfinite` 持 `fightTypeInfiniteInfo`，`GetRoundIntensityRate(round)` = 征服行 `attack_intensity_baserate`(每轮恒定基区) × 无尽行 `round_intensity_addrate^(round-1)`(逐轮乘区) × 终焉议会强度议案倍率；无尽配置行缺失时 LogError 且轮次倍率按1处理。

## 地图生成与刷新（UIBasePortal）

- **不洗牌补足**：`InitMap` 先显示 `listPortalWorldInfoRandomData` 已缓存世界，再 `for cachedCount..showCount` 用 `CreateRandomPortalWorld` 补足差额并 `AddPortalWorldInfoRandomData`。解锁研究提高数量后，旧世界不动、只新增——避免每次打开重洗。
- **刷新（研究解锁 + 次数限制 + 通关回满）**：默认关闭——设施研究 `UnlockEnum.PortalRefreshNum`(100300006) 未解锁时 `ui_BtnRefresh` 整个隐藏（`RefreshBtnRefreshState` 在 `InitMap` 末尾按 `CheckIsUnlockPortalRefresh` 控制）。已解锁时 `ui_BtnRefreshNum` 显示剩余次数(形如 `x5`)；`OnClickForRefresh` 仅剩余>0 才 `ReducePortalRefreshNum`+`ClearPortalWorldInfoRandomData`+`ClearMap`+`InitMap`(全部重随)+`SaveUserData`，剩余=0 弹提示(UI文本 id 2007「刷新次数已用完」)不刷新。
- **刷新次数账本（"已用次数"模型）**：剩余 = 刷新研究等级(上限，每级+1，满级10) − `UserTempBean.portalRefreshUsedNum`(已用)；存取 `GetPortalRefreshRemainNum`/`ReducePortalRefreshNum`(已用+1)/`RefillPortalRefreshNum`(已用归0回满)。研究升级使上限+1时剩余自动+1，无需特判研究购买流程。
- **挑战结束后的世界消耗（按模式分开，2026-10 起）**：
  - **征服**：通关一个世界 `GameFightLogicConquer.ActionForUIRewardSelectEnd` 同时调 `RefillPortalRefreshNum`(次数回满) 与 `ClearPortalWorldInfoRandomData`(清空整个世界列表) → 下次打开传送门UI `InitMap` 缓存为空、全量重新生成全部世界(随 `EndGameAndReturnToBase` 的 `SaveUserData` 落盘)。**仅通关路径**；战败/中途退出走 `EndGameAndReturnToBase` 不清世界、不回满，故世界保留不重洗。
  - **无尽 / 挑战100勇士**：**该世界只能挑战一次，挑战结束即消耗**（输赢/ESC「结束战斗」中途放弃都算）。基类 `GameFightLogic.ConsumeCurrentPortalWorld()` → `UserTempBean.RemovePortalWorldInfoRandomData(本场世界)` **只移除这一条**，其余世界不动、**不回满刷新次数**；不调 `ClearPortalWorldInfoRandomData`（不全清）。下次打开传送门UI时 `InitMap` 检测到 `cachedCount < showCount`，自动补生成一个新世界。三处挂钩：无尽结算退出 `ActionForUIFightSettlementExit` + 无尽 `ExitFightAndReturnToBase` 重写；100勇士 `EndGameAndReturnToBase`(胜利领奖结束/失败结算) + `ExitFightAndReturnToBase` 重写。100勇士页内的「重新挑战」不消耗世界（同世界重开）。
  - **为何"移除→下次打开惰性补生成"而非结束瞬间重建**：`CreateRandomPortalWorld`→`GetRandomMapPos` 依赖传送门UI的 `ui_Content`/item rect，UI 关着时取不到布局，无法即时重建。
- **位置避重叠**：`GetRandomMapPos(listOldPos)` 在内容区 90% 范围内随机，与已占位置(含配置项尺寸)碰撞则递归重取。
- **图标按模式区分（SetIcon(iconRes, iconSeed, gameFightType)，ui_Icon 为 Image）**：全部为 `icon_res` 为空时程序化生成星球贴图——`CreateTools.CreatePlanetTexture(new CreateToolsForPlanetTextureBean(iconSeed){ shape = ... })` 再 `Sprite.Create` 赋 `ui_Icon.sprite`；外形由 `UIViewBasePortalItem.GetPlanetShapeByFightType(gameFightType)` 决定：征服等默认=圆形(Circle)、无尽=倒三角(InvertedTriangle 顶点内接于圆)、挑战100勇士=正方形(Square 内接于圆, 半边长=半径/√2 宽高不超圆形范围)，外形枚举 `PlanetTextureShapeEnum` 与边缘距离/内切圆半径helper(`GetPlanetShapeEdgeDistance`/`GetPlanetShapeInradius`)在框架层 CreateTools（见 utils/framework）。两 prefab 的 Icon 节点原为 RawImage，因 UI 图集开启 tight packing（uvRect 裁不出干净区域）已统一改为 Image。
- **退出/刷新**：ESC/ui_ViewExit → `UIBaseMain`；ui_BtnRefresh → 消耗1次刷新次数并重洗（未解锁则按钮隐藏、用完弹提示）。

## 传送门item（UIViewBasePortalItem）

- 悬停气泡：`ui_BG` 上的 `PopupButtonCommonView` 注册 Enter/Exit，`SetData((gameWorldInfo, gameWorldInfoRandom, difficultyLevel), PopupEnum.PortalDetails)`（地图item展示**当前难度** `gameWorldInfoRandom.difficultyLevel`）。悬停时停止旋转(`isRotate=false`)。
  - **气泡展示哪个难度**：SetData 第三参 `difficultyLevel` 即展示难度。地图item传的是**当前难度**——它在世界创建时由 `SetRandomDataForConquer`(征服)/`SetRandomDataForInfinite`(无尽) 末尾 `SetDifficultyLevel(已解锁最高难度)` 置为**已解锁最高难度**(征服非难度1、无尽非难度2以下；构造器 `difficultyLevel=1` 只是占位，创建时被覆盖)，之后跟随难度对话框选择。故解锁多难度时，地图气泡默认显示**已解锁最高难度**。对话框各item则传各自难度号(展示该item自身难度)。事后解锁更高难度不会自动抬高已缓存世界(clamp 只向下夹)，需世界重新生成(通关清空/刷新)才重取。
- 缓慢绕 `rotateCenter` 旋转(随机 speed)；出现 `DOScale` OutBack。
- **BG 颜色按模式区分（`SetBGColor`，取值收口 `UIViewBasePortalItem.GetBGColorByFightType(gameFightType)`）**：征服等默认=白、无尽=深紫 `ColorUtil.PortalInfinitePurple`(#5B2C6F)、挑战100勇士=金 `ColorUtil.PortalChallengeHundredGold`(#D4AF37)。`ui_BG`(Button) 过渡为 **SpriteSwap**(不换色)，直接改 `ui_BG.image.color` 可持久生效、不会被悬停/按压覆盖。**注意**：Animator 的 `UIViewBasePortalItem.anim`(Start 状态, `Assets/LoadResources/Anim/UI/`) 原有 BG m_Color.r/g/b/a 4 条曲线会每帧覆盖代码设置的颜色，已删除（BG 的 localScale 曲线保留）；SetBGColor 保留 prefab 原透明度(0.9)只替换 RGB。
- **名字显示按模式区分**：ChallengeHundred 传送门名字显示模式名(UIText 417「是魔王就挑战100勇士」)替代世界名；Infinite 显示 `string.Format`(UIText 420「{0}·无尽」, 世界名)加「·无尽」后缀(地图上可直接辨识)；征服显示世界配置名。
- 点击 `ui_BG` → `OnClickForEnterWorld`。

## 进入流程（OnClickForEnterWorld）

1. 造确认 `DialogBean`(content 按模式区分：征服=文本401「是否开启{0}的征服之旅？」带世界名参数，无尽=文本419「是否开启{0}的无尽之战？」带世界名参数，ChallengeHundred=文本416「是否接受100勇士的挑战？」)，`actionSubmit` 内：item 放大靠近动画 + `ShowMask` → 按 `gameFightType` 造 `FightBeanForConquer`/`FightBeanForInfinite`/`FightBeanForChallengeHundred`(switch case) → `WorldHandler.EnterGameForFightScene(fightData)`。
2. 同时 `UIHandler.ShowDialogPortalDetails(dialogData)` 弹**难度选择对话框** `UIDialogPortalDetails.SetData(gameWorldInfo, gameWorldInfoRandom)`。
3. `actionCancel` → `isSelectWorld=false`(恢复旋转)。

## 难度选择对话框（UIDialogPortalDetails）

- **ChallengeHundred 模式分支（`SetData` 内 + `RefreshCreatureList`）**：100勇士自 2026-10 起**有独立难度链**，与无尽一样**同时显示难度选择器与来袭魔物列表**——`difficultyMin=1`、`unlockDifficultyMax=GetUnlockChallengeHundredDifficultyLevel(worldId)`(难度1默认可用)、`configDifficultyMax`=世界配了难度链时取 `FightTypeChallengeHundredInfoCfg.GetMaxDifficultyLevel()`(未解锁档显示锁链预览，否则=已解锁上限)；先 `EnsureChallengeHundredRandomData()` 兼容旧存档，`ResolveDefaultDifficulty` 取最近已解锁档后 `SetDifficultyLevel`；难度选择器与 `ui_CreatureList` 的**位置/高度完全沿用 prefab 默认布局**(当前两容器均 300 高，运行时不改 sibling index/高度/spacing)，实例化魔物 item 尺寸跟随容器高度(方形)。来袭魔物列表按**当前活动难度**的 `challengeHundredRowId` 取配置，`GetEnemyIdList()` **去重**后动态实例化 `UIViewDialogPortalDetailsCreatureItem`(模板隐藏作蓝本，手动等距居中排布；**2026-10 排布规则**：`item 尺寸 = 间距 = Min(容器高, 容器宽/数量)`——数量少时按容器高(300)排布且间距=尺寸不叠压，数量多时同步等比缩小，整排恰好铺满容器宽、两端不出界)；切难度后重新调用 `RefreshCreatureList()` 刷新列表。`OnInputActionForStarted` 的 Navigate 左右键对 ChallengeHundred 也切难度。AutoLink 绑定 `ui_Difficulty`/`ui_CreatureList`/`ui_UIViewDialogPortalDetailsCreatureItem`；prefab 结构：DialogContent 下 Difficulty 容器与 CreatureList 容器(含隐藏的 CreatureItem 嵌套模板)。
- **来袭魔物列表的研究门控（挑战100勇士与征服共用）**：`SetData` 里 `isShowCreatureList = (ChallengeHundred || Conquer) && userUnlock.CheckIsUnlock(UnlockEnum.PortalPreviewCreature)`（设施研究 unlock_id **100300011**「来袭魔物预览」，前置=奖励预览 100300005，1000 魔晶）。**未解锁时 `ui_CreatureList` 整区隐藏且不实例化任何魔物**；`RefreshCreatureList()` 先清空旧 item 再据此早退，避免弹窗复用时残留上一次的敌人。
- **Conquer 模式分支（来袭魔物列表，2026-10 新增）**：征服同样显示 `ui_CreatureList`（受上条研究门控）——`RefreshCreatureList()` 按 `gameFightType` 分派到 `CollectConquerCreatureIds`：普通敌人在前、BOSS 在后（均按 npcId 去重、普通优先占位）；**普通敌人**：配置了 `enemy_type_num` 的难度只显示世界生成时冻结的那几种（`GetDifficultyEnemyTypeIdList(D,false)`），未配置则显示整池可能出现的敌人；**BOSS**：显示世界生成时冻结的**出场阵容去重后的种类**（`gameWorldInfoRandomData.GetDifficultyEnemyBossSpawnIdList(D)`，阵容由 `attack_boss_num` 数量 × BOSS 种类随出）——例如 BOSS 数量 5 只、这 5 只里只有 3 种类型，就只显示这 3 种；阵容为空（不刷 BOSS）不显示 BOSS。BOSS item 用低饱和红底。无尽不显示该列表。
- **Infinite 模式分支**：无尽**有难度概念**(难度2起，第一难度没有无尽模式)，难度选择器保持显示：`difficultyMin=2`(征服=1/无尽=2/100勇士=1)、`unlockDifficultyMax=UserUnlockBean.GetUnlockInfiniteDifficultyLevel(worldId)`(扫描取最高已解锁)、`configDifficultyMax=FightTypeInfiniteInfoCfg.GetMaxLevel(worldId)`(无尽配置表最高难度，供未解锁预览)；`IsValidDisplayDifficulty` 用 `difficultyMin` 与 `max(unlock,config)`；**逐档解锁判断**用 `CheckInfiniteUnlock(worldId,D)`(跳级时 D2 锁、D3 解锁会正确显示锁链)，`ShowItem` 的 `isUnlock`、点击拦截、左右切换全部走逐档判断；左右切换**跳过未解锁空档**；默认难度取夹取范围内最近的已解锁档(`ResolveDefaultDifficulty`)；难度item**隐藏通关标记**(`ShowItem` 传 `isShowCompleteMark: gameFightType==Conquer`——无尽无通关统计)。
- **UIViewDialogPortalDetailsItem.SetData 新增可选参 `isShowCompleteMark=true`**：false 时 `ui_Complete_0`/`ui_Complete_1` 恒隐藏(无尽等无通关统计的模式用；征服照旧按 `GetConquerCompleteCount` 显隐)。
- **UIViewDialogPortalDetailsCreatureItem**（PortalDetails 目录，挑战100勇士/征服共用来袭魔物item）：`SetData(npcId, isBoss=false)` → `NpcInfoCfg.GetItemData` → `GameUIUtil.SetCreatureUIForSimple(ui_Icon, new CreatureBean(npcInfo))`(Simple 管线 Spine 图标)。**BG 按 `isBoss` 着色**：BOSS（征服的 BOSS 池 / 100勇士 `challenge_type==1` 的真勇士整组）用 `ColorUtil.PortalCreatureBossRed`(#B85656 低饱和红)，普通魔物白；只替换 RGB、保留 prefab 原透明度(0.6)，`ui_BG.color` 直接赋值即可（prefab 无 Animator 覆盖）。Component 的 `ui_Icon` 为 SkeletonGraphic(prefab 上 Icon 残留的 SkeletonAnimation 已移除)。Spine 尺寸/位置取**小卡口径**(幻化 `show_data` 优先、回落 `ui_data_s`)，再按容器实际高度走 `GameUIUtil.ApplyCardIconSizeFit(..., GameUIUtil.cardContentHeightForS)` 等比修正（实例化尺寸=ui_CreatureList 高度(当前 300)×300 方形，容器高度≠小卡标准 142，直接套小卡尺寸会偏小偏位；口径同 UIViewMainLoadItem 的大卡版）。
- **7+1 item 对象池**：常驻显示中心±3 共**最多 7 档**，切换时第 8 个临时滑出(`InitItemPool` 克隆共 8 个)。**难度卡垂直位置 `itemPosY` 取模板 item 在 prefab 中的原始 Y**(`InitItemPool` 时读取，当前 50)，`ShowItem` 不再写死 y=0——调上下位置直接改 prefab 中 `ui_UIViewDialogPortalDetailsItem` 的 AnchoredPosition.y。透明度/缩放按距中心距离取梯度(`alphaByDistance`/`scaleByDistance`，0=中心 1/0.75… → 3=最外 0.4/0.6)。
- 左右滑动切换(`AnimSwitchDifficulty`，`itemSpacing=230`，`Ease.OutBack`)，到边界回弹(`AnimSwitchBlocked`)；**逐档判断是否解锁**：点击未解锁 item 回弹 + Toast「难度未解锁」(文本404)；左右切换用 `FindNextUnlockDifficulty` **跳过跳级造成的未解锁空档**；大幅跳级(跨度>3)直接刷新不做长距离滑入。
- **点击item直切**：`OnClickForDifficultyLevel(itemView)`——点击任一 item 直接 `SetDifficultyLevel` 到该难度(未解锁 item 回弹+提示)；左右切换按钮/方向键仍走 `OnClickForChangeDifficultyLevel(±1)`。
- **切换后**：`SetDifficultyLevel` 同步该难度道路/关卡(征服/无尽)或配置行/道路/奖励(100勇士)；随后统一调 `RefreshCreatureList()` 刷新来袭魔物列表（挑战100勇士/征服；无尽内部直接返回）。
- **克隆item按钮注册**：运行时 `InitItemPool` 克隆的 item 不在 Awake 的 `RegisterButtons` 收集范围，须用 `RegisterButton` 手动注册(模板 item 的按钮已在 Awake 注册)。
- 输入：ESC 取消(等同点取消按钮, `CancelOnClick`)、左右方向键切换。依赖框架层放行：`BaseUIInit.CallBackForInputActionStarted` 在 `dialogList` 非空时仅放行最顶层弹窗(列表末位)自身，其余 UI 拦截——弹窗 ESC/快捷键能生效全靠此规则，勿改回"有弹窗全拦截"。
- **出战阵容选择区（ui_Lineup）**：已解锁阵容数 `GetUnlockLineupNum()` >= 2 才显示（否则整区隐藏）。`ui_LineupTitle` 标题=UIText 30009「出战阵容」；`ui_LineupName` 显示当前出战阵容名（`userData.GetLineupShowName(lineupFightIndex)`：自定义名优先，未改名回退默认「阵容 {序号}」30005）；`ui_LineupLeftBtn/RightBtn` 循环切换（`OnClickForChangeLineup(±1)`，0基偏移取模绕回）。选择存 `UserDataBean.lineupFightIndex`（`GetLineupFightIndex` 夹取 [1,已解锁数]），**切换即 SaveUserData**，下次打开弹窗默认选中上次阵容；进入战斗时 `FightBeanForConquer`/`FightBeanForDoomCouncil` 按该序号读阵容（替代旧写死第 1 套）。
- **UIViewDialogPortalDetailsItem**：单难度卡，图标与 UIViewBasePortalItem.SetIcon 同逻辑(程序化星球:征服=圆形/无尽=倒三角/100勇士=正方形)、难度文本(403「难度 等级{0}」)、解锁/未解锁灰罩、`bg_color`(征服难度表)、完成度、透明度/缩放动画、悬停 `PopupEnum.PortalDetails`(展示**该item自身难度**)。100勇士与无尽都使用该item；100勇士额外在下方显示来袭魔物列表。

## 详情气泡（UIPopupPortalDetails）

继承 `PopupShowCommonView`，`SetData((GameWorldInfoBean, GameWorldInfoRandomBean, int difficultyLevel))`；第三参**传 -1 表示跟随 `gameWorldInfoRandom.difficultyLevel` 当前活动难度**(地图传送门item用)，≥1 表示展示该item自身难度(难度选择器item用)。AutoLinkUI 按名绑定的 6 个 `UIViewPopupPortalDetailsItem`(名字/模式/难度/线路数/关卡数/路径长度) + `ui_UIViewItem` 模板缓存池(奖励道具)。

- **气泡自身底色**：根节点 Image(Awake `GetComponent<Image>()` 缓存为 `imageBG`)，与传送门item BG 同色——复用 `UIViewBasePortalItem.GetBGColorByFightType(gameFightType)`：征服=白、无尽=深紫(#5B2C6F)、挑战100勇士=金(#D4AF37)；气泡由 UIHandler 缓存复用，每次 `SetData` 必须显式设置防上一次残留。

- **名字/模式/难度始终显示（不受研究门控），其余 4 项受「设施」研究门控**（`UserUnlock.CheckIsUnlock(UnlockEnum.PortalPreview*)`，**未解锁该项整行隐藏**；无尽模式**显示难度行与线路数行**(无尽有难度概念，难度2起；道路数/长度按难度走 `GetInfiniteDifficultyRandom(D)` 读取，已解锁未预生成档懒补齐，不走 `GetDifficultyRandom`——其懒生成会连带生成征服通关奖励)，**不展示关卡数/路径长度/奖励行**(无尽无关卡数与通关奖励)；**ChallengeHundred 隐藏关卡数行**(单关恒1无信息量)、**难度行显示「难度N（勇士/真勇士）」**(`string.Format(UIText 426, difficultyLevel, 424/425)`，`IsChallengeHundredBossChallenge(D)` 判定该难度普通/BOSS)，线路数量/路径长度行按 `GetChallengeHundredRandom(D)` 取该档预生成值(缺失已解锁档懒补齐)，奖励行数据源改 `GetChallengeHundredReward(D)`）：

  | 详情项 | 文本id | UnlockEnum | 值 |
  |--------|--------|-----------|----|
  | 名字 | 411 | (无门控,始终显示) | — |
  | 模式 | 421 | (无门控,始终显示,内容=模式名:征服422/无尽423/挑战100勇士417) | — |
  | 难度 | 415 | (无门控,始终显示; 征服/无尽内容=difficultyLevel, 挑战100勇士内容=难度N（勇士424/真勇士425，由 `IsChallengeHundredBossChallenge(D)` 判定普通/BOSS）) | — |
  | 线路数量 | 412 | `PortalPreviewRoadNum` | 100300002 |
  | 关卡数量 | 413 | `PortalPreviewFightNum` | 100300003 |
  | 路径长度 | 414 | `PortalPreviewRoadLength` | 100300004 |
  | 奖励道具 | — | `PortalPreviewReward` | 100300005 |

- **奖励缓存池**：以 `ui_UIViewItem` 为模板的 `listRewardItemPool`(池首=模板、不足克隆、多余隐藏)。征服**只取 `GetDifficultyReward(difficultyLevel)` 的首箱保底位(`listReward[0]`)组单件列表填充**（通关时该箱自动开启必得，其余可选箱不预览）；**ChallengeHundred 取 `GetChallengeHundredReward(difficultyLevel)` 全量(普通3件/BOSS挑战装备6件)传入 `RefreshRewardItems`**(预览=实领，通关全手动开)。改动后 `LayoutRebuilder.ForceRebuildLayoutImmediate(ui_Items)` 再整体重建。
- `UIViewPopupPortalDetailsItem.SetData(title, content, isShow)`：`isShow==false` 整行隐藏。
- `UIViewItem`(`Common/Item`)：通用道具项(图标+数量+ItemInfo悬停气泡)，子类 `UIViewItemBackpack`/`UIViewItemEquip`。

## 奖励预生成与解锁重生成（预览即实领）

- 创建传送门时 `CreateDifficultyRandom` 按难度**预生成并冻结** `listReward`(`RewardSelectBean.CreateRewardListForConquer`) + 记录 `rewardUnlockSign`(`GetConquerEquipPoolSign`)。
- `GetDifficultyReward`：`listReward` 空(老存档) 或 解锁新魔物掉落致签名变化(`rewardUnlockSign != GetConquerEquipPoolSign()`)时按配置**重新生成并刷新签名**(魔物掉落装备需研究解锁，解锁后奖励池变大→重 roll)。
- 通关 BOSS 领奖消费同一份(`GameFightLogicConquer.ActionForUIFightSettlementNext → InitDataForReward`)，保证**预览=实领**（预览只显示首箱保底奖励 `listReward[0]`——即通关领奖时落地动画播完自动开启、必得的那件）。详见 `fight-reward-system` / `conquer-system`。
- **ChallengeHundred 同契约**：`SetRandomDataForChallengeHundred` 按 `1~已解锁最高100勇士难度` **逐难度预生成** `listChallengeHundredRandom`(每档冻结行/道路/奖励/签名)；传送门创建后新解锁的难度由 `GetChallengeHundredRandom(D)` 懒补齐(`CreateChallengeHundredRandomByLevel`)；`GetChallengeHundredReward(D)` 在空或签名变化时按该难度重生成；通关领奖按本场冻结难度取同一份(`GameFightLogicChallengeHundred.ActionForUIFightSettlementNext` 调 `GetChallengeHundredReward(gameWorldInfoRandomData.difficultyLevel)`：`selectNumMax=奖励总数`、`isAutoOpenFirstBox=false`，全手动开，无首箱保底自动开)。

## 解锁相关（UnlockEnum / UserUnlockBean）

- 世界池：`GetUnlockGameWorldIds()`；传送门数量：`GetUnlockPortalShowCount()`(含 `UnlockEnum.PortalShowNum=100300001` 研究等级)。
- 征服难度上限：`GetUnlockGameWorldConquerDifficultyLevel(worldId)`。
- 详情预览门控：`UnlockEnum.PortalPreviewRoadNum/FightNum/RoadLength/Reward = 100300002~100300005`(设施研究)。新增/调整这些研究节点(配置表/解锁/多语言)见 `research-system`。
- **来袭魔物列表门控**：`UnlockEnum.PortalPreviewCreature = 100300011`(设施研究「来袭魔物预览」，前置=奖励预览 100300005，1000 魔晶，挑战100勇士与征服共用)；门控在 `UIDialogPortalDetails.SetData` → `isShowCreatureList`，未解锁时敌人/BOSS 列表整区隐藏。
- **ChallengeHundred 出现概率**：`UnlockEnum.ChallengeHundredShowRate = 100300007`(研究等级×10%=传送门刷新为该模式的概率，满级10级=100%；前置=100勇士难度2研究100310160)；取值 `UserUnlockBean.GetUnlockChallengeHundredShowRate()`。
- **ChallengeHundred BOSS挑战出现概率**：`UnlockEnum.ChallengeHundredBossRate = 100300009`(研究名「真勇者挑战」；**基础10%**+研究等级×10%，4级满级=50%；前置=是魔王就挑战100勇士出现概率研究 `pre_unlock_ids="100300007"`)；取值 `UserUnlockBean.GetUnlockChallengeHundredBossRate()`，消费在挑战100勇士抽配置行的两段判定(先按它判定普通/BOSS，再在命中类型行内等概率随机)——替代旧的"全部匹配行等概率随机"(旧逻辑 BOSS 率随行数分布漂移)。研究节点配置详见 `research-system`。
- **无尽难度解锁**：`unlock_id_infinite` 是**无尽研究起始ID**(非单值)——难度N(2~10)无尽对应解锁ID = 起始ID+(N-2)(世界1=100310102~100310110，难度2~10；第一难度没有无尽模式)。`UserUnlockBean.GetUnlockInfiniteDifficultyLevel(worldId)`：**扫描 D2~D10 取最高已解锁**(2026-10 起各难度单前置、允许跳级，不再连续计数)，起始id=0或全部未解锁返回0；`CheckInfiniteUnlock(worldId, difficultyLevel)`：难度<2 恒 false，否则查 `CheckIsUnlock(起始id+(难度-2))`。
- **100勇士难度解锁**：`unlock_id_challenge_hundred` 是**100勇士难度研究起始ID**——难度1默认可用，难度N(2~10)对应解锁ID = 起始ID+(N-2)(世界1=100310160~100310168)。`GetUnlockChallengeHundredDifficultyLevel(worldId)` 扫描取最高已解锁(最小1，世界未配置时返回1)；`CheckChallengeHundredUnlock(worldId,D)` 的 D≤1 恒 true。100勇士难度在传送门详情弹窗内可切换，气泡显示「难度N（勇士/真勇士）」。
- **无尽研究链(世界1)**：excel_research_info 新增9行无尽研究(research_type=4，icon `ui_research_155`（2026-10-25 起为无尽专属图标，原 `ui_research_11`），position x=-480/y 与征服难度链对齐 0~2560(2026-10-12 行距160→320加倍)，pay_crystal 200/1000/2000/4000/8000/16000/32000/64000/128000)：100310102(前置=100310112 征服难度2研究)~100310110，**双前置链式**(难度N无尽 前置=上一无尽节点+对应征服难度研究 100310112+N-2)；excel_unlock_info 补登记 100310103~110(100310102 原有，备注改"剑与魔法-无尽模式·难度2")。世界2/3/4 暂无征服难度研究链，本期未配无尽节点(代码天然兼容)。研究节点配置详见 `research-system`。

## 配置（Excel）

- `excel_game_world_info[游戏世界信息].xlsx`(工作表 `GameWorldInfo`) —— 世界配置唯一真实源；导出 `Assets/Resources/JsonText/GameWorldInfo.txt`。**2026-10 新增列 `unlock_id_challenge_hundred`**(100勇士难度研究起始ID：世界1=100310160，世界2~4=0；字段暂在 `GameWorldInfoBeanPartial` 临时 region，重新生成 Entity 后删除)。`unlock_id_infinite` 列值不变(各世界 x02 起)，语义为无尽研究起始ID。
- `excel_fight_type_infinite_info[战斗-无尽模式].xlsx`(工作表 `FightTypeInfiniteInfo`) —— 无尽模式配置唯一真实源；导出 `FightTypeInfiniteInfo.txt`。列：`id` / `world_id` / `level`(难度2~10，无尽无难度1) / `round_intensity_addrate`(每轮强度倍率，^(轮-1) 逐轮递增) / `remark`；当前 9 行=世界1难度2~10。Bean：`FightTypeInfiniteInfoBean.cs`(自动生成,禁改) / `FightTypeInfiniteInfoBeanPartial.cs`(`GetRoundIntensityRate`；Cfg `GetItemData(worldId,level)`/`GetMaxLevel(worldId)`)。
- `excel_fight_type_challenge_hundred_info[战斗-挑战100勇士].xlsx`(工作表 `FightTypeChallengeHundredInfo`) —— 挑战100勇士配置唯一真实源；导出 `FightTypeChallengeHundredInfo.txt`。列：`id` / `enemy_ids`(,分隔，100只怪每只独立抽取) / `difficulty_levels`(,分隔，该行适配难度列表=抽中条件) / `challenge_type`(0=普通挑战,1=BOSS挑战通关宝箱奖励翻倍) / `attack_intensity_baserate`(强度倍率) / `attack_show_time` / `road_num`(x或x-y) / `road_length` / `fight_scene_ids`(,分隔) / `drop_crystal` / `reward_crystal`(每档x或x-y) / `reward_equip_rarity` / `reward_exp` / `reward_reputation`(通关声望,逐难度对齐,当前值=难度等级,需研究100200011门控才发放) / `remark`；一行=一个挑战配置(当前 28 行=征服全部进攻敌人 14普通+14BOSS，难度=征服出场难度)；**强度倍率/击杀掉晶/每箱魔晶/装备稀有度/通关经验/通关声望为「逐难度对齐字段」**：单值=全难度共用，或与难度列表等长的逗号分隔值按冻结难度逐档取值(BeanPartial 统一解析)。
- **可视化编辑**：战斗模式编辑工具（菜单 `游戏/战斗模式编辑`）的「挑战100勇士」页签 `Assets/Editor/FightModeEditorTabChallengeHundred.cs`——**以配置行列表为主组织**(不按难度分页)：配置行下拉(`[id] [BOSS]备注 (难度:x,y)`)→加载编辑；`challenge_type` 下拉(普通/BOSS)、`difficulty_levels` 用 1~10 多选页签编辑(附实时覆盖提示)、**逐难度对齐字段按已选难度逐档独立输入框**(多难度时每难度一行；单难度/未选退化单输入框；编辑后各档相同自动折叠回单值；勾选增删难度时自动插入(继承邻档)/删除对应档值；数量不符旧数据警示)、敌人/场景 ID 列表(逗号分隔,与征服的 `&` 不同)手输或下拉按名字选取、修改字段淡黄高亮(ID 列表用「●已修改」文字标记)；支持新增行(id=max+1、适配难度沿用模板行)与删除行(EPPlus 删行)，保存/增删均单 EPPlus 会话写回 Excel(数值列写数值类型)后 `ExcelUtil.ExcelToJsonItem` 重导 JSON。
- 改配置**必须改 Excel** 再由 Unity 导出 JSON；仅改 JSON 下次导出会被覆盖。

## 关键文件

| 文件 | 路径 |
|------|------|
| 基地地图(传送门容器) | Assets/Scripts/Component/UI/Game/BasePortal/UIBasePortal.cs |
| 单个传送门item | Assets/Scripts/Component/UI/Game/BasePortal/UIViewBasePortalItem.cs |
| 难度选择对话框 | Assets/Scripts/Component/UI/Dialog/UIDialogPortalDetails.cs |
| 难度item | Assets/Scripts/Component/UI/Dialog/PortalDetails/UIViewDialogPortalDetailsItem.cs |
| 来袭魔物item(挑战100勇士/征服, BOSS红底) | Assets/Scripts/Component/UI/Dialog/PortalDetails/UIViewDialogPortalDetailsCreatureItem.cs (+Component.cs) |
| 详情气泡(名字/模式/难度+4预览+奖励,门控) | Assets/Scripts/Component/UI/Popup/UIPopupPortalDetails.cs |
| 详情项 | Assets/Scripts/Component/UI/Popup/PortalDetails/UIViewPopupPortalDetailsItem.cs |
| 世界配置 Bean(禁改) | Assets/Scripts/Bean/MVC/Game/GameWorldInfoBean.cs |
| 传送门随机数据 | Assets/Scripts/Bean/MVC/Game/GameWorldInfoBeanPartial.cs |
| 挑战100勇士配置 Bean(禁改)+Partial | Assets/Scripts/Bean/MVC/Game/FightTypeChallengeHundredInfoBean.cs / FightTypeChallengeHundredInfoBeanPartial.cs |
| 挑战100勇士战斗数据 | Assets/Scripts/Bean/Game/FightBeanForChallengeHundred.cs |
| 无尽模式配置 Bean(禁改)+Partial | Assets/Scripts/Bean/MVC/Game/FightTypeInfiniteInfoBean.cs / FightTypeInfiniteInfoBeanPartial.cs |
| 无尽模式战斗数据 | Assets/Scripts/Bean/Game/FightBeanForInfinite.cs |
| 传送门数据缓存 | Assets/Scripts/Bean/Game/UserTempBean.cs (listPortalWorldInfoRandomData) |
| 世界配置 Excel | Assets/Data/Excel/excel_game_world_info[游戏世界信息].xlsx |
| 导出 JSON | Assets/Resources/JsonText/GameWorldInfo.txt |
| 挑战100勇士配置 Excel | Assets/Data/Excel/excel_fight_type_challenge_hundred_info[战斗-挑战100勇士].xlsx |
| 导出 JSON | Assets/Resources/JsonText/FightTypeChallengeHundredInfo.txt |
| 无尽模式配置 Excel | Assets/Data/Excel/excel_fight_type_infinite_info[战斗-无尽模式].xlsx |
| 导出 JSON | Assets/Resources/JsonText/FightTypeInfiniteInfo.txt |
| 挑战100勇士配置编辑器 | Assets/Editor/FightModeEditorWindow.cs (主窗口,菜单:游戏/战斗模式编辑) + Assets/Editor/FightModeEditorTabChallengeHundred.cs (挑战100勇士页签) |

## 约束

- 配置变更**必须改 Excel**(`excel_game_world_info`)，由 Unity 导出 JSON；仅改 JSON 下次导出被覆盖。
- `GameWorldInfoBean.cs` 自动生成**禁改**；扩展写 `GameWorldInfoBeanPartial.cs`。`GameWorldInfoRandomBean`/`GameWorldDifficultyRandomBean` 是手写 Bean，可直接改。
- 地图位置用 `Vector2Bean` 包装序列化(规避 Newtonsoft 序列化 Vector2 的 normalized 递归栈溢出)。
- `InitMap` 是"补足不洗牌"语义：别改成每次重随(会让已解锁数量/缓存失效、每次打开都变)；要重洗走 `OnClickForRefresh`。
- 气泡名字/难度始终显示(不门控)，线路数/关卡数/路径长度/奖励四项**未解锁整行隐藏**(不是占位)；无尽模式有难度概念(难度2起)：显示难度/线路数行，隐藏关卡数/路径长度/奖励行(无尽无关卡数与通关奖励)；ChallengeHundred 隐藏关卡数行(单关恒1无信息量)、难度行显示「勇士/真勇士」标签，奖励行全量预览(普通3箱/BOSS装备6箱；征服仅首箱保底 `listReward[0]`)。
- ChallengeHundred 自 2026-10 起**有独立难度概念**：难度1默认可用，难度2~10 由 `unlock_id_challenge_hundred` 研究链解锁；传送门详情弹窗**同时显示难度选择器与来袭魔物列表**(难度在上、魔物在下)，逐档解锁判断、左右切换跳过未解锁空档；`difficultyLevel` 为当前活动难度，冻结该难度的配置行/道路/奖励，强度由配置行 `attack_intensity_baserate` 按该难度取档。传送门名字仍显示模式名(UIText 417)而非世界名、确认文案用 416。文本 id：416=「是否接受100勇士的挑战？」、417=「是魔王就挑战100勇士」、418=「来袭魔物」(预留标题文案，当前代码未用)、424=「勇士」、425=「真勇士」、426=「{0}（{1}）」(气泡难度行格式，EN等半角括号)。
- ChallengeHundred 预生成数据按难度存 `listChallengeHundredRandom`；活动字段 `challengeHundredRowId/listRewardChallengeHundred/rewardUnlockSignChallengeHundred` 是当前难度的镜像，切难度必须走 `SetDifficultyLevel` 同步，否则战斗会用到旧难度行。
- 预生成列表只在传送门创建时按当时的已解锁难度建好，**研究解锁新难度不会重建列表**：取用时一律走 `GetChallengeHundredRandom(D)`/`GetChallengeHundredReward(D)`/`GetInfiniteDifficultyRandom(D)`(内部懒补齐已解锁缺档)，不要自己 `list.Find` 或新增"只读已预生成"的分支，否则新解锁难度会预览/战斗/结算全取不到数据。
- **战斗场地（fightSceneId）与道路/关卡同契约：创建传送门时按难度冻结**。FightBean 里**禁止**再写 `fightTypeConquerInfo.GetRandomFightScene(...)`/`fightTypeChallengeHundredInfo.GetRandomFightScene()`——一律走 `gameWorldInfoRandomData.GetFightSceneId(difficultyLevel, isBoss)`，否则同一世界重进场地会重新随机（用户反馈过的 bug：传送门世界每次重进场地都变）。征服普通关/BOSS关各一份冻结值，无尽取 `bossFightSceneId`，挑战100勇士取该难度冻结行场景；老存档字段为0时由 `GetFightSceneId` 懒补齐写回。
- 输入走 `InputActionUIEnum`(ESC/Navigate)，禁用旧版 `Input` API。

## 关联 Skill 与 Agent

- 进入后的征服战斗(多关卡/BOSS/结算): `game-conquer` agent + `conquer-system` skill
- 无尽等其它战斗模式: `game-fight-logic` agent + `game-fight-system` skill
- 通关奖励生成规则/领奖: `game-fight-reward` agent + `fight-reward-system` skill
- 预览门控/数量研究节点配置与解锁: `game-research` agent + `research-system` skill
- 对话框/气泡 UI 基类: `ui-dialog` / `ui-popup` agent + `ui-framework` skill
- 基地相机(SetBasePortalCamera): `system-camera` agent + `camera-system` skill
- 世界配置表导入导出: `data-excel` agent + `excel-io` skill
- 传送门奖励预生成跨系统速查: 见记忆 `reference_portal_reward_pregen_research_gate`
