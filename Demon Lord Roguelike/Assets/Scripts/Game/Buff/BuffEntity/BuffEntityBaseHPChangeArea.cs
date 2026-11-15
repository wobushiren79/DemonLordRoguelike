using System;
using System.Globalization;

public class BuffEntityBaseHPChangeArea : BuffEntityBaseHPChange
{
    #region 范围生命变化
    /// <summary>
    /// 执行通用触发判定并对范围内友军应用生命变化。
    /// </summary>
    public override bool TriggerBuff(BuffEntityBean buffEntityData)
    {
        if (!InvokeBaseTriggerBuff(buffEntityData)) return false;
        return HPChangeArea(buffEntityData, GetFightCreatureEntityForTarget());
    }

    /// <summary>
    /// 对范围内友军应用生命变化。死亡效果使用死者的生命上限，常规效果使用受益者上限。
    /// </summary>
    public static bool HPChangeArea(BuffEntityBean buffEntityData, FightCreatureEntity source, bool isChangeSelf = false, float? sourceMax = null)
    {
        return BuffEntityAreaChange.Apply(buffEntityData, source, isChangeSelf, target => HPChange(buffEntityData, target, sourceMax));
    }
    #endregion
}

/// <summary>
/// 范围回复查找同阵营的存活目标，包含友方特殊显示层。
/// </summary>
internal static class BuffEntityAreaChange
{
    #region 范围判定
    /// <summary>
    /// 读取配置半径并对命中的友军应用效果。
    /// </summary>
    internal static bool Apply(BuffEntityBean buffEntityData, FightCreatureEntity source, bool isChangeSelf, Action<FightCreatureEntity> applyChange)
    {
        if (source?.fightCreatureData == null
            || !float.TryParse(buffEntityData.GetBuffInfo().class_entity_data, NumberStyles.Float, CultureInfo.InvariantCulture, out float radius)
            || radius <= 0f)
            return false;

        var sourceData = source.fightCreatureData;
        int layerMask = 1 << sourceData.GetCreatureLayer(false);
        if (sourceData.creatureFightType == CreatureFightTypeEnum.FightDefense)
            layerMask |= (1 << LayerInfo.CreatureDef_Front) | (1 << LayerInfo.CreatureDef_Back);
        else if (sourceData.creatureFightType == CreatureFightTypeEnum.FightAttack)
            layerMask |= (1 << LayerInfo.CreatureAtt_Front) | (1 << LayerInfo.CreatureAtt_Back);

        var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        var center = source.IsDead() ? sourceData.positionDead : source.creatureObj.transform.position;
        var colliders = RayUtil.OverlapToSphere(center, radius, layerMask);
        if (colliders != null)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                var target = gameFightLogic.fightData.GetCreatureById(colliders[i].gameObject.name, sourceData.creatureFightType);
                if (target != null && !target.IsDead() && (target != source || isChangeSelf))
                    applyChange(target);
            }
        }
        BaseAttackMode.DrawTestAreaForSphere(center, radius, 1);
        return true;
    }
    #endregion
}
