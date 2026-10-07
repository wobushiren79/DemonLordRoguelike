---
name: project_spine_threading_policy
description: spine 线程化策略定案——全局两开关必须保持关闭（UI 骨架线程化竞态致部件闪烁），世界骨架经 AddSkeletonAnimation/FightCreatureEntity.SetData 单独 Enable
metadata:
  type: project
---

# Spine 线程化策略（2026-10-05 UI 闪烁事故定案）

**全局 `Assets/Resources/SpineRuntimeSettings.asset` 的 `useThreadedMeshGeneration` / `useThreadedAnimation` 必须保持关闭（=0），禁止重新打开。**

**Why（竞态机制）**：UI 骨架（SkeletonGraphic）走线程化时，其网格生成与自身 `UpdateWorldTransform` 分属 `SkeletonUpdateSystem` 的不同工作线程任务（`DONT_WAIT_FOR_ALL_LATEUPDATE_TASKS` 不等上一帧任务）。`UpdateWorldTransform` 先对每根骨骼 `ResetConstrained()`（渲染姿势回退到**无约束姿势**）再重算约束——网格生成若命中该窗口，双缓冲 mesh 一个写入无约束姿势、一个写入有约束姿势，显示在两版间乒乓。带 IK/Transform 约束的骨架（OtherSpine 幻化资源 Luoluo 等，4 IK+1 Transform）在 UI 上表现为**部件（发型）位置闪烁**；无约束骨架（主项目生物）竞态不可见；世界生物因动画持续播放，单帧无约束读也不可分辨。结算界面高发是因为战场上几十个大骨架使工作线程池繁忙、窗口期频繁命中；阵容界面骨架少几乎不命中。

spine-unity 4.3 三态语义为 `UsesThreaded* => 组件==Enable || 全局开关`：**只支持「全局关时单独开」，不支持「全局开时单独关」**（组件 Disable 在全局开时无效）。

**How to apply（现行结构）**：
- 全局关 → 所有 UI 骨架（卡片/弹窗/详情/对话）走主线程串行，无竞态。
- 世界骨架单独开：框架层 `SpineHandler.AddSkeletonAnimation`（[Assets/FrameWork/Scripts/Component/Handler/SpineHandler.cs](Assets/FrameWork/Scripts/Component/Handler/SpineHandler.cs)，世界生物唯一创建入口：战斗生物/防御核心/议员/测试预览）与 `FightCreatureEntity.SetData`（[Assets/Scripts/Game/Fight/FightCreatureEntity.cs:81-83](Assets/Scripts/Game/Fight/FightCreatureEntity.cs#L81-L83)）均显式 `ThreadedAnimation`/`ThreadedMeshGeneration = SettingsTriState.Enable`。
- 新增世界骨架创建路径时若需要线程化，必须走上述两个入口或自行显式 Enable；新增 UI 骨架**不得** Enable。
- 排障史：UIViewFightSettlementItem 幻化卡片闪烁（2026-10-04/05），按 [[feedback_diagnose_instrument_first]] 式插桩（骨骼哈希/AnimationState 实例指纹/双缓冲 mesh 实例-内容关联）逐层排除后由「关全局线程化」实验一锤定音。
- 注意 Play 模式下在 Inspector 改 `SpineRuntimeSettings` 退出 Play 会被还原，必须在编辑模式改。

相关文档：`.claude/agents/system-spine.md`「线程化开关策略」节、`.claude/skills/spine-system/SKILL.md` 注意事项第 7 条。
