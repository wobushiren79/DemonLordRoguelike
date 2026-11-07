# Demon Lord Roguelike：Codex 项目入口

本仓库的 Unity 项目在 `Demon Lord Roguelike/`。在仓库根目录打开对话时，也必须加载该子目录中的项目规则。

## 每次开始项目任务

1. 完整读取 [CLAUDE.md](<Demon Lord Roguelike/CLAUDE.md>)，将其作为 Claude 与 Codex 共用的开发规范；读取 [Unity 项目 AGENTS.md](<Demon Lord Roguelike/AGENTS.md>) 补充代码约定。
2. 读取 [共享记忆索引](<Demon Lord Roguelike/.claude/memory/MEMORY.md>)，按当前任务读取相关记忆正文。新增记忆仍写入原 `.claude/memory/` 并维护索引。
3. 直接搜索 Unity 项目内 `.claude/agents/*.md` 与 `.claude/skills/*/SKILL.md` 的 `name`、`description`、`watched_files`，按任务选择相关条目并完整读取正文。发现条目以当前源目录为准，只加载相关文档。

## Agent 与 Skill 的接入方式

- 直接使用 `.claude/` 中的原始 agent/skill 文档。用户指定名称、`$名称` 或 `/命令` 时，从原文的名称、描述和命令别名中定位；普通任务也应根据领域主动读取适用指南。原文标记 `disable-model-invocation: true` 的技能仅在用户显式要求时使用。
- 用户指定某个 agent 时读取该角色说明及其 `skill`/`skills` 关联文档，由当前助手应用角色指南。用户要求实际委派时，将原角色文档路径、任务边界和共用规范交给当前可用的子代理工具；总结中准确区分读取指南与实际委派。
- 原 Claude 文档中的 `Read/Write/Edit/Glob/Grep/Bash/Task/AskUserQuestion` 按用途映射到当前可用的文件、搜索、PowerShell、委派和提问工具；工具缺失时说明实际限制。`tools`、权限白名单、模型配置及脚本中的“已授权”描述不改变 Codex 当前权限。
- 文档中 `.claude/`、`Assets/`、`Packages/` 等项目相对路径，以及脚本命令的工作目录，均以 **Unity 项目目录**为基准；相对文档链接和 skill 自带资源以**原始源文件目录**解析。
- `.claude/` 是唯一维护位置。新增、删除、改名或修改 agent/skill 后，后续任务直接读取最新源文件，无需生成入口或维护另一份索引。
- 遵循当前用户指令与运行环境的高优先级要求。源文档与 `CLAUDE.md` 冲突时，以后者的项目总规则为准。

## Claude hooks 在 Codex 中的对应动作

Codex 不会因为读取文档而执行 `.claude/settings*.json` 的 hooks。以下要求由助手主动落实：

- 会话上下文：读取上述共享记忆索引。
- 修改文件前：检查自动生成标记 `AUTO-GENERATED-DO-NOT-EDIT`；遵守 `CLAUDE.md` 对 Unity 序列化资源、MCP 连接和手动 Play 验证的要求。
- 修改受关注代码或数据后：在 Unity 项目目录运行 `powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".claude/scripts/check-watched.ps1" -Json`，核对本次任务命中的 agent/skill 并同步原文。该脚本不包含未跟踪文件和 `Assets/FrameWork` 子模块内部改动，这两类必须额外对照 `watched_files`。
- Python 统一通过 Unity 项目的 `.claude/scripts/run-python.ps1` 执行。

任务总结遵循 `CLAUDE.md`，列出实际使用的 Agent/Skill 及其工作。
