---
name: feedback-code-style
description: 所有方法和属性必须加 XML 注释并用 #region 分类
metadata:
  type: feedback
---

所有 C# 文件中的方法和属性必须：
- 使用 `/// <summary>` XML 注释说明用途
- 用 `#region` / `#endregion` 按功能分类组织代码

**Why:** 用户明确要求，保持代码可读性和一致性。

**How to apply:** 新增或修改任何方法/属性时，同步添加 XML 注释和 #region 归类。如果修改的文件已有 #region 结构，将新代码放入合适的 region；如果没有，按功能补全 region 分类。
