---
name: game-research
description: 研究模块开发：基地研究界面（设施/强化/魔物/世界四大分支）、研究节点解锁、研究等级、连线绘制、解锁条件判定、ResearchInfo 配置、UserUnlock 存档。
tools: Read, Write, Edit, Glob, Grep, Bash
skill: research-system
watched_files:
  - Assets/Scripts/Component/UI/Game/BaseResearch/
  - Assets/Scripts/Component/UI/Popup/UIPopupResearchInfo.cs
  - Assets/Scripts/Component/UI/Popup/UIPopupResearchInfoComponent.cs
  - Assets/Scripts/Bean/MVC/Game/ResearchInfoBean.cs
  - Assets/Scripts/Bean/MVC/Game/ResearchInfoBeanPartial.cs
  - Assets/Scripts/Bean/MVC/Game/UnlockInfoBean.cs
  - Assets/Scripts/Bean/Game/UserUnlockBean.cs
  - Assets/Data/Excel/excel_research_info[研究信息].xlsx
  - Assets/Data/Excel/excel_unlock_info[解锁信息].xlsx
---

# 研究模块 (Research) 开发代理

你负责 [Scripts/Component/UI/Game/BaseResearch/](Assets/Scripts/Component/UI/Game/BaseResearch/) 中的研究模块开发，包括研究界面、研究节点解锁、研究等级、连线绘制、解锁条件判定、研究配置读取以及对应的玩家解锁存档。

## 职责范围

### 研究 UI

- **UIBaseResearch** - 基地研究主界面，承载四大研究分支（设施 / 强化 / 魔物 / 世界）的切换、Tab 选择、缩放交互
- **UIBaseResearchTest** - 研究界面的编辑器调试模式，用于在游戏内调整节点坐标并回写 Excel 配置
- **UIViewBaseResearchItem** - 单个研究节点的展示（图标、等级、解锁状态、解锁动画、点击购买、可购买高亮）。悬停效果：prefab 根下新建 `CardContent` 子节点（全拉伸 RectTransform，BG/Board/Icon/Level 全部移入）并直接挂载框架层通用组件 `UIHoverCardView`（非代码挂载），实现小丑牌风格悬停（弹起放大+上抬+朝光标 3D 倾斜+弹簧甩动回摆+悬停静止持续摆动）；上抬/倾斜只动 CardContent 与根节点的研究树布局坐标(SetPosition)天然隔离，解锁动画 `AnimForUnlock` 操作根 transform 亦不冲突，无需抑制协调
- **UIPopupResearchInfo** - 研究节点悬浮气泡，展示名称+details 详情拼接（`GetDetailsLanguageWithLevelDetail` 按待解锁等级填累计效果数值）、图标、当前/最大等级、需要支付的水晶、前置解锁条件

### 研究数据

