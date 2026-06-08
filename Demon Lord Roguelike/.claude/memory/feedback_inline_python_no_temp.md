---
name: feedback-inline-python-no-temp
description: 跑一次性 Python 优先用 run-python.ps1 -c 内联，别建临时 .py；以及绝对路径/点目录导致 allow 规则失配的原因
metadata:
  type: feedback
---

一次性的 Python 小任务（dump Excel 结构、临时校验、转换等）**优先用预授权的内联形式**，不要先 Write 一个临时 `.py` 再跑：

```
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".claude/scripts/run-python.ps1" -c "代码"
```

`...run-python.ps1 *` 在 `settings.json` 已 allow，内联 `-c` 根本不触发 Write，永不弹窗，也自动满足「临时脚本清理规则」。确需落临时文件时才写，且任务结束即删。

**Why:** 用户问「为什么 Write `.claude/scripts/_tmp_dump_lang.py` 还弹确认」。根因是用**绝对路径**调 Write 时，两条本该命中的 allow 规则都失配：① `Write(.claude/scripts/**)` 是以项目根锚定的相对模式，绝对路径带盘符+项目前缀（不以 `.claude/scripts/` 开头）故不匹配；② `Write(**/*.py)` 的 `**`/`*` 默认**不穿透点目录** `.claude/`，捞不到其下的 `.py`。两条全落空 → 每个路径单独弹窗。建临时文件既弹窗又制造待清理垃圾，内联 `-c` 一举绕开。

**How to apply:** 默认走内联 `-c`；只有「需反复执行/复用」才在 `.claude/scripts/` 落 `.py`。给 allow 规则配「绝对路径也能命中」的兜底时，用把点目录段写成字面量的浮动模式 `Write(**/.claude/scripts/**)`（`**/` 吃掉盘符前缀，`.claude/scripts/` 字面量避开点目录穿透问题），已加入 `settings.json`。相关：[[feedback-agent-skill-sync]]。
