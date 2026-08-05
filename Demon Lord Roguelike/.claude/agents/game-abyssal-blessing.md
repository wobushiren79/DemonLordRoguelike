---
name: game-abyssal-blessing
description: 深渊馈赠系统开发：征服模式关卡间馈赠选择、馈赠 BUFF 添加、深渊馈赠配置表(excel_abyssal_blessing_info)新增、parent_id/level 同族等级链升级替换、AbyssalBlessingInfoBean 配置、AbyssalBlessingInfoCfg.GetFamilyRootId 族根回溯、UIFightAbyssalBlessing 选择界面(RollCandidates 按族取下一级)、UIViewAbyssalBlessingInfoContent 常驻列表、UIPopupAbyssalBlessingInfo 详情气泡、Buff_AbyssalBlessingChange 事件。
tools: Read, Write, Edit, Glob, Grep, Bash
skill: abyssal-blessing-system
watched_files:
  - Assets/Scripts/Bean/Game/AbyssalBlessingEntityBean.cs
  - Assets/Scripts/Bean/MVC/Game/AbyssalBlessingInfoBean.cs
  - Assets/Scripts/Bean/MVC/Game/AbyssalBlessingInfoBeanPartial.cs
  - Assets/Scripts/Component/UI/Game/FightAbyssalBlessing/
  - Assets/Scripts/Component/UI/Common/AbyssalBlessing/
  - Assets/Scripts/Component/UI/Popup/UIPopupAbyssalBlessingInfo.cs
  - Assets/Scripts/Component/UI/Popup/UIPopupAbyssalBlessingInfoComponent.cs
  - Assets/Data/Excel/excel_abyssal_blessing_info[深渊馈赠信息].xlsx
  - Assets/Resources/JsonText/AbyssalBlessingInfo.txt
---

# 深渊馈赠 (Abyssal Blessing) 开发代理

你负责 `Assets/Scripts/Component/UI/Game/FightAbyssalBlessing/`、`Assets/Scripts/Component/UI/Common/AbyssalBlessing/`、`Assets/Scripts/Bean/Game/AbyssalBlessingEntityBean.cs` 及相关配置的深渊馈赠系统开发。

## 职责范围

### 数据
- **AbyssalBlessingInfoBean** - 馈赠配置 Bean（自动生成，禁改）
- **AbyssalBlessingInfoBeanPartial** - 馈赠配置扩展（写自定义逻辑）
- **AbyssalBlessingEntityBean** - 馈赠运行时实例（含 UUID）

