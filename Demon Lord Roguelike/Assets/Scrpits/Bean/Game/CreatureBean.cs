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
    //所有的皮肤数据
    public List<CreatureSkinBean> listSkinData = new List<CreatureSkinBean>();

    public CreatureBean(long id)
    {
        this.id = id;
    }

    public void AddSkin(int skinId)
    {
        CreatureSkinBean creatureSkinBean = new CreatureSkinBean(skinId);
        listSkinData.Add(creatureSkinBean);
    }

    /// <summary>
    /// 获取皮肤列表
    /// </summary>
    /// <returns></returns>
    public string[] GetSkinArray()
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
                listSkin.Add(itemSkinInfo.res_name);
            }
        }
        return listSkin.ToArray();
    }
}
