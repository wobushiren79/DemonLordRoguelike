using System;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using UnityEngine;

public class AttackModeRanged : BaseAttackMode
{
    #region 初始化外形
    /// <summary>
    /// 初始化攻击外形：基类流程后开启速度朝向——仅火球/冰球这类声明了 _VelocityWS 的 billboard shader 视觉生效，
    /// 由渲染器灌入 _VelocityWS.w 使贴图头（默认朝右）对准飞行方向；其余材质（如 RangedNormal 骷髅投手）无该属性、w 不灌入，不受影响。
    /// </summary>
    public override void InitAttackModeShow()
    {
        base.InitAttackModeShow();
        visualVelocityOrient = true;
    }
    #endregion

    /// <summary>
    /// 开始攻击
    /// </summary>
    public override void StartAttack(FightCreatureEntity attacker, FightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        actionForAttackEnd?.Invoke(this);
    }

    /// <summary>
    /// 收集本帧射线检测请求：直线弹道在当前位置沿攻击方向入队一条射线
    /// </summary>
    public override void PrepareRaycast(FightRaycastBatch batch)
    {
        batchRayStart = -1;
        EnqueueSingleRay(batch);
    }

    /// <summary>
    /// 更新处理
    /// </summary>
    public override void Update()
    {
        base.Update();
        FightCreatureEntity fightCreatureEntity = CheckHitTargetForSingle();
        if (fightCreatureEntity != null)
        {
            HandleForHitTarget(fightCreatureEntity);
            return;
        }
        //移动处理
        HandleForMove();
        //边界处理
        HandleForBound();
    }

    /// <summary>
    /// 处理击中生物之后的逻辑
    /// </summary>
    public virtual void HandleForHitTarget(FightCreatureEntity FightCreatureEntity)
    {
        //扣血
        FightCreatureEntity.UnderAttack(this);
        //攻击完了就回收这个攻击
        Destroy();
    }

    /// <summary>
    /// 移动处理
    /// </summary>
    public virtual void HandleForMove()
    {
        TranslatePosition(attackModeData.attackDirection * GameFightLogic.GetFightDeltaTime() * GetMoveSpeed());
    }

    /// <summary>
    /// 边界处理 飞太远的情况
    /// </summary>
    public virtual void HandleForBound()
    {
        if (CheckIsMoveBound())
        {
            Destroy();
        }
    }
}
