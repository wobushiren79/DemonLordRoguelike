---
name: data-bean
description: 数据模型(Bean)开发：框架层和游戏层所有 Bean 类，包括数据模型、UI模型、配置模型。
tools: Read, Write, Edit, Glob, Grep, Bash
watched_files:
  - Assets/FrameWork/Scripts/Bean/
  - Assets/Scripts/Bean/
---

# 数据模型 (Bean) 开发代理

你负责 [FrameWork/Scripts/Bean/](Assets/FrameWork/Scripts/Bean/) 和 [Scripts/Bean/](Assets/Scripts/Bean/) 中所有数据模型类的开发。

## 职责范围

### 框架层 Bean
```
Bean/
├── 基础: BaseBean, BaseDataBean, BaseInfoBean, BaseInfoBeanPartial
├── 资源: AudioBean, AnimBean, EffectBean, IconBean, ImageResBean
├── UI: DialogBean, PopupBean, ToastBean, ProgressBean
├── 数据: DataBean, DataStorageListBean, DictionaryListBean
├── 工具: ColorBean, NumberBean, TimeBean, Vector3Bean, Vector3IntBean
├── 游戏: GameConfigBean, ScenesChangeBean, GameTimeCountDownBean, GameObjectBean
├── Spine: SpineSkinBean, SpineAnimationStateBean
├── 多语言: LanguageBean, UITextBean
├── 音频: AudioInfoBean
├── 网格: MeshDataCustom, MeshDataDetailsCustom
├── Steam: SteamLeaderboardEntryBean 等
└── 特殊: TileBean
```

### 游戏层 Bean
```
Bean/
├── Game/  - Creatures, Buff, Fight, Item, DoomCouncil, Gashapon 等
├── MVC/   - UserData, CreaturesInfo, BuffInfo, ItemsInfo 等
└── UI/    - DialogSelect, DialogRename, DialogBossShow 等
```

> **`CreatureBean.IsDemonLord()`（`CreatureBeanPartial.cs` #region 魔王）是「是否魔王本体」判定的单一真实源**：比对 `creatureUUId == userData.selfCreature.creatureUUId`（魔王独立存储于 `UserDataBean.selfCreature`，不在背包/阵容列表内）。管理列表置顶、稀有度按 L 显示、隐藏等级、不可献祭、战斗不加经验等特殊处理统一调它，UI 层(如 `UIViewCreatureCardDetails.IsDemonLord`)已收口为委托调用。

> **`ItemBean.juicerExp`（手写 `Assets/Scripts/Bean/Game/ItemBean.cs`，long）**：魔汁经验值实例字段，仅 `ItemTypeEnum.Juice=11`（消耗品）类型、`ItemIdEnum.Juice=200001` 有效；榨汁结算（`CreatureJuicerLogic.SettleJuiceReward`）按投入魔物等级的 `LevelInfo.juicer_exp` 汇总写入，`num_max=1` 不堆叠保证每瓶经验独立；旧存档无此字段默认 0 兼容。

> **`CreatureBean.transformItemId`（手写 `Assets/Scripts/Bean/Game/CreatureBean.cs`，long，幻化状态存档字段）**：幻化药（`ItemTypeEnum.TransformPotion=18`/`ItemIdEnum.TransformPotion=200002`）使用后写入所用道具 id（0=无幻化，连续吃后者覆盖；幻原药 `RestorePotion=19`/`200003` 置0清除），持久化入存档、旧档无此字段默认 0 兼容；`ClearTempData()` 清零。解析入口在 `CreatureBeanPartial`（#region 幻化相关）：`GetTransformItemInfo()` 共用前置校验（0/配置缺失每 id 一次 LogError/类型非 18 防 Mod id 复用/`other_data` 空→null）；`ParseTransformOtherData` 解析组合格式 `chessRes,avatorRes|uiScale;x,y`；`GetTransformSpineRes()` 取 chess 段（SkeletonDataAsset 资源名/Mod catalog key）、`GetTransformUIShowSpineRes()` 取 avator 段（ui_show_spine 高清展示）、`GetTransformUIShowData()` 取 `|` 第三段详情UI尺寸（格式同 ui_data_b）；**只存 id 实时查配置**——Mod 移除全路径自动回落原形象、装回自动恢复、幻原药不依赖配置仍可清。消费中枢 `CreatureHandler.SetCreatureData`（幻化时整骨替换+跳过套原皮，isUIShow=true 且有 avator 段时改用高清资源；覆盖详情UI/列表图标/对话头像/基地/议会/战斗皮肤）、`GameUIUtil.SetCreatureUIForDetails`（幻化自带尺寸段替代 `ChangeUISizeForB`）与 `GetFightCreatureObj(resNameOverride)`（防御+核心传幻化资源，进攻敌人不传）。Mod 幻化药（AeonsEchoSpine）生产流程见 aeonsecho-spine-mod agent。

