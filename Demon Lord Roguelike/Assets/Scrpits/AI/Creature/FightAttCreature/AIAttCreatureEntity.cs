using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIAttCreatureEntity : AICreatureEntity
{
    //�Լ�
    public GameFightCreatureEntity selfAttCreatureEntity;
    //Ŀ�깥��
    public GameFightCreatureEntity targetDefCreatureEntity;
    //Ŀ���ƶ�λ��
    public Vector3 targetMovePos;

    /// <summary>
    /// ��ʼ������
    /// </summary>
    /// <param name="selfAttCreatureEntity"></param>
    public void InitData(GameFightCreatureEntity selfAttCreatureEntity)
    {
        EventHandler.Instance.RegisterEvent<GameFightCreatureEntity>(EventsInfo.GameFightLogic_PutCard, EventForGameFightLogicPutCard);
        this.selfAttCreatureEntity = selfAttCreatureEntity;
    }

    public override void StartAIEntity()
    {
        //Ĭ������
        ChangeIntent(AIIntentEnum.AttCreatureIdle);
    }

    public override void CloseAIEntity()
    {

    }

    public override void ClearData()
    {
        selfAttCreatureEntity = null;
        targetDefCreatureEntity = null;
        EventHandler.Instance.UnRegisterEvent<GameFightCreatureEntity>(EventsInfo.GameFightLogic_PutCard, EventForGameFightLogicPutCard);
    }

    /// <summary>
    ///  ��ʼ����ͼö��
    /// </summary>
    /// <param name="listIntentEnum"></param>
    public override void InitIntentEnum(List<AIIntentEnum> listIntentEnum)
    {
        listIntentEnum.Add(AIIntentEnum.AttCreatureIdle);
        listIntentEnum.Add(AIIntentEnum.AttCreatureDead);
        listIntentEnum.Add(AIIntentEnum.AttCreatureAttack);
        listIntentEnum.Add(AIIntentEnum.AttCreatureMove);
    }


    /// <summary>
    /// ��ѯҪ�����ķ�������(�������)
    /// </summary>
    /// <returns></returns>
    public GameFightCreatureEntity FindDefCreatureDisMinEntity(int roadIndex, DirectionEnum direction = DirectionEnum.Left)
    {
        //���Ȳ�ѯͬһ·�ķ�������
        var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        List<FightPositionBean> listFightPosition = gameFightLogic.fightData.GetFightPosition(roadIndex);
        float disMin = float.MaxValue;
        for (int i = 0; i < listFightPosition.Count; i++)
        {
            //��ȡ��������ķ�������
            var itemFightPosition = listFightPosition[i];
            if (itemFightPosition.creatureMain != null)
            {
                var creatureObj = itemFightPosition.creatureMain.creatureObj;
                if (direction == DirectionEnum.Left && creatureObj.transform.position.x <= selfAttCreatureEntity.creatureObj.transform.position.x)
                {
                    float dis = Vector3.Distance(creatureObj.transform.position, selfAttCreatureEntity.creatureObj.transform.position);
                    if (dis < disMin)
                    {
                        disMin = dis;
                        targetDefCreatureEntity = itemFightPosition.creatureMain;
                    }
                }
            }
        }
        //���û������ ˵������·��û�з��������ֱ��ǰ��·�ľ�ͷ
        if (targetDefCreatureEntity == null)
        {
            targetMovePos = new Vector3(0, 0, -roadIndex);
        }
        else
        {
            targetMovePos = targetDefCreatureEntity.creatureObj.transform.position;
        }
        return targetDefCreatureEntity;
    }

    #region �¼��ص�
    public void EventForGameFightLogicPutCard(GameFightCreatureEntity gameFightCreatureEntity)
    {
        //�����ͬһ·��
        if (gameFightCreatureEntity.fightCreatureData.positionZCurrent == selfAttCreatureEntity.fightCreatureData.positionZCurrent)
        {
            //�������ǰ��Ŀ�� ������Ѱ��Ŀ��
            if (currentIntentEnum == AIIntentEnum.AttCreatureMove || currentIntentEnum == AIIntentEnum.AttCreatureAttack)
            {
                ChangeIntent(AIIntentEnum.AttCreatureIdle);
            }
        }
    }
    #endregion
}
