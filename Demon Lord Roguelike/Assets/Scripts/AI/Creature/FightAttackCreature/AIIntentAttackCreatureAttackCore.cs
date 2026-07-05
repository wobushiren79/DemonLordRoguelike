using UnityEngine;

/// <summary>
/// 进攻生物-攻击魔王(核心)意图。
/// <para>进攻生物(无论近战/远程)完全靠近魔王(距离&lt;0.25)后进入本意图：固定播放一次攻击动作，
/// 动作出手时让魔王出血死亡——不经过任何 AttackMode(弹道/近战判定)。魔王死亡后由
/// GameFightLogic.CheckGameEnd 判定为战斗失败，游戏结束。</para>
/// <para><b>多单位并发处理</b>：允许多个进攻生物同时靠近魔王并各自播放攻击动作，但"魔王出血死亡"
/// 全局只结算一次——由 <see cref="KillDefenseCore"/> 内的 <c>IsDead()</c> 守卫保证(出血特效/致死/游戏结束均不重复)，
/// 即使同一帧多个单位同时到点，也只有第一个真正结算、其余被守卫拦下。
/// 若本单位到点前魔王已被他人处决，则直接回待机、不再攻击，也不会定格在本意图空转。</para>
/// </summary>
public class AIIntentAttackCreatureAttackCore : AIBaseIntent
{
    #region 字段
    /// <summary>攻击动作出手时长的保底值(秒)：配置 anim_attack_time 缺省时使用，保证攻击动作可见</summary>
    private const float AttackTimeFallback = 0.5f;
    /// <summary>所属进攻生物AI实体</summary>
    private AIAttackCreatureEntity selfAIEntity;
    /// <summary>攻击动作已计时(秒)</summary>
    private float timeForAttack;
    /// <summary>攻击动作出手时长(秒)：到点即结算魔王死亡</summary>
    private float timeForAttackCD;
    /// <summary>本单位是否已了结对魔王的攻击(已结算致死 或 因魔王已被他人处决而退出)，避免重复处理</summary>
    private bool hasFinished;
    #endregion

    #region 意图生命周期
    /// <summary>
    /// 进入意图：面向魔王并固定播放一次攻击动作，记录出手时长
    /// </summary>
    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        selfAIEntity = aiEntity as AIAttackCreatureEntity;
        timeForAttack = 0;
        hasFinished = false;
        var selfCreatureEntity = selfAIEntity?.selfCreatureEntity;
        if (selfCreatureEntity == null)
            return;
        //面向魔王(魔王固定在左侧)
        selfCreatureEntity.SetFaceDirection(Direction2DEnum.Left);
        //固定播放一次攻击动作(非循环), 结束后回到待机避免定格在最后一帧
        selfCreatureEntity.PlayAnim(SpineAnimationStateEnum.Attack, false);
        selfCreatureEntity.AddAnim(0, SpineAnimationStateEnum.Idle, true, 1);
        //攻击出手时长: 到点结算魔王死亡(配置缺省时用保底值)
        float animTime = selfCreatureEntity.fightCreatureData.creatureData.GetAttackAnimTime();
        timeForAttackCD = animTime > 0 ? animTime : AttackTimeFallback;
    }

    /// <summary>
    /// 每帧更新：攻击动作出手时让魔王出血死亡；多单位并发时只需一次致死，魔王已死则本单位回待机
    /// </summary>
    public override void IntentUpdate(AIBaseEntity aiEntity)
    {
        if (hasFinished)
            return;
        //清场回收后 selfCreatureEntity 置空时跳过, 避免空引用
        if (selfAIEntity == null || selfAIEntity.selfCreatureEntity == null)
            return;
        var coreCreature = GetDefenseCore();
        //魔王已被其它进攻生物处决(或已不存在): 本单位不再攻击, 回待机, 不定格在本意图空转
        if (coreCreature == null || coreCreature.creatureObj == null || coreCreature.IsDead())
        {
            hasFinished = true;
            selfAIEntity.ChangeIntent(AIIntentEnum.AttackCreatureIdle);
            return;
        }
        //攻击动作到点: 结算魔王出血死亡(每单位仅一次)
        timeForAttack += Time.deltaTime;
        if (timeForAttack >= timeForAttackCD)
        {
            hasFinished = true;
            KillDefenseCore(coreCreature);
        }
    }

    /// <summary>
    /// 离开意图：重置计时与了结标记
    /// </summary>
    public override void IntentLeaving(AIBaseEntity aiEntity)
    {
        timeForAttack = 0;
        hasFinished = false;
    }
    #endregion

    #region 魔王死亡结算
    /// <summary>
    /// 获取当前战斗的魔王(防守核心)实体，取不到返回 null
    /// </summary>
    private FightCreatureEntity GetDefenseCore()
    {
        var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        return gameFightLogic?.fightData?.fightDefenseCoreCreature;
    }

    /// <summary>
    /// 让魔王出血并死亡(不经过 AttackMode)。魔王死亡触发核心死亡意图，死亡结束后由
    /// GameFightLogic.CheckGameEnd 判定战斗失败、游戏结束。
    /// <para>此处的 <c>IsDead()</c> 二次判定是"多单位并发只结算一次"的关键守卫：同一帧多个单位同时到点时，
    /// 第一个执行的单位置魔王为死亡状态，其余单位在此被拦下，不重复播出血特效/不重复致死。</para>
    /// </summary>
    private void KillDefenseCore(FightCreatureEntity coreCreature)
    {
        if (coreCreature == null || coreCreature.creatureObj == null || coreCreature.IsDead())
            return;
        //魔王出血特效(方向指向魔王 即左)
        EffectHandler.Instance.ShowBloodEffect(coreCreature.creatureObj.transform.position + new Vector3(0, 0.5f, 0), Vector3.left);
        //直接判定魔王死亡→切换核心死亡意图, 由死亡结束事件驱动游戏结束检测
        coreCreature.SetCreatureDead();
    }
    #endregion
}
