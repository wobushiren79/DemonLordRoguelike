---
name: project-framework-submodule
description: Assets/FrameWork 是 git 子模块，框架层改动需在子模块内单独 git 操作；check-watched.ps1 只扫主仓库 diff，框架层改动不会触发 watched_files 自动提示，需手动核对
metadata:
  type: project
---

`Assets/FrameWork/` 是一个 **git 子模块**（独立仓库，分支 master）。主仓库 `git status` 对它只显示小写 `m Assets/FrameWork`（子模块内部有改动），不展开具体文件。

**影响：**

- 框架层代码（`Assets/FrameWork/**`）的 git 提交/比对要在 `Assets/FrameWork` 目录内单独执行（`git -C Assets/FrameWork ...`）。
- `.claude/scripts/check-watched.ps1` 的 watched_files 自动比对**只扫主仓库 diff**，框架层改动（如 SpineHandler.cs、框架层新组件）**不会触发 PostToolUse 提示**——改完框架层文件后须手动对照各 agent/skill 的 watched_files 声明同步文档（典型命中：system-spine、spine-system、framework-* 系列）。
- 框架层新建的 `.cs` 文件在主仓库 `git status` 中不可见，提交时别忘了进子模块提交。

**Why:** 2026-09-27 修复 SkeletonGraphic mask 剔除时发现：改了 `Assets/FrameWork/Scripts/Component/Handler/SpineHandler.cs` 后 check-watched.ps1 未命中任何 spine 文档，排查确认是子模块盲区。

**How to apply:** 每次修改 `Assets/FrameWork/**` 后，主动 grep 各 `.claude/agents/*.md` 与 `.claude/skills/*/SKILL.md` 的 watched_files 确认命中项并同步；git 操作注意主仓库与子模块分别提交。
