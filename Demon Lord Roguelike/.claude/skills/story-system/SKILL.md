---
name: story-system
description: Demon Lord Roguelike 游戏的故事演出(Story/新手引导/剧情演出)系统开发指南。使用此SKILL当需要新增/修改故事演出配置(StoryInfo/StoryDetailsInfo/StoryTalkInfo 三表)、演出步骤(对话/镜头移动/等待/特效/音效/淡入淡出)、触发条件(首次进基地/首次进战斗(等下方卡片出现动画播完,UIFightMain_CardCreateAnimEnd)/首次掉魔晶)、演出运行时(StoryHandler/StoryManager)、故事演出编辑器(StoryEditorWindow)、故事演出测试(StoryTest)等，包括 StoryEnum、三个 BeanPartial、UIGameConversation.SetDataForStory 旁白模式、Story 专用虚拟相机(自管 CinemachineCamera,复制参数+停靠还原)、UserStoryBean 已播记录(独立存档 UserStory_{slot})等。
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
`id | name[language] | trigger_type(1引导 2剧情预留) | scene_type(1基地 2战斗 3议会) | trigger_condition(1首次进基地后 2首次进战斗(下方卡片出现动画播完)后 3战斗首次掉魔晶) | priority(小先播) | is_once | valid | remark`

### StoryDetailsInfo 列与 param 语义表（三方同步：Excel 表头 / BeanPartial 注释 / 本表）
`id(约定=story_id*1000+step_order) | story_id | step_order | step_type | is_async(0阻塞 1并发) | param_1~4 | remark`

| step_type | param_1 | param_2 | param_3 | param_4 |
|---|---|---|---|---|
| Talk(1) 对话 | talk_id（`&`分隔=同一步内顺序连播多句，每句各等一次点击） | 对话框对齐（bottom/bottom_left/bottom_right/middle/middle_left/middle_right/top/top_left/top_right，空=bottom下对齐）；可接 `\|`高亮目标（demon魔王核心/crystal掉落魔晶/ui_fight_card手卡/ui_fight_remove删除按钮/ui_fight_att_progress进攻进度，空=不高亮）`\|`形状（rect方形默认/circle圆形）`\|`尺寸倍率（默认1，以目标自身大小为基准） | 对话框偏移X（默认0） | 对话框偏移Y（默认0） |
| CameraMove(2) 镜头 | 目标标记（基地: self/core/portal/gashapon/juicer/altar/vat/achievement/council；战斗: core；通用: back=回演出起始位） | 时长秒(默认1) | 缓动DOTween序号(默认0=默认缓动) | — |
| Wait(3) 等待 | 秒（实时，不受 timeScale 影响） | — | — | — |
| Effect(4) 特效 | effect_id(EffectInfo.id) | 目标标记(空=战斗防守核心/基地魔王位) | 尺寸倍率(默认1) | — |
| Audio(5) 音效 | audio_id(AudioInfo.id) | — | — | — |
| Fade(6) 淡入淡出 | out=淡出变黑 / in=淡入 | 时长秒(默认0.5) | — | — |

### StoryTalkInfo 列
`id(约定=story_id*1000+号段内序号,一个故事最多999句;存量旧全局自增id已于2026-08迁移) | story_id(所属故事 StoryInfo.id,0=通用;编辑器按此过滤对话下拉,新增对话自动绑定当前故事) | npc_id(NpcInfo.id, 0=旁白无立绘无名字无贿赂) | content[language] | remark`

## 运行时

