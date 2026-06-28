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
    /// 进阶规则:每个素材BUFF id 的直接命中概率(百分点)= 本常量 × 该 id 在素材中出现的数量。
    /// 生成(roll累加)与展示(概率列表)同口径,单点化避免两处魔法数 10 不一致。
    /// </summary>
    private const float AscendMaterialBuffRatePerCount = 10f;

    /// <summary>
    /// 进阶规则:生成 newRarity 的一条BUFF。
    /// <para>默认走通用随机;素材魔物在 newRarity 槽位的BUFF会按 buff id 聚合,每个 id 提供 10%×数量 的直接命中概率(decision: 按 buff id 判定同一)。</para>
    /// <para>命中某素材BUFF时,继承其 id 并按下限重随机数值(结果≥素材原数值);未命中则回退通用随机。</para>
    /// <para>newRarity 为 UR/L 等无对应BUFF类型时返回 null(只升稀有度不授BUFF)。</para>
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
        //先按素材加成概率 roll(每个 id 10%×数量),命中则继承该BUFF
        if (dicMaterialBuff != null && dicMaterialBuff.Count > 0)
        {
            float roll = Random.Range(0f, 100f);
            float acc = 0f;
            foreach (var item in dicMaterialBuff)
            {
                acc += AscendMaterialBuffRatePerCount * item.Value.count;
                if (roll < acc)
                {
                    return BuffBean.CreateRandomWithFloor(item.Key, item.Value.floorValue, item.Value.floorValueRate);
                }
            }
        }
        //未命中素材加成:走通用随机
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
    /// 按进阶BUFF生成规则,计算 newRarity 下各BUFF的命中概率(供孵化缸进阶详情实时展示)。
    /// <para>与 <see cref="CreateAscendRarityBuff"/> 同口径:每个素材BUFF id 提供 10%×数量 的直接命中概率;</para>
    /// <para>列表末尾追加一项"随机增益"(buffId=-1)表示剩余概率(100%-素材命中总和,走通用随机)。</para>
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
        //素材命中:每个 buff id 提供 10%×数量 的概率
        float materialTotalRate = 0f;
        Dictionary<long, CreatureAscendMaterialBuffStruct> dicMaterialBuff = GetMaterialBuffStats(newRarity, materials);
        if (dicMaterialBuff != null)
        {
            foreach (var item in dicMaterialBuff)
            {
                float rate = AscendMaterialBuffRatePerCount * item.Value.count;
                materialTotalRate += rate;
                var buffInfo = BuffInfoCfg.GetItemData(item.Key);
                listChance.Add(new CreatureAscendBuffChanceStruct
                {
                    buffId = item.Key,
                    name = buffInfo != null ? buffInfo.name_language : "",
                    rate = rate,
                });
            }
        }
        //兜底"随机增益":剩余概率(钳制到≥0)
        listChance.Add(new CreatureAscendBuffChanceStruct
        {
            buffId = -1,
            name = AscendRandomBuffName,
            rate = Mathf.Max(0f, 100f - materialTotalRate),
        });
        return listChance;
    }
    #endregion
}
