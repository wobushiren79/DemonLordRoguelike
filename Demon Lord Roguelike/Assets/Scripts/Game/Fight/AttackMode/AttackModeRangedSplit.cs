using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 分裂弹-发射器：本身不飞、不画、不命中，只在攻击瞬间按「攻击者所在道路 + 上下交替偏移」算出分裂道路，
/// 每条道路发射一发 child_attack_mode_id 指向的子弹道 <see cref="AttackModeRangedSplitChild"/>，随即自毁。
/// <para>【为什么是发射器】每发子弹都是独立的 <see cref="BaseAttackMode"/>，位置(position)/射线批处理/DSP 批量渲染/拖尾/
/// 对象池全部免费复用现成体系。旧实现由本类自管多个 GameObject，导致每加一个新特性(射线批处理、DSP 渲染…)都要在本类里
/// 重做一遍多路版本，故改为发射器。</para>
/// <para>【配置】父行(本类)只配 class_name + child_attack_mode_id，外加可选的 sound_start；
/// prefab_name/visual_name/speed_move/collider_size/sound_hit 等飞行与命中字段全部配在子弹道行上。</para>
/// <para>⚠️sound_start 只能配在父行：GetAttackModePrefab 是每发子弹道各调一次的，配到子弹道行上会导致一次射击播 N 遍发射音效。</para>
/// </summary>
public class AttackModeRangedSplit : BaseAttackMode
{
    #region 字段
    /// <summary>分裂数量（自身道路之外额外分裂出的道路数，上下交替取；越界的道路会被跳过，故实际发射数≤splitNum+1）</summary>
    public int splitNum = 2;
    /// <summary>本次分裂的目标道路列表（含攻击者自身所在道路；复用避免每次发射 new）</summary>
    private readonly List<int> listSplitRoad = new List<int>();
    #endregion

    #region 开始攻击
    /// <summary>
    /// 开始攻击-默认
    /// </summary>
    public override void StartAttack()
    {
        base.StartAttack();
        CreatureSplitAttack();
        //发射器完成使命 立刻回收（子弹道各自独立存活）
        Destroy();
    }

    /// <summary>
    /// 开始攻击-生物
    /// </summary>
    public override void StartAttack(FightCreatureEntity attacker, FightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        CreatureSplitAttack();
        actionForAttackEnd?.Invoke(this);
        //发射器完成使命 立刻回收（子弹道各自独立存活）
        Destroy();
    }
    #endregion

    #region 创建
    /// <summary>
    /// 按分裂道路逐条发射子弹道
    /// </summary>
    public void CreatureSplitAttack()
    {
        long childAttackModeId = attackModeInfo.child_attack_mode_id;
        if (childAttackModeId == 0)
        {
            LogUtil.LogError($"分裂弹[{attackModeInfo.id}]未配置 child_attack_mode_id，无法发射子弹道");
            return;
        }
        //子弹道配置必须存在：GetAttackModePrefab 内部会直接解引用它，缺行会 NRE 且已取出的 AttackModeBean 回不了池
        var childAttackModeInfo = AttackModeInfoCfg.GetItemData(childAttackModeId);
        if (childAttackModeInfo == null)
        {
            LogUtil.LogError($"分裂弹[{attackModeInfo.id}]的 child_attack_mode_id={childAttackModeId} 在配置表中不存在");
            return;
        }
        //⚠️子弹道不能又是发射器：发射是同步的(GetAttackModePrefab 立即回调 StartAttack)，
        //故自指(204001→204001)或成环(A→B→A)都会在同一帧无限递归到 StackOverflow —— 该异常在 Mono 下不可 catch，直接崩进程且无有效日志。
        //任何环必然经过另一个发射器，故"子弹道不得为发射器"这一条即可把自指与成环一并挡住。
        if (childAttackModeInfo.class_name == nameof(AttackModeRangedSplit))
        {
            LogUtil.LogError($"分裂弹[{attackModeInfo.id}]的 child_attack_mode_id={childAttackModeId} 指向的仍是分裂发射器，会无限递归导致崩溃，已阻止发射");
            return;
        }
        RefreshSplitRoad();
        for (int i = 0; i < listSplitRoad.Count; i++)
        {
            CreateSplitChild(childAttackModeId, listSplitRoad[i]);
        }
    }

    /// <summary>
    /// 计算本次分裂的目标道路：攻击者所在道路 + 上下交替偏移(+1、-1、+2、-2…)，超出场景道路范围的跳过
    /// </summary>
    private void RefreshSplitRoad()
    {
        var gameFightLogic = FightHandler.Instance.manager.GetCachedFightLogic();
        int sceneRoadNum = gameFightLogic.fightData.sceneRoadNum;
        //⚠️必须四舍五入而非(int)截断：进攻生物的 z 带 ±0.01 防重叠抖动(见 CreatureHandler.CreateAttackCreature)，
        //截断会把道路3的敌人(z=2.99)算成道路2，导致约半数射击整个扇面错道一格(防守生物 z 为整数，故该 bug 只在进攻侧显现)
        int startRoad = Mathf.RoundToInt(attackModeData.startPos.z);
        listSplitRoad.Clear();
        //自身所在道路（startRoad 由生物位置推导，同样要过边界检查，不可无条件信任）
        TryAddSplitRoad(startRoad, sceneRoadNum);
        for (int i = 0; i < splitNum; i++)
        {
            //偏移量按 i 依次取 +1、-1、+2、-2…（偶数索引向上、奇数索引向下）
            int roadOffset = (i / 2) + 1;
            if (i % 2 == 1)
            {
                roadOffset = -roadOffset;
            }
            TryAddSplitRoad(startRoad + roadOffset, sceneRoadNum);
        }
    }

    /// <summary>
    /// 把一条道路加入本次分裂列表；超出场景道路范围 [1, sceneRoadNum] 的直接丢弃（否则会发出一发飞向场外、永远打不到人的废弹）
    /// </summary>
    private void TryAddSplitRoad(int targetRoad, int sceneRoadNum)
    {
        if (targetRoad >= 1 && targetRoad <= sceneRoadNum)
        {
            listSplitRoad.Add(targetRoad);
        }
    }

    /// <summary>
    /// 发射一发子弹道：复制本发的攻击者快照数据，指定目标道路后开始攻击
    /// </summary>
    private void CreateSplitChild(long childAttackModeId, int targetRoad)
    {
        var fightManager = FightHandler.Instance.manager;
        //子弹道各自持有一份数据（攻击者快照与父弹道一致，attackModeId 换成子弹道自己的）
        AttackModeBean childData = fightManager.GetAttackModeData(childAttackModeId);
        childData.CopyAttackerDataFrom(attackModeData);
        fightManager.GetAttackModePrefab(childAttackModeId, (childAttackMode) =>
        {
            childAttackMode.StartAttackInit(childData);
            if (childAttackMode is AttackModeRangedSplitChild splitChild)
            {
                splitChild.targetRoad = targetRoad;
            }
            else
            {
                //配错 class_name 时不静默降级：否则各发子弹全部重叠沿同一条道路飞，看着像只发了一发，却无任何报错
                LogUtil.LogError($"分裂弹[{attackModeInfo.id}]的子弹道[{childAttackModeId}] class_name 不是 {nameof(AttackModeRangedSplitChild)}，无法归位到目标道路，各发子弹会重叠飞行");
            }
            childAttackMode.StartAttack();
        });
    }
    #endregion
}
