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
    /// ���еĿ���
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
    /// ������ر����п���
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
        //���û���ҵ�������ͷ �����һ��
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

    /// <summary>
    /// ������Ϸ����
    /// </summary>
    protected ControlForGameBase _controlForGameBase;
    public ControlForGameBase controlForGameBase
    {
        get
        {
            if (_controlForGameBase == null)
            {
                _controlForGameBase = gameObject.AddComponent<ControlForGameBase>();
                listControl.Add(_controlForGameFight);
            }
            return _controlForGameBase;
        }
    }
}
