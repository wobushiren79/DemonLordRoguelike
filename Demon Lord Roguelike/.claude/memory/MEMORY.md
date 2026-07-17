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
- [`reference_research_white_icons.md`](reference_research_white_icons.md) — 研究图标 ui_research_* 纯白16x16剪影风格 + PixelLab(create_1_direction_object size64批量)→实心剪影挖近黑(dark_cut=35,非旧的亮度阈值≥115)后处理流水线；暂存已到 ui_research_577，下一个从 578 起
- [`reference_colored_icons.md`](reference_colored_icons.md) — 彩色图标(深渊馈赠ui_abyssalblessing_/成就ui_achievement_)32x32 每张≤N色(用户可调,2026-07为≤6)：create_1_direction_object size32(64个/批)→quantize6(合成黑底再中位切分)控色流水线；深渊暂存已到2621(下一个2622)、成就已到63(下一个64)

### Reference
- [reference_unityskills_shadergraph_limits.md](reference_unityskills_shadergraph_limits.md) — Unity-Skills shadergraph 工具限制：节点白名单(无噪声/Time/NormalFromHeight)、Vector2 赋值 bug、值格式约定
- [reference_language_excel_source.md](reference_language_excel_source.md) — 多语言 Language_*.txt 的真实源是 excel_language 同名工作表（非各自配置表），改文本必须改该 Excel 否则导出被覆盖；GetTextReplace 占位符模板
- [reference_spine_outline_vs_rim.md](reference_spine_outline_vs_rim.md) — 平面 Spine 精灵(固定法线)Rim 边缘光恒为 0 不可见，高亮边框要用 OutlineOnly 真描边 + CustomMaterialOverride；场上魔物悬停高亮的共享单例预览预制方案
- [reference_unity_mcp_tool_bug.md](reference_unity_mcp_tool_bug.md) — Unity MCP 的 -32602 全滅 bug 早已修复、工具正常可优先直接用；2026-07-03 Unity 包升到 10.0.0(server 仍报 3.4.2)工具集大扩：新增脚本编辑(manage_script/create_script/apply_text_edits)、AI 生成(generate_image/generate_model,付费须先征同意)、manage_components/camera/animation/ui/physics/probuilder/profiler/packages/build 等；仍无 Shader Graph 节点编辑(manage_shader 仅.shader文本CRUD，.shadergraph 走 unity-skills 8090)；Mcp-Session-Id 须原样回传否则 -32600
- [reference_unity_editor_self_run_delete_trick.md](reference_unity_editor_self_run_delete_trick.md) — 无人值守建/改 Unity 资源的小技巧：临时编辑器脚本 [DidReloadScripts] 搭编译便车自动执行 + 幂等守卫 + AssetDatabase.DeleteAsset 自删；MCP 不可用时优先用
- [reference_ui_gradient_atlas_uv.md](reference_ui_gradient_atlas_uv.md) — UI 渐变不能用 sprite 贴图 UV(图集 V1 mode=4 压缩+9宫格切段致两色混成单色)；FrameWork/UI/Shader_UI_ImageGradient 读 UV1 + UIGradientMeshUV(BaseMeshEffect 写归一化 UV1 并开 Canvas TexCoord1)
- [reference_shader_common_layering.md](reference_shader_common_layering.md) — Shader 公共 hlsl 分层：Common=跨效果通用件(ParticleCommon.hlsl 粒子淡出/贴图/公共CBUFFER宏 · Outline.hlsl Alpha扩张描边 · CommonLit.hlsl BlinnPhong受光 · Transform.hlsl 顶点位移+欧拉角旋转 · Noise.hlsl 哈希/值噪声/Fbm2D程序化湍流) / Effect/<效果>/=效果专属业务件(WindSway/GrassWind+TreeWind 风摆算法)；判定"跨效果可复用才进Common,被复用≠通用"；风摆shader的include链与SRP Batcher宏约束；含8个WindSway shader全覆盖的 _OUTLINE_ON 描边接线三件套 + 统一 WindSwayShaderGUI 分组折叠材质面板；三件套 *_Common_1(粒子/精灵/网格)描边+Lit开关+_VertexOffset/_VertexRotation 顶点变换(网格三pass同步)；通用 SurfaceOptions(表面类型不透明/透明+渲染模式+Alpha裁剪+渲染面)接入者=ProgressBar/MeshCommon1/**6个WindSway(默认不透明+开alpha clip,粒子去手动混合改预设,61引用材质批量开_ALPHATEST_ON防镂空变方块)**；弹道拖尾 TrailInstanced1(逐实例_TrailAlpha年龄档)与**火球 MeshFireBallInstanced1(vertex shader内跑GPU粒子:火星汤网格+预烤方向/种子,中心火焰核心+四散火星同一次draw,顶点色a区分两类quad故须运行时if分支非keyword,核心用Fbm2D噪声撕火舌;坑=重力须世界空间加·bounds须手工写否则视锥剔除·加法混合用MixFogColor拉向黑·HDR色LDR截断偏黄非bug·面板分组顺序由MeshCommonShaderGUI.sections数组定非Properties块,逐实例_SeedOffset防同屏齐跳·_SparkVelocity让火星脱离火球留世界空间(posWS-=速度×已存活秒数退回出生点;**零输入不可能**=shader只知火球此刻位置且无状态/DrawMeshInstanced无unity_MatrixPreviousM;落地=**材质面板直接填速度**,逐实例属性无MPB时回退读材质值,渲染器与BaseAttackMode零改动(用户明令不许加),代价=整桶一个速度),配套 FireBallMeshGeneratorWindow 火球网格生成器;FrameWork嵌套仓库内改名走Move-Item连.meta一起移保GUID零断链)**
- [reference_screen_distortion_heathaze.md](reference_screen_distortion_heathaze.md) — 屏幕空间扭曲(热浪/折射)套路：URP三档均开Opaque Texture→透明Quad+ComputeScreenPos+SampleSceneColor按滚动值噪声偏移UV；FrameWork/Effect/HeatHaze shader(程序化噪声无需贴图)+MatHeatHaze，挂沙漠FightScene_Desert_1 Effect节点下横铺Quad
- [reference_juicer_system.md](reference_juicer_system.md) — 魔汁机(魔物回收/Juicer)系统：术语统一(面向玩家魔汁机/代码Juicer)+UI驱动轻量Logic架构(E键JuicerInteraction=9直接开UICreatureJuicer→切CV_Juicer镜头→多选投入魔物listSelectCreature复用CreatureAscendTarget卡片态,默认等级降序/排除上阵→Start走GameHandler.StartCreatureJuicer(List)→CreatureJuicerLogic.StartJuice(List))；建筑ScenePrefabForBase.objBuildingJuicer门控UnlockEnum.Juicer=100600001；投入数量上限GetUnlockJuicerCreatureMax=juicerCreatureMax(5)+JuicerNum(100600002)研究等级(满级15),ui_LimmitText计数;镜头CameraHandler.SetJuicerCamera→CV_Juicer固定机位；⚠️榨汁流程/奖励留桩待接入StartJuice；配置ResearchInfo/UnlockInfo/Language(2009魔汁机/61010未投入/61011开始/61012超上限,icon ui_research_65占位)；详见juicer-system skill+game-juicer agent
- [reference_equip_reward_race_gated.md](reference_equip_reward_race_gated.md) — 传送门/征服装备奖励是"种族级"门控(CreatureModel.unlock_id=EquipRewardXxx研究开关,如骷髅300200301)，与解锁具体生物职业(骷髅魔法师300200003/4)平级独立解耦；骷髅武器全绑model2不分具体生物→"没解锁某生物却掉其装备"非bug是设计,2026-07确认维持现状
- [reference_portal_reward_pregen_research_gate.md](reference_portal_reward_pregen_research_gate.md) — 传送门世界奖励"创建时预生成并冻结"+预览即实领(GameWorldDifficultyRandomBean.listReward/rewardUnlockSign、GetDifficultyReward、GameFightLogicConquer 消费 InitDataForReward)+解锁新魔物掉落致签名变化重生成；RewardSelectBean 奖励生成单一真实源(InitData(conquerInfo)/CreateRewardListForConquer/InitDataForReward/GetConquerEquipPoolSign)；UIPopupPortalDetails 四项预览(线路/关卡/路径长度414/奖励)受设施研究门控 UnlockEnum.PortalPreview*=100300002~5(未解锁整行隐藏)
- [reference_world_quick_attack_research.md](reference_world_quick_attack_research.md) — 战斗进度条 Quick 按钮(+10%进度并即时刷该段进攻波次,无消耗无冷却,0.3s平滑过渡)；GameFightLogic.QuickAdvanceAttackCreate 复用逐帧刷怪步进语义；与世界绑定的 id 块约定 1003_10_W_nn(nn=30=Quick,避开12~20征服难度)+UserUnlockBean.CheckIsUnlockWorldQuickAttack,新增世界无需世界表加列；研究节点 research_type=4 世界分类 id=100310130
- [reference_boss_skill_attack_mode_ext.md](reference_boss_skill_attack_mode_ext.md) — 额外攻击(攻击模块扩展,旧称BOSS技能,通用)按间隔自动释放：NpcInfo.attack_mode_ext→AttackModeExtInfo(ext_type=1BossSkill/trigger_interval/attack_mode_id)→基类AIIntentCreatureAttack消费(融入普通攻击循环非并行:UpdateExtraAttackTimer仅计时,AttackCreatureStart判定GetReadyExtraAttack优先于普通攻击,AttackCreatureStartEnd发射复用StartCreateAttackMode customAttackModeId)；AreaBoxFront size=(前方,高,上下)即102001"3,1,0.25"=前方6格、101001"0.5,1,1"=前方1格+上下1格；含"BOSS=FightAttack进攻敌人,非防守核心AIDefenseCore"术语澄清；命中特效射程=EffectInfo Speed×LifeTime与攻击盒无关(size只缩放粒子大小非距离)→远距离攻击须单配EffectInfo(如400002 Speed:30×0.2=6给102001)
- [reference_grass_particle_lit_shadow.md](reference_grass_particle_lit_shadow.md) — 草粒子Lit化+阴影踩坑：Mesh模式必配Render Alignment=Local否则mesh朝相机+法线偏暗；"阴影脱离草"是斜光自然长影非bug(顶光测试判定,URP bias在管线层且降它无效)；密草地关真影用sub-emitter假阴影(复用slash_circle01_AB)；草与Spine生物斜角排序翻转(草虽Transparent队列但ZWrite On+cutoff实为镂空实体)→材质renderQueue改2450(AlphaTest)回不透明阶段逐像素深度
- [reference_instanced_lit_flat_gi.md](reference_instanced_lit_flat_gi.md) — Graphics.DrawMeshInstanced 的 Lit 材质比预制 MeshRenderer 偏暗=缺环境光(SampleSH 读不到环境探针)+缺附加光；CustomProvided+CopySHCoefficientArraysFrom 灌 per-instance SH **无效**(自定义 shader 未启用 UNITY_INSTANCED_SH)；确定性修法=shader 加 _InstancedFlatGI(默认0,Lit 分支加到反照率)+C# 把 ambientProbe 6轴求值平均经 MPB.SetVector 灌入(仅变化时重算,LightProbeUsage.Off)；属性走 MPB 不写材质(预制共用会双亮)；⚠️MaterialPropertyBlock 禁在 MonoBehaviour 构造期/字段初始化器 new(CreateImpl 异常连带组件创建失败)→运行时懒建
- [reference_unity_mcp_execute_code_assets.md](reference_unity_mcp_execute_code_assets.md) — 用 Unity MCP execute_code 建资源/特效踩坑：编译器是 CodeDom(C#6)不能 C#7+ 且包编辑器程序集(Addressables)不被引用须反射调；AddressableAssetSettingsDefaultObject 在 UnityEditor.AddressableAssets(非.Settings)；safety_checks 拦 DeleteAsset(关它会被拒)→用 load-or-create；⚠️无人值守(Unity非前台)时 execute_code 做 AssetDatabase 写会死锁主线程(建资产优先改用自跑编辑器脚本)；本项目特效走 Addressables 组 Effect、address=全资源路径,新特效必须注册否则 LoadAssetSync 返 null；EffectBase(mainPS+listPS,PlayEffect连带子PS)+EffectBean(isPlayInShow=false回调里改startColor再播=运行时按稀有度上色)；PS 调 MCP streamable-http 每调用重握手+session id 数组要强转字符串
- [reference_ui_click_sound_default_on.md](reference_ui_click_sound_default_on.md) — 通用UI点击音效由白名单改为"默认全响+排除式"：ActionForUIOnClick 中可交互带sprite的Button默认播sound_btn_1，靠 listExcludeUIClickByName(GameObject名)+listExcludeUIClickBySprite(sprite名)静音；共用枠ui_border_2/ui_bg_1须按GameObject名排除、页签专用ui_border_7/8可按sprite名排；各UI别再手动补sound_btn_1(会重复)；ViewExit/弹窗背景/ESC 仍走sound_btn_31

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
- [`feedback_shader_chinese_labels.md`](feedback_shader_chinese_labels.md) — shader Properties 参数显示名必须中文、Header 标题只能 ASCII；Inspector 尽量像 PaletteFX_Project 那样按功能分板块可折叠(CustomEditor/TabShaderGUI 或 Header 分组)
- [`feedback_prefer_language_property.md`](feedback_prefer_language_property.md) — 取多语言文本优先用框架自动生成的 _language 属性（带缓存），不手写 GetTextById(fileName,id,idx)
- [`feedback_audio_use_enum.md`](feedback_audio_use_enum.md) — 音频播放统一用 AudioEnum 枚举调用，禁止裸 id；音频 id 全面 long 化（AudioEnum 底层 long、框架层接口 long）；游戏层 partial 提供枚举重载转发；新增音频须同步维护枚举
- [`feedback_toasthint_state.md`](feedback_toasthint_state.md) — UIHandler.ToastHintText(content, state) 第二参数 state：0=失败(红)、1=成功(绿)，默认0；正向反馈必传1，别传错图标
