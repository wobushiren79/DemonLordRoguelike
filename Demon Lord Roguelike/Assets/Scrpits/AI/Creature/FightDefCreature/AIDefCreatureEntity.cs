using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIDefCreatureEntity : AICreatureEntity
{
    public GameFightCreatureEntity selfDefCreatureEntity;
    public GameFightCreatureEntity targetAttCreatureEntity;

    /// <summary>
    /// 初始化数据
    /// </summary>
    public void InitData(GameFightCreatureEntity selfDefCreatureEntity)
    {
        RegisterEvent<UIViewCreatureCardItem>(EventsInfo.GameFightLogic_PutCard, EventForGameFightLogicPutCard);
        this.selfDefCreatureEntity = selfDefCreatureEntity;
    }

    /// <summary>
    /// 清空数据
    /// </summary>
    public override void ClearData()
    {
        base.ClearData();
        selfDefCreatureEntity = null;
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
        listIntentEnum.Add(AIIntentEnum.DefCreatureDead);
    }

    /// <summary>
    /// 查询要攻击的防御生物(距离最近)
    /// </summary>
    /// <returns></returns>
    public GameFightCreatureEntity FindAttCreatureDisMinEntity(int roadIndex, DirectionEnum direction = DirectionEnum.Right)
    {
        //首先查询同一路的防守生物
        var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        List<GameFightCreatureEntity> listTargetData = gameFightLogic.fightData.GetFightAttCreatureByRoad(roadIndex);
        if (listTargetData.IsNull())
            return null;
        float disMin = float.MaxValue;
        GameFightCreatureEntity targetEntity = null;
        for (int i = 0; i < listTargetData.Count; i++)
        {
            //获取距离最近的防守生物
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

    #region 事件回调
    public void EventForGameFightLogicPutCard(UIViewCreatureCardItem targetView)
    {

    }
    #endregion
}