### 配置（Excel + JSON）
- `excel_abyssal_blessing_info[深渊馈赠信息].xlsx` - 唯一真实源（数据写在 **AbyssalBlessingInfo** 工作表，三行表头：字段名/类型/中文说明）
  - 列：`id`(long) `icon_res`(string) `parent_id`(long) `level`(int) `buff_ids`(string,逗号分隔) `name`(language) `details`(language) `remark`(string) `valid`(int) `max_count`(int)
  - **`max_count`**：一局最多可获得（选取）次数，仅对可重复馈赠(`level<=0`)生效；`0`/留空=不限，`N>0`=最多 N 次（`1`=一次性）。升级链(`level>0`)留 0。**新增此列后须在 Unity 跑「生成 Entity」让 Bean 多出字段、再「导出 JSON」**。
  - **`valid`**：`1`=有效，`0`=无效。生成器检测到 `valid` 列即自动生成 `valid!=0` 过滤（`GetAllArrayData`/`GetItemData` 均排除），valid==0 的行运行时彻底不存在、不进候选池。⚠️ 新增行必须填 `1`（JSON 缺省 0 会被当无效）。详见 editor-extension-system SKILL「valid 有效性列约定」。
  - **一个等级 = 一行**；同族升级链：lv1 `parent_id=0`，lv2 `parent_id=lv1.id`，lv3 `parent_id=lv2.id`……`level` 从 1 连续递增（`level=0` 为不参与升级链的可重复馈赠）
  - **两个正交维度**：`level`/`parent_id` 管"强度升级链"，`max_count` 管"一局可获得次数"。类型：① 可重复·不限 `level=0,max_count=0`（重复叠加、无角标，如增殖）；② 可重复·限N次 `level=0,max_count=N`（一局最多N次；`N=1` 即"一次性"，现役无实例）；③ 多级升级链 `level=1..N` 链式（逐级升级、显示 LvN，`max_count` 留 0）。**「一次性」用 `level=0+max_count=1`，不再用废弃的 `level=1` 单行族**。次数门控落点 `UIFightAbyssalBlessing.IsCandidateEligible` + `BuffHandler.GetAbyssalBlessingPickCount`。
  - id 约定 10 位（如 `2000001005`，末 3 位=等级序号）
  - **现役配置**：共 43 行 11 族——增殖(1000001001)、奖励多多(1000002001)、再来一瓶(1000003001)、钱多多 5 级链(2000001001~005)、闪电 5 级链(2000002001~005，首个**周期型**馈赠：BUFF `3000300001~005`=`BuffEntityPeriodicAttackMultiInstant`，`class_entity_data`="次数,攻击模块ID"（半径/单雷命中上限配在攻击模块表 300031~035 的 collider_area_size/hit_max），每 3 秒 1~5 道落雷 0.1 秒间隔连发、不放回抽主目标——一轮内多道雷主目标互不重复(敌人少于雷数只发同等数量的雷)，溅射不受限(同一目标可被多道雷重复溅射、被溅射过的仍可作主目标)，AOE 多目标伤害按命中次序依次减半保底1，伤害=魔王攻击力×`trigger_value`；落雷粒子全局单例 `Effect_Thunder_3`(900003)→`EffectHandler.ShowThunderEffect`)、失控的矿车 5 级链(2000003001~005，周期型道路冲撞：BUFF `3000400001~005`=`BuffEntityPeriodicAttackRoad`，`class_entity_data`="车数,攻击模块ID"，5 级共用攻击模块 300041=`AttackModeRangedPiercingRoad`、视觉 `AttackModeVisual_Minecart_1`、图标 `ui_abyssalblessing_minecar`、sound_start=310001(出车音效 sound_clean_1，框架 0.1s 同音去重防同帧多车叠播)、sound_hit=420001，每 5 秒随机 1~5 条道路各从最左端驶出 1 辆矿车沿路向右碾压穿透整路、每个目标只撞一次，第 1 撞=魔王攻击力×`trigger_value`、之后每撞减半保底 1、驶到路尽头消失；车数>路数时每路先各 1 辆、多出随机重复分配、同路第 2 辆起间隔 0.5 秒)、死亡回旋 5 级链(2000004001~005，周期型回旋镖：BUFF `3000500001~005`=`BuffEntityPeriodicAttackBoomerang`，`class_entity_data`="镖数,攻击模块ID"，5 级共用攻击模块 300051=`AttackModeRangedBoomerang`、视觉 `AttackModeVisual_Boomerang_1`、图标 `ui_abyssalblessing_boomerang`、sound_start=400003(发射音效 sound_fight_3)，每 5 秒发射 1~5 枚回旋镖随机瞄准敌人(不放回抽目标、敌人少于镖数只发同等数量、0.2 秒间隔连发)，伤害=魔王攻击力×`trigger_value`、逐目标减半保底 1，镖到目标后超出一格折返回到魔王身边、返程可再命中)、跳跳斧 5 级链(2000005001~005，周期型弹跳斧头：BUFF `3000600001~005`=`BuffEntityPeriodicAttackBounceAxe`，`class_entity_data`="斧数,攻击模块ID,弹跳次数"，5 级共用攻击模块 300061=`AttackModeRangedArcBounce`、视觉 `AttackModeVisual_Axe_1`、图标 `ui_abyssalblessing_axe`、sound_start=400003(发射音效 sound_fight_3)、sound_hit=420001，每 5 秒扔出 1~5 把斧头抛物线追踪飞向随机一排的最远敌人(按 roadIndex 分组同路 x 降序、不放回抽取首目标互不重复、敌人少于斧数只扔同等数量、0.2 秒间隔连发)，伤害=魔王攻击力×`trigger_value`、逐目标减半保底 1，命中后弹跳至附近 1 单位内另一目标(Lv1~5 弹跳 1~5 次、同一斧头目标不重复、无候选即停)，弹跳也是抛物线分段(弧高=首段一半，每段仅下落阶段命中)，弹跳搜索半径配在攻击模块表 collider_area_size 第 1 项=1)、知识的力量 5 级链(2000006001~005，首个**常驻环绕型**馈赠：BUFF `3000700001~005`=`BuffEntityPeriodicAttackOrbit`，`class_entity_data`="书本数,攻击模块ID,转速(弧度/秒)"，5 级共用攻击模块 300071=`AttackModeOrbit`(常驻不自毁、环绕宿主 XZ 圆周运动半径 0.75、高度取魔王位置+攻击起始偏移、attack_search_type=11 球形触碰、sound_hit=420001、残影拖尾)、视觉 `AttackModeVisual_Book_1`(复用图标贴图)、图标 `ui_abyssalblessing_book`；随机选 1 只最前排(x 最大)存活魔物为宿主环绕 1~5 本书(转速 Lv1~5=0.6/1.2/1.8/2.4/3 弧度/秒、角度均分)，触碰敌人伤害=魔王实时攻击力×`trigger_value`(0.4)、不暴击、每只书对同一敌人 0.5 秒冷却；宿主死亡/有更前排魔物→改选(并列随机 1 只、并列不换防抖动)，无己方魔物→书本全销毁有再生成，关卡切换被回收→整套重建)、回弹菱块 5 级链(2000007001~005，**常驻反弹弹球型**馈赠：BUFF `3000800001~005`=`BuffEntityPeriodicAttackRebound`，`class_entity_data`="弹数,攻击模块ID"，攻击模块 300081~300085=`AttackModeRangedRebound`(直线弹道、道路矩形 x∈[0.5,0.5+路长] z∈[0.5,路数+0.5] 内四壁永久反弹±5°随机偏转防90°死角、左墙入场后才生效、同目标1秒命中冷却、永不自毁)、视觉 `AttackModeVisual_Pingpang_1`(贴图 `Textures/Game/Pingpang_1.png` 复用图标)、图标 `ui_abyssalblessing_pingpang`；场上永久维持 1~5 颗菱块(不销毁、不累积、总数恒定=等级)，BUFF 每秒自检补弹(关卡切换被 ClearAttackModePrefab 清掉后自动补回、升级替换/清空时 ClearData 销毁存量弹)，初始角度前向锥 ±75° 随机、无敌人也照常发射，击中伤害=魔王实时攻击力×`trigger_value`(0.5)、不暴击不递减、同一目标 1 秒冷却；弹速 Lv1~5=2/2.5/3/3.5/4 配在攻击模块行 speed_move)、第六次冲击 5 级链(2000008001~005，周期型圆环冲击波：BUFF `3001100001~005`=`BuffEntityPeriodicAttackShockwave`，`class_entity_data`="攻击模块ID"，5 级共用攻击模块 300091=`AttackModeShockwaveRing`(以魔王为圆心半径按 speed_move=6 扩张、XZ 环带命中自遍历不走射线批处理、每敌每波只中一次、最大半径=道路右缘+余量0.5−圆心x=覆盖整条道路、命中按「圆心→敌人」瞬时击退0.5=collider_area_size 第 2 项、落点 x 钳制道路范围)、sound_start=420001(占位待定)、图标 `ui_abyssalblessing_boom`，视觉 `Effect_Shockwave_1`(1700001)走 `EffectHandler.ShowShockwaveEffect`(startSize/startLifetime multiplier 同步半径与扩张时长)；每 10 秒从魔王位置+攻击起始偏移处发出一道冲击波沿整条道路扩散，扫到的敌人受魔王实时攻击力×`trigger_value`(Lv1~5=0.1~0.3)伤害(保底1、不暴击)并被击退0.5(交 AI 击退意图：方向固定 +x 沿道路向后推、固定0.2s推完、攻击循环打断、结束回闲置重索敌)，场上无敌人本轮不触发；首个带**击退机制**的馈赠——击退为攻击模块内的瞬时位移，z 偏移由敌人 AI 移动意图自动归位)。瓶装炼狱火 5 级链(2000009001~005，周期型抛物线地形火焰：BUFF `3001200001~005`=`BuffEntityPeriodicAttackFireBottle`，`class_entity_data`="瓶数,攻击模块ID"，5 级共用攻击模块 300101=`AttackModeRangedArcGround`(双状态：飞行段抛物线飞向**固定落点不追踪**、纯投掷物禁用命中、弹体自旋-720°/s绕-Z轴，到达切燃烧段驻留每1秒对半径1.2范围跳伤、满5秒自毁；燃烧段 visualBucketKey 置空隐藏 DSP 弹体，火焰视觉走 `EffectHandler.ShowFloorFireEffect` 全局单例 burst 重播粒子 `Effect_FloorFire_1`(1800001))、视觉 `AttackModeVisual_FireBottle_1`(贴图复用馈赠图标)、图标 `ui_abyssalblessing_fire`、sound_start=400003(发射音效 sound_fight_3)；每 10 秒向不放回抽取的 1~5 个敌人投掷炼狱火瓶(第1瓶立即+0.2秒间隔连发、敌人少于瓶数只丢同等数量、同轮多瓶不瞄同一目标)，瓶子抛物线飞向该目标当前位置(不追踪、目标中途死亡/移动照飞原落点)，落地燃放半径1.2地形火焰持续5秒，每1秒对火焰上敌人造成魔王实时攻击力×`trigger_value`(0.1)伤害(保底1、不暴击、不递减——多片叠加可多次跳伤)，场上无敌人本轮不触发)。单体定向 4 个（大力出奇迹等 1000004~7xxx）、固定数值 6 族（强身健体等 2000002~7xxx）、动态率 6 族（都是兄弟/杀红了眼 2000008~13xxx）已删除，**C# 基础设施保留**；单行族号 4~7、多级族号 10~13（8 已被第六次冲击占用、9 已被瓶装炼狱火占用）、BUFF 族号 9 与 13~20（11 已被第六次冲击占用、12 已被瓶装炼狱火占用）已释放可复用。
