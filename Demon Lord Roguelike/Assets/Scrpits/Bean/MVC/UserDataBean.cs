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
    public Dictionary<int, List<string>> dicLineupCreature =new Dictionary<int, List<string>>();
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
    /// ��ȡ������������λ��
    /// </summary>
    public int GetLineupCreaturePosIndex(int lineupIndex, string creatureId)
    {
        if (dicLineupCreature.TryGetValue(lineupIndex, out List<string> listCreatureId))
        {
            if (listCreatureId.Contains(creatureId))
            {
                return listCreatureId.IndexOf(creatureId); ;
            }
            else
            {
                return -1;
            }
        }
        else
        {
            return -1;
        }
    }

    /// <summary>
    /// �����������
    /// </summary>
    public bool AddLineupCreature(int lineupIndex, string creatureId)
    {
        if (dicLineupCreature.TryGetValue(lineupIndex, out List<string> listCreatureId))
        {
            if (listCreatureId.Contains(creatureId)) 
            {
                return false;
            }
            else
            {
                listCreatureId.Add(creatureId);
            }
        }
        else
        {
            dicLineupCreature.Add(lineupIndex,new List<string>() { creatureId });
        }
        return true;
    }

    /// <summary>
    /// �Ƴ���������
    /// </summary>
    public bool RemoveLineupCreature(int lineupIndex, string creatureId)
    {
        if (dicLineupCreature.TryGetValue(lineupIndex, out List<string> listCreatureId))
        {
            if (listCreatureId.Contains(creatureId))
            {
                listCreatureId.Remove(creatureId);
                return true;
            }
            else
            {
                return false;
            }
        }
        return false;
    }

    /// <summary>
    /// ��ӱ�������
    /// </summary>
    public void AddBackpackCreature(CreatureBean creatureData)
    {
        listBackpackCreature.Add(creatureData);
    }

    /// <summary>
    /// ����Ƿ������������
    /// </summary>
    public bool CheckIsLineup(int lineupIndex,string creatureId)
    {
        if (dicLineupCreature.TryGetValue(lineupIndex,out List<string> listCreatureId))
        {
            if (listCreatureId != null && listCreatureId.Contains(creatureId))
            {
                return true;
            }
        }
        return false;
    }
}