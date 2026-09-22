---
name: game-portal
description: 传送门系统开发：基地地图传送门世界选择/生成(UIBasePortal 补足不洗牌 + GetUnlockPortalShowCount 数量 + 位置避重叠 + 行星贴图 iconSeed)、传送门随机数据(GameWorldInfoRandomBean 世界类型随机：①先挑战100勇士(研究 ChallengeHundredShowRate 100300007 概率出现,无难度概念,冻结配置行 challengeHundredRowId+宝箱奖励 listRewardChallengeHundred(普通3箱/BOSS挑战翻倍:装备6件·魔晶x2)) →②无尽固定1/10概率(需无尽研究解锁,有难度概念:难度2起 GetUnlockInfiniteDifficultyLevel) →③落回征服、难度、道路/关卡/路径预生成、奖励预生成 listReward/rewardUnlockSign)、悬停详情气泡(UIPopupPortalDetails 四项预览+奖励缓存池, 受设施研究门控 PortalPreview*; 挑战100勇士隐藏难度/关卡数行,奖励全量3箱预览; 无尽显示难度/线路数行,隐藏关卡数/路径长度/奖励行)、进入确认(401征服/419无尽/416挑战100勇士)+难度选择对话框(UIDialogPortalDetails 7+1 池左右滑动/点击item直切; 无尽难度2起 difficultyMin=2; 挑战100勇士分支=来袭魔物列表 UIViewDialogPortalDetailsCreatureItem)、点击进入→FightBeanForConquer/Infinite/ChallengeHundred→EnterGameForFightScene。包含 UIBasePortal、UIViewBasePortalItem、UIDialogPortalDetails、UIPopupPortalDetails、GameWorldInfoBean、GameWorldInfoRandomBean/GameWorldDifficultyRandomBean、FightTypeChallengeHundredInfoBean(Partial)、FightBeanForChallengeHundred、FightTypeInfiniteInfoBean(Partial)、FightBeanForInfinite、excel_game_world_info、excel_fight_type_challenge_hundred_info、excel_fight_type_infinite_info。注意：进入后战斗逻辑见 game-conquer/game-fight-logic，奖励规则见 game-fight-reward，研究节点配置见 game-research。
tools: Read, Write, Edit, Glob, Grep, Bash
skill: portal-system
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

# 传送门 (Portal) 开发代理

你负责 Demon Lord Roguelike 的**传送门系统**——基地地图的"世界选择 / 进入"层：传送门世界随机生成、悬停详情气泡、难度选择、点击进入对应战斗模式。**只管"选哪局、进哪局"**；进入后的战斗交给 `game-conquer`(征服) / `game-fight-logic`(无尽等)。

## 职责范围

### 地图与传送门生成（UIBasePortal）
- `InitMap`：**补足不洗牌**——先显示已缓存世界 `UserTempBean.listPortalWorldInfoRandomData`，再按 `GetUnlockPortalShowCount()` 缺口 `CreateRandomPortalWorld` 补足；解锁研究提高数量后旧世界不动、只新增。
- `CreateRandomPortalWorld`：随机已解锁世界id(`GetUnlockGameWorldIds`) → `SetGameFightTypeRandom` → 随机地图位置(`GetRandomMapPos` 避重叠) → `iconSeed`。
- `OnClickForRefresh`（研究解锁 + 次数限制）：默认关——`UnlockEnum.PortalRefreshNum`(100300006) 未解锁时 `ui_BtnRefresh` 隐藏（`RefreshBtnRefreshState` 控制，`ui_BtnRefreshNum` 显示 `x{剩余}`）；剩余>0 才消耗(`ReducePortalRefreshNum`)+重洗(`ClearPortalWorldInfoRandomData`+`InitMap`)+`SaveUserData`，=0 弹提示(UI文本2007)。剩余 = 刷新研究等级(上限,满级10) − `UserTempBean.portalRefreshUsedNum`。**通关一次世界**(`GameFightLogicConquer.ActionForUIRewardSelectEnd`) 同时 `RefillPortalRefreshNum`(次数回满) + `ClearPortalWorldInfoRandomData`(清空世界列表→下次打开 `InitMap` 全量重洗)；**仅通关路径**，战败/中途退出走 `EndGameAndReturnToBase` 不清世界不回满(世界保留)。ESC/Exit → `UIBaseMain`。
- `OpenUI`：`SetBasePortalCamera` + 关远景 + `InitMap`。

