using System;
using System.Collections.Generic;
public partial class CreatureModelInfoBean
{
    
    public CreatureSkinTypeEnum GetPartType()
    {
        return (CreatureSkinTypeEnum)part_type;
    }
}
public partial class CreatureModelInfoCfg
{
    public static Dictionary<long, Dictionary<CreatureSkinTypeEnum, List<CreatureModelInfoBean>>> dicDetailsModelInfo;

    public static Dictionary<CreatureSkinTypeEnum, List<CreatureModelInfoBean>> GetData(long modelId)
    {
        if (dicDetailsModelInfo == null || dicDetailsModelInfo.Count == 0)
        {
            InitDetailsData();
        }
        if (dicDetailsModelInfo.TryGetValue(modelId, out Dictionary<CreatureSkinTypeEnum, List<CreatureModelInfoBean>> valueModel))
        {
            return valueModel;
        }
        else
        {
            LogUtil.Log($"CreatureModelInfoCfg 没有找到 modelId_{modelId}的数据");
            return null;
        }
    }

    public static List<CreatureModelInfoBean> GetData(long modelId, CreatureSkinTypeEnum modelType)
    {
        Dictionary<CreatureSkinTypeEnum, List<CreatureModelInfoBean>> valueModel = GetData( modelId);
        if (valueModel != null)
        {
            if (valueModel.TryGetValue(modelType, out List<CreatureModelInfoBean> creatureModelInfoList))
            {
                return creatureModelInfoList;
            }
            else
            {
                LogUtil.Log($"CreatureModelInfoCfg 没有找到 modelId_{modelId} CreatureModelPartTypeEnum_{modelType.ToString()}的数据");
                return null;
            }
        }
        return null;
    }
    
    /// <summary>
    /// 初始化数据
    /// </summary>
    public static void InitDetailsData()
    {
        dicDetailsModelInfo = new Dictionary<long, Dictionary<CreatureSkinTypeEnum, List<CreatureModelInfoBean>>>();
        var allData = GetAllData();
        foreach (var itemData in allData)
        {
            var itemDataDetails = itemData.Value;
            CreatureSkinTypeEnum partType = (CreatureSkinTypeEnum)itemDataDetails.part_type;
            if (dicDetailsModelInfo.TryGetValue(itemDataDetails.model_id, out Dictionary<CreatureSkinTypeEnum, List<CreatureModelInfoBean>> valueModel))
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
                Dictionary<CreatureSkinTypeEnum, List<CreatureModelInfoBean>> newData = new Dictionary<CreatureSkinTypeEnum, List<CreatureModelInfoBean>>()
                {
                    {partType, newListData}
                };
                dicDetailsModelInfo.Add(itemDataDetails.model_id, newData);
            }
        }
    }
}
