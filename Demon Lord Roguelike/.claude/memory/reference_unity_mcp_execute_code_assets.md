---
name: reference_unity_mcp_execute_code_assets
description: 用 Unity MCP execute_code 建特效/资源的踩坑：CodeDom(C#6)只能反射调包类型、Addressables 命名空间陷阱、特效走 Addressables 组 Effect 且地址=全路径、safety_checks 拦 DeleteAsset
metadata:
  type: reference
---

用 **Unity MCP（mcpforunity, HTTP 8080）的 `execute_code` 工具**在编辑器里跑 C# 直接建资源（预制/材质/贴图/粒子）时的关键约束，2026-07 建"魔物进阶完成专用特效 EffectAscendComplete_1"实测：

## execute_code 本身
- 以「方法体」运行，`return <obj>` 回传；导入 `UnityEngine`/`UnityEditor`；`Debug.Log` 可被 `read_console` 读到。
- **编译器是 CodeDom（C# 6）**，本机没装 Roslyn(Microsoft.CodeAnalysis)——`compiler:"auto"` 回退 codedom。⇒ **不能用 C#7+ 语法**(元组/模式匹配/`out var`/本地函数等)；**包的编辑器程序集(如 Unity.Addressables.Editor)不被引用**，直接写其类型会 `does not exist / missing assembly reference` 编译失败。
- 绕过办法：**反射**。`foreach (asm in System.AppDomain.CurrentDomain.GetAssemblies()) asm.GetType("全名")` 按字符串取类型(运行时程序集都在)，再 `GetProperty/GetMethod/Invoke`。运行时类型(如 Assembly-CSharp 里的 `EffectBase`)也这样反射拿，避免编译期依赖。
- `safety_checks`(默认 true)**拦 `AssetDatabase.DeleteAsset`/`File.Delete`/`Process.Start`/死循环**等。**关掉 safety_checks 会被 Claude Code auto 分类器拒绝**(用户没点名授权)。⇒ 别用 DeleteAsset；幂等改用 **load-or-create**(`LoadAssetAtPath` 有则改+`EditorUtility.SetDirty`,无则 `CreateAsset`)；`SaveAsPrefabAsset` 本身会覆盖同路径预制。
- **⚠️ 无人值守(Unity 窗口非前台)时 execute_code 做任何 `AssetDatabase` 写操作会死锁 Unity 主线程**(2026-07 建成就卡脉冲动画/流光材质实测)：纯逻辑(`return 2+2`)秒回，但 `CreateAsset`/`SaveAssets` 会挂起——execute_code 占着主线程、资产导入管线又要主线程泵消息 → 死锁，并把后续所有 MCP 主线程命令(连 `editor/state` 读取)全堵死。解法：把 Unity 窗口真正切到前台(ALT 解锁 + `SetForegroundWindow`，**别用 `AttachThreadInput`(也死锁)**)，前台化后卡住的导入立即完成、队列排空。**结论：无人值守场景建资产别用 execute_code，优先「临时自跑编辑器脚本」套路(见 [[reference_unity_editor_self_run_delete_trick]])——编辑器脚本用真 Roslyn 编译，无 CodeDom/C#6 限制、`UnityEngine.UI` 等程序集都可用，还搭 Auto Refresh 便车自动跑。**

## Addressables 陷阱(反射时的正确命名空间)
- `AddressableAssetSettingsDefaultObject` 在 **`UnityEditor.AddressableAssets`**(不是 `.Settings`!)；`AddressableAssetSettings`/`AddressableAssetGroup` 在 `UnityEditor.AddressableAssets.Settings`。取错命名空间→反射找不到类型静默失败。
- 拿 settings：`AddressableAssetSettingsDefaultObject.Settings`(静态属性,`BindingFlags.Public|Static`)。注册条目：`settings.CreateOrMoveEntry(guid, group)`→`entry.address=path`→`EditorUtility.SetDirty((Object)settings)`+`AssetDatabase.SaveAssets()`。

## 本项目特效加载约定(见 [[reference_unity_mcp_tool_bug]])
- `EffectManager` 用 `LoadAddressablesUtil.LoadAssetSync(key)`=`Addressables.LoadAssetAsync(key).WaitForCompletion()`,**key = 全资源路径**(`"Assets/LoadResources/Effects/{effectName}.prefab"`,pathEffect=`Assets/LoadResources/Effects`)。
- ⇒ **新特效预制必须注册成 Addressable,且 address=该全路径**,否则 `LoadAssetSync` 返回 null 加载不出。现有特效(如 EffectBuff_1)都在 **组 `Effect`**、address=各自全路径——新特效放同组、同址约定即可(可反射 `FindAssetEntry(existingGuid).parentGroup` 拿到该组)。
- 特效预制结构:根挂 `EffectBase`(`mainPS`+`listPS`),`PlayEffect()` 调 `mainPS.Play()`(默认 withChildren=true,连带子 PS 播);`EffectBean{effectName,effectPosition,timeForShow,isDestoryPlayEnd,isPlayInShow}` 走 `ShowEffect(EffectBean, cbShow)`——`isPlayInShow=false` 时回调里改完 `listPS[i].main.startColor` 再 `PlayEffect()`,可做「运行时按稀有度上色」。

## PowerShell 调 MCP streamable-http 的坑
- 每个独立 shell 调用是新进程,**MCP session 不跨调用持久**⇒ 每次要重新握手(initialize→取 `Mcp-Session-Id`→`notifications/initialized`→tools/call),或把多步塞进同一段脚本。
- pwsh `Invoke-WebRequest` 的响应头是**数组**,session id 要 `[string]($r.Headers["Mcp-Session-Id"]|Select-Object -First 1).Trim()` 强转干净字符串,否则回传 `-32600 Session not found`。响应是 SSE:取 `data:` 行 JSON。域重载(改脚本触发)会让旧 session 失效,轮询编译就绪需每轮重建 session;编译是否干净以 `read_console`(types=[error]) 为准。