- `AbyssalBlessingInfo.txt` - Excel 导出 JSON（不可单独改）
- `Language_AbyssalBlessingInfo_{cn,en}.txt` - 多语言

### UI 组件
- **UIFightAbyssalBlessing** - 征服模式关卡间馈赠选择界面（随机 3 选 1）
- **UIViewFightAbyssalBlessingItem** - 候选项（带等级 BUFF 预览）
- **UIViewAbyssalBlessingInfoContent** - 战斗界面常驻已选馈赠列表
- **UIViewAbyssalBlessingInfoContentItem** - 已选馈赠列表项
- **UIPopupAbyssalBlessingInfo** - 馈赠详情气泡

### 流程入口
- **GameFightLogicConquer.ActionForUIFightSettlementNext** - 关卡间触发选择
- **GameFightLogicConquer.ActionForUIFightAbyssalBlessingSelect/Skip** - 选择/跳过回调
- **GameFightLogicConquer.ActionForUIRewardSelectEnd** - 全通关后清空馈赠
- **FightBeanForConquer.AddAbyssalBlessing** - 添加馈赠到征服数据

### 升级链（核心机制，配置表自身负责）
- **AbyssalBlessingInfoCfg.GetFamilyRootId(id)** - 沿 `parent_id` 回溯到族根（parent_id==0），防循环 64 层 + 缓存
- **AbyssalBlessingInfoCfg.GetFamilyMaxLevel(rootId)** - 族内最大 level（带缓存，level==0 不计入）
- **AbyssalBlessingInfoCfg.GetItemDataByFamilyLevel(rootId, level)** - 按族根+等级取目标行配置（遍历全表带缓存，仅供测试等低频场景；战斗测试的馈赠等级选择即走它）
- **AbyssalBlessingInfoCfg.IsSingleLevelOnce(info)** - 单级不可重复判定（`level==1` 且 `GetFamilyMaxLevel(族根)==1`），仅用于 UI 隐藏等级角标
- **AbyssalBlessingInfoBean.IsLevelUp()** - `level > 0`
- 升级链**由馈赠表 `parent_id`/`level` 定义，与 BUFF 的 buff_parent_id/buff_level 无关**（旧设计已废弃）