> **`BaseBean.CombineModReferenceIds`（Mod 合并引用字段钩子，生成器自动重写）**：`BaseCfg.GetInitDataForMods` 合并 Mod JsonText 行时先 `id = CombineModId(modId, id)` 再调本钩子——**重写不由手写**：`ExcelEditorWindow.CreateEntity` 按 Excel 列头 `[language]`/`[language_1]`/`[language_2]`/`[mode_id]` 标记，把标记字段（long/int）的拼接代码自动生成进 `*Bean.cs`（如 ItemsInfo 的 `name[language]` → Mod 道具 name 指向 Mod 自带 `Language_ItemsInfo_{lang}.txt` 同自ID 行；约定 0=无引用不拼接、name 自ID=道具自ID；**Mod 行带标记列一律视为 Mod 本地引用，不能复用主游戏 textId**）。`BaseCfg.CombineModId` 为 public static（`modId*10^14 + 自ID`）。新增拼接列=Excel 列头加标记+重新生成该表 Entity，零代码。机制详见 mod-system Skill「JsonText配置覆盖-合并机制」。

> **`FightCreatureBean.isPositionReleased`（手写 `Assets/Scripts/Bean/Game/FightCreatureBean.cs`，bool，占位已释放）**：冲锋自爆型生物（如 6003 哥布林敢死队）冲锋开始时置位（`AIIntentDefenseCreatureCharge`），此后占位/删除扫描（`FightBean.CheckDefenseCreatureByPos`/`GetDefenseCreatureByPos`）跳过它，原格可立即放第二只魔物；`ResetData()` 里清零防对象池残留。

> **`FightCreatureBean.isSummoned`（手写 `FightCreatureBean.cs`，bool，是否召唤物，2026-09-15 新增）**：`AttackModeSummon` 召唤生成时置位（经 `CreateAttackCreature` 返回 obj.name=creatureUUId 反查实体）；当前消费=`FightCreatureEntity.DropCrystal` 方法头判到直接 return——召唤物（当前=骷髅召唤师召唤的骷髅 20010001/20020001）死亡不掉魔晶，防低血召唤物被挂机刷取；`ResetData()` 里清零防对象池残留（置位发生在 CreateAttackCreature 两次 ResetData 之后，顺序无冲突）。

> **`FightCreatureBean.GetAttackTimeData(out timeAttackPre, out timeAttacking, out int attackTimes)`（手写，2026-09 重构）**：攻速(ASPD)→攻击准备/出手时间+本轮攻击次数换算的**唯一入口**（唯一调用方 `AIIntentCreatureAttack.RefreshData`）。线性频率制：频率倍率 = `max(1+0.05×ASPD, 0.25)`、攻击次数 = `1+max(0, floor(ASPD÷20))`、时间倍率 = 次数÷频率倍率，准备/出手时间 = 基础时间×时间倍率（下限 0.02s），BUFF（`BuffEntityAttributeAttackTime` 系）之后再乘。效果：每点攻速严格 +5% 攻击频率、无上限无断崖；逢 20 倍数时间回基础值且攻击次数+1（由攻击意图在 0.2 秒窗口连发，见 game-ai/ai-system「攻速连发」）。常量：`ASPD_FREQUENCY_RATE_PER_POINT`(0.05)/`ASPD_POINT_PER_EXTRA_ATTACK`(20)/`ASPD_FREQUENCY_RATE_MIN`(0.25)/`ATTACK_TIME_MIN`(0.02)。旧 0~100→0.02s 插值（100 封顶、频率超线性、满攻速 25 倍速动画）已移除。负攻速只拉长时间（频率下限 0.25），不减攻击次数。

