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
- [`reference_research_white_icons.md`](reference_research_white_icons.md) — 研究图标 ui_research_* 纯白16x16剪影风格 + PixelLab(create_1_direction_object size64批量)→亮度阈值化纯白后处理流水线；当前已到 ui_research_200，下一个从 201 起
- [`reference_colored_icons.md`](reference_colored_icons.md) — 彩色图标(深渊馈赠ui_abyssalblessing_/成就ui_achievement_)32x32 ≤8色：create_1_direction_object size32(64个/批)→quantize8中位切分量化控色流水线；深渊已到119(下一个120)、成就已到63(下一个64)

### Reference
- [reference_unityskills_shadergraph_limits.md](reference_unityskills_shadergraph_limits.md) — Unity-Skills shadergraph 工具限制：节点白名单(无噪声/Time/NormalFromHeight)、Vector2 赋值 bug、值格式约定
- [reference_language_excel_source.md](reference_language_excel_source.md) — 多语言 Language_*.txt 的真实源是 excel_language 同名工作表（非各自配置表），改文本必须改该 Excel 否则导出被覆盖；GetTextReplace 占位符模板
- [reference_spine_outline_vs_rim.md](reference_spine_outline_vs_rim.md) — 平面 Spine 精灵(固定法线)Rim 边缘光恒为 0 不可见，高亮边框要用 OutlineOnly 真描边 + CustomMaterialOverride；场上魔物悬停高亮的共享单例预览预制方案
- [reference_unity_mcp_tool_bug.md](reference_unity_mcp_tool_bug.md) — 本机 Unity MCP(3.4.2 HTTP)多数变更工具报 -32602、execute_code 无 Roslyn 走 CodeDom 命令行过长失败；建资源改用 [DidReloadScripts] 自动执行的临时编辑器脚本回退
- [reference_unity_editor_self_run_delete_trick.md](reference_unity_editor_self_run_delete_trick.md) — 无人值守建/改 Unity 资源的小技巧：临时编辑器脚本 [DidReloadScripts] 搭编译便车自动执行 + 幂等守卫 + AssetDatabase.DeleteAsset 自删；MCP 不可用时优先用
- [reference_ui_gradient_atlas_uv.md](reference_ui_gradient_atlas_uv.md) — UI 渐变不能用 sprite 贴图 UV(图集 V1 mode=4 压缩+9宫格切段致两色混成单色)；FrameWork/UI/Shader_UI_ImageGradient 读 UV1 + UIGradientMeshUV(BaseMeshEffect 写归一化 UV1 并开 Canvas TexCoord1)
- [reference_shader_common_layering.md](reference_shader_common_layering.md) — Shader 公共 hlsl 分层：Common=跨效果通用件(ParticleCommon.hlsl 粒子淡出/贴图/公共CBUFFER宏) / Effect/<效果>/=效果专属业务件(WindSway/GrassWind+TreeWind 风摆算法)；判定"跨效果可复用才进Common,被复用≠通用"；4个风摆粒子shader的include链与SRP Batcher宏约束
- [reference_portal_reward_pregen_research_gate.md](reference_portal_reward_pregen_research_gate.md) — 传送门世界奖励"创建时预生成并冻结"+预览即实领(GameWorldDifficultyRandomBean.listReward/rewardUnlockSign、GetDifficultyReward、GameFightLogicConquer 消费 InitDataForReward)+解锁新魔物掉落致签名变化重生成；RewardSelectBean 奖励生成单一真实源(InitData(conquerInfo)/CreateRewardListForConquer/InitDataForReward/GetConquerEquipPoolSign)；UIPopupPortalDetails 四项预览(线路/关卡/路径长度414/奖励)受设施研究门控 UnlockEnum.PortalPreview*=100300002~5(未解锁整行隐藏)
- [reference_boss_skill_attack_mode_ext.md](reference_boss_skill_attack_mode_ext.md) — 额外攻击(攻击模块扩展,旧称BOSS技能,通用)按间隔自动释放：NpcInfo.attack_mode_ext→AttackModeExtInfo(ext_type=1BossSkill/trigger_interval/attack_mode_id)→基类AIIntentCreatureAttack消费(融入普通攻击循环非并行:UpdateExtraAttackTimer仅计时,AttackCreatureStart判定GetReadyExtraAttack优先于普通攻击,AttackCreatureStartEnd发射复用StartCreateAttackMode customAttackModeId)；AreaBoxFront size=(前方,高,上下)即102001"1.5,1,0.25"=前方3格、101001"0.5,1,1"=前方1格+上下1格；含"BOSS=FightAttack进攻敌人,非防守核心AIDefenseCore"术语澄清
- [reference_grass_particle_lit_shadow.md](reference_grass_particle_lit_shadow.md) — 草粒子Lit化+阴影踩坑：Mesh模式必配Render Alignment=Local否则mesh朝相机+法线偏暗；"阴影脱离草"是斜光自然长影非bug(顶光测试判定,URP bias在管线层且降它无效)；密草地关真影用sub-emitter假阴影(复用slash_circle01_AB)；草与Spine生物斜角排序翻转(草虽Transparent队列但ZWrite On+cutoff实为镂空实体)→材质renderQueue改2450(AlphaTest)回不透明阶段逐像素深度

