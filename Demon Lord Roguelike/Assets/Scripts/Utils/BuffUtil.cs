using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 处理稀有度 BUFF 的随机生成、素材继承和预览。
/// </summary>
public static class BuffUtil
{
    #region 稀有度 BUFF 随机生成与追加攻击判定
    /// <summary>
    /// 获取稀有度对应的稀有度BUFF类型。仅 R/SR/SSR 配有对应类型,其余(N/UR/L)返回 None。
    /// </summary>
    /// <param name="rarityEnum">稀有度枚举</param>
    /// <returns>对应的 BuffTypeEnum;无对应类型返回 None</returns>
    public static BuffTypeEnum GetRarityBuffType(RarityEnum rarityEnum)
    {
        switch (rarityEnum)
        {
            case RarityEnum.R: return BuffTypeEnum.CreatureRarityR;
            case RarityEnum.SR: return BuffTypeEnum.CreatureRaritySR;
            case RarityEnum.SSR: return BuffTypeEnum.CreatureRaritySSR;
            default: return BuffTypeEnum.None;
        }
    }

    /// <summary>
    /// 从对应稀有度的完整奖池等概率抽取一条 BUFF，数值按配置的整数闭区间随机。
    /// </summary>
    /// <param name="rarityEnum">目标稀有度</param>
    /// <returns>随机 BUFF，无对应类型或配置时返回 null。</returns>
    public static BuffBean CreateRandomRarityBuff(RarityEnum rarityEnum)
    {
        BuffTypeEnum buffType = GetRarityBuffType(rarityEnum);
        if (buffType == BuffTypeEnum.None)
        {
            return null;
        }
        var listBuffInfo = BuffInfoCfg.GetItemDataByBuffType(buffType);
        if (listBuffInfo == null || listBuffInfo.Count == 0)
        {
            return null;
        }
        int randomIndex = Random.Range(0, listBuffInfo.Count);
        var randomBuffInfo = listBuffInfo[randomIndex];
        return new BuffBean(randomBuffInfo.id, isRandom: true);
    }

    /// <summary>
    /// 判断战斗中魔物能否排队发射 BUFF 追加攻击。冲锋自爆不经过攻击意图，主动自爆出手后死亡。
    /// </summary>
    public static bool CanPerformBuffAttack(CreatureInfoBean creatureInfo)
    {
        if (creatureInfo == null || creatureInfo.attack_mode <= 0 || creatureInfo.IsChargeAttack())
            return false;

        var attackModeInfo = AttackModeInfoCfg.GetItemData(creatureInfo.attack_mode);
        return attackModeInfo != null && attackModeInfo.class_name != nameof(AttackModeExplosion);
    }

    #endregion

    #region 进阶BUFF生成
    /// <summary>
    /// 统计素材BUFF总份数(各 id 数量之和;无素材BUFF返回0)。
    /// </summary>
    /// <param name="dicMaterialBuff">素材BUFF命中统计(GetMaterialBuffStats 结果,可空)</param>
    /// <returns>素材BUFF总份数</returns>
    private static int GetMaterialBuffTotalCount(Dictionary<long, CreatureAscendMaterialBuffStruct> dicMaterialBuff)
    {
        int total = 0;
        if (dicMaterialBuff != null)
        {
            foreach (var item in dicMaterialBuff)
            {
                total += item.Value.count;
            }
        }
        return total;
    }

    /// <summary>
    /// 按新稀有度槽位的可继承素材数量抽取一个 BUFF，重抽数值时以同 ID 的最强素材为保底。
    /// </summary>
    /// <param name="newRarity">进阶后的目标稀有度</param>
    /// <param name="materials">作为素材的魔物列表</param>
    /// <returns>预定授予的BUFF;无可用BUFF返回 null</returns>
    public static BuffBean CreateAscendRarityBuff(RarityEnum newRarity, List<CreatureBean> materials)
    {
        BuffTypeEnum buffType = GetRarityBuffType(newRarity);
        if (buffType == BuffTypeEnum.None)
        {
            return null;
        }
        //聚合素材在 newRarity 槽位的BUFF: buff id -> 命中统计(数量/数值下限)
        Dictionary<long, CreatureAscendMaterialBuffStruct> dicMaterialBuff = GetMaterialBuffStats(newRarity, materials);
        int materialTotal = GetMaterialBuffTotalCount(dicMaterialBuff);
        if (materialTotal <= 0)
        {
            return null;
        }
        int roll = Random.Range(0, materialTotal);
        foreach (var item in dicMaterialBuff)
        {
            roll -= item.Value.count;
            if (roll < 0)
            {
                return BuffBean.CreateRandomWithFloor(item.Key, item.Value.floorValue, item.Value.floorValueRate);
            }
        }
        return null;
    }

