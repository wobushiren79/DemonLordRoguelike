using System;
using System.Collections.Generic;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
public partial class CreatureInfoBean
{
    //spine身体基础部位IDs
    protected List<long> listSpineBaseIds;
    //spine身体可替换部位类型
    protected List<CreatureSkinTypeEnum> listSpineChangeSkinTypes;

    public int GetHP()
    {
        return HP * 10;
    }
    public int GetHPOrigin()
    {
        return HP;
    }
    public int GetDR()
    {
        return DR * 10;
    }
    public int GetDROrigin()
    {
        return DR;
    }
    public CreatureTypeEnum GetCreatureType()
    {
        return (CreatureTypeEnum)creature_type;
    }

    /// <summary>
    /// 获取所有基础部位IDs
    /// </summary>

    public List<long> GetSpineBaseIds()
    {
        if (listSpineBaseIds == null)
        {
            listSpineBaseIds = new List<long>();
            if(!spine_base.IsNull())
            {
                listSpineBaseIds = spine_base.SplitForListLong(',');
            }
        }
        return listSpineBaseIds;
    }

    /// <summary>
    /// 获取所有可替换皮肤类型
    /// </summary>
    public List<CreatureSkinTypeEnum> GetSpineSkinChangeTypes()
    {
        if (listSpineChangeSkinTypes == null)
        {
            listSpineChangeSkinTypes = new List<CreatureSkinTypeEnum>();
            if(!spine_skin_change_type.IsNull())
            {
                listSpineChangeSkinTypes = spine_base.SplitForListEnum<CreatureSkinTypeEnum>(',');
            }
        }
        return listSpineChangeSkinTypes;
    }
}
public partial class CreatureInfoCfg
{
}
