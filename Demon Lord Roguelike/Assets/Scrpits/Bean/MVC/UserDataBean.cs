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
    //ӵ�еĽ��������ħ��ʯ��
    public long coin;


    /// <summary>
    /// ���ӽ��
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