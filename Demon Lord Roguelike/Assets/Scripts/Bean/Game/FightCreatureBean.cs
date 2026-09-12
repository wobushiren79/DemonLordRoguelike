using System;
using System.Collections;
using System.Collections.Generic;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using UnityEngine;

[Serializable]
public class FightCreatureBean
{
    public CreatureFightTypeEnum creatureFightType;//生物战斗类型
    public CreatureBean creatureData;    //生物数据
    public Vector3Int positionCreate;//生成位置（用于防守生物）
    public Vector3 positionDead;//死亡位置
    public int roadIndex;//当前道路（用于进攻生物）

    public int HPCurrent;//当前生命值
    public int DRCurrent;//当前护甲值
    public float MPCurrent;//当前魔力值（仅战斗中有效 魔王核心创建魔物消耗的魔力 float用于累积每帧的MPF恢复量）
    public bool isInvincible;//是否无敌（免疫一切 UnderAttack 伤害；当前由SSR稀有度BUFF「真男人」驱动）
    public string damageTransferApplierId;//伤害转移代受者UUID（非空时受到的 UnderAttack 伤害在结算前改道给该生物承受；当前由大盾战士BOSS「援护护盾」BUFF驱动）
    public BuffBaseEntity damageTransferBuff;//伤害转移来源BUFF实例（回指用于受击反馈，避免受击时扫描BUFF列表）
    public bool isPositionReleased;//占位已释放（冲锋生物冲锋开始后置位：占位/删除扫描跳过本生物，原格可立即放第二只；重生落点改取死亡位置）
    public Dictionary<CreatureAttributeTypeEnum, float> dicAttribute = new Dictionary<CreatureAttributeTypeEnum, float>(); //属性

    //强度倍率(用于征服模式普通进攻敌人按关卡递增强度; 默认1=不变, 对 HP/护甲(DR)/攻击力(ATK) 最终值整体相乘)
    public float intensityRate = 1f;

    public Color colorBodyCurrent;//当前身体颜色

    public FightCreatureBean(long id, CreatureFightTypeEnum creatureFightType)
    {
        creatureData =  CreatureHandler.Instance.manager.GetCreatureData(id);
        SetData(creatureData, creatureFightType);
    }

    public FightCreatureBean(NpcInfoBean npcInfo, CreatureFightTypeEnum creatureFightType)
    {
        creatureData = CreatureHandler.Instance.manager.GetCreatureData(npcInfo);
        SetData(creatureData, creatureFightType);
    }

    public FightCreatureBean(CreatureBean creatureData, CreatureFightTypeEnum creatureFightType)
    {
        SetData(creatureData, creatureFightType);
    }

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(CreatureBean creatureData, CreatureFightTypeEnum creatureFightType)
    {
        this.creatureFightType = creatureFightType;
        this.creatureData = creatureData;
        ResetData();
    }

    /// <summary>
    /// 重置数据
    /// </summary>
    public void ResetData()
    {
        //刷新一下基础属性
        RefreshBaseAttribute();
        HPCurrent = (int)GetAttribute(CreatureAttributeTypeEnum.HP);
        DRCurrent = (int)GetAttribute(CreatureAttributeTypeEnum.DR);
        //战斗开始时魔力默认满值
        MPCurrent = GetAttribute(CreatureAttributeTypeEnum.MP);
        //重置无敌状态（BUFF会在生成挂接时按需重新开启）
        isInvincible = false;
        //重置伤害转移标记（对象池复用防残留；护盾BUFF生效时才会重新写入）
        damageTransferApplierId = null;
        damageTransferBuff = null;
        //重置占位释放标记（对象池复用防残留；冲锋意图进入时才会重新置位）
        isPositionReleased = false;
    }

    //modifier 收集缓冲区（复用以避免每次 Refresh 都重新分配）
    private readonly List<AttributeModifier> modifierBuffer = new List<AttributeModifier>(32);