### Project Work
- [`project_loop_sound_design.md`](project_loop_sound_design.md) — 连续音效(LoopSound)已实现：框架层通用"循环播放能力"(非新audio_type/目录/枚举,复用任意clip按audio_type加载在循环AudioSource池上播)；音量跟随soundVolume、异步竞态token防护、暂停只恢复被暂停源、MaxLoopSource16池；走路声复用sound_walk_1挂ControlForGameBase(移动/静止/禁用三处),ClearWorldData兜底StopAllLoopSound；未做loopVolume条/3D多路/同id换clip

### Collaboration Feedback
- [`feedback_task_summary.md`](feedback_task_summary.md) — 任务总结必须列出参与的 Agent/Skill 名称及操作
- [`feedback_bean_partial.md`](feedback_bean_partial.md) — 文件可改性只看文件头有无 AUTO-GENERATED-DO-NOT-EDIT 标记：有则写 Partial，无（含 Bean/Game 手写 Bean、MVC 脚手架 UserDataBean）可直接改
- [`feedback_code_style.md`](feedback_code_style.md) — 方法/属性必须加 XML 注释并用 #region 分类；方法体内注释尽量单行（多行先总结，压不下再多行）
- [`feedback_comment_sync.md`](feedback_comment_sync.md) — 修改代码逻辑时必须同步更新对应的 XML 注释
- [`feedback_excel_id_sorted_insert.md`](feedback_excel_id_sorted_insert.md) — 新增配置表数据行必须按 id 升序插入，禁止 append 追加末尾（用 excel_add_row.py）
- [`feedback_input_system.md`](feedback_input_system.md) — 输入处理必须走 InputActionUIEnum，禁止使用旧版 Input API（Input.GetKeyDown 等）
- [`feedback_agent_skill_sync.md`](feedback_agent_skill_sync.md) — 改了被 watched_files 命中的代码必须同步 agent/skill 文档；含自动 Hook 机制与 PS 脚本必须 UTF-8 BOM 的编码约束
- [`feedback_inline_python_no_temp.md`](feedback_inline_python_no_temp.md) — 一次性 Python 优先用 run-python.ps1 -c 内联，别建临时 .py；附绝对路径/点目录致 allow 失配原因
- [`feedback_ask_before_architecture_change.md`](feedback_ask_before_architecture_change.md) — 涉及改变原有架构/数据流向的修改（如配置来源 Excel⇄代码字段迁移）必须先询问用户确认
- [`feedback_shader_chinese_labels.md`](feedback_shader_chinese_labels.md) — 所有 shader 的 Properties 参数显示名必须用中文标注用途；Header 分组标题只能用 ASCII
- [`feedback_prefer_language_property.md`](feedback_prefer_language_property.md) — 取多语言文本优先用框架自动生成的 _language 属性（带缓存），不手写 GetTextById(fileName,id,idx)
- [`feedback_audio_use_enum.md`](feedback_audio_use_enum.md) — 音频播放统一用 AudioEnum 枚举调用，禁止裸 id；音频 id 全面 long 化（AudioEnum 底层 long、框架层接口 long）；游戏层 partial 提供枚举重载转发；新增音频须同步维护枚举
- [`feedback_toasthint_state.md`](feedback_toasthint_state.md) — UIHandler.ToastHintText(content, state) 第二参数 state：0=失败(红)、1=成功(绿)，默认0；正向反馈必传1，别传错图标
