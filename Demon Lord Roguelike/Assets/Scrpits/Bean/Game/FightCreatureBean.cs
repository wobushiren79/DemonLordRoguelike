using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FightCreatureBean
{
    public CreatureBean creatureData;    //生物数据
    public Vector3Int positionCreate;//生成位置

    public int liftCurrent;//当前生命值
    public int liftMax;//最大生命值

    public int armorCurrent;//当前护甲值
    public int armorMax;//最大护甲值

    public CardStateEnum stateForCard = CardStateEnum.None;//卡片状态(用于UI展示)

    public FightCreatureBean(int creatureId)
    {
        creatureData = new CreatureBean(creatureId);
        ResetData();
    }

    /// <summary>
    /// 重置数据
    /// </summary>
    public void ResetData()
    {
        var creatureInfo = creatureData.GetCreatureInfo();
        liftCurrent = creatureInfo.life;
        liftMax = creatureInfo.life;
    }

    /// <summary>
    /// 改变护甲
    /// </summary>
    /// <param name="changeArmorData"></param>
    public int ChangeArmor(int changeArmorData,out int outArmorChangeData)
    {
        outArmorChangeData = 0;
        armorCurrent += changeArmorData;
        if (armorCurrent < 0)
        {
            outArmorChangeData = armorCurrent;
            armorCurrent = 0;
        }
        if (armorCurrent > armorMax)
        {
            armorCurrent = armorMax;
        }
        return armorCurrent;
    }

    /// <summary>
    /// 改变生命值
    /// </summary>
    /// <param name="changeLifeData"></param>
    public int ChangeLife(int changeLifeData)
    {
        liftCurrent += changeLifeData;
        if (liftCurrent < 0)
        {
            liftCurrent = 0;
        }
        if (liftCurrent > liftMax)
        {
            liftCurrent = liftMax;
        }
        return liftCurrent;
    }

}
