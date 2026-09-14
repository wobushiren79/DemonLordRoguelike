/// <summary>
/// AI参数 Update 事件类型（NpcInfo.ai_param 每项的类型段，如 skill_update:技能extId:间隔秒）
/// </summary>
public enum AIParamUpdateEventTypeEnum
{
    None = 0,
    SkillUpdate = 1,//技能定时触发（skill_update:技能extId:间隔秒；技能须配 trigger_scene=1 释放技能意图，由 AICreatureEntity 通用 Update 事件到点切入）
}