### BUFF 联动
- **BuffHandler.AddAbyssalBlessing** - 添加馈赠：`GetFamilyRootId` → `RemoveAbyssalBlessingByRootId`(移除同族旧级) → 解析 `buff_ids` 加到防守核心 → 触发事件。⚠️ **必须在防守核心创建后调用**（战斗中或 `GameFightLogic.PreGameForAfterCreateDefenseCore` 钩子），此前调用 `LogWarning` 跳过（核心未创建时曾 NRE，已加防御）
- **BuffHandler.GetAbyssalBlessingOwnedLevel(rootId)** - 查询某族当前拥有等级（传**族根 id**，0=未拥有）
- **BuffHandler.GetAbyssalBlessingPickCount(rootId)** - 查询某族一局已选取次数（数容器内同族实例数；用于可重复馈赠 `max_count` 候选门控）
- **BuffHandler.RemoveAbyssalBlessingByRootId(rootId)** - 移除某族的所有馈赠及其 BUFF（升级时用）
- **BuffHandler.GetDefenseCoreUUID** - 馈赠 BUFF 的目标/施加者（防守核心）
- **BuffManager.dicAbyssalBlessingBuffsActivie** - 独立的馈赠 BUFF 容器（key=馈赠实例）
- **BuffManager.AddAbyssalBlessingEntity / AddAbyssalBlessingBuff** - 写入容器
- **BuffManager.ClearAbyssalBlessing** - 清空所有馈赠（只在全通关后调）
- **单体定向馈赠（随机一只防守生物属性/攻速翻倍）**：`level=0` + `max_count=N`（`1`=整局限 1 次；改 `max_count` 即可调次数、与 BUFF 逻辑独立），效果只作用于随机一只防守生物（非全体/非核心）。⚠️ **现役无配置**：原 4 个（大力出奇迹 1000004001/ATK、膘肥体壮 1000005001/HP、钢铁憨憨 1000006001/DR、急性子 1000007001/攻速）已于 2026-07 删除，基础设施保留可复用。BUFF 实体 `BuffEntityAttributeSingleTarget`/`BuffEntityAttributeAttackTimeSingleTarget` 实现 `IBuffSingleTarget`(仅暴露 `SingleTargetCreatureUUId`)，`SetData` 时 `fightData.GetRandomDefenseCreatureUUId()`(实例方法在 `FightBean`) 随机锁定一只 UUID；属性类在 `FightCreatureBean.CollectFromBuffList`、攻速类在 `BuffHandler.ChangeAttackTimeDataForBuff` 按 `SingleTargetCreatureUUId` 过滤。**绝不能改 `dlDefenseCreatureData` 里 CreatureBean 的 creatureAttribute（与存档共享引用，会污染永久存档）**，只改运行时 dicAttribute/攻击时间。图标历史用 `ui_abyssalblessing_11~14`（闲置未删）。
  - **复制魔物(增殖)不继承单体定向**：`BuffEntityInstantCloneDefenseCreature` 克隆出的魔物是**新 UUID**，与单体定向馈赠锁定的原魔物 UUID 不匹配，故不显示也不继承；克隆体只继承「作用于全体防守生物」的馈赠(靠 `trigger_creature_type` 过滤、与 UUID 无关，新魔物 `RefreshBaseAttribute` 时自动生效)。
  - **战斗卡片展示**：`UIViewCreatureCardItemForFight` 用 `AbyssalBlessingUtil.IsAbyssalBlessingTargetCreature(buff, creatureData, FightDefense)`(在 `Assets/Scripts/Utils/AbyssalBlessingUtil.cs`；trigger_creature_type + 单体定向 UUID + 仅属性/攻速BUFF 三连) 取「实际作用于本魔物」的馈赠图标展示——含全体防守加成，排除敌方/核心/掉落/奖励/复制类。详见 abyssal-blessing-system / buff-system / creature-card-system SKILL。
