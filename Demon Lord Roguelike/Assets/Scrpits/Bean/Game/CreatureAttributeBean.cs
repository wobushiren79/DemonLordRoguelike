using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureAttributeBean
{
    public int addHP = 0;//额外增加的HP
    public int addDR = 0;//额外增加的DR
    public int addATK = 0;//额外增加的ATK
    public int addASPD = 0;//额外增加的ASPD
    public int addMSPD = 0;//额外增加的MSPD

    /// <summary>
    /// 增加随机属性
    /// </summary>
    /// <param name="addNum"></param>
    public void AddRandomAttribute(int addNum)
    {
        for (int i = 0; i < addNum; i++)
        {
            int randomIndex = Random.Range(1, 5);
            switch (randomIndex)
            {
                case 1:
                    addHP += 10;
                    break;
                case 2:
                    addDR += 10;
                    break;
                case 3:
                    addATK += 1;
                    break;
                case 4:
                    addASPD += 1;
                    break;
            }
        }
    }
}