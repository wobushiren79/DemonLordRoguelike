using System.Collections.Generic;
using UnityEngine;

public class DoomCouncilBean
{
    //议案ID
    public long doomCouncilBillId;

    //议会信息
    protected DoomCouncilInfoBean _doomCouncilInfo;
    public DoomCouncilInfoBean doomCouncilInfo
    {
        get
        {
            if (_doomCouncilInfo == null)
            {
                _doomCouncilInfo = DoomCouncilInfoCfg.GetItemData(doomCouncilBillId);
            }
            return _doomCouncilInfo;
        }
    }

    //所有的议员
    public List<CreatureBean> listCouncilor;
    //议员位置
    public Dictionary<string, Vector3> dicCouncilorPosition;

    public DoomCouncilBean(long doomCouncilBillId)
    {
        this.doomCouncilBillId = doomCouncilBillId;
    }

    /// <summary>
    /// 获取议员数据
    /// </summary>
    public CreatureBean GetCouncilor(string creatureUUId)
    {
        if (listCouncilor.IsNull())
        {
            return null;
        }
        for (int i = 0; i < listCouncilor.Count; i++)
        {
            if (listCouncilor[i].creatureUUId == creatureUUId)
            {
                return listCouncilor[i];
            }
        }
        return null;
    }

    /// <summary>
    /// 获取所有议员的NPCID
    /// </summary>
    /// <returns></returns>
    public List<long> GetCouncilorAllNpcId()
    {
        List<long> listNPCId = new List<long>();
        if (listCouncilor.IsNull())
        {
            return listNPCId;
        }
        for (int i = 0; i < listCouncilor.Count; i++)
        {
            var itemCreature = listCouncilor[i];
            var creatureNpcData = itemCreature.GetCreatureNpcData();
            listNPCId.Add(creatureNpcData.npcId);
        }
        return listNPCId;
    }

    /// <summary>
    /// 获取所有议员的位置X
    /// </summary>
    /// <returns></returns>
    public List<float> GetCouncilorAllPositionX()
    {
        List<float> listData = new List<float>();
        for (int i = 0; i < listCouncilor.Count; i++)
        {
            var itemCreature = listCouncilor[i];
            dicCouncilorPosition.TryGetValue(itemCreature.creatureUUId, out Vector3 position);
            listData.Add(position.z);
        } 
        return listData;
    }
}