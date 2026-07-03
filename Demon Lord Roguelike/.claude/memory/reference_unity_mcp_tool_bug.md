---
name: reference_unity_mcp_tool_bug
description: Unity MCP -32602 全滅 bug 已修复且工具正常；2026-07-02 Unity 包升 10.0.0(server 仍 3.4.2)工具集大扩(脚本编辑/AI 生成/组件相机动画等)；仍无 Shader Graph 节点编辑
metadata:
  type: reference
---

本机 Unity MCP（`http://127.0.0.1:8080/mcp`，HTTP transport，`--project-scoped-tools`）。**版本双轨**：Unity 编辑器包 `com.coplaydev.unity-mcp`（git `#main`）已升到 **10.0.0**，但 Python 端 `mcp-for-unity-server`（uvx/PyPI）握手仍报 **v3.4.2**——工具集实际由 Unity 包动态注册（见 `mcpforunity://custom-tools`），故升包即换工具集。

## 现状（2026-07-03 复测，Unity 包升 10.0.0 后）

- **`-32602 Invalid request parameters` 全滅 bug 早已修复**：`tools/call` 正常构建校验器并执行。实测 `manage_asset(action=search)` 返回 `success:true`；参数缺失返回正常 pydantic 校验错误（如 `path Missing required argument`）而非 -32602。
- **`tools/list` 47 个（custom-tools 34 个）；数量与旧版巧合相同，但组成大改**。相较旧版新增一批能力：
  - **脚本编辑**：`manage_script`/`create_script`/`delete_script`/`validate_script`/`apply_text_edits`/`script_apply_edits`/`manage_script_capabilities`/`get_sha`（MCP 现可读写 `.cs`；但项目规则本就允许直接 Write/Edit `.cs`，用不用随意）。
  - **AI 生成**：`generate_image`/`generate_model`（含 `import_model`/`import_model_file`，均 `requires_polling` 上限 300s）。属**付费/AI 生成类**，调用前须比照 PixelLab 规则先征得用户同意，别在别的任务里"顺带"生成（见 [[feedback_pixellab_require_consent]]）。
  - **新增 manage_* 面**：`manage_components`/`manage_camera`/`manage_animation`/`manage_ui`/`manage_physics`/`manage_probuilder`/`manage_profiler`/`manage_packages`/`manage_build`；工具类 `find_gameobjects`/`find_in_file`/`batch_execute`/`execute_code`/`unity_reflect`/`unity_docs`/`get_test_job`/`debug_request_context`。
  - **仍在**：`manage_asset`/`manage_material`/`manage_prefabs`/`manage_scene`/`manage_gameobject`/`manage_shader`/`manage_graphics`/`manage_vfx`/`manage_texture`/`manage_scriptable_object`/`execute_menu_item`/`refresh_unity`/`read_console`/`run_tests`/`manage_editor`/`set_active_instance`。
- 握手/`tools/list`/`resources/read`(`mcpforunity://instances`、`mcpforunity://custom-tools`) 正常。服务端提示：先读 `mcpforunity://custom-tools` 看动态工具；多实例须 `set_active_instance`。**踩坑**：`Mcp-Session-Id` 响应头必须原样回传每个后续请求，否则报 `-32600 Session not found`（分多条 PowerShell 命令时 session 变量易丢，建议单条脚本内一气呵成握手+调用）。

## Shader Graph 说明（重要）

- **Unity MCP 无 Shader Graph 节点编辑工具**。`manage_shader` 仅是**手写 `.shader`(ShaderLab/HLSL) 文本 CRUD**(create/read/update/delete)，不碰 `.shadergraph` 节点图。`manage_material`/`manage_graphics`/`manage_vfx` 也非节点图编辑。
- `.shadergraph` 节点图的构建/编辑仍走**另一套服务 unity-skills**(`localhost:8090` 的 `shadergraph_*`)，见 [[reference_unityskills_shadergraph_limits]]（节点白名单、Vector2 赋值 bug 等限制未变）。

## 历史（升级前，已过时，保留备查）

升级前多数变更工具 `tools/call` 返回 `-32602`（即使参数合法/空参也报），仅 `read_console`/`execute_code` schema 简单可调；且 `execute_code` 因本机无 Roslyn、CodeDom 回退命令行过长而编译失败。故当时回退方案是**临时编辑器脚本 + `[DidReloadScripts]` 重载自动执行**（见 [[reference_unity_editor_self_run_delete_trick]]）。**现工具已修复，可优先直接用 MCP 工具，该回退仅在特殊场景备用。**
