# Demon Lord Roguelike - 开发规范

## Claude / Codex 共用上下文

在此目录及其子目录工作时，遵循[仓库入口](../AGENTS.md)的上下文加载和工具适配约定：开始任务先读取本目录的 [CLAUDE.md](CLAUDE.md) 与[共享记忆索引](.claude/memory/MEMORY.md)，再按任务直接查找并读取 `.claude/agents/`、`.claude/skills/` 原文。

共用文档继续在 `.claude/` 维护。Claude hooks 在 Codex 中的对应动作与路径基准见仓库入口。

## 代码规范

### 字符串拼接

**规范**：字符串拼接必须使用 `$""` 插值语法，不要使用 `+` 连接符。

**示例**：
```csharp
// ✅ 推荐
string result = $"{name} {value}";

// ❌ 不推荐
string result = name + " " + value;
```
