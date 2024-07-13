using System;
using System.Collections.Generic;
public partial class CreatureModelInfoBean
{
}
public partial class CreatureModelInfoCfg
{
    public Dictionary<int, Dictionary<CreatureSkinTypeEnum, List<CreatureModelInfoBean>>> dicDetailsModelInfo;

    public List<CreatureModelInfoBean> GetData(int modelId, CreatureSkinTypeEnum modelType)
    {
        if (dicDetailsModelInfo == null || dicDetailsModelInfo.Count == 0)
        {
            InitDetailsData();
        }
        if (dicDetailsModelInfo.TryGetValue(modelId, out Dictionary<CreatureSkinTypeEnum, List<CreatureModelInfoBean>> valueModel))
        {
            if (valueModel.TryGetValue(modelType, out List<CreatureModelInfoBean> creatureModelInfoList))
            {
                return creatureModelInfoList;
            }
            else
            {
                LogUtil.LogError($"CreatureModelInfoCfg û���ҵ� modelId_{modelId} CreatureModelPartTypeEnum_{modelType.ToString()}������");
                return null;
            }
        }
        else
        {
            LogUtil.LogError($"CreatureModelInfoCfg û���ҵ� modelId_{modelId}������");
            return null;
        }
    }

    /// <summary>
    /// ��ʼ������
    /// </summary>
    public void InitDetailsData()
    {
        dicDetailsModelInfo = new Dictionary<int, Dictionary<CreatureSkinTypeEnum, List<CreatureModelInfoBean>>>();
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
