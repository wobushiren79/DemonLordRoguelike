---
name: story-system
description: Demon Lord Roguelike 游戏的故事演出(Story/新手引导/剧情演出)系统开发指南。使用此SKILL当需要新增/修改故事演出配置(StoryInfo/StoryDetailsInfo/StoryTalkInfo 三表)、演出步骤(对话/镜头移动/等待/特效/音效/淡入淡出)、触发条件(首次进基地/首次进战斗/首次掉魔晶)、演出运行时(StoryHandler/StoryManager)、故事演出编辑器(StoryEditorWindow)、故事演出测试(StoryTest)等，包括 StoryEnum、三个 BeanPartial、UIGameConversation.SetDataForStory 旁白模式、CameraHandler 故事镜头 API、UserStoryBean 已播记录(独立存档 UserStory_{slot})等。
watched_files:
  - Assets/Scripts/Component/Handler/StoryHandler.cs
  - Assets/Scripts/Component/Manager/StoryManager.cs
  - Assets/Scripts/Enums/StoryEnum.cs
  - Assets/Scripts/Bean/Game/UserStoryBean.cs
  - Assets/Scripts/Bean/MVC/Game/StoryInfoBeanPartial.cs
  - Assets/Scripts/Bean/MVC/Game/StoryDetailsInfoBeanPartial.cs
  - Assets/Scripts/Bean/MVC/Game/StoryTalkInfoBeanPartial.cs
  - Assets/Editor/StoryEditorWindow.cs
  - Assets/Data/Excel/excel_story_info[故事信息].xlsx
  - Assets/Data/Excel/excel_story_details_info[故事详情信息].xlsx
  - Assets/Data/Excel/excel_story_talk_info[故事对话信息].xlsx
---

# 故事演出系统开发指南

## 系统定位

故事演出系统承载**新手引导**与后续**剧情演出**：按配置触发（进基地/进战斗/掉魔晶等时机）→ 锁定输入（战斗场景叠加暂停）→ 按步骤表顺序执行演出（对话/镜头/等待/特效/音效/淡入淡出）→ 恢复并记录存档（只播一次）。

第一期只有「引导」触发类型（trigger_type=1）；对话树/选项/议会触发等为预留扩展点（见文末）。

## 三张配置表（Excel 唯一真实源）

| 表 | 文件 / sheet | 职责 |
|---|---|---|
| 故事表 | `excel_story_info[故事信息].xlsx` / `StoryInfo` | 触发类型/演出场景/触发条件/优先级/只播一次/有效 |
| 步骤表 | `excel_story_details_info[故事详情信息].xlsx` / `StoryDetailsInfo` | 每行一个演出步骤，step_order 升序执行 |
| 对话表 | `excel_story_talk_info[故事对话信息].xlsx` / `StoryTalkInfo` | 对话内容（npc_id=0 为旁白） |

多语言：excel_language 有同名 sheet `StoryInfo`（故事名）与 `StoryTalkInfo`（对话内容）；**textId 约定 = 业务行 id**（同 ConversationCouncilorInfo 先例）。Bean 生成 `name_language`/`content_language` 懒加载属性。

### StoryInfo 列
`id | name[language] | trigger_type(1引导 2剧情预留) | scene_type(1基地 2战斗 3议会) | trigger_condition(1首次进基地后 2首次进战斗后 3战斗首次掉魔晶) | priority(小先播) | is_once | valid | remark`

### StoryDetailsInfo 列与 param 语义表（三方同步：Excel 表头 / BeanPartial 注释 / 本表）
`id(约定=story_id*1000+step_order) | story_id | step_order | step_type | is_async(0阻塞 1并发) | param_1~4 | remark`

| step_type | param_1 | param_2 | param_3 |
|---|---|---|---|
| Talk(1) 对话 | talk_id（`&`分隔=同一步内顺序连播多句，每句各等一次点击） | — | — |
| CameraMove(2) 镜头 | 目标标记（基地: self/core/portal/gashapon/juicer/altar/vat/achievement/council；战斗: core；通用: back=回演出起始位） | 时长秒(默认1) | 缓动DOTween序号(默认0=默认缓动) |
| Wait(3) 等待 | 秒（实时，不受 timeScale 影响） | — | — |
| Effect(4) 特效 | effect_id(EffectInfo.id) | 目标标记(空=战斗防守核心/基地魔王位) | 尺寸倍率(默认1) |
| Audio(5) 音效 | audio_id(AudioInfo.id) | — | — |
| Fade(6) 淡入淡出 | out=淡出变黑 / in=淡入 | 时长秒(默认0.5) | — |

