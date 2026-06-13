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
    /// 通过献祭升级一级: 扣除本级所需经验(跨级累加制,余量保留)、等级+1,并返回本次升级获得的可分配属性加点数。
    /// <para>属性加点不再自动加成, 改由玩家在 UICreatureAddAttribute 界面手动分配(见献祭升级成功流程)。</para>
    /// <para>仅在献祭成功时调用;调用前应已通过 CanUpLevel() 校验。</para>
    /// </summary>
    /// <returns>本次升级获得的属性加点数(取下一级 LevelInfo.attribute_point, 未配置默认 1);已满级返回 0</returns>
    public int UpLevelForSacrifice()
    {
        var nextLevelInfo = GetNextLevelInfo();
        //已满级,不再升级
        if (nextLevelInfo == null || nextLevelInfo.id == 0)
            return 0;
        //扣除升到下一级所需经验,余量保留用于后续升级
        long needExp = long.Parse(nextLevelInfo.level_exp);
        levelExp -= needExp;
        if (levelExp < 0)
            levelExp = 0;
        //等级+1
        level++;
        //本次升级获得的加点数(配置驱动, 未配置默认 1)
        int attributePoint = nextLevelInfo.attribute_point;
        if (attributePoint <= 0)
            attributePoint = 1;
        return attributePoint;
    }
    #endregion

    #region 创建随机属性
    /// <summary>
    /// 创建生物时随机属性加点(孕育扭蛋/新建存档初始魔物共用)
    /// <para>总点数取自 UserLimmitBean.gashaponRandomAttributeNum, 配置异常(小于等于0)时兜底不加点。</para>
    /// </summary>
    /// <param name="userData">用户数据(新建存档时 GameDataHandler 尚未 SetUserData, 需显式传入新建的 UserDataBean)</param>
    public void RandomAttributeForCreate(UserDataBean userData)
    {
        if (userData == null)
            return;
        int randomAttributeNum = userData.GetUserLimmitData().gashaponRandomAttributeNum;
        if (randomAttributeNum <= 0)
            return;
        creatureAttribute.AddRandomAttributeForCreate(randomAttributeNum);
    }

    /// <summary>
    /// 创建生物时固定属性加点(新建存档赠送的初始魔物专用)
    /// <para>总点数取自 UserLimmitBean.gashaponRandomAttributeNum(与孕育扭蛋一致), 但不再随机分配,</para>
    /// <para>而是把全部点数固定堆到指定属性上(单点增量见 CreatureUtil.GetAttributePointAddValue)。</para>
    /// <para>配置异常(小于等于0)时兜底不加点。</para>
    /// </summary>
    /// <param name="userData">用户数据(新建存档时 GameDataHandler 尚未 SetUserData, 需显式传入新建的 UserDataBean)</param>
    /// <param name="attributeType">要固定堆叠的属性类型</param>
    public void FixedAttributeForCreate(UserDataBean userData, CreatureAttributeTypeEnum attributeType)
    {
        if (userData == null)
            return;
        int attributeNum = userData.GetUserLimmitData().gashaponRandomAttributeNum;
        if (attributeNum <= 0)
            return;
        creatureAttribute.AddFixedAttributeForCreate(attributeNum, attributeType);
    }
    #endregion

    #region 战斗属性
    /// <summary>
    /// 获取基础属性（含魔力上限 MP 分支）
    /// <para>GetAttribute 位于自动生成的 CreatureBean.cs，其属性 switch 缺少 MP 分支且不可直接修改；</para>
    /// <para>需要取魔力上限(MP)时统一走此方法，其余属性原样透传 GetAttribute。</para>
    /// <para>MP/MPF 仅在战斗中有效：MP=魔王魔力上限（创建魔物消耗魔力），MPF=每秒恢复的魔力值。</para>
    /// </summary>
    /// <param name="creatureAttributeType">属性类型</param>
    /// <returns>属性值</returns>
    public float GetAttributeWithMP(CreatureAttributeTypeEnum creatureAttributeType)
    {
        //非MP属性直接透传原有逻辑
        if (creatureAttributeType != CreatureAttributeTypeEnum.MP)
            return GetAttribute(creatureAttributeType);
        //MP魔力上限：与 GetAttribute 内其他属性相同的计算管线（基础值→角色加点→装备→BUFF）
        var npcInfo = creatureNpcData?.npcInfo;
        float targetData = npcInfo != null ? npcInfo.MP : creatureInfo.MP;
        //获取角色属性加成
        targetData += creatureAttribute.GetAttribute(CreatureAttributeTypeEnum.MP);
        //获取装备属性
        targetData += GetEquipAttribute(CreatureAttributeTypeEnum.MP);
        //获取BUFF改变后的属性加成
        targetData = GetBuffChangeAttribute(CreatureAttributeTypeEnum.MP, targetData);
        return targetData;
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
