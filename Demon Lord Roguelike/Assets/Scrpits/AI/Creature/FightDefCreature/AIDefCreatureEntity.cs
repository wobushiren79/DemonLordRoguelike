using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIDefCreatureEntity : AICreatureEntity
{

    /// <summary>
    /// 初始化数据
    /// </summary>
    public void InitData(GameFightCreatureEntity selfDefCreatureEntity)
    {
        RegisterEvent<UIViewCreatureCardItem>(EventsInfo.GameFightLogic_PutCard, EventForGameFightLogicPutCard);
        this.selfCreatureEntity = selfDefCreatureEntity;
    }

    /// <summary>
    /// 清空数据
    /// </summary>
    public override void ClearData()
    {
        base.ClearData();
        selfCreatureEntity = null;
    }

    /// <summary>
    /// 开始AI
    /// </summary>
    public override void StartAIEntity()
    {
        //默认闲置
        ChangeIntent(AIIntentEnum.DefCreatureIdle);
    }

    /// <summary>
    /// 关闭AI
    /// </summary>
    public override void CloseAIEntity()
    {

    }

    /// <summary>
    ///  初始化意图枚举
    /// </summary>
    /// <param name="listIntentEnum"></param>
    public override void InitIntentEnum(List<AIIntentEnum> listIntentEnum)
    {
        listIntentEnum.Add(AIIntentEnum.DefCreatureIdle);
        listIntentEnum.Add(AIIntentEnum.DefCreatureAttack);
        listIntentEnum.Add(AIIntentEnum.DefCreatureDefend);
        listIntentEnum.Add(AIIntentEnum.DefCreatureDead);
    }

    #region 事件回调
    public void EventForGameFightLogicPutCard(UIViewCreatureCardItem targetView)
    {

    }
    #endregion
}

