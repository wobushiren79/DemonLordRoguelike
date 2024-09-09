using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Cinemachine.CinemachineOrbitalTransposer;

[Serializable]
public class CreatureBean
{
    //����ID
    public string creatureId;
    //ID
    public long id;
    //��������
    public string creatureName;
    //�ȼ�
    public int level;
    //ϡ�ж�
    public int rarity;

    //���е�Ƥ������
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
    /// ���Ƥ��
    /// </summary>
    public void ClearSkin(bool isAddBase = true)
    {
        dicSkinData.Clear();
        if(isAddBase)
            AddSkinForBase();
    }

    /// <summary>
    /// ��ӻ���Ƥ��
    /// </summary>
    public void AddSkinForBase(int skinId = -1)
    {
        //��ӻ���Ƥ��
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
    /// �������Ƥ�� ���ڲ���
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
    /// ���Ƥ��
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
    /// ��ȡƤ���б�
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
                LogUtil.LogError($"��ȡCreatureModelInfoCfg����ʧ�� û���ҵ�ID_{itemSkinData.skinId} ������");
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
    /// ��ȡ����ֵ
    /// </summary>
    /// <returns></returns>
    public int GetLife()
    {
        return creatureInfo.life;
    }

    /// <summary>
    /// ��ȡ����
    /// </summary>
    /// <returns></returns>
    public int GetAttDamage()
    {
        return creatureInfo.att_damage;
    }

    /// <summary>
    /// ��ȡ�ƶ��ٶ�
    /// </summary>
    /// <returns></returns>
    public float GetMoveSpeed()
    {
        return creatureInfo.speed_move;
    }

    /// <summary>
    /// ��ȡ����CD
    /// </summary>
    /// <returns></returns>
    public float GetAttCD()
    {
        return creatureInfo.att_cd;
    }

    /// <summary>
    /// ��ȡ������������ʱ��
    /// </summary>
    /// <returns></returns>
    public float GetAttAnimCastTime()
    {
        return creatureInfo.att_anim_cast_time;
    }


    /// <summary>
    /// ��ȡ������ħ��
    /// </summary>
    public int GetCreateMagic()
    {
        var creatureInfo = CreatureInfoCfg.GetItemData(id);
        return creatureInfo.create_magic;
    }
}
