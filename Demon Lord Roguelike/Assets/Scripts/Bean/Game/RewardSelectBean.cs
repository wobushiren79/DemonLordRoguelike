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
    #region 数据
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
    #endregion

    #region 初始化（奖励生成入口）
    /// <summary>
    /// 初始化数据（正常通关领奖 / 测试模式）
    /// </summary>
    /// <param name="fightData">战斗数据，正常游戏时传入（征服战斗据其配置生成奖励）</param>
    /// <param name="testData">测试数据，测试模式下传入（当fightData为null时生效）</param>
    public void InitData(FightBean fightData, RewardSelectTestData testData = null)
    {
        //征服战斗的奖励配置(稀有度/魔晶数)来自征服配置表; 其它情况(测试/容错)为null
        FightTypeConquerInfoBean conquerInfo = (fightData as FightBeanForConquer)?.fightTypeConquerInfo;
        //测试模式下使用测试数据的配置
        if (fightData == null && testData != null)
        {
            createItemNum = testData.createItemNum;
            createEquipNum = testData.createEquipNum;
            selectNumMax = testData.selectNumMax;
            createEquipDemonLordRate = testData.createEquipDemonLordRate;
        }
        InitRewardList(conquerInfo, testData);
    }

    /// <summary>
    /// 由征服配置直接初始化奖励（传送门预生成/预览用，与通关领奖同规则）
    /// </summary>
    /// <param name="conquerInfo">征服配置（决定装备稀有度 reward_equip_rarity 与魔晶数 reward_crystal）</param>
    public void InitData(FightTypeConquerInfoBean conquerInfo)
    {
        InitRewardList(conquerInfo, null);
    }

    /// <summary>
    /// 用传送门预生成的基础奖励初始化领奖数据（预览=实领），并在其后按深渊馈赠「奖励多多」追加额外奖励件数（魔晶）
    /// </summary>
    /// <param name="baseReward">传送门预生成并冻结的基础奖励（装备+魔晶）</param>
    /// <param name="conquerInfo">征服配置（用于生成追加的额外魔晶；及无预生成奖励时的容错生成）</param>
    /// <param name="extraItemNum">深渊馈赠累计的额外奖励件数（rewardAddItemNum）</param>
    public void InitDataForReward(List<ItemBean> baseReward, FightTypeConquerInfoBean conquerInfo, int extraItemNum)
    {
        if (baseReward != null && baseReward.Count > 0)
        {
            //基础奖励直接采用预生成列表，保证预览所见即实领
            listReward = new List<ItemBean>(baseReward);
        }
        else
        {
            //容错：无预生成奖励时按配置即时生成基础奖励（等价于原通关领奖逻辑）
            InitData(conquerInfo);
        }
        //深渊馈赠「奖励多多」额外件数：与基础奖励中超出装备数的部分同规则(魔晶)，追加在基础奖励之后
        for (int i = 0; i < extraItemNum; i++)
        {
            CreateItemCrystal(conquerInfo);
        }
    }

    /// <summary>
    /// 由征服配置生成一份奖励物品列表（传送门预生成/预览用，与通关领奖同规则）
    /// </summary>
    /// <param name="conquerInfo">征服配置</param>
    /// <returns>奖励物品列表（装备+魔晶）</returns>
    public static List<ItemBean> CreateRewardListForConquer(FightTypeConquerInfoBean conquerInfo)
    {
        RewardSelectBean rewardSelect = new RewardSelectBean();
        rewardSelect.InitData(conquerInfo);
        return rewardSelect.listReward;
    }
    #endregion

    #region 奖励生成
    /// <summary>
    /// 按当前 createItemNum/createEquipNum 生成奖励列表（前 createEquipNum 个生成装备，其余生成魔晶）
    /// </summary>
    /// <param name="conquerInfo">征服配置（决定装备稀有度与魔晶数；为null则走测试/默认规则）</param>
    /// <param name="testData">测试数据（conquerInfo为null时生效）</param>
    private void InitRewardList(FightTypeConquerInfoBean conquerInfo, RewardSelectTestData testData)
    {
        listReward = new List<ItemBean>();
        List<long> unlockCreatureModelIds = GetUnlockCreatureModelIdsForEquip();
        for (int i = 0; i < createItemNum; i++)
        {
            //如果还有装备生成数量 优先生成装备
            if (i < createEquipNum)
            {
                CreateItemEquip(conquerInfo, unlockCreatureModelIds, testData);
            }
            //其他生成魔晶
            else
            {
                CreateItemCrystal(conquerInfo, testData);
            }
        }
    }

    /// <summary>
    /// 获取征服装备奖励池的"解锁签名"：可用于生成装备的已解锁生物模型数量。
    /// 解锁新魔物掉落后该值变化，用于判定传送门预生成奖励是否需要重新生成（魔物掉落道具需研究解锁）。
    /// </summary>
    public static int GetConquerEquipPoolSign()
    {
        return GetUnlockCreatureModelIdsForEquip().Count;
    }

    /// <summary>
    /// 获取可用于生成装备奖励的已解锁生物模型ID列表（排除没有对应道具的生物）
    /// </summary>
    public static List<long> GetUnlockCreatureModelIdsForEquip()
    {
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
        return unlockCreatureModelIds;
    }

    /// <summary>
    /// 创建一个装备道具
    /// </summary>
    /// <param name="conquerInfo">征服配置（决定装备稀有度；为null则走测试/默认规则）</param>
    /// <param name="unlockCreatureModelIds">已解锁的生物模型ID列表</param>
    /// <param name="testData">测试数据，测试模式下使用</param>
    private void CreateItemEquip(FightTypeConquerInfoBean conquerInfo, List<long> unlockCreatureModelIds, RewardSelectTestData testData = null)
    {
        var randomCreatureModelId = RandomUtil.GetRandomDataByList(unlockCreatureModelIds);
        List<ItemsInfoBean> listItemsInfo = ItemsInfoCfg.GetDataByCreatureModelId(randomCreatureModelId);
        //如果没有相关道具 生成魔晶（容错）
        if (listItemsInfo == null)
        {
            CreateItemCrystal(conquerInfo);
            return;
        }

        //先确定本次装备的目标稀有度/加点数/使用者类型（稀有度过滤依赖目标稀有度，故需先算）
        int rarityItem = 1;
        int addAttribute = 0;
        int userType = 0;

        if (conquerInfo != null)
        {
            //正常游戏模式：征服配置只决定装备稀有度
            rarityItem = conquerInfo.reward_equip_rarity;
            //属性加点数量由稀有度配置表决定
            addAttribute = RarityInfoCfg.GetItemData(rarityItem).equip_attribute_add;
            //根据概率决定是否生成魔王专属装备
            if (Random.value < createEquipDemonLordRate)
            {
                userType = (int)ItemUserTypeEnum.DemonLord;
            }
        }
        else if (testData != null)
        {
            //测试模式：使用传入的测试数据（addAttribute 为测试覆盖值）
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
            //无任何数据：默认 N 级，属性加点取稀有度配置
            rarityItem = 1;
            userType = 0;
            addAttribute = RarityInfoCfg.GetItemData(rarityItem).equip_attribute_add;
        }

        //按道具 reward_rarity 白名单过滤：只保留可在本次目标稀有度产出的道具(空白名单=全稀有度适配)
        List<ItemsInfoBean> listMatchItemsInfo = new List<ItemsInfoBean>();
        for (int i = 0; i < listItemsInfo.Count; i++)
        {
            if (listItemsInfo[i].IsMatchRewardRarity(rarityItem))
                listMatchItemsInfo.Add(listItemsInfo[i]);
        }
        //过滤后无匹配道具 生成魔晶（容错，与"无相关道具"一致）
        if (listMatchItemsInfo.Count == 0)
        {
            CreateItemCrystal(conquerInfo, testData);
            return;
        }
        //从匹配白名单的道具中随机取一件
        var randomItemInfo = RandomUtil.GetRandomDataByList(listMatchItemsInfo);

        //走统一的奖励装备生成逻辑(属性条数=稀有度、加点数由本处已算好的 addAttribute 覆盖)
        ItemBean itemData = CreateEquipItemForReward(randomItemInfo.id, rarityItem, userType, addAttribute);
        listReward.Add(itemData);
    }

    /// <summary>
    /// 生成一个装备奖励道具(征服通关奖励的装备生成单一真实源)。
    /// 与 CreateItemEquip 同规则：品质=rarity、属性条数=品质、每条加点数=addAttributeOverride(未传则取稀有度配置 equip_attribute_add)、userType 决定普通/魔王专属属性池。
    /// 供 GM/测试按「指定道具id+指定稀有度」直接发货(不经解锁生物模型池、不含魔王专属概率)。
    /// </summary>
    /// <param name="itemId">道具ID</param>
    /// <param name="rarity">道具品质(RarityEnum: N=1 ~ L=6)</param>
    /// <param name="userType">使用者类型(0=默认, 1=魔王专属), 默认普通</param>
    /// <param name="addAttributeOverride">属性加点数覆盖值; &lt;0 时按稀有度配置 equip_attribute_add 取值</param>
    /// <returns>已随机好属性的装备道具</returns>
    public static ItemBean CreateEquipItemForReward(long itemId, int rarity, int userType = 0, int addAttributeOverride = -1)
    {
        //加点数: 未传覆盖值则由稀有度配置决定(与征服通关奖励一致)
        int addAttribute = addAttributeOverride >= 0 ? addAttributeOverride : RarityInfoCfg.GetItemData(rarity).equip_attribute_add;
        ItemBean itemData = new ItemBean(itemId, 1, rarity, userType);
        //随机添加属性
        itemData.InitRandomAttributeForCreate(addAttribute);
        return itemData;
    }

    /// <summary>
    /// 创建一个魔晶道具
    /// </summary>
    /// <param name="conquerInfo">征服配置（决定魔晶基础数量；为null则走测试/默认规则）</param>
    /// <param name="testData">测试数据，测试模式下使用</param>
    private void CreateItemCrystal(FightTypeConquerInfoBean conquerInfo, RewardSelectTestData testData = null)
    {
        //基础魔晶道具数量
        int itemCrystalNum = 100;
        //征服配置 获取基础魔晶道具数量
        if (conquerInfo != null)
        {
            itemCrystalNum = conquerInfo.reward_crystal;
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
    #endregion
}