- **ResearchInfoBean / ResearchInfoCfg** - 研究节点配置（自动生成），不可直接修改
- **ResearchInfoBeanPartial** - 扩展方法：前置解锁解析（`GetPreUnlockIdsForLine`）、类型枚举映射（`GetResearchType`）、阶梯水晶价格计算（`GetPayCrystal`）、详情描述填充（`GetDetailsLanguageWithLevelDetail(currentLevel)`——读研究表 `details[language]` 列对应的详情模板，把 `{Value}` 占位替换为「待解锁等级=min(当前+1,满级)」的**累计总效果**数值（走通用 `TextHandler.GetTextReplace` + `TextReplaceEnum.Value`，与成就 `GetLevelDescription` 同口径），满级停留满级数值；数值公式统一收口 `UserUnlockBean` 的 static `Get*ForLevel` 方法，私有 `GetLevelDetailValueString(targetLevel)` 按 unlock_id if/else 分发 22 个 level_max>1 节点）；气泡名称与详情直接拼接（`UIPopupResearchInfo.SetData`，括号/前导空格由各语言详情文本自带：cn/tw/jp 全角无空格、其余半角带前导空格）
- **details 详情描述机制（2026-10 新增，取代旧的名字模板 `{Value}` 写法）**：研究表新增 `details[language]` 列（long，语言 id，0=无详情，位于 name 与 remark 之间）；22 个 level_max>1 节点的详情语言条目占号段 **900000001~900000022**（excel_language ResearchInfo 工作表，12 语言全翻译，SpaceDash/CD/挑战100勇士三条的详情从原名字译文剪取而来、名字已还原纯文本）；**`details` 字段暂在 Partial（含 `[JsonIgnore] details_language` LanguageCache 懒查属性，仿 pre_data 先例），重新生成 ResearchInfoBean.cs 后需删除该临时 region**（生成物自带 details/details_language 且 `CombineModReferenceIds` 自动补 details 拼接——过渡期 Mod 研究行的 details 语言 id 不拼接，现有 Mod 无 ResearchInfo 行无实际影响）；prefab 内预留未绑定的 `DetailsText` 孤儿节点（380 宽独立行），若日后嫌名字挤可拆到该行（需改 prefab+AutoLink）
- **UnlockInfoBean / UnlockInfoCfg** - 解锁条目配置（区分 0 研究 / 1 扭蛋机）
- **ResearchInfoTypeEnum** - 研究类型枚举（Building / Strengthen / Creature / World）
- **UnlockEnum** - 关键模块解锁 ID 枚举（生物进阶、祭坛、终焉议会、阵容数、扭蛋稀有度等）
  - 设施段(1002) 征服通关获得声望：`ConquerReputationReward`(100200004，`research_type=1`，`pre_unlock_ids="100200001"` 前置=终焉议会 `DoomCouncil`，`level_max=1`，`pay_crystal=1000`)——「解锁开关驱动游戏逻辑」范例：解锁后玩家每次完整通关征服模式按难度加自身声望，逻辑门控在 `GameFightLogicConquer.AddReputationForConquerComplete`(`CheckIsUnlock(UnlockEnum.ConquerReputationReward)` 才 `userData.AddReputation(conquerInfo.GetRewardReputation())`)，声望值取征服难度表新增列 `reward_reputation`。详见 `game-conquer`/`game-fight-reward`
  - 设施段(1003) 传送门详情预览 4 节点：`PortalPreviewRoadNum`(100300002 线路数) / `PortalPreviewFightNum`(100300003 关卡数) / `PortalPreviewRoadLength`(100300004 路径长度) / `PortalPreviewReward`(100300005 奖励道具)，均为 `research_type=1` 设施节点，门控传送门详情弹窗 `UIPopupPortalDetails` 各项是否显示
  - 设施段(1003) 传送门刷新：`PortalRefreshNum`(100300006，`research_type=1`，`level_max=10`)——研究等级=传送门地图刷新次数上限，未解锁则 `UIBasePortal` 刷新按钮隐藏，通关一次世界回满(`GameFightLogicConquer.ActionForUIRewardSelectEnd` 调 `RefillPortalRefreshNum`)
  - 世界段(1003) 挑战100勇士出现概率：`ChallengeHundredShowRate`(100300007，`research_type=4`，`level_max=10`，`icon_res=ui_research_9`，position(-160,-160) 与难度链同列、前置 100310112 正下方，`pre_unlock_ids="100310112"` 前置=剑与魔法征服难度2研究(同分支)，`pay_crystal` 十级独立阶梯 100,200,400,800,1500,2500,4000,6000,9000,15000)——**概率型研究消费点先例**(仿扭蛋概率)：研究等级×10=传送门世界刷为「是魔王就挑战100勇士」模式的概率(百分数0~100)，消费在 `GameWorldInfoRandomBean.SetGameFightTypeRandom`(所有已解锁世界都可能刷出；命中但当前世界最高已解锁难度无匹配配置行则落回原随机)；多语言 ResearchInfo id=100300007 名字十二语言保持原名「是魔王就挑战100勇士」，概率详情经 details=900000022「（概率{Value}%）」由 `GetDetailsLanguageWithLevelDetail` 按待解锁级填（详见上方 details 机制条目）
  - 世界段(1003) 无尽模式出现概率：`InfiniteShowRate`(100300008，`research_type=4`，`level_max=9`，`icon_res=ui_research_11`，position(-480,-160) 无尽链根部正下方，`pre_unlock_ids` 留空 + `pre_data="AnyWorldInfiniteUnlocked"` 前置=**任意一个世界无尽已解锁**(四世界无尽起始id的OR若走pre_unlock_ids会因100310202/302/402无研究节点触发CreateLine空引用,故走pre_data新条件枚举,未满足时节点隐藏不画线)，`pay_crystal` 九档 100,200,400,800,1500,2500,4000,6000,9000)——概率=基础10%+等级×10%(0级=10%保持原固定概率,9级满级=100%)，消费在 `SetGameFightTypeRandom` 无尽分支(前提该世界 `unlock_id_infinite` 已解锁)；详情**复用** details=900000022，公式 `GetInfiniteShowRateForLevel(level)`=10+等级×10；多语言 cn「无尽涌现」其余暂中文占位
  - 世界段(1003) 真勇者挑战(挑战100勇士BOSS挑战)出现概率：`ChallengeHundredBossRate`(100300009，研究名「真勇者挑战」，`research_type=4`，`level_max=4`，`icon_res=ui_research_9`，position(-480,-160) 与前置节点(-160,-160)水平连线，`pre_unlock_ids="100300007"` 前置=是魔王就挑战100勇士出现概率研究，`pre_data` 留空，`pay_crystal` 四档 100,200,400,800)——概率=基础10%+等级×10%(0级=10%,4级满级=50%)，消费在 `SetGameFightTypeRandom` 挑战100勇士分支抽配置行：**两段判定**先按该概率判定普通/BOSS、再在命中类型行内等概率随机(`FightTypeChallengeHundredInfoCfg.GetRandomRow(unlockDifficultyMax, bossRate)`)，替代旧的全部匹配行等概率随机(旧逻辑 BOSS 率随行数分布漂移)；详情复用 details=900000022，公式 `GetChallengeHundredBossRateForLevel(level)`=10+等级×10；多语言 cn「真勇者挑战」12语言已翻译
  - 强化段(2010) 阵容重命名：`LineupRename`(201000001，`research_type=2`，`pre_unlock_ids="200100001"` 前置=解锁多阵容 `LineupNum`，`level_max=1`，`pay_crystal=200`，position(-400,0) 挂在多阵容节点左侧)——纯解锁开关无衍生数值：解锁后 `UILineupManager` 显示 RenameBtn 可给当前选中阵容改名（自定义名存 `UserDataBean.dicLineupName`，显示名走 `UserDataBean.GetLineupShowName` 优先自定义名、未改名回退默认 UIText 30005，UILineupManager 页签与 UIDialogPortalDetails 出战阵容选择区共用；按钮悬停气泡 UIText 30008；icon 暂为占位 `ui_research_6`）

