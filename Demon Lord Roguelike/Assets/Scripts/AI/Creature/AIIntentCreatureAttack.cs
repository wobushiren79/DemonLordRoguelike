using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 生物通用攻击意图：以"准备→出手→发起攻击→找下个目标"循环驱动普通攻击；
/// 并内置"额外攻击(攻击模块扩展)"机制——带 NpcInfo.attack_mode_ext 的生物在每次攻击判定时可优先出额外攻击。
/// 进攻/防守生物的攻击意图均继承自本类。
/// </summary>
public class AIIntentCreatureAttack : AIBaseIntent
{
    #region 字段
    /// <summary>攻击准备阶段已计时（秒）</summary>
    public float timeUpdateAttackPre = 0;
    /// <summary>攻击准备阶段时长（由攻速ASPD换算，每次 RefreshData 刷新）</summary>
    public float timeUpdateAttackPreCD = 0.2f;
    /// <summary>攻击出手阶段已计时（秒）</summary>
    public float timeUpdateAttacking = 0;
    /// <summary>攻击出手阶段时长（由攻速ASPD换算，每次 RefreshData 刷新）</summary>
    public float timeUpdateAttackingCD = 0.2f;
    /// <summary>所属生物AI实体</summary>
    public AICreatureEntity selfAIEntity;
    /// <summary>战斗生物数据（= selfAIEntity.selfCreatureEntity.fightCreatureData）</summary>
    public FightCreatureBean fightCreatureData;
    /// <summary>攻击状态：0准备中 1出手中(播攻击动画) 2已发起本次攻击(等待结束回调)</summary>
    public int attackState = 0;
    /// <summary>找不到目标时回退的待机意图（由子类按生物类型指定）</summary>
    public AIIntentEnum intentForIdle;
    /// <summary>自身死亡时切换的死亡意图（由子类按生物类型指定）</summary>
    public AIIntentEnum intentForDead;
    /// <summary>攻击准备基础时长（动画基准，用于动画速度换算）</summary>
    public float attackPreTimeBase;
    /// <summary>攻击动画基础时长（动画基准，用于动画速度换算）</summary>
    public float attackAnimTimeBase;
    #endregion

