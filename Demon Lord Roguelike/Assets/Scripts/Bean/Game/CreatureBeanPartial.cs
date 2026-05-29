using System;
using System.Collections.Generic;

public partial class CreatureBean
{
    [Newtonsoft.Json.JsonIgnore]
    [NonSerialized]
    public int order;//排序

    [Newtonsoft.Json.JsonIgnore]
    [NonSerialized]
    public float RCDTimeUpdate = 0;    //生物复活更新时间

    [Newtonsoft.Json.JsonIgnore]
    [NonSerialized]
    protected CreatureInfoBean _creatureInfo;

    [Newtonsoft.Json.JsonIgnore]
    public CreatureInfoBean creatureInfo
    {
        get
        {
            if(_creatureInfo == null)
            {
                _creatureInfo = CreatureInfoCfg.GetItemData(creatureId);
                if(_creatureInfo == null)
                {
                    LogUtil.LogError($"获取CreatureInfoBean失败 id_{creatureId}");
                }
            }
            return _creatureInfo;
        }
    }

    [Newtonsoft.Json.JsonIgnore]
    [NonSerialized]
    protected CreatureModelBean _creatureModel;

    [Newtonsoft.Json.JsonIgnore]
    public CreatureModelBean creatureModel
    {
        get
        {
            if (_creatureModel == null)
            {
                _creatureModel = CreatureModelCfg.GetItemData(creatureInfo.model_id);
            }
            return _creatureModel;
        }
    }

    /// <summary>
    /// 清理临时数据
    /// <para>会清空皮肤/装备/等级/星级/稀有度等所有数据，仅用于"一次性 Bean 入池复用"（复用时会通过 SetData 重建）。</para>
    /// <para>切勿对与玩家存档共享引用的阵容生物 Bean 调用此方法，否则会清空其皮肤数据导致 Spine 无法显示，请改用 <see cref="ClearFightTempData"/>。</para>
    /// </summary>
    public void ClearTempData()
    {
        order = 0;
        RCDTimeUpdate = 0;
        creatureState = CreatureStateEnum.Idle;
        level = 0;
        levelExp = 0;
        starLevel = 0;
        rarity = 0;
        relationship = 0;
        creatureNpcData = null;
        dicSkinData.Clear();
        dicEquipItemData.Clear();
        dicRarityBuff.Clear();
    }

    /// <summary>
    /// 清理战斗运行时临时状态
    /// <para>仅重置战斗期间产生的运行时状态（排序、复活计时、生物状态），</para>
    /// <para>保留皮肤(dicSkinData)/装备/等级/星级/稀有度等持久核心数据。</para>
    /// <para>用于战斗结束后还原与玩家存档共享引用的阵容生物 Bean，使其回到可用的待机状态。</para>
    /// </summary>
    public void ClearFightTempData()
    {
        order = 0;
        RCDTimeUpdate = 0;
        creatureState = CreatureStateEnum.Idle;
    }

}
