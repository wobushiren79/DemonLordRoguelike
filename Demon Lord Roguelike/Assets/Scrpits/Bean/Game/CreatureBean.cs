using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Cinemachine.CinemachineOrbitalTransposer;

[Serializable]
public partial class CreatureBean
{
    //生物ID
    public string creatureId;
    //ID
    public long id;
    //生物名字
    public string creatureName;
    //等级
    public int level;
    //星级
    public int starLevel;
    //稀有度
    public int rarity;

    //所有的皮肤数据
    public Dictionary<CreatureSkinTypeEnum, CreatureSkinBean> dicSkinData = new Dictionary<CreatureSkinTypeEnum, CreatureSkinBean>();

    public CreatureBean(long id)
    {
        this.id = id;
        this.creatureId = SystemUtil.GetUUID(SystemUtil.UUIDTypeEnum.N);
    }

    /// <summary>
    /// 清空皮肤
    /// </summary>
    public void ClearSkin(bool isAddBase = true)
    {
        dicSkinData.Clear();
        if (isAddBase)
            AddSkinForBase();
    }

    /// <summary>
    /// 添加基础皮肤
    /// </summary>
    public void AddSkinForBase()
    {
        //添加基础皮肤
        List<long> listSpineBaseIds = creatureInfo.GetSpineBaseIds();
        if (!listSpineBaseIds.IsNull())
        {
            listSpineBaseIds.ForEach((index, itemData) =>
            {
                AddSkin(itemData);
            });
        }
        //容错处理 如果没有设置数据 默认添加第一个基础皮肤
        else
        {
            var listBaseSkin = CreatureModelInfoCfg.GetData(creatureInfo.model_id, CreatureSkinTypeEnum.Base);
            if (!listBaseSkin.IsNull())
            {
                AddSkin(listBaseSkin[0].id);
            }
        }
    }

    /// <summary>
    /// 添加所有皮肤 用于测试
    /// </summary>
    public void AddTestSkin()
    {
        dicSkinData.Clear();
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

    /// <summary>
    /// 添加皮肤
    /// </summary>
    public void AddSkin(long skinId)
    {
        var modelDetailsInfo = CreatureModelInfoCfg.GetItemData(skinId);
        CreatureSkinTypeEnum targetSkinType = (CreatureSkinTypeEnum)modelDetailsInfo.part_type;
        if (dicSkinData.TryGetValue(targetSkinType, out CreatureSkinBean creatureSkin))
        {
            creatureSkin.skinId = skinId;
        }
        else
        {
            CreatureSkinBean creatureSkinBean = new CreatureSkinBean(skinId);
            dicSkinData.Add(targetSkinType, creatureSkinBean);
        }
    }

    /// <summary>
    /// 获取皮肤列表
    /// </summary>
    public string[] GetSkinArray(int showType = 0)
    {
        List<string> listSkin = new List<string>();
        foreach (var itemSkin in dicSkinData)
        {
            var itemSkinData = itemSkin.Value;
            var itemSkinInfo = CreatureModelInfoCfg.GetItemData(itemSkinData.skinId);
            if (itemSkinInfo == null)
            {
                LogUtil.LogError($"获取CreatureModelInfoCfg数据失败 没有找到ID_{itemSkinData.skinId} 的数据");
            }
            else
            {
                if (itemSkinInfo.show_type == showType)
                {
                    listSkin.Add(itemSkinInfo.res_name);
                }
            }
        }
        return listSkin.ToArray();
    }

    /// <summary>
    /// 获取生命值
    /// </summary>
    public int GetHP()
    {
        return creatureInfo.GetHP();
    }
    public int GetHPOrigin()
    {
        return creatureInfo.GetHPOrigin();
    }

    /// <summary>
    /// 获取护甲值
    /// </summary>
    public int GetDR()
    {
        return creatureInfo.GetDR();
    }
    public int GetDROrigin()
    {
        return creatureInfo.GetDROrigin();
    }
    
    /// <summary>
    /// 获取攻击
    /// </summary>
    public int GetATK()
    {
        return creatureInfo.ATK;
    }

    /// <summary>
    /// 获取攻击速度
    /// </summary>
    /// <returns></returns>
    public int GetASPD()
    {
        return creatureInfo.ASPD;
    }

    /// <summary>
    /// 获取移动速度
    /// </summary>
    public float GetMSPD()
    {
        return creatureInfo.MSPD;
    }

    /// <summary>
    /// 获取攻击CD
    /// </summary>
    /// <returns></returns>
    public float GetAttackCD()
    {
        return creatureInfo.attack_cd;
    }

    /// <summary>
    /// 获取攻击动画出手时间
    /// </summary>
    /// <returns></returns>
    public float GetAttackAnimCastTime()
    {
        return creatureInfo.attack_anim_cast_time;
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
