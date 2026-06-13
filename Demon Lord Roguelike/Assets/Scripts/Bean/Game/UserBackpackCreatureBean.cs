using System;
using System.Collections.Generic;

/// <summary>
/// 用户背包生物数据存档Bean
/// 仅作为「背包生物列表」的容器，从 UserData 主存档拆分为同槽目录下的独立存档 UserBackpackCreature_{slot}，
/// 由 UserDataService 在加载/保存时注入与落盘；生物的增删/查询等编排逻辑仍由 UserDataBean 统一维护
/// </summary>
[Serializable]
public class UserBackpackCreatureBean
{
    #region 数据字段
    /// <summary>
    /// 背包里的所有生物
    /// </summary>
    public List<CreatureBean> listBackpackCreature = new List<CreatureBean>();
    #endregion
}
