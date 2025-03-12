using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FightCreatureBean
{
    public CreatureBean creatureData;    //生物数据
    public Vector3Int positionCreate;//生成位置

    public int lifeCurrent;//当前生命值
    public int lifeMax;//最大生命值

    public int armorCurrent;//当前护甲值
    public int armorMax;//最大护甲值

    public float moveSpeedCurrent;//当前移动速度
    public Color colorBodyCurrent;//当前身体颜色
    public List<FightBuffBean> listBuff;//所有的buff

    public FightCreatureBean(int creatureId)
    {
        creatureData = new CreatureBean(creatureId);
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
        var creatureInfo = creatureData.creatureInfo;
        lifeCurrent = creatureInfo.life;
        lifeMax = creatureInfo.life;
        listBuff = new List<FightBuffBean>();

        InitBaseAttribute();
    }

    /// <summary>
    /// 初始化基础属性
    /// </summary>
    public void InitBaseAttribute(Action actionForComplete = null)
    {
        moveSpeedCurrent = creatureData.GetMoveSpeed();
        colorBodyCurrent = Color.white;
        if (!listBuff.IsNull())
        {
            float changeMoveSpeed = 0;
            float changeRateMoveSpeed = 0;
            for (int i = 0; i < listBuff.Count; i++)
            {
                FightBuffBean itemBuff = listBuff[i];
                var buffEntity = itemBuff.GetBuffEntity();
                //设置速度相关
                var targetChangeData = buffEntity.GetChangeDataForMoveSpeed(itemBuff);
                changeMoveSpeed += targetChangeData.change;
                changeRateMoveSpeed += targetChangeData.changeRate;
                //设置身体颜色
                if (!itemBuff.fightBuffInfo.color_body.IsNull())
                {
                    colorBodyCurrent = itemBuff.fightBuffInfo.GetBodyColor();
                }
            }
            moveSpeedCurrent += changeMoveSpeed;
            if (moveSpeedCurrent < 0)
                moveSpeedCurrent = 0;
            moveSpeedCurrent *= 1 + changeRateMoveSpeed;
            if (moveSpeedCurrent < 0)
                moveSpeedCurrent = 0;
        }
        if (moveSpeedCurrent < 0)
            moveSpeedCurrent = 0;
        actionForComplete?.Invoke();
    }

    /// <summary>
    /// 获取角色移动速度
    /// </summary>
    /// <returns></returns>
    public float GetMoveSpeed()
    {
        return moveSpeedCurrent;
    }

    /// <summary>
    /// 获取身体颜色
    /// </summary>
    /// <returns></returns>
    public Color GetBodyColor()
    {
        return colorBodyCurrent;
    }


    public void ChangeArmorAndLife(int changeValue,
        out int curArmor, out int curLife,
        out int changeArmorReal, out int changeLifeReal)
    {
        changeArmorReal = 0;
        changeLifeReal = 0;

        //先改变护甲
        ChangeArmor(changeValue, out int leftArmor, out changeArmorReal);
        //如果真实改变的护甲值==改变的值（没有冗余）
        if (changeValue == changeArmorReal)
        {

        }
        //如果真实改变的护甲值!=改变的值有冗余（有冗余 需要再改变生命值）
        else
        {
            int changeValue2 = changeValue - changeArmorReal;
            ChangeLife(changeValue2, out int leftLife, out changeLifeReal);
        }
        curArmor = armorCurrent;
        curLife = lifeCurrent;
    }


    /// <summary>
    /// 改变护甲
    /// </summary>
    /// <param name="changeArmor">改变值</param>
    /// <param name="leftArmor">剩余值</param>
    /// <param name="changeArmorReal">真实改变值</param>
    public void ChangeArmor(int changeArmor, out int leftArmor, out int changeArmorReal)
    {
        armorCurrent += changeArmor;
        changeArmorReal = changeArmor;
        if (armorCurrent < 0)
        {
            changeArmorReal = changeArmor - armorCurrent;
            armorCurrent = 0;
        }
        if (armorCurrent > armorMax)
        {
            changeArmorReal = changeArmor - (armorCurrent - armorMax);
            armorCurrent = armorMax;
        }
        leftArmor = armorCurrent;
    }


    /// <summary>
    /// 改变生命值
    /// </summary>
    /// <param name="changeLife">改变值</param>
    /// <param name="leftLife">剩余值</param>
    /// <param name="changeLifeReal">真实改变值</param>
    public void ChangeLife(int changeLife, out int leftLife, out int changeLifeReal)
    {
        lifeCurrent += changeLife;
        changeLifeReal = changeLife;
        if (lifeCurrent < 0)
        {
            changeLifeReal = changeLife - lifeCurrent;
            lifeCurrent = 0;
        }
        if (lifeCurrent > lifeMax)
        {
            changeLifeReal = changeLife - (lifeCurrent - lifeMax);
            lifeCurrent = lifeMax;
        }
        leftLife = lifeCurrent;
    }

    /// <summary>
    /// 添加BUFF
    /// </summary>
    public void AddBuff(List<FightBuffBean> targetBuffs, Action<FightBuffBean> actionForCombineNew = null, Action<FightBuffBean> actionForCombineOld = null, Action actionForComplete = null)
    {
        for (int f = 0; f < targetBuffs.Count; f++)
        {
            var targetBuff = targetBuffs[f];
            bool hasOldBuff = false;
            for (int i = 0; i < listBuff.Count; i++)
            {
                var itemBuff = listBuff[i];
                if (itemBuff.fightBuffStruct.id == targetBuff.fightBuffStruct.id)
                {
                    hasOldBuff = true;
                    itemBuff.triggerNumLeft = targetBuff.triggerNumLeft;
                    actionForCombineOld?.Invoke(itemBuff);
                    break;
                }
            }
            if (!hasOldBuff)
            {
                listBuff.Add(targetBuff);
                actionForCombineNew?.Invoke(targetBuff);

            }
        }
        InitBaseAttribute(actionForComplete);
    }

    /// <summary>
    /// 移除BUFF
    /// </summary>
    public void RemoveBuff(FightBuffBean targetBuff, Action actionForComplete = null)
    {
        if (!listBuff.IsNull())
        {
            listBuff.Remove(targetBuff);
        }
        InitBaseAttribute(actionForComplete);
    }

}
