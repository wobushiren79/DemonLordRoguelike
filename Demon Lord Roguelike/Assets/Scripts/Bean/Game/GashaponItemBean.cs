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
        float successRate = userUnlock.GetUnlockResearchLeveByUnlockEnum(unlockRarityRate);
        //检测是否解锁
        if (userUnlock.CheckIsUnlock(unlockRarity))
        {
            float randomData = Random.Range(0f, 100f);
            if (randomData < successRate)
            {
                return true;
            }
        }
        return false;
    }

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