---
name: reference_world_quick_attack_research
description: 世界「加快进攻节奏(Quick)」+「0倍速游戏(Speed0)」+「2倍速游戏(Speed2)」研究——战斗进度条 Quick 按钮 +10% 推进 / Speed0 按钮整场暂停 / Speed2 按钮整场2倍速 + id 块约定(nn=30~39 Quick / 40~49 Speed2 / 50~59 Speed0)仅是 Excel 填表规范,起始id存世界表 unlock_id_quick_attack/unlock_id_speed2/unlock_id_speed0 列 + 难度研究拆分(nn=12~20) + pre_data 通关前置；Speed0/Speed2 互斥,F1/F2快捷键
metadata:
  type: reference
---

战斗界面进攻进度条(`UIViewFightMainAttCreateProgress`, 仅征服模式显示)上有两个世界绑定的按钮：

**① Quick 按钮**：点一次立即向前推进「总进攻时长10%」的进度、并把这段时间本应生成的进攻波次全部立即生成；无消耗无冷却，到100%封顶点击无效；进度条用 0.3s DOTween 平滑过渡不瞬跳。
- `GameFightLogic.QuickAdvanceAttackCreate(advanceRate=0.1f)`——用与逐帧刷怪 `UpdateGameForAttackCreate` 同一套「累加达标即出下一波」步进语义逐波消费推进时间(含 BOSS 特写 `ShowBossDialog`)，队列耗尽即停、进度封顶，返回最新进度。
- `UIViewFightMainAttCreateProgress.OnClickForQuick`(BaseUIView.OnClickForButton 派发 `ui_Quick`)→ `GetAttackProgress()>=1` 则 return，否则推进后 `SetProgress(newProgress, animTime:0.3f)`。

**② Speed2 按钮(RadioButtonView)**：切换整场战斗 2 倍速。**默认不开启**，点击切换 2倍/原速，速度挂在 `fightData.gameSpeed` 上**仅本场战斗有效**(征服 run 内跨关卡保留，新 run 重建 fightData 自然还原)。
- 链路：`UIViewFightMainAttCreateProgress`(实现 `IRadioButtonCallBack`, Awake 里 `ui_Speed2_RadioButtonView.SetCallBack(this)`) → `RadioButtonSelected` → `OnSpeed2Changed` → `GameFightLogic.SetGameSpeed(isOpen ? GAME_SPEED_2X(2) : 1)`。
- **加速原理(不是 TimeScale)**：`SetGameSpeed` 只改 `fightData.gameSpeed`；① `UpdateGame` 的 `updateTime = Time.deltaTime * fightData.gameSpeed`(刷怪/BUFF/魔王/RCD)；② AI意图(移动/攻击/索敌/死亡计时)、弹道飞行、掉落物寿命等不经 UpdateGame 驱动的战斗系统统一用 `GameFightLogic.GetFightDeltaTime()`(=Time.deltaTime×当前速度，非战斗恒1倍) 替代 `Time.deltaTime`——**新增战斗内计时逻辑必须用 GetFightDeltaTime()**。Time.timeScale 保留给暂停(UIGameSystem)/BOSS特写减速(UIDialogBossShow)，互不冲突。
- **动画同步**：`SetGameSpeed` 内 `RefreshAllCreatureAnimTimeScale()` 给全部在场生物 `SetAnimTimeScale(gameSpeed)`(=`SkeletonAnimation.timeScale`，与 PlayAnim 的 animSpeed 相乘叠加)；新建生物在 `FightCreatureEntity.SetData` 里自动按当前速度初始化。

**③ Speed0 按钮(RadioButtonView)**：切换整场战斗 **0 速**（=暂停：游戏时间流速归零，一切走 gameSpeed 的逻辑与生物动画全部冻结）。与 Speed2 **互斥**——开启一方时若另一方选中则 `ChangeStates(false)` 取消（速度由开启方直接 SetGameSpeed，不走回调）。
- 链路同 Speed2：`RadioButtonSelected` → `OnSpeed0Changed` → `GameFightLogic.SetGameSpeed(isOpen ? GAME_SPEED_0X(0) : 1)`（常量 `GAME_SPEED_0X=0f` 在 GameFightLogic）。
- **2026-10-12 起改为研究门控**（原为默认开放无需研究）：与 Speed2 同口径——仅征服模式且已解锁当前世界当前难度的「0倍速游戏」研究才显示（`RefreshSpeed0ButtonShow` → `CheckIsUnlockWorldSpeed0`）；显示时调 `RefreshSpeed0State()`（`gameSpeed<=0` 选中）同步选中态（BOSS关重载场景后不丢）。研究 id 段 nn=50~59（world1=100310150~159），起始id存世界表 `unlock_id_speed0` 列（该字段暂在 GameWorldInfoBeanPartial 临时 region，重新生成 Entity 后删除）。
- **快捷键**：F1=切换 Speed0、F2=切换 Speed2——View 重写 `OnInputActionForStarted`（`InputActionUIEnum.F1/F2`，inputactions 已绑定 `<Keyboard>/f1`/`f2`），走 `SetStates(!isSelect)` 与点击同路径；按钮隐藏（未解锁研究/进度条视图隐藏）时对应快捷键无效。

**显隐链路**：`UIFightMain.RefreshUIData` → `RefreshQuickButtonShow` / `RefreshSpeed0ButtonShow` / `RefreshSpeed2ButtonShow`——仅征服模式且 `UserUnlockBean.CheckIsUnlockWorldQuickAttack / CheckIsUnlockWorldSpeed0 / CheckIsUnlockWorldSpeed2(worldId, difficultyLevel)`(当前世界**当前难度**的研究已解锁)才显示对应按钮；Speed0/Speed2 显示时还调 `RefreshSpeed0State()/RefreshSpeed2State()` 把选中态对齐 `fightData.gameSpeed`(BOSS关重载场景后不丢)。worldId/difficultyLevel 取 `FightBeanForConquer.gameWorldInfoRandomData` 同名字段。

