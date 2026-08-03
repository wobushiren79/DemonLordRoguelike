---
name: feedback_creature_layer_front_design
description: CreatureDef_Front/CreatureAtt_Front 层让敌对方物理搜索搜不到是故意设计（烂泥史莱姆：敌人走过被减速但不把它当攻击目标），禁止当"索敌失效bug"去扩搜索mask/改回layer
metadata:
  type: feedback
---

`excel_creature_info.creature_layer` 配为 `CreatureDef_Front` / `CreatureAtt_Front` 的生物，会被 `CreatureHandler.GetFightCreatureObj` 把根 GameObject.layer 改为 Front 层（BoxCollider 挂在预制根节点，随根一并改层），同时 Spine 节点 Z 前移 0.1（渲染显示在其他魔物前面）。

由此产生的效果——索敌/攻击命中的物理搜索 mask 写死 `1 << CreatureDef` / `1 << CreatureAtt`（[FightCreatureSearchUtil.cs](Assets/Scripts/Utils/FightCreatureSearchUtil.cs) `FindCreatureEntity`、[BaseAttackMode.cs](Assets/Scripts/Game/Fight/AttackMode/BaseAttackMode.cs) 攻击层 mask），Front 层不在 mask 内，**敌人在物理上搜不到它**——是**故意设计，不是 bug**。

**Why:** 典型使用者烂泥史莱姆(id=3003, attack_mode=400001)：敌人从它身上走过会被减速，但敌人不会发现/攻击它——"地面附着型"魔物就是靠移出敌方搜索层来实现"只影响敌人、不被敌人当目标"。2026-08-03 我（AI）曾把"配 Front 层后敌人搜不到"误判为致命 bug 并建议扩搜索 mask 修复，被用户纠正。

**How to apply:**
- 禁止以"修复索敌失效/打不到"为由扩大搜索 mask（把 Front 层加进 mask）或把生物 layer 改回默认层——那会直接破坏烂泥史莱姆的设计。
- 涉及 creature_layer / 生物前后显示（渲染排序）的需求时，先认清这套机制的**双重语义**：①移出敌人物理搜索（不被攻击）②Spine Z 前移（显示在前）；只想要"显示在前"而不想影响索敌时，不能复用 Front 层，应走渲染侧方案（sortingOrder 等）。
- 渲染侧"显示在前"的根治方案已落地（2026-08-03）：**战斗场景相机透明排序改自定义世界 Z 轴**（`CameraManager.SetTransparencySortForFight()`，仅 `SetCameraForControlFight` 启用 cm_Fight 时调用；`HideAllCM` 内 `ResetTransparencySort()` 还原 Default，仅战斗场景生效），使 Z 前移 0.1 与镜头角度无关。注意勿选"全局抬 sortingOrder"方案——它会让 Front 生物压过更前排（小路号）的生物，破坏"前排挡后排"语义。
- `creature_layer_find` 字段（"生物优先级搜寻"）在配置里存在但代码未接线，勿假设它生效。
- 语义权威文档：[[creature-system]] SKILL「CreatureInfoBean - 生物配置」章节的 creature_layer 说明块。
