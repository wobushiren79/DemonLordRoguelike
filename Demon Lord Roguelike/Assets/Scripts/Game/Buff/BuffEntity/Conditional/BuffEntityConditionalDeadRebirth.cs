using UnityEngine;

public class BuffEntityConditionalDeadRebirth : BuffEntityConditionalDead
{
    #region 重生请求
    /// <summary>
    /// 触发BUFF（死亡结束事件时，旧实体尚未清理——事件先行是 BuffEntityConditionalDead 系的契约）
    /// <para>重生落点：占位已释放（冲锋生物冲锋中死亡）→ 死亡地点重生并再次冲锋；否则原格重生（原行为）。</para>
    /// <para>落点被其他生物占用则放弃重生（BUFF照常消耗）；放弃时不动卡片状态，卡片正常走休整CD回手。</para>
    /// </summary>
    public override bool TriggerBuffConditional(BuffEntityBean buffEntityData)
    {
        bool isTriggerSuccess = base.TriggerBuffConditional(buffEntityData);
        if (isTriggerSuccess == false)
            return false;
        var fightCreatureEntity = GetFightCreatureEntityForTarget();
        if (fightCreatureEntity == null)
            return false;
        if (fightCreatureEntity.aiEntity?.currentIntent is not AIIntentCreatureDead deathIntent)
            return false;
        var fightCreatureData = fightCreatureEntity.fightCreatureData;
        //重生落点：占位已释放（冲锋生物）→ 死亡地点；否则原格
        Vector3Int rebirthPos = fightCreatureData.isPositionReleased
            ? Vector3Int.RoundToInt(fightCreatureData.positionDead)
            : fightCreatureData.positionCreate;
        //落点被其他生物占用则放弃重生（此时旧实体仍在主列表，自身可能命中落点扫描，需排除自身）
        var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        var occupantEntity = gameFightLogic.fightData.GetDefenseCreatureByPos(rebirthPos);
        if (occupantEntity != null && occupantEntity != fightCreatureEntity)
            return true;
        //完成死亡事件派发后重生，旧 BUFF 仍可处理当前死亡事件。
        return deathIntent.TryQueueRebirth(rebirthPos);
    }
    #endregion
}
