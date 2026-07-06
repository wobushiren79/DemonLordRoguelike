using System.Collections.Generic;
using UnityEngine;

public class CreatureSacrificeBean
{
    public List<CreatureBean> fodderCreatures;//被献祭的饲料生物

    public CreatureBean targetCreature;//接受献祭的生物

    #region 测试参数(仅测试模式使用)
    // 「是否测试模式」已统一为全局标记 GameDataManager.isTestSimulation(不落盘由存档层统一拦截);此处只保留献祭专属的测试参数

    /// <summary>
    /// 是否使用手动指定的成功率：为 true 时掷骰用 manualSuccessRate，否则用真实公式计算(仅测试模拟模式下生效)
    /// </summary>
    public bool useManualSuccessRate = false;

    /// <summary>
    /// 手动指定的献祭成功率(0~1)，仅在测试模拟模式(GameDataManager.isTestSimulation)且 useManualSuccessRate 为 true 时生效
    /// </summary>
    public float manualSuccessRate = 1f;
    #endregion
}
