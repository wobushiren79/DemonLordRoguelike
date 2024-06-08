using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameControlManager : BaseManager
{
    /// <summary>
    /// 所有的控制
    /// </summary>
    public List<BaseControl> listControl = new List<BaseControl>();

    /// <summary>
    /// 开启或关闭所有控制
    /// </summary>
    /// <param name="isEnable"></param>
    public void EnableAllControl(bool isEnable)
    {
        for (int i = 0; i < listControl.Count; i++)
        {
            var itemControl = listControl[i];
            itemControl.EnabledControl(isEnable);
        }
    }

    /// <summary>
    /// 战斗游戏控制
    /// </summary>
    protected ControlForGameFight _controlForGameFight;
    public ControlForGameFight controlForGameFight
    {
        get
        {
            if (_controlForGameFight == null)
            {
                _controlForGameFight = gameObject.AddComponent<ControlForGameFight>();
                listControl.Add(_controlForGameFight);
            }
            return _controlForGameFight;
        }
    }
}
