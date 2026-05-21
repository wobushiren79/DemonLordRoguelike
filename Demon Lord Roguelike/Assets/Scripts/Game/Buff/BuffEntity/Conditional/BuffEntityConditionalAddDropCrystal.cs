/// <summary>
/// 增加掉落的魔晶
/// </summary>
public class BuffEntityConditionalAddDropCrystal : BuffEntityConditional
{
    public FightDropCrystalBean addFightDropCrystal;

    /// <summary>
    /// 清理数据
    /// </summary>
    public override void ClearData()
    {
        base.ClearData();
        addFightDropCrystal = null;
    }

    public override void HandleForEvent()
    {
        base.HandleForEvent();
        TriggerBuffConditional(buffEntityData);
    }

    /// <summary>
    /// 事件触发
    /// </summary>
    public override void EventForCreatureDeadDropCrystal(FightDropCrystalBean fightDropCrystal)
    {
        if (buffEntityData == null || buffEntityData.isValid == false) return;
        if (fightDropCrystal == null) return;
        //只响应"真实生物死亡掉落"事件
        //BUFF追加的水晶 dropperCreatureUUId 为空 不再触发任何 AddDropCrystal BUFF 避免叠加自反馈
        if (fightDropCrystal.dropperCreatureUUId.IsNull()) return;
        //跳过 BUFF 持有者自身掉落 防御自身掉水晶不应该再让自己的 BUFF 触发
        if (fightDropCrystal.dropperCreatureUUId.Equals(buffEntityData.targetCreatureUUId)) return;

        addFightDropCrystal = FightHandler.Instance.manager.GetFightDropCrystalBean(fightDropCrystal);
        base.EventForCreatureDeadDropCrystal(fightDropCrystal);
    }

    /// <summary>
    /// 触发BUFF
    /// </summary>
    public override bool TriggerBuffConditional(BuffEntityBean buffEntityData)
    {
        bool isTriggerSuccess = base.TriggerBuffConditional(buffEntityData);
        //无论是否触发成功 都释放对池化对象的引用 避免后续被池复用产生脏数据
        var dropToCreate = addFightDropCrystal;
        addFightDropCrystal = null;
        if (isTriggerSuccess == false)
            return false;
        if (dropToCreate == null)
            return false;
        //掉落水晶
        FightHandler.Instance.CreateDropCrystal(dropToCreate);
        return true;
    }
}