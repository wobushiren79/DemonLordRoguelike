using UnityEngine;

/// <summary>
/// 进攻生物被击退意图（深渊馈赠「第六次冲击」冲击波等位移效果用）
/// <para>被击退时由 AIAttackCreatureEntity.StartKnockback 强制切换进入：按击退方向匀速推移，
/// 固定 KnockbackDuration 秒推完全程（任何击退距离时长一致、手感统一），期间不能攻击/索敌（攻击循环被自然打断）；</para>
/// <para>推移落点 x 钳制：右缘硬钳(0.5+路长)、左缘只防「从道路内被推出左缘」——已在左缘内(x&lt;0.5，直冲魔王阶段)的敌人不往前拉，自然向右推回道路；</para>
/// <para>击退中再次被击退：StartKnockback 直接刷新本意图的方向/剩余距离（原地续推，不重进意图）；</para>
/// <para>推移结束回 AttackCreatureIdle，重新走「闲置→移动→攻击」索敌流程（与防守目标的距离重新判定，不会隔空续打）。</para>
/// </summary>
public class AIIntentAttackCreatureKnockback : AIBaseIntent
{
    #region 常量
    /// <summary>击退推移固定时长（秒）：任何击退距离都在此时长内推完（距离越大推速越快），手感统一</summary>
    public const float KnockbackDuration = 0.2f;
    #endregion

    #region 字段
    /// <summary>所属生物AI实体</summary>
    public AIAttackCreatureEntity selfAIEntity;
    /// <summary>击退方向（XZ 平面归一化）</summary>
    public Vector3 knockbackDirection = Vector3.right;
    /// <summary>剩余待推移距离</summary>
    public float knockbackDistanceRemain;
    /// <summary>击退推移速度（= 总距离/KnockbackDuration，SetupKnockback 时算）</summary>
    public float knockbackSpeed;
    /// <summary>道路 x 范围（落点钳制用，IntentEntering 时按当场路长缓存）</summary>
    public float roadMinX, roadMaxX;
    #endregion

    #region 意图生命周期
    /// <summary>
    /// 进入击退意图：缓存道路 x 范围并播待机动画（被控推移状态）
    /// </summary>
    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        selfAIEntity = aiEntity as AIAttackCreatureEntity;
        var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        if (gameFightLogic?.fightData != null)
        {
            roadMinX = 0.5f;
            roadMaxX = 0.5f + gameFightLogic.fightData.sceneRoadLength;
        }
        else
        {
            roadMinX = 0.5f;
            roadMaxX = 15f;
        }
        selfAIEntity.selfCreatureEntity.PlayAnim(SpineAnimationStateEnum.Idle, true);
    }

    /// <summary>
    /// 每帧：按击退速度推移（x 钳制道路范围），剩余距离走完回闲置重新索敌
    /// </summary>
    public override void IntentUpdate(AIBaseEntity aiEntity)
    {
        if (knockbackDistanceRemain <= 0)
        {
            ChangeIntent(AIIntentEnum.AttackCreatureIdle);
            return;
        }
        float moveDistance = knockbackSpeed * GameFightLogic.GetFightDeltaTime();
        if (moveDistance > knockbackDistanceRemain)
            moveDistance = knockbackDistanceRemain;
        knockbackDistanceRemain -= moveDistance;
        Transform selfTF = selfAIEntity.selfCreatureEntity.creatureObj.transform;
        Vector3 newPos = selfTF.position + knockbackDirection * moveDistance;
        //x 钳制：右缘硬钳；左缘下限取「当前x与左缘的较小者」——道路内敌人防被推出左缘，
        //已在左缘内(x<roadMinX，直冲魔王阶段)的敌人不往前拉（防击退变"前吸"），自然向右推回道路
        newPos.x = Mathf.Clamp(newPos.x, Mathf.Min(roadMinX, selfTF.position.x), roadMaxX);
        selfTF.position = newPos;
    }
    #endregion

    #region 击退参数
    /// <summary>
    /// 设置击退参数（由 StartKnockback 写入）：方向归一化、按固定时长换算推移速度；重复击退直接刷新（原地续推）
    /// </summary>
    /// <param name="direction">击退方向（XZ 平面，内部归一化）</param>
    /// <param name="distance">击退总距离</param>
    public void SetupKnockback(Vector3 direction, float distance)
    {
        direction.y = 0;
        knockbackDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.right;
        knockbackDistanceRemain = distance;
        knockbackSpeed = distance / KnockbackDuration;
    }
    #endregion
}
