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
    /// 随机稀有度BUFF
    /// </summary>
    public void RandomRarityBuff(RarityEnum rarityEnum)
    {
        BuffTypeEnum buffType = BuffTypeEnum.None;
        switch (rarityEnum)
        {
            case RarityEnum.R:
                buffType = BuffTypeEnum.CreatureRarityR;
                break;
            case RarityEnum.SR:
                buffType = BuffTypeEnum.CreatureRaritySR;
                break;
            case RarityEnum.SSR:
                buffType = BuffTypeEnum.CreatureRaritySSR;
                break;
        }
        if (buffType == BuffTypeEnum.None)
        {
            return;
        }
        //通过BUFF类型获取BUFF
        var listBuffInfo = BuffInfoCfg.GetItemDataByBuffType(buffType);
        //随机一个BUFF
        int randomIndex = Random.Range(0, listBuffInfo.Count);
        var randomBuffInfo = listBuffInfo[randomIndex];
        //添加BUFF
        BuffBean buffData = new BuffBean(randomBuffInfo.id, isRandom: true);
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
        int randomAttributeNum = 5;//随机属性点数
        creatureData.creatureAttribute.AddRandomAttributeForCreate(randomAttributeNum);
    }
}