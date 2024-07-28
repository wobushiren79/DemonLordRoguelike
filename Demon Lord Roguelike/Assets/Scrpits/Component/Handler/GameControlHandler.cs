using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GameControlHandler : BaseHandler<GameControlHandler,GameControlManager>
{
    /// <summary>
    /// 设置战斗控制
    /// </summary>
    public void SetFightControl()
    {
        manager.EnableAllControl(false);
        manager.controlForGameFight.EnabledControl(true);
    }

    /// <summary>
    /// 设置基础移动控制
    /// </summary>
    public void SetBaseControl()
    {
        manager.EnableAllControl(false);
        manager.controlForGameBase.EnabledControl(true);
    }
}