- **动态数值馈赠（加成率随战况实时算）**：加成率随战况**每次重算属性时动态算**（非配置写死）。BUFF 继承抽象基类 `BuffEntityAttributeDynamicRate : BuffEntityAttribute`（重写 `CollectModifiers`+`ChangeData` 用 `GetDynamicRate()` 替代固定率、仅走 PercentAdd，用于 ATK/DR/HP；`trigger_creature_type=1` 不含核心）；子类为**通用功能类**（按缩放来源命名、不绑馈赠名，可被其它同功能馈赠复用）。⚠️ **现役无配置**：原 6 族（都是兄弟/杀红了眼，各 3 属性，每族 5 级共 30 行）已于 2026-07 删除，`HasDynamicRateAbyssalBlessing()` 当前恒 false、广播不触发；基础设施保留可复用：
  - 随场上魔物数缩放（全体）：`BuffEntityAttributeScaleByDefenseCount`(data=ATK/DR/HP)，`rate=(场上存活防守魔物数N-1)×trigger_value_rate`（N 数 `dlDefenseCreatureEntity.List` 中 `!IsDead()`，N≤1 为 0）。曾用于都是兄弟（馈赠 id 2000008~10xxx，BUFF 30015~17xxx，图标 120~122，均已删/段已释放）。
  - 随击杀数缩放（**单体定向**，兼 `IBuffSingleTarget`）：`BuffEntityAttributeScaleByKillCount`(data=ATK/DR/HP)，选取时 `SetData` 用 `GetRandomDefenseCreatureUUId()` 随机锁定一只，`rate=fightRecordsData.GetRecordsForCreatureData(锁定UUID,false)?.killNum×trigger_value_rate`（仅魔物击杀；killNum 按 `creatureUUId` 持久累积，跨关卡保留）。过滤由 `FightCreatureBean.CollectFromBuffList` 的 `IBuffSingleTarget` 落点自动完成。曾用于杀红了眼（馈赠 id 2000011~13xxx，BUFF 30018~20xxx，图标 123~125，均已删/段已释放）。
  - 每级 `trigger_value_rate=0.01~0.05`（每只/每杀 +1%~5%）；语言 BUFF 用 `{Percentage}` 占位、馈赠逐级写死。**当前 id 进度**：现役深渊馈赠 BUFF 段（`buff_type=3`）→ `3000100001`、`3000200001~005`、`3000300001~005`（闪电）、`3000400001~005`（失控的矿车）、`3000500001~005`（死亡回旋）、`3000600001~005`（跳跳斧）、`3000700001~005`（知识的力量）、`3000800001~005`（回弹菱块）、`3000900001`、`3001000001`、`3001100001~005`（第六次冲击）、`3001200001~005`（瓶装炼狱火）；BUFF 族号 9、13~20 与馈赠族号 10~13 已释放。
  - **广播重算（rate 变化才生效，事件驱动）**：泛型守卫 `BuffHandler.HasDynamicRateAbyssalBlessing()`（通用：馈赠池含指定类型/子类 BUFF 才广播，避免普通对局开销）+ 入口 `GameFightLogic.RefreshAllDefenseCreatureAttribute()`（public，刷新防守核心+全体防守魔物 `RefreshBaseAttribute`，由原 `EventForAbyssalBlessingChange` 循环抽出）。两处广播：① `GameFightLogic.EventForGameFightLogicCreatureDeadEnd`（死亡→N变/击杀数变，重算放在 `CheckGameEnd()` 之前）；② `CreatureHandler.CreateDefenseCreatureEntity` 末尾**推送新事件** `EventsInfo.GameFightLogic_DefenseCreatureCreate`（参数 FightCreatureEntity）→ `GameFightLogic.EventForDefenseCreatureCreate` 监听后按守卫广播（放置/增殖；CreatureHandler 只生成、推事件，重算职责归 GameFightLogic）。继承 `BuffEntityAttribute`，天然被卡片展示/`GetAttribute(true)` 判定通过，无需改判定（全体类全显示；单体定向类因兼 `IBuffSingleTarget`，仅在锁定那只魔物卡上显示图标）。

