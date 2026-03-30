using System;
using System.Collections.Generic;
public partial class ConversationCouncilorInfoBean
{
    public NpcRelationshipEnum GetRelationship()
    {
        return (NpcRelationshipEnum)relationship;
    }
}
public partial class ConversationCouncilorInfoCfg
{
    public static List<ConversationCouncilorInfoBean> GetDataByRelationship(NpcRelationshipEnum relationship)
    {
        List<ConversationCouncilorInfoBean> list = new List<ConversationCouncilorInfoBean>();
        var arrayData = GetAllArrayData();
        for (int i = 0; i < arrayData.Length; i++)
        {
            ConversationCouncilorInfoBean itemData = arrayData[i];
            if (itemData.GetRelationship() == relationship)
            {
                list.Add(itemData);
            }
        }
        return list;
        
    }
}
