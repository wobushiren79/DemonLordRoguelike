using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FightCreatureBean
{
    public CreatureBean creatureData;    //生物数据
    public Vector3Int positionCreate;//生成位置（用于防守生物）
    public int roadIndex;//当前道路（用于进攻生物）

    public int HPCurrent;//当前生命值
    public int HPMax;//最大生命值

    public int DRCurrent;//当前护甲值
    public int DRMax;//最大护甲值

    public int ATKCurrent;//当前攻击力
    public float ASPDCurrent;//当前攻击间隔
    public float MSPDCurrent;//当前移动速度
    public float EVACurrent;//当前闪避率
    public float CRTCurrent;//暴击率
    public Color colorBodyCurrent;//当前身体颜色

    public FightCreatureBean(long id)
    {
        creatureData = new CreatureBean(id);
        ResetData();
    }

    public FightCreatureBean(CreatureBean creatureData)
    {
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
        HPCurrent = HPMax;
        DRCurrent = DRMax;
    }

    /// <summary>
    /// 初始化基础属性
    /// </summary>
    public void RefreshBaseAttribute(Action actionForComplete = null)
    {
        //先还原基础数据
        HPMax = creatureData.GetHP();
        DRMax = creatureData.GetDR();

        MSPDCurrent = creatureData.GetMSPD();
        CRTCurrent = creatureData.GetCRT();
        EVACurrent = creatureData.GetEVA();
        ATKCurrent = creatureData.GetATK();
        ASPDCurrent = creatureData.GetASPD();

        colorBodyCurrent = Color.white;

        var creatureBuffs = BuffHandler.Instance.manager.GetCreatureBuffsActivie(creatureData.creatureUUId);
        //生物buff相关加成
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
        BuffEntityBean buffEntityData = buffEntity.buffEntityData;
        //如果不是全触发 需要判断一下生物类型
        CreatureTypeEnum triggerCreatureType = buffEntity.buffEntityData.buffInfo.GetTriggerCreatureType();
        if (triggerCreatureType != CreatureTypeEnum.None)
        {
            if (triggerCreatureType != creatureData.creatureInfo.GetCreatureType())
            {
                return;
            }
        }
        
        if (buffEntity is BuffEntityAttribute buffEntityAttribute)
        { 
            HPMax = (int)buffEntityAttribute.ChangeData(CreatureAttributeTypeEnum.HP, HPMax);
            DRMax = (int)buffEntityAttribute.ChangeData(CreatureAttributeTypeEnum.DR, DRMax);

            ATKCurrent = (int)buffEntityAttribute.ChangeData(CreatureAttributeTypeEnum.ATK, ATKCurrent);
            ASPDCurrent = buffEntityAttribute.ChangeData(CreatureAttributeTypeEnum.ASPD, ASPDCurrent);
            MSPDCurrent = buffEntityAttribute.ChangeData(CreatureAttributeTypeEnum.MSPD, MSPDCurrent);
            CRTCurrent = buffEntityAttribute.ChangeData(CreatureAttributeTypeEnum.CRT, CRTCurrent);
            EVACurrent = buffEntityAttribute.ChangeData(CreatureAttributeTypeEnum.EVA, EVACurrent);
        }
        //设置身体颜色
        if (!buffEntityData.buffInfo.color_body.IsNull())
        {
            colorBodyCurrent = buffEntity.GetChangeBodyColor(buffEntityData);
        }
    }

    /// <summary>
    /// 获取攻击力
    /// </summary>
    /// <returns></returns>
    public int GetATK()
    {
        return ATKCurrent;
    }

    /// <summary>
    /// 获取攻击速度
    /// </summary>
    /// <returns></returns>
    public float GetASPD()
    {
        return ASPDCurrent;
    }

    /// <summary>
    /// 获取角色移动速度
    /// </summary>
    /// <returns></returns>
    public float GetMSPD()
    {
        return MSPDCurrent;
    }

    /// <summary>
    /// 获取闪避概率
    /// </summary>
    /// <returns></returns>
    public float GetEVA()
    {
        return EVACurrent;
    }

    /// <summary>
    /// 获取暴击率
    /// </summary>
    public float GetCRT()
    {
        return CRTCurrent;
    }

    /// <summary>
    /// 获取身体颜色
    /// </summary>
    /// <returns></returns>
    public Color GetBodyColor()
    {
        return colorBodyCurrent;
    }


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
        if (DRCurrent > DRMax)
        {
            changeDRReal = ChangeDR - (DRCurrent - DRMax);
            DRCurrent = DRMax;
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
}