### 事件
- **EventsInfo.Buff_AbyssalBlessingChange** - 馈赠变化（参数 AbyssalBlessingEntityBean）

### 图标资源
- **专用图集** `AtlasForAbyssalBlessing.spriteatlas` 存放所有馈赠图标，所有馈赠相关 UI 图标必须放入此图集
- **枚举映射** `SpriteAtlasTypeEnum.AbyssalBlessing`（`Assets/FrameWork/Scripts/Enums/BaseGameEnum.cs`）
- **加载入口** `IconHandler.Instance.SetAbyssalBlessingIcon(iconName, image)` —— 馈赠图标统一走此方法，禁止用 `SetUIIcon`

## 关键流程

```
关卡结算 → 非最后一关 → 打开 UIFightAbyssalBlessing.SetData
                          ↓
          RollCandidates(SHOW_NUM=3)：按 GetFamilyRootId 分族 →
          每族取"当前等级+1"那一行(未拥有取族根) → 洗牌取前 3
                          ↓
                       玩家选择 3 选 1（或跳过）
                          ↓
          FightBeanForConquer.AddAbyssalBlessing(info)
                          ↓
          new AbyssalBlessingEntityBean(info)   // 构造函数自动生成 UUID
                          ↓
          BuffHandler.AddAbyssalBlessing(entity)
            ↓
            GetFamilyRootId → RemoveAbyssalBlessingByRootId(移除同族旧级)
            ↓
            解析 buff_ids(逗号分隔) → 添加到防守核心
            ↓
            触发 Buff_AbyssalBlessingChange 事件
            ↓
          UIViewAbyssalBlessingInfoContent 刷新

关卡全通关 → 领奖结束 → BuffHandler.manager.ClearAbyssalBlessing()
```

