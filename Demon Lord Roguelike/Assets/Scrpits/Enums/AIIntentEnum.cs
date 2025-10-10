using UnityEditor;
using UnityEngine;

public enum AIIntentEnum
{
    AttackCreatureIdle,//闲置
    AttackCreatureMove,//移动
    AttackCreatureAttack,//攻击
    AttackCreatureDead,//死亡
    AttackCreatureLured,//被诱惑中

    DefenseCreatureIdle,//闲置
    DefenseCreatureAttack,//攻击
    DefenseCreatureDead,//死亡
    DefenseCreatureDefend,//防守

    DefenseCoreCreatureIdle,//闲置
    DefenseCoreCreatureDead,//死亡
}