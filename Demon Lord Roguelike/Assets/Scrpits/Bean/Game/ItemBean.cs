using System.Collections.Generic;
using UnityEngine;

public partial class ItemBean
{
    //道具ID
    public long itemId;
    //道具数量
    public int itemNum;
    //道具品质
    public int rarity;
    //装备属性加成
    public Dictionary<CreatureAttributeTypeEnum, float> dicAttribute = new Dictionary<CreatureAttributeTypeEnum, float>();

    public ItemBean()
    {

    }

    public ItemBean(long itemId, int itemNum = 1, int rarity = 1)
    {
        InitData(itemId, itemNum, rarity);
    }


    public ItemBean(ItemIdEnum itemId, int itemNum = 1, int rarity = 1)
    {
        InitData((long)itemId, itemNum, rarity);
    }

    public void InitData(long itemId, int itemNum = 1, int rarity = 1)
    {
        this.itemId = itemId;
        this.itemNum = itemNum;
        this.rarity = rarity;
    }

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
                    AddAttribute(CreatureAttributeTypeEnum.HP, 10);
                    break;
                case 2:
                    AddAttribute(CreatureAttributeTypeEnum.DR, 10);
                    break;
                case 3:
                    AddAttribute(CreatureAttributeTypeEnum.ATK, 1);
                    break;
                case 4:
                    AddAttribute(CreatureAttributeTypeEnum.ASPD, 1);
                    break;
            }
        }
    }

    /// <summary>
    /// 增加属性
    /// </summary>
    public void AddAttribute(CreatureAttributeTypeEnum attributeType, float addNum)
    {
        float tempValue = 0;
        if (dicAttribute.TryGetValue(attributeType, out float targetValue))
        {
            tempValue = targetValue + addNum;
        }
        else
        {
            dicAttribute.Add(attributeType, 0);
        }
        if (tempValue < 0)
        {
            tempValue = 0;
        }
        dicAttribute[attributeType] = tempValue;
    }

    /// <summary>
    /// 获取道具类型
    /// </summary>
    public ItemTypeEnum GetItemType()
    {
        return itemsInfo.GetItemType();
    }

    /// <summary>
    /// 获取道具稀有度
    /// </summary>
    /// <returns></returns>
    public RarityEnum GetRarityEnum()
    {
        return (RarityEnum)rarity;
    }

    /// <summary>
    /// 获取属性
    /// </summary>
    public float GetAttribute(CreatureAttributeTypeEnum attributeType)
    {
        if (dicAttribute.TryGetValue(attributeType, out float targetValue))
        {
            return targetValue;
        }
        else
        {
            return 0;
        }
    }
}
