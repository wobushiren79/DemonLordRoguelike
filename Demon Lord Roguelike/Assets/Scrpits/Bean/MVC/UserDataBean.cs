/*
* FileName: UserData 
* Author: AppleCoffee 
* CreateTime: 2024-07-16-17:44:25 
*/

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

[Serializable]
public class UserDataBean : BaseBean
{
    //ӵ�еĽ��������ħ��ʯ��
    public long coin;

    //�����������
    public int lineupMax = 10;

    //��������
    public List<string> listLineupCreature = new List<string>();
    //���������������
    public List<CreatureBean> listBackpackCreature = new List<CreatureBean>();


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
        EventHandler.Instance.TriggerEvent(EventsInfo.Coin_Change);
    }

    /// <summary>
    /// ��ӱ�������
    /// </summary>
    public void AddBackpackCreature(CreatureBean creatureData)
    {
        listBackpackCreature.Add(creatureData);
    }
}