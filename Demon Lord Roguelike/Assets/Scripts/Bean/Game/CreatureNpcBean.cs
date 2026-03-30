using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CreatureNpcBean
{
    public long npcId;

    [Newtonsoft.Json.JsonIgnore]
    [NonSerialized]
    protected NpcInfoBean _npcInfo;

    [Newtonsoft.Json.JsonIgnore]
    public NpcInfoBean npcInfo
    {
        get
        {
            if (_npcInfo == null)
            {
                _npcInfo = NpcInfoCfg.GetItemData(npcId);
                if (_npcInfo == null)
                {
                    LogUtil.LogError($"获取NPC数据失败 npcID_{npcId}");
                }
            }
            return _npcInfo;
        }
    }

    public CreatureNpcBean(long npcId)
    {
        this.npcId = npcId;
    }

}
