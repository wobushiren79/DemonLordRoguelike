---
name: project-attack-dead-pos-pool-aba-bug
description: 战斗中进攻生物"攻击死亡位置/原地抽搐"偶发BUG排查进展：主嫌疑=FightCreatureEntity对象池ABA复用（SetData重置state=Live使持尸引用看到IsDead()=false），已排除预览体/尸体过滤/PutCard事件路径，下次复现需重新插桩（三处插桩点已记录）
metadata:
  type: project
---

# 进攻生物"攻击死亡位置/原地抽搐"偶发 BUG 排查（2026-08-03 起，未结案）

## 现象（两次复现，均为偶发、未开2倍速）
1. 防御单位阵亡后，玩家立刻选卡想补位但魔力不够放不上 → 进攻近战单位不往前走，原地抽搐（疑似意图循环），放卡成功后恢复攻击。
2. 防守生物死后玩家一直点击放置（魔力不足失败）→ 进攻生物**开始攻击死亡位置**（明确播攻击动画，不切移动），放置成功后恢复。

## 主嫌疑：FightCreatureEntity / FightCreatureBean 对象池 ABA 复用
- [CreatureManager.cs](Assets/Scripts/Component/Manager/CreatureManager.cs) `GetFightCreatureEntity`：池化复用（queuePoolForFightCreatureEntity，约8个缓存），出池 `SetData` 第一行即 `creatureFightState = Live`（FightCreatureEntity.cs:68）→ **任何仍持有旧 entity 引用的对象（AI 的 targetCreatureEntity、弹道、BUFF）会看到尸体"复活"（IsDead()=false），且位置/类型/UUID 已被新主人（进攻刷怪/新放防守）改写**。
- `IsDead()` 只看 `creatureFightState == Dead`（FightCreatureEntity.cs:600，旧的多重判据已注释掉）。
- FightCreatureBean / CreatureBean 同样池化（queuePoolForFightCreatureData / queuePoolForCreatureData），`EventForGameFightLogicCreatureDeadStart` 用 `fightCreatureData` **引用比较**判定"是不是我打死的那个"，ABA 下可能误判。
- 入池点：CreatureHandler.RemoveFightCreatureEntity（尸体 DeadEnd 后，延迟 RecycleDelay）。
- 解释力：放置成功时 `EventForGameFightLogicPutCard`（AIAttackCreatureEntity.cs:76）会把同路 Move/Attack 进攻单位强制切 Idle 重索敌 → **恰好打破"持尸卡住"状态**，与"放上去就恢复"吻合。

## 已排除路径（静态分析确认）
- 放卡预览 GameObject 不是 FightCreatureEntity、不进 dlDefenseCreatureEntity，索敌按 collider GameObject.name 反查 GetCreatureById（FightCreatureSearchUtil.FindCreatureEntityByRay）→ 预览体查不到被跳过，无害。
- 魔力不足时 PutCard 只 Toast return，不触发 GameFightLogic_PutCard（只有成功才触发，GameFightLogic.cs:595）。
- 尸体 state=Dead 在 SetCreatureDead 第一帧即设置，所有索敌口径（Ray/Area/Dis）都过滤 IsDead → 尸体本身不会被选为目标。
- CheckTargetInAttackRange（AIIntentCreatureAttack.cs:242）与索敌同口径（2026-08 击退卡死 BUG 已修过中心距误判），Move→Attack→Idle 几何上不构成自持循环。
- 防守方/防守核心的 PutCard 回调为空实现。

## 下次复现时的插桩方案（三处，用完即删）
1. `AIBaseEntity.ChangeIntent`：进攻生物每次切换打印 帧号/生物UUID/新意图/targetUUID/位置（判断是否意图循环——已验证不刷屏=非快速循环）。
2. `CreatureManager.GetFightCreatureEntity` 出池 + `RemoveFightCreatureEntity` 入池：打印 旧UUID→新UUID/类型（实锤 ABA 复用时刻）。
3. `AIIntentCreatureAttack.IntentUpdate`：每30帧打印 attackState/目标UUID/类型/IsDead/位置（观测卡住时内部状态，重点看 target UUID 是否突变为池复用后的新 UUID）。
- 复现要点：出现后**别救场，让异常持续≥15秒**再停，收集 `[意图插桩]/[池插桩]/[攻击插桩]` 三类日志；留意当时是否有新敌人波次刷出（刷怪=池复用的主要"新主人"）。

## 若实锤 ABA 的候选修复方向（未实施，到时再评估）
- 入池/出池时给 entity 加"代数"戳（generation），AI 持引用时同时记录代数，判活先比代数；或
- 出池 SetData 前广播"实体回收"事件让 AI 清空指向它的 targetCreatureEntity；或
- AI 不持 entity 引用，改持 UUID 每次经 GetCreatureById 查询（注意查询开销与死亡后查不到=目标丢失语义变化）。
- 注意同类问题可能也存在于弹道目标引用与 BUFF 引用，修复时一并评估。