### StoryHandler / StoryManager（Handler-Manager 配对）
- [StoryHandler.cs](Assets/Scripts/Component/Handler/StoryHandler.cs)：`BaseHandler<StoryHandler, StoryManager>`；`InitData()` **由 LauncherGame.Launch 与 LauncherTest.StartForNormalGame（「正常启动游戏」入口，漏调会导致进档后引导演出永不触发）调用**；StoryTest 测试场景不注册，自动触发天然关闭，测试走 `PlayStory` 强制播放。
- 监听三事件：`World_EnterGameForBaseScene`→条件1、`UIFightMain_CardCreateAnimEnd`(下方卡片出现动画播完,`UIFightMain.ShowCardCreateAnim` 末卡落位广播;空卡列表立即广播)→条件2、`GameFightLogic_CreatureDeadDropCrystal`→条件3。
- `TryTriggerStory(condition)`：演出中/无存档丢弃；候选列表经 `StoryManager.dicConditionStories` 缓存（配置静态不变，不重复筛选排序）找第一个未播（`UserStoryBean.IsStoryPlayed`）且场景匹配的故事播放；一次事件最多播一个。**高频事件短路**（掉晶等）：候选全部为只播一次且已播完时把条件记入 `setExhaustedCondition`，后续事件一次 HashSet 查询秒退；`exhaustedForStoryData` 记录标记对应的存档实例，切换存档槽（实例变更）自动重建防误伤新档。
- [StoryManager.cs](Assets/Scripts/Component/Manager/StoryManager.cs)：纯状态（isInited/isStoryPlaying/currentStoryData/timeScaleOrigin/storyCameraOriginPos/storyCamera/storyCameraAnchor/storyParkedCamera/storyBlendTimeOrigin/cancelForStory）。

### PlayStory 主流程（PlayStoryAsync）
1. 锁输入：基地 `SetBaseControl(false, isHideControlTarget:false)`（魔王保持可见，与议会交谈同款）；战斗 `EnableAllControl(false)`。（镜头走专用虚拟相机+独立锚点，**不再依赖 controlTargetForEmpty**，锁输入隐藏它不影响演出。）
2. 战斗场景：`Time.timeScale=0`（缓存原值，结束还原；先例 UIGameSystem）。
3. `BeginStoryCamera()` 接管镜头并记录 `storyCameraOriginPos`（详见下方「镜头」）。
4. 逐步执行：`is_async=1` → `_ = ExecuteStep`（并发，发起即下一步）；`=0` → `await ExecuteStep`（阻塞）。**"弹对话同时移动镜头"= 一个并发 CameraMove + 一个阻塞 Talk。**
5. `try/finally` 兜底 `FinishStory`：镜头归还 → 恢复 timeScale/控制（SetFightControl/SetBaseControl）→ is_once 记录 `UserStoryBean.MarkStoryPlayed` + SaveUserData（isTestSimulation 自动拦截）。

### unscaled 纪律（战斗演出 timeScale=0 下一切照常的关键）
演出系统内一切等待/补间必须 unscaled：`GTask.WaitReal`（不用 Wait）、DOMove/DOColor 一律 `.SetUpdate(true)`、打字机已改 `GTask.WaitReal`（UIGameConversation.TextAnimForContent）、WaitUntil 逐帧轮询不受 timeScale 影响。

### 对话复用（UIGameConversation.SetDataForStory）
`SetDataForStory(creatureObj, talkData, onEnd)`：npc_id≠0 → 复用现有 `SetData` 管线（spine/静态头像/名字/打字机）后强制隐藏 ui_Gift；npc_id=0（旁白）→ 隐藏 ui_Icon/ui_IconImg/ui_Gift、名字置空、直接起打字机。打开用 `UIHandler.OpenUI<UIGameConversation>()`（**不用 OpenUIAndCloseOther**，保留 UIFightMain 等场景 UI），打开后 `PlayTalkOnce` 内 `transform.SetAsLastSibling()` **置顶**（复用旧实例时 sibling 位置停留创建时，可能在战斗主 UI 之下；置顶保证演出对话永远显示在其他 UI 之上）。现有调用方（DoomCouncilLogic/LauncherTest）不受影响。

对话框布局（Talk 步骤 param_2 对齐段 + param_3/4）：`StoryHandler.PlayTalkOnce` 在 `SetDataForStory` 前调 `SetStoryContentLayout(anchor, offset)`（anchor/offset 由 `StoryDetailsInfoBeanPartial.GetTalkContentAnchor()/GetTalkContentOffset()` 解析，空=默认下对齐(0,0)）。**还原保证**：`UIGameConversation.OpenUI` 每次打开先 `ResetContentLayout()`（首次打开捕获预制体默认锚点/pivot/坐标快照）——演出改过布局后，议会对话等其它打开方式不会残留。合法对齐值表 = `StoryDetailsInfoBean.TalkContentAligns`（编辑器下拉与保存校验共用）。