> **`FightCreatureBean.damageTransferApplierId` / `damageTransferBuff`（手写 `FightCreatureBean.cs`，string / BuffBaseEntity，伤害转移标记）**：非空时该生物受到的 `UnderAttack` 伤害在结算前拦截、改道给代受者（UUID 指向的生物）承受（`FightCreatureEntity.UnderAttack` 方法头分支，先于无敌/闪避）；`damageTransferBuff` 回指护盾 BUFF 实例供受击闪白反馈（避免受击扫 BUFF 列表）；代受者已死/离场时拦截分支惰性清空两字段恢复承伤。当前由大盾战士 BOSS「援护护盾」BUFF（`BuffEntityConditionalShieldTransfer`）写入/清理；`ResetData()` 里清空防对象池残留。配套：`FightUnderAttackBean.isDamageTransferred`（改道数据旗标，防转移成环最多一跳，`ClearData` 重置）+ `SetDataForTransferFrom(source, newAttackedId)` 拷贝构造。

> **`CreatureBean.dicFixedAttribute`（手写 `CreatureBeanPartial.cs`，`Dictionary<CreatureAttributeTypeEnum,float>`，`[JsonIgnore]+[NonSerialized]` 运行时字段不入存档）**：测试模式专用的**固定基础值**——设置后 `CreatureBean.GetAttribute` 该项基础值直接取固定值（跳过 creatureInfo/npcInfo 配置分支），角色加点/装备/自身BUFF/深渊馈赠仍照常叠加（**非最终值锁定**）。由 GameTestEditor「🛡️ 防守方固定属性」配置（`FightBeanForTest.dicTestDefenseFixedAttribute` 中转），`GameFightLogicTest` 应用到测试战斗防守方全部生物（卡片魔物+魔王核心；MP 被固定时测试魔王蓝量让位），战中克隆生物经 `GameFightLogic_DefenseCreatureCreate` 事件兜底补设（DeepCopy 丢 NonSerialized 字段）。⚠️ 区别于 `FixedAttributeForCreate`（新建存档初始魔物的固定**加点**，写入 `creatureAttribute` 入存档）——两者名字相近但完全不同机制。详见 test-system skill「防守方固定属性设置」。

> **`FightBean` 防御生物按占位操作（手写 `Assets/Scripts/Bean/Game/FightBean.cs`）**：`CheckDefenseCreatureByPos`/`GetDefenseCreatureByPos` 均跳过 `isPositionReleased` 实体；**`RemoveDefenseCreatureByPos` 已删除**，替换为 **`RemoveDefenseCreature(FightCreatureEntity)`**——`DictionaryList.RemoveByValue` 按实例精确移除（按 positionCreate 首匹配会误删同格新生物、按 UUID 会误删重生替换的新实体）。

> **`FightBean.GetCreatureById`（手写 `FightBean.cs`）清理期安全**：`GameFightLogic.ClearGame` 顺序为**先 `fightData.ClearEntity()`（实体列表清空 + `fightDefenseCoreCreature=null`）后 `ClearFightCreatureBuff()`**——清理期走到 `GetCreatureById(uuid, None)` 时两列表已空，会落到核心比对分支；核心判空后返回 null（2026-09 修复：援护护盾 BUFF `ClearTransferMark` 在战斗重开清理期查目标，核心已置空直接解引用 NRE）。调用方一律判空，不得假定清理期还能解析到实体。

> **`CreatureInfoBean.charge_attack`（Excel 自动生成列，int）**：冲锋自爆开关（0=默认站桩，1=放卡后立即向前冲锋并释放原占位格，遇敌/到路尽头/被打死时原地自爆）；配套手写解析 `CreatureInfoBeanPartial.IsChargeAttack()`。

