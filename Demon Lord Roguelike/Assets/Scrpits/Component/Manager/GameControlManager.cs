using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameControlManager : BaseManager
{
    /// <summary>
    /// ���еĿ���
    /// </summary>
    public List<BaseControl> listControl = new List<BaseControl>();

    /// <summary>
    /// ������ر����п���
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
    /// ս����Ϸ����
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