### 玩家解锁存档

- **UserUnlockBean** - 玩家解锁数据（`unlockInfoData` 字典）
  - 解锁操作：`AddUnlock(unlockId, unlockLevel = 1)`，**新增或等级发生变化时**都触发 `EventsInfo.User_AddUnlock`（可升级解锁如 `CreatureVatAdd` 后续升级也会驱动场景刷新/出现动画）；新建条目按传入 level，已存在仅等级变化才覆盖并通知
  - 解锁检测：`CheckIsUnlock(string)` 支持 `,`（与）与 `|`（或）的复合表达式；以及 `long[] / UnlockEnum / long` 重载
  - 研究等级获取：`GetUnlockResearchLeveByUnlockEnum / ByUnlockId / ByResearchId / GetUnlockResearchLevelByResearchInfo`
  - 解锁衍生数值：`GetUnlockPortalShowCount` / `GetUnlockPortalRefreshMax`(传送门刷新次数上限=`PortalRefreshNum`等级,满级10) / `CheckIsUnlockPortalRefresh`(是否解锁传送门刷新) / `GetUnlockChallengeHundredShowRate`(挑战100勇士出现概率=`ChallengeHundredShowRate`(100300007)等级×`CHALLENGE_HUNDRED_SHOW_RATE_PER_LEVEL`(10),百分数0~100未解锁0满级100；消费点 `GameWorldInfoRandomBean.SetGameFightTypeRandom` 按它判定传送门世界是否生成为挑战100勇士世界) / `GetChallengeHundredShowRateForLevel(int)`(静态,指定等级的挑战100勇士出现概率=等级×10,供研究气泡详情填充) / `GetUnlockChallengeHundredBossRate`(挑战100勇士-BOSS挑战出现概率=`CHALLENGE_HUNDRED_BOSS_RATE_BASE`(10)+`ChallengeHundredBossRate`(100300009)等级×`CHALLENGE_HUNDRED_BOSS_RATE_PER_LEVEL`(10),百分数10~50未研究10满级4级=50;消费点 `SetGameFightTypeRandom` 挑战100勇士分支抽配置行前两段判定普通/BOSS) / `GetChallengeHundredBossRateForLevel(int)`(静态,=10+等级×10,供研究气泡详情填充) / `GetUnlockInfiniteShowRate`(无尽模式出现概率=`INFINITE_SHOW_RATE_BASE`(10)+`InfiniteShowRate`(100300008)等级×`INFINITE_SHOW_RATE_PER_LEVEL`(10),百分数10~100未研究保持10满级9级=100；消费点 `SetGameFightTypeRandom` 无尽分支,前提该世界无尽已解锁) / `GetInfiniteShowRateForLevel(int)`(静态,=10+等级×10,供研究气泡详情填充) /
  - **数值公式 static ForLevel 收口（2026-10 details 机制配套）**：上述衍生数值的实例方法均委托对应 `static Get*ForLevel(int level)`（单一真实源，研究气泡详情按任意等级求值）：`GetCreatureVatNumForLevel`(creatureVatMax+lv) / `GetCreatureVatAddProgressForLevel`(=lv) / `GetCreatureVatMaterialMaxForLevel` / `GetSacrificeMaxForLevel` / `GetSacrificeFailPityAddRateForLevel`(lv×0.05f) / `GetSacrificeDifferentIdRateForLevel`(lv×0.05f) / `GetPortalRefreshMaxForLevel` / `GetGashaponRarityRateForLevel`(RarityInfo.gashapon_rate 按档基础概率 R25/SR20/SSR15/其他10 + lv，扭蛋抽取/展示/气泡三处共用) / `GetJuicerCreatureMaxForLevel` / `GetLineupCreatureNumForLevel` / `GetLineupNumForLevel` / `GetDropCrystalAddLifeTimeForLevel`(lv×5) / `GetDemonLordMPMaxAddValueForLevel`(lv×10) / `GetDemonLordMPFAddValueForLevel`(lv×1) / `GetAbyssalBlessingRefreshMaxForLevel` / `GetDemonLordAutoPickCrystalIntervalForLevel`(lv<=0→-1 否则 11-lv) / `GetDemonLordAutoPickCrystalCountForLevel`(1+lv)；另有先行的 `GetSpaceDashDistanceForLevel` / `GetSpaceDashCDForLevel` / `GetChallengeHundredShowRateForLevel` / `GetChallengeHundredBossRateForLevel`(10+等级×10) `GetUnlockLineupNum` / `GetUnlockLineupCreatureNum` / `GetUnlockGameWorldConquerDifficultyLevel` / `GetUnlockInfiniteDifficultyLevel`(世界无尽：从 `unlock_id_infinite` 起始id连续向后统计已解锁个数+1=已解锁最高无尽难度，起始id=0或起始id未解锁返回0=未解锁无尽) / `CheckInfiniteUnlock`(指定世界指定难度无尽是否解锁，难度<2恒false，起始id+(难度-2)) / `GetUnlockCreatureVatNum` / `GetUnlockCreatureVatAddProgressLevel`(生物进阶魔晶加速研究等级0~5,恒消耗1魔晶,等级=每次进度增加秒数=进度倍率,0级隐藏加速按钮,`UnlockEnum.CreatureVatAddProgress`=100000007) / `GetUnlockCreatureVatMaterialMax`(进阶素材可选上限 = 5 + `UnlockEnum.CreatureVatMaterialNum`(100000008) 研究等级,满级10) / `GetUnlockSacrificeMax`(献祭祭品上限 = 3 + `UnlockEnum.SacrificeNum` 研究等级,满级15) / `GetUnlockSacrificeFailPityAddRate`(献祭失败保底增量 = `SacrificePityRate`(100100003) 等级×5%) / `GetUnlockSacrificeDifferentIdRate`(不同id祭品成功率 = `SacrificeDifferentIdRate`(100100004) 等级×5%) / `GetUnlockDropCrystalAddLifeTime`(魔晶掉落额外时长 = `DropCrystalLifeTime`(200200001) 等级×5秒,在 FightCreatureEntity.DropCrystal 生效) / `GetUnlockDemonLordMPMaxAddValue`(魔王魔力上限加成 = `DemonLordMPMax`(200300001) 等级×10) / `GetUnlockDemonLordMPFAddValue`(魔王魔力恢复加成 = `DemonLordMPF`(200400001) 等级×1/秒,后两者在 CreatureBean.GetAttribute 的 MP/MPF 分支对 IsDemonLord() 叠加,战斗与基地魔物管理页面同一口径) / `GetUnlockSpaceDashLevel`(空格突进研究等级 = `SpaceDash`(200600001) 等级,0=未解锁,1/2/3级=1/2/3距离单位) / `GetUnlockSpaceDashCD`(空格突进冷却秒 = `GetSpaceDashCDForLevel(SpaceDashCD(200700001) 等级)`,未解锁3s每级-0.5最低1s;两者由基地控制 ControlForGameBase 读取驱动突进,详见 control-system) / `GetSpaceDashDistanceForLevel(int)`(静态,指定突进等级的距离=等级×`SPACE_DASH_DISTANCE_PER_LEVEL`(1.5,每级距离唯一真实源,ControlForGameBase.dashDistancePerLevel 默认值亦引用之)) / `GetSpaceDashCDForLevel(int)`(静态,指定CD等级的冷却=max(3-等级×0.5,1);两者供研究气泡文本填充) / `GetUnlockDemonLordAutoPickCrystalInterval`(魔王自动拾取魔晶间隔秒 = 11 - `DemonLordAutoPickCrystal`(200800001) 等级,未解锁返回-1禁用,10级10s→满级1s) / `GetUnlockDemonLordAutoPickCrystalCount`(魔王每次拾取魔晶数量 = 1 + `DemonLordAutoPickCrystalNum`(200900001) 等级,基础1满级6;两者由 GameFightLogic.UpdateGameForDefenseCore 按间隔驱动 PickupCrystalForCoreAuto 从场上FIFO拾魔晶,详见 game-fight-system)
  - 世界 Quick(加快进攻节奏)：`GetWorldQuickAttackUnlockId(worldId, difficultyLevel)` = 世界配置表 `unlock_id_quick_attack` 起始id + (难度-1)(起始id=0 返回-1=未配置；world1 起始id=100310130, 难度1→100310130~难度10→100310139,避开征服难度 12~20 段)；`CheckIsUnlockWorldQuickAttack(worldId, difficultyLevel)` 战斗中按当前世界+当前难度判定是否显示战斗界面 Quick 按钮。Quick 研究已按难度拆分(难度1~10各一个, 难度2~10 pre=同难度难度研究/难度1 pre 留空)，且节点**需通关过该难度才显示**(配置表 `pre_data` 列驱动: Quick行填 `World1ConquerCompleteCount{难度}:1`, `UIBaseResearch.CheckPreIsUnlock` 追加 `CheckPreDataIsMeet` 判定)。研究节点是**世界分类(research_type=4)**、起始id存世界表 `unlock_id_quick_attack` 列(新增世界需填该列)，详见 research-system SKILL「世界分支」
  - 世界 2倍速游戏(Speed2)：`GetWorldSpeed2UnlockId(worldId, difficultyLevel)` = 世界配置表 `unlock_id_speed2` 起始id + (难度-1)(起始id=0 返回-1=未配置；world1 起始id=100310140, 难度1→100310140~难度10→100310149)；`CheckIsUnlockWorldSpeed2(worldId, difficultyLevel)` 战斗中判定是否显示 2倍速按钮。研究按难度拆分(难度1~10各一个, **pre=同难度 Quick 研究**挂在每个 Quick 后面, `pre_data` 留空——Quick 的通关显示前置已把关)；节点坐标 x=480(Quick 列 x=160 的下一列, y 与同难度 Quick 一致)，icon ui_research_94，pay=200。解锁后战斗界面显示 2倍速 RadioButton(默认关,点击整场游戏时间流速×2,仅本场有效,机制见 game-fight-system SKILL Speed2)
  - pre_data 前置解锁条件(特殊)：ResearchInfo 新列，格式 `条件枚举名:数值` 多条 `&` 组合(与, 数值缺省1)，条件枚举 `ResearchPreConditionEnum`(在 GameStateEnum.cs, 现有 World1ConquerCompleteCount1~10=世界1难度1~10通关次数 + GashaponCreatureDrawCount1001~7004=30 个职业扭蛋累计抽出次数,枚举值=职业id + AnyWorldInfiniteUnlocked=10001 任意世界无尽已解锁(遍历 GameWorldInfoCfg 任一世界 GetUnlockInfiniteDifficultyLevel>0,用于无尽概率研究100300008前置——无尽起始id走pre_unlock_ids OR会触发CreateLine空引用,故走pre_data))，拆分走通用扩展 `StringExtension.SplitForDictionaryEnumLong`、判定在 `ResearchInfoBeanPartial.GetPreDataConditions/CheckPreDataIsMeet`，与 pre_unlock_ids 一起决定节点显示；**pre_data 字段暂在 Partial，重新生成 Entity 后需删除该临时字段**
  - 魔物分支(300X1NNND 段) 职业独立扭蛋研究：30 职业 × 3 档(x1/x5/x10) 共 90 节点（如 `300110011`=人类战士x1）。x1 pre=族「孕育x10」研究(300X00102)+`pre_data` 该职业抽出99只，x5 pre=x1、x10 pre=x5；消耗=族x10研究 ×2/×5/×10（普通族 200/500/1000、史莱姆族 2000/5000/10000）；图标与该职业「解锁职业」研究(300X0000N)同图标（ui_research_N）；纯数据驱动无需新增 UnlockEnum，解锁后扭蛋商店出现对应职业独立扭蛋（见 game-gashapon）
  - 世界征服难度：难度2~10 已拆分为每难度独立研究节点(块内 nn=12~20, 链式前置), `GetUnlockGameWorldConquerDifficultyLevel(worldId)` = `conquerDifficultyMax` + 从难度起始id(`unlock_id_conquer_difficulty_level`)连续向后统计的已解锁个数
  - 世界无尽难度：难度2~10 已拆分为每难度独立研究节点(块内 nn=02~10, world1 即 100310102~100310110；第一难度没有无尽模式), `level_max=1`，双前置链式(难度2无尽 pre=征服难度2研究 100310112；难度N无尽 pre="上一无尽节点,同难度征服难度研究"，如 100310103 pre="100310102,100310113")，`pay_crystal` 独立阶梯 200/1000/2000/4000/8000/16000/32000/64000/128000，icon 与征服难度链同为 `ui_research_11`，坐标 x=-480(新列)、y=(难度-2)*160(与难度链行 y 对齐 0~1280)；世界表 `unlock_id_infinite`(world1=100310102)语义由「单值解锁ID」改为「无尽研究起始ID」(难度N无尽=起始id+(N-2)，与征服难度链 `unlock_id_conquer_difficulty_level` 的「起始ID+连续块」模式同构)；`GetUnlockInfiniteDifficultyLevel(worldId)` 从起始id连续统计已解锁个数+1(起始id=0/未解锁返回0)，`CheckInfiniteUnlock(worldId, difficultyLevel)` 判定指定难度；世界2/3/4 的 x02 已在 unlock_info 登记但暂无研究节点(这些世界连征服难度链都没有)，后续按 x02~x10 同模式补即可
  - 解锁列表：`GetUnlockGameWorldIds` / `GetUnlockCreatureModelIds` / `GetUnlockGashaponCreatureInfos`(已解锁可孕育生物=creature_type=1且职业研究已解锁, 通关奖励装备池可用性过滤用, 见 game-fight-reward)

