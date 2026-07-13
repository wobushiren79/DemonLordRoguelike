using UnityEngine;

/// <summary>
/// 防守生物攻击意图：在通用攻击意图基础上，支持"正面优先、正面无目标时转身攻击身后敌人"。
/// 是否开启身后搜索由配置 CreatureInfo.attack_search_back 门控（IsAttackSearchBack）。
/// </summary>
public class AIIntentDefenseCreatureAttack : AIIntentCreatureAttack
{
    #region 字段
    /// <summary>是否允许搜索并转身攻击身后敌人（IntentEntering 缓存，避免每次攻击循环重复读配置）</summary>
    private bool isAttackSearchBack = false;
    #endregion

    #region 意图生命周期
    /// <summary>
    /// 进入防守攻击意图：指定待机/死亡回退意图，并缓存身后搜索开关
    /// </summary>
    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        selfAIEntity = aiEntity as AIDefenseCreatureEntity;
        intentForIdle = AIIntentEnum.DefenseCreatureIdle;
        intentForDead = AIIntentEnum.DefenseCreatureDead;
        //缓存身后搜索开关（配置门控，仅骷髅战士等配置为1的生物开启）
        isAttackSearchBack = selfAIEntity.selfCreatureEntity.fightCreatureData.creatureData.creatureInfo.IsAttackSearchBack();
        base.IntentEntering(aiEntity);
    }
    #endregion

    #region 攻击流程覆盖
    /// <summary>
    /// 重新搜索目标：防守生物正面（Right）优先，正面无目标且开启身后搜索时才向背后补搜
    /// </summary>
    protected override FightCreatureEntity FindNextTarget(BaseAttackMode attackMode)
    {
        return selfAIEntity.FindCreatureEntityForSingeFrontThenBack(DirectionEnum.Right, isAttackSearchBack);
    }

    /// <summary>
    /// 按目标相对自身的位置转身：目标在右→朝右，目标在左（身后）→朝左；不开启身后搜索时无需转身
    /// </summary>
    protected override void RefreshFaceForTarget()
    {
        if (!isAttackSearchBack)
            return;
        var targetEntity = selfAIEntity.targetCreatureEntity;
        if (targetEntity == null || targetEntity.creatureObj == null)
            return;
        float selfX = selfAIEntity.selfCreatureEntity.creatureObj.transform.position.x;
        float targetX = targetEntity.creatureObj.transform.position.x;
        //目标在自身右侧(含同x)→朝右迎正面来敌；在左侧→转身朝背后。SetFaceDirection 内部已对朝向未变做去重跳过
        Direction2DEnum faceDirection = targetX >= selfX ? Direction2DEnum.Right : Direction2DEnum.Left;
        selfAIEntity.selfCreatureEntity.SetFaceDirection(faceDirection);
    }
    #endregion
}
