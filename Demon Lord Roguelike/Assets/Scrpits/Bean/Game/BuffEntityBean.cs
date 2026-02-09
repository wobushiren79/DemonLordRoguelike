public class BuffEntityBean
{
    public long buffId;
    //是否有效
    public bool isValid;
    //buff施加者
    public string applierCreatureUUId;
    //buff触发者
    public string targetCreatureUUId;
    //buff持续时间
    public float timeUpdateTotal = 0;
    //触发时间
    public float timeUpdate = 0;
    //剩下的触发次数
    public int triggerNumLeft;

    //buff数据 固定数据 不可以修改
    public BuffBean buffData;

    public BuffEntityBean(BuffBean buffData, string applierCreatureUUId, string targetCreatureUUId)
    {
        SetData(buffData, applierCreatureUUId, targetCreatureUUId);
    }

    public void SetData(BuffBean buffData, string applierCreatureUUId, string targetCreatureUUId)
    {
        timeUpdateTotal = 0;
        timeUpdate = 0;
        this.buffId = buffData.id;
        this.buffData = buffData;

        var buffInfo = BuffInfoCfg.GetItemData(buffId);
        if (buffInfo == null)
        {
            LogUtil.LogError($"buff初始化失败 没有找到applierCreatureId_{applierCreatureUUId} targetCreatureId_{targetCreatureUUId}  buffId_{buffId}");
        }
        else
        {
            this.triggerNumLeft = buffInfo.trigger_num;
        }
        this.applierCreatureUUId = applierCreatureUUId;
        this.targetCreatureUUId = targetCreatureUUId;
        isValid = true;
    }

    public void ClearData()
    {
        buffId = 0;
        isValid = false;  
        applierCreatureUUId = null;
        targetCreatureUUId = null;
        timeUpdateTotal = 0;
        timeUpdate = 0;   
        triggerNumLeft = 0;
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