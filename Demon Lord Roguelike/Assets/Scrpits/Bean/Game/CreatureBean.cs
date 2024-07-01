using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Cinemachine.CinemachineOrbitalTransposer;

[Serializable]
public class CreatureBean
{
    public long id;//ID;

    public int skinBaseId;//ͷ��ID
    public int skinHeadId;//ͷ��ID
    public int skinHatId;//ñ��ID
    public int skinArmorShoulderId;//�粿����ID
    public int skinArmorPantsId;//���ӻ���ID

    public CreatureBean(long id)
    {
        this.id = id;
    }

    /// <summary>
    /// ��ȡƤ���б�
    /// </summary>
    /// <returns></returns>
    public string[] GetSkinArray()
    {
        List<string> listSkin = new List<string>();
        GetAndAddSkin(listSkin, skinBaseId);
        GetAndAddSkin(listSkin, skinHeadId);
        GetAndAddSkin(listSkin, skinHatId);
        GetAndAddSkin(listSkin, skinArmorShoulderId);
        GetAndAddSkin(listSkin, skinArmorPantsId);
        return listSkin.ToArray();
    }

    protected void GetAndAddSkin(List<string> listSkin,int targetId)
    {
        if (targetId != 0)
        {
            var itemInfo = CreatureModelInfoCfg.GetItemData(targetId);
            if (itemInfo == null)
            {
                LogUtil.LogError($"��ȡCreatureModelInfoCfg����ʧ�� û���ҵ�ID_{targetId} ������");
            }
            else
            {
                listSkin.Add(itemInfo.res_name);
            }
        }
    }
}