    /// <summary>
    /// 初始化基础属性
    /// 采用 channel pipeline 计算：先把所有BUFF的 modifier 收集到 buffer，再按属性叠加 Flat → PercentAdd → PercentMul → Override
    /// 叠序无关，跟主流方案一致
    /// </summary>
    public void RefreshBaseAttribute(Action actionForComplete = null)
    {
        //先还原基础数据
        dicAttribute.Clear();
        var listCreatureAttributeType = EnumExtension.GetEnumValue<CreatureAttributeTypeEnum>();
        for (int i = 0; i < listCreatureAttributeType.Count; i++)
        {
            var creatureAttributeType = listCreatureAttributeType[i];
            //CreatureBean.GetAttribute 已含 MP/CMP 等全部分支（基础值→角色加点→装备→BUFF）
            var attributeData = creatureData.GetAttribute(creatureAttributeType);
            dicAttribute.Add(creatureAttributeType, attributeData);
        }
        //还原基础身体颜色
        colorBodyCurrent = Color.white;

        //收集所有BUFF的 modifier，同时处理身体颜色等非属性副作用
        modifierBuffer.Clear();
        var creatureBuffs = BuffHandler.Instance.manager.GetFightCreatureBuffsActivie(creatureData.creatureUUId);
        CollectFromBuffList(creatureBuffs);
        //深渊馈赠buff加成
        var abyssalBlessingBuffs = BuffHandler.Instance.manager.dicAbyssalBlessingBuffsActivie;
        if (!abyssalBlessingBuffs.List.IsNull())
        {
            for (int i = 0; i < abyssalBlessingBuffs.List.Count; i++)
            {
                CollectFromBuffList(abyssalBlessingBuffs.List[i]);
            }
        }

        //按通道叠加 modifier 计算最终属性
        for (int i = 0; i < listCreatureAttributeType.Count; i++)
        {
            var attr = listCreatureAttributeType[i];
            dicAttribute[attr] = ModifierPipeline.Apply(dicAttribute[attr], attr, modifierBuffer);
        }

        //强度倍率：征服模式进攻敌人(含BOSS)按关卡递增强度，对最终的 HP/护甲(DR)/攻击力(ATK) 整体相乘
        if (intensityRate != 1f)
        {
            dicAttribute[CreatureAttributeTypeEnum.HP] *= intensityRate;
            dicAttribute[CreatureAttributeTypeEnum.DR] *= intensityRate;
            dicAttribute[CreatureAttributeTypeEnum.ATK] *= intensityRate;
        }

        actionForComplete?.Invoke();
    }

    /// <summary>
    /// 把一组BUFF的 modifier 与副作用（身体颜色）汇入当前刷新
    /// 仅在BUFF的 trigger_creature_type 匹配本生物时生效
    /// </summary>
    private void CollectFromBuffList(List<BuffBaseEntity> buffs)
    {
        if (buffs.IsNull()) return;
        for (int i = 0; i < buffs.Count; i++)
        {
            var buff = buffs[i];
            if (buff == null) continue;
            var buffEntityData = buff.buffEntityData;
            if (buffEntityData == null || !buffEntityData.isValid) continue;
            var buffInfo = buffEntityData.GetBuffInfo();
            //生物类型过滤：BUFF 配置指定了 trigger_creature_type 时只对匹配的生物生效
            CreatureFightTypeEnum triggerCreatureType = buffInfo.GetTriggerCreatureType();
            if (triggerCreatureType != CreatureFightTypeEnum.None && triggerCreatureType != creatureFightType)
            {
                continue;
            }
            //单体定向深渊馈赠：仅对选取时被随机锁定的那一只防守生物生效(实现「随机一只防守生物属性翻倍」)
            if (buff is IBuffSingleTarget singleTargetBuff && singleTargetBuff.SingleTargetCreatureUUId != creatureData.creatureUUId)
            {
                continue;
            }
            //属性 modifier 来源
            if (buff is IAttributeModifierSource src)
            {
                src.CollectModifiers(modifierBuffer);
            }
            //身体颜色副作用（保留原逻辑，最后一个生效）
            if (!buffInfo.color_body.IsNull())
            {
                colorBodyCurrent = buff.GetChangeBodyColor(buffEntityData);
            }
        }
    }

    #region 获取
    /// <summary>
    /// 获取敌人的战斗生物类型
    /// </summary>
    /// <returns></returns>
    public CreatureFightTypeEnum GetCreatureFightTypeForEnemy()
    {
        switch (creatureFightType)
        {
            case CreatureFightTypeEnum.FightAttack:
                return CreatureFightTypeEnum.FightDefense;
            case CreatureFightTypeEnum.FightDefenseCore:
                return CreatureFightTypeEnum.FightAttack;
            case CreatureFightTypeEnum.FightDefense:
                return CreatureFightTypeEnum.FightAttack;
            default:
                return creatureFightType;
        }
    }

