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
}
