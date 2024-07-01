using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Cinemachine.CinemachineOrbitalTransposer;

[Serializable]
public class CreatureBean
{
    public long id;//ID;

    public int skinBaseId;//头部ID
    public int skinHeadId;//头部ID
    public int skinHatId;//帽子ID
    public int skinArmorShoulderId;//肩部护甲ID
    public int skinArmorPantsId;//裤子护甲ID

    public CreatureBean(long id)
    {
        this.id = id;
    }

    /// <summary>
    /// 获取皮肤列表
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
                LogUtil.LogError($"获取CreatureModelInfoCfg数据失败 没有找到ID_{targetId} 的数据");
            }
            else
            {
                listSkin.Add(itemInfo.res_name);
            }
        }
    }
}