    /// <summary>
    ///  获取敌人的战斗生物层级
    /// </summary>
    /// <returns></returns>
    public int GetCreatureLayer(bool isEnemy)
    {
        CreatureFightTypeEnum targetTypeEnum = isEnemy ? GetCreatureFightTypeForEnemy() : creatureFightType; 
        switch (targetTypeEnum)
        {
            case CreatureFightTypeEnum.FightAttack:
                return LayerInfo.CreatureAtt;
            case CreatureFightTypeEnum.FightDefense:
                return LayerInfo.CreatureDef;
            case CreatureFightTypeEnum.FightDefenseCore:
                return 0;
        }
        return 0;
    }

    /// <summary>
    /// 获取基础属性
    /// </summary>
    public float GetAttribute(CreatureAttributeTypeEnum attributeType)
    {
        if (dicAttribute.TryGetValue(attributeType, out float targetValue))
        {
            return targetValue;
        }
        return 0;
    }

    /// <summary>
    /// 获取身体颜色
    /// </summary>
    /// <returns></returns>
    public Color GetBodyColor()
    {
        return colorBodyCurrent;
    }

    /// <summary>每点攻速的攻击频率收益（线性：频率倍率 = 1 + 攻速×本值）</summary>
    public const float ASPD_FREQUENCY_RATE_PER_POINT = 0.05f;
    /// <summary>每累积多少点攻速转化为 1 次额外攻击（0.2 秒窗口连发，由攻击意图调度）</summary>
    public const float ASPD_POINT_PER_EXTRA_ATTACK = 20f;
    /// <summary>攻击频率倍率下限（负攻速兜底，防 debuff 把频率压到 0 导致永不攻击）</summary>
    public const float ASPD_FREQUENCY_RATE_MIN = 0.25f;
    /// <summary>攻击时间下限（秒，防配置时间为 0 时攻击循环每帧空转）</summary>
    public const float ATTACK_TIME_MIN = 0.02f;

    /// <summary>
    /// 获取攻击时间数据：攻速(ASPD)线性提升攻击频率（每点 +5%），每满 20 点转化为 1 次额外攻击，余数继续压缩准备/出手时间。
    /// <para>时间倍率 = 攻击次数 ÷ 频率倍率——使「段内时间压缩」与「跨段多打一发」在 20/40/60… 边界处连续（边界上时间回到基础值、攻击次数 +1），
    /// 攻击频率恒 = 基础频率 × (1 + 0.05×攻速)，无断崖、无上限；时间倍率 ≥ 0.5（正攻速时），动画最多 2 倍速。</para>
    /// <para>负攻速（debuff）：不减少攻击次数，仅按频率倍率（下限 <see cref="ASPD_FREQUENCY_RATE_MIN"/>）等比拉长时间。</para>
    /// </summary>
    /// <param name="timeAttackPre">攻击准备时间</param>
    /// <param name="timeAttacking">攻击出手时间</param>
    /// <param name="attackTimes">本轮攻击循环发出的攻击次数（含主攻击；额外攻击由攻击意图在 0.2 秒窗口内连发）</param>
    public void GetAttackTimeData(out float timeAttackPre, out float timeAttacking, out int attackTimes)
    {
        float attributeASPD = GetAttribute(CreatureAttributeTypeEnum.ASPD);

        float attackPreTime = creatureData.GetAttackPreTime();
        float attackAnimTime = creatureData.GetAttackAnimTime();

        //攻击次数：每满 20 点攻速 +1 次；频率倍率：每点 +5%
        attackTimes = 1 + Mathf.Max(0, Mathf.FloorToInt(attributeASPD / ASPD_POINT_PER_EXTRA_ATTACK));
        float frequencyRate = Mathf.Max(1 + ASPD_FREQUENCY_RATE_PER_POINT * attributeASPD, ASPD_FREQUENCY_RATE_MIN);
        float timeRate = attackTimes / frequencyRate;

        timeAttackPre = Mathf.Max(attackPreTime * timeRate, ATTACK_TIME_MIN);
        timeAttacking = Mathf.Max(attackAnimTime * timeRate, ATTACK_TIME_MIN);

        //根据BUFF改变攻击时间
        BuffHandler.Instance.ChangeAttackTimeDataForBuff(creatureData.creatureUUId, ref timeAttackPre, ref timeAttacking);
    }
    #endregion

