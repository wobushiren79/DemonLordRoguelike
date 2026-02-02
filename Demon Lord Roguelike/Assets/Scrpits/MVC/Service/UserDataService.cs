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
        dataStoragePath =  $"{Application.persistentDataPath}/{saveFileName}_{index}";
        return BaseLoadData<UserDataBean>($"{saveFileName}_{index}", jsonType: JsonTypeEnum.Net);
    }

    /// <summary>
    /// 更新数据
    /// </summary>
    public void UpdateData(UserDataBean data, int index)
    {
        //创建文件 如果没有的话
        dataStoragePath =  $"{Application.persistentDataPath}/{saveFileName}_{index}";
        FileUtil.CreateDirectory(dataStoragePath);
        //备份数据最多备份3份
        if (data.saveRemarkIndex >= 3)
        {
            data.saveRemarkIndex = 0;
        }
        //先复制一份原来的数据（备份）
        bool isRemarkSuccess = FileUtil.CopyFile($"{dataStoragePath}/{saveFileName}_{index}", $"{dataStoragePath}/{saveFileName}_{index}_Backups_{data.saveRemarkIndex}", true);
        if (isRemarkSuccess)
        { 
            data.saveRemarkIndex ++;
        }
        //再生成新的
        BaseSaveData<UserDataBean>($"{saveFileName}_{index}", data, jsonType: JsonTypeEnum.Net);
    }

    /// <summary>
    /// 删除数据
    /// </summary>
    public void DeleteData(int index)
    {
        dataStoragePath =  $"{Application.persistentDataPath}/{saveFileName}_{index}";
        BaseDeleteFile($"{saveFileName}_{index}");
    }
}