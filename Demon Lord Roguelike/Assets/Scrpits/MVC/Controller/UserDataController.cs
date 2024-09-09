/*
* FileName: UserData 
* Author: AppleCoffee 
* CreateTime: 2024-07-16-17:44:25 
*/

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class UserDataController : BaseMVCController<UserDataModel, IUserDataView>
{

    public UserDataController(BaseMonoBehaviour content, IUserDataView view) : base(content, view)
    {

    }

    public override void InitData()
    {

    }

    /// <summary>
    /// 保存配置数据
    /// </summary>
    /// <param name="configBean"></param>
    /// <returns></returns>
    public void SaveUserData(UserDataBean userData, Action<UserDataBean> action)
    {
        if (userData == null)
        {
            GetView().SetUserDataFail("没有数据");
            return;
        }
        GetModel().SetUserDataData(userData);
        GetView().SetUserDataSuccess(userData, action);
    }

    /// <summary>
    /// 获取数据
    /// </summary>
    /// <param name="action"></param>
    /// <returns></returns>
    public void GetUserDataData(int index, Action<UserDataBean> action)
    {
        UserDataBean data = GetModel().GetUserDataData(index);
        if (data == null)
        {
            GetView().GetUserDataFail("没有数据", (() => { action?.Invoke(null); }));
        }
        GetView().GetUserDataSuccess<UserDataBean>(data, action);
    }
}