### StoryTalkInfo 列
`id | story_id(所属故事 StoryInfo.id,0=通用;编辑器按此过滤对话下拉,新增对话自动绑定当前故事) | npc_id(NpcInfo.id, 0=旁白无立绘无名字无贿赂) | content[language] | remark`

## 运行时

### StoryHandler / StoryManager（Handler-Manager 配对）
- [StoryHandler.cs](Assets/Scripts/Component/Handler/StoryHandler.cs)：`BaseHandler<StoryHandler, StoryManager>`；`InitData()` **仅由 LauncherGame.Launch 调用**（测试场景不注册，自动触发天然关闭，测试走 `PlayStory` 强制播放）。
- 监听三事件：`World_EnterGameForBaseScene`→条件1、`GameFightLogic_StartGame`→条件2、`GameFightLogic_CreatureDeadDropCrystal`→条件3。
- `TryTriggerStory(condition)`：演出中/无存档丢弃；候选列表经 `StoryManager.dicConditionStories` 缓存（配置静态不变，不重复筛选排序）找第一个未播（`UserStoryBean.IsStoryPlayed`）且场景匹配的故事播放；一次事件最多播一个。**高频事件短路**（掉晶等）：候选全部为只播一次且已播完时把条件记入 `setExhaustedCondition`，后续事件一次 HashSet 查询秒退；`exhaustedForStoryData` 记录标记对应的存档实例，切换存档槽（实例变更）自动重建防误伤新档。
- [StoryManager.cs](Assets/Scripts/Component/Manager/StoryManager.cs)：纯状态（isInited/isStoryPlaying/currentStoryData/timeScaleOrigin/storyCameraOriginPos/cancelForStory）。

### PlayStory 主流程（PlayStoryAsync）
1. 锁输入：基地 `SetBaseControl(false, isHideControlTarget:false)`（魔王保持可见，与议会交谈同款）；战斗 `EnableAllControl(false)`；**锁后重新激活 controlTargetForEmpty**（EnableAllControl 会隐藏它，而演出镜头移动依赖 Cinemachine 跟随）。
2. 战斗场景：`Time.timeScale=0`（缓存原值，结束还原；先例 UIGameSystem）。
3. `CameraHandler.BeginStoryCameraControl(isFight)` 接管镜头并记录 `storyCameraOriginPos`。
4. 逐步执行：`is_async=1` → `_ = ExecuteStep`（并发，发起即下一步）；`=0` → `await ExecuteStep`（阻塞）。**"弹对话同时移动镜头"= 一个并发 CameraMove + 一个阻塞 Talk。**
5. `try/finally` 兜底 `FinishStory`：镜头归还 → 恢复 timeScale/控制（SetFightControl/SetBaseControl）→ is_once 记录 `UserStoryBean.MarkStoryPlayed` + SaveUserData（isTestSimulation 自动拦截）。

### unscaled 纪律（战斗演出 timeScale=0 下一切照常的关键）
演出系统内一切等待/补间必须 unscaled：`GTask.WaitReal`（不用 Wait）、DOMove/DOColor 一律 `.SetUpdate(true)`、打字机已改 `GTask.WaitReal`（UIGameConversation.TextAnimForContent）、WaitUntil 逐帧轮询不受 timeScale 影响。

### 对话复用（UIGameConversation.SetDataForStory）
`SetDataForStory(creatureObj, talkData, onEnd)`：npc_id≠0 → 复用现有 `SetData` 管线（spine/静态头像/名字/打字机）后强制隐藏 ui_Gift；npc_id=0（旁白）→ 隐藏 ui_Icon/ui_IconImg/ui_Gift、名字置空、直接起打字机。打开用 `UIHandler.OpenUI<UIGameConversation>()`（**不用 OpenUIAndCloseOther**，保留 UIFightMain 等场景 UI）。现有调用方（DoomCouncilLogic/LauncherTest）不受影响。

### 镜头 API（CameraHandler 游戏层 partial「故事演出镜头」region）
- `BeginStoryCameraControl(isFight)` → Transform：战斗=controlTargetForEmpty（cm_Fight 本就跟随它）；基地=cm_Base.Follow 从魔王临时切到 controlTargetForEmpty（位置先同步魔王位，镜头不跳变）。
- `MoveStoryCameraTarget(pos, duration, easeIndex)`：DOMove + SetUpdate(true)，先 DOKill 防并发叠加。
- `EndStoryCameraControl(originPos, duration)`：补间回起始位后基地恢复 cm_Base.Follow 原绑定。
- 标记解析在 StoryHandler.GetStoryMarkerPosition：基地建筑读 ScenePrefabForBase.objBuilding*（portal/gashapon 无建筑物体，取 CV_Portal/CV_GashaponMachine 机位节点近似）；战斗 core=fightDefenseCoreCreature.creatureObj。**新标记=改这一个 switch，不动表结构。**

