---
name: reference_unityskills_shadergraph_limits
description: Unity-Skills REST 工具的 shadergraph_* API 已知限制（节点白名单、Vector2 赋值 bug、URP 模板）
metadata:
  type: reference
---

通过 Unity-Skills REST 服务（localhost:8090，`shadergraph_*` skills）操作 Shader Graph 时的已知限制（基于 Unity 6000.3.11f1 / ShaderGraph current 实测）：

**节点白名单（`shadergraph_add_node` 只放行 29 个已验证节点）**
- 用 `shadergraph_list_supported_nodes` 查全集。
- **不支持**（实测报 `Unsupported nodeType`）：`NoiseNode`(Simple Noise)、`GradientNoiseNode`、`VoronoiNode`、`NormalFromHeightNode`、`NormalFromTextureNode`、`TimeNode`。
- 含意：**纯程序化噪声法线、时间驱动的流动动画无法通过工具构建**，只能在 Shader Graph 编辑器里手动补，或（绕过工具规则）手写 .shadergraph JSON。
- 可用的关键节点：Property/Color/Vector1-4、SampleTexture2D、SamplerState、UV、TilingAndOffset、Split/Combine/AppendVector、Add/Subtract/Multiply/Divide/Lerp/OneMinus/Saturate/Clamp/Remap/Branch、NormalUnpack、NormalStrength、Position、NormalVector、ViewDirection。

**Vector2 属性赋值 bug**
- `shadergraph_add_property` / `update_property` 对 `Vector2` 属性设置 value 必报错 `Object of type 'UnityEngine.Vector2' cannot be converted to type 'UnityEngine.Vector4'`，任何 value 格式（{x,y} / {x,y,z,w} / 数组）都失败。
- 规避：Vector2 属性只能加（默认 0,0）不能设值；需要非零 Vector2 常量时改用 `Vector2Node` —— 它的 X/Y 是标量输入槽（`set_node_defaults` 用裸数字 `4.0` 可设），或 `set_node_settings` 传 `settings={value:{x,y}}` 可整体设值。

**值格式约定**
- `set_node_defaults` 标量(Vector1)槽：value 传**裸数字**（`4.0`），传 `{x:4}` 报 cast 错。
- `add_property` Color：value=`{r,g,b,a}`；Vector1：裸数字。
- PropertyNode 须先 `add_property` 再 `add_node nodeType=PropertyNode settings={propertyReferenceName:"_Xxx"}`。

**模板**
- `shadergraph_create_graph templateName="0_Lit Basic"`（Cross Pipeline）产出含 HDTarget+UniversalTarget 的 Lit 图，但会带一整套示例 PBR 贴图网络（27 节点）；干净重建需先 `remove_node`/`remove_property` 清掉非 BlockNode 节点和模板属性，保留 SurfaceDescription.*/VertexDescription.* 主输出块。

**环境**：系统无 Python，调用工具用 PowerShell `Invoke-RestMethod` 直连 `http://localhost:8090/skill/<name>`（POST，UTF-8 JSON）即可。

成果案例：[[../../Assets/Out/WaterSurface.shadergraph]] 即用此工具构建的 URP Lit 水面图（外部法线贴图 + Lerp 深浅水色）。
