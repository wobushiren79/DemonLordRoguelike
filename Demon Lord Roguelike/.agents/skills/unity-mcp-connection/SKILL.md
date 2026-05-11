---
name: unity-mcp-connection
description: Unity MCP连接状态检测与自动启动。当AI需要与Unity Editor通过MCP协议交互时，使用此SKILL检查MCP HTTP服务器和桥接会话状态，并在必要时自动启动服务器。适用于任何涉及Unity Editor操作、场景编辑、GameObject操作、资源管理、运行测试等需要MCP通信的场景。
watched_files:
  - .agents/check-unity-mcp.ps1
---

# Unity MCP 连接管理

## 何时使用

在与Unity Editor交互**之前**，必须先确认MCP连接状态。以下场景需要调用此SKILL：

- 需要读取或修改Unity场景、GameObject、组件
- 需要执行Unity Editor操作（如运行测试、构建、截图）
- 需要管理Unity资源（预制体、材质、动画等）
- 用户提到了与Unity Editor相关的任何操作

## 连接架构

```
Claude Code (AI)
    |
    |  MCP Protocol (JSON-RPC over HTTP/WebSocket)
    v
MCP HTTP Server (uvx --from mcpforunityserver mcp-for-unity)
    |
    |  WebSocket / stdio
    v
Unity Editor (MCPForUnity.Editor)
```

## 检测脚本

使用 `.agents/check-unity-mcp.ps1` 脚本检测和自动启动MCP连接：

```powershell
# 检测 + 自动启动（如果未运行）
.agents/check-unity-mcp.ps1

# 仅检测，不尝试启动
.agents/check-unity-mcp.ps1 -CheckOnly

# 指定端口（默认8080）
.agents/check-unity-mcp.ps1 -Port 8080
```

脚本行为：
1. TCP探测 `127.0.0.1:8080` 检测HTTP server是否运行
2. 检测Unity Editor进程是否运行
3. 如果server未运行且未指定 `-CheckOnly`：
   - 查找 `uvx` 可执行文件
   - 在新终端窗口中自动启动HTTP server
4. 输出systemMessage（仅在未连接时）

## 手动连接流程

如果自动启动失败，按以下步骤手动连接：

### 1. 启动HTTP服务器

在终端中运行：

```bash
# 使用uvx启动（推荐）
uvx --from mcpforunityserver mcp-for-unity --transport http --http-url http://127.0.0.1:8080 --project-scoped-tools

# 或使用Python直接运行（如果已克隆仓库）
python -m mcp_for_unity_server --transport http --http-url http://127.0.0.1:8080
```

### 2. 在Unity Editor中开启Session

1. 打开Unity Editor
2. 菜单栏：`Window > MCP For Unity > Toggle MCP Window`（或快捷键 `Ctrl+Shift+M`）
3. 在MCP窗口中确认Transport为 **HTTP Local**
4. 点击 **Start Server** 按钮（如果服务器未运行）
5. 点击 **Start Session** 按钮
6. 状态显示为 "Session Active" 即连接成功

### 3. 验证连接

在MCP窗口的 **Advanced** 标签页中点击 **Test Connection** 验证。

## MCP API 交互流程

连接建立后，所有与Unity的交互通过 JSON-RPC over HTTP 协议完成。以下是以"在TestScene中创建GameObject"为例的完整调用链：

### 请求基础

- **URL**: `http://127.0.0.1:8080/mcp`
- **Method**: POST
- **必需请求头**:
  - `Content-Type: application/json`
  - `Accept: application/json, text/event-stream`
  - `Mcp-Session-Id: <session-id>`（initialize 之后的所有请求）
- **响应格式**: SSE (Server-Sent Events)，每条消息以 `event: message\ndata: <json>\n\n` 包裹

### 步骤 1: initialize（握手）

获取 Session ID，后续所有请求都需要携带此 ID。

```powershell
$body = '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"claude-code","version":"1.0"}}}'
$headers = @{"Accept"="application/json, text/event-stream"; "Content-Type"="application/json"}
$r = Invoke-WebRequest -Uri "http://127.0.0.1:8080/mcp" -Method POST -Body $body -Headers $headers
$sessionId = $r.Headers["Mcp-Session-Id"]  # 保存此 ID
```

**关键点**: `Mcp-Session-Id` 在 HTTP 响应头中返回，不在 body 中。

### 步骤 2: notifications/initialized

