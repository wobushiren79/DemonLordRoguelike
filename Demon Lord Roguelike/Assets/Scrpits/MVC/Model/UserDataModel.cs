/*
* FileName: UserData 
* Author: AppleCoffee 
* CreateTime: 2024-07-16-17:44:25 
*/

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class UserDataModel : BaseMVCModel
{
    protected UserDataService serviceUserData;

    public override void InitData()
    {
        serviceUserData = new UserDataService();
    }

    /// <summary>
    /// 获取游戏数据
    /// </summary>
    /// <returns></returns>
    public UserDataBean GetUserDataData(int index)
    {
        UserDataBean data = serviceUserData.QueryData(index);
        return data;
    }

    /// <summary>
    /// 保存游戏数据
    /// </summary>
    /// <param name="data"></param>
    public void SetUserDataData(UserDataBean data)
    {
        serviceUserData.UpdateData(data, data.saveIndex);
    }

}