目标高亮（Talk 步骤 param_2 高亮/形状/倍率段=`对齐|高亮目标|形状|倍率` 组合，`GetTalkHighlightMarker()/GetTalkHighlightShape()/GetTalkHighlightScale()` 解析）：`PlayTalkOnce` 再调 `ApplyTalkHighlight`——UI 类目标（`ui_fight_` 前缀）经 `GetOpenedFightMain`（只扫已开 UI 列表，**不用 GetUI**，它找不到会自动创建）取 UIFightMain 控件 RectTransform（ui_fight_card=第一张手卡兜底模板/ui_fight_remove/ui_fight_att_progress）；场景类目标取世界包围盒（demon=魔王核心合并子 Renderer bounds，crystal=`FightDropCrystalInstanceRenderer.TryGetFirstCrystalPosition` 优先第一颗 Landed 魔晶+固定 0.6 尺寸），再调 `UIGameConversation.SetStoryHighlight(RectTransform/Bounds, shapeType, sizeScale)`：世界角点→观察相机（场景=mainCamera、UI=uiCamera）屏幕→`ScreenPointToLocalPointInRectangle` 转 Mask UV，**范围默认=目标自身大小**（UI 矩形/场景包围盒投影），**倍率在其基准上缩放**（下限 0.01 防退化），**圆形按屏幕像素取最大边为直径换算正圆**（shader 的圆本是按 _Size 长短轴的椭圆），克隆 ui_MaskTarget 材质写 `_Center/_Size/_ShapeType`（Shader_UI_GuideHighlight，压暗全屏透亮目标），遮罩兜底拉伸满屏、尺寸下限 0.03 防整屏压暗。**还原保证**：`OpenUI` 每次打开先 `HideStoryHighlight()`。**高亮生命周期与防闪烁（亮→亮切换）**：对话步骤内连播/相邻对话步骤间复用同一 `UIGameConversation` 实例（`manager.storyConversationUI`，PlayTalkOnce 判定 `activeInHierarchy` 时**不再重走 OpenUI**——否则 OpenUI 的 HideStoryHighlight 会造成"压暗消失一瞬再淡入"的切换闪一帧），由非对话步骤（循环里进入前）/故事收尾（FinishStory 的 `CloseStoryConversationUI`）统一关闭；`ApplyStoryHighlight` 状态机化：mask 上次已显示（亮→亮）时**透明度恒定（压暗不闪）+ `_Center/_Size` 从旧值 0.18s 插值过渡到新目标**（洞移动/缩放的可见出现动画引导视线，快速连点下旧动画 Kill 重启），仅首现/无亮→有亮时从 0 快速淡入（DOFade 0.12s `.SetUpdate(true)`）。合法值表 = `StoryDetailsInfoBean.TalkHighlightMarkers/TalkHighlightShapes`（编辑器下拉与保存校验共用；目标当前不存在时警告并兜底不高亮）。**crystal 高亮联动魔晶置顶**：目标为 crystal（掉落魔晶）时 ApplyTalkHighlight 同时调 `FightDropCrystalInstanceRenderer.SetAlwaysOnTop(true)`——魔晶 ZTest Always+queue 4000 无视深度永远绘制在最前（随机落点撒到尸体背后也能透过遮挡看到，防引导暂停画面看不到魔晶；shader `Shader_Mesh_DropCrystalInstanced_1.shader` 暴露 `_ZTest`/`_ZWrite` 材质属性默认 LEqual/On 与原行为一致）；非 crystal/无高亮步骤自动还原，`CloseStoryConversationUI` 兜底还原（细节见 game-fight-core）。

### 镜头（Story 专用虚拟相机，StoryHandler「故事专用镜头」region）

演出**不接管场景相机**：StoryHandler 自管一台专用 `CinemachineCamera`（`EnsureStoryCamera()` 纯代码懒创建挂 StoryHandler 常驻 GameObject 下，含 `CinemachineFollow`+`CinemachineRotationComposer`，初始隐藏 Priority=0），移动目标是自己持有的 `storyCameraAnchor` 空物体（不再复用 controlTargetForEmpty）。主相机持续渲染，故 UI/场景高亮投影、AudioListener、后处理全不受影响。

