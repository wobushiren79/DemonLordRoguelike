using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIDefCoreCreatureEntity : AICreatureEntity
{
    public GameFightCreatureEntity selfDefCoreCreatureEntity;

    /// <summary>
    /// ��ʼ������
    /// </summary>
    public void InitData(GameFightCreatureEntity selfDefCoreCreatureEntity)
    {
        EventHandler.Instance.RegisterEvent<FightCreatureBean>(EventsInfo.GameFightLogic_PutCard, EventForGameFightLogicPutCard);
        this.selfDefCoreCreatureEntity = selfDefCoreCreatureEntity;
    }

    /// <summary>
    /// �������
    /// </summary>
    public override void ClearData()
    {
        selfDefCoreCreatureEntity = null;
    }

    /// <summary>
    /// ��ʼAI
    /// </summary>
    public override void StartAIEntity()
    {
        //Ĭ������
        ChangeIntent(AIIntentEnum.DefCoreCreatureIdle);
    }

    /// <summary>
    /// �ر�AI
    /// </summary>
    public override void CloseAIEntity()
    {

    }

    /// <summary>
    ///  ��ʼ����ͼö��
    /// </summary>
    /// <param name="listIntentEnum"></param>
    public override void InitIntentEnum(List<AIIntentEnum> listIntentEnum)
    {
        listIntentEnum.Add(AIIntentEnum.DefCoreCreatureIdle);
        listIntentEnum.Add(AIIntentEnum.DefCoreCreatureDead);
    }

    #region �¼��ص�
    public void EventForGameFightLogicPutCard(FightCreatureBean targetData)
    {

    }
    #endregion
}

