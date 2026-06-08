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

    //献祭升级保底成功率: 上一次献祭失败时记录为"当次成功率的一半",下一次献祭叠加在祭品成功率之上;献祭成功后清零。
    public float sacrificePityRate;

    #region 等级升级
    /// <summary>
    /// 获取下一级所需的等级配置(达到上限返回 null)
    /// </summary>
    /// <returns>下一级的 LevelInfoBean;若已满级则为 null</returns>
    public LevelInfoBean GetNextLevelInfo()
    {
        return LevelInfoCfg.GetItemData(level + 1);
    }

    /// <summary>
    /// 是否已达到等级上限(没有下一级配置)
    /// </summary>
    /// <returns>已满级返回 true</returns>
    public bool IsMaxLevel()
    {
        var nextLevelInfo = GetNextLevelInfo();
        return nextLevelInfo == null || nextLevelInfo.id == 0;
    }

    /// <summary>
    /// 当前经验是否已满足升到下一级(未满级且 levelExp >= 下一级所需经验)
    /// </summary>
    /// <returns>可升级返回 true</returns>
    public bool CanUpLevel()
    {
        var nextLevelInfo = GetNextLevelInfo();
        if (nextLevelInfo == null || nextLevelInfo.id == 0)
            return false;
        return levelExp >= long.Parse(nextLevelInfo.level_exp);
    }

    /// <summary>
    /// 通过献祭升级一级: 扣除本级所需经验(跨级累加制,余量保留)、等级+1、并按规则加点属性。
    /// <para>仅在献祭成功时调用;调用前应已通过 CanUpLevel() 校验。</para>
    /// </summary>
    public void UpLevelForSacrifice()
    {
        var nextLevelInfo = GetNextLevelInfo();
        //已满级,不再升级
        if (nextLevelInfo == null || nextLevelInfo.id == 0)
            return;
        //扣除升到下一级所需经验,余量保留用于后续升级
        long needExp = long.Parse(nextLevelInfo.level_exp);
        levelExp -= needExp;
        if (levelExp < 0)
            levelExp = 0;
        //等级+1
        level++;
        //每升一级暂加 1 点攻击(后续再优化升级属性成长规则)
        creatureAttribute.AddAttributeForLevelUp(CreatureAttributeTypeEnum.ATK, 1);
    }
    #endregion

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
        sacrificePityRate = 0;
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