### 关键文件

| 文件 | 路径 |
|------|------|
| 研究主界面（逻辑） | Assets/Scripts/Component/UI/Game/BaseResearch/UIBaseResearch.cs |
| 研究主界面（AutoLink） | Assets/Scripts/Component/UI/Game/BaseResearch/UIBaseResearchComponent.cs |
| 研究主界面（测试模式） | Assets/Scripts/Component/UI/Game/BaseResearch/UIBaseResearchTest.cs |
| 研究节点 View | Assets/Scripts/Component/UI/Game/BaseResearch/UIViewBaseResearchItem.cs |
| 研究节点 View（AutoLink） | Assets/Scripts/Component/UI/Game/BaseResearch/UIViewBaseResearchItemComponent.cs |
| 研究信息气泡 | Assets/Scripts/Component/UI/Popup/UIPopupResearchInfo.cs |
| 研究信息气泡（AutoLink） | Assets/Scripts/Component/UI/Popup/UIPopupResearchInfoComponent.cs |
| 研究配置 Bean | Assets/Scripts/Bean/MVC/Game/ResearchInfoBean.cs |
| 研究配置 Partial | Assets/Scripts/Bean/MVC/Game/ResearchInfoBeanPartial.cs |
| 解锁配置 Bean | Assets/Scripts/Bean/MVC/Game/UnlockInfoBean.cs |
| 玩家解锁存档 | Assets/Scripts/Bean/Game/UserUnlockBean.cs |
| 研究配置表 | Assets/Data/Excel/excel_research_info[研究信息].xlsx |
| 解锁配置表 | Assets/Data/Excel/excel_unlock_info[解锁信息].xlsx |

