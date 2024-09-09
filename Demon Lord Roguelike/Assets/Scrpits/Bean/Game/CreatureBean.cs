using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Cinemachine.CinemachineOrbitalTransposer;

[Serializable]
public class CreatureBean
{
    //生物ID
    public string creatureId;
    //ID
    public long id;
    //生物名字
    public string creatureName;
    //等级
    public int level;
    //稀有度
    public int rarity;

    //所有的皮肤数据
    public Dictionary<CreatureSkinTypeEnum, CreatureSkinBean> dicSkinData = new Dictionary<CreatureSkinTypeEnum, CreatureSkinBean>();

    [Newtonsoft.Json.JsonIgnore]
    [NonSerialized]
    protected CreatureInfoBean _creatureInfo;

    [Newtonsoft.Json.JsonIgnore]
    public CreatureInfoBean creatureInfo
    {
        get
        {
            if(_creatureInfo == null)
            {
                _creatureInfo = CreatureInfoCfg.GetItemData(id);
            }
            return _creatureInfo;
        }
    }

    [Newtonsoft.Json.JsonIgnore]
    [NonSerialized]
    protected CreatureModelBean _creatureModel;

    [Newtonsoft.Json.JsonIgnore]
    public CreatureModelBean creatureModel
    {
        get
        {
            if (_creatureModel == null)
            {
                _creatureModel = CreatureModelCfg.GetItemData(creatureInfo.model_id);
            }
            return _creatureModel;
        }
    }


    public CreatureBean(long id)
    {
        this.id = id;
    }

    /// <summary>
    /// 清空皮肤
    /// </summary>
    public void ClearSkin(bool isAddBase = true)
    {
        dicSkinData.Clear();
        if(isAddBase)
            AddSkinForBase();
    }

    /// <summary>
    /// 添加基础皮肤
    /// </summary>
    public void AddSkinForBase(int skinId = -1)
    {
        //添加基础皮肤
        var listBaseSkin = CreatureModelInfoCfg.GetData(creatureInfo.model_id, CreatureSkinTypeEnum.Base);
        if (!listBaseSkin.IsNull())
        {
            if (skinId == -1)
            {
                AddSkin(listBaseSkin[0].id);
            }
            else
            {
                for (int i = 0; i < listBaseSkin.Count; i++)
                {
                   var itemSkinData= listBaseSkin[i];
                    if (itemSkinData.id == skinId)
                    {
                        AddSkin(skinId);
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 添加所有皮肤 用于测试
    /// </summary>
    public void AddAllSkin()
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
    /// <returns></returns>
    public int GetLife()
    {
        return creatureInfo.life;
    }

    /// <summary>
    /// 获取攻击
    /// </summary>
    /// <returns></returns>
    public int GetAttDamage()
    {
        return creatureInfo.att_damage;
    }

    /// <summary>
    /// 获取移动速度
    /// </summary>
    /// <returns></returns>
    public float GetMoveSpeed()
    {
        return creatureInfo.speed_move;
    }

    /// <summary>
    /// 获取攻击CD
    /// </summary>
    /// <returns></returns>
    public float GetAttCD()
    {
        return creatureInfo.att_cd;
    }

    /// <summary>
    /// 获取攻击动画出手时间
    /// </summary>
    /// <returns></returns>
    public float GetAttAnimCastTime()
    {
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
