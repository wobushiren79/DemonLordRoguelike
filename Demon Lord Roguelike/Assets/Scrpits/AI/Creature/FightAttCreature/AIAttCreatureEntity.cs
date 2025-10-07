using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIAttCreatureEntity : AICreatureEntity
{
    //目标移动位置
    public Vector3 targetMovePos;

    /// <summary>
    /// 初始化数据
    /// </summary>
    /// <param name="selfAttCreatureEntity"></param>
    public void InitData(GameFightCreatureEntity selfAttCreatureEntity)
    {
        RegisterEvent<UIViewCreatureCardItem>(EventsInfo.GameFightLogic_PutCard, EventForGameFightLogicPutCard);
        RegisterEvent<FightCreatureBean>(EventsInfo.GameFightLogic_CreatureDeadStart, EventForGameFightLogicCreatureDeadStart);
        this.selfCreatureEntity = selfAttCreatureEntity;
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
        base.ClearData();
        selfCreatureEntity = null;
        targetCreatureEntity = null;
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
        listIntentEnum.Add(AIIntentEnum.AttCreatureLured);
    }

    #region 事件回调
    public void EventForGameFightLogicPutCard(UIViewCreatureCardItem targetView)
    {
        //如果是同一路线
        var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        var defenseCreature =  gameFightLogic.fightData.GetCreatureById(targetView.cardData.creatureData.creatureUUId, CreatureTypeEnum.FightDefense);
        if (defenseCreature.fightCreatureData.positionCreate.z == selfCreatureEntity.fightCreatureData.positionCreate.z)
        {
            //如果正在前往目标 则重新寻找目标
            if (currentIntentEnum == AIIntentEnum.AttCreatureMove || currentIntentEnum == AIIntentEnum.AttCreatureAttack)
            {
                ChangeIntent(AIIntentEnum.AttCreatureIdle);
            }
        }
    }

    public void EventForGameFightLogicCreatureDeadStart(FightCreatureBean fightCreatureData)
    {
        //如果自己是在攻击中
        if (currentIntentEnum == AIIntentEnum.AttCreatureAttack)
        {   //如果是防御生物死了 并且是自己攻击的生物
            CreatureInfoBean creatureInfo = fightCreatureData.creatureData.creatureInfo;
            if (creatureInfo.GetCreatureType() == CreatureTypeEnum.FightDefense && fightCreatureData == targetCreatureEntity.fightCreatureData)
            {
                ChangeIntent(AIIntentEnum.AttCreatureIdle);
            }
        }
    }
    #endregion
}
