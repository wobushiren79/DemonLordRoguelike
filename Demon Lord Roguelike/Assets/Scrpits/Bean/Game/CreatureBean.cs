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
    //��������
    public string creatureName;
    //�ȼ�
    public int level;
    //���е�Ƥ������
    public List<CreatureSkinBean> listSkinData = new List<CreatureSkinBean>();

    protected CreatureInfoBean creatureInfo;//������Ϣ
    public CreatureBean(long id)
    {
        this.id = id;
    }

    /// <summary>
    /// �������Ƥ�� ���ڲ���
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
    /// ��ȡƤ���б�
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
                LogUtil.LogError($"��ȡCreatureModelInfoCfg����ʧ�� û���ҵ�ID_{itemSkinData.skinId} ������");
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
    /// ��ȡ������Ϣ
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
    /// ��ȡ����ֵ
    /// </summary>
    /// <returns></returns>
    public int GetLife()
    {
        var creatureInfo = GetCreatureInfo();
        return creatureInfo.life;
    }

    /// <summary>
    /// ��ȡ����
    /// </summary>
    /// <returns></returns>
    public int GetAttDamage()
    {
        var creatureInfo = GetCreatureInfo();
        return creatureInfo.att_damage;
    }

    /// <summary>
    /// ��ȡ�ƶ��ٶ�
    /// </summary>
    /// <returns></returns>
    public float GetMoveSpeed()
    {
        var creatureInfo = GetCreatureInfo();
        return creatureInfo.speed_move;
    }

    /// <summary>
    /// ��ȡ����CD
    /// </summary>
    /// <returns></returns>
    public float GetAttCD()
    {
        var creatureInfo = GetCreatureInfo();
        return creatureInfo.att_cd;
    }

    /// <summary>
    /// ��ȡ������������ʱ��
    /// </summary>
    /// <returns></returns>
    public float GetAttAnimCastTime()
    {
        var creatureInfo = GetCreatureInfo();
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
