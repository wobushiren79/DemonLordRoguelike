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
    /// 获取用户数据
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
    /// 设置用户信息
    /// </summary>
    /// <param name="targetUserData"></param>
    public void SetUserData(UserDataBean targetUserData)
    {
        userData = targetUserData;
    }

    /// <summary>
    /// 保存用户数据
    /// </summary>
    public UserDataBean SaveUserData()
    {
        if (userData != null)
        {
            SaveUserData(userData);
        }
        return userData;
    }

    public void SaveUserData(UserDataBean targetUserData)
    {
        controllerForUserData.SaveUserData(targetUserData, null);
    }

    /// <summary>
    /// 删除用户数据
    /// </summary>
    public void DeleteUserData(UserDataBean targetUserData)
    {
        controllerForUserData.DeleteUserData(targetUserData);
    }

    /// <summary>
    /// 读取用户数据
    /// </summary>
    public void LoadUserData(int index, Action<int, UserDataBean> actionForComplete)
    {
        try
        {
            controllerForUserData.GetUserDataData(index, (userData) =>
            {
                actionForComplete?.Invoke(index, userData);
            });
        }
        catch (Exception e)
        {
            LogUtil.LogError($"读取用户数据失败:{e.Message}");
            UserDataBean errorData = new UserDataBean();
            errorData.isErrorData = true;
            actionForComplete?.Invoke(index, errorData);
        }
    }

    #region 回调
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
