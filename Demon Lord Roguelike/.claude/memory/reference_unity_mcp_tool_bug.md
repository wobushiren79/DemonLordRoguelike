---
name: reference_unity_mcp_tool_bug
description: 本机 Unity MCP 服务端(mcp-for-unity 3.4.2 HTTP)多数变更工具报 -32602，建资源走"重载自动执行的编辑器脚本"回退
metadata:
  type: reference
---

本机 Unity MCP（`http://127.0.0.1:8080/mcp`，server `mcp-for-unity-server` v3.4.2，HTTP transport，`--project-scoped-tools`）实测：握手/`tools/list`/`resources/read`(查 `mcpforunity://instances`) 正常，但**绝大多数工具 `tools/call` 返回 `-32602 Invalid request parameters`**（即使参数完全合法、空参也报）——疑似服务端对含复杂 schema(如 `value` 带 `items:{}`、多层 anyOf)的工具构建校验器失败。

- **不可用**（-32602）：`manage_asset`/`manage_material`/`manage_prefabs`/`execute_menu_item`/`refresh_unity`/`set_active_instance` 等。
- **可用**：`read_console`、`execute_code`（schema 简单）。
- **`execute_code` 实际也用不了**：本机无 Roslyn(Microsoft.CodeAnalysis)，`auto`/`codedom` 回退到 CodeDom，调 `mono.exe` 因引用程序集过多导致"文件名或扩展名太长"编译失败。

**结论/回退**：需要创建/改 Unity 资源(.mat/.prefab/标记 Addressable)时，别指望这套 MCP 工具。改用**临时编辑器脚本 + `[DidReloadScripts]` 重载自动执行**（带幂等守卫、建完自删），靠 Unity 下次编译(改了运行时 .cs 后聚焦 Unity 即 Auto Refresh)顺带触发；用仍可用的 `read_console` 验证创建日志。Addressable 标记用项目自带 `AddressableUtil.FindOrCreateGroup`/`AddAssetEntry(group, path, address=path)`。
