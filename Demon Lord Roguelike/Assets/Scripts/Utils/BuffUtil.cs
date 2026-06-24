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
        Dictionary<long, MaterialBuffStat> dicMaterialBuff = GetMaterialBuffStats(newRarity, materials);
        //先按素材加成概率 roll(每个 id 10%×数量),命中则继承该BUFF
        if (dicMaterialBuff != null && dicMaterialBuff.Count > 0)
        {
            float roll = Random.Range(0f, 100f);
            float acc = 0f;
            foreach (var item in dicMaterialBuff)
            {
                acc += 10f * item.Value.count;
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
    private static Dictionary<long, MaterialBuffStat> GetMaterialBuffStats(RarityEnum rarityEnum, List<CreatureBean> materials)
    {
        if (materials == null || materials.Count == 0)
        {
            return null;
        }
        Dictionary<long, MaterialBuffStat> dicStat = null;
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
                dicStat = new Dictionary<long, MaterialBuffStat>();
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
                stat = new MaterialBuffStat
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

    /// <summary>
    /// 素材BUFF命中统计:同一 buff id 在素材里出现的数量,及其数值/百分比下限。
    /// </summary>
    private struct MaterialBuffStat
    {
        public int count;
        public float floorValue;
        public float floorValueRate;
    }
    #endregion
}