**研究节点显示前置**：Quick(难度D) 节点需**通关过该难度**才出现在研究界面——配置表 `pre_data` 列驱动(Quick 行填 `World1ConquerCompleteCount{D}:1`)，`UIBaseResearch.CheckPreIsUnlock` 在 `pre_unlock_ids` 之外追加 `ResearchInfoBean.CheckPreDataIsMeet()`；条件枚举 `ResearchPreConditionEnum`(World1ConquerCompleteCount1~10)，通关次数取自成就统计 `GetConquerCompleteCount`。难度1 也有 Quick 研究(100310130, pre 留空, pre_data=World1ConquerCompleteCount1:1)。**Speed2 研究 pre=同难度 Quick id、`pre_data` 留空**——Quick 的通关显示前置已把关。**Speed0 研究(2026-10-12 起) pre=同难度征服难度研究**(难度1无难度研究、pre 留空)、`pre_data` 留空——不再挂 Quick，只需解锁当前难度即显示、不受 Quick 通关前置影响。

**世界绑定 id 块约定(关键、非显然)**：每个世界的专属解锁 id 都落在 `1003_10_W_nn` 块内(W=世界id, 每世界独占100个id)。块内偏移：01=世界解锁 / 02=无尽 / **12~20=征服难度(难度2~10各一个独立研究, nn=10+难度)** / **30~39=各难度Quick(nn=29+难度, 段基址30)** / **40~49=各难度2倍速(nn=39+难度, 段基址40)** / **50~59=各难度0倍速(nn=49+难度, 段基址50)**。2026-07 起难度研究已从单节点 `level_max=9` 拆分为9个独立节点(链式前置)，Quick/0倍速/2倍速也按难度拆分(难度1~10各一个)。**该块约定只是 Excel 填表编号规范，代码不按常量推导**——起始id存世界表列：`unlock_id_conquer_difficulty_level`(难度,如100310112) / `unlock_id_quick_attack`(Quick,如100310130) / `unlock_id_speed2`(2倍速,如100310140) / `unlock_id_speed0`(0倍速,如100310150)，代码统一做 `起始id + (难度-1)`(起始id=0=该世界未配置, Get 方法返回-1 以避开 CheckIsUnlock(0) 恒真约定)；**新增世界只需在世界表填这四列**。旧的 `UserUnlockBean.WORLD_UNLOCK_BLOCK_BASE / WORLD_QUICK_ATTACK_UNLOCK_OFFSET / WORLD_SPEED2_UNLOCK_OFFSET` 常量已于 2026-07-20 删除(ID重编号只需改 Excel)。`GetUnlockGameWorldConquerDifficultyLevel` = `conquerDifficultyMax` + 从难度起始id连续向后统计的已解锁个数(断档即止)。

**落表(world1)**：难度研究9行 `excel_research_info`(id=100310112~120, research_type=4 世界分类, icon ui_research_11, level_max=1, 难度2无前置/难度D前置=难度D-1 id, pay 阶梯 100/500/1000/2000/4000/8000/16000/32000/64000, x=-160, y=(难度-2)*320——2026-10-12 行距由160加倍为320, y 范围0~2560, 无尽链/Quick行同步加倍) + Quick研究10行(id=100310130~139, icon ui_research_66, 难度1 pre 留空/难度2~10 pre=同难度难度id, pre_data=World1ConquerCompleteCount{难度}:1, pay=同难度征服难度研究×2(难度1固定100)：100/200/1000/2000/4000/8000/16000/32000/64000/128000, x=160, 难度1 y=-320/难度D≥2 y=(D-2)*320) + **0倍速研究10行(id=100310150~159, icon ui_research_94(占位,与2倍速同款), pre=同难度征服难度研究id(难度1留空)——2026-10-12 起不再挂 Quick、不受通关显示前置影响, pre_data 留空, pay=同难度2倍速的一半(难度1~10=50/100/500/1000/2000/4000/8000/16000/32000/64000), x=160(与Quick同列), y=同难度Quick行+180(正上方,2026-10-13 由+80整体上移100))** + **2倍速研究10行(id=100310140~149, icon ui_research_94(三道速度线), pre=同难度Quick id, pre_data 留空, pay=同难度Quick同价, x=480, y=与同难度Quick同行)** + `excel_unlock_info` 同 id 各一行(unlock_type=0) + `excel_language` ResearchInfo(cn 征服难度2~10 / 加快进攻节奏(难度1~10) / 0倍速游戏(难度1~10) / 2倍速游戏(难度1~10)；en Conquest Difficulty / Faster Assault / 0x Game Speed / 2x Game Speed；0倍速12语言已翻译)。0/2倍速布局(2026-10-13 更新)：0倍速=Quick 同列正上方+180(由+80上移100), 2倍速=Quick 同行下一列(x=480)；难度1行整体在 y=-320(挑战100勇士概率100300007(-160,-320)/真勇者挑战100300009(-480,-320)同步下移)。目前只有剑与魔法(world1)落了这些节点，世界表(world1行)已填 `unlock_id_quick_attack=100310130` / `unlock_id_speed2=100310140` / `unlock_id_speed0=100310150`(world2~4 该三列为0=未配置)。

详见 [[research-system]] 「世界分支」与 game-fight-system SKILL。研究门控模式同 [[reference_portal_reward_pregen_research_gate]]。
