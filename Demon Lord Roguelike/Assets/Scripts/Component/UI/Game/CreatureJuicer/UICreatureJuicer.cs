using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

public partial class UICreatureJuicer : BaseUIComponent
{
    //退出回调(由打开入口注入:场景E键交互返回UIBaseMain)
    public Action actionForExit;
    //当前展示的可榨汁魔物列表
    public List<CreatureBean> listCreatureData = new List<CreatureBean>();
    //当前已选中要投入榨汁的魔物(多选,上限由研究门控)
    public List<CreatureBean> listSelectCreature = new List<CreatureBean>();
    //魔汁机摄像头(CV_Juicer,打开时切换)
    public CinemachineCamera juicerCamera;
    //当前基地场景预制(投入/跳出动画作用于 scenePrefab.objBuildingJuicer)
    public ScenePrefabForBase scenePrefab;
    //魔汁水位材质实例(运行时克隆自 ui_JuicerWater 的共享材质,避免改动 Mat_UICreatureJuicer_Exp 资源本身)
    protected Material juicerWaterMatInstance;
    //水位上限(0~1):区间进度映射的水面最高位置,不顶到1留出容器边沿
    protected const float JuicerWaterLevelMax = 0.95f;
    //后层水色相对前层的暗化倍率(仿 Mat_UICreatureJuicer_Exp 已配置的前后层明暗比 ~0.6)
    protected const float JuicerWaterBackLayerRate = 0.6f;

    #region 生命周期
    public override void OpenUI()
    {
        base.OpenUI();
        //关闭基地移动控制(与其它基地子界面一致):避免榨汁界面期间仍能控制角色移动
        GameControlHandler.Instance.SetBaseControl(false);
        //切换魔汁机摄像头 + 关闭远景虚化(对准魔汁机建筑)
        juicerCamera = CameraHandler.Instance.SetJuicerCamera(int.MaxValue, true);
        VolumeHandler.Instance.SetDepthOfFieldActive(false);
        //抓取当前基地场景预制(魔物投入/跳出动画作用在 objBuildingJuicer 上)
        scenePrefab = WorldHandler.Instance.GetCurrentScenePrefab<ScenePrefabForBase>(GameSceneTypeEnum.BaseGaming);
        this.RegisterEvent<UIViewCreatureCardItem>(EventsInfo.UIViewCreatureCardItem_OnClickSelect, EventForCardClickSelect);
        InitCreatureData();
        RefreshUI();
    }

    public override void CloseUI()
    {
        base.CloseUI();
        ui_UIViewCreatureCardList_Target.CloseUI();
        //恢复远景虚化(基地镜头由返回 UIBaseMain 时统一还原)
        VolumeHandler.Instance.SetDepthOfFieldActive(true);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        //销毁运行时克隆的水位材质实例,防泄漏
        if (juicerWaterMatInstance != null)
            Destroy(juicerWaterMatInstance);
    }

    /// <summary>
    /// 刷新UI:重刷目标魔物列表卡片(选中态由 OnCellChangeForTarget 逐卡回填) + 计数文本 + 魔汁水位/经验预览
    /// </summary>
    public void RefreshUI()
    {
        ui_UIViewCreatureCardList_Target.RefreshAllCard();
        RefreshLimitText();
        RefreshJuicerWater();
    }
    #endregion

    #region 数据
    /// <summary>
    /// 初始化可榨汁魔物列表:取背包内空闲(未上阵、未被其它流程占用)的魔物,默认按等级降序排序。
    /// 复用进阶目标列表的卡片使用态(CreatureAscendTarget),与预制体已挂的卡片变体(UIViewCreatureCardItemForCreatureAscend)匹配。
    /// </summary>
    public void InitCreatureData()
    {
        listSelectCreature.Clear();
        listCreatureData.Clear();
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        userData.GetUserBackpackCreatureData().listBackpackCreature.ForEach((int index, CreatureBean creatureData) =>
        {
            //仅空闲态魔物可榨汁(排除进阶中/献祭中等占用态)
            if (creatureData.creatureState != CreatureStateEnum.Idle)
                return;
            //排除已上阵魔物
            if (userData.CheckIsInAnyLineup(creatureData.creatureUUId))
                return;
            listCreatureData.Add(creatureData);
        });
        //默认排序:等级降序(高→低)
        listCreatureData.Sort((a, b) => b.level.CompareTo(a.level));
        ui_UIViewCreatureCardList_Target.SetData(listCreatureData, CardUseStateEnum.CreatureAscendTarget, OnCellChangeForTarget);
    }

