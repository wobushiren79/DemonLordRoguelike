using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIAttCreatureEntity : AICreatureEntity
{
    //自己
    public GameFightCreatureEntity selfAttCreatureEntity;
    //目标攻击
    public GameFightCreatureEntity targetDefCreatureEntity;
    //目标移动位置
    public Vector3 targetMovePos;

    /// <summary>
    /// 初始化数据
    /// </summary>
    /// <param name="selfAttCreatureEntity"></param>
    public void InitData(GameFightCreatureEntity selfAttCreatureEntity)
    {
        EventHandler.Instance.RegisterEvent<FightCreatureBean>(EventsInfo.GameFightLogic_PutCard, EventForGameFightLogicPutCard);
        EventHandler.Instance.RegisterEvent<FightCreatureBean>(EventsInfo.GameFightLogic_CreatureDead, EventForGameFightLogicCreatureDead);
        this.selfAttCreatureEntity = selfAttCreatureEntity;
    }

    public override void StartAIEntity()
    {
        //默认闲置
        ChangeIntent(AIIntentEnum.AttCreatureIdle);
    }

    public override void CloseAIEntity()
    {

    }

    public override void ClearData()
    {
        selfAttCreatureEntity = null;
        targetDefCreatureEntity = null;
        EventHandler.Instance.UnRegisterEvent<FightCreatureBean>(EventsInfo.GameFightLogic_PutCard, EventForGameFightLogicPutCard);
        EventHandler.Instance.UnRegisterEvent<FightCreatureBean>(EventsInfo.GameFightLogic_CreatureDead, EventForGameFightLogicCreatureDead);
    }

    /// <summary>
    ///  初始化意图枚举
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
    /// 查询要攻击的防御生物(距离最近)
    /// </summary>
    /// <returns></returns>
    public GameFightCreatureEntity FindDefCreatureDisMinEntity(int roadIndex, DirectionEnum direction = DirectionEnum.Left)
    {
        //首先查询同一路的防守生物
        var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        List<FightPositionBean> listFightPosition = gameFightLogic.fightData.GetFightPosition(roadIndex);
        float disMin = float.MaxValue;
        GameFightCreatureEntity targetEntity = null;
        for (int i = 0; i < listFightPosition.Count; i++)
        {
            //获取距离最近的防守生物
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
                        targetEntity = itemFightPosition.creatureMain;
                    }
                }
            }
        }
        return targetEntity;
    }

    #region 事件回调
    public void EventForGameFightLogicPutCard(FightCreatureBean fightCreatureData)
    {
        //如果是同一路线
        if (fightCreatureData.positionCreate.z == selfAttCreatureEntity.fightCreatureData.positionCreate.z)
        {
            //如果正在前往目标 则重新寻找目标
            if (currentIntentEnum == AIIntentEnum.AttCreatureMove || currentIntentEnum == AIIntentEnum.AttCreatureAttack)
            {
                ChangeIntent(AIIntentEnum.AttCreatureIdle);
            }
        }
    }

    public void EventForGameFightLogicCreatureDead(FightCreatureBean fightCreatureData)
    {
        //如果自己是在攻击中
        if (currentIntentEnum == AIIntentEnum.AttCreatureAttack)
        {   //如果是防御生物死了 并且是自己攻击的生物
            CreatureInfoBean creatureInfo = fightCreatureData.GetCreatureInfo();
            if (creatureInfo.GetCreatureType() == CreatureTypeEnum.FightDef && fightCreatureData == targetDefCreatureEntity.fightCreatureData)
            {
                ChangeIntent(AIIntentEnum.AttCreatureIdle);
            }
        }
    }
    #endregion
}
