using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GameControlHandler : BaseHandler<GameControlHandler,GameControlManager>
{
    /// <summary>
    /// ����ս������
    /// </summary>
    public void SetFightControl()
    {
        manager.EnableAllControl(false);
        manager.controlForGameFight.EnabledControl(true);
    }
}