- `BeginStoryCamera()` → Transform（锚点）：取 `CinemachineBrain.ActiveVirtualCamera` 为源相机（通用，不分战斗/基地/议会）→ **复制参数**（`Lens`；源有 `CinemachineFollow` 则复制 `FollowOffset/TrackerSettings`，有 `CinemachineRotationComposer` 则复制 `TargetOffset/Damping`——新增构图参数需同步复制）→ 锚点同步到源相机跟随目标位（镜头不跳变）→ 缓存 `storyParkedCamera=源相机` 与 `storyBlendTimeOrigin` → `SetMainCameraDefaultBlend(0)` → 源相机 `SetActive(false)`（**只改激活态，Follow/LookAt 全程不动**）→ 故事相机 `SetActive(true)`+`Priority=int.MaxValue` 瞬切。
- `MoveStoryCamera(pos, duration, easeIndex)`：DOMove 锚点 + `.SetUpdate(true)`，先 DOKill 防并发叠加；取消源传 null（结束回位必须不可取消，否则取消/异常时还原链会断）。
- `EndStoryCamera()`：锚点补间回 `storyCameraOriginPos`（0.5s）→ 故事相机 `SetActive(false)`+`Priority=0` → 恢复 `storyParkedCamera` 激活态与默认混合时长（姿态与起始一致，blend 0 瞬切无跳变）。
- 标记解析在 StoryHandler.GetStoryMarkerPosition：基地建筑读 ScenePrefabForBase.objBuilding*（portal/gashapon 有专属字段 objBuildingPortal/objBuildingGashaponMachine，取实体建筑锚点，预制体手动接线；**勿用 CV 机位节点当锚点**——机位常驻未激活时 Cinemachine 不驱动其 transform，读到的是出厂陈旧坐标）；战斗 core=fightDefenseCoreCreature.creatureObj。**新标记=改这一个 switch，不动表结构。**

## 编辑器（StoryEditorWindow）

[StoryEditorWindow.cs](Assets/Editor/StoryEditorWindow.cs)，菜单 `游戏/故事演出编辑`。骨架照抄 FightTypeConquerEditorWindow（Excel 直读→编辑→单会话写回→重导JSON），增删行照 EquipSuitEditorWindow（降序 DeleteRow/追加/新id=max+1）。

- 四栏：左故事列表（搜索/新增/删除，删除级联删步骤并提示孤儿对话）｜故事字段（名字中文直接编辑写回语言表 content_cn）｜步骤编排（foldout 列表/类型 EnumPopup/并发开关/**➕行前插入**/↑↓移/末尾添加，按 step_type 动态参数标签；Talk 步骤只做引用选择与只读预览 + 对话框对齐下拉/偏移X-Y/目标高亮开关+目标下拉/形状下拉(方形/圆形)+尺寸倍率（param_2=对齐[|高亮[|形状[|倍率]]]组合、param_3/4=偏移，空=默认下对齐(0,0)不高亮））｜**对话列表**（本故事+通用对话的统一 CRUD 面板：npc 下拉含 0=旁白/内容中文/备注/删除——删除时若被步骤引用会提示并自动移除引用；+新增对话自动绑定当前故事）。步骤编排与对话管理分离，不混在一起。新步骤 id 规则=本故事最大 id+1（story_id*1000 号段内聚，中间插入后 id 与 step_order 不再一一对应属正常，执行只读 step_order）；新对话 id 同约定=story_id*1000+号段内序号（GetNextTalkId：号段内最大+1，无对话取 story_id*1000+1，超上限 story_id*1000+999 报错阻断）。
- 栏宽：三个固定栏（故事列表/故事字段/对话列表）栏间分隔条可拖拽调宽、双击复位默认宽；步骤栏为弹性栏自动占满剩余宽度（DrawSplitter/HandleSplitterDrag/ClampSplitterWidth，各栏有最小宽保护）。
- 对话选择：Talk 步骤 param_1 下拉追加（**按 story_id 过滤只显示当前故事的对话** + story_id=0 的通用对话；引用其它故事对话可手输 ID）。
- 保存：Validate（场景-条件一致性/步骤参数合法性/对话存在性，错误阻断警告可过）→ 4 个 xlsx 各自单会话写回 → `ExcelUtil.ExcelToJsonItem` ×4 → Refresh → 提交快照。
- 注意：编辑器只维护中文（不写 content_en 等其它语种列，Excel 已有的其它语种保持不变）；英文及其他语种需在语言表人工补录后重新导出 JSON。

