using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 奖励数据
/// </summary>
public class RewardSelectBean
{
    //奖励列表
    public List<ItemBean> listReward;

    //当前已经选择的次数
    public int selectNum;
    //可以选择的最大次数
    public int selectNumMax;  
    //道具生成数量
    public int createItemNum;
    //装备生成数量
    public int createEquipNum;
    
    public RewardSelectBean()
    {
        selectNum = 0;
        selectNumMax = 1;
        createItemNum = 3;
        createEquipNum = 1;
    }

    /// <summary>
    /// 初始化数据
    /// </summary>
    public void InitData(FightBean fightData)
    {
        listReward = new List<ItemBean>();
        var userData = GameDataHandler.Instance.manager.GetUserData();
        var userUnlock = userData.GetUserUnlockData();
        var unlockCreatureModelIds = userUnlock.GetUnlockCreatureModelIds();
        //排除没有道具的生物ID
        for (int i = 0; i < unlockCreatureModelIds.Count; i++)
        {
            var creatureModelId = unlockCreatureModelIds[i];
            if (!ItemsInfoCfg.ContainsKeyForCreatureModelId(creatureModelId))
            {
                unlockCreatureModelIds.Remove(creatureModelId);
                i--;
            }
        }
        for (int i = 0; i < createItemNum; i++)
        {               
            //如果还有装备生成数量 优先生成装备
            if (i < createEquipNum)
            {
                CreateItemEquip(fightData, unlockCreatureModelIds);
            }
            //其他生成魔晶
            else
            {
                CreateItemCrystal(fightData);
            }
        }
    }

    /// <summary>
    /// 创建一个装备道具
    /// </summary>
    private void CreateItemEquip(FightBean fightData, List<long> unlockCreatureModelIds)
    {
        var randomCreatureModelId = RandomUtil.GetRandomDataByList(unlockCreatureModelIds);
        List<ItemsInfoBean> listItemsInfo = ItemsInfoCfg.GetDataByCreatureModelId(randomCreatureModelId);
        //如果没有相关道具 生成魔晶（容错）
        if (listItemsInfo == null)
        {
            CreateItemCrystal(fightData);
            return;
        }
        //正常生成装备
        var randomItemInfo = RandomUtil.GetRandomDataByList(listItemsInfo);
        int rarityItem = 1;
        int addAttribute = 0;
        if (fightData != null)
        {
            if (fightData is FightBeanForConquer fightBeanForConquer)
            {
                //设置装备品质
                rarityItem = fightBeanForConquer.fightTypeConquerInfo.reward_equip_rarity;
                addAttribute = fightBeanForConquer.fightTypeConquerInfo.reward_equip_attribute_add;
            }
        }
        ItemBean itemData = new ItemBean(randomItemInfo.id, 1, rarityItem);
        //随机添加属性
        itemData.AddRandomAttributeForCreate(addAttribute);
        listReward.Add(itemData);
    }

    /// <summary>
    /// 创建一个魔晶道具
    /// </summary>
    /// <param name="fightData"></param>
    private void CreateItemCrystal(FightBean fightData)
    {       
        //基础魔晶道具数量
        int itemCrystalNum = 100;
        //战斗数据 获取基础魔晶道具数量
        if (fightData != null)
        {
            if (fightData is FightBeanForConquer fightBeanForConquer)
            {
                itemCrystalNum = fightBeanForConquer.fightTypeConquerInfo.reward_crystal;
            }
        }
        //随机魔晶道具数量
        int itemCrystalNumRandomLimit = itemCrystalNum / 2;
        int randomNum = Random.Range(-itemCrystalNumRandomLimit, itemCrystalNumRandomLimit);
        var itemData = new ItemBean(ItemIdEnum.Crystal, itemCrystalNum + randomNum);
        listReward.Add(itemData);
    }
}
