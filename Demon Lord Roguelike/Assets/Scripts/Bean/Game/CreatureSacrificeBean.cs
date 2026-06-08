using System.Collections.Generic;
using UnityEngine;

public class CreatureSacrificeBean
{
    public List<CreatureBean> fodderCreatures;//被献祭的饲料生物

    public CreatureBean targetCreature;//接受献祭的生物

    #region 测试参数(仅测试模式使用)
    /// <summary>
    /// 是否为测试模式：为 true 时结算不落盘到真实存档，仅在内存中生效
    /// </summary>
    public bool isTestMode = false;

    /// <summary>
    /// 是否使用手动指定的成功率：为 true 时掷骰用 manualSuccessRate，否则用真实公式计算
    /// </summary>
    public bool useManualSuccessRate = false;

    /// <summary>
    /// 手动指定的献祭成功率(0~1)，仅在 isTestMode 且 useManualSuccessRate 为 true 时生效
    /// </summary>
    public float manualSuccessRate = 1f;
    #endregion
}