    /// <summary>
    /// 目标魔物卡片刷新:按是否已被选中(在投入列表内)设置选中/未选中样式
    /// </summary>
    public void OnCellChangeForTarget(int index, UIViewCreatureCardItem itemView, CreatureBean itemData)
    {
        if (listSelectCreature.Contains(itemData))
        {
            itemView.SetCardState(CardStateEnum.CreatureAscendSelect);
        }
        else
        {
            itemView.SetCardState(CardStateEnum.CreatureAscendNoSelect);
        }
    }

    /// <summary>
    /// 获取当前可投入魔物上限(基础5 + 投入上限研究等级 JuicerNum,满级15)。
    /// </summary>
    /// <returns>可投入选择的最大魔物数量</returns>
    protected int GetJuicerMax()
    {
        return GameDataHandler.Instance.manager.GetUserData().GetUserUnlockData().GetUnlockJuicerCreatureMax();
    }

    /// <summary>
    /// 刷新投入计数文本:格式「已选/上限」,达上限时数量转通用警示红(ColorUtil.WrapLimitFull)。
    /// </summary>
    public void RefreshLimitText()
    {
        if (ui_LimmitText == null)
            return;
        int juicerMax = GetJuicerMax();
        int selectCount = listSelectCreature.Count;
        ui_LimmitText.text = ColorUtil.WrapLimitFull($"{selectCount}/{juicerMax}", selectCount >= juicerMax);
    }
    #endregion

    #region 魔汁水位与经验预览
    /// <summary>
    /// 刷新魔汁水位与经验预览:按已选魔物汇总榨汁经验(各级 LevelInfo.juicer_exp 累加,与 CreatureJuicerLogic.SettleJuiceReward 同口径)。
    /// <para>水色:总经验达到某等级区间则取该等级 LevelInfo.level_color(经 GetLevelColor,0级为白);</para>
    /// <para>水位:= 当前等级区间进度(0~1) × 0.95(最高不顶到1);</para>
    /// <para>一个素材都没选时隐藏水位与经验文本。</para>
    /// </summary>
    public void RefreshJuicerWater()
    {
        if (ui_JuicerWater == null || ui_JuicerText == null)
            return;
        //一个素材都没选:隐藏水位与经验文本
        if (listSelectCreature.Count == 0)
        {
            ui_JuicerWater.gameObject.SetActive(false);
            ui_JuicerText.gameObject.SetActive(false);
            return;
        }
        ui_JuicerWater.gameObject.SetActive(true);
        ui_JuicerText.gameObject.SetActive(true);
        //汇总本次投入魔物的榨汁经验(无配置兜底0)
        long totalJuiceExp = 0;
        foreach (var creatureData in listSelectCreature)
        {
            var levelInfo = LevelInfoCfg.GetItemData(creatureData.level);
            if (levelInfo != null)
                totalJuiceExp += levelInfo.juicer_exp;
        }
        ui_JuicerText.text = $"+{totalJuiceExp}";
        //求总经验所处等级:取 juicer_exp <= 总经验 的最高等级(0级区间下限按0计,保证最低投入也有水位)
        int levelMax = 0;
        foreach (var key in LevelInfoCfg.GetAllData().Keys)
        {
            if (key > levelMax)
                levelMax = (int)key;
        }
        int curLevel = 0;
        for (int level = 1; level <= levelMax; level++)
        {
            var levelData = LevelInfoCfg.GetItemData(level);
            if (levelData == null || totalJuiceExp < levelData.juicer_exp)
                break;
            curLevel = level;
        }
        //区间进度:(总经验-当前级下限)/(下一级-当前级下限);无下一级=已达最高级,进度拉满
        long rangeStart = curLevel == 0 ? 0 : LevelInfoCfg.GetItemData(curLevel).juicer_exp;
        var nextLevelData = LevelInfoCfg.GetItemData(curLevel + 1);
        float progress = 1;
        if (nextLevelData != null)
        {
            long rangeLength = nextLevelData.juicer_exp - rangeStart;
            progress = rangeLength > 0 ? Mathf.Clamp01((float)(totalJuiceExp - rangeStart) / rangeLength) : 0;
        }
        //懒克隆水位材质实例(避免运行时改共享材质资源,与 UIBaseResearch 连线材质同写法)
        if (juicerWaterMatInstance == null)
        {
            juicerWaterMatInstance = new Material(ui_JuicerWater.material);
            ui_JuicerWater.material = juicerWaterMatInstance;
        }
        //水位=区间进度映射到 0~0.95;水色=当前等级颜色,后层按比例暗化保持前后层视差(材质为2层水)
        juicerWaterMatInstance.SetFloat("_WaterLevel", progress * JuicerWaterLevelMax);
        Color waterColor = LevelInfoCfg.GetLevelColor(curLevel);
        Color backLayerColor = waterColor * JuicerWaterBackLayerRate;
        backLayerColor.a = waterColor.a;
        juicerWaterMatInstance.SetColor("_LayerColor1", waterColor);
        juicerWaterMatInstance.SetColor("_LayerColor2", backLayerColor);
    }
    #endregion