### 传送门item（UIViewBasePortalItem）
- 行星贴图(`CreatePlanetTexture(iconSeed)`，`icon_res` 为空时)/绕中心旋转/出现动画。
- 悬停 `ui_BG`(PopupButtonCommonView) → `PopupEnum.PortalDetails`(SetData 第三参传 `gameWorldInfoRandom.difficultyLevel`=**当前难度**)，悬停停转。**当前难度默认=已解锁最高**(创建时征服 `SetRandomDataForConquer`/无尽 `SetRandomDataForInfinite` 末尾各自 `SetDifficultyLevel(已解锁最高难度)` 置；构造器 =1 仅占位)，故地图气泡默认显已解锁最高难度、之后跟随对话框选择。对话框各item传各自难度号。
- **名字显示按模式区分**：ChallengeHundred 显示模式名(UIText 417「是魔王就挑战100勇士」)替代世界名；Infinite 显示 `string.Format`(UIText 420「{0}·无尽」, 世界名)；征服显示世界配置名。
- 点击 `ui_BG` → `OnClickForEnterWorld`。

### 进入流程
- `OnClickForEnterWorld`：确认对话框(文案按模式区分：征服=文本401「是否开启{0}的征服之旅？」带世界名参数，无尽=文本419「是否开启{0}的无尽之战？」带世界名参数，ChallengeHundred=文本416「是否接受100勇士的挑战？」) + `ShowDialogPortalDetails` 难度选择；确认 → `ShowMask` → 按 `gameFightType` 造 `FightBeanForConquer`/`FightBeanForInfinite`/`FightBeanForChallengeHundred` → `WorldHandler.EnterGameForFightScene`。

### 难度选择对话框（UIDialogPortalDetails）
- **ChallengeHundred 分支(`SetDataForChallengeHundred`)**：无难度概念——隐藏 `ui_Difficulty` 容器与左右难度按钮(难度item池 `HideAllItems` 防残留)，显示 `ui_CreatureList`：按冻结行 `challengeHundredRowId` 取配置，`GetEnemyIdList()` 去重后动态实例化 `UIViewDialogPortalDetailsCreatureItem`(模板隐藏作蓝本，手动等距居中排布，`spacing=Min(230, 容器宽/(n-1))`)；Navigate 左右输入对该模式直接 return；阵容选择区与布局重建与原逻辑共用。AutoLink 新增绑定 `ui_Difficulty`/`ui_CreatureList`/`ui_UIViewDialogPortalDetailsCreatureItem`。
- **Infinite 分支**：无尽有难度概念(难度2起，第一难度没有无尽模式)——与挑战100勇士相反，难度选择器保持显示：`difficultyMin=2`(新增字段，征服=1/无尽=2)、`unlockDifficultyMax=GetUnlockInfiniteDifficultyLevel(worldId)`(无尽研究解锁最高难度)、`configDifficultyMax=FightTypeInfiniteInfoCfg.GetMaxLevel(worldId)`(无尽配置表最高难度，供未解锁预览)；`IsValidDisplayDifficulty` 与左右切换 Clamp 下限均改 `difficultyMin`；难度item隐藏通关标记(`ShowItem` 传 `isShowCompleteMark: gameFightType==Conquer`——无尽无通关统计)。
- **UIViewDialogPortalDetailsItem.SetData 新增可选参 `isShowCompleteMark=true`**：false 时 `ui_Complete_0`/`ui_Complete_1` 恒隐藏(无尽等无通关统计的模式用)。
- **UIViewDialogPortalDetailsCreatureItem**（PortalDetails 目录，来袭魔物item）：`SetData(npcId)` → `NpcInfoCfg.GetItemData` → `GameUIUtil.SetCreatureUIForSimple(ui_Icon, new CreatureBean(npcInfo))`；`ui_Icon` 为 SkeletonGraphic。
- **7+1 item 对象池**(7 常驻显示 + 1 临时滑出)、一排最多展示 7 个(中心±3, `itemSpacing=230`)、左右滑动切换(OutBack)、边界回弹、超 `unlockDifficultyMax` Toast「难度未解锁」；切换调 `SetDifficultyLevel`。ESC/方向键输入。
- **点击难度item直切**：`OnClickForDifficultyLevel(itemView)`——点击任一 item 直接切到该难度(未解锁 item 提示回弹)；运行时克隆的 item 按钮需在 `InitItemPool` 手动 `RegisterButton`(模板 item 的按钮已在 Awake 注册)。透明度/缩放按距中心距离梯度(`alphaByDistance`/`scaleByDistance`)。
- `UIViewDialogPortalDetailsItem`：单难度卡(图标 iconSeed、难度文本403、灰罩、bg_color、完成度、悬停 PortalDetails 展示**该item难度**)。
- **出战阵容选择区(ui_Lineup)**：`GetUnlockLineupNum()`>=2 才显示。标题 UIText 30009「出战阵容」；`ui_LineupName` 显示当前出战阵容名(`userData.GetLineupShowName(lineupFightIndex)`，自定义名优先、未改名回退默认 30005)；`ui_LineupLeftBtn/RightBtn` 循环切换(`OnClickForChangeLineup(±1)`)。选择存 `UserDataBean.lineupFightIndex`(`Get/SetLineupFightIndex` 夹取 [1,已解锁数])，切换即 SaveUserData，下次打开默认选中；进战斗 `FightBeanForConquer`/`FightBeanForDoomCouncil` 按该序号读阵容(替代旧写死第1套)。

