using UnityEditor;
using UnityEngine;

public enum AIIntentEnum
{
    AttackCreatureIdle,//闲置
    AttackCreatureMove,//移动
    AttackCreatureAttack,//攻击
    AttackCreatureAttackCore,//攻击魔王(核心)：靠近魔王后固定触发一次攻击并让魔王死亡, 不走AttackMode
    AttackCreatureDead,//死亡
    AttackCreatureLured,//被诱惑中
    AttackCreatureKnockback,//被击退中（冲击波等位移效果强制切换，推移过程结束后回闲置重新索敌）
    AttackCreatureCastSkill,//释放技能（trigger_scene=1 技能的释放载体：由 NPC ai_param 的 skill_update 事件[AICreatureEntity 通用 Update 事件]到点切入，播一次攻击动作并在出手点发射技能攻击模块，结束回闲置）

    DefenseCreatureIdle,//闲置
    DefenseCreatureAttack,//攻击
    DefenseCreatureDead,//死亡
    DefenseCreatureDefend,//防守
    DefenseCreatureCharge,//冲锋（charge_attack=1 冲锋自爆型：放卡后立即向前冲锋并释放原占位格，遇敌/到路尽头即死亡引爆）

    DefenseCoreCreatureIdle,//闲置
    DefenseCoreCreatureDead,//死亡

    //通用（进攻/防守生物均可切换，注册见各自 InitIntentEnum）
    CreatureEmerge,//出土冒出（召唤物出场动画，攻守通用：由 AICreatureEntity.StartEmerge 强制切换，从地底匀速升回地面，期间不能移动/索敌/攻击，冒出完成按阵营回各自闲置意图）
}