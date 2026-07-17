using UnityEngine;

/// <summary>
/// 分裂弹-子弹道：由发射器 <see cref="AttackModeRangedSplit"/> 按分裂道路逐条发射，每发独立存活。
/// <para>与普通直线远程弹道的唯一差别 = 飞行途中额外向自己的目标道路(z 轴)归位；命中/边界/射线批处理/DSP 批量渲染/拖尾/对象池
/// 全部原样复用 <see cref="AttackModeRanged"/>，本类不自管 GameObject。</para>
/// <para>【配置】对应父行 child_attack_mode_id 指向的那一行(class_name=AttackModeRangedSplitChild)，飞行与命中字段
/// (prefab_name/visual_name/speed_move/collider_size/sound_hit 等)全配在这一行上。</para>
/// </summary>
public class AttackModeRangedSplitChild : AttackModeRanged
{
    #region 字段
    /// <summary>目标道路(z 轴世界坐标)：由发射器在 StartAttack 之前写入</summary>
    public int targetRoad;
    #endregion

    #region 数值常量（策划调整入口）
    /// <summary>向目标道路归位的速度倍率（相对弹道飞行速度）</summary>
    public const float RoadCloseSpeedRate = 2f;
    /// <summary>与目标道路距离小于此值即视为已归位</summary>
    public const float RoadCloseThreshold = 0.02f;
    #endregion

    #region 逻辑处理
    /// <summary>
    /// 移动处理：先向目标道路(z 轴)归位，再走父类的直线飞行
    /// </summary>
    public override void HandleForMove()
    {
        //还没到目标道路 先向它靠拢（归位速度 = 飞行速度 × RoadCloseSpeedRate）
        if (Mathf.Abs(position.z - targetRoad) > RoadCloseThreshold)
        {
            Vector3 roadPosition = new Vector3(position.x, position.y, targetRoad);
            SetPosition(Vector3.MoveTowards(position, roadPosition, Time.deltaTime * GetMoveSpeed() * RoadCloseSpeedRate));
        }
        base.HandleForMove();
    }
    #endregion
}
