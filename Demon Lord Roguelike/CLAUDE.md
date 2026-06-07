# CLAUDE.md

## Shell 优先级规则

**执行命令行操作时一律优先使用 PowerShell（pwsh 7+）**，遵循 PowerShell 语法（`$null`、`$env:VAR`、反引号续行、动词-名词 cmdlet 等）。仅当任务确需 POSIX/Unix 脚本能力时才改用 Bash。

## 路径动态化规则

**所有需要记录/写入文件的路径（配置、脚本、权限规则、文档示例等）一律使用动态/相对地址，禁止写死静态绝对路径**（如 `E:\Unity\Project\DLR\...`、`C:\Users\galasports\...`）。原因：项目会在不同电脑、不同用户、不同盘符下打开，静态绝对路径换环境即失效。

具体要求：

- **权限规则（`.claude/settings*.json`）**：用相对项目根目录的写法（如 `Read(.claude/scripts/**)`），用户主目录用 `~`（如 `Read(~/.claude/projects/**/memory/**)`），禁止写绝对盘符路径。
- **PowerShell 脚本**：用 `$PSScriptRoot` 推导脚本所在目录、`$env:USERPROFILE` 取用户主目录；项目根目录 = 脚本位置按层级 `Split-Path` 向上推导（`.claude/scripts/` 下脚本向上两级即项目根）。
- **Python 脚本**：用 `os.path.dirname(__file__)` / `Path(__file__).resolve().parents[N]` 推导项目根，禁止硬编码绝对路径；可变路径通过命令行参数传入。
- **文档/示例**：涉及用户名、盘符的路径用占位符（如 `C:\Users\<USER>\...`）或相对路径，不写具体机器路径。
- **记忆文件**：统一存项目内 `.claude/memory/`（见下方「记忆系统」），随 git 共享，禁止写入 C 盘用户主目录。

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

1. **MCP连接检测与建立**：运行 `.claude/scripts/check-unity-mcp.ps1` 检查HTTP服务器状态，若未运行则自动启动。
2. **MCP会话初始化**：发送 `initialize` 请求并获取 `Mcp-Session-Id`，随后发送 `notifications/initialized` 完成握手。
3. **Unity实例查询与设置**：读取 `mcpforunity://instances` 资源获取活跃实例，调用 `set_active_instance` 设置目标实例。
4. **通过Unity MCP工具操作场景/GameObject/资源**：调用 `manage_scene`、`manage_gameobject`、`manage_asset` 等 MCP 工具时，直接执行无需确认。

以上操作的 PowerShell 命令（`Invoke-WebRequest`/`Invoke-RestMethod` 到 `http://127.0.0.1:8080/mcp`）均视为已授权。

## 代码注释与分类规则

所有 C# 文件中的方法和属性必须：

- 使用 `/// <summary>` XML 注释说明用途
- 用 `#region` / `#endregion` 按功能分类组织代码

## 输入处理规则

游戏内所有键盘/手柄等输入处理一律走 **InputActionUIEnum + Unity InputSystem** 体系，**禁止使用旧版 `Input` API**（如 `Input.GetKeyDown`、`Input.GetKey`、`Input.GetMouseButton`、`Input.GetAxis` 等）。

- UI 类（继承 `BaseUIInit`/`BaseUIView`/`BaseUIComponent`）通过重写 `OnInputActionForStarted(InputActionUIEnum inputType, InputAction.CallbackContext callback)` 响应输入，输入动作来源于 `GameInputActions.inputactions`，由 `InputManager.dicInputUI` 映射到 `InputActionUIEnum`。
- 新增按键需求时，应在 `GameInputActions.inputactions` 中配置绑定、在 `InputActionUIEnum` 中补充枚举，再通过 `OnInputActionForStarted` 派发，**不要**在 `Update()` 里轮询 `Input`。
- 数字键已封装为 `InputActionUIEnum.N1~N9`（同时绑定主键盘与小键盘），可直接复用。

## Bean 修改规则

