
using System;
using Unity.VisualScripting;
using UnityEngine;

[Serializable]
public class BuffBean
{
    //buffId
    public long id;
    //BUFF创建几率
    public float createRate;
    //触发改变的值
    public float trigger_value;
    //触发改变的值百分比
    public float trigger_value_rate;
    //触发几率 0为必然触发
    public float  trigger_chance;
    //触发次数 0为无限
    public int  trigger_num = 0;
    //触发时间 -1为无限
    public float trigger_time = -1f;

    #region 构造与随机词条

    /// <summary>
    /// 供 ClassUtil 深拷贝逐字段复制已初始化的 BUFF 数据。
    /// </summary>
    public BuffBean() { }

    /// <summary>
    /// 构造BUFF运行时数据。isRandom=true时(扭蛋稀有度BUFF)对触发值做整数化闭区间随机
    /// </summary>
    /// <param name="id">buffId</param>
    /// <param name="isRandom">是否对触发值随机取值(扭蛋创建时为true)</param>
    /// <param name="createRate">BUFF创建几率</param>
    [Newtonsoft.Json.JsonConstructor]
    public BuffBean(long id, bool isRandom = false, float createRate = 1f)
    {
        this.id = id;
        this.createRate = createRate;
        var buffInfo = BuffInfoCfg.GetItemData(id);
        if (isRandom)
        {
            //触发改变的值：整数闭区间随机 [min, max]（如1~2只会得到1或2，不会出现1.1）
            int triggerValueMin = Mathf.RoundToInt(buffInfo.trigger_value_min);
            int triggerValueMax = Mathf.RoundToInt(buffInfo.trigger_value);
            this.trigger_value = UnityEngine.Random.Range(triggerValueMin, triggerValueMax + 1);
            //触发改变的值百分比：整数百分点闭区间随机 [min, max]（如10%~20%只会得到11%、12%这样的整数百分比，不会出现11.5%）
            int triggerValueRateMin = Mathf.RoundToInt(buffInfo.trigger_value_rate_min * 100);
            int triggerValueRateMax = Mathf.RoundToInt(buffInfo.trigger_value_rate * 100);
            this.trigger_value_rate = UnityEngine.Random.Range(triggerValueRateMin, triggerValueRateMax + 1) / 100f;
            //触发几率：直接取固定配置值(不再随机)
            this.trigger_chance = buffInfo.trigger_chance;
        }
        else
        {
            this.trigger_value = buffInfo.trigger_value;
            this.trigger_value_rate = buffInfo.trigger_value_rate;
            this.trigger_chance = buffInfo.trigger_chance;
        }
        this.trigger_num = buffInfo.trigger_num;
        this.trigger_time = buffInfo.trigger_time;
    }

    /// <summary>
    /// 创建一条继承素材且强度不低于素材的随机稀有度 BUFF。沿用扭蛋的整数闭区间口径。
    /// </summary>
    /// <param name="id">buffId</param>
    /// <param name="floorValue">触发值下限(素材原 trigger_value)</param>
    /// <param name="floorValueRate">素材原百分比；对召唤冷却和耗魔减免是数值上限。</param>
    /// <param name="createRate">BUFF创建几率</param>
    /// <returns>带下限随机后的BUFF运行时数据</returns>
    public static BuffBean CreateRandomWithFloor(long id, float floorValue, float floorValueRate, float createRate = 1f)
    {
        BuffBean buffData = new BuffBean(id, isRandom: true, createRate: createRate);
        var buffInfo = BuffInfoCfg.GetItemData(id);
        var (valueMin, valueMax) = GetInheritedValueRange(buffInfo, floorValue);
        buffData.trigger_value = UnityEngine.Random.Range(valueMin, valueMax + 1);
        var (rateMin, rateMax) = GetInheritedRateRange(buffInfo, floorValueRate);
        buffData.trigger_value_rate = UnityEngine.Random.Range(rateMin, rateMax + 1) / 100f;
        return buffData;
    }

    /// <summary>
    /// 计算进阶继承的触发值整数范围，供随机和预览共用。
    /// </summary>
    public static (int min, int max) GetInheritedValueRange(BuffInfoBean buffInfo, float inheritedValue)
    {
        int min = Mathf.RoundToInt(Mathf.Max(buffInfo.trigger_value_min, inheritedValue));
        return (min, Mathf.Max(min, Mathf.RoundToInt(buffInfo.trigger_value)));
    }

