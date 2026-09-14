/// <summary>
/// 攻击模块扩展触发场景（AttackModeExtInfo.trigger_scene）
/// </summary>
public enum AttackModeExtTriggerSceneEnum
{
    AttackIntent = 0,//攻击意图内释放（默认：AIIntentCreatureAttack 额外攻击机制，攻击循环出手点发射，trigger_interval 生效）
    CastSkillIntent = 1,//释放技能意图（由 NPC ai_param 的 skill_update 事件注册到 AICreatureEntity 通用 Update 事件，全局周期触发，走路也放；trigger_interval 不生效，间隔以 ai_param 为准）
}
