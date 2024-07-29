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
    protected CreatureInfoBean creatureInfo;//生物信息

    public FightCreatureBean(int creatureId)
    {
        creatureData = new CreatureBean(creatureId);
        ResetData();
    }

    /// <summary>
    /// 添加所有皮肤 用于测试
    /// </summary>
    public void AddAllSkin()
    {
        var allData = CreatureModelInfoCfg.GetAllData();
        var creatureInfo = GetCreatureInfo();
        foreach (var itemData in allData)
        {
            var itemCreatureModelInfo = itemData.Value;
            if (itemCreatureModelInfo.model_id == creatureInfo.model_id)
            {
                creatureData.AddSkin(itemCreatureModelInfo.id);
            }
        }
    }

    /// <summary>
    /// 重置数据
    /// </summary>
    public void ResetData()
    {
        var creatureInfo = GetCreatureInfo();
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

    /// <summary>
    /// 获取攻击
    /// </summary>
    /// <returns></returns>
    public int GetAttDamage()
    {
        var creatureInfo = GetCreatureInfo();
        return creatureInfo.att_damage;
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
    /// 获取攻击动画出手时间
    /// </summary>
    /// <returns></returns>
    public float GetAttAnimCastTime()
    {
        var creatureInfo = GetCreatureInfo();
        return creatureInfo.att_anim_cast_time;
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
