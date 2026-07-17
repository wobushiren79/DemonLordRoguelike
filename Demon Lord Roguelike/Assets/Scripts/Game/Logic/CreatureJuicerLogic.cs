using System;
using System.Collections.Generic;

/// <summary>
/// 魔汁机(魔物回收)逻辑:轻量逻辑,由 UICreatureJuicer 的 Start 按钮经 GameHandler.StartCreatureJuicer 驱动。
/// <para>与献祭/扭蛋不同,魔汁机为「UI 驱动」:E 键交互直接打开 UICreatureJuicer 选目标魔物(多选),点击 Start 才进入本逻辑。</para>
/// <para>当前仅搭好骨架,真正的榨汁流程(建筑动画/消耗魔物)与奖励结算留待后续接入 <see cref="StartJuice"/>。</para>
/// </summary>
[Serializable]
public class CreatureJuicerLogic : BaseGameLogic
{
    #region 数据
    //本次投入榨汁的魔物列表(多选,上限由研究门控)
    public List<CreatureBean> targetCreatures = new List<CreatureBean>();
    //当前基地场景预制(榨汁动画作用于 scenePrefab.objBuildingJuicer)
    public ScenePrefabForBase scenePrefab;
    #endregion

    #region 榨汁流程
    /// <summary>
    /// 开始榨汁:记录投入魔物列表并抓取场景预制,随后进入榨汁流程。
    /// <para>【留桩】榨汁流程与奖励后续接入:关UI/切镜头 → 播 objBuildingJuicer 榨汁动画(消耗 targetCreatures) → 奖励入账+存档 → 反馈返回。</para>
    /// </summary>
    /// <param name="targets">被投入榨汁的魔物列表</param>
    public void StartJuice(List<CreatureBean> targets)
    {
        targetCreatures = targets ?? new List<CreatureBean>();
        //抓取当前基地场景预制(后续榨汁动画作用在 objBuildingJuicer 上)
        scenePrefab = WorldHandler.Instance.GetCurrentScenePrefab<ScenePrefabForBase>(GameSceneTypeEnum.BaseGaming);

        //TODO 榨汁流程与奖励(后续接入):
        //1.锁UI/切魔汁机镜头; 2.播放 objBuildingJuicer 榨汁动画(消耗 targetCreatures);
        //3.结算奖励入账 + 存档; 4.反馈提示并返回。
        LogUtil.Log($"[魔汁机] 开始榨汁,投入魔物数量={targetCreatures.Count}(流程/奖励待接入)");
    }
    #endregion
}
