using UnityEngine;

/// <summary>
/// 生物出土冒出意图（召唤物出场动画：从地底冒出；攻守通用——进攻/防守生物均可经 StartEmerge 切入）。
/// <para>由 AICreatureEntity.StartEmerge 强制切换进入（照 AIAttackCreatureEntity.StartKnockback 先例；当前唯一调用方=AttackModeSummon 召唤生成当帧）：</para>
/// <para>进入时把生物压到地面下 EmergeDepth 深处并播破土特效(Effect_BodySlam_1)，随后 EmergeDuration 秒匀速升回地面（期间不能移动/索敌/攻击，攻击循环被自然打断）；</para>
/// <para>生物低于地面部分被不透明道路面深度遮挡，自然形成"破土而出"观感；冒出完成按阵营回各自闲置意图（防守→DefenseCreatureIdle 朝右，进攻→AttackCreatureIdle 朝左，按 AI 实体类型解析）；计时走 GetFightDeltaTime 随倍速/暂停缩放。</para>
/// <para>注册：AIIntentFactory 注册 CreatureEmerge；进攻(AIAttackCreatureEntity)/防守(AIDefenseCreatureEntity) 的 InitIntentEnum 均已加入（魔王核心 AIDefenseCoreCreatureEntity 未注册——核心生物不会被召唤，需要时先补注册并确认回 idle 的阵营解析）。</para>
/// </summary>
public class AIIntentCreatureEmerge : AIBaseIntent
{
    #region 常量
    /// <summary>出土时长（秒）</summary>
    public const float EmergeDuration = 0.6f;
    /// <summary>地底下压深度（世界单位）</summary>
    public const float EmergeDepth = 1.2f;
    /// <summary>破土特效（EffectInfo 配置表 id：Effect_BodySlam_1 地面打击，走 ShowEffect 逐实例通道，多只同帧冒出各自可见）</summary>
    public const long EmergeEffectId = 700001;
    #endregion

    #region 字段
    /// <summary>所属生物AI实体（攻守通用基类）</summary>
    public AICreatureEntity selfAIEntity;
    /// <summary>冒出完成回落的闲置意图（按阵营解析：防守→DefenseCreatureIdle，进攻→AttackCreatureIdle）</summary>
    public AIIntentEnum intentForIdle;
    /// <summary>冒出期朝向（按阵营解析：防守→Right 朝敌来向，进攻→Left 朝魔王）</summary>
    public Direction2DEnum faceDirection;
    /// <summary>地面目标 y（冒出完成落点，进入时缓存=出生 y）</summary>
    public float groundY;
    /// <summary>已出土时间（秒，累计 GetFightDeltaTime）</summary>
    public float emergeTimer;
    #endregion

    #region 意图生命周期
    /// <summary>
    /// 进入出土意图：按阵营解析回落意图与朝向，压到地底并播破土特效，播待机动画（被控出土状态）
    /// </summary>
    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        selfAIEntity = aiEntity as AICreatureEntity;
        //阵营解析：防守生物回防守闲置朝右，其余（进攻）回进攻闲置朝左
        bool isDefense = aiEntity is AIDefenseCreatureEntity;
        intentForIdle = isDefense ? AIIntentEnum.DefenseCreatureIdle : AIIntentEnum.AttackCreatureIdle;
        faceDirection = isDefense ? Direction2DEnum.Right : Direction2DEnum.Left;
        Transform selfTF = selfAIEntity.selfCreatureEntity.creatureObj.transform;
        groundY = selfTF.position.y;
        emergeTimer = 0;
        //压到地底（低于地面的部分被道路面深度遮挡）+ 在地面位置播破土特效
        selfTF.position = new Vector3(selfTF.position.x, groundY - EmergeDepth, selfTF.position.z);
        EffectHandler.Instance.ShowEffect(EmergeEffectId, new Vector3(selfTF.position.x, groundY, selfTF.position.z));
        selfAIEntity.selfCreatureEntity.SetFaceDirection(faceDirection);
        selfAIEntity.selfCreatureEntity.PlayAnim(SpineAnimationStateEnum.Idle, true);
    }

    /// <summary>
    /// 每帧：按出土进度匀速升回地面，升完按阵营回各自闲置意图重新索敌
    /// </summary>
    public override void IntentUpdate(AIBaseEntity aiEntity)
    {
        emergeTimer += GameFightLogic.GetFightDeltaTime();
        float progress = Mathf.Clamp01(emergeTimer / EmergeDuration);
        Transform selfTF = selfAIEntity.selfCreatureEntity.creatureObj.transform;
        selfTF.position = new Vector3(selfTF.position.x, Mathf.Lerp(groundY - EmergeDepth, groundY, progress), selfTF.position.z);
        if (progress >= 1)
        {
            ChangeIntent(intentForIdle);
        }
    }
    #endregion
}
