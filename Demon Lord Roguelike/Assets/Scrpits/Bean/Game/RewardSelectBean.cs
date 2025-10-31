using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 奖励数据
/// </summary>
public class RewardSelectBean
{
    //奖励列表
    public List<ItemBean> listReward = new List<ItemBean>();
    //已经选择次数
    public int selectNum;
    //可以选择的最大次数
    public int selectNumMax;
}
