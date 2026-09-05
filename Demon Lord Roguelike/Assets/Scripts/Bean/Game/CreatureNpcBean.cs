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
            if (npcId == 0)
                return null;
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

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器专用：注入 npcInfo 编辑副本（供 NPC创建编辑器窗口预览未保存/编辑中的 NPC，
    /// 绕过 npcInfo getter 的 Cfg 懒加载——未保存的新 id 会 LogError 返回 null，已有 id 会返回缓存原值而非编辑副本）
    /// </summary>
    public void SetNpcInfoForEditor(NpcInfoBean info)
    {
        _npcInfo = info;
    }
#endif

}
