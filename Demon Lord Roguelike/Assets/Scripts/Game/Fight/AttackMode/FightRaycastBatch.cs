using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

/// <summary>
/// 攻击模块射线检测的批量调度器
/// <para>把同一帧内所有走射线检测(Ray/RaySelf)的攻击模块收集到一个 NativeArray，用 RaycastCommand.ScheduleBatch 并行跑完，</para>
/// <para>替代原本"每个弹道各自 Physics.RaycastAll"的主线程串行 + 每帧数组分配(GC)。同帧 Schedule+Complete，命中零延迟。</para>
/// <para>NativeArray 走 Allocator.Persistent 常驻，仅在容量不足时增长；战斗 Clear 时 Dispose。</para>
/// </summary>
public class FightRaycastBatch
{
    #region 常量
    //每条射线最多记录的命中数(窗口大小)；弹道射线极短(collider_size~0.1)，几乎不会排到多个碰撞体，穿透也够用
    public const int MaxHitsPerRay = 4;
    //每个并行 Job 最少处理的命令数(Unity 建议按批分摊调度开销)
    private const int MinCommandsPerJob = 32;
    //首次分配容量
    private const int InitCapacity = 64;
    #endregion

    #region 字段
    //本帧射线命令缓冲(容量 = capacity)
    private NativeArray<RaycastCommand> commands;
    //射线命中结果缓冲(容量 = capacity * MaxHitsPerRay)
    private NativeArray<RaycastHit> results;
    //已分配容量(命令数)
    private int capacity = 0;
    //本帧已入队的命令数
    private int count = 0;
    #endregion

    #region 帧生命周期
    /// <summary>
    /// 开始新一帧的收集(重置计数)
    /// </summary>
    public void BeginFrame()
    {
        count = 0;
    }

    /// <summary>
    /// 入队一条射线检测命令，返回其命令索引(供消费阶段按索引取命中结果)
    /// </summary>
    public int Enqueue(Vector3 origin, Vector3 direction, float distance, int layerMask)
    {
        EnsureCapacity(count + 1);
        commands[count] = new RaycastCommand(origin, direction, new QueryParameters(layerMask), distance);
        return count++;
    }

    /// <summary>
    /// 批量调度所有已入队命令并同帧等待完成
    /// </summary>
    public void Schedule()
    {
        if (count == 0)
            return;
        //RaycastCommand 在 Job 内读物理世界、不会自动同步 transform；生物用 transform 移动，
        //须先同步一次，保证射线命中的是生物当前位置(对齐旧 Physics.RaycastAll 的 autoSyncTransforms 行为)
        Physics.SyncTransforms();
        var cmdSlice = commands.GetSubArray(0, count);
        var resSlice = results.GetSubArray(0, count * MaxHitsPerRay);
        RaycastCommand.ScheduleBatch(cmdSlice, resSlice, MinCommandsPerJob, MaxHitsPerRay).Complete();
    }

    /// <summary>
    /// 取某条命令的第 hitIndex 个命中结果(collider 为空表示该命令后续无更多命中)
    /// </summary>
    public RaycastHit GetHit(int cmdIndex, int hitIndex)
    {
        return results[cmdIndex * MaxHitsPerRay + hitIndex];
    }
    #endregion

    #region 容量与释放
    /// <summary>
    /// 确保容量足够，不足则翻倍增长并拷贝已入队命令(结果缓冲无需拷贝，Schedule 时会覆写)
    /// </summary>
    private void EnsureCapacity(int need)
    {
        if (need <= capacity)
            return;
        int newCap = capacity == 0 ? InitCapacity : capacity;
        while (newCap < need)
            newCap *= 2;
        var newCommands = new NativeArray<RaycastCommand>(newCap, Allocator.Persistent);
        var newResults = new NativeArray<RaycastHit>(newCap * MaxHitsPerRay, Allocator.Persistent);
        if (commands.IsCreated && count > 0)
            NativeArray<RaycastCommand>.Copy(commands, newCommands, count);
        DisposeArrays();
        commands = newCommands;
        results = newResults;
        capacity = newCap;
    }

    /// <summary>
    /// 释放底层 NativeArray(战斗结束调用；下一场战斗首次 Enqueue 时会重新按需分配)
    /// </summary>
    public void Dispose()
    {
        DisposeArrays();
        capacity = 0;
        count = 0;
    }

    /// <summary>
    /// 释放已分配的 NativeArray
    /// </summary>
    private void DisposeArrays()
    {
        if (commands.IsCreated)
            commands.Dispose();
        if (results.IsCreated)
            results.Dispose();
    }
    #endregion
}
