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
    //本次榨汁已添加的魔晶颗数(仅界面预览,点 Start 开始榨汁时才一次性扣除)
    protected long crystalAddNum;
    //魔晶特效起点高度偏移(魔汁机上方,参照进阶界面 UICreatureVat 的加速光点)
    protected const float CrystalEffectStartOffsetY = 2.5f;
    //魔晶特效终点高度偏移(滴嘴附近)
    protected const float CrystalEffectEndOffsetY = 1.2f;

    #region 生命周期
    public override void Awake()
    {
        base.Awake();
        //长按回调只在此处注册一次(界面实例复用,放 OpenUI 会重复订阅导致一次长按成倍加颗数)
        if (ui_BtnAddCrystal_LongPressButton != null)
            ui_BtnAddCrystal_LongPressButton.AddLongEventAction(OnLongPressForAddCrystal);
        if (ui_BtnReduceCrystal_LongPressButton != null)
            ui_BtnReduceCrystal_LongPressButton.AddLongEventAction(OnLongPressForReduceCrystal);
    }

    public override void OpenUI()
    {
        base.OpenUI();
        //本界面实例复用:每次打开清零本次添加的魔晶颗数
        crystalAddNum = 0;
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
        RefreshCrystalAdd();
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
    /// 汇总已选魔物的榨汁经验(各级 LevelInfo.juicer_exp 累加,与 CreatureJuicerLogic.SettleJuiceReward 同口径,无配置兜底0)
    /// </summary>
    /// <returns>魔物等级折算的榨汁经验</returns>
    protected long GetCreatureJuiceExp()
    {
        return CreatureJuicerLogic.GetCreatureJuicerExp(listSelectCreature);
    }

    /// <summary>
    /// 汇总本次添加魔晶折算的榨汁经验(颗数 × 研究等级对应的每颗经验)
    /// </summary>
    /// <returns>魔晶折算的榨汁经验</returns>
    protected long GetCrystalJuiceExp()
    {
        return CreatureJuicerLogic.GetCrystalJuicerExp(crystalAddNum, GetCrystalExpPerCrystal());
    }

    /// <summary>
    /// 刷新魔汁水位与经验预览:按已选魔物 + 本次添加魔晶汇总榨汁经验(与 CreatureJuicerLogic.SettleJuiceReward 同口径)。
    /// <para>经验文本分两段:有魔晶时显示「+魔物经验（+魔晶经验）」;</para>
    /// <para>水色/水位按两者合计值计算(水色取该等级区间的 LevelInfo.level_color,水位 = 区间进度 × 0.95);</para>
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
        //魔物经验与魔晶经验分两段显示,水位按合计值计算
        long creatureJuiceExp = GetCreatureJuiceExp();
        long crystalJuiceExp = GetCrystalJuiceExp();
        long totalJuiceExp = creatureJuiceExp + crystalJuiceExp;
        ui_JuicerText.text = crystalJuiceExp > 0 ? $"+{creatureJuiceExp}（+{crystalJuiceExp}）" : $"+{creatureJuiceExp}";
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

    #region 魔晶加经验
    /// <summary>
    /// 获取每颗魔晶折算的榨汁经验(=「魔汁机榨汁加魔晶」研究等级,未解锁为0)
    /// </summary>
    /// <returns>每颗魔晶增加的榨汁经验</returns>
    protected int GetCrystalExpPerCrystal()
    {
        return GameDataHandler.Instance.manager.GetUserData().GetUserUnlockData().GetUnlockJuicerCrystalExp();
    }

    /// <summary>
    /// 刷新魔晶加减行:研究未解锁整行隐藏;已解锁刷新颗数文本,并按魔晶余额/下限设置两按钮可点状态
    /// </summary>
    public void RefreshCrystalAdd()
    {
        if (ui_CrystalAddRoot == null)
            return;
        bool isUnlock = GetCrystalExpPerCrystal() > 0;
        ui_CrystalAddRoot.gameObject.SetActive(isUnlock);
        if (!isUnlock)
            return;
        //添加上限=当前魔晶余额,减少下限=0(仅界面预览,真正扣费在点 Start 时)
        long crystal = GameDataHandler.Instance.manager.GetUserData().crystal;
        if (ui_CrystalAddNumText != null)
            ui_CrystalAddNumText.text = $"{crystalAddNum}";
        if (ui_BtnAddCrystal_Button != null)
            ui_BtnAddCrystal_Button.interactable = crystalAddNum < crystal;
        if (ui_BtnReduceCrystal_Button != null)
            ui_BtnReduceCrystal_Button.interactable = crystalAddNum > 0;
    }

    /// <summary>
    /// 长按添加魔晶(不逐次提示余额不足)
    /// </summary>
    public void OnLongPressForAddCrystal()
    {
        OnClickForAddCrystal(false);
    }

    /// <summary>
    /// 长按减少魔晶(不逐次提示)
    /// </summary>
    public void OnLongPressForReduceCrystal()
    {
        OnClickForReduceCrystal(false);
    }

    /// <summary>
    /// 添加 1 颗魔晶:只增加本次预览颗数(上限=当前魔晶余额),不立即扣费,点 Start 时统一扣除
    /// </summary>
    /// <param name="isHint">达到余额上限时是否提示(长按传 false 避免刷屏)</param>
    public void OnClickForAddCrystal(bool isHint = true)
    {
        if (!CheckCanChangeCrystal())
            return;
        long crystal = GameDataHandler.Instance.manager.GetUserData().crystal;
        if (crystalAddNum >= crystal)
        {
            if (isHint)
                UIHandler.Instance.ToastHintText(TextHandler.Instance.GetTextById(3000001));
            return;
        }
        crystalAddNum++;
        RefreshCrystalAdd();
        RefreshJuicerWater();
        PlayCrystalAddEffect(true);
    }

    /// <summary>
    /// 减少 1 颗魔晶:只减少本次预览颗数(下限0),减到 0 后不再变化
    /// </summary>
    /// <param name="isHint">已达 0 时是否提示(长按传 false 避免刷屏)</param>
    public void OnClickForReduceCrystal(bool isHint = true)
    {
        if (!CheckCanChangeCrystal() || crystalAddNum <= 0)
            return;
        crystalAddNum--;
        RefreshCrystalAdd();
        RefreshJuicerWater();
        PlayCrystalAddEffect(false);
    }

    /// <summary>
    /// 加减魔晶的统一前置校验:长按回调绕过点击门禁,锁屏/未解锁研究在此统一拦截
    /// </summary>
    /// <returns>允许改变颗数返回 true</returns>
    protected bool CheckCanChangeCrystal()
    {
        if (!UIHandler.Instance.manager.CanClickUIButtons)
            return false;
        return GetCrystalExpPerCrystal() > 0;
    }

    /// <summary>
    /// 播放魔晶飞入/飞出特效:添加=魔汁机上方飞向滴嘴,减少=滴嘴反向飞回上方
    /// <para>复用进阶界面的持久光点粒子(Effect_Move_1),连续触发即形成「魔晶持续加入」</para>
    /// </summary>
    /// <param name="isAdd">true=添加(飞入) false=减少(飞出)</param>
    protected void PlayCrystalAddEffect(bool isAdd)
    {
        if (scenePrefab == null || scenePrefab.objBuildingJuicer == null)
            return;
        Transform buildingJuicer = scenePrefab.objBuildingJuicer.transform;
        Vector3 startPosition = buildingJuicer.position + new Vector3(0, CrystalEffectStartOffsetY, 0);
        Transform juicerHole = scenePrefab.GetBuildingJuicerHole();
        Vector3 endPosition = juicerHole != null ? juicerHole.position : buildingJuicer.position + new Vector3(0, CrystalEffectEndOffsetY, 0);
        //减少时起终点对调,表现为魔晶从魔汁机飞出
        if (isAdd)
            EffectHandler.Instance.ShowCreatureAscendAddProgressEffect(1, startPosition, endPosition);
        else
            EffectHandler.Instance.ShowCreatureAscendAddProgressEffect(1, endPosition, startPosition);
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
        else if (viewButton == ui_BtnAddCrystal_Button)
        {
            OnClickForAddCrystal();
        }
        else if (viewButton == ui_BtnReduceCrystal_Button)
        {
            OnClickForReduceCrystal();
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
    /// 点击开始榨汁:校验至少投入一只魔物后交由 CreatureJuicerLogic 处理,并带上本次添加的魔晶颗数
    /// <para>魔晶在逻辑层「开始榨汁时」一次性扣除,加减阶段只做预览不扣费</para>
    /// </summary>
    public void OnClickForStart()
    {
        if (listSelectCreature.Count == 0)
        {
            //未投入任何魔物:提示并拦截
            UIHandler.Instance.ToastHintText(TextHandler.Instance.GetTextById(61010));
            return;
        }
        //交由逻辑层开始榨汁,并带上本次添加的魔晶颗数(逻辑层扣除并折算经验)
        GameHandler.Instance.StartCreatureJuicer(listSelectCreature, crystalAddNum);
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
