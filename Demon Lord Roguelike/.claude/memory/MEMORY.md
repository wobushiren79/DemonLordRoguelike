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

### PixelLab
- [`feedback_pixellab_require_consent.md`](feedback_pixellab_require_consent.md) — 调用 PixelLab 生成前必须征得用户明确同意（付费服务），禁止其他任务中"顺带"自行生成图片
- [`feedback_pixellab_animation_output.md`](feedback_pixellab_animation_output.md) — 帧动画只保留合成精灵表，不保留单帧文件
- [`feedback_pixellab_auto_download.md`](feedback_pixellab_auto_download.md) — 生成完成后必须自动下载到 Assets/Out/<子目录>/，不能只给链接

### Reference
- [reference_unityskills_shadergraph_limits.md](reference_unityskills_shadergraph_limits.md) — Unity-Skills shadergraph 工具限制：节点白名单(无噪声/Time/NormalFromHeight)、Vector2 赋值 bug、值格式约定
- [reference_language_excel_source.md](reference_language_excel_source.md) — 多语言 Language_*.txt 的真实源是 excel_language 同名工作表（非各自配置表），改文本必须改该 Excel 否则导出被覆盖；GetTextReplace 占位符模板

### Collaboration Feedback
- [`feedback_task_summary.md`](feedback_task_summary.md) — 任务总结必须列出参与的 Agent/Skill 名称及操作
- [`feedback_bean_partial.md`](feedback_bean_partial.md) — 文件可改性只看文件头有无 AUTO-GENERATED-DO-NOT-EDIT 标记：有则写 Partial，无（含 Bean/Game 手写 Bean、MVC 脚手架 UserDataBean）可直接改
- [`feedback_code_style.md`](feedback_code_style.md) — 所有方法和属性必须加 XML 注释并用 #region 分类
- [`feedback_comment_sync.md`](feedback_comment_sync.md) — 修改代码逻辑时必须同步更新对应的 XML 注释
- [`feedback_excel_id_sorted_insert.md`](feedback_excel_id_sorted_insert.md) — 新增配置表数据行必须按 id 升序插入，禁止 append 追加末尾（用 excel_add_row.py）
- [`feedback_input_system.md`](feedback_input_system.md) — 输入处理必须走 InputActionUIEnum，禁止使用旧版 Input API（Input.GetKeyDown 等）
- [`feedback_agent_skill_sync.md`](feedback_agent_skill_sync.md) — 改了被 watched_files 命中的代码必须同步 agent/skill 文档；含自动 Hook 机制与 PS 脚本必须 UTF-8 BOM 的编码约束
- [`feedback_inline_python_no_temp.md`](feedback_inline_python_no_temp.md) — 一次性 Python 优先用 run-python.ps1 -c 内联，别建临时 .py；附绝对路径/点目录致 allow 失配原因
- [`feedback_ask_before_architecture_change.md`](feedback_ask_before_architecture_change.md) — 涉及改变原有架构/数据流向的修改（如配置来源 Excel⇄代码字段迁移）必须先询问用户确认
- [`feedback_shader_chinese_labels.md`](feedback_shader_chinese_labels.md) — 所有 shader 的 Properties 参数显示名必须用中文标注用途；Header 分组标题只能用 ASCII
- [`feedback_prefer_language_property.md`](feedback_prefer_language_property.md) — 取多语言文本优先用框架自动生成的 _language 属性（带缓存），不手写 GetTextById(fileName,id,idx)
