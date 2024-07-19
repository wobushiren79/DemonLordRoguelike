/*
* FileName: UserData 
* Author: AppleCoffee 
* CreateTime: 2024-07-16-17:44:25 
*/

using UnityEngine;
using UnityEditor;
using System;

[Serializable]
public class UserDataBean : BaseBean
{
    //拥有的金币数量（魔晶石）
    public long coin;


    /// <summary>
    /// 增加金币
    /// </summary>
    public void AddCoin(int coinNum)
    {
        coin += coinNum;
        if (coin < 0)
        {
            coin = 0;
        }
    }
}