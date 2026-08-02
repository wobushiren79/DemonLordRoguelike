using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIAttackCreatureEntity : AICreatureEntity
{
    //目标移动位置
    public Vector3 targetMovePos;

    /// <summary>
    /// 初始化数据
    /// </summary>
    /// <param name="selfAttCreatureEntity"></param>
    public void InitData(FightCreatureEntity selfAttCreatureEntity)
    {
        RegisterEvent<UIViewCreatureCardItem>(EventsInfo.GameFightLogic_PutCard, EventForGameFightLogicPutCard);
        RegisterEvent<FightCreatureEntity>(EventsInfo.GameFightLogic_CreatureDeadStart, EventForGameFightLogicCreatureDeadStart);
        this.selfCreatureEntity = selfAttCreatureEntity;
    }

    public override void StartAIEntity()
    {
        //默认闲置
        ChangeIntent(AIIntentEnum.AttackCreatureIdle);
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
        listIntentEnum.Add(AIIntentEnum.AttackCreatureIdle);
        listIntentEnum.Add(AIIntentEnum.AttackCreatureDead);
        listIntentEnum.Add(AIIntentEnum.AttackCreatureAttack);
        listIntentEnum.Add(AIIntentEnum.AttackCreatureAttackCore);
        listIntentEnum.Add(AIIntentEnum.AttackCreatureMove);
        listIntentEnum.Add(AIIntentEnum.AttackCreatureLured);
        listIntentEnum.Add(AIIntentEnum.AttackCreatureKnockback);
    }

    #region 击退
    /// <summary>
    /// 发起击退（冲击波等位移效果的统一入口）：把击退参数写入击退意图并切换到它；
    /// 当前正在击退中则只刷新参数（原地续推，不重进意图）；击退过程/攻击循环打断由击退意图处理。
    /// </summary>
    /// <param name="knockbackDirection">击退方向（XZ 平面，意图内部归一化）</param>
    /// <param name="knockbackDistance">击退总距离（≤0 时不发起）</param>
    public void StartKnockback(Vector3 knockbackDirection, float knockbackDistance)
    {
        if (knockbackDistance <= 0)
            return;
        var kbIntent = GetIntent<AIIntentAttackCreatureKnockback>(AIIntentEnum.AttackCreatureKnockback);
        if (kbIntent == null)
            return;
        kbIntent.SetupKnockback(knockbackDirection, knockbackDistance);
        if (currentIntentEnum != AIIntentEnum.AttackCreatureKnockback)
            ChangeIntent(AIIntentEnum.AttackCreatureKnockback);
    }
    #endregion

    #region 事件回调
    public void EventForGameFightLogicPutCard(UIViewCreatureCardItem targetView)
    {
        //如果是同一路线（用当前道路 roadIndex 判定：被诱导换路后 positionCreate.z 仍是出生线路，会误判）
        var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        var defenseCreature =  gameFightLogic.fightData.GetCreatureById(targetView.cardData.creatureData.creatureUUId, CreatureFightTypeEnum.FightDefense);
        if (defenseCreature.fightCreatureData.positionCreate.z == selfCreatureEntity.fightCreatureData.roadIndex)
        {
            //如果正在前往目标 则重新寻找目标
            if (currentIntentEnum == AIIntentEnum.AttackCreatureMove || currentIntentEnum == AIIntentEnum.AttackCreatureAttack)
            {
                ChangeIntent(AIIntentEnum.AttackCreatureIdle);
            }
        }
    }

    public void EventForGameFightLogicCreatureDeadStart(FightCreatureEntity fightCreatureEntity)
    {
        //如果自己是在攻击中
        if (currentIntentEnum == AIIntentEnum.AttackCreatureAttack)
        {   //如果是防御生物死了 并且是自己攻击的生物
            if (fightCreatureEntity.fightCreatureData.creatureFightType == CreatureFightTypeEnum.FightDefense && fightCreatureEntity.fightCreatureData == targetCreatureEntity.fightCreatureData)
            {
                ChangeIntent(AIIntentEnum.AttackCreatureIdle);
            }
        }
    }
    #endregion
}
