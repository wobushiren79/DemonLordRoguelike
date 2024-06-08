using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIDefCreatureEntity : AICreatureEntity
{
    public override void StartAIEntity()
    {
        base.StartAIEntity();
        //默认闲置
        ChangeIntent(AIIntentEnum.DefCreatureIdle);
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
        listIntentEnum.Add(AIIntentEnum.DefCreatureIdle);
        listIntentEnum.Add(AIIntentEnum.DefCreatureDead);
    }
}