> **`CreatureInfoBean.details`（Excel 自动生成列 `details[language_1]`，long）**：生物详情描述（攻击方式说明）文本 id，值=生物自身 id；配套自动属性 `details_language`（`GetTextById(CreatureInfoCfg.fileName, details, 1)` 取语言表 content_1 语种列，带 LanguageCache）。仅 id 1001~7004 的 30 个生物已配 12 语种；0/空=详情面板隐藏说明区块。消费方：`UIViewCreatureCardDetails.SetRenmark`。

> **`CreatureInfoBean.show_attribute`（Excel 自动生成列，string）**：展示属性列表，逗号分隔 `CreatureAttributeTypeEnum` 枚举值（如 `1,3,4,6`=HP/DR/ATK/ASPD）；**同一配置同时控制三处**——卡片详情面板显示项（`UIViewCreatureCardDetails.SetAttribute`）、献祭加点界面可加项（`UICreatureAddAttribute.InitItems`）、创建随机加点池（`CreatureAttributeBean.AddRandomAttributeForCreate`）。配套手写解析 `CreatureInfoBeanPartial.GetShowAttributeList()`（懒解析缓存，空/解析失败兜底默认 HP/DR/ATK/ASPD）。当前配置：烂泥/毒液史莱姆（3003/3004）配 `4`（仅攻击力）、守护史莱姆（3001）配 `1,3`（仅 HP/DR，无攻击模式纯肉盾）、魔王物种行（id 1-7，creature_type=0 创建角色）配 `4,5,2,11`（ATK/MSPD/MP/MPF），其余全配 `1,3,4,6`。

> **`CreatureNpcBean.SetNpcInfoForEditor`（手写 `Assets/Scripts/Bean/Game/CreatureNpcBean.cs`，`#if UNITY_EDITOR` 编辑器专用）**：注入 npcInfo 编辑副本，供 NPC创建编辑器窗口（`游戏/NPC创建编辑`）预览未保存/编辑中的 NPC——`npcInfo` getter 懒查 `NpcInfoCfg.GetItemData(npcId)`，未保存的新 id 会 LogError 返回 null，已有 id 返回的是 Cfg 缓存原值而非编辑副本，编辑器装配 CreatureBean 预览时必须注入。

> **`UserStoryBean`（手写 `Assets/Scripts/Bean/Game/UserStoryBean.cs`，仿 UserUnlockBean 拆档模式）**：用户故事演出数据存档——`dicPlayedStory`（`Dictionary<long,long>`，key=StoryInfo.id、value=播放完成时间戳 Ticks；字典而非列表，事件多了查询仍 O(1)）+ `IsStoryPlayed/MarkStoryPlayed/GetDicPlayedStory` 懒加载；已拆分为独立存档 `UserStory_{slot}`（`UserDataService` 加载/保存/删除时与 UserUnlock 等同管线注入落盘），经 `UserDataBean.userStoryData`（[JsonIgnore]）+ `GetUserStoryData()` 访问（故事演出系统 story-system 使用）。

> **`FightTypeChallengeHundredInfoBean` / `FightTypeChallengeHundredInfoBeanPartial`（2026-09 新增自动生成对，Bean 禁改）**：`excel_fight_type_challenge_hundred_info[战斗-挑战100勇士]` 配置对（一行=一个挑战配置、`difficulty_levels` 声明适配难度可多选，当前 28 行=征服全部进攻敌人 14普通+14BOSS；本表多值列统一 `,` 分隔，与征服表 `&` 各自独立）。Partial 手写扩展：`GetEnemyIdList`/`GetRandomEnemyId`（enemy_ids 池，生成 100 只怪每只独立随机）、`GetDifficultyLevelList`/`IsMatchDifficulty`（世界最高已解锁难度在 difficulty_levels 列内才可抽中本行）、`GetRandomFightScene`/`GetRandomRoadNum`/`GetRandomRoadLength`（后两者走框架层 `RandomUtil.GetRandomIntByRangeString` 解析 "x" 或 "x-y" 区间——**该方法为本次新增，位于 git submodule `Assets/FrameWork` 内，改动在子模块内提交**）、`IsBossChallenge`（challenge_type==1，BOSS 挑战通关宝箱奖励翻倍）、**逐难度对齐取值**（强度/掉晶/箱晶/稀有度/经验五列为 string：单值=全难度共用，或与难度列表等长逗号分隔、按冻结难度取对齐档，不在列钳到最近档）：`GetIntensityRate(difficulty)`（≤0 按 1）/`GetDropCrystal(difficulty)`/`GetRewardExp(difficulty)`/`GetRewardEquipRarity(difficulty)`（≤0 按 1）/`GetRandomRewardCrystal(difficulty)`（对齐档元素仍 x 或 x-y）；Cfg 扩展 `GetMatchRows(unlockDifficultyMax)`/`GetRandomRow`（无匹配返回 null，调用方落回原随机）。

