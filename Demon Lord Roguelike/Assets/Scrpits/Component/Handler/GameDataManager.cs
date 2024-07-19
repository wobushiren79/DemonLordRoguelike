using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public  partial class GameDataManager : IUserDataView
{
    public UserDataBean userData;
    public UserDataController controllerForUserData;

    public void Awake()
    {
        controllerForUserData = new UserDataController(this,this);
        controllerForGameConfig = new GameConfigController(this,this);
    }

    /// <summary>
    /// ��ȡ�û�����
    /// </summary>
    public UserDataBean GetUserData()
    {
        if (userData == null)
        {
            userData = new UserDataBean();
        }
        return userData;
    }

    /// <summary>
    /// �����û�����
    /// </summary>
    public void SaveUserData()
    {
        controllerForUserData.SaveUserData(userData,null);
    }

    #region �ص�
    public void GetUserDataFail(string failMsg, Action action)
    {

    }

    public void GetUserDataSuccess<T>(T data, Action<T> action)
    {

    }

    public void SetUserDataSuccess<T>(T data, Action<T> action)
    {

    }

    public void SetUserDataFail(string failMsg)
    {

    }
    #endregion
}