## 约束

- **Bean 文件不可直接改**：`ResearchInfoBean.cs` / `UnlockInfoBean.cs` 为自动生成，所有扩展方法写入对应的 `*Partial.cs`
- **Excel 配置改动统一通过 `.claude/scripts/excel_*.py`（openpyxl）执行**，不得使用 pandas/xlrd 等
- **解锁 ID 与研究 ID 必须区分**：一条研究记录的主键是 `id`，但购买入账写入存档时使用的是 `unlock_id`；检查解锁时也只认 `unlock_id`
- **前置条件表达式语义**：`,` 与；`|` 或；可嵌套（例如 `1,2|3,4` = `1 AND (2 OR 3) AND 4`）
- **支付水晶配置格式**：`pay_crystal` 支持三种写法
  - 单值：`100` → 仅 1 级
  - 逗号分隔：`100,200,300` → 每级独立配置
  - `基础*倍率`：`100*2` → 自动生成 `level_max` 个阶梯（基础 + 基础×倍率×index）
- **解锁动画先于设施出现动画**：`OnClickForPay` 确认后先扣水晶(仅改内存)，再调 `AnimForUnlock(targetLevel, actionComplete)` 播节点解锁动画；**动画完成回调里**才 `AddUnlock` + `SaveUserData()` 落盘并刷新页面。原因：`AddUnlock` 会同步触发 `User_AddUnlock`，`ScenePrefabForBase.EventForUserAddUnlock` 立刻切设施镜头/隐藏研究 UI 播设施出现动画，若与节点解锁动画同时发生会冲突；推迟到动画后可保证「节点解锁动画 → 设施镜头切换+出现动画」顺序播放。`User_AddUnlock` 事件同时通知场景刷新
- **节点动画播完即显示已解锁**：`AnimForUnlock` 的 `OnComplete` 里会在 `AddUnlock` 之前先 `SetStateForLevel(targetLevel)` 把本节点图标/颜色刷成已解锁外观。原因：解锁数据要等回调里才 `AddUnlock`，而 `AddUnlock` 又会同步隐藏研究 UI 去播设施动画——若只靠回调里的 `InitResearchItems` 刷新，玩家会看到「节点动画播完图标仍是未解锁占位(白) → 设施动画播完才变已解锁」。`SetState` 已抽出 `SetStateForLevel(int)` 供此处按解锁后的目标等级直接刷新
- **同类型连线**：`CreateLine` 中若前置节点 `research_type` 与目标节点不一致，会跳过连线（跨类型的关系仅作为解锁条件，不画线）
- **可购买高亮（魔晶足够即提示）**：节点未满级(含未解锁)且当前魔晶 ≥ 下一级价格(`GetPayCrystal(level+1)`)时，`RefreshCanPayFx` 启用 Icon 上的脉冲 Animator（复用成就 `UIViewAchievementRewardPulse.controller`，驱动 Icon 自身 localScale 呼吸缩放）并显示 `Shine` 流光层（CardContent 下全拉伸 Image，材质 `Mat_UIViewBaseResearchItemShine.mat` 由成就流光材质复制解耦，默认隐藏）；不满足则停 Animator+复位 scale+隐藏 Shine。判定与 `OnClickForPay` 可购买口径一致；`SetData` 刷一次，`UIBaseResearch.Awake` 监听 `Backpack_Crystal_Change` 事件 → `RefreshAllCanPayFx` 重刷整页（兜住界面开着期间的魔晶入账与购买扣款灭灯）；脉冲/流光只动 Icon scale 与材质 shader，与悬停(CardContent)/解锁动画(根)三层 transform 互不干扰
- **设施研究门控 UI 范例**：传送门详情弹窗 `UIPopupPortalDetails` 四项预览用 `UserUnlock.CheckIsUnlock(UnlockEnum.PortalPreview*)` 判定是否显示，未解锁则该详情项整行隐藏（奖励区不显示），名字行始终显示。新增此类设施门控时：`excel_research_info` 加 `research_type=1` 节点 + `excel_unlock_info`(`unlock_type=0`，`id`=`unlock_id`) + 多语言 `excel_language` 的 `ResearchInfo` 工作表节点名 + `UnlockEnum` 常量。另一实例：魔物进阶详情气泡的数值范围预览 `CreatureVatBuffPreview`(unlock_id 100000006, 1000 设施段, pre=进阶设施 100000000)，未解锁 BUFF 数值显 `???`、解锁后显 `min~max`（门控在 `UIViewCreatureVatAscendBuffItem`）
- **退出研究界面**：固定回到 `UIBaseCore`（基地核心界面），不要硬跳到其它界面
- **节点坐标编辑**：仅在 `UIBaseResearchTest`（测试模式）下显示保存按钮，通过 `ExcelUtil.SetExcelData` 写回 Excel；正式运行不显示

## 关联 Skill

详细开发指南请参考: [research-system](../skills/research-system/SKILL.md)
