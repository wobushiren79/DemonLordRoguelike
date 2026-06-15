---
name: feedback-shader-chinese-labels
description: 所有 shader 的 Properties 参数显示名必须用中文标注用途；Header 分组标题只能用 ASCII
metadata:
  type: feedback
---

编写/修改任何 `.shader` 文件时，`Properties` 块里每个参数的**显示名**（即 `("...")` 引号内的字符串）必须用**中文**说明该参数是干啥的，最好附带简短作用提示，例如：

```
_SwayStrength ("摆动幅度 (左右摇摆大小)", Range(0, 1)) = 0.08
```

**Why:** 用户（美术/策划会调材质）要在 Material Inspector 里一眼看懂每个参数的作用，中文标注降低理解成本。

**How to apply:**
- 参数显示名 `("...")` 是字符串字面量，写中文完全合法，Inspector 正常显示——所有参数都要写中文。
- **但 `[Header(...)]` 分组标题括号内是标识符，不是字符串，只能用 ASCII（英文/无空格），写中文会导致 ShaderLab 解析报错** `syntax error, unexpected $undefined, expecting TVAL_ID`。所以分组标题保持英文，靠各参数的中文显示名表达含义。
- 委派给 Agent/Skill 写 shader 时，须在 prompt 中转达这条规则。
