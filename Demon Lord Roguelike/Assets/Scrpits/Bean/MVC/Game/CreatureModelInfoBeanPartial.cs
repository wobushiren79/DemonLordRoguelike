using System;
using System.Collections.Generic;
public partial class CreatureModelInfoBean
{
}
public partial class CreatureModelInfoCfg
{
    public Dictionary<int, Dictionary<CreatureModelPartTypeEnum, List<CreatureModelInfoBean>>> dicDetailsModelInfo;

    public List<CreatureModelInfoBean> GetData(int modelId, CreatureModelPartTypeEnum modelType)
    {
        if (dicDetailsModelInfo == null || dicDetailsModelInfo.Count == 0)
        {
            InitDetailsData();
        }
        if (dicDetailsModelInfo.TryGetValue(modelId, out Dictionary<CreatureModelPartTypeEnum, List<CreatureModelInfoBean>> valueModel))
        {
            if (valueModel.TryGetValue(modelType, out List<CreatureModelInfoBean> creatureModelInfoList))
            {
                return creatureModelInfoList;
            }
            else
            {
                LogUtil.LogError($"CreatureModelInfoCfg 没有找到 modelId_{modelId} CreatureModelPartTypeEnum_{modelType.ToString()}的数据");
                return null;
            }
        }
        else
        {
            LogUtil.LogError($"CreatureModelInfoCfg 没有找到 modelId_{modelId}的数据");
            return null;
        }
    }

    /// <summary>
    /// 初始化数据
    /// </summary>
    public void InitDetailsData()
    {
        dicDetailsModelInfo = new Dictionary<int, Dictionary<CreatureModelPartTypeEnum, List<CreatureModelInfoBean>>>();
        var allData = GetAllData();
        foreach (var itemData in allData)
        {
            var itemDataDetails = itemData.Value;
            CreatureModelPartTypeEnum partType = (CreatureModelPartTypeEnum)itemDataDetails.part_type;
            if (dicDetailsModelInfo.TryGetValue(itemDataDetails.model_id, out Dictionary<CreatureModelPartTypeEnum, List<CreatureModelInfoBean>> valueModel))
            {
                if (valueModel.TryGetValue(partType, out List<CreatureModelInfoBean> valueModelInfoList))
                {
                    valueModelInfoList.Add(itemDataDetails);
                }
                else
                {
                    List<CreatureModelInfoBean> newListData = new List<CreatureModelInfoBean>()
                    {
                        itemDataDetails
                    };
                    valueModel.Add(partType, newListData);
                }
            }
            else
            {
                List<CreatureModelInfoBean> newListData = new List<CreatureModelInfoBean>()
                {
                     itemDataDetails
                };
                Dictionary<CreatureModelPartTypeEnum, List<CreatureModelInfoBean>> newData = new Dictionary<CreatureModelPartTypeEnum, List<CreatureModelInfoBean>>()
                {
                    {partType, newListData}
                };
                dicDetailsModelInfo.Add(itemDataDetails.model_id, newData);
            }
        }
    }
}
