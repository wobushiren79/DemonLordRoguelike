/*
* FileName: UserData 
* Author: AppleCoffee 
* CreateTime: 2024-07-16-17:44:25 
*/

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

public class UserDataService : BaseDataStorage
{
    protected readonly string saveFileName;

    public UserDataService()
    {
        saveFileName = "UserData";
    }

    /// <summary>
    /// 查询游戏配置数据
    /// </summary>
    public UserDataBean QueryData(int index)
    {
        return BaseLoadData<UserDataBean>($"{saveFileName}_{index}", jsonType: JsonType.Net);
    }

    /// <summary>
    /// 更新数据
    /// </summary>
    public void UpdateData(UserDataBean data, int index)
    {
        BaseSaveData<UserDataBean>($"{saveFileName}_{index}", data, jsonType: JsonType.Net);
    }

    /// <summary>
    /// 删除数据
    /// </summary>
    public void DeleteData(int index)
    {
        BaseDeleteFile($"{saveFileName}_{index}");
    }
}