# Project Memory

## Project: Demon Lord Roguelike (Unity C# Roguelike Tower Defense)

### Documentation
- [`MD/ProjectDocs.md`](MD/ProjectDocs.md) - Complete project reference: modules, APIs, code examples, technical details
- [`MD/ProjectFrame.md`](MD/ProjectFrame.md) - Architecture analysis: inheritance hierarchies, design patterns, data flow, startup/game loop
- Both files cross-reference each other

### Key Architecture
- Framework layer (FrameWork/) + Game logic layer (Scrpits/)
- Handler-Manager paired pattern: Handler=singleton logic, Manager=MonoBehaviour resources
- Global event system: EventHandler singleton + BaseEvent instance events
- AI: State machine pattern (AIBaseEntity + AIBaseIntent)
- MVC: GameConfig + UserData controllers

### Collaboration Feedback
- [`feedback_task_summary.md`](feedback_task_summary.md) — 任务总结必须列出参与的 Agent/Skill 名称及操作
- [`feedback_bean_partial.md`](feedback_bean_partial.md) — Bean 文件是自动生成的，扩展代码必须写在 BeanPartial 文件中
- [`feedback_code_style.md`](feedback_code_style.md) — 所有方法和属性必须加 XML 注释并用 #region 分类
- [`feedback_comment_sync.md`](feedback_comment_sync.md) — 修改代码逻辑时必须同步更新对应的 XML 注释
