using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 奖励选择测试数据
/// 用于测试模式下配置生成的装备和魔晶属性
/// </summary>
[System.Serializable]
public class RewardSelectTestData
{
    //装备品质（默认为N）
    public RarityEnum rarity = RarityEnum.N;
    //增加的属性值（默认为5）
    public int addAttribute = 5;
    //魔晶道具基础数量（默认为100）
    public int crystalNum = 100;
    //装备生成数量（默认为1）
    public int createEquipNum = 1;
    //道具生成数量（默认为3）
    public int createItemNum = 3;
    //可以选择的最大次数（默认为1）
    public int selectNumMax = 1;
    //装备是魔王专属的概率（默认 0.1f = 1/10）
    public float createEquipDemonLordRate = 0.1f;

    public RewardSelectTestData()
    {
        rarity = RarityEnum.N;
        addAttribute = 5;
        crystalNum = 100;
        createEquipNum = 1;
        createItemNum = 3;
        selectNumMax = 1;
        createEquipDemonLordRate = 0.1f;
    }

    public RewardSelectTestData(RarityEnum rarity, int addAttribute, int crystalNum = 100, 
        int createEquipNum = 1, int createItemNum = 3, int selectNumMax = 1, float createEquipDemonLordRate = 0.1f)
    {
        this.rarity = rarity;
        this.addAttribute = addAttribute;
        this.crystalNum = crystalNum;
        this.createEquipNum = createEquipNum;
        this.createItemNum = createItemNum;
        this.selectNumMax = selectNumMax;
        this.createEquipDemonLordRate = createEquipDemonLordRate;
    }
}

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
    //装备是魔王专属的概率（默认 1/10）
    public float createEquipDemonLordRate;

    public RewardSelectBean()
    {
        selectNum = 0;
        selectNumMax = 1;
        createItemNum = 3;
        createEquipNum = 1;
        createEquipDemonLordRate = 0.1f;
    }

    /// <summary>
    /// 初始化数据
    /// </summary>
    /// <param name="fightData">战斗数据，正常游戏时传入</param>
    /// <param name="testData">测试数据，测试模式下传入（当fightData为null时生效）</param>
    public void InitData(FightBean fightData, RewardSelectTestData testData = null)
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

        //测试模式下使用测试数据的配置
        if (fightData == null && testData != null)
        {
            createItemNum = testData.createItemNum;
            createEquipNum = testData.createEquipNum;
            selectNumMax = testData.selectNumMax;
            createEquipDemonLordRate = testData.createEquipDemonLordRate;
        }

        for (int i = 0; i < createItemNum; i++)
        {               
            //如果还有装备生成数量 优先生成装备
            if (i < createEquipNum)
            {
                CreateItemEquip(fightData, unlockCreatureModelIds, testData);
            }
            //其他生成魔晶
            else
            {
                CreateItemCrystal(fightData, testData);
            }
        }
    }

    /// <summary>
    /// 创建一个装备道具
    /// </summary>
    /// <param name="fightData">战斗数据，如果为null则表示测试模式</param>
    /// <param name="unlockCreatureModelIds">已解锁的生物模型ID列表</param>
    /// <param name="testData">测试数据，测试模式下使用</param>
    private void CreateItemEquip(FightBean fightData, List<long> unlockCreatureModelIds, RewardSelectTestData testData = null)
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
        int userType = 0;

        if (fightData != null)
        {
            //正常游戏模式：从战斗数据获取装备品质和属性加成
            if (fightData is FightBeanForConquer fightBeanForConquer)
            {
                //设置装备品质
                rarityItem = fightBeanForConquer.fightTypeConquerInfo.reward_equip_rarity;
                addAttribute = fightBeanForConquer.fightTypeConquerInfo.reward_equip_attribute_add;
            }
            //根据概率决定是否生成魔王专属装备
            if (Random.value < createEquipDemonLordRate)
            {
                userType = (int)ItemUserTypeEnum.DemonLord;
            }
        }
        else if (testData != null)
        {
            //测试模式：使用传入的测试数据
            rarityItem = (int)testData.rarity;
            addAttribute = testData.addAttribute;
            //根据测试数据的概率决定是否生成魔王专属装备
            if (Random.value < testData.createEquipDemonLordRate)
            {
                userType = (int)ItemUserTypeEnum.DemonLord;
            }
        }
        else
        {
            //测试模式（无测试数据）：使用默认固定值
            rarityItem = 1;
            userType = 0;
            addAttribute = 5;
        }

        ItemBean itemData = new ItemBean(randomItemInfo.id, 1, rarityItem, userType);
        //随机添加属性
        itemData.InitRandomAttributeForCreate(addAttribute);
        listReward.Add(itemData);
    }

    /// <summary>
    /// 创建一个魔晶道具
    /// </summary>
    /// <param name="fightData">战斗数据</param>
    /// <param name="testData">测试数据，测试模式下使用</param>
    private void CreateItemCrystal(FightBean fightData, RewardSelectTestData testData = null)
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
        else if (testData != null)
        {
            //测试模式：使用传入的测试数据
            itemCrystalNum = testData.crystalNum;
        }
        //随机魔晶道具数量
        int itemCrystalNumRandomLimit = itemCrystalNum / 2;
        int randomNum = Random.Range(-itemCrystalNumRandomLimit, itemCrystalNumRandomLimit);
        var itemData = new ItemBean(ItemIdEnum.Crystal, itemCrystalNum + randomNum);
        listReward.Add(itemData);
    }
}