## 等级链替换机制（重点）

升级链**由馈赠配置表自身的 `parent_id` + `level` 定义**（链表式，每个等级一条独立配置行，`buff_ids` 只决定该级数值）：
1. 选择界面 `RollCandidates` 用 `GetAbyssalBlessingOwnedLevel(rootId)` 取当前等级，只展示 `level == owned+1` 那一行（玩家看到的即"将获得"的等级）
2. `BuffHandler.AddAbyssalBlessing` 添加时：`GetFamilyRootId(id)` → `RemoveAbyssalBlessingByRootId`(整条移除同族旧级) → 逐个解析 `buff_ids` 加到防守核心
3. `parent_id` 链断裂（某级缺失或指向错误）→ `RollCandidates` 取不到下一级，该族卡住
4. ⚠️ 与 BUFF 的 `buff_parent_id`/`buff_level` **无关**，那是旧设计已废弃

## 关键文件

| 文件 | 路径 |
|------|------|
| 馈赠配置 Bean | Assets/Scripts/Bean/MVC/Game/AbyssalBlessingInfoBean.cs（自动生成，禁改） |
| 馈赠配置扩展 | Assets/Scripts/Bean/MVC/Game/AbyssalBlessingInfoBeanPartial.cs |
| 馈赠运行时实例 | Assets/Scripts/Bean/Game/AbyssalBlessingEntityBean.cs |
| Excel 源表 | Assets/Data/Excel/excel_abyssal_blessing_info[深渊馈赠信息].xlsx |
| 导出 JSON | Assets/Resources/JsonText/AbyssalBlessingInfo.txt |
| 选择界面 | Assets/Scripts/Component/UI/Game/FightAbyssalBlessing/UIFightAbyssalBlessing.cs |
| 候选项 | Assets/Scripts/Component/UI/Game/FightAbyssalBlessing/UIViewFightAbyssalBlessingItem.cs |
| 常驻列表 | Assets/Scripts/Component/UI/Common/AbyssalBlessing/UIViewAbyssalBlessingInfoContent.cs |
| 常驻项 | Assets/Scripts/Component/UI/Common/AbyssalBlessing/UIViewAbyssalBlessingInfoContentItem.cs |
| 详情气泡 | Assets/Scripts/Component/UI/Popup/UIPopupAbyssalBlessingInfo.cs |
| 图集 | Assets/LoadResources/Textures/SpriteAtlas/AtlasForAbyssalBlessing.spriteatlas |
| 图集枚举 | Assets/FrameWork/Scripts/Enums/BaseGameEnum.cs（SpriteAtlasTypeEnum.AbyssalBlessing） |
| 图标加载入口 | Assets/Scripts/Component/Handler/IconHandler.cs（SetAbyssalBlessingIcon） |
| BUFF 集成 | Assets/Scripts/Component/Handler/BuffHandler.cs（深渊馈赠BUFF region） |
| BUFF 容器 | Assets/Scripts/Component/Manager/BuffManager.cs |
| 征服流程 | Assets/Scripts/Game/Logic/GameFightLogicConquer.cs |
| 数据持有 | Assets/Scripts/Bean/Game/FightBeanForConquer.cs |
| 动态率馈赠BUFF（现役无配置，机制留存；曾用于都是兄弟/杀红了眼） | Assets/Scripts/Game/Buff/BuffEntity/Attribute/BuffEntityAttributeDynamicRate.cs（基类）/ BuffEntityAttributeScaleByDefenseCount.cs（通用"随场上魔物数缩放"）/ BuffEntityAttributeScaleByKillCount.cs（通用"随累计击杀数缩放"） |
| 动态馈赠广播重算 | Assets/Scripts/Game/Logic/GameFightLogic.cs（RefreshAllDefenseCreatureAttribute 全体重算 + 死亡事件在 CheckGameEnd 前按守卫广播 + EventForDefenseCreatureCreate 监听放置事件按守卫广播）/ Assets/Scripts/Component/Handler/BuffHandler.cs（O(1) 缓存守卫 HasDynamicRateAbyssalBlessing 读 BuffManager.hasDynamicRateAbyssalBlessing；缓存在 AddAbyssalBlessing 选取动态率馈赠时单调置 true、ClearAbyssalBlessing 复位）/ Assets/Scripts/Component/Handler/CreatureHandler.cs（CreateDefenseCreatureEntity 末尾只推送 GameFightLogic_DefenseCreatureCreate 事件，不直接重算） |