## 测试（StoryTest 四件套）

测试模式 `TestSceneTypeEnum.StoryTest=15`：`GameTestEditor.DrawStoryTest`（下拉 `[id] 名字 [类型/场景/条件]` + 手动ID非0优先 + 存档槽位(0=当前测试数据,1~3=读取对应存档作运行时数据,测试模拟不写回) + 🔄刷新清Cfg缓存 + 📂配置表 + ▶️播放）→ `LauncherTest.StartForStoryTest(storyId, saveSlot=0)`：saveSlot>0 先读档（献祭测试同范式）→ `isTestSimulation=true` → 按 scene_type 进场景（Base=EnterGameForBaseScene+一次性事件回调；Fight=内置默认测试战斗数据 BuildStoryTestFightData 进战斗+等 `UIFightMain_CardCreateAnimEnd`(与真实触发同钩点,卡片落位后高亮手卡等目标才在最终位置)；DoomCouncil=StartDoomCouncil 默认议案+轮询场景就绪+1s 缓冲）→ `StoryHandler.PlayStory(storyId)`。

同面板还有「清除存档故事演出数据」区块：选目标槽位(1~3) → 🔍查询状态(`UserDataService.LoadStoryData` 读已播数量) → 🗑️清除(二次确认 → `UserDataService.DeleteStoryData` 删 UserStory_{slot} 拆分档;运行中且当前加载的正是该槽位时同步清空内存 dicPlayedStory + StoryManager.setExhaustedCondition/exhaustedForStoryData 缓存)——让故事触发条件重新生效，便于进游戏反复测试同一存档的真实演出。

## 触发与事件

- `EventsInfo.UIFightMain_CardCreateAnimEnd`（战斗中 region）：`UIFightMain.ShowCardCreateAnim()` 整组卡片出现动画播完（最后一张卡 `AnimForCreateShow` 落位回调）广播；条件2（首次进战斗）挂此事件而非 `GameFightLogic_StartGame`——等下方卡片弹入落位后再触发，保证 `ui_fight_card` 高亮目标已在最终位置、演出暂停（timeScale=0）不会冻结在途卡片动画。`GameFightLogic_StartGame` 仍在 `GameFightLogic.PreGame()` 末尾 `StartGame()` 后触发（基类单点覆盖全部模式），保留作通用挂钩。
- 存档：`UserStoryBean.dicPlayedStory`（`Dictionary<long,long>`，key=StoryInfo.id、value=播放完成时间戳 Ticks；字典而非列表，事件多了查询仍 O(1)），拆分为独立存档 `UserStory_{slot}`（仿 UserUnlockBean，UserDataService 注入/落盘/删除同管线），经 `UserDataBean.GetUserStoryData()` 访问；单档轻量读写走 `UserDataService.LoadStoryData()`/`DeleteStoryData()`（不读主档，测试工具用）。
- 新档/读档不区分：已播记录为空自然首播；老存档上线后首次进基地会补播一次引导（可接受）。

## 扩展指引（剧情演出预留）

- `trigger_type=2 Plot` / `is_once=0` 可重复演出已入枚举与表结构。
- 新触发条件：StoryTriggerConditionEnum 加值 → StoryHandler.InitData 注册事件 → 表配条件（condition=4 议会预留，挂钩点=EnterDoomCouncilScene 链尾加事件）。
- 新步骤类型：StoryStepTypeEnum 加值 + ExecuteStep 加分支（Talk 已用满 param_1~4——param_2 为「对齐|高亮」组合段；再加参数需新增 param_5 列并重生成 Bean）；对话树/选项可走 Talk 新类型 + StoryTalkInfo 加 next_id 列演进。
- 镜头聚焦任意物体再回来：仿 FocusJuicerCameraOnHole/RestoreJuicerCameraFocus 做通用版（第一期未做）。
- 真·全锁输入：`UIHandler.ShowScreenLock()`（会挡对话点击，需白名单，第一期未用）。
- 演出排队：manager 加 `Queue<long> pendingStoryIds`（第一期播放中来的触发直接丢弃）。
