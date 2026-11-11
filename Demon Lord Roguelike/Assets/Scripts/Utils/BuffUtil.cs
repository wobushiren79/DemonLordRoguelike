using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BUFF 通用工具类:把「稀有度BUFF随机生成」规则统一收口,供扭蛋(GashaponItemBean)与魔物进阶(UICreatureVat)共用。
/// </summary>
public static class BuffUtil
{
    #region 稀有度BUFF通用生成
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
    /// 通用规则:随机生成一条该稀有度的BUFF(扭蛋口径,数值按 isRandom 整数闭区间随机)。
    /// 无对应BUFF类型或该类型下无BUFF配置时返回 null。
    /// </summary>
    /// <param name="rarityEnum">目标稀有度</param>
    /// <returns>随机BUFF运行时数据;无可用BUFF返回 null</returns>
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
    #endregion

    #region 进阶BUFF生成
    /// <summary>
    /// 获取当前进阶「随机增益」权重:默认1,「随机增益缩减」研究(UnlockEnum.CreatureVatRandomReduce)每级-0.1,满级10=0。
    /// 生成(roll)与展示(概率列表)同口径,单点化避免两处规则不一致。
    /// </summary>
    /// <returns>随机增益权重(0~1)</returns>
    private static float GetAscendRandomWeight()
    {
        int reduceLevel = GameDataHandler.Instance.manager.GetUserData().GetUserUnlockData().GetUnlockCreatureVatRandomReduceLevel();
        return UserUnlockBean.GetCreatureVatRandomWeightForLevel(reduceLevel);
    }

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
    /// 进阶规则:生成 newRarity 的一条BUFF(权重占比制)。
    /// <para>素材魔物在 newRarity 槽位的BUFF按 buff id 聚合(decision: 按 buff id 判定同一),每份素材权重1;「随机增益」默认占1份权重,经「随机增益缩减」研究每级-0.1(满级0)。</para>
    /// <para>各素材id命中概率=数量/(素材总份数+随机权重);命中继承该id并按下限重随机数值(结果≥素材原数值),落空(随机区间)回退通用随机。</para>
    /// <para>无素材BUFF时始终走通用随机(随机是唯一来源,不受缩减研究影响);newRarity 为 UR/L 等无对应BUFF类型时返回 null(只升稀有度不授BUFF)。</para>
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
        //无素材BUFF:始终通用随机
        if (materialTotal <= 0)
        {
            return CreateRandomRarityBuff(newRarity);
        }
        //权重×10转整数 roll(素材每份=10,随机权重0~10):规避 float Random.Range 上限含端点与 0.1 步进尾差,保证满级(随机权重0)素材必中
        float randomWeight = GetAscendRandomWeight();
        int randomWeightInt = Mathf.RoundToInt(randomWeight * 10f);
        int roll = Random.Range(0, materialTotal * 10 + randomWeightInt);
        int acc = 0;
        foreach (var item in dicMaterialBuff)
        {
            acc += item.Value.count * 10;
            if (roll < acc)
            {
                return BuffBean.CreateRandomWithFloor(item.Key, item.Value.floorValue, item.Value.floorValueRate);
            }
        }
        //落空=随机区间(randomWeightInt>0):走通用随机
        return CreateRandomRarityBuff(newRarity);
    }

    /// <summary>
    /// 聚合素材在指定稀有度槽位的BUFF统计(按 buff id 归并:出现数量 + 数值下限取各素材最大原值)。
    /// </summary>
    /// <param name="rarityEnum">要统计的稀有度槽位(进阶后的目标稀有度)</param>
    /// <param name="materials">素材魔物列表</param>
    /// <returns>buff id 到命中统计的字典;无任何素材BUFF返回 null</returns>
    private static Dictionary<long, CreatureAscendMaterialBuffStruct> GetMaterialBuffStats(RarityEnum rarityEnum, List<CreatureBean> materials)
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
            if (dicStat == null)
            {
                dicStat = new Dictionary<long, CreatureAscendMaterialBuffStruct>();
            }
            if (dicStat.TryGetValue(matBuff.id, out var stat))
            {
                //同 id 多素材:数量累加,数值下限取最大原值(保证≥任一素材)
                stat.count += 1;
                stat.floorValue = Mathf.Max(stat.floorValue, matBuff.trigger_value);
                stat.floorValueRate = Mathf.Max(stat.floorValueRate, matBuff.trigger_value_rate);
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
    /// 进阶"随机增益"兜底项默认名(暂无对应多语言配置,硬编码与孵化缸item模板文案保持一致)。
    /// </summary>
    public const string AscendRandomBuffName = "随机增益";

    /// <summary>
    /// 按进阶BUFF生成规则(权重占比制),计算 newRarity 下各BUFF的命中概率(供孵化缸进阶详情实时展示)。
    /// <para>与 <see cref="CreateAscendRarityBuff"/> 同口径:每份素材BUFF权重1,随机增益权重=1-0.1×「随机增益缩减」研究等级;各项概率=权重/总权重。</para>
    /// <para>rate 经最大余数法取整为整数百分比且合计恰好100;随机权重0(研究满级)且有素材时不追加随机项;无素材BUFF时仅一项100%随机增益。</para>
    /// <para>newRarity 无对应BUFF类型(N/UR/L)时返回空列表(只升稀有度不授BUFF,不展示任何增益)。</para>
    /// </summary>
    /// <param name="newRarity">进阶后的目标稀有度</param>
    /// <param name="materials">已选素材魔物列表(可空,空时仅返回100%随机增益)</param>
    /// <returns>各BUFF命中概率列表(素材BUFF在前,随机增益兜底在后);无对应类型时为空列表</returns>
    public static List<CreatureAscendBuffChanceStruct> GetCreatureAscendBuffChances(RarityEnum newRarity, List<CreatureBean> materials)
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
        //无素材BUFF:仅100%随机增益(与生成口径一致,不受缩减研究影响)
        if (materialTotal <= 0)
        {
            listChance.Add(new CreatureAscendBuffChanceStruct
            {
                buffId = -1,
                name = AscendRandomBuffName,
                rate = 100,
            });
            return listChance;
        }
        float randomWeight = GetAscendRandomWeight();
        float totalWeight = materialTotal + randomWeight;
        //素材BUFF项:概率=数量/总权重
        foreach (var item in dicMaterialBuff)
        {
            var buffInfo = BuffInfoCfg.GetItemData(item.Key);
            listChance.Add(new CreatureAscendBuffChanceStruct
            {
                buffId = item.Key,
                name = buffInfo != null ? buffInfo.name_language : "",
                rate = item.Value.count / totalWeight * 100f,
                //素材命中该 id 时数值下限被抬高:透传给预览按抬高后下限显示 min~max(与 CreateRandomWithFloor 同口径)
                floorValue = item.Value.floorValue,
                floorValueRate = item.Value.floorValueRate,
            });
        }
        //兜底"随机增益":随机权重>0才追加(研究满级后不再显示0%随机项)
        if (randomWeight > 0)
        {
            listChance.Add(new CreatureAscendBuffChanceStruct
            {
                buffId = -1,
                name = AscendRandomBuffName,
                rate = randomWeight / totalWeight * 100f,
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
}
