
public class BuffEntityBaseDRChangeArea : BuffEntityBaseDRChange
{
    public override bool TriggerBuff(BuffEntityBean buffEntityData)
    {
        bool isTriggerSuccess = base.InvokeBaseTriggerBuff(buffEntityData);
        if (isTriggerSuccess == false) return false;

        //获取指定生物
        var targetCreature = GetFightCreatureEntityForTarget();
        return DRChangeArea(buffEntityData, targetCreature);
    }

    /// <summary>
    /// 区域DR改变
    /// </summary>
    public static bool DRChangeArea(BuffEntityBean buffEntityData, FightCreatureEntity fightCreatureEntity, bool isChangeSelf = false)
    {
        var buffInfo = buffEntityData.GetBuffInfo();
        //作用半径
        float checkRadius = float.Parse(buffInfo.class_entity_data);
        //圆形半径
        var targetColliders = RayUtil.OverlapToSphere(fightCreatureEntity.fightCreatureData.positionDead, checkRadius, 1 << fightCreatureEntity.fightCreatureData.GetCreatrueLayer(false));
        if (targetColliders != null)
        {
            GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
            for (int i = 0; i < targetColliders.Length; i++)
            {
                var itemHitterCollder = targetColliders[i];
                string creatureId = itemHitterCollder.gameObject.name;
                var targetCreature = gameFightLogic.fightData.GetCreatureById(creatureId, fightCreatureEntity.fightCreatureData.creatureFightType);
                if (targetCreature != null && !targetCreature.IsDead())
                {
                    //判断是否要改变自己的HP 如果不改变并且当前生物是自己 则不处理
                    if (!isChangeSelf && targetCreature == fightCreatureEntity)
                    {

                    }
                    else
                    {

                        DRChange(buffEntityData, targetCreature);
                    }
                }
            }
        }
        //绘制测试范围
        BaseAttackMode.DrawTestAreaForSphere(fightCreatureEntity.fightCreatureData.positionDead, checkRadius, 1);
        return true;
    }
}