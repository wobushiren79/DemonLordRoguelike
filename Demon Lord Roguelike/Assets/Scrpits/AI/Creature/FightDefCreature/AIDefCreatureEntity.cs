using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIDefCreatureEntity : AICreatureEntity
{
    public override void StartAIEntity()
    {
        base.StartAIEntity();
        //Ĭ������
        ChangeIntent(AIIntentEnum.DefCreatureIdle);
    }

    public override void CloseAIEntity()
    {
        base.CloseAIEntity();
    }

    /// <summary>
    ///  ��ʼ����ͼö��
    /// </summary>
    /// <param name="listIntentEnum"></param>
    public override void InitIntentEnum(List<AIIntentEnum> listIntentEnum)
    {
        listIntentEnum.Add(AIIntentEnum.DefCreatureIdle);
        listIntentEnum.Add(AIIntentEnum.DefCreatureDead);
    }
}
