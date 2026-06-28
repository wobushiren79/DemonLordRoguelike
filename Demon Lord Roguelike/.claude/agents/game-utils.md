---
name: game-utils
description: 游戏层工具类开发：Assets/Scripts/Utils/ 下的 AnimUtil、ColorUtil、CreatureUtil、ItemsUtil、GameUIUtil、FightCreatureSearchUtil 等。与 framework-utils（框架层）互补。
tools: Read, Write, Edit, Glob, Grep, Bash
skill: utils-system
watched_files:
  - Assets/Scripts/Utils/
---

# 游戏层工具类 (Game Utils) 开发代理

你负责 [Scripts/Utils/](Assets/Scripts/Utils/) 中的游戏层静态工具类开发。

## 职责范围

### 已有工具类

| 文件 | 用途 | 关联模块 |
|------|------|---------|
| [AbyssalBlessingUtil.cs](Assets/Scripts/Utils/AbyssalBlessingUtil.cs) | 深渊馈赠通用查询：`IsAbyssalBlessingTargetCreature(buff, creatureData, fightType)` 判定某馈赠 BUFF 是否实际作用于某生物（trigger_creature_type + 单体定向 UUID + 仅 IAttributeModifierSource/BuffEntityAttributeAttackTime 三连），口径与属性/攻速管线一致，供战斗卡片展示「作用于本魔物的馈赠」及 `CreatureBean.GetAbyssalBlessingChangeAttribute`（详情面板/非战斗预览的属性翻倍计算）复用；`CollectAbyssalBlessingEntityBean(creatureData, fightType, result)` 基于前者收集「作用于某生物的全部馈赠实体」(去重到馈赠粒度、result 复用零分配)，供战斗卡片图标展示等场景复用 | BuffBaseEntity、IBuffSingleTarget、CreatureFightTypeEnum |
| [AnimUtil.cs](Assets/Scripts/Utils/AnimUtil.cs) | DOTween 动画工具（UI 数字滚动等）。`partial class`，与框架层 [AnimUtil.cs](Assets/FrameWork/Scripts/Utils/AnimUtil.cs) 同名共享：本层只放 UI 表现类动画，与业务无关的通用 Animator 方法（如 `GetAnimClipLength`）放框架层 | DOTween |
| [BuffUtil.cs](Assets/Scripts/Utils/BuffUtil.cs) | 稀有度 BUFF 生成（扭蛋与魔物进阶共用）：`GetRarityBuffType`（R/SR/SSR→类型，余 None）/ `CreateRandomRarityBuff`（通用随机抽 1 条）/ `CreateAscendRarityBuff`（进阶：素材 BUFF 按 id 聚合，每 id 10%×数量 命中概率，命中继承并 `BuffBean.CreateRandomWithFloor` 重随机数值≥素材原值，未命中回退通用随机；UR/L 返回 null）/ `GetCreatureAscendBuffChances`（进阶详情展示：与 `CreateAscendRarityBuff` 同口径算各 BUFF 命中概率，素材 BUFF 每 id 10%×数量、末尾追加「随机增益」buffId=-1 兜底剩余概率，无对应类型返回空列表） | BuffBean、CreatureBean、RarityEnum/BuffTypeEnum |
| [ColorUtil.cs](Assets/Scripts/Utils/ColorUtil.cs) | HTML 颜色字符串解析(`ParseHtmlString`)、进度百分比分段配色(`GetProgressColor(rate01)`：0~1 分5段 0-0.2红`#C0392B`/0.2-0.4橙`#E67E22`/0.4-0.6黄`#F1C40F`/0.6-0.8浅绿`#2ECC71`/0.8-1蓝`#3498DB`，献祭成功率进度条与孵化缸进阶BUFF概率统一复用此单一真实源) | UnityEngine.ColorUtility |
| [CreatureUtil.cs](Assets/Scripts/Utils/CreatureUtil.cs) | CreatureSkin 类型多语言名 | TextHandler、Creature 枚举 |
| [ItemsUtil.cs](Assets/Scripts/Utils/ItemsUtil.cs) | 道具枚举多语言扩展 | TextHandler、Item 枚举 |
| [GameUIUtil.cs](Assets/Scripts/Utils/GameUIUtil.cs) | 渐变色 / 生物 UI 简易设置 / 详情设置 | CreatureHandler、SpineHandler、IconHandler |
| [FightCreatureSearchUtil.cs](Assets/Scripts/Utils/FightCreatureSearchUtil.cs) | 战斗目标搜索（射线/范围/距离遍历） | RayUtil、GameFightLogic |

### 与其他系统的边界

- **框架层工具类**（`Assets/FrameWork/Scripts/Utils/`、`Extension/`、`Tools/`）由 [framework-utils](framework-utils.md) 负责
- **CreatureUtil / FightCreatureSearchUtil** 同时被 [game-creature](game-creature.md) 观察，本 agent 侧重「工具类规范与重构」，game-creature 侧重「生物业务语义」
- **扩展方法**（带 `this` 关键字的静态方法）按约定应放到 `Assets/FrameWork/Scripts/Extension/` 或建立 `Assets/Scripts/Extension/`，而不是 Utils 目录

## 约束

- 工具类必须是 `static class`，方法 `public static`，保持纯函数风格
- 所有 `public` 方法必须有 `/// <summary>` XML 注释（遵循 CLAUDE.md 注释规则）
- 多个功能区使用 `#region` / `#endregion` 分组（按用途分，不按可见性分）
- 禁止在 Utils 中持有可变静态字段；只读常量必须用 `const` 或 `static readonly`
- 字符串拼接使用 `$""` 插值语法
- 硬编码的多语言 textId（如 `TextHandler.Instance.GetTextById(1001)`）应抽取为 `const int` 或在 EnumExtension 里集中维护
- 扩展方法（`this T`）必须放到 `Extension/` 目录，**不要**写在 `Utils/` 下
- 工具类禁止直接持有 UI 引用；UI 操作通过参数传入 `Graphic` / `RectTransform` 等
- 战斗搜索类工具应避免热路径分配：循环外缓存 `GameFightLogic`，0 结果返回 `null` 而非空 list

## 关联 Skill

详细开发指南请参考: [utils-system](../skills/utils-system/SKILL.md)
