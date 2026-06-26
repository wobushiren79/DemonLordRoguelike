using System.Collections.Generic;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using UnityEditor;
using UnityEngine;

public class GashaponItemBean
{
    //生物数据
    public CreatureBean creatureData;
    //是否打开
    public bool isBreak;
    //解锁某稀有度档位后的起始命中概率(%),在此基础上叠加"概率+1%"研究等级
    private const float rarityBaseRate = 10f;

    public GashaponItemBean(long creatureId, GashaponMachineCreatureStruct gashaponMachineCreature)
    {
        isBreak = false;
        creatureData = new CreatureBean(creatureId);
        //随机皮肤
        RandomSkill(gashaponMachineCreature);
        //随机属性
        RandomAttribute();
        //随机稀有度
        RandomRarity();
    }

    /// <summary>
    /// 随机稀有度
    /// </summary>
    public void RandomRarity()
    {
        if (RandomRarityItem(UnlockEnum.GashaponRarityUR, UnlockEnum.GashaponRarityURRate))
        {
            creatureData.rarity = (int)RarityEnum.UR;
            RandomRarityBuff(RarityEnum.R);
            RandomRarityBuff(RarityEnum.SR);
            RandomRarityBuff(RarityEnum.SSR);
            RandomRarityBuff(RarityEnum.UR);
            return;
        }
        if (RandomRarityItem(UnlockEnum.GashaponRaritySSR, UnlockEnum.GashaponRaritySSRRate))
        {
            creatureData.rarity = (int)RarityEnum.SSR;
            RandomRarityBuff(RarityEnum.R);
            RandomRarityBuff(RarityEnum.SR);
            RandomRarityBuff(RarityEnum.SSR);
            return;
        }
        if (RandomRarityItem(UnlockEnum.GashaponRaritySR, UnlockEnum.GashaponRaritySRRate))
        {
            creatureData.rarity = (int)RarityEnum.SR;
            RandomRarityBuff(RarityEnum.R);
            RandomRarityBuff(RarityEnum.SR);
            return;
        }
        if (RandomRarityItem(UnlockEnum.GashaponRarityR, UnlockEnum.GashaponRarityRRate))
        {
            creatureData.rarity = (int)RarityEnum.R;
            RandomRarityBuff(RarityEnum.R);
            return;
        }
        creatureData.rarity = (int)RarityEnum.N;
    }

    /// <summary>
    /// 随机稀有度BUFF(走 BuffUtil 统一的稀有度BUFF通用生成规则,与魔物进阶共用同一口径)
    /// </summary>
    public void RandomRarityBuff(RarityEnum rarityEnum)
    {
        BuffBean buffData = BuffUtil.CreateRandomRarityBuff(rarityEnum);
        if (buffData == null)
        {
            return;
        }
        creatureData.dicRarityBuff.Add(rarityEnum, buffData);
    }

    /// <summary>
    /// 随机稀有度item
    /// </summary>
    private bool RandomRarityItem(UnlockEnum unlockRarity, UnlockEnum unlockRarityRate)
    {
        var userData = GameDataHandler.Instance.manager.GetUserData();
        var userUnlock = userData.GetUserUnlockData();
        //检测是否解锁
        if (userUnlock.CheckIsUnlock(unlockRarity))
        {
            //起始10% + 概率研究等级(每级+1%)
            float successRate = rarityBaseRate + userUnlock.GetUnlockResearchLeveByUnlockEnum(unlockRarityRate);
            float randomData = Random.Range(0f, 100f);
            if (randomData < successRate)
            {
                return true;
            }
        }
        return false;
    }

    #region 稀有度概率(展示用)

    /// <summary>
    /// 计算当前解锁状态下各稀有度的实际抽中概率(把顺序判定 UR→SSR→SR→R→N 换算成真实命中概率)。
    /// 仅返回已解锁的稀有度档位 + 普通(N);普通为剩余补足,列表按 普通→R→SR→SSR→UR 排序,所有概率合计=1。
    /// </summary>
    public static List<KeyValuePair<RarityEnum, float>> GetRarityProbabilityList()
    {
        var userData = GameDataHandler.Instance.manager.GetUserData();
        var userUnlock = userData.GetUserUnlockData();

        //按抽取顺序(高→低)逐级用剩余概率乘以本档 rate,剩余概率归入普通(N)
        float remaining = 1f;
        var listHigh = new List<KeyValuePair<RarityEnum, float>>();
        GetRarityProbabilityItem(userUnlock, RarityEnum.UR, UnlockEnum.GashaponRarityUR, UnlockEnum.GashaponRarityURRate, ref remaining, listHigh);
        GetRarityProbabilityItem(userUnlock, RarityEnum.SSR, UnlockEnum.GashaponRaritySSR, UnlockEnum.GashaponRaritySSRRate, ref remaining, listHigh);
        GetRarityProbabilityItem(userUnlock, RarityEnum.SR, UnlockEnum.GashaponRaritySR, UnlockEnum.GashaponRaritySRRate, ref remaining, listHigh);
        GetRarityProbabilityItem(userUnlock, RarityEnum.R, UnlockEnum.GashaponRarityR, UnlockEnum.GashaponRarityRRate, ref remaining, listHigh);

        //输出顺序:普通(N,始终展示)→ R → SR → SSR → UR(listHigh 为高→低,故倒序追加)
        var listResult = new List<KeyValuePair<RarityEnum, float>>();
        listResult.Add(new KeyValuePair<RarityEnum, float>(RarityEnum.N, remaining));
        for (int i = listHigh.Count - 1; i >= 0; i--)
        {
            listResult.Add(listHigh[i]);
        }
        return listResult;
    }

    /// <summary>
    /// 计算单个稀有度档位的实际抽中概率:未解锁则跳过(不入列表);已解锁则取 剩余概率×本档rate,并从剩余概率中扣除
    /// </summary>
    private static void GetRarityProbabilityItem(UserUnlockBean userUnlock, RarityEnum rarityEnum, UnlockEnum unlockRarity, UnlockEnum unlockRarityRate, ref float remaining, List<KeyValuePair<RarityEnum, float>> listHigh)
    {
        if (!userUnlock.CheckIsUnlock(unlockRarity))
        {
            return;
        }
        //起始10% + 概率研究等级(每级+1%),与 RandomRarityItem 口径一致
        float rate = (rarityBaseRate + userUnlock.GetUnlockResearchLeveByUnlockEnum(unlockRarityRate)) / 100f;
        float probability = remaining * rate;
        remaining -= probability;
        listHigh.Add(new KeyValuePair<RarityEnum, float>(rarityEnum, probability));
    }

    #endregion

    /// <summary>
    /// 随机皮肤
    /// </summary>
    public void RandomSkill(GashaponMachineCreatureStruct gashaponMachineCreature)
    {
        //随机皮肤
        foreach (var item in gashaponMachineCreature.randomCreatureMode)
        {
            var listSkin = item.Value;
            int randomIndex = Random.Range(0, listSkin.Count);
            var randomSkin = listSkin[randomIndex];
            creatureData.AddSkin(randomSkin);
        }
        //添加基础皮肤
        creatureData.AddSkinForBase();
    }

    /// <summary>
    /// 随机属性
    /// </summary>
    public void RandomAttribute()
    {
        //创建时随机属性加点(共用逻辑见 CreatureBean.RandomAttributeForCreate, 点数取自 UserLimmitBean 基础值)
        creatureData.RandomAttributeForCreate(GameDataHandler.Instance.manager.GetUserData());
    }
}