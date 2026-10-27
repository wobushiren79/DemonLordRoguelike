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
    //本次界面额外添加的魔晶颗数(开始榨汁时一次性扣除,按研究等级折算额外经验)
    public long addCrystalNum;
    //当前基地场景预制(榨汁动画作用于 scenePrefab.objBuildingJuicer)
    public ScenePrefabForBase scenePrefab;
    #endregion

    #region 经验公式
    /// <summary>
    /// 汇总投入魔物的榨汁经验(各级 LevelInfo.juicer_exp 累加,无配置兜底0)
    /// <para>UI 预览与结算共用本方法,避免两处口径分叉</para>
    /// </summary>
    /// <param name="creatures">投入榨汁的魔物列表</param>
    /// <returns>魔物等级折算的榨汁经验</returns>
    public static long GetCreatureJuicerExp(IReadOnlyList<CreatureBean> creatures)
    {
        if (creatures == null)
            return 0;
        long totalJuiceExp = 0;
        for (int i = 0; i < creatures.Count; i++)
        {
            var levelInfo = LevelInfoCfg.GetItemData(creatures[i].level);
            if (levelInfo != null)
                totalJuiceExp += levelInfo.juicer_exp;
        }
        return totalJuiceExp;
    }

    /// <summary>
    /// 计算魔晶折算的额外榨汁经验 = 颗数 × 每颗经验(=研究等级);任一项非正返回 0
    /// <para>UI 预览与结算共用本方法</para>
    /// </summary>
    /// <param name="crystalNum">本次添加的魔晶颗数</param>
    /// <param name="expPerCrystal">每颗魔晶折算的榨汁经验</param>
    /// <returns>魔晶折算的额外榨汁经验</returns>
    public static long GetCrystalJuicerExp(long crystalNum, int expPerCrystal)
    {
        if (crystalNum <= 0 || expPerCrystal <= 0)
            return 0;
        return crystalNum * expPerCrystal;
    }
    #endregion

    #region 榨汁流程
    /// <summary>
    /// 开始榨汁:记录投入魔物列表并抓取场景预制,随后进入榨汁流程。
    /// <para>流程:锁UI保持魔汁机镜头 → 瓶子显示(液体隐藏) → 锤子3秒内落下3次(首锤落下后显示血液) →
    /// 锤子升起后镜头聚焦滴嘴 → 精华水滴坠入瓶子(液体显示) → 血液隐藏+镜头还原 → 奖励结算(消耗投入魔物,产出1个魔汁道具) → 重回魔汁机UI(玩家可继续榨汁)。</para>
    /// </summary>
    /// <param name="targets">被投入榨汁的魔物列表</param>
    /// <param name="addCrystalNum">本次界面额外添加的魔晶颗数(0=未加魔晶);在开始榨汁时一次性扣除</param>
    public async void StartJuice(List<CreatureBean> targets, long addCrystalNum = 0)
    {
        //复制一份投入列表(调用方持有的是 UI 选中列表引用,后续可能被清空)
        targetCreatures = targets == null ? new List<CreatureBean>() : new List<CreatureBean>(targets);
        this.addCrystalNum = addCrystalNum;
        //抓取当前基地场景预制(榨汁动画作用在 objBuildingJuicer 上)
        scenePrefab = WorldHandler.Instance.GetCurrentScenePrefab<ScenePrefabForBase>(GameSceneTypeEnum.BaseGaming);
        if (scenePrefab == null || scenePrefab.objBuildingJuicer == null)
        {
            LogUtil.LogError("[魔汁机] 榨汁失败:找不到基地场景魔汁机建筑");
            return;
        }
        //开始榨汁时一次性扣除本次添加的魔晶(余额不足则拦截,UI 保持打开不进入演出;落盘随结算统一执行)
        if (this.addCrystalNum > 0)
        {
            UserDataBean userDataForPay = GameDataHandler.Instance.manager.GetUserData();
            //研究等级为0(未解锁/异常)时折算经验为0,直接放弃扣费防白扣
            if (userDataForPay.GetUserUnlockData().GetUnlockJuicerCrystalExp() <= 0)
            {
                this.addCrystalNum = 0;
            }
            else if (!userDataForPay.CheckHasCrystal(this.addCrystalNum, isHint: true, isAddCrystal: true))
            {
                this.addCrystalNum = 0;
                return;
            }
        }
        LogUtil.Log($"[魔汁机] 开始榨汁,投入魔物数量={targetCreatures.Count},添加魔晶={this.addCrystalNum}");

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
    /// 再加上本次添加魔晶折算的额外经验(每颗=研究等级点),
    /// 生成 1 个魔汁道具(num_max=1 不堆叠,经验值存 ItemBean.juicerExp 实例字段)入背包,立即落盘并 Toast 提示。
    /// </summary>
    protected void SettleJuiceReward()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        //魔物经验走公共公式(与 UI 预览同口径)
        long totalJuiceExp = GetCreatureJuicerExp(targetCreatures);
        foreach (var creatureData in targetCreatures)
        {
            //装备退回背包后再移除魔物(与献祭消耗写法一致:背包+阵容双删)
            creatureData.RemoveAllEquipToBackpack();
            userData.RemoveBackpackCreature(creatureData);
        }
        //加上本次添加魔晶折算的额外经验(每颗=「魔汁机榨汁加魔晶」研究等级点)
        long crystalJuiceExp = GetCrystalJuicerExp(addCrystalNum, userData.GetUserUnlockData().GetUnlockJuicerCrystalExp());
        totalJuiceExp += crystalJuiceExp;
        //生成魔汁道具(单个不堆叠,经验按投入素材等级汇总)
        ItemBean juiceData = new ItemBean((long)ItemIdEnum.Juice, 1);
        juiceData.juicerExp = totalJuiceExp;
        userData.AddBackpackItem(juiceData);
        GameDataHandler.Instance.manager.SaveUserData();
        //榨汁成功提示:图标改用魔汁道具图标(原为 state=1 的默认绿色对勾),取到图后再弹提示
        string hintContent = string.Format(TextHandler.Instance.GetTextById(61016), totalJuiceExp);
        IconHandler.Instance.GetItemIconSprite((long)ItemIdEnum.Juice, sprite =>
            UIHandler.Instance.ToastHint<ToastView>(sprite, Color.white, hintContent));
        LogUtil.Log($"[魔汁机] 榨汁结算完成:消耗魔物={targetCreatures.Count},添加魔晶={addCrystalNum}(额外经验={crystalJuiceExp}),获得魔汁经验={totalJuiceExp}");
    }
    #endregion
}
