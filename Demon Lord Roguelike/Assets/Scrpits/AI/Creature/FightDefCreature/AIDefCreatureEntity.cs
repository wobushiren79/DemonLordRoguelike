using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIDefCreatureEntity : AICreatureEntity
{
    public GameFightCreatureEntity selfDefCreatureEntity;
    public GameFightCreatureEntity targetAttCreatureEntity;

    /// <summary>
    /// ��ʼ������
    /// </summary>
    public void InitData(GameFightCreatureEntity selfDefCreatureEntity)
    {
        RegisterEvent<UIViewCreatureCardItem>(EventsInfo.GameFightLogic_PutCard, EventForGameFightLogicPutCard);
        this.selfDefCreatureEntity = selfDefCreatureEntity;
    }

    /// <summary>
    /// �������
    /// </summary>
    public override void ClearData()
    {
        base.ClearData();
        selfDefCreatureEntity = null;
    }

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
    ///  ��ʼ����ͼö��
    /// </summary>
    /// <param name="listIntentEnum"></param>
    public override void InitIntentEnum(List<AIIntentEnum> listIntentEnum)
    {
        listIntentEnum.Add(AIIntentEnum.DefCreatureIdle);
        listIntentEnum.Add(AIIntentEnum.DefCreatureAttack);
        listIntentEnum.Add(AIIntentEnum.DefCreatureDead);
    }

    /// <summary>
    /// ��ѯҪ�����ķ�������(�������)
    /// </summary>
    /// <returns></returns>
    public GameFightCreatureEntity FindAttCreatureDisMinEntity(int roadIndex, DirectionEnum direction = DirectionEnum.Right)
    {
        //���Ȳ�ѯͬһ·�ķ�������
        var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        List<GameFightCreatureEntity> listTargetData = gameFightLogic.fightData.GetFightAttCreatureByRoad(roadIndex);
        if (listTargetData.IsNull())
            return null;
        float disMin = float.MaxValue;
        GameFightCreatureEntity targetEntity = null;
        for (int i = 0; i < listTargetData.Count; i++)
        {
            //��ȡ��������ķ�������
            var itemTargetEntity = listTargetData[i];
            if (itemTargetEntity != null && !itemTargetEntity.IsDead())
            {
                var creatureObj = itemTargetEntity.creatureObj;
                if (direction == DirectionEnum.Right && creatureObj.transform.position.x >= selfDefCreatureEntity.creatureObj.transform.position.x)
                {
                    float dis = Vector3.Distance(creatureObj.transform.position, selfDefCreatureEntity.creatureObj.transform.position);
                    if (dis < disMin)
                    {
                        disMin = dis;
                        targetEntity = itemTargetEntity;
                    }
                }
            }
        }
        return targetEntity;
    }

    #region �¼��ص�
    public void EventForGameFightLogicPutCard(UIViewCreatureCardItem targetView)
    {

    }
    #endregion
}