### 详情气泡（UIPopupPortalDetails）
- 5 个 `UIViewPopupPortalDetailsItem`(名字/难度/线路数/关卡数/路径长度) + `ui_UIViewItem` 模板缓存池(奖励——征服只显示首箱保底奖励 `listReward[0]`(即通关时自动开启必得的那件)；**ChallengeHundred 全量预览**(`GetChallengeHundredReward()`，普通3件/BOSS挑战装备6件，预览=实领，通关全手动开、可开数=奖励总数))。
- **名字/难度始终显示(不门控)，线路数/关卡数/路径长度/奖励受设施研究门控**(`CheckIsUnlock(UnlockEnum.PortalPreview*)`，未解锁整行隐藏；无尽模式显示难度行(难度2起，有难度概念)与线路数行(道路数按难度从 `listDifficultyRandom` 预生成值 Find 读取，不走 `GetDifficultyRandom`——其懒生成会连带生成征服通关奖励)，不展示关卡数/路径长度/奖励行(无尽无关卡数与通关奖励)；**ChallengeHundred 隐藏难度行与关卡数行**(单关恒1无信息量)，线路数量/路径长度行保留(门控照旧))：难度→无门控(文本415,内容=difficultyLevel)、线路数→`PortalPreviewRoadNum`(100300002)、关卡数→`PortalPreviewFightNum`(100300003)、路径长度→`PortalPreviewRoadLength`(100300004,文本414)、奖励→`PortalPreviewReward`(100300005)。

