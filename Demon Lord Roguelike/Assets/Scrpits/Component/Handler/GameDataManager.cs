using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class GameDataManager : IUserDataView
{
    public UserDataBean userData;
    public UserDataController controllerForUserData;

    public void Awake()
    {
        controllerForUserData = new UserDataController(this, this);
        controllerForGameConfig = new GameConfigController(this, this);
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
    /// �����û���Ϣ
    /// </summary>
    /// <param name="targetUserData"></param>
    public void SetUserData(UserDataBean targetUserData)
    {
        userData = targetUserData;
    }

    /// <summary>
    /// �����û�����
    /// </summary>
    public void SaveUserData()
    {
        SaveUserData(userData);
    }

    public void SaveUserData(UserDataBean targetUserData)
    {
        controllerForUserData.SaveUserData(targetUserData, null);
    }

    /// <summary>
    /// ��ȡ�û�����
    /// </summary>
    public void LoadUserData(int index, Action<int, UserDataBean> actionForComplete)
    {
        controllerForUserData.GetUserDataData(index, (userData) =>
        {
            actionForComplete?.Invoke(index, userData);
        });
    }

    #region �ص�
    public void GetUserDataFail(string failMsg, Action action)
    {
        action?.Invoke();
    }

    public void GetUserDataSuccess<T>(T data, Action<T> action)
    {
        action?.Invoke(data);
    }

    public void SetUserDataSuccess<T>(T data, Action<T> action)
    {
        action?.Invoke(data);
    }

    public void SetUserDataFail(string failMsg)
    {

    }
    #endregion
}
