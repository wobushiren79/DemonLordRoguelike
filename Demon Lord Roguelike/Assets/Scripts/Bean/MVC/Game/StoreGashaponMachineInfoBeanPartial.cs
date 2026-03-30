using System;
using System.Collections.Generic;
using NUnit.Framework.Internal;
public partial class StoreGashaponMachineInfoBean
{
    protected List<long> listCreatureId;

    /// <summary>
    /// 获取所有的生物
    /// </summary>
    public List<long> GetCreatureIds()
    {
        if (listCreatureId == null)
        {
            listCreatureId = creature_ids.SplitForListLong(',', '-');
        }
        return listCreatureId;
    }
}
public partial class StoreGashaponMachineInfoCfg
{
}