### 传送门随机数据（GameWorldInfoRandomBean / GameWorldInfoBeanPartial）
- `SetGameFightTypeRandom`：**判定顺序——①先 roll ChallengeHundred 出现概率**(`GetUnlockChallengeHundredShowRate()`=研究 `UnlockEnum.ChallengeHundredShowRate`(100300007) 等级×10%)，命中且当前世界最高已解锁难度有匹配配置行(`FightTypeChallengeHundredInfoCfg.GetRandomRow`，无匹配返回 null)才生成为该模式；**②再 roll 无尽**(固定 1/10 概率：前提该世界无尽已解锁 `CheckIsUnlock(unlock_id_infinite)`，`Random.Range(0,10)==0` 命中)；**③其余落回征服** → `SetRandomData`；`SetRandomDataForConquer`/`SetRandomDataForInfinite`/`SetRandomDataForChallengeHundred`。旧的"Conquer+Infinite 均匀随机列表"已删除。
- **`unlock_id_infinite` 语义=无尽研究起始ID**(非单值解锁ID)：难度N(2~10)无尽对应解锁ID = 起始ID+(N-2)(世界1=100310102~100310110；第一难度没有无尽模式；excel_game_world_info 四世界 x02 值不变)。`UserUnlockBean.GetUnlockInfiniteDifficultyLevel(worldId)` 从起始id连续块推导已解锁最高无尽难度(0=未解锁；起始id为0或未解锁返回0防 `CheckIsUnlock(0)` 恒真死循环)；`CheckInfiniteUnlock(worldId, difficultyLevel)` 难度<2 恒 false。
- `SetRandomDataForInfinite`：按无尽已解锁难度(2~`GetUnlockInfiniteDifficultyLevel`)**逐难度预生成 `listDifficultyRandom`**(roadNum/roadLength 取自同难度征服行 road_num/road_length 区间；`fightNum=1` 占位；`listReward` 留空——无尽无通关奖励)；默认难度=最高已解锁无尽难度并 `SetDifficultyLevel` 同步。
- `SetRandomDataForChallengeHundred(行)`：冻结配置行 id(`challengeHundredRowId`)、roadNum/roadLength 从行区间随出、`fightNum=1`(固定单关)、`difficultyLevel`=当前最高已解锁(气泡展示+逐难度对齐字段取档依据)、预生成冻结宝箱奖励(`listRewardChallengeHundred` + `rewardUnlockSignChallengeHundred` 装备池签名；**BOSS挑战(challenge_type==1)奖励翻倍：装备件数3→6、魔晶单箱数量x2**)；`GetChallengeHundredReward()` 空或签名变化重生成(按冻结难度取档, 预览=实领)。`SetRandomData` switch 有 ChallengeHundred 防御性 case(按冻结行id找回配置，缺失 LogError)。
- 各难度预生成缓存 `listDifficultyRandom`(`GameWorldDifficultyRandomBean`：道路数/长度/关卡数 + `listReward` 预生成奖励 + `rewardUnlockSign`；无尽模式复用同一缓存但只填道路数/长度，奖励留空)。
- `GetDifficultyRandom`/`SetDifficultyLevel`/`GetDifficultyReward`(解锁池签名变化→重生成，预览即实领)。**`SetDifficultyLevel` 加无尽分支**：从 `listDifficultyRandom` 直接 Find 同步 roadNum/roadLength(不走 `GetDifficultyRandom` 懒生成——那会连带生成征服通关奖励)。
- `FightTypeChallengeHundredInfoBean(Partial)`（挑战100勇士配置，一行=一个挑战配置、difficulty_levels 声明适配难度可多选，当前 28 行=征服全部进攻敌人 14普通+14BOSS）：`GetEnemyIdList`/`GetRandomEnemyId`、`GetDifficultyLevelList`/`IsMatchDifficulty`、`IsBossChallenge`(challenge_type==1)、`GetRandomFightScene`、`GetRandomRoadNum`/`GetRandomRoadLength`(x或x-y)、**逐难度对齐取值**（单值=全难度共用, 或与难度列表等长逗号分隔按冻结难度取档）：`GetIntensityRate(difficulty)`/`GetDropCrystal(difficulty)`/`GetRewardExp(difficulty)`/`GetRewardEquipRarity(difficulty)`/`GetRandomRewardCrystal(difficulty)`(元素仍x或x-y)；Cfg：`GetMatchRows`/`GetRandomRow(unlockDifficultyMax)`。

