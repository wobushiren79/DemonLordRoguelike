using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 装备工具类：装备道具的通用创建/处理逻辑
/// </summary>
public static class EquipUtil
{
    #region 核心生成
    /// <summary>
    /// 生成一个装备道具(全项目装备生成的核心入口，各场景封装方法最终都收口到这里)。
    /// 品质=rarity、属性条数=品质、每条加点数=addAttributeOverride(&lt;0 时取稀有度配置 equip_attribute_add)、userType 决定普通/魔王专属属性池。
    /// </summary>
    /// <param name="itemId">道具ID</param>
    /// <param name="rarity">道具品质(RarityEnum: N=1 ~ L=6)</param>
    /// <param name="userType">使用者类型(0=默认, 1=魔王专属), 默认普通</param>
    /// <param name="addAttributeOverride">属性加点数覆盖值; &lt;0 时按稀有度配置 equip_attribute_add 取值</param>
    /// <returns>已随机好属性的装备道具</returns>
    public static ItemBean CreateEquipItem(long itemId, int rarity, int userType = 0, int addAttributeOverride = -1)
    {
        //加点数: 未传覆盖值则由稀有度配置决定
        int addAttribute = addAttributeOverride >= 0 ? addAttributeOverride : RarityInfoCfg.GetItemData(rarity).equip_attribute_add;
        ItemBean itemData = new ItemBean(itemId, 1, rarity, userType);
        //随机添加属性
        itemData.InitRandomAttributeForCreate(addAttribute);
        return itemData;
    }
    #endregion

    #region 场景封装
    /// <summary>
    /// 生成装备-【征服奖励场景】：稀有度/使用者类型/加点数均由奖励生成方算好后传入(与征服通关奖励规则一致)。
    /// </summary>
    /// <param name="itemId">道具ID</param>
    /// <param name="rarity">道具品质(RarityEnum: N=1 ~ L=6)</param>
    /// <param name="userType">使用者类型(0=默认, 1=魔王专属), 默认普通</param>
    /// <param name="addAttributeOverride">属性加点数覆盖值; &lt;0 时按稀有度配置 equip_attribute_add 取值</param>
    /// <returns>已随机好属性的装备道具</returns>
    public static ItemBean CreateEquipItemForReward(long itemId, int rarity, int userType = 0, int addAttributeOverride = -1)
    {
        return CreateEquipItem(itemId, rarity, userType, addAttributeOverride);
    }

    /// <summary>
    /// 生成装备-【NPC随机装备场景】：普通使用者(userType=0, 不出魔王专属)、加点按稀有度配置默认取值。
    /// </summary>
    /// <param name="itemId">道具ID</param>
    /// <param name="rarity">道具品质枚举</param>
    /// <returns>已随机好属性的装备道具</returns>
    public static ItemBean CreateEquipItemForNpc(long itemId, RarityEnum rarity)
    {
        return CreateEquipItem(itemId, (int)rarity, userType: 0);
    }

    /// <summary>
    /// 生成装备-【GM/测试场景】：按「指定道具id+指定稀有度」直接发货(普通使用者、加点按稀有度配置默认取值, 不经任何池/概率)。
    /// </summary>
    /// <param name="itemId">道具ID</param>
    /// <param name="rarity">道具品质(RarityEnum: N=1 ~ L=6)</param>
    /// <returns>已随机好属性的装备道具</returns>
    public static ItemBean CreateEquipItemForTest(long itemId, int rarity)
    {
        return CreateEquipItem(itemId, rarity, userType: 0);
    }
    #endregion
}