    /// <summary>
    /// 按 id 聚合进阶目标可继承的 BUFF 次数和该词条的最强素材数值。
    /// </summary>
    /// <param name="rarityEnum">要统计的稀有度槽位(进阶后的目标稀有度)</param>
    /// <param name="materials">素材魔物列表</param>
    /// <returns>buff id 到命中统计的字典;无任何素材BUFF返回 null</returns>
    private static Dictionary<long, CreatureAscendMaterialBuffStruct> GetMaterialBuffStats(
        RarityEnum rarityEnum, List<CreatureBean> materials)
    {
        if (materials == null || materials.Count == 0)
        {
            return null;
        }
        Dictionary<long, CreatureAscendMaterialBuffStruct> dicStat = null;
        for (int i = 0; i < materials.Count; i++)
        {
            var material = materials[i];
            if (material == null || material.dicRarityBuff == null)
            {
                continue;
            }
            //取素材在该稀有度槽位的BUFF(素材为更高稀有度,通常已填充该槽;缺失则跳过)
            if (!material.dicRarityBuff.TryGetValue(rarityEnum, out var matBuff) || matBuff == null)
            {
                continue;
            }
            var buffInfo = BuffInfoCfg.GetItemData(matBuff.id);
            if (buffInfo == null)
                continue;

            if (dicStat == null)
            {
                dicStat = new Dictionary<long, CreatureAscendMaterialBuffStruct>();
            }
            if (dicStat.TryGetValue(matBuff.id, out var stat))
            {
                stat.count += 1;
                stat.floorValue = Mathf.Max(stat.floorValue, matBuff.trigger_value);
                stat.floorValueRate = BuffBean.UsesLowerRateForStrongerEffect(buffInfo)
                    ? Mathf.Min(stat.floorValueRate, matBuff.trigger_value_rate)
                    : Mathf.Max(stat.floorValueRate, matBuff.trigger_value_rate);
            }
            else
            {
                stat = new CreatureAscendMaterialBuffStruct
                {
                    count = 1,
                    floorValue = matBuff.trigger_value,
                    floorValueRate = matBuff.trigger_value_rate,
                };
            }
            dicStat[matBuff.id] = stat;
        }
        return dicStat;
    }
    #endregion

    #region 进阶BUFF概率展示
    /// <summary>
    /// 计算进阶详情展示的继承概率，每项占比为同 ID 素材数除以可继承素材总数，按最大余数法取整后合计 100%。
    /// </summary>
    /// <param name="newRarity">进阶后的目标稀有度</param>
    /// <param name="materials">已选素材魔物列表，可为空。</param>
    /// <returns>各素材 BUFF 的继承概率，无可继承 BUFF 时返回空列表。</returns>
    public static List<CreatureAscendBuffChanceStruct> GetCreatureAscendBuffChances(
        RarityEnum newRarity, List<CreatureBean> materials)
    {
        List<CreatureAscendBuffChanceStruct> listChance = new List<CreatureAscendBuffChanceStruct>();
        BuffTypeEnum buffType = GetRarityBuffType(newRarity);
        //无对应BUFF类型:不授BUFF,不展示任何增益
        if (buffType == BuffTypeEnum.None)
        {
            return listChance;
        }
        Dictionary<long, CreatureAscendMaterialBuffStruct> dicMaterialBuff = GetMaterialBuffStats(newRarity, materials);
        int materialTotal = GetMaterialBuffTotalCount(dicMaterialBuff);
        if (materialTotal <= 0)
        {
            return listChance;
        }
        foreach (var item in dicMaterialBuff)
        {
            var buffInfo = BuffInfoCfg.GetItemData(item.Key);
            listChance.Add(new CreatureAscendBuffChanceStruct
            {
                buffId = item.Key,
                name = buffInfo != null ? buffInfo.name_language : "",
                rate = (float)item.Value.count / materialTotal * 100f,
                //透传最强素材值，预览和生成共用继承范围。
                floorValue = item.Value.floorValue,
                floorValueRate = item.Value.floorValueRate,
            });
        }
        //整数百分比化(最大余数法):合计恰好100,UI直接显示整数
        RoundChanceRatesToInteger(listChance);
        return listChance;
    }

    /// <summary>
    /// 概率列表整数百分比化(最大余数法):各项先向下取整,差额按小数余数从大到小逐项+1,使合计恰好100;原地修改 rate。
    /// </summary>
    /// <param name="listChance">命中概率列表(rate 为精确百分比)</param>
    private static void RoundChanceRatesToInteger(List<CreatureAscendBuffChanceStruct> listChance)
    {
        int count = listChance.Count;
        if (count == 0)
        {
            return;
        }
        int[] floorRates = new int[count];
        float[] remainders = new float[count];
        int floorSum = 0;
        for (int i = 0; i < count; i++)
        {
            float rate = listChance[i].rate;
            floorRates[i] = Mathf.FloorToInt(rate);
            remainders[i] = rate - floorRates[i];
            floorSum += floorRates[i];
        }
        int remain = 100 - floorSum;
        for (int k = 0; k < remain; k++)
        {
            //选出当前余数最大的项+1(余数相同取靠前者;已补项置-1不再参与)
            int maxIndex = -1;
            for (int i = 0; i < count; i++)
            {
                if (remainders[i] >= 0 && (maxIndex < 0 || remainders[i] > remainders[maxIndex]))
                {
                    maxIndex = i;
                }
            }
            if (maxIndex < 0)
            {
                break;
            }
            floorRates[maxIndex] += 1;
            remainders[maxIndex] = -1f;
        }
        for (int i = 0; i < count; i++)
        {
            var chance = listChance[i];
            chance.rate = floorRates[i];
            listChance[i] = chance;
        }
    }
    #endregion

    #region 词条显示
    /// <summary>
    /// 读取属性词条的第二个属性倍率，卡片和进阶预览使用与战斗相同的配表。
    /// </summary>
    public static bool TryGetSecondaryRateMultiplier(BuffInfoBean buffInfo, out float rateMultiplier)
    {
        rateMultiplier = 0f;
        if (buffInfo.class_entity != nameof(BuffEntityAttributeMulti))
            return false;

        var pairs = buffInfo.GetAttributeMultiPairs();
        if (pairs.Count < 2)
            return false;

        rateMultiplier = pairs[1].rateMultiplier;
        return true;
    }
    #endregion
}