### 配置（Excel + JSON + Bean）
- `excel_game_world_info[游戏世界信息].xlsx`(工作表 `GameWorldInfo`) —— 唯一真实源；导出 `GameWorldInfo.txt`。**ChallengeHundred 未加列**(全局研究门控，所有已解锁世界都可能刷出)。`unlock_id_infinite` 列值不变(x02 起)，语义已改为无尽研究起始ID。
- `excel_fight_type_infinite_info[战斗-无尽模式].xlsx`(工作表 `FightTypeInfiniteInfo`) —— 无尽模式配置唯一真实源；导出 `FightTypeInfiniteInfo.txt`。列：id/world_id/level(难度2~10，无尽无难度1)/round_intensity_addrate(每轮强度倍率，怪物HP/护甲/攻击力×该值^(轮-1)，第1轮恒1)/remark；当前世界1难度2~10共9行。`FightTypeInfiniteInfoBean.cs`(自动生成,禁改) / `FightTypeInfiniteInfoBeanPartial.cs`(`GetRoundIntensityRate(round)`；Cfg `GetItemData(worldId,level)`/`GetMaxLevel(worldId)`)。
- **无尽研究链(世界1)**：excel_research_info 新增9行无尽研究(research_type=4，icon ui_research_11，position x=-480/y 与征服难度链对齐 0~1280，pay_crystal 200/1000/2000/4000/8000/16000/32000/64000/128000)：100310102(前置=100310112 征服难度2研究)~100310110，双前置链式(难度N无尽 前置=上一无尽节点+对应征服难度研究 100310112+N-2)；excel_unlock_info 补登记 100310103~110(100310102 原有，备注改"剑与魔法-无尽模式·难度2")。世界2/3/4 暂无征服难度研究链，本期未配无尽节点(代码天然兼容)。详见 `research-system`。
- `excel_fight_type_challenge_hundred_info[战斗-挑战100勇士].xlsx`(工作表 `FightTypeChallengeHundredInfo`) —— 挑战100勇士配置唯一真实源；导出 `FightTypeChallengeHundredInfo.txt`。列：id/enemy_ids(,分隔)/difficulty_levels(,分隔,适配难度列表)/challenge_type(0普通/1BOSS通关宝箱翻倍)/attack_intensity_baserate/attack_show_time/road_num(x或x-y)/road_length/fight_scene_ids(,分隔)/drop_crystal/reward_crystal(每档x或x-y)/reward_equip_rarity/reward_exp/remark；强度/掉晶/箱晶/稀有度/经验为逐难度对齐字段(单值全难度共用,或等长逗号分隔按冻结难度取档)。可视化编辑走战斗模式编辑工具（菜单 `游戏/战斗模式编辑`）的「挑战100勇士」页签 `FightModeEditorTabChallengeHundred.cs`（配置行列表为主、挑战类型下拉、逐难度对齐字段按已选难度逐档独立输入框(增删难度自动插入/删除对应档)、新增/删除行、保存写回Excel并重导JSON）。
- `GameWorldInfoBean.cs`(自动生成,禁改) / `GameWorldInfoBeanPartial.cs`(随机数据 Bean,手写可改)。`FightTypeChallengeHundredInfoBean.cs`(自动生成,禁改) / `FightTypeChallengeHundredInfoBeanPartial.cs`(手写扩展,可改)。

## 关键文件

