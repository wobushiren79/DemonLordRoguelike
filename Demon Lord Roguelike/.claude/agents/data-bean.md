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

> **`FightCreatureBean.isPositionReleased`（手写 `Assets/Scripts/Bean/Game/FightCreatureBean.cs`，bool，占位已释放）**：冲锋自爆型生物（如 6003 哥布林敢死队）冲锋开始时置位（`AIIntentDefenseCreatureCharge`），此后占位/删除扫描（`FightBean.CheckDefenseCreatureByPos`/`GetDefenseCreatureByPos`）跳过它，原格可立即放第二只魔物；`ResetData()` 里清零防对象池残留。

> **`FightBean` 防御生物按占位操作（手写 `Assets/Scripts/Bean/Game/FightBean.cs`）**：`CheckDefenseCreatureByPos`/`GetDefenseCreatureByPos` 均跳过 `isPositionReleased` 实体；**`RemoveDefenseCreatureByPos` 已删除**，替换为 **`RemoveDefenseCreature(FightCreatureEntity)`**——`DictionaryList.RemoveByValue` 按实例精确移除（按 positionCreate 首匹配会误删同格新生物、按 UUID 会误删重生替换的新实体）。

> **`CreatureInfoBean.charge_attack`（Excel 自动生成列，int）**：冲锋自爆开关（0=默认站桩，1=放卡后立即向前冲锋并释放原占位格，遇敌/到路尽头/被打死时原地自爆）；配套手写解析 `CreatureInfoBeanPartial.IsChargeAttack()`。

> **`CreatureInfoBean.details`（Excel 自动生成列 `details[language_1]`，long）**：生物详情描述（攻击方式说明）文本 id，值=生物自身 id；配套自动属性 `details_language`（`GetTextById(CreatureInfoCfg.fileName, details, 1)` 取语言表 content_1 语种列，带 LanguageCache）。仅 id 1001~7004 的 30 个生物已配 12 语种；0/空=详情面板隐藏说明区块。消费方：`UIViewCreatureCardDetails.SetRenmark`。

> **`CreatureInfoBean.show_attribute`（Excel 自动生成列，string）**：展示属性列表，逗号分隔 `CreatureAttributeTypeEnum` 枚举值（如 `1,3,4,6`=HP/DR/ATK/ASPD）；**同一配置同时控制三处**——卡片详情面板显示项（`UIViewCreatureCardDetails.SetAttribute`）、献祭加点界面可加项（`UICreatureAddAttribute.InitItems`）、创建随机加点池（`CreatureAttributeBean.AddRandomAttributeForCreate`）。配套手写解析 `CreatureInfoBeanPartial.GetShowAttributeList()`（懒解析缓存，空/解析失败兜底默认 HP/DR/ATK/ASPD）。当前配置：烂泥/毒液史莱姆（3003/3004）配 `4`（仅攻击力）、守护史莱姆（3001）配 `1,3`（仅 HP/DR，无攻击模式纯肉盾）、魔王物种行（id 1-7，creature_type=0 创建角色）配 `4,5,2,11`（ATK/MSPD/MP/MPF），其余全配 `1,3,4,6`。

> **`UserStoryBean`（手写 `Assets/Scripts/Bean/Game/UserStoryBean.cs`，仿 UserUnlockBean 拆档模式）**：用户故事演出数据存档——`dicPlayedStory`（`Dictionary<long,long>`，key=StoryInfo.id、value=播放完成时间戳 Ticks；字典而非列表，事件多了查询仍 O(1)）+ `IsStoryPlayed/MarkStoryPlayed/GetDicPlayedStory` 懒加载；已拆分为独立存档 `UserStory_{slot}`（`UserDataService` 加载/保存/删除时与 UserUnlock 等同管线注入落盘），经 `UserDataBean.userStoryData`（[JsonIgnore]）+ `GetUserStoryData()` 访问（故事演出系统 story-system 使用）。

### Bean 命名规范
- 基础 Bean 后缀：`Bean`
- 部分数据 Bean：`BeanPartial`
- 配置数据 Bean：`InfoBean`

## 约束

- Bean 类保持纯数据结构，不包含业务逻辑
- 需要序列化的 Bean 使用 `[Serializable]` 标记
- Bean 字段使用公共属性或字段，便于 JSON 序列化
- **`*InfoBean.cs` 和 `*Bean.cs` 是自动生成文件，禁止直接修改**。所有手写扩展方法、辅助属性、解析逻辑必须写在对应的 `*BeanPartial.cs` 文件中
