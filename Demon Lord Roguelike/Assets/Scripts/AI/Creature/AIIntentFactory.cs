using UnityEngine;

/// <summary>
/// AI意图工厂注册器：在游戏启动前把所有已知意图的构造方式注册到 AIBaseEntity，
/// 取代原先基于字符串拼接类名 + 反射的创建方式，让缺失/重命名问题在编译期暴露。
/// </summary>
public static class AIIntentFactory
{
    #region 注册入口
    /// <summary>
    /// 运行时自动注册所有意图工厂
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterAll()
    {
        //进攻生物
        AIBaseEntity.RegisterIntentFactory(AIIntentEnum.AttackCreatureIdle, () => new AIIntentAttackCreatureIdle());
        AIBaseEntity.RegisterIntentFactory(AIIntentEnum.AttackCreatureMove, () => new AIIntentAttackCreatureMove());
        AIBaseEntity.RegisterIntentFactory(AIIntentEnum.AttackCreatureAttack, () => new AIIntentAttackCreatureAttack());
        AIBaseEntity.RegisterIntentFactory(AIIntentEnum.AttackCreatureAttackCore, () => new AIIntentAttackCreatureAttackCore());
        AIBaseEntity.RegisterIntentFactory(AIIntentEnum.AttackCreatureDead, () => new AIIntentAttackCreatureDead());
        AIBaseEntity.RegisterIntentFactory(AIIntentEnum.AttackCreatureLured, () => new AIIntentAttackCreatureLured());

        //防守生物
        AIBaseEntity.RegisterIntentFactory(AIIntentEnum.DefenseCreatureIdle, () => new AIIntentDefenseCreatureIdle());
        AIBaseEntity.RegisterIntentFactory(AIIntentEnum.DefenseCreatureAttack, () => new AIIntentDefenseCreatureAttack());
        AIBaseEntity.RegisterIntentFactory(AIIntentEnum.DefenseCreatureDefend, () => new AIIntentDefenseCreatureDefend());
        AIBaseEntity.RegisterIntentFactory(AIIntentEnum.DefenseCreatureDead, () => new AIIntentDefenseCreatureDead());

        //核心生物
        AIBaseEntity.RegisterIntentFactory(AIIntentEnum.DefenseCoreCreatureIdle, () => new AIIntentDefenseCoreCreatureIdle());
        AIBaseEntity.RegisterIntentFactory(AIIntentEnum.DefenseCoreCreatureDead, () => new AIIntentDefenseCoreCreatureDead());
    }
    #endregion
}
