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
    /// 返回议会主界面
    /// </summary>
    public void BackDoomCouncilMain()
    {        
        //刷新控制魔王的生物数据
        var userData = GameDataHandler.Instance.manager.GetUserData();
        var controlForGame = GameControlHandler.Instance.manager.controlForGameBase;
        controlForGame.SetCreatureData(userData.selfCreature);

        //弹出议会UI
        UIDoomCouncilMain doomCouncilMain = UIHandler.Instance.OpenUIAndCloseOther<UIDoomCouncilMain>();
    }
}