告知服务器客户端已就绪。这是一个**通知**（没有 `id` 字段），服务器不返回响应。

```powershell
$body = '{"jsonrpc":"2.0","method":"notifications/initialized","params":{}}'
# 注意: 此请求需要携带 Mcp-Session-Id 头
Invoke-RestMethod -Uri "http://127.0.0.1:8080/mcp" -Method POST -Body $body -Headers $headers
```

### 步骤 3: 查询 Unity 实例

通过 `resources/read` 读取 `mcpforunity://instances` 获取当前连接的 Unity Editor 实例列表。

```powershell
$body = '{"jsonrpc":"2.0","id":2,"method":"resources/read","params":{"uri":"mcpforunity://instances"}}'
$r = Invoke-RestMethod -Uri "http://127.0.0.1:8080/mcp" -Method POST -Body $body -Headers $headers
```

返回数据中的 `instances[].id` 格式为 `项目名称@哈希`，如 `Demon Lord Roguelike@742b8dd43152d9d1`。将此值作为后续工具调用的 `unity_instance` 参数。

### 步骤 4: 调用工具（tools/call）

所有实际操作通过 `tools/call` 方法执行，`params.name` 指定工具名，`params.arguments` 传递参数。必须携带 `unity_instance` 路由到正确的 Unity 实例。

**检查当前场景**:
```powershell
$body = '{"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"manage_scene","arguments":{"action":"get_active","unity_instance":"Demon Lord Roguelike@742b8dd43152d9d1"}}}'
```

**创建 GameObject**:
```powershell
$body = '{"jsonrpc":"2.0","id":5,"method":"tools/call","params":{"name":"manage_gameobject","arguments":{"action":"create","name":"TT","unity_instance":"Demon Lord Roguelike@742b8dd43152d9d1"}}}'
```

### 完整调用链图示

```
initialize (获取 Session ID)
  → notifications/initialized (握手完成)
    → resources/read(mcpforunity://instances) (查询实例)
      → tools/call(manage_scene, get_active) (确认场景)
        → tools/call(manage_gameobject, create) (执行操作)
```

### 常用工具速查

| 工具名 | 用途 | 常用 action |
|--------|------|-------------|
| `manage_scene` | 场景管理 | `get_active`, `get_hierarchy`, `load_scene`, `save_scene` |
| `manage_gameobject` | GameObject操作 | `create`, `get_components`, `set_property`, `delete` |
| `manage_asset` | 资源管理 | `search`, `create`, `modify`, `delete` |
| `manage_editor` | 编辑器控制 | `play`, `pause`, `stop` |
| `read_console` | 读取控制台 | - |

### 注意事项

- `Invoke-RestMethod` 在收到 SSE 响应时可能解析失败（非标准 JSON），改用 `Invoke-WebRequest` 并手动提取 `data:` 行
- 响应中 `structuredContent` 字段包含已解析的操作结果，比原始 `content[].text` JSON 字符串更易使用
- 同一个 Session ID 可复用多次调用，无需每次重新握手
- 如果服务器返回 `instance_count > 1`，必须先调用 `set_active_instance` 设置默认实例，或在每次工具调用时携带 `unity_instance` 参数

## 故障排除

| 问题 | 原因 | 解决方案 |
|------|------|----------|
| uvx not found | uv未安装或不在PATH中 | 安装uv：`powershell -c "irm https://astral.sh/uv/install.ps1 | iex"` |
| Port 8080 in use | 端口被其他程序占用 | 更改端口或在MCP窗口中修改HTTP URL |
| Session fails to start | Unity Editor未运行 | 确保Unity Editor已打开 |
| Connection timeout | 防火墙或网络问题 | 检查防火墙规则，确保localhost可访问 |
| Transport mismatch | 客户端和服务器传输模式不一致 | 统一使用HTTP Local或stdio模式 |

## 端口配置

默认HTTP端口为 **8080**。如需修改：

1. Unity Editor中打开MCP窗口
2. 在Connection部分修改HTTP URL（如 `http://127.0.0.1:9090`）
3. 同时修改脚本中的端口参数：`.agents/check-unity-mcp.ps1 -Port 9090`

## 注意事项

- HTTP server启动后可能需要3-10秒才能完全就绪（首次运行需下载依赖）
- 自动启动的server会在新终端窗口中运行，关闭终端即停止server
- Unity Editor必须在运行状态才能建立MCP session
- 多项目场景下，确保每个项目使用不同的HTTP端口