using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 长枪骑士普攻-向前突刺：以施法者根节点为起点，命中本路正前方 collider_area_size[0]×2 距离内的全部防守生物。
/// <para>与 AttackModeMeleeArea 的差异：不走 Physics 范围检测（其层掩码只含 CreatureDef，会漏 CreatureDef_Front/Back 层生物），
/// 改走 FightBean.GetDefenseCreatureByRoad 数据列表，按 roadIndex 与 x 距离精确筛选。</para>
/// </summary>
public class AttackModeMeleeLanceStab : BaseAttackMode
{
    #region 常量
    /// <summary>未配置攻击距离时的兜底突刺距离(格)</summary>
    private const float DefaultStabRange = 2f;
    /// <summary>浮点比较容差</summary>
    private const float PositionEpsilon = 0.001f;
    #endregion

    #region 攻击生命周期
    /// <summary>
    /// 无参路径（纯数据发射）：无施法者实体，不结算伤害，直接回收
    /// </summary>
    public override void StartAttack()
    {
        base.StartAttack();
        Destroy();
    }

    /// <summary>
    /// 开始攻击：立即结算本路正前方范围内的全部目标，播放命中特效后回收并回调
    /// </summary>
    public override void StartAttack(FightCreatureEntity attacker, FightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        if (attacker == null || attacker.IsDead() || attacker.creatureObj == null)
        {
            Destroy();
            return;
        }
        //突刺固定朝左（敌人向 -X 进攻），保证受击方向正确
        attackModeData.attackDirection = Vector3.left;
        StabHandle(attacker);
        PlayEffectForHit(attackModeData.startPos);
        //攻击完了就回收这个攻击
        Destroy();
        //攻击结束回调
        actionForAttackEnd?.Invoke(this);
    }
    #endregion

    #region 命中结算
    /// <summary>
    /// 结算本路正前方范围内的所有防守生物（每个目标只结算一次）
    /// </summary>
    private void StabHandle(FightCreatureEntity attacker)
    {
        var gameFightLogic = FightHandler.Instance.manager.GetCachedFightLogic();
        if (gameFightLogic == null || gameFightLogic.fightData == null)
            return;
        float range = GetStabRange();
        float attackerX = attacker.creatureObj.transform.position.x;
        List<FightCreatureEntity> listTarget = gameFightLogic.fightData.GetDefenseCreatureByRoad(attacker.fightCreatureData.roadIndex);
        for (int i = 0; i < listTarget.Count; i++)
        {
            var target = listTarget[i];
            if (target == null || target.IsDead() || target.creatureObj == null)
                continue;
            //正前方 = 目标在施法者左侧（x 更小），距离不超过突刺长度
            float dx = attackerX - target.creatureObj.transform.position.x;
            if (dx < -PositionEpsilon || dx > range + PositionEpsilon)
                continue;
            target.UnderAttack(this);
        }
    }

    /// <summary>
    /// 获取突刺距离：collider_area_size[0] × 2（AreaBoxFront 前向覆盖口径），未配置时回退默认值
    /// </summary>
    private float GetStabRange()
    {
        float[] areaSize = attackModeInfo != null ? attackModeInfo.GetColliderAreaSize() : null;
        if (areaSize == null || areaSize.Length == 0 || areaSize[0] <= 0f)
            return DefaultStabRange;
        return areaSize[0] * 2f;
    }
    #endregion
}
