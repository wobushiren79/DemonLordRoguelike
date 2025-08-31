using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIDefCoreCreatureEntity : AICreatureEntity
{
    /// <summary>
    /// 初始化数据
    /// </summary>
    public void InitData(GameFightCreatureEntity selfDefCoreCreatureEntity)
    {
        RegisterEvent<UIViewCreatureCardItem>(EventsInfo.GameFightLogic_PutCard, EventForGameFightLogicPutCard);
        this.selfCreatureEntity = selfDefCoreCreatureEntity;
    }

    /// <summary>
    /// 清空数据
    /// </summary>
    public override void ClearData()
    {
        base.ClearData();
        this.selfCreatureEntity = null;
    }

    /// <summary>
    /// 开始AI
    /// </summary>
    public override void StartAIEntity()
    {
        //默认闲置
        ChangeIntent(AIIntentEnum.DefCoreCreatureIdle);
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
        listIntentEnum.Add(AIIntentEnum.DefCoreCreatureIdle);
        listIntentEnum.Add(AIIntentEnum.DefCoreCreatureDead);
    }

    #region 事件回调
    public void EventForGameFightLogicPutCard(UIViewCreatureCardItem targetView)
    {

    }
    #endregion
}

