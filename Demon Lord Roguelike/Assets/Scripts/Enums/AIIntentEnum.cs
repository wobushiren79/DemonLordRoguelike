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

    DefenseCreatureIdle,//闲置
    DefenseCreatureAttack,//攻击
    DefenseCreatureDead,//死亡
    DefenseCreatureDefend,//防守
    DefenseCreatureCharge,//冲锋（charge_attack=1 冲锋自爆型：放卡后立即向前冲锋并释放原占位格，遇敌/到路尽头即死亡引爆）

    DefenseCoreCreatureIdle,//闲置
    DefenseCoreCreatureDead,//死亡
}