    #region 意图生命周期
    /// <summary>
    /// 进入攻击意图：缓存战斗数据与动画基准时长，重置计时与状态，初始化额外攻击，并立即触发一次攻击
    /// </summary>
    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        fightCreatureData = selfAIEntity.selfCreatureEntity.fightCreatureData;
        attackPreTimeBase = fightCreatureData.creatureData.GetAttackPreTime();
        attackAnimTimeBase = fightCreatureData.creatureData.GetAttackAnimTime();
        timeUpdateAttackPre = 0;
        timeUpdateAttacking = 0;
        attackState = 0;
        RefreshData();
        //刚进来立即开始一次攻击
        timeUpdateAttackPre = timeUpdateAttackPreCD;
        //初始化额外攻击（攻击模块扩展），进入攻击状态后各自开始计时
        InitExtraAttack();
    }

    /// <summary>
    /// 每帧更新：累计额外攻击CD，并推进"准备→出手"普通攻击循环
    /// </summary>
    public override void IntentUpdate(AIBaseEntity aiEntity)
    {
        //额外攻击各自累计CD（仅计时，释放时机融入下方普通攻击循环的判定）
        UpdateExtraAttackTimer();
        //攻击准备中
        if (attackState == 0)
        {
            timeUpdateAttackPre += GameFightLogic.GetFightDeltaTime();
            if (timeUpdateAttackPre >= timeUpdateAttackPreCD)
            {
                timeUpdateAttackPre = 0;
                RefreshData();
                AttackCreatureStart();
            }
        }
        //攻击出手中
        else if (attackState == 1)
        {
            timeUpdateAttacking += GameFightLogic.GetFightDeltaTime();
            if (timeUpdateAttacking >= timeUpdateAttackingCD)
            {
                timeUpdateAttacking = 0;
                RefreshData();
                AttackCreatureStartEnd();
            }
        }
    }

    /// <summary>
    /// 离开攻击意图：重置计时并清空额外攻击运行时数据
    /// </summary>
    public override void IntentLeaving(AIBaseEntity aiEntity)
    {
        timeUpdateAttackPre = 0;
        timeUpdateAttacking = 0;
        //离开攻击状态时清空额外攻击运行时数据
        listExtraAttack = null;
        currentExtraAttack = null;
    }

    /// <summary>
    /// 刷新攻击时长：按攻速ASPD换算准备/出手两阶段的CD
    /// </summary>
    public void RefreshData()
    {
        fightCreatureData.GetAttackTimeData(out float timeAttackPre, out float timeAttacking);
        timeUpdateAttackPreCD = timeAttackPre;
        timeUpdateAttackingCD = timeAttacking;
    }
    #endregion

    #region 攻击流程
    /// <summary>
    /// 立即触发一次攻击（把准备计时拉满，下一帧即进入出手）
    /// </summary>
    public virtual void AttackImm()
    {
        timeUpdateAttackPre = timeUpdateAttackPreCD;
    }

    /// <summary>
    /// 攻击开始（准备完毕的判定点）：校验目标/自身存活，判定本次出额外攻击还是普通攻击，并播放攻击动画
    /// </summary>
    public virtual void AttackCreatureStart()
    {
        attackState = 1;
        //目标已无 → 回待机
        if (selfAIEntity.targetCreatureEntity == null || selfAIEntity.targetCreatureEntity.IsDead())
        {
            ChangeIntent(intentForIdle);
            return;
        }
        //自己已死 → 切死亡
        if (selfAIEntity.selfCreatureEntity == null || selfAIEntity.selfCreatureEntity.IsDead())
        {
            ChangeIntent(intentForDead);
            return;
        }
        //出手前按目标位置刷新朝向（默认空实现，仅需转身的生物如防守生物覆盖）
        RefreshFaceForTarget();
        //本次攻击判定：额外攻击CD已到则本次出额外攻击(优先级高于普通攻击)，否则普通攻击
        currentExtraAttack = GetReadyExtraAttack();
        var selfCreatureInfo = fightCreatureData.creatureData.creatureInfo;
        //按基础动画时长与实际出手CD等比例计算动画播放速度
        float animSpeed = 1f;
        if (attackAnimTimeBase > 0 && timeUpdateAttackingCD > 0)
        {
            animSpeed = attackAnimTimeBase / timeUpdateAttackingCD;
        }
        //播放攻击动画
        if (selfCreatureInfo.anim_attack_loop == 1)//循环则只播攻击动画
        {
            selfAIEntity.selfCreatureEntity.PlayAnim(SpineAnimationStateEnum.Attack, true, animSpeed: animSpeed);
        }
        else//非循环则先播打击再接待机
        {
            selfAIEntity.selfCreatureEntity.PlayAnim(SpineAnimationStateEnum.Attack, false, animSpeed: animSpeed);
            selfAIEntity.selfCreatureEntity.AddAnim(0, SpineAnimationStateEnum.Idle, true, 1, animSpeed: animSpeed);
        }
    }

    /// <summary>
    /// 攻击出手（发射点）：本次有就绪额外攻击则用其攻击模块并清零其CD，否则用生物默认攻击模块
    /// </summary>
    public virtual void AttackCreatureStartEnd()
    {
        attackState = 2;
        if (currentExtraAttack != null)
        {
            //出额外攻击：用其 attack_mode_id 发射，并重置该额外攻击CD
            long extraAttackModeId = currentExtraAttack.extInfo.attack_mode_id;
            currentExtraAttack.timer = 0;
            currentExtraAttack = null;
            FightHandler.Instance.StartCreateAttackMode(selfAIEntity.selfCreatureEntity, selfAIEntity.targetCreatureEntity, ActionForAttackEnd, customAttackModeId: extraAttackModeId);
        }
        else
        {
            //出普通攻击：用生物默认攻击模块
            FightHandler.Instance.StartCreateAttackMode(selfAIEntity.selfCreatureEntity, selfAIEntity.targetCreatureEntity, ActionForAttackEnd);
        }
    }

    /// <summary>
    /// 攻击发起后的回调：按攻击方向重新搜索目标，有目标则回到准备阶段继续攻击，无目标则回待机
    /// </summary>
    public void ActionForAttackEnd(BaseAttackMode attackMode)
    {
        //按方向策略重新搜索目标（默认沿本发攻击方向单向搜；子类可覆盖为正面优先+背后补搜）
        var findTargetCreature = FindNextTarget(attackMode);
        //找不到最近目标 → 回待机
        if (findTargetCreature == null)
        {
            ChangeIntent(intentForIdle);
            return;
        }
        //更新为新目标
        if (findTargetCreature != selfAIEntity.targetCreatureEntity)
        {
            selfAIEntity.targetCreatureEntity = findTargetCreature;
        }
        //目标已无 → 回待机
        if (selfAIEntity.targetCreatureEntity == null || selfAIEntity.targetCreatureEntity.IsDead())
        {
            ChangeIntent(intentForIdle);
            return;
        }
        //目标可能切到另一侧（如从正面切到背后），同步刷新朝向
        RefreshFaceForTarget();
        //否则回到准备阶段继续攻击
        attackState = 0;
    }

    /// <summary>
    /// 攻击后重新搜索下一个目标的方向策略；默认沿本发攻击方向单向搜索。
    /// 子类可覆盖（如防守生物：正面优先，正面无目标时向身后补搜）。
    /// </summary>
    protected virtual FightCreatureEntity FindNextTarget(BaseAttackMode attackMode)
    {
        DirectionEnum findDirectrion = attackMode.attackModeData.attackDirection.x > 0 ? DirectionEnum.Right : DirectionEnum.Left;
        return selfAIEntity.FindCreatureEntityForSinge(findDirectrion);
    }

    /// <summary>
    /// 按当前目标位置刷新自身朝向；默认空实现（进攻/核心生物朝向固定，无需转身）。
    /// 需要转身的生物（如可攻击身后的防守生物）覆盖此方法。
    /// </summary>
    protected virtual void RefreshFaceForTarget()
    {
    }
    #endregion

    #region 额外攻击（攻击模块扩展）
    /// <summary>
    /// 额外攻击运行时数据（每个额外攻击独立计时）
    /// </summary>
    protected class ExtraAttackRuntime
    {
        /// <summary>额外攻击配置</summary>
        public AttackModeExtInfoBean extInfo;
        /// <summary>进入攻击状态后已累计的时间（秒）</summary>
        public float timer;
    }
    /// <summary>当前生物的额外攻击列表（无则为null）</summary>
    protected List<ExtraAttackRuntime> listExtraAttack;
    /// <summary>本次攻击循环选中的额外攻击（AttackCreatureStart 判定、AttackCreatureStartEnd 发射；null=本次为普通攻击）</summary>
    protected ExtraAttackRuntime currentExtraAttack;

    /// <summary>
    /// 初始化额外攻击：读取NPC的 attack_mode_ext，筛选按间隔释放的类型并重置各自计时器
    /// </summary>
    protected void InitExtraAttack()
    {
        listExtraAttack = null;
        currentExtraAttack = null;
        //仅NPC生物(敌人)带有 attack_mode_ext 配置
        var npcInfo = fightCreatureData?.creatureData?.creatureNpcData?.npcInfo;
        if (npcInfo == null)
        {
            return;
        }
        var listExtInfo = npcInfo.GetListAttackModeExtInfo();
        if (listExtInfo.IsNull())
        {
            return;
        }
        for (int i = 0; i < listExtInfo.Count; i++)
        {
            var extInfo = listExtInfo[i];
            //目前仅 BossSkill 类型按间隔自动释放，未来新增其他类型可在此扩展分支
            if (extInfo == null || extInfo.GetExtType() != AttackModeExtTypeEnum.BossSkill)
            {
                continue;
            }
            if (listExtraAttack == null)
            {
                listExtraAttack = new List<ExtraAttackRuntime>();
            }
            listExtraAttack.Add(new ExtraAttackRuntime { extInfo = extInfo, timer = 0 });
        }
    }

    /// <summary>
    /// 累计各额外攻击的CD（仅计时，不在此释放；释放时机融入普通攻击循环的判定）
    /// </summary>
    protected void UpdateExtraAttackTimer()
    {
        if (listExtraAttack == null)
        {
            return;
        }
        for (int i = 0; i < listExtraAttack.Count; i++)
        {
            listExtraAttack[i].timer += GameFightLogic.GetFightDeltaTime();
        }
    }

    /// <summary>
    /// 取第一个CD已到达的额外攻击（按列表顺序即优先级）；都未到则返回null（本次走普通攻击）
    /// </summary>
    protected ExtraAttackRuntime GetReadyExtraAttack()
    {
        if (listExtraAttack == null)
        {
            return null;
        }
        for (int i = 0; i < listExtraAttack.Count; i++)
        {
            var extraAttack = listExtraAttack[i];
            if (extraAttack.timer >= extraAttack.extInfo.trigger_interval)
            {
                return extraAttack;
            }
        }
        return null;
    }
    #endregion
}
