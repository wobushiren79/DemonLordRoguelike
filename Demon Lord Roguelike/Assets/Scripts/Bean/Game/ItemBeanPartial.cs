using System;
using UnityEngine;

public partial class ItemBean
{
    [Newtonsoft.Json.JsonIgnore]
    [NonSerialized]
    protected ItemsInfoBean _itemsInfo;

    /// <summary>
    /// 配置是否已查询过(含失败):失败同样标记,避免排序/遍历等高频访问反复查表并刷错误日志。
    /// 配置缺失通常源于所属Mod未开启,运行期内开关不变(重开Mod需重启游戏),故缓存失败结果是安全的
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [NonSerialized]
    protected bool _isItemsInfoQueried;

    [Newtonsoft.Json.JsonIgnore]
    public ItemsInfoBean itemsInfo
    {
        get
        {
            if (!_isItemsInfoQueried)
            {
                _isItemsInfoQueried = true;
                _itemsInfo = ItemsInfoCfg.GetItemData(itemId);
                if (_itemsInfo == null)
                {
                    LogUtil.LogError($"获取道具数据失败 id_{itemId}(可能来自未开启/已删除的Mod)");
                }
            }
            return _itemsInfo;
        }
    }

    #region 装备资格
    /// <summary>
    /// 判断指定生物是否可装备该道具：在配置级校验(CanEquipItem: 槽位/种族模组/武器类型)之上叠加实例级使用者类型校验(魔王专属仅魔王本体可装备)。
    /// <para>userType 是生成装备时写入的实例字段(见 RewardSelectBean),配置级 CanEquipItem 拿不到,必须由本方法收口。</para>
    /// </summary>
    /// <param name="creatureData">目标生物</param>
    /// <returns>true=可装备</returns>
    public bool CanEquipForCreature(CreatureBean creatureData)
    {
        if (creatureData == null || creatureData.creatureInfo == null || itemsInfo == null)
            return false;
        //魔王专属装备仅魔王本体可装备
        if (GetUserTypeEnum() == ItemUserTypeEnum.DemonLord && !creatureData.IsDemonLord())
            return false;
        return creatureData.creatureInfo.CanEquipItem(itemsInfo);
    }
    #endregion
}