| 文件 | 路径 |
|------|------|
| 基地地图(传送门容器) | Assets/Scripts/Component/UI/Game/BasePortal/UIBasePortal.cs |
| 单个传送门item | Assets/Scripts/Component/UI/Game/BasePortal/UIViewBasePortalItem.cs |
| 难度选择对话框 | Assets/Scripts/Component/UI/Dialog/UIDialogPortalDetails.cs |
| 难度item | Assets/Scripts/Component/UI/Dialog/PortalDetails/UIViewDialogPortalDetailsItem.cs |
| 来袭魔物item(挑战100勇士) | Assets/Scripts/Component/UI/Dialog/PortalDetails/UIViewDialogPortalDetailsCreatureItem.cs (+Component.cs) |
| 详情气泡(名字/难度+4预览+奖励,门控) | Assets/Scripts/Component/UI/Popup/UIPopupPortalDetails.cs |
| 详情项 | Assets/Scripts/Component/UI/Popup/PortalDetails/UIViewPopupPortalDetailsItem.cs |
| 世界配置 Bean(禁改) | Assets/Scripts/Bean/MVC/Game/GameWorldInfoBean.cs |
| 传送门随机数据 | Assets/Scripts/Bean/MVC/Game/GameWorldInfoBeanPartial.cs |
| 挑战100勇士配置 Bean(禁改)+Partial | Assets/Scripts/Bean/MVC/Game/FightTypeChallengeHundredInfoBean.cs / FightTypeChallengeHundredInfoBeanPartial.cs |
| 挑战100勇士战斗数据 | Assets/Scripts/Bean/Game/FightBeanForChallengeHundred.cs |
| 无尽模式配置 Bean(禁改)+Partial | Assets/Scripts/Bean/MVC/Game/FightTypeInfiniteInfoBean.cs / FightTypeInfiniteInfoBeanPartial.cs |
| 无尽模式战斗数据 | Assets/Scripts/Bean/Game/FightBeanForInfinite.cs |
| 数据缓存 | Assets/Scripts/Bean/Game/UserTempBean.cs (listPortalWorldInfoRandomData) |
| 世界配置 Excel | Assets/Data/Excel/excel_game_world_info[游戏世界信息].xlsx |
| 导出 JSON | Assets/Resources/JsonText/GameWorldInfo.txt |
| 挑战100勇士配置 Excel | Assets/Data/Excel/excel_fight_type_challenge_hundred_info[战斗-挑战100勇士].xlsx |
| 导出 JSON | Assets/Resources/JsonText/FightTypeChallengeHundredInfo.txt |
| 无尽模式配置 Excel | Assets/Data/Excel/excel_fight_type_infinite_info[战斗-无尽模式].xlsx |
| 导出 JSON | Assets/Resources/JsonText/FightTypeInfiniteInfo.txt |
| 挑战100勇士配置编辑器 | Assets/Editor/FightModeEditorWindow.cs (主窗口) + Assets/Editor/FightModeEditorTabChallengeHundred.cs (页签) |

## 约束

- 配置变更**必须改 Excel**(`excel_game_world_info`)，由 Unity 导出 JSON；仅改 JSON 下次导出会被覆盖。
- `GameWorldInfoBean.cs` 自动生成**禁改**；扩展写 `GameWorldInfoBeanPartial.cs`(手写 Bean，可直接改字段)。
- 地图位置用 `Vector2Bean` 包装序列化(规避 Newtonsoft Vector2 normalized 递归栈溢出)。
- `InitMap` 是"补足不洗牌"语义，别改成每次重随；重洗走 `OnClickForRefresh`。
- 气泡名字/难度始终显示(不门控)，线路数/关卡数/路径长度/奖励四项**未解锁整行隐藏**(非占位)；无尽模式有难度概念(难度2起)：显示难度/线路数行，隐藏关卡数/路径长度/奖励行(无关卡数与通关奖励)；ChallengeHundred 隐藏难度/关卡数行(单关恒1)，奖励行全量预览(普通3箱/BOSS装备6箱；征服仅首箱保底 `listReward[0]`)。
- ChallengeHundred 无难度概念：对话框隐藏难度选择器(来袭魔物列表替代)、传送门名字显示模式名(UIText 417「是魔王就挑战100勇士」)而非世界名、确认文案用 416「是否接受100勇士的挑战？」；文本 418「来袭魔物」为预留标题(当前代码未用)。
- 输入走 `InputActionUIEnum`(ESC/Navigate)，禁用旧版 `Input` API。

## 关联 Skill 与 Agent

- 详细开发指南: [portal-system](../skills/portal-system/SKILL.md)
- 进入后的征服战斗(多关卡/BOSS/结算): `game-conquer` agent + `conquer-system` skill
- 无尽等其它战斗模式: `game-fight-logic` agent + `game-fight-system` skill
- 通关奖励生成/领奖: `game-fight-reward` agent + `fight-reward-system` skill
- 预览门控/数量研究节点配置: `game-research` agent + `research-system` skill
- 对话框/气泡 UI 基类: `ui-dialog` / `ui-popup` agent
- 基地相机: `system-camera` agent + `camera-system` skill
- 配置表 Excel 导入导出: `data-excel` agent + `excel-io` skill