    /// <summary>
    /// 计算进阶继承的百分比整数范围。减免词条的素材值限制上界，其他词条限制下界。
    /// </summary>
    public static (int min, int max) GetInheritedRateRange(BuffInfoBean buffInfo, float inheritedRate)
    {
        int min = Mathf.RoundToInt(buffInfo.trigger_value_rate_min * 100);
        int max = Mathf.RoundToInt(buffInfo.trigger_value_rate * 100);
        int inherited = Mathf.RoundToInt(inheritedRate * 100);
        if (UsesLowerRateForStrongerEffect(buffInfo))
        {
            max = Mathf.Min(max, inherited);
            return (Mathf.Min(min, max), max);
        }
        min = Mathf.Max(min, inherited);
        return (min, Mathf.Max(min, max));
    }

    /// <summary>
    /// 判断词条是否以更低的百分比产生更强的效果。仅用于单属性召唤冷却或召唤耗魔。
    /// </summary>
    public static bool UsesLowerRateForStrongerEffect(BuffInfoBean buffInfo)
    {
        return buffInfo != null && buffInfo.class_entity == nameof(BuffEntityAttribute) &&
            (buffInfo.class_entity_data == nameof(CreatureAttributeTypeEnum.RCD) ||
             buffInfo.class_entity_data == nameof(CreatureAttributeTypeEnum.CMP));
    }

    /// <summary>
    /// 判断触发值和触发百分比是否各自命中最优端点，以及词条是否至少有一个有效随机字段且全部命中。
    /// </summary>
    public (bool isValueMaxRoll, bool isRateMaxRoll, bool isFullRoll) GetMaxRollFlags()
    {
        var buffInfo = BuffInfoCfg.GetItemData(id);
        int valueMin = Mathf.RoundToInt(buffInfo.trigger_value_min);
        int valueMax = Mathf.RoundToInt(buffInfo.trigger_value);
        bool hasRandomValue = valueMin < valueMax;
        bool isValueMaxRoll = hasRandomValue && Mathf.Approximately(trigger_value, valueMax);

        int rateMin = Mathf.RoundToInt(buffInfo.trigger_value_rate_min * 100);
        int rateMax = Mathf.RoundToInt(buffInfo.trigger_value_rate * 100);
        if (rateMin >= rateMax)
            return (isValueMaxRoll, false, isValueMaxRoll);

        int bestRate = UsesLowerRateForStrongerEffect(buffInfo) ? rateMin : rateMax;
        bool isRateMaxRoll = Mathf.Approximately(trigger_value_rate, bestRate / 100f);
        bool isFullRoll = isRateMaxRoll && (!hasRandomValue || isValueMaxRoll);
        return (isValueMaxRoll, isRateMaxRoll, isFullRoll);
    }

    #endregion

    /// <summary>
    /// 是否是基础属性BUFF(单属性,只添加属性没有额外条件),是则返回该属性,否则返回 None
    /// </summary>
    /// <returns></returns>
    public CreatureAttributeTypeEnum IsBuffEntityAttributeBase()
    {
        var buffInfo = BuffInfoCfg.GetItemData(id);
        if (!buffInfo.class_entity.IsNull() && buffInfo.class_entity.Equals("BuffEntityAttribute"))
        {
            return buffInfo.class_entity_data.GetEnum<CreatureAttributeTypeEnum>();
        }
        return CreatureAttributeTypeEnum.None;
    }

    /// <summary>
    /// 是否是多属性BUFF(BuffEntityAttributeMulti,同时改变多个属性)
    /// </summary>
    public bool IsBuffEntityAttributeMulti()
    {
        var buffInfo = BuffInfoCfg.GetItemData(id);
        return !buffInfo.class_entity.IsNull() && buffInfo.class_entity.Equals("BuffEntityAttributeMulti");
    }

    /// <summary>
    /// 是否是"纯属性BUFF"(单属性 BuffEntityAttribute 或多属性 BuffEntityAttributeMulti)。
    /// 纯属性BUFF走属性烘焙路径,不作为运行时BUFF实体添加(避免与烘焙重复计算)。
    /// </summary>
    public bool IsBuffEntityAttributeOnly()
    {
        return IsBuffEntityAttributeBase() != CreatureAttributeTypeEnum.None || IsBuffEntityAttributeMulti();
    }
}
