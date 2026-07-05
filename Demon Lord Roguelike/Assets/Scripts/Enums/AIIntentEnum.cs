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

    DefenseCreatureIdle,//闲置
    DefenseCreatureAttack,//攻击
    DefenseCreatureDead,//死亡
    DefenseCreatureDefend,//防守

    DefenseCoreCreatureIdle,//闲置
    DefenseCoreCreatureDead,//死亡
}