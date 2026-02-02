using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FightCreatureBean
{
    public CreatureFightTypeEnum creatureFightType;//生物战斗类型
    public CreatureBean creatureData;    //生物数据
    public Vector3Int positionCreate;//生成位置（用于防守生物）
    public int roadIndex;//当前道路（用于进攻生物）

    public int HPCurrent;//当前生命值
    public int DRCurrent;//当前护甲值

    public static List<CreatureAttributeTypeEnum> listCreatureAttributeType = new List<CreatureAttributeTypeEnum>()
    {
        CreatureAttributeTypeEnum.HP,
        CreatureAttributeTypeEnum.DR,
        CreatureAttributeTypeEnum.ATK,
        CreatureAttributeTypeEnum.ASPD,
        CreatureAttributeTypeEnum.MSPD,
        CreatureAttributeTypeEnum.EVA,
        CreatureAttributeTypeEnum.CRT
    };

    public Dictionary<CreatureAttributeTypeEnum, float> dicAttribute = new Dictionary<CreatureAttributeTypeEnum, float>(); //属性

    public Color colorBodyCurrent;//当前身体颜色

    public FightCreatureBean(long id, CreatureFightTypeEnum creatureFightType)
    {
        creatureData = new CreatureBean(id);
        SetData(creatureData, creatureFightType);
    }

    public FightCreatureBean(NpcInfoBean npcInfo, CreatureFightTypeEnum creatureFightType)
    {
        creatureData = new CreatureBean(npcInfo);
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
    }

    /// <summary>
    /// 初始化基础属性
    /// </summary>
    public void RefreshBaseAttribute(Action actionForComplete = null)
    {
        //先还原基础数据
        dicAttribute.Clear();
        for (int i = 0; i < listCreatureAttributeType.Count; i++)
        {
            var creatureAttributeType = listCreatureAttributeType[i];
            var attributeData = creatureData.GetAttribute(creatureAttributeType);
            dicAttribute.Add(creatureAttributeType, attributeData);
        }
        //还原基础身体颜色
        colorBodyCurrent = Color.white;

        //战斗生物buff相关加成
        var creatureBuffs = BuffHandler.Instance.manager.GetFightCreatureBuffsActivie(creatureData.creatureUUId);
        if (!creatureBuffs.IsNull())
        {
            for (int i = 0; i < creatureBuffs.Count; i++)
            {
                BuffBaseEntity buffEntity = creatureBuffs[i];
                SetAttributeBaseForBuff(buffEntity);
            }
        }
        //深渊馈赠buff加成
        var abyssalBlessingBuffs = BuffHandler.Instance.manager.dicAbyssalBlessingBuffsActivie;
        if (!abyssalBlessingBuffs.List.IsNull())
        {
            for (int i = 0; i < abyssalBlessingBuffs.List.Count; i++)
            {
                var itemAbyssalBlessingBuff = abyssalBlessingBuffs.List[i];
                for (int j = 0; j < itemAbyssalBlessingBuff.Count; j++)
                {
                    BuffBaseEntity buffEntity = itemAbyssalBlessingBuff[j];
                    SetAttributeBaseForBuff(buffEntity);
                }
            }
        }
        actionForComplete?.Invoke();
    }

    /// <summary>
    /// 根据BUFF设置属性
    /// </summary>
    protected void SetAttributeBaseForBuff(BuffBaseEntity buffEntity)
    {
        var buffEntityData = buffEntity.buffEntityData;
        var buffInfo = buffEntityData.GetBuffInfo();
        //如果不是全触发 需要判断一下生物类型
        CreatureFightTypeEnum triggerCreatureType = buffInfo.GetTriggerCreatureType();
        if (triggerCreatureType != CreatureFightTypeEnum.None)
        {
            if (triggerCreatureType != creatureFightType)
            {
                return;
            }
        }
        //如果是属性类 
        if (buffEntity is BuffEntityAttribute buffEntityAttribute)
        {
            CreatureAttributeTypeEnum targetAttributeType = buffEntityAttribute.attributeType;
            float targetAttributeData = GetAttribute(targetAttributeType);
            //设置新加成后属性
            targetAttributeData = buffEntityAttribute.ChangeData(targetAttributeType, targetAttributeData);
            dicAttribute[targetAttributeType] = targetAttributeData;
        }
        //设置身体颜色
        if (!buffInfo.color_body.IsNull())
        {
            colorBodyCurrent = buffEntity.GetChangeBodyColor(buffEntityData);
        }
    }

    #region 获取
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

    /// <summary>
    /// 获取攻击时间数据
    /// </summary>
    /// <returns></returns>
    public void GetAttackTimeData(out float timeAttackPre,out float timeAttacking)
    {
        float attributeASPD = GetAttribute(CreatureAttributeTypeEnum.ASPD);
        
        float attackPreTime = creatureData.GetAttackPreTime();
        float attackAnimTime = creatureData.GetAttackAnimTime();

        timeAttackPre =  MathUtil.InterpolationLerp(attributeASPD, 0, 100, attackPreTime, 0.02f);
        timeAttacking =  MathUtil.InterpolationLerp(attributeASPD, 0, 100, attackAnimTime, 0.02f);
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
}
