using UnityEngine;

public class AIIntentCreatureDead : AIBaseIntent
{
    public float timeUpdateForDead = 0f;
    public float timeUpdateForDeadCD = 1.1f;
    //当前AIEntity
    public AICreatureEntity selfAIEntity;
    public CreatureFightTypeEnum creatureFightType;
    private Vector3Int? rebirthPosition;

    #region 生命周期
    /// <summary>
    /// 记录死亡结束事件申请的重生落点，等事件派发结束后再生成替换实体。
    /// </summary>
    public bool TryQueueRebirth(Vector3Int position)
    {
        if (creatureFightType != CreatureFightTypeEnum.FightDefense || rebirthPosition.HasValue)
            return false;
        rebirthPosition = position;
        return true;
    }

    /// <summary>
    /// 开始死亡动画并记录触发死亡效果所需的落点。
    /// </summary>
    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        timeUpdateForDead = 0;
        rebirthPosition = null;
        //战斗结束清场(ClearGameForSimple)会回收AI实例并把 selfCreatureEntity 置空，
        //若此时仍有在途逻辑触发死亡意图，直接跳过避免空引用
        if (selfAIEntity == null || selfAIEntity.selfCreatureEntity == null)
            return;
        selfAIEntity.selfCreatureEntity.PlayAnim(SpineAnimationStateEnum.Dead, false);

        var selfFightCreatureEntity = selfAIEntity.selfCreatureEntity;
        selfFightCreatureEntity.fightCreatureData.positionDead = selfFightCreatureEntity.creatureObj.transform.position;
        EventHandler.Instance.TriggerEvent(EventsInfo.GameFightLogic_CreatureDeadStart, selfFightCreatureEntity);

        //成就统计-只有进攻方(敌方)生物死亡算作玩家击杀; 防守方死亡当前无任何消费者, 不派发以减少战斗高峰的事件量
        if (selfFightCreatureEntity.fightCreatureData.creatureFightType == CreatureFightTypeEnum.FightAttack)
        {
            EventHandler.Instance.TriggerEvent(EventsInfo.Achievement_CreatureKill, true);
        }
    }

    /// <summary>
    /// 播完死亡动画后派发死亡效果，完成待处理的重生，最后回收旧实体。
    /// </summary>
    public override void IntentUpdate(AIBaseEntity aiEntity)
    {
        //AI实例可能已被清场回收，selfCreatureEntity 置空时跳过，避免空引用
        if (selfAIEntity == null || selfAIEntity.selfCreatureEntity == null)
            return;
        timeUpdateForDead += GameFightLogic.GetFightDeltaTime();
        if (timeUpdateForDead >= timeUpdateForDeadCD)
        {
            timeUpdateForDead = 0;
            var selfFightCreatureEntity = selfAIEntity.selfCreatureEntity;
            //死亡结束事件的监听者仍需要旧实体及其 BUFF。
            EventHandler.Instance.TriggerEvent(EventsInfo.GameFightLogic_CreatureDeadEnd, selfFightCreatureEntity);
            if (rebirthPosition is Vector3Int position)
            {
                var creatureData = selfFightCreatureEntity.fightCreatureData.creatureData;
                CreatureHandler.Instance.CreateDefenseCreatureEntity(creatureData, position);
                BuffHandler.Instance.RemoveFightCreatureBuffs<BuffEntityConditionalDeadRebirth>(creatureData.creatureUUId);
            }
            CreatureHandler.Instance.RemoveFightCreatureEntity(selfFightCreatureEntity, creatureFightType);
        }
    }

    /// <summary>
    /// 清理重生请求，便于死亡意图复用。
    /// </summary>
    public override void IntentLeaving(AIBaseEntity aiEntity)
    {
        rebirthPosition = null;
    }
    #endregion

}
