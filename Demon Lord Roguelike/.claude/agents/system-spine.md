---
name: system-spine
description: Spine动画系统开发：SpineHandler/SpineManager、动画播放控制、皮肤管理、Spine编辑器工具。
tools: Read, Write, Edit, Glob, Grep, Bash
watched_files:
  - Assets/FrameWork/Scripts/Component/Handler/SpineHandler.cs
  - Assets/FrameWork/Scripts/Component/Manager/SpineManager.cs
  - Assets/FrameWork/Scripts/Component/UI/SkeletonGraphicExtend.cs
  - Assets/FrameWork/Editor/Base/SkeletonGraphicExtendMenu.cs
  - Assets/FrameWork/Editor/Base/Window/SpineWindow.cs
  - Assets/FrameWork/Editor/Base/Window/SpineWindowPreview.cs
  - Assets/FrameWork/Addons/Spine/
---

# Spine 动画系统 (Spine System) 开发代理

你负责 Spine 动画系统的开发。

## 职责范围

### Spine 管理
- **SpineHandler** - Spine 逻辑处理单例 [FrameWork/Scripts/Component/Handler/SpineHandler.cs](Assets/FrameWork/Scripts/Component/Handler/SpineHandler.cs)
- **SpineManager** - Spine 资源与动画管理 [FrameWork/Scripts/Component/Manager/SpineManager.cs](Assets/FrameWork/Scripts/Component/Manager/SpineManager.cs)

### Spine 数据
- **SpineSkinBean** - 皮肤数据
- **SpineAnimationStateBean / SpineAnimationStateBeanPartial** - 动画状态数据

### Spine 编辑器
- **SpineWindow** - Spine 工具窗口（皮肤提取页签）[FrameWork/Editor/Base/Window/SpineWindow.cs](Assets/FrameWork/Editor/Base/Window/SpineWindow.cs)
- **SpineWindowPreview** - 动画预览页签（partial）：绕过官方版本兼容检查的动画预览 + 皮肤分组自由搭配 [FrameWork/Editor/Base/Window/SpineWindowPreview.cs](Assets/FrameWork/Editor/Base/Window/SpineWindowPreview.cs)
- **SkeletonGraphicExtendMenu** - 菜单 `Custom/Spine/替换 SkeletonGraphic 为 SkeletonGraphicExtend`：批量把 UI 预制体的 SkeletonGraphic 升级为 SkeletonGraphicExtend（SerializedObject 改写 m_Script 引用，组件 fileID/字段/外部引用零改动，幂等；含保存后自检）[FrameWork/Editor/Base/SkeletonGraphicExtendMenu.cs](Assets/FrameWork/Editor/Base/SkeletonGraphicExtendMenu.cs)

### UI Spine 组件
- **SkeletonGraphicExtend** - SkeletonGraphic 扩展（框架层 UI Spine 标准组件）：修复 RectMask2D 按 RectTransform rect 整体误剔除——rect 出 mask 时改用实时 mesh 包围盒判定（`cullByMeshBounds` 开关默认开），非居中骨架 pos 偏移出 mask 时内容仍正常显示 [FrameWork/Scripts/Component/UI/SkeletonGraphicExtend.cs](Assets/FrameWork/Scripts/Component/UI/SkeletonGraphicExtend.cs)
- 预制体与运行时创建（`SpineHandler.AddSkeletonGraphic`）一律用它，不再直接用原生 SkeletonGraphic

### 动画播放
```csharp
// 播放循环动画
creature.PlayAnim(SpineAnimationStateEnum.Idle, true);

// 播放单次动画
creature.PlayAnim(SpineAnimationStateEnum.Attack, false);
```

### Spine 运行时
- [Assets/FrameWork/Addons/Spine/](Assets/FrameWork/Addons/Spine/)
- spine-unity 4.3（git 包），多线程由 `SkeletonUpdateSystem` 单例驱动（工人线程做顶点 deform，主线程做 Mesh 上传）
- **线程化开关策略（2026-10-05 UI 闪烁事故定案）**：全局 `SpineRuntimeSettings` 两开关（`useThreadedMeshGeneration`/`useThreadedAnimation`）**必须保持关闭**——UI 骨架走线程化时，其网格生成与自身 `UpdateWorldTransform` 分属不同工作线程任务，高负载下会读到 `ResetConstrained()` 之后、约束重算之前的无约束中间态，双缓冲网格交替写入两版姿势，带 IK/Transform 约束的骨架（如 OtherSpine 幻化资源）在 UI 上表现为部件（发型）位置闪烁（无约束骨架竞态不可见）。spine-unity 三态语义=「`组件==Enable || 全局`」，只支持全局关时单独开、不支持全局开时单独关，故采取「全局关 + 世界骨架单独开」：框架层 `SpineHandler.AddSkeletonAnimation`（世界生物唯一创建入口）+ `FightCreatureEntity.SetData` 均显式 `ThreadedAnimation`/`ThreadedMeshGeneration=Enable`；UI 骨架（卡片/弹窗/详情）保持 `UseGlobalSetting` 走主线程串行。**禁止重开全局开关**（一开 UI 竞态即复发）
- **多线程性能配置**：`SpineHandler.Awake → ConfigureSkeletonUpdateSystem()` 全局应用一次——`UpdateChunksPerThread`/`LateUpdateChunksPerThread` 由官方默认 8 降为 2（const `SpineUpdateChunksPerThread`/`SpineLateUpdateChunksPerThread`，骨架总量不大时切太碎反增调度/信号开销）、`GroupRenderersBySkeletonType`/`GroupAnimationBySkeletonType=true`（同种骨架连续处理提缓存命中）、`MainThreadUpdateCallbacks=false`（const `SpineMainThreadUpdateCallbacks`，省"工人→主线程→再等工人"分段循环；前提是全项目无 Spine 动画事件订阅，目前仅 `CreatureSpineOutlineFollow.UpdateLocal` 纯骨骼数据读写可在线程侧执行——**新增 `UpdateLocal`/`AnimationState` 事件订阅时若触碰 Unity API 须先改回 true**）
- 战斗生物渲染器优化项（`FightCreatureEntity.SetData`）：`UpdateWhenInvisible=Nothing`、关法线/切线/tintBlack/裁剪、`immutableTriangles`、`singleSubmesh`、`ThreadedAnimation`/`ThreadedMeshGeneration=Enable`

## 约束

- Spine 动画通过 SpineHandler 统一管理
- 动画状态使用 SpineAnimationStateEnum 枚举
- 皮肤切换通过 SpineSkinBean 数据驱动；另有 `ChangeSkeletonSkin(Skeleton, string skinName)` 按名整皮替换重载（2026-09-24 新增，幻化药 ui_show_skin 等指定骨架内皮肤的场景用）与 `ChangeSkeletonSkin(Skeleton, params string[] skinNames)` 多皮肤叠加重载（2026-09-29 新增，幻化药 ui_show_skin「|」分隔组合皮肤场景用，如 CherryTaleSpine 的 Eye_01|Mouth_01；未命中部件回落骨架默认皮肤）
- Spine 资源加载后需正确释放
