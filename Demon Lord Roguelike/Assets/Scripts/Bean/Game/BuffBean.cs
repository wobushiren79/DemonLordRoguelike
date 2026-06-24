
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
    
    /// <summary>
    /// 构造BUFF运行时数据。isRandom=true时(扭蛋稀有度BUFF)对触发值做整数化闭区间随机
    /// </summary>
    /// <param name="id">buffId</param>
    /// <param name="isRandom">是否对触发值随机取值(扭蛋创建时为true)</param>
    /// <param name="createRate">BUFF创建几率</param>
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
    /// 创建一条「带下限的随机」稀有度BUFF(用于魔物进阶继承素材BUFF的场景)。
    /// 沿用扭蛋的整数闭区间随机口径,但把随机下限抬高到 floor(取 max(配置min, floor)),
    /// 保证重随机出的数值≥素材原数值(decision: 命中素材BUFF时数值重随机但不低于原值)。
    /// </summary>
    /// <param name="id">buffId</param>
    /// <param name="floorValue">触发值下限(素材原 trigger_value)</param>
    /// <param name="floorValueRate">触发值百分比下限(素材原 trigger_value_rate)</param>
    /// <param name="createRate">BUFF创建几率</param>
    /// <returns>带下限随机后的BUFF运行时数据</returns>
    public static BuffBean CreateRandomWithFloor(long id, float floorValue, float floorValueRate, float createRate = 1f)
    {
        BuffBean buffData = new BuffBean(id, isRandom: true, createRate: createRate);
        var buffInfo = BuffInfoCfg.GetItemData(id);
        //触发值:下限抬到 max(配置min, floor),上限取配置max,整数闭区间随机
        int valueFloor = Mathf.RoundToInt(Mathf.Max(buffInfo.trigger_value_min, floorValue));
        int valueMax = Mathf.Max(valueFloor, Mathf.RoundToInt(buffInfo.trigger_value));
        buffData.trigger_value = UnityEngine.Random.Range(valueFloor, valueMax + 1);
        //触发值百分比:同口径,按整数百分点随机
        int rateFloor = Mathf.RoundToInt(Mathf.Max(buffInfo.trigger_value_rate_min, floorValueRate) * 100);
        int rateMax = Mathf.Max(rateFloor, Mathf.RoundToInt(buffInfo.trigger_value_rate * 100));
        buffData.trigger_value_rate = UnityEngine.Random.Range(rateFloor, rateMax + 1) / 100f;
        return buffData;
    }

    /// <summary>
    /// 是否是基础属性BUFF(只添加属性没有额外条件)
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
}