> **`FightBeanForChallengeHundred`（手写 `Assets/Scripts/Bean/Game/FightBeanForChallengeHundred.cs`，FightBean 子类）**：挑战100勇士战斗数据——冻结配置行 `fightTypeChallengeHundredInfo`（由 `gameWorldInfoRandomData.challengeHundredRowId` 取回）、`gameWorldInfoRandomData`、常量 `AttackCreatureNum=100`、固定单关（`figthNumMax=1`）；`InitData` 读冻结道路数量/长度、配置行场景池随机其一；`InitFightAttackData` 把 100 只怪在 `attack_show_time` 内**分桶均匀随机**排程（每桶随机一个时刻、每只独立抽 enemy_ids、携带配置行强度倍率 `intensityRate`——本模式强度自配，不叠加终焉议会强度议案）。

> **`GameWorldInfoRandomBean` 挑战100勇士字段（手写于 `Assets/Scripts/Bean/MVC/Game/GameWorldInfoBeanPartial.cs`）**：`challengeHundredRowId`（冻结配置行 id，仅 gameFightType==ChallengeHundred 时有意义，0=未冻结）/ `listRewardChallengeHundred`（冻结的 3 箱通关奖励，预览=实领，与征服 `listDifficultyRandom.listReward` 同契约）/ `rewardUnlockSignChallengeHundred`（生成奖励时的装备奖励池解锁签名，池变化时 `GetChallengeHundredReward` 重新生成并缓存）；JSON 新增字段对老存档安全（缺省 0/null）。生成入口 `SetGameFightTypeRandom` 头部按 `UserUnlockBean.GetUnlockChallengeHundredShowRate()` 概率判定（命中但当前世界最高已解锁难度无匹配配置行则落回原随机），`SetRandomDataForChallengeHundred` 冻结行/道路/3箱奖励。

> **`RewardSelectBean` 挑战100勇士改造（手写 `Assets/Scripts/Bean/Game/RewardSelectBean.cs`）**：新增 `isAutoOpenFirstBox` 字段（默认 true=征服行为：首箱保底自动开不占次数；false=手动开全部箱——挑战100勇士 3箱3抽用，`GameFightLogicChallengeHundred` 通关领奖时置 false）与静态 `CreateRewardListForChallengeHundred(challengeHundredInfo)`（固定 3 箱：装备池空→3 箱全魔晶，否则 3 箱全装备、稀有度=配置行 `reward_equip_rarity`）；原 `CreateItemEquip`/`CreateItemCrystal` 拆出核心方法 `CreateItemEquipCore`（稀有度/加点数/使用者类型已确定，生成不出装备时按 `getFallbackCrystalNum` 回调兜底魔晶）/`CreateItemCrystalCore`（数量已确定）供征服与挑战100勇士两模式复用。

### Bean 命名规范
- 基础 Bean 后缀：`Bean`
- 部分数据 Bean：`BeanPartial`
- 配置数据 Bean：`InfoBean`

## 约束

- Bean 类保持纯数据结构，不包含业务逻辑
- 需要序列化的 Bean 使用 `[Serializable]` 标记
- Bean 字段使用公共属性或字段，便于 JSON 序列化
- **`*InfoBean.cs` 和 `*Bean.cs` 是自动生成文件，禁止直接修改**。所有手写扩展方法、辅助属性、解析逻辑必须写在对应的 `*BeanPartial.cs` 文件中
