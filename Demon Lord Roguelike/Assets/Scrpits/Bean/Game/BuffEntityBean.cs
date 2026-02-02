public class BuffEntityBean
{
    public long buffId;
    //是否有效
    public bool isValid;
    //buff施加者
    public string applierCreatureId;
    //buff触发者
    public string targetCreatureId;
    //buff持续时间
    public float timeUpdateTotal = 0;
    //触发时间
    public float timeUpdate = 0;
    //剩下的触发次数
    public int triggerNumLeft;

    //buff数据 固定数据 不可以修改
    public BuffBean buffData;

    public BuffEntityBean(BuffBean buffData, string applierCreatureId, string targetCreatureId)
    {
        SetData(buffData, applierCreatureId, targetCreatureId);
    }

    public void SetData(BuffBean buffData, string applierCreatureId, string targetCreatureId)
    {
        this.buffId = buffData.id;
        this.buffData = buffData;

        var buffInfo = BuffInfoCfg.GetItemData(buffId);
        if (buffInfo == null)
        {
            LogUtil.LogError($"buff初始化失败 没有找到applierCreatureId_{applierCreatureId} targetCreatureId_{targetCreatureId}  buffId_{buffId}");
        }
        else
        {
            this.triggerNumLeft = buffInfo.trigger_num;
        }
        this.applierCreatureId = applierCreatureId;
        this.targetCreatureId = targetCreatureId;
        isValid = true;
    }

    public BuffInfoBean GetBuffInfo()
    {
        return BuffInfoCfg.GetItemData(buffId);
    }

    public float GetTriggerValue()
    {
        return buffData.trigger_value;
    }

    public float GetTriggerValueRate()
    {
        return buffData.trigger_value_rate;
    }

    public float GetTriggerTime()
    {
        return buffData.trigger_time;
    }

    public float GetTriggerChance()
    {
        return buffData.trigger_chance;
    }

    public int GetTriggerNum()
    {
        return buffData.trigger_num;
    }
}