`*InfoBean.cs` 和 `*Bean.cs` 文件是自动生成的，**禁止直接修改**。所有扩展方法、辅助属性、解析逻辑必须写在对应的 `*BeanPartial.cs` 文件中（如 `AttackModeInfoBeanPartial.cs`）。

## 记忆系统

项目记忆存储在 `.claude/memory/` 目录下，所有 AI 协作记忆（feedback、project、user、reference）均写入此目录，**禁止写入用户个人目录（C盘）**。读写记忆时路径统一使用：

```
.claude/memory/MEMORY.md         # 索引文件
.claude/memory/<name>.md         # 各条记忆文件
```

**团队共享机制**：`.claude/memory/` 随 git 提交，所有成员 clone/pull 即可获得。由于 Claude Code 原生只自动加载用户主目录下的记忆（不读仓库内的 `.claude/memory/`），项目通过 `.claude/settings.json` 的 **SessionStart Hook**（`.claude/scripts/load-memory.ps1`）在每次会话开始时把 `MEMORY.md` 索引注入上下文，确保任何成员的 AI 都能识别。新增/修改记忆后**必须同步更新 `MEMORY.md` 索引**，否则不会被注入。

## Excel 读写规则

所有通过脚本对 `.xlsx` 文件的读取和写入操作必须使用 **openpyxl** 库，并遵守以下规范：

- 文件编码统一使用 **UTF-8**（openpyxl 默认支持，无需额外指定）
- 读取时使用 `openpyxl.load_workbook(path)` 或 `load_workbook(path, read_only=True)`
- 写入时调用 `workbook.save(path)` 保存，**不得覆盖原文件前未备份**
- 禁止使用 `xlrd`、`xlwt`、`xlwings`、`pandas.read_excel` 等其他 Excel 库
- Python 脚本文件统一存放在 `.claude/scripts/` 目录下

### 配置表数据修改：Excel 是唯一真实源

当用户要求**新增/修改/删除配置表数据**时（例如 `excel_*[*].xlsx` 下的任何配置表），必须遵守：

- **Excel 源表必须修改**：`Assets/Data/Excel/excel_*.xlsx` 是配置数据的唯一真实源（single source of truth），任何配置数据变更**必须落到对应的 Excel 文件**。
- **对应的 JSON 可以不修改**：`Assets/Resources/JsonText/*.txt`（如 `UnlockInfo.txt`、`AchievementInfo.txt` 等）是由 Excel 通过 `ExcelEditorWindow` 编辑器工具导出生成的产物，理论上是可再生的派生数据。
  - 如果只改 JSON 不改 Excel，下次运行编辑器导出工具时会**直接覆盖丢失**这次修改 —— 严禁此类操作。
  - 仅修改 Excel 而不同步 JSON 时，应在任务总结中明确告知用户："JSON 未同步，需在 Unity 编辑器中运行配置导出工具重新生成"。
- **当 Excel 与 JSON 数据不一致时**（例如 JSON 存在但 Excel 缺失），默认以 **JSON 现存的运行时数据为参考**补齐 Excel，并提示用户核对源头是否还有遗漏。
- **禁止**仅修改 JSON 文件来"快速生效"配置变更，即使运行时立即可见，也是技术债，下一次导出就会丢失。

### Excel 备份清理规则

修改 Excel 前为防丢失数据创建的备份文件（如 `xxx.xlsx.bak`、`xxx.xlsx.bak.20260524_150955` 等任何形式的副本），在任务结束时**必须及时删除**，禁止遗留：

