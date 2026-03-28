---
name: code-review
description: Code review tool for analyzing code changes. Use when the user triggers with /code-review command to review uncommitted git changes or specific modules. Supports reviewing current working directory changes or targeted module analysis.
---

# Code Review Skill

This skill provides automated code review for git-based projects.

## Usage

Trigger with `/code-review` command. Two modes:

### Mode 1: Review Uncommitted Changes (Default)
When user runs `/code-review` without arguments, review all uncommitted changes in the working directory.

Steps:
1. Run `scripts/get-git-diff.ps1` to get uncommitted changes
2. Analyze the diff for code quality issues
3. Provide structured review feedback

### Mode 2: Review Specific Module
When user runs `/code-review <module-name>`, review the specified module.

Steps:
1. Run `scripts/get-module-files.ps1 <module-name>` to find module files
2. Read and analyze the relevant source files
3. Provide structured review feedback

## Review Checklist

For each code review, analyze:

1. **Code Quality**
   - Variable/function naming clarity
   - Code complexity and readability
   - Proper error handling
   - Edge case handling

2. **Best Practices**
   - Language/framework conventions
   - Design patterns usage
   - DRY principle adherence
   - Single responsibility principle

3. **Potential Issues**
   - Logic errors or bugs
   - Performance concerns
   - Security vulnerabilities
   - Memory leaks

4. **Maintainability**
   - Comment quality and necessity
   - Documentation completeness
   - Test coverage indicators
   - Coupling and cohesion

## Output Format

Provide review results in this structure:

```
## 代码审查报告

### 审查范围
[描述审查的文件/模块]

### 总体评价
[简要总结代码质量]

### 详细审查结果

#### ✅ 优点
- [优点1]
- [优点2]

#### ⚠️ 建议改进
1. **[问题类别]**: [问题描述]
   - **位置**: [文件:行号]
   - **建议**: [具体改进方案]

#### 🐛 潜在问题
1. **[问题类型]**: [问题描述]
   - **位置**: [文件:行号]
   - **风险**: [可能的影响]
   - **建议**: [修复方案]

### 总结
[整体建议和行动项]
```
