using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIAttCreatureEntity : AICreatureEntity
{
    public override void StartAIEntity()
    {
        base.StartAIEntity();
        //默认闲置
        ChangeIntent(AIIntentEnum.AttCreatureIdle);
    }

    public override void CloseAIEntity()
    {
        base.CloseAIEntity();
    }

    /// <summary>
    ///  初始化意图枚举
    /// </summary>
    /// <param name="listIntentEnum"></param>
    public override void InitIntentEnum(List<AIIntentEnum> listIntentEnum)
    {
        listIntentEnum.Add(AIIntentEnum.AttCreatureIdle);
        listIntentEnum.Add(AIIntentEnum.AttCreatureDead);
        listIntentEnum.Add(AIIntentEnum.AttCreatureMove);
    }
}
