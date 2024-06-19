using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIDefCreatureEntity : AICreatureEntity
{
    /// <summary>
    /// ��ʼAI
    /// </summary>
    public override void StartAIEntity()
    {
        //Ĭ������
        ChangeIntent(AIIntentEnum.DefCreatureIdle);
    }

    /// <summary>
    /// �ر�AI
    /// </summary>
    public override void CloseAIEntity()
    {

    }

    /// <summary>
    /// �������
    /// </summary>
    public override void ClearData()
    {

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
