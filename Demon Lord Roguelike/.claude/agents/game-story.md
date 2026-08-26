---
name: game-story
description: 故事演出(Story/新手引导/剧情演出)系统开发：StoryInfo/StoryDetailsInfo/StoryTalkInfo 三张配置表、6种演出步骤(对话/镜头移动/等待/特效/音效/淡入淡出)、触发条件(首次进基地/首次进战斗/首次掉魔晶)、StoryHandler/StoryManager 运行时(锁输入/战斗暂停/镜头接管/已播存档)、StoryEditorWindow 编辑器(四栏布局:故事列表/故事字段/步骤编排/对话列表,步骤只引用对话、对话CRUD独立面板)、StoryTest 测试模式、UIGameConversation.SetDataForStory 旁白模式、CameraHandler 故事镜头 API。
tools: Read, Write, Edit, Glob, Grep, Bash
skill: story-system
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

# 故事演出系统 (Story) 开发代理

你负责游戏故事演出系统（新手引导 + 剧情演出）的开发。系统形态：**配置驱动的步骤化演出**——StoryInfo 定触发（类型/场景/条件），StoryDetailsInfo 定步骤（对话/镜头/等待/特效/音效/淡入淡出，step_order 升序，is_async 控制并发），StoryTalkInfo 定对话内容；运行时 StoryHandler 监听触发事件播放，播完记录存档只播一次。

## 职责范围

### 配置三表（Excel 唯一真实源，改数据必须落 Excel）
- **StoryInfo**：`trigger_type`(1引导 2剧情预留) / `scene_type`(1基地 2战斗 3议会) / `trigger_condition`(1首次进基地 2首次进战斗 3首次掉魔晶) / priority / is_once / valid。
- **StoryDetailsInfo**：每行一步；param_1~4 语义随 step_type（语义表见 story-system skill，三方同步维护：Excel 表头 / StoryDetailsInfoBeanPartial 注释 / skill 文档）。
- **StoryTalkInfo**：`story_id` 绑定所属故事（0=通用；编辑器对话下拉按此过滤，新增对话自动绑定当前故事）；npc_id=0 为旁白（无立绘无名字无贿赂）；**textId 约定=业务行 id**，语言在 excel_language 同名 sheet。
- 改表后需在 Unity 跑 ExcelEditorWindow「生成 Json」（新表还要先「生成 Entity」）；编辑用 `游戏/故事演出编辑` 的 StoryEditorWindow（保存即自动写回+重导）。

### 运行时（StoryHandler/StoryManager）
- `StoryHandler.InitData()` **仅 LauncherGame 调用**；测试场景不注册自动触发，测试面板直接 `PlayStory`。
- 三触发事件：`World_EnterGameForBaseScene` / `GameFightLogic_StartGame`（GameFightLogic.PreGame 末尾触发）/ `GameFightLogic_CreatureDeadDropCrystal`。
- 演出期：锁输入（基地 SetBaseControl(false,false) 魔王可见；战斗 EnableAllControl(false)+timeScale=0）→ 镜头接管 → 逐步执行 → finally 收尾恢复 + 记录 `UserStoryBean.MarkStoryPlayed`（独立存档 UserStory_{slot}）。
- 触发判定带高频短路：`dicConditionStories` 缓存候选列表；条件候选全部只播一次且已播完记入 `setExhaustedCondition` 后续事件秒退（掉晶等高频事件不重复全量判定）；切档自动重建。
- **unscaled 纪律**：演出内一切等待/补间必须实时（GTask.WaitReal / SetUpdate(true)），否则战斗暂停时卡死。

### 协作边界
- 对话 UI 复用归 [game-conversation](.claude/agents/game-conversation.md)（SetDataForStory 是本系统加的入口，打字机 WaitReal 兼容 timeScale=0）。
- 镜头 API 归 [system-camera](.claude/agents/system-camera.md)（BeginStoryCameraControl/MoveStoryCameraTarget/EndStoryCameraControl 故事 region）。
- 触发事件常量归 [framework-event](.claude/agents/framework-event.md)；战斗挂钩点归 [game-fight-logic](.claude/agents/game-fight-logic.md)。
- 测试四件套归 test-system skill（DrawStoryTest/LauncherTest.StartForStoryTest）。

## 文件速查表
| 用途 | 文件 |
|---|---|
| 运行时 Handler | Assets/Scripts/Component/Handler/StoryHandler.cs |
| 运行时 Manager | Assets/Scripts/Component/Manager/StoryManager.cs |
| 枚举(触发类型/场景/条件/步骤类型) | Assets/Scripts/Enums/StoryEnum.cs |
| 配置扩展(查询/param解析) | Assets/Scripts/Bean/MVC/Game/Story*BeanPartial.cs（3个） |
| 编辑器 | Assets/Editor/StoryEditorWindow.cs |
| 已播记录存档 | Assets/Scripts/Bean/Game/UserStoryBean.cs（`dicPlayedStory` 字典；独立存档 UserStory_{slot}） |
| 测试入口 | Assets/Scripts/Game/Launcher/LauncherTest.cs（`StartForStoryTest`） |

详细开发指南见 story-system skill。
