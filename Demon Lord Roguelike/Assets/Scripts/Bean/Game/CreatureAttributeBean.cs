using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureAttributeBean
{
    //创建时加成属性
    public Dictionary<CreatureAttributeTypeEnum, float> dicAttributeCreate = new Dictionary<CreatureAttributeTypeEnum, float>();
    //升级时加点
    public Dictionary<CreatureAttributeTypeEnum, float> dicAttributeLevelUp = new Dictionary<CreatureAttributeTypeEnum, float>();

    #region  获取属性点
    /// <summary>
    /// 获取属性
    /// </summary>
    public float GetAttributeForLevelUp(CreatureAttributeTypeEnum attributeType)
    {
        return GetAttribute(dicAttributeLevelUp, attributeType);
    }

    /// <summary>
    /// 获取属性
    /// </summary>
    public float GetAttributeForCreate(CreatureAttributeTypeEnum attributeType)
    {
        return GetAttribute(dicAttributeCreate, attributeType);
    }

    /// <summary>
    /// 获取属性
    /// </summary>
    public float GetAttribute(CreatureAttributeTypeEnum attributeType)
    {
        float totalAttribute = GetAttributeForCreate(attributeType) + GetAttributeForLevelUp(attributeType);
        return totalAttribute;
    }

    /// <summary>
    /// 获取属性
    /// </summary>
    public float GetAttribute(Dictionary<CreatureAttributeTypeEnum, float> dic, CreatureAttributeTypeEnum attributeType)
    {
        if (dic.TryGetValue(attributeType, out float targetValue))
        {
            return targetValue;
        }
        else
        {
            return 0;
        }
    }
    #endregion

    #region  增加属性点
    /// <summary>
    /// 增加随机属性
    /// </summary>
    /// <param name="addNum"></param>
    public void AddRandomAttributeForCreate(int addNum)
    {
        for (int i = 0; i < addNum; i++)
        {
            int randomIndex = Random.Range(1, 5);
            switch (randomIndex)
            {
                case 1:
                    AddAttributeForCreate(CreatureAttributeTypeEnum.HP, 10);
                    break;
                case 2:
                    AddAttributeForCreate(CreatureAttributeTypeEnum.DR, 10);
                    break;
                case 3:
                    AddAttributeForCreate(CreatureAttributeTypeEnum.ATK, 1);
                    break;
                case 4:
                    AddAttributeForCreate(CreatureAttributeTypeEnum.ASPD, 1);
                    break;
            }
        }
    }

    /// <summary>
    /// 增加固定属性(创建时把指定点数全部加到同一属性上)
    /// <para>与随机加点共用单点增量(CreatureUtil.GetAttributePointAddValue): HP/DR 每点+10, ATK/ASPD 每点+1。</para>
    /// <para>用于新建存档赠送的初始魔物: 不再随机分配, 而是固定堆某一属性。</para>
    /// </summary>
    /// <param name="addNum">属性点数</param>
    /// <param name="attributeType">要堆叠的属性类型</param>
    public void AddFixedAttributeForCreate(int addNum, CreatureAttributeTypeEnum attributeType)
    {
        float pointAddValue = CreatureUtil.GetAttributePointAddValue(attributeType);
        AddAttributeForCreate(attributeType, pointAddValue * addNum);
    }

    /// <summary>
    /// 增加属性
    /// </summary>
    public void AddAttributeForCreate(CreatureAttributeTypeEnum attributeType, float addNum)
    {
        AddAttribute(dicAttributeCreate, attributeType, addNum);
    }

    /// <summary>
    /// 增加属性
    /// </summary>
    public void AddAttributeForLevelUp(CreatureAttributeTypeEnum attributeType, float addNum)
    {
        AddAttribute(dicAttributeLevelUp, attributeType, addNum);
    }

    /// <summary>
    /// 增加属性
    /// </summary>
    public void AddAttribute(Dictionary<CreatureAttributeTypeEnum, float> dic, CreatureAttributeTypeEnum attributeType, float addNum)
    {
        float tempValue;
        if (dic.TryGetValue(attributeType, out float targetValue))
        {
            tempValue = targetValue + addNum;
        }
        else
        {
            //首次加入该属性时直接以增量为初值(此前误置为0会丢失第一次加点)
            tempValue = addNum;
        }
        if (tempValue < 0)
        {
            tempValue = 0;
        }
        dic[attributeType] = tempValue;
    }
    #endregion
}