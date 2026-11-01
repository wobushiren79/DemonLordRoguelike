public class DoomCouncilBaseEntity
{
    public long doomCouncilBillId;
    //议会信息
    protected DoomCouncilInfoBean _doomCouncilInfo;
    public DoomCouncilInfoBean doomCouncilInfo
    {
        get
        {
            if (_doomCouncilInfo == null)
            {
                _doomCouncilInfo = DoomCouncilInfoCfg.GetItemData(doomCouncilBillId);
            }
            return _doomCouncilInfo;
        }
    }
    
    #region 支付
    /// <summary>
    /// 是否需要支付议案费用(仅直接通过入口置true;投票路径已在进入议会前支付)
    /// </summary>
    public bool isNeedPay;

    /// <summary>
    /// 是否延后到议案效果最终成功时支付(需要后续交互的立即型议案重写为true)
    /// </summary>
    public virtual bool isDeferredPayment => false;

    /// <summary>
    /// 尝试支付议案费用(魔晶与声望均足够才一起扣除;已支付或无需支付时直接成功)
    /// </summary>
    /// <returns>true=无需支付或支付成功</returns>
    public bool TryPayCost()
    {
        if (!isNeedPay)
            return true;
        var userData = GameDataHandler.Instance.manager.GetUserData();
        if (!userData.CheckHasCrystal(doomCouncilInfo.cost_crystal, true))
            return false;
        if (!userData.CheckHasReputation(doomCouncilInfo.cost_reputation, true))
            return false;
        userData.AddCrystal(-doomCouncilInfo.cost_crystal);
        userData.AddReputation(-doomCouncilInfo.cost_reputation);
        isNeedPay = false;
        return true;
    }
    #endregion

    /// <summary>
    /// 首次添加时触发
    /// </summary>
    /// <returns>是否结束</returns>
    public virtual bool TriggerFirst()
    {
        return false;
    }

    /// <summary>
    /// 战斗时掉落魔晶拾取
    /// </summary>
    /// <returns></returns>
    public virtual bool TriggerGameFightLogicDropAddCrystal(int addCrystal)
    {
        return false;
    }

    /// <summary>
    /// 战斗时添加经验
    /// </summary>
    /// <returns></returns>
    public virtual bool TriggerGameFightLogicAddExp(int addExp)
    {
        return false;
    }

    /// <summary>
    /// 战斗结束时触发
    /// </summary>
    /// <returns>是否结束</returns>
    public virtual bool TriggerGameFightLogicEndGame()
    {
        return false;
    }

    /// <summary>
    /// 进入基地时触发
    /// </summary>
    /// <returns>是否结束</returns>
    public virtual bool TriggerWorldEnterGameForBaseScene()
    {
        return false;
    }

    /// <summary>
    /// 获取该议案对下一场战斗敌人的强度倍率(整体相乘到敌人 HP/护甲/攻击力)
    /// 默认1=不改变; 「敌人更强/更弱」议案重写此方法返回倍率(如2/0.5)
    /// </summary>
    /// <returns>敌人强度倍率</returns>
    public virtual float GetEnemyIntensityRate()
    {
        return 1f;
    }

    /// <summary>
    /// 返回议会主界面
    /// </summary>
    public void BackDoomCouncilBill()
    {        
        //刷新控制魔王的生物数据
        var userData = GameDataHandler.Instance.manager.GetUserData();
        var controlForGame = GameControlHandler.Instance.manager.controlForGameBase;
        controlForGame.SetCreatureData(userData.selfCreature);

        //弹出议会UI
        UIDoomCouncilBill doomCouncilBill = UIHandler.Instance.OpenUIAndCloseOther<UIDoomCouncilBill>();
    }
}