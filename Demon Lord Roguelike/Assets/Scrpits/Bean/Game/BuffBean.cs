
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

    public BuffBean(long id, bool isRandom = false, float createRate = 1f)
    {
        this.id = id;
        this.createRate = createRate;
        var buffInfo = BuffInfoCfg.GetItemData(id);
        if (isRandom)
        {
            this.trigger_value = UnityEngine.Random.Range(buffInfo.trigger_chance_min, buffInfo.trigger_chance);
            this.trigger_value_rate = UnityEngine.Random.Range(buffInfo.trigger_value_rate_min, buffInfo.trigger_value_rate);
            this.trigger_chance = UnityEngine.Random.Range(buffInfo.trigger_chance_min, buffInfo.trigger_chance);
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
}