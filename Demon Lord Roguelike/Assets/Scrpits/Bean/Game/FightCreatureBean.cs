using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FightCreatureBean
{
    public CreatureBean creatureData;    //生物数据
    public int positionZCurrent;//当前位置

    public int liftCurrent;//当前生命值
    public int liftMax;//最大生命值

    public int armorCurrent;//当前护甲值
    public int armorMax;//最大护甲值

    protected CreatureInfoBean creatureInfo;//生物信息

    public FightCreatureBean(int creatureId)
    {
        creatureData = new CreatureBean(creatureId);
        var creatureInfo = GetCreatureInfo();
        liftCurrent = creatureInfo.life;
        liftMax = creatureInfo.life;
    }

    /// <summary>
    /// 获取移动速度
    /// </summary>
    /// <returns></returns>
    public float GetMoveSpeed()
    {
        var creatureInfo = GetCreatureInfo();
        return creatureInfo.speed_move;
    }

    /// <summary>
    /// 获取攻击CD
    /// </summary>
    /// <returns></returns>
    public float GetAttCD()
    {
        var creatureInfo = GetCreatureInfo();
        return creatureInfo.att_cd;
    }

    /// <summary>
    /// 获取生物信息
    /// </summary>
    /// <returns></returns>
    public CreatureInfoBean GetCreatureInfo()
    {
        if (creatureInfo == null || creatureInfo.id != creatureData.id)
        {
            creatureInfo = CreatureInfoCfg.GetItemData(creatureData.id);
        }
        return creatureInfo;
    }

    /// <summary>
    /// 获取创建的魔力
    /// </summary>
    public int GetCreateMagic()
    {
        var creatureInfo = CreatureInfoCfg.GetItemData(creatureData.id);
        return creatureInfo.create_magic;
    }
}