- **判定标准**：仅为本次修改做的"编辑前快照"或一次性回滚副本，属于临时备份，必须清理。
- **严禁留在 `Assets/Data/Excel/` 目录内**：该目录会被 `ExcelEditorWindow` 的"全部导出/全部生成 Entity"按文件遍历，导出的 JSON 以**工作表名**命名。备份文件含同名工作表时会**覆盖真表导出的 JSON**（典型症状：单个导出正常，全部导出却把数据还原成旧值），同时 Unity 还会为其生成多余的 `.meta`。
- **删除时机**：Excel 改动已验证/落盘后，立即删除备份文件（及其 `.meta`）。
- **确需长期保留备份时**：放到仓库工作区**之外**或 `Assets/` 之外的目录（例如仓库根的 `ExcelBackup/` 并加入 `.gitignore`），**绝不**放在被导出工具扫描的 Excel 目录里。
- **禁止把备份提交进 git**：提交前确认 `git status` 中没有 `*.bak*` 文件被 `add`。
- **委派给 Agent/Skill 修改 Excel 时**，须在 prompt 中明确告知"任务结束前删除所有 Excel 备份文件"。
- **任务结束总结**中如创建过 Excel 备份，应说明已删除（或已移出 Excel 目录）的备份路径，便于用户审计。

## 临时脚本清理规则

为完成单次任务而临时生成的 **PowerShell**（`.ps1`）或 **Python**（`.py`）脚本，在任务结束后必须**及时删除**，避免污染项目目录：

- **判定标准**：仅用于本次任务一次性执行（如临时图片合成、临时数据转换、一次性查询等），且不属于可复用工具链一部分的脚本，视为"临时脚本"。
- **删除时机**：脚本执行完毕、产出结果已经被验证或落盘后，立即删除该脚本文件。
- **保留例外**：明确具有复用价值、长期维护需求或被项目其他流程引用的脚本（例如位于 `.claude/scripts/` 下的通用工具脚本），不属于临时脚本，**不应删除**。是否保留如有疑问，须先与用户确认。
- **委派给 Agent/Skill 执行任务**时，若过程中产生临时脚本，亦需在任务结束总结前完成清理，或在 prompt 中明确告知子代理执行该清理动作。
- **任务结束总结**中如有创建过临时脚本，应在总结里简要说明已删除的脚本路径，便于用户审计。

## PixelLab 像素图生成规则

使用 PixelLab MCP 工具生成像素图时，所有生成的图片中的物体轮廓必须带有 **outline（描边）**：

- 调用任何生成类工具（`create_character`、`create_object`、`create_isometric_tile`、`create_topdown_tileset`、`create_sidescroller_tileset`、`create_tiles_pro` 等）时，必须在 `description` 或相关参数中明确要求 outline，例如添加描述词：`with black outline`、`outlined`、`with clear pixel outline`。
- 若工具提供独立的 outline 参数，优先使用该参数开启描边。
- 禁止生成无轮廓（no outline）的像素图片。

### 生成等待与轮询规则

PixelLab 所有生成类工具均为异步任务（返回 job/资源 ID 后需要后续查询）。**无论是主对话直接调用，还是通过 Agent（如 general-purpose、Explore、Plan 等子代理）或 Skill 间接调用 PixelLab MCP 工具**，在等待生成结果期间均必须遵守以下轮询规范：

- **轮询间隔固定为 60 秒**：每次调用对应的 `get_*` 工具（如 `get_character`、`get_object`、`get_isometric_tile`、`get_topdown_tileset`、`get_sidescroller_tileset`、`get_tiles_pro` 等）查询状态后，若状态仍为 `processing` / `pending` / `review` 未完成，等待 60 秒再发起下一次查询，不要进行其他操作。
- **不得使用更短的轮询间隔**（如每 1~15 秒查询一次），避免对 PixelLab 服务造成不必要的负担。
- 等待过程中应通过 `ScheduleWakeup` 或带有 60 秒延迟的脚本/sleep 命令实现间隔检测，禁止使用空轮询或无延迟循环。
- 一旦状态变为 `completed` 或 `failed`，立即停止轮询并处理结果。
- **委派给 Agent/Skill 执行 PixelLab 任务时**，必须在 prompt 中明确写明"轮询间隔固定为 60 秒"的要求，确保子代理或技能内部循环亦遵守该规则。

## 任务结束总结规则

每次任务处理完成后的总结中，如果有 Agent 或 Skill 参与执行，必须列出：

- **Agent**：每个 Agent 的名称及其执行的具体操作
- **Skill**：每个 Skill 的名称及其执行的具体操作
