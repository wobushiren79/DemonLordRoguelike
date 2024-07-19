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
    public UserDataBean GetUserDataData(Action<UserDataBean> action)
    {
        UserDataBean data = GetModel().GetUserDataData();
        if (data == null) {
            GetView().GetUserDataFail("没有数据",null);
            return null;
        }
        GetView().GetUserDataSuccess<UserDataBean>(data,action);
        return data;
    }

    /// <summary>
    /// 获取所有数据
    /// </summary>
    /// <param name="action"></param>
    public void GetAllUserDataData(Action<List<UserDataBean>> action)
    {
        List<UserDataBean> listData = GetModel().GetAllUserDataData();
        if (listData.IsNull())
        {
            GetView().GetUserDataFail("没有数据", null);
        }
        else
        {
            GetView().GetUserDataSuccess<List<UserDataBean>>(listData, action);
        }
    }

    /// <summary>
    /// 根据ID获取数据
    /// </summary>
    /// <param name="action"></param>
    public void GetUserDataDataById(long id,Action<UserDataBean> action)
    {
        List<UserDataBean> listData = GetModel().GetUserDataDataById(id);
        if (listData.IsNull())
        {
            GetView().GetUserDataFail("没有数据", null);
        }
        else
        {
            GetView().GetUserDataSuccess(listData[0], action);
        }
    }
} 