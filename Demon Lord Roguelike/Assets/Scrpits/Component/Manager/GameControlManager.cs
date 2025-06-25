using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameControlManager : BaseManager
{
    public GameObject objControlData;
    public GameObject controlTargetForEmpty;
    public GameObject controlTargetForCreature;

    /// <summary>
    /// 所有的控制
    /// </summary>
    public List<BaseControl> listControl = new List<BaseControl>();

    public void Awake()
    {
        if (objControlData == null)
        {
            LoadControlData();
        }
    }

    /// <summary>
    /// 开启或关闭所有控制
    /// </summary>
    /// <param name="isEnable"></param>
    public void EnableAllControl(bool isEnable)
    {
        controlTargetForEmpty.ShowObj(false);
        controlTargetForCreature.ShowObj(false);
        for (int i = 0; i < listControl.Count; i++)
        {
            var itemControl = listControl[i];
            itemControl.EnabledControl(isEnable);
        }
    }


    public void LoadControlData()
    {
        //如果没有找到主摄像头 则加载一个
        if (objControlData == null)
        {
            GameObject objCameraDataModel = LoadAddressablesUtil.LoadAssetSync<GameObject>(PathInfo.ControlDataPath);
            objControlData = Instantiate(gameObject, objCameraDataModel);
            objControlData.transform.localPosition = Vector3.zero;
            controlTargetForEmpty = objControlData.transform.Find("ControlEmty").gameObject;
            controlTargetForCreature = objControlData.transform.Find("ControlCreature").gameObject;
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

    /// <summary>
    /// 基础游戏控制
    /// </summary>
    protected ControlForGameBase _controlForGameBase;
    public ControlForGameBase controlForGameBase
    {
        get
        {
            if (_controlForGameBase == null)
            {
                _controlForGameBase = gameObject.AddComponent<ControlForGameBase>();
                listControl.Add(_controlForGameBase);
            }
            return _controlForGameBase;
        }
    }
}
