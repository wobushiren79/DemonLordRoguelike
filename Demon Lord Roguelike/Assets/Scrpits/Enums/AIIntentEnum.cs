using UnityEditor;
using UnityEngine;

public enum AIIntentEnum
{
    AttCreatureIdle,//闲置
    AttCreatureMove,//移动
    AttCreatureAttack,//攻击
    AttCreatureDead,//死亡

    DefCreatureIdle,//闲置
    DefCreatureAttack,//攻击
    DefCreatureDead,//死亡
    DefCreatureDefend,//防守

    DefCoreCreatureIdle,//闲置
    DefCoreCreatureDead,//死亡
}