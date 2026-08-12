using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 魔汁机(魔物回收)逻辑:轻量逻辑,由 UICreatureJuicer 的 Start 按钮经 GameHandler.StartCreatureJuicer 驱动。
/// <para>与献祭/扭蛋不同,魔汁机为「UI 驱动」:E 键交互直接打开 UICreatureJuicer 选目标魔物(多选),点击 Start 才进入本逻辑。</para>
/// <para>榨汁流程:关UI看场景演出 → 瓶子弹出 → 锤子3秒内落下3次(首锤亮血) → 锤子升起后镜头聚焦滴嘴 → 精华滴落入瓶 → 血液隐藏+镜头还原 → 奖励结算(消耗魔物+产出魔汁) → 重回魔汁机UI(可继续榨汁)。</para>
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
    /// <para>流程:锁UI保持魔汁机镜头 → 瓶子显示(液体隐藏) → 锤子3秒内落下3次(首锤落下后显示血液) →
    /// 锤子升起后镜头聚焦滴嘴 → 精华水滴坠入瓶子(液体显示) → 血液隐藏+镜头还原 → 奖励结算(消耗投入魔物,产出1个魔汁道具) → 重回魔汁机UI(玩家可继续榨汁)。</para>
    /// </summary>
    /// <param name="targets">被投入榨汁的魔物列表</param>
    public async void StartJuice(List<CreatureBean> targets)
    {
        //复制一份投入列表(调用方持有的是 UI 选中列表引用,后续可能被清空)
        targetCreatures = targets == null ? new List<CreatureBean>() : new List<CreatureBean>(targets);
        //抓取当前基地场景预制(榨汁动画作用在 objBuildingJuicer 上)
        scenePrefab = WorldHandler.Instance.GetCurrentScenePrefab<ScenePrefabForBase>(GameSceneTypeEnum.BaseGaming);
        if (scenePrefab == null || scenePrefab.objBuildingJuicer == null)
        {
            LogUtil.LogError("[魔汁机] 榨汁失败:找不到基地场景魔汁机建筑");
            return;
        }
        LogUtil.Log($"[魔汁机] 开始榨汁,投入魔物数量={targetCreatures.Count}");

        //1.锁UI+镜头:关闭全部UI只看场景演出,保持魔汁机镜头,关远景虚化
        UIHandler.Instance.CloseAllUI();
        VolumeHandler.Instance.SetDepthOfFieldActive(false);
        CameraHandler.Instance.SetJuicerCamera(int.MaxValue, true);

        //2.流程开始:瓶子弹出显示(液体隐藏),锤子归位
        scenePrefab.BuildingJuicerProcessBegin();
        await new WaitForSeconds(0.35f);

        //3.锤子阶段:3秒内落下3次再升起(首锤落下后显示血液,每锤机器抖动+砸击音+镜头震动)
        await scenePrefab.BuildingJuicerAnimForHammer();

        //4.镜头聚焦滴嘴(推近特写,等镜头就位)
        CameraHandler.Instance.FocusJuicerCameraOnHole(scenePrefab.GetBuildingJuicerHole());
        await new WaitForSeconds(0.9f);

        //5.精华滴落:水滴从滴嘴坠入瓶子,液体弹出显示
        await scenePrefab.BuildingJuicerAnimForEssenceDrop();
        await new WaitForSeconds(0.6f);

        //6.流程结束:血液隐藏+镜头还原
        scenePrefab.BuildingJuicerProcessEnd();
        CameraHandler.Instance.RestoreJuicerCameraFocus();
        await new WaitForSeconds(0.6f);

        //7.奖励结算:消耗投入魔物+产出魔汁+落盘(须先于重开UI,保证重开时读到最新存档)
        SettleJuiceReward();

        //重回魔汁机UI(注入退出回调,与场景E键入口一致:退出回 UIBaseMain);DoF 由 UI 的 OpenUI 自行关闭
        UIHandler.Instance.OpenUIAndCloseOther<UICreatureJuicer>(ui =>
            ui.actionForExit = () => UIHandler.Instance.OpenUIAndCloseOther<UIBaseMain>());
    }
    #endregion

    #region 奖励结算
    /// <summary>
    /// 榨汁奖励结算:消耗全部投入魔物(装备退回背包后移除),按每只魔物等级的 LevelInfo.juicer_exp 汇总经验,
    /// 生成 1 个魔汁道具(num_max=1 不堆叠,经验值存 ItemBean.juicerExp 实例字段)入背包,立即落盘并 Toast 提示。
    /// </summary>
    protected void SettleJuiceReward()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        long totalJuiceExp = 0;
        foreach (var creatureData in targetCreatures)
        {
            //按被榨汁魔物的等级累计榨汁经验(0~10级行均已配置,无配置兜底0)
            var levelInfo = LevelInfoCfg.GetItemData(creatureData.level);
            if (levelInfo != null)
                totalJuiceExp += levelInfo.juicer_exp;
            //装备退回背包后再移除魔物(与献祭消耗写法一致:背包+阵容双删)
            creatureData.RemoveAllEquipToBackpack();
            userData.RemoveBackpackCreature(creatureData);
        }
        //生成魔汁道具(单个不堆叠,经验按投入素材等级汇总)
        ItemBean juiceData = new ItemBean((long)ItemIdEnum.Juice, 1);
        juiceData.juicerExp = totalJuiceExp;
        userData.AddBackpackItem(juiceData);
        GameDataHandler.Instance.manager.SaveUserData();
        //榨汁成功提示(state=1 成功图标,绿色对勾)
        UIHandler.Instance.ToastHintText(string.Format(TextHandler.Instance.GetTextById(61016), totalJuiceExp), 1);
        LogUtil.Log($"[魔汁机] 榨汁结算完成:消耗魔物={targetCreatures.Count},获得魔汁经验={totalJuiceExp}");
    }
    #endregion
}
