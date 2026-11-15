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
    /// 检测是否击中生物：归位(z 横移)途中不检测，归位完成后才恢复父类检测。
    /// <para>⚠️归位途中子箭仍停在出发路(z 在攻击者所在道路附近)，若照常检测，朝前的短射线会命中出发路(同一路)的敌人——
    /// 这发子弹本该打目标路，故归位途中必须关闭命中；判定条件与 <see cref="HandleForMove"/> 的归位条件一致，自洽。</para>
    /// </summary>
    public override FightCreatureEntity CheckHitTargetForSingle()
    {
        if (Mathf.Abs(position.z - targetRoad) > RoadCloseThreshold)
        {
            return null;
        }
        return base.CheckHitTargetForSingle();
    }

    /// <summary>
    /// 移动处理：先向目标道路(z 轴)归位，归位完成后再走父类的直线飞行
    /// <para>⚠️归位阶段只动 z、不前进（归位途中 return 跳过本帧直线飞行）：若归位与前进同时进行，归位会消耗
    /// z差/RoadCloseSpeedRate 格的 x 行程（分裂到相邻路 z差1 时=0.5 格）。近距离发射（如防守单位放最前排贴路右缘、
    /// 弓箭手走进道路即索敌出手，距离仅约0.5格）时，上下路子箭归位完成前 x 就越过了该路防守单位，归位完成后已处于其背后
    /// （更靠核心一侧），朝前的短射线永远打不到背后的目标 → 分裂箭从防守单位背后穿过（中路那发无需归位故正常）。
    /// 归位阶段 x 不动可完整保留命中窗口：归位完成后 x=发射时 x，与目标距离=索敌命中距离，必定仍在正前方，贴脸/远射均必中。</para>
    /// </summary>
    public override void HandleForMove()
    {
        //还没到目标道路 先向它靠拢（归位速度 = 飞行速度 × RoadCloseSpeedRate；此阶段只横移不前进，保命中窗口）
        if (Mathf.Abs(position.z - targetRoad) > RoadCloseThreshold)
        {
            Vector3 roadPosition = new Vector3(position.x, position.y, targetRoad);
            SetPosition(Vector3.MoveTowards(position, roadPosition, GameFightLogic.GetFightDeltaTime() * GetMoveSpeed() * RoadCloseSpeedRate));
            return;
        }
        base.HandleForMove();
    }
    #endregion
}