## 编辑器（StoryEditorWindow）

[StoryEditorWindow.cs](Assets/Editor/StoryEditorWindow.cs)，菜单 `游戏/故事演出编辑`。骨架照抄 FightTypeConquerEditorWindow（Excel 直读→编辑→单会话写回→重导JSON），增删行照 EquipSuitEditorWindow（降序 DeleteRow/追加/新id=max+1）。

- 四栏：左故事列表（搜索/新增/删除，删除级联删步骤并提示孤儿对话）｜故事字段（名字中文直接编辑写回语言表 content_cn）｜步骤编排（foldout 列表/类型 EnumPopup/并发开关/**➕行前插入**/↑↓移/末尾添加，按 step_type 动态参数标签；Talk 步骤只做引用选择与只读预览）｜**对话列表**（本故事+通用对话的统一 CRUD 面板：npc 下拉含 0=旁白/内容中文/备注/删除——删除时若被步骤引用会提示并自动移除引用；+新增对话自动绑定当前故事）。步骤编排与对话管理分离，不混在一起。新步骤 id 规则=本故事最大 id+1（story_id*1000 号段内聚，中间插入后 id 与 step_order 不再一一对应属正常，执行只读 step_order）。
- 对话选择：Talk 步骤 param_1 下拉追加（**按 story_id 过滤只显示当前故事的对话** + story_id=0 的通用对话；引用其它故事对话可手输 ID）。
- 保存：Validate（场景-条件一致性/步骤参数合法性/对话存在性，错误阻断警告可过）→ 4 个 xlsx 各自单会话写回 → `ExcelUtil.ExcelToJsonItem` ×4 → Refresh → 提交快照。
- 注意：编辑器只维护中文（不写 content_en 等其它语种列，Excel 已有的其它语种保持不变）；英文及其他语种需在语言表人工补录后重新导出 JSON。

## 测试（StoryTest 四件套）

测试模式 `TestSceneTypeEnum.StoryTest=15`：`GameTestEditor.DrawStoryTest`（下拉 `[id] 名字 [类型/场景/条件]` + 手动ID非0优先 + 存档槽位(0=当前测试数据,1~3=读取对应存档作运行时数据,测试模拟不写回) + 🔄刷新清Cfg缓存 + 📂配置表 + ▶️播放）→ `LauncherTest.StartForStoryTest(storyId, saveSlot=0)`：saveSlot>0 先读档（献祭测试同范式）→ `isTestSimulation=true` → 按 scene_type 进场景（Base=EnterGameForBaseScene+一次性事件回调；Fight=内置默认测试战斗数据 BuildStoryTestFightData 进战斗+等 `GameFightLogic_StartGame`；DoomCouncil=StartDoomCouncil 默认议案+轮询场景就绪+1s 缓冲）→ `StoryHandler.PlayStory(storyId)`。

## 触发与事件

- `EventsInfo.GameFightLogic_StartGame`（战斗中 region）：`GameFightLogic.PreGame()` 末尾 `StartGame()` 后触发，基类单点覆盖全部战斗模式。
- 存档：`UserStoryBean.dicPlayedStory`（`Dictionary<long,long>`，key=StoryInfo.id、value=播放完成时间戳 Ticks；字典而非列表，事件多了查询仍 O(1)），拆分为独立存档 `UserStory_{slot}`（仿 UserUnlockBean，UserDataService 注入/落盘/删除同管线），经 `UserDataBean.GetUserStoryData()` 访问。
- 新档/读档不区分：已播记录为空自然首播；老存档上线后首次进基地会补播一次引导（可接受）。

## 扩展指引（剧情演出预留）

- `trigger_type=2 Plot` / `is_once=0` 可重复演出已入枚举与表结构。
- 新触发条件：StoryTriggerConditionEnum 加值 → StoryHandler.InitData 注册事件 → 表配条件（condition=4 议会预留，挂钩点=EnterDoomCouncilScene 链尾加事件）。
- 新步骤类型：StoryStepTypeEnum 加值 + ExecuteStep 加分支（param_4 空列可先用，不够再加列）；对话树/选项可走 Talk 新类型 + StoryTalkInfo 加 next_id 列演进。
- 镜头聚焦任意物体再回来：仿 FocusJuicerCameraOnHole/RestoreJuicerCameraFocus 做通用版（第一期未做）。
- 真·全锁输入：`UIHandler.ShowScreenLock()`（会挡对话点击，需白名单，第一期未用）。
- 演出排队：manager 加 `Queue<long> pendingStoryIds`（第一期播放中来的触发直接丢弃）。