    #region 改变护甲和生命
    public void ChangeDRAndHP(int changeValue,
        out int curDR, out int curHP,
        out int changeDRReal, out int changeHPReal,
        bool isRefreshAttribute = true)
    {
        changeDRReal = 0;
        changeHPReal = 0;

        //先改变护甲
        ChangeDR(changeValue, out int leftDR, out changeDRReal, isRefreshAttribute: false);
        //如果真实改变的护甲值==改变的值（没有冗余）
        if (changeValue == changeDRReal)
        {

        }
        //如果真实改变的护甲值!=改变的值有冗余（有冗余 需要再改变生命值）
        else
        {
            int changeValue2 = changeValue - changeDRReal;
            ChangeHP(changeValue2, out int leftHP, out changeHPReal, isRefreshAttribute: false);
        }
        curDR = DRCurrent;
        curHP = HPCurrent;

        //刷新一下基础属性
        if (isRefreshAttribute)
        {
            RefreshBaseAttribute();
        }
    }


    /// <summary>
    /// 改变护甲
    /// </summary>
    /// <param name="ChangeDR">改变值</param>
    /// <param name="leftDR">剩余值</param>
    /// <param name="changeDRReal">真实改变值</param>
    public void ChangeDR
    (
        int ChangeDR,
        out int leftDR, out int changeDRReal,
        bool isRefreshAttribute = true
    )
    {
        DRCurrent += ChangeDR;
        changeDRReal = ChangeDR;
        if (DRCurrent < 0)
        {
            changeDRReal = ChangeDR - DRCurrent;
            DRCurrent = 0;
        }
        int DRMAX = (int)GetAttribute(CreatureAttributeTypeEnum.DR);
        if (DRCurrent > DRMAX)
        {
            changeDRReal = ChangeDR - (DRCurrent - DRMAX);
            DRCurrent = DRMAX;
        }
        leftDR = DRCurrent;
        //刷新一下基础属性
        if (isRefreshAttribute)
        {
            RefreshBaseAttribute();
        }
    }


    /// <summary>
    /// 改变生命值
    /// </summary>
    /// <param name="ChangeHP">改变值</param>
    /// <param name="leftHP">剩余值</param>
    /// <param name="changeHPReal">真实改变值</param>
    public void ChangeHP
    (
        int ChangeHP,
        out int leftHP, out int changeHPReal,
        bool isRefreshAttribute = true
    )
    {
        HPCurrent += ChangeHP;
        changeHPReal = ChangeHP;
        if (HPCurrent < 0)
        {
            changeHPReal = ChangeHP - HPCurrent;
            HPCurrent = 0;
        }
        int HPMax = (int)GetAttribute(CreatureAttributeTypeEnum.HP);
        if (HPCurrent > HPMax)
        {
            changeHPReal = ChangeHP - (HPCurrent - HPMax);
            HPCurrent = HPMax;
        }
        leftHP = HPCurrent;
        //刷新一下基础属性
        if (isRefreshAttribute)
        {
            RefreshBaseAttribute();
        }
    }
    #endregion

    #region 改变魔力
    /// <summary>
    /// 改变魔力值（仅战斗中有效 魔王核心专用）
    /// <para>正值=恢复魔力（如每秒恢复MPF点），负值=消耗魔力（如创建魔物扣除 CMP）。</para>
    /// <para>结果会被限制在 [0, 魔力上限MP] 区间内。</para>
    /// </summary>
    /// <param name="changeMP">改变值</param>
    /// <param name="leftMP">剩余值</param>
    /// <param name="changeMPReal">真实改变值</param>
    public void ChangeMP(float changeMP, out float leftMP, out float changeMPReal)
    {
        MPCurrent += changeMP;
        changeMPReal = changeMP;
        if (MPCurrent < 0)
        {
            changeMPReal = changeMP - MPCurrent;
            MPCurrent = 0;
        }
        float MPMax = GetAttribute(CreatureAttributeTypeEnum.MP);
        if (MPCurrent > MPMax)
        {
            changeMPReal = changeMP - (MPCurrent - MPMax);
            MPCurrent = MPMax;
        }
        leftMP = MPCurrent;
    }
    #endregion
}
