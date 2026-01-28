public class BuffEntityConditionalDelayTime : BuffEntityConditional
{

    public override void UpdateBuffTime(float buffTime)
    {
        base.UpdateBuffTime(buffTime);
        buffEntityData.timeUpdate += buffTime;
        float triggerTime = buffEntityData.GetTriggerTime();
        if (buffEntityData.timeUpdate >= triggerTime)
        {
            buffEntityData.timeUpdate = 0;
            TriggerBuffConditional(buffEntityData);
        }
    }
}