## 约束

- 配置变更**必须改 Excel**（`excel_abyssal_blessing_info`），由 Unity 编辑器导出 JSON。仅改 JSON 会在下次导出被覆盖。
- `AbyssalBlessingInfoBean.cs` 是自动生成的，**禁止直接修改**；扩展写到 `AbyssalBlessingInfoBeanPartial.cs`。
- 添加馈赠必须经过 `BuffHandler.AddAbyssalBlessing`，**不要直接写 `manager.dicAbyssalBlessingBuffsActivie`**（会跳过同族替换 + 事件通知）。
- 升级链由**馈赠表 `parent_id`+`level`** 定义：`parent_id` 链表式逐级指向上一级 id（lv2→lv1，lv3→lv2），**不是都指向根**；`level` 从 1 连续递增。链断裂该族会卡住。
- 馈赠 BUFF 目标固定为**防守核心**（CreatureFightTypeEnum.FightDefenseCore），施加者也是核心 UUID。
- `ClearAbyssalBlessing` **只能在征服全通关 + 领奖结束后调用**，中途调用会丢失玩家选择。
- `GetAbyssalBlessingOwnedLevel` 必须传**族根 id**（`GetFamilyRootId` 取得），不是任意等级的 id。
- 配置数据写在 Excel 的 **`AbyssalBlessingInfo`** 工作表（不存在 `Sheet1`/`Sort Title`）；改完 Excel 必须用 Unity 编辑器导出 JSON。
- BUFF 具体实体类型 / 触发逻辑 / 属性管线请走 `game-buff` 代理 + `buff-system` SKILL。
- 馈赠图标必须放入 `AtlasForAbyssalBlessing.spriteatlas`，加载只能走 `IconHandler.Instance.SetAbyssalBlessingIcon`；用 `SetUIIcon` 会去 UI 图集查找导致丢图。

## 关联 Skill 与 Agent

- 详细开发指南: [abyssal-blessing-system](../skills/abyssal-blessing-system/SKILL.md)
- BUFF 实体开发: `game-buff` agent + `buff-system` skill
- 征服模式战斗流程: `game-fight-logic` agent + `game-fight-system` skill
- 选择界面 UI 通用约束: `ui-game` agent
- 详情气泡 UI 通用约束: `ui-popup` agent
- 配置表 Excel 导入导出: `data-excel` agent + `excel-io` skill
