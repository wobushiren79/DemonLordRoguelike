using System;
using System.Collections.Generic;
public partial class NpcRelationshipInfoBean
{
}
public partial class NpcRelationshipInfoCfg
{
    public static NpcRelationshipEnum GetNpcRelationship(int relationshipData)
    {
        var allData = GetAllArrayData();
        for (int i = 0; i < allData.Length; i++)
        {
            var itemData = allData[i];
            if (relationshipData >= itemData.relationship_min && relationshipData <= itemData.relationship_max)
            {
                return (NpcRelationshipEnum)itemData.relationship_type;
            }
        }
        //上下限处理 如果小于0则使用最小 如果没有找到数据则使用最大
        if (relationshipData < 0)
        {
            return NpcRelationshipEnum.Hatred;
        }
        else
        {
            return NpcRelationshipEnum.Infatuation;
        }
    }
}
