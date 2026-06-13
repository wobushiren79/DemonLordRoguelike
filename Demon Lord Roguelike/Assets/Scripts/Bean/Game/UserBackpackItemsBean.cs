using System;
using System.Collections.Generic;

/// <summary>
/// 用户背包道具数据存档Bean
/// 仅作为「背包道具列表」的容器，从 UserData 主存档拆分为同槽目录下的独立存档 UserBackpackItem_{slot}，
/// 由 UserDataService 在加载/保存时注入与落盘；道具的增删/堆叠等编排逻辑仍由 UserDataBean 统一维护
/// </summary>
[Serializable]
public class UserBackpackItemsBean
{
    #region 数据字段
    /// <summary>
    /// 背包里的所有道具
    /// </summary>
    public List<ItemBean> listBackpackItems = new List<ItemBean>();
    #endregion
}
