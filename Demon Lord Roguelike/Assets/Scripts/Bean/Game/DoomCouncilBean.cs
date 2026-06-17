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
    //议员投票态度(Key: 议员UUID, Value: 态度0~100=投赞成的概率); 只和本场议案绑定, 不随议员/存档持久化
    public Dictionary<string, int> dicCouncilorAttitude = new Dictionary<string, int>();

    public DoomCouncilBean(long doomCouncilBillId)
    {
        this.doomCouncilBillId = doomCouncilBillId;
    }

    /// <summary>
    /// 获取议员投票态度(无记录返回0)
    /// </summary>
    /// <param name="creatureUUId">议员UUID</param>
    /// <returns>态度值(0~100)</returns>
    public int GetCouncilorAttitude(string creatureUUId)
    {
        if (dicCouncilorAttitude.TryGetValue(creatureUUId, out int attitude))
        {
            return attitude;
        }
        return 0;
    }

    /// <summary>
    /// 设置议员投票态度(钳制在0~100)
    /// </summary>
    /// <param name="creatureUUId">议员UUID</param>
    /// <param name="attitude">态度值</param>
    public void SetCouncilorAttitude(string creatureUUId, int attitude)
    {
        dicCouncilorAttitude[creatureUUId] = Mathf.Clamp(attitude, 0, 100);
    }

    /// <summary>
    /// 增加议员投票态度(钳制在0~100)
    /// </summary>
    /// <param name="creatureUUId">议员UUID</param>
    /// <param name="addData">增加的态度值</param>
    /// <returns>增加后的态度值</returns>
    public int AddCouncilorAttitude(string creatureUUId, int addData)
    {
        int attitude = Mathf.Clamp(GetCouncilorAttitude(creatureUUId) + addData, 0, 100);
        dicCouncilorAttitude[creatureUUId] = attitude;
        return attitude;
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