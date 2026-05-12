# CLAUDE.md

## Unity资源修改规则

所有Unity资源文件的修改（创建、编辑、删除）必须通过Unity MCP（mcpforunityserver）进行，禁止直接使用Write/Edit工具操作以下类型文件：

- `.prefab` - 预制体
- `.unity` - 场景
- `.mat` - 材质
- `.anim` / `.controller` - 动画/动画控制器
- `.asset` - ScriptableObject等资源
- `.meta` - 资源元数据
- 其他Unity序列化的二进制/YAML资源文件

C#脚本（`.cs`）不受此限制，可以正常直接编辑。

## MCP连接与Unity操作规则

以下MCP相关操作均为自动执行，无需征询用户确认：

1. **MCP连接检测与建立**：运行 `.agents/check-unity-mcp.ps1` 检查HTTP服务器状态，若未运行则自动启动。
2. **MCP会话初始化**：发送 `initialize` 请求并获取 `Mcp-Session-Id`，随后发送 `notifications/initialized` 完成握手。
3. **Unity实例查询与设置**：读取 `mcpforunity://instances` 资源获取活跃实例，调用 `set_active_instance` 设置目标实例。
4. **通过Unity MCP工具操作场景/GameObject/资源**：调用 `manage_scene`、`manage_gameobject`、`manage_asset` 等 MCP 工具时，直接执行无需确认。

以上操作的 PowerShell 命令（`Invoke-WebRequest`/`Invoke-RestMethod` 到 `http://127.0.0.1:8080/mcp`）均视为已授权。

## 任务结束总结规则

每次任务处理完成后的总结中，如果有 Agent 或 Skill 参与执行，必须列出：

- **Agent**：每个 Agent 的名称及其执行的具体操作
- **Skill**：每个 Skill 的名称及其执行的具体操作
