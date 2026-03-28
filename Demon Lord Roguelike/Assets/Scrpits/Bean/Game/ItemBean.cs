using System.Collections.Generic;
using UnityEngine;

public partial class ItemBean
{
    //道具 ID
    public long itemId;
    //道具数量
    public int itemNum;
    //道具品质
    public int rarity;
    //道具使用者类型 0=默认 1=魔王专属
    public int userType;
    //装备属性加成
    public Dictionary<CreatureAttributeTypeEnum, float> dicAttribute = new Dictionary<CreatureAttributeTypeEnum, float>();

    public ItemBean()
    {

    }

    public ItemBean(long itemId, int itemNum = 1, int rarity = 1, int userType = 0)
    {
        InitData(itemId, itemNum, rarity, userType);
    }


    public ItemBean(ItemIdEnum itemId, int itemNum = 1, int rarity = 1, int userType = 0)
    {
        InitData((long)itemId, itemNum, rarity, userType);
    }

    public void InitData(long itemId, int itemNum = 1, int rarity = 1, int userType = 0)
    {
        this.itemId = itemId;
        this.itemNum = itemNum;
        this.rarity = rarity;
        this.userType = userType;
    }

    /// <summary>
    /// 创建道具时初始化随机属性
    /// 根据道具品质 (rarity) 决定随机属性的条数，品质越高属性条数越多
    /// 同一属性可被多次选中，数值会累加
    /// userType=1 时固定加成 MSPD 和 MP 属性，否则随机加成 HP/DR/ATK/ASPD
    /// </summary>
    /// <param name="addNum">属性加成的倍率系数</param>
    public void InitRandomAttributeForCreate(int addNum)
    {
        // 属性条数等于道具品质等级
        int rarityNum = rarity;
        for (int i = 0; i < rarityNum; i++)
        {
            // userType=1 时固定加成 MSPD 和 MP，否则随机选择一种属性类型 (1~4)
            if (userType == 1)
            {
                int randomIndex = Random.Range(1, 3);
                switch (randomIndex)
                {
                    case 1:
                        AddAttribute(CreatureAttributeTypeEnum.MSPD, 1 * addNum);
                        break;
                    case 2:
                        AddAttribute(CreatureAttributeTypeEnum.MP, 10 * addNum);
                        break;
                }
            }
            else
            {
                int randomIndex = Random.Range(1, 5);
                switch (randomIndex)
                {
                    case 1:
                        AddAttribute(CreatureAttributeTypeEnum.HP, 10 * addNum);
                        break;
                    case 2:
                        AddAttribute(CreatureAttributeTypeEnum.DR, 10 * addNum);
                        break;
                    case 3:
                        AddAttribute(CreatureAttributeTypeEnum.ATK, 1 * addNum);
                        break;
                    case 4:
                        AddAttribute(CreatureAttributeTypeEnum.ASPD, 1 * addNum);
                        break;
                }
            }
        }
    }

    /// <summary>
    /// 增加属性
    /// </summary>
    public void AddAttribute(CreatureAttributeTypeEnum attributeType, float addNum)
    {
        if (dicAttribute.TryGetValue(attributeType, out float targetValue))
        {
            float tempValue = targetValue + addNum;
            if (tempValue < 0) tempValue = 0;
            dicAttribute[attributeType] = tempValue;
        }
        else
        {
            float tempValue = addNum;
            if (tempValue < 0) tempValue = 0;
            dicAttribute.Add(attributeType, tempValue);
        }
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
    public RarityEnum GetRarityEnum()
    {
        return (RarityEnum)rarity;
    }

    /// <summary>
    /// 获取道具使用者类型
    /// </summary>
    public ItemUserTypeEnum GetUserTypeEnum()
    {
        return (ItemUserTypeEnum)userType;
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
