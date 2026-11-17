using System.Globalization;
using UnityEngine;

/// <summary>
/// 震撼到场：玩家放置持有者时，按词条记录的固定距离击退正前方同路的敌人。
/// </summary>
public class BuffEntityConditionalDeploymentKnockback : BuffEntityConditional
{
    #region 放置事件
    /// <summary>
    /// 匹配刚放置的防守魔物，从放置格向敌方入口击退范围内的敌人。
    /// </summary>
    public override void EventForPlayerPutCard(UIViewCreatureCardItem card)
    {
        if (buffEntityData?.isValid != true)
            return;

        string creatureId = card?.cardData?.creatureData?.creatureUUId;
        if (string.IsNullOrEmpty(creatureId) || creatureId != buffEntityData.targetCreatureUUId)
            return;

        var fightData = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>()?.fightData;
        var placedCreature = fightData?.GetCreatureById(creatureId, CreatureFightTypeEnum.FightDefense);
        var placedData = placedCreature?.fightCreatureData;
        if (placedData == null || placedCreature.creatureObj == null || placedCreature.IsDead() || placedData.isSummoned)
            return;

        float knockbackDistance = buffEntityData.GetTriggerValue();
        bool shouldTrigger = TryGetForwardRange(buffEntityData.GetBuffInfo(), out int forwardRange)
            && knockbackDistance > 0f && !float.IsInfinity(knockbackDistance)
            && TriggerBuffConditional(buffEntityData);
        //该实体只消费本次成功放置，重生和跨关恢复不派发放置事件。
        buffEntityData.isValid = false;
        if (!shouldTrigger)
            return;

        Vector3Int placedPosition = placedData.positionCreate;
        foreach (var enemy in fightData.GetAttackCreatureByRoad(placedData.roadIndex))
        {
            if (enemy.fightCreatureData?.creatureFightType != CreatureFightTypeEnum.FightAttack
                || enemy.creatureObj == null || enemy.IsDead() || enemy.aiEntity is not AIAttackCreatureEntity enemyAI)
                continue;

            Vector3 enemyPosition = enemy.creatureObj.transform.position;
            float forwardDistance = enemyPosition.x - placedPosition.x;
            //道路索引在受诱导换道时先更新，实际仍在邻路的敌人暂不受影响。
            if (forwardDistance <= 0f || forwardDistance > forwardRange || Mathf.Abs(enemyPosition.z - placedPosition.z) > 0.5f)
                continue;

            enemyAI.StartKnockback(Vector3.right, knockbackDistance);
        }
    }
    #endregion

    #region 配置
    /// <summary>
    /// 解析正前方范围，供战斗与词条说明共用。
    /// </summary>
    public static bool TryGetForwardRange(BuffInfoBean buffInfo, out int forwardRange)
    {
        return int.TryParse(buffInfo?.class_entity_data, NumberStyles.Integer, CultureInfo.InvariantCulture, out forwardRange)
            && forwardRange > 0;
    }
    #endregion
}
