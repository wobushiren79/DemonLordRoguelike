using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Cinemachine.CinemachineOrbitalTransposer;

[Serializable]
public class CreatureBean
{
    //ID
    public long id;
    //生物名字
    public string creatureName;
    //等级
    public int level;
    //所有的皮肤数据
    public List<CreatureSkinBean> listSkinData = new List<CreatureSkinBean>();

    protected CreatureInfoBean creatureInfo;//生物信息
    public CreatureBean(long id)
    {
        this.id = id;
    }

    /// <summary>
    /// 添加所有皮肤 用于测试
    /// </summary>
    public void AddAllSkin()
    {
        var allData = CreatureModelInfoCfg.GetAllData();
        var creatureInfo = CreatureInfoCfg.GetItemData(id);
        foreach (var itemData in allData)
        {
            var itemCreatureModelInfo = itemData.Value;
            if (itemCreatureModelInfo.model_id == creatureInfo.model_id)
            {
                AddSkin(itemCreatureModelInfo.id);
            }
        }
    }

    public void AddSkin(long skinId)
    {
        CreatureSkinBean creatureSkinBean = new CreatureSkinBean(skinId);
        listSkinData.Add(creatureSkinBean);
    }

    /// <summary>
    /// 获取皮肤列表
    /// </summary>
    public string[] GetSkinArray(int showType = 0)
    {
        List<string> listSkin = new List<string>();
        for (int i = 0; i < listSkinData.Count; i++)
        {
            var itemSkinData = listSkinData[i];
            var itemSkinInfo = CreatureModelInfoCfg.GetItemData(itemSkinData.skinId);
            if (itemSkinInfo == null)
            {
                LogUtil.LogError($"获取CreatureModelInfoCfg数据失败 没有找到ID_{itemSkinData.skinId} 的数据");
            }
            else
            {
                if(itemSkinInfo.show_type == showType)
                {
                    listSkin.Add(itemSkinInfo.res_name);
                }
            }
        }
        return listSkin.ToArray();
    }

    /// <summary>
    /// 获取生物信息
    /// </summary>
    /// <returns></returns>
    public CreatureInfoBean GetCreatureInfo()
    {
        if (creatureInfo == null || creatureInfo.id != id)
        {
            creatureInfo = CreatureInfoCfg.GetItemData(id);
        }
        return creatureInfo;
    }

    /// <summary>
    /// 获取生命值
    /// </summary>
    /// <returns></returns>
    public int GetLife()
    {
        var creatureInfo = GetCreatureInfo();
        return creatureInfo.life;
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
    /// 获取创建的魔力
    /// </summary>
    public int GetCreateMagic()
    {
        var creatureInfo = CreatureInfoCfg.GetItemData(id);
        return creatureInfo.create_magic;
    }
}
