---
name: reference_epplus_stream_write_noop
description: EPPlus 写 xlsx 严禁「new FileStream + new ExcelPackage(fs)」模式——本项目版本下 Save() 无异常但文件完全未落盘（2026-09-30 两次"保存成功却被覆盖"事故根因，PowerShell 加载同一 DLL 实证）；写必须用 new ExcelPackage(new FileInfo(path))，敏感路径加写后回读校验
metadata:
  type: reference
---

# EPPlus 流模式写入静默不生效（本项目 DLL 实证）

**现象**：`new FileStream(path, Open, ReadWrite, None)` + `new ExcelPackage(fs)` + 改单元格 + `ep.Save()` + `fs.Dispose()` —— **Save() 不抛异常、返回正常，但文件完全未修改**（mtime 与内容均不变）。

**实证**（2026-09-30）：用 PowerShell 直接加载项目同一 DLL（`Assets/FrameWork/Plugins/EPPlus/EPPlus.dll`）复现该模式 → 文件未变；同条件改用 `new ExcelPackage(new FileInfo(path))` + `Save()` + `Dispose()` → 正常落盘。

**事故**：幻化药测试面板 `TestTransformPotionGUI.TryWriteExcelOtherDataBatch` 是全项目唯一用流模式写的路径，导致用户两次「保存全部修改」显示 Excel✓ 实际未写入，随后 Mod 一键构建 `export` 用旧 Excel 重出 JsonText，手调值被覆盖丢失且无法找回（MOD 项目无 git、主项目 Mods/ 被 gitignore、ExcelBackup 无手调版本）。

**规则**：
- C# 写 xlsx 一律用 `new ExcelPackage(new FileInfo(path))` + using + `Save()`（ExcelUtil.SetExcelData 及全部编辑器工具同款）。
- `new ExcelPackage(FileStream)` 只允许用于读（如 ExcelUtil.GetExcelPackage 的 FileAccess.Read）。
- 可靠性敏感的写路径加**写后回读校验**（重开文件比对写入值，参照 TestTransformPotionGUI.TryWriteExcelOtherDataBatch），"Save() 无异常"不等于落盘。

相关：[[reference_unity_mcp_execute_code_encoding]]（GetExcelPackage 静默吞异常致导出假成功，同属"静默失败须显式校验"家族）
