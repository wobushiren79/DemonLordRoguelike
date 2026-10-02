---
name: feedback-component-migration-mscript
description: Unity 组件类型迁移勿用 AddComponent+CopyComponent/PasteComponentValues+DestroyImmediate 组合（实测组件块整个丢失、引用清 0），改用 SerializedObject 改写 m_Script 引用——fileID/字段/外部引用全保留，预制体 diff 仅一行 guid
metadata:
  type: feedback
---

预制体组件批量替换类型（如 SkeletonGraphic → SkeletonGraphicExtend）时，**禁止**用 `AddComponent<新类型>()` + `ComponentUtility.CopyComponent/PasteComponentValues` + `DestroyImmediate(旧)` 的组合做迁移。

**Why:** 2026-09-27 SkeletonGraphicExtend 替换工具实测：该组合在 `PrefabUtility.LoadPrefabContents` 场景里执行后，保存出的预制体中**新组件块整个缺失**（旧组件已删、新组件未写入），导致 UI 组件的序列化引用全部变 `fileID: 0`（运行 NRE）、同物体 SkeletonAnimation 报 "No skeleton renderer found"。AddComponent/PasteComponentValues 对 [ExecuteAlways] 重型组件的编辑器生命周期（Awake/OnValidate 链）是黑盒，失败时无异常抛出，极难排查。

**How to apply:** 组件类型迁移改用「换 m_Script」方案（Unity 官方升级工具同款技巧）：

```csharp
// extendScript = AssetDatabase.LoadAssetAtPath<MonoScript>(新脚本路径)
SerializedObject so = new SerializedObject(oldComp);
so.FindProperty("m_Script").objectReferenceValue = extendScript;
so.ApplyModifiedPropertiesWithoutUndo();
```

组件 fileID 不变 → 外部引用（含 UI Component 字段）自动保持；序列化字段按名保留；新增字段取脚本默认值。预制体 diff 只有一行 guid，易审计。参考实现：[[SkeletonGraphicExtendMenu]]（`Assets/FrameWork/Editor/Base/SkeletonGraphicExtendMenu.cs`）。前提：新类型是原类型的子类（字段全集兼容）。