    #region 点击事件
    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_ViewExit)
        {
            OnClickForExit();
        }
        else if (viewButton == ui_BtnStart)
        {
            OnClickForStart();
        }
    }

    /// <summary>
    /// 点击离开:执行由打开入口注入的退出回调(场景E键交互返回UIBaseMain)
    /// </summary>
    public void OnClickForExit()
    {
        actionForExit?.Invoke();
    }

    /// <summary>
    /// 点击开始榨汁:校验至少投入一只魔物后交由 CreatureJuicerLogic 处理(榨汁流程/奖励后续接入)
    /// </summary>
    public void OnClickForStart()
    {
        if (listSelectCreature.Count == 0)
        {
            //未投入任何魔物:提示并拦截
            UIHandler.Instance.ToastHintText(TextHandler.Instance.GetTextById(61010));
            return;
        }
        //交由逻辑层开始榨汁(当前为留桩,后续接入榨汁流程与奖励)
        GameHandler.Instance.StartCreatureJuicer(listSelectCreature);
    }
    #endregion

    #region 事件
    /// <summary>
    /// 目标魔物选择(多选:再次点击已选魔物则移出;新增时超过投入上限则提示并拦截)
    /// <para>选中:播魔物跳入魔汁机动画;取消:播魔物跳出魔汁机动画(作用于场景 objBuildingJuicer)</para>
    /// </summary>
    public void EventForCardClickSelect(UIViewCreatureCardItem selectItemView)
    {
        var selectCreatureData = selectItemView.cardData.creatureData;
        if (listSelectCreature.Contains(selectCreatureData))
        {
            //再次点击已选魔物:移出投入列表 + 播跳出动画
            listSelectCreature.Remove(selectCreatureData);
            scenePrefab.BuildingJuicerAnimForCreatureJumpOut(selectCreatureData);
        }
        else
        {
            //投入数量达到上限(基础5+投入上限研究等级)则拒绝并提示
            int juicerMax = GetJuicerMax();
            if (listSelectCreature.Count >= juicerMax)
            {
                UIHandler.Instance.ToastHintText(string.Format(TextHandler.Instance.GetTextById(61012), juicerMax));
            }
            else
            {
                listSelectCreature.Add(selectCreatureData);
                //播投入动画(入机瞬间机器抖动+入汁音效)
                scenePrefab.BuildingJuicerAnimForCreatureJumpIn(selectCreatureData);
            }
        }
        ui_UIViewCreatureCardList_Target.RefreshAllCard();
        //RefreshUI 内含卡片刷新,此处只需补计数文本与魔汁水位/经验预览
        RefreshLimitText();
        RefreshJuicerWater();
    }
    #endregion
}
