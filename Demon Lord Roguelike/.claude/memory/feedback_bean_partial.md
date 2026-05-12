---
name: feedback-bean-partial
description: Bean 文件是自动生成的，扩展代码必须写在 BeanPartial 文件中
metadata:
  type: feedback
---

`*InfoBean.cs` 和 `*Bean.cs` 是框架自动生成文件，禁止直接添加任何方法或属性。所有扩展方法、辅助属性、解析逻辑必须写在对应的 `*BeanPartial.cs` 文件中。

**Why:** 自动生成文件随时可能被覆盖，手写代码会丢失。用户在 AttackModeInfoBean.cs 中发现了错误放置的 GetEffectHitId 方法。

**How to apply:** 需要给任何 Bean 类添加方法时，先找到或创建对应的 `*BeanPartial.cs` 文件，将代码写入其中，不得修改原始 Bean 文件。
