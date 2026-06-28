
using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public partial class UICreatureVat : BaseUIComponent
{
    //初始可选素材魔物数量上限
    public const int MaterialMax = 5;

    //当前容器序号
    public int currentIndexVat;
    //场景预制
    public ScenePrefabForBase scenePrefab;
    //摄像头
    public CinemachineCamera vatCamera;

    //当前目标生物
    public List<CreatureBean> listTargetCreatureShow = new List<CreatureBean>();
    public List<CreatureBean> listMaterialCreatureShow = new List<CreatureBean>();

    //当前选中的生物
    public CreatureBean targetCreatureSelect;
    public List<CreatureBean> listMaterialCreatureSelect = new List<CreatureBean>();

    //用户进阶数据
    protected UserAscendDetailsBean userAscendDetails;
    protected Transform targetVat;

    //进阶BUFF增益item缓存(实时生成、按需复用)
    protected List<UIViewCreatureVatAscendBuffItem> listAscendBuffItem = new List<UIViewCreatureVatAscendBuffItem>();
    //BUFF增益item一排最多显示数量
    protected const int AscendBuffMaxPerRow = 5;
    //BUFF增益item横向格距(item宽150 + 间隔)
    protected const float AscendBuffCellWidth = 162f;
    //BUFF增益item纵向格距(item高60 + 间隔)
    protected const float AscendBuffCellHeight = 70f;

    //升阶前/后卡牌的初始锚点位置与缩放(用于掉落动画的落点,Awake时缓存)
    protected Vector2 advanceCardBeforePos;
    protected Vector2 advanceCardAfterPos;
    protected Vector3 advanceCardBeforeScale = Vector3.one;
    protected Vector3 advanceCardAfterScale = Vector3.one;

    public override void Awake()
    {
        base.Awake();
        ui_BtnAddProgress_LongPressButton.AddLongEventAction(OnClickLongForAddProgress);
        //缓存升阶前/后卡牌初始位置与缩放(掉落动画的落点)
        if (ui_UIViewCreatureCardItem_BeforeAscend != null)
        {
            advanceCardBeforePos = ui_UIViewCreatureCardItem_BeforeAscend.rectTransform.anchoredPosition;
            advanceCardBeforeScale = ui_UIViewCreatureCardItem_BeforeAscend.rectTransform.localScale;
        }
        if (ui_UIViewCreatureCardItem_AfterAscend != null)
        {
            advanceCardAfterPos = ui_UIViewCreatureCardItem_AfterAscend.rectTransform.anchoredPosition;
            advanceCardAfterScale = ui_UIViewCreatureCardItem_AfterAscend.rectTransform.localScale;
        }
        //BUFF增益item模板仅作原型,隐藏不直接展示(展示项全部用克隆)
        if (ui_UIViewCreatureVatAscendBuffItem != null)
            ui_UIViewCreatureVatAscendBuffItem.gameObject.SetActive(false);
    }

    public override void OpenUI()
    {
        base.OpenUI();

        this.RegisterEvent<UIViewCreatureCardItem>(EventsInfo.UIViewCreatureCardItem_OnClickSelect, EventForCardClickSelect);
        this.RegisterEvent(EventsInfo.CreatureAscend_AddProgress, EventForRefreshVatProgress);

        //场景实例
        scenePrefab = WorldHandler.Instance.GetCurrentScenePrefab<ScenePrefabForBase>(GameSceneTypeEnum.BaseGaming);
        //获取摄像头
        GameControlHandler.Instance.SetBaseControl(false);
        vatCamera = CameraHandler.Instance.SetCreatureVatCamera(int.MaxValue, true);
        //关闭远景
        VolumeHandler.Instance.SetDepthOfFieldActive(false);
        //设置数据
        SetCurrentVat(0);
        RefreshVatState();
        RefreshVatProgress();
    }

    public override void CloseUI()
    {
        base.CloseUI();
        listTargetCreatureShow.Clear();
        listMaterialCreatureShow.Clear();
        ui_UIViewCreatureCardList_Target.CloseUI();
        ui_UIViewCreatureCardList_Material.CloseUI();
        //隐藏所有进阶BUFF增益item
        HideAllAscendBuffItems();
        //设置展示vat
        scenePrefab.BuildingVatShow(-1);
        //关闭远景
        VolumeHandler.Instance.SetDepthOfFieldActive(true);
    }

    /// <summary>
    /// 设置容器状态
    /// </summary>
    public void RefreshVatState(bool hasAnim = false)
    {
        ui_BtnStart.gameObject.SetActive(false);
        ui_BtnEnd.gameObject.SetActive(false);
        ui_BtnComplete.gameObject.SetActive(false);
        ui_BtnAddProgress_Button.gameObject.SetActive(false);
        AnimForListShow(ui_UIViewCreatureCardList_Target.transform, false, hasAnim);
        AnimForListShow(ui_UIViewCreatureCardList_Material.transform, false, hasAnim);

        if (userAscendDetails != null)
        {
            //是否已经完成(进度达到按稀有度的总时长)
            if (userAscendDetails.IsComplete())
            {
                ui_BtnComplete.gameObject.SetActive(true);
            }
            else
            {
                ui_BtnEnd.gameObject.SetActive(true);
                ui_BtnAddProgress_Button.gameObject.SetActive(true);
                ui_BtnAddProgressText.text = string.Format(TextHandler.Instance.GetTextById(80009), 1);
            }
        }
        //如果没有数据
        else
        {
            ui_BtnStart.gameObject.SetActive(true);
            ui_UIViewCreatureCardList_Material.gameObject.SetActive(true);
            AnimForListShow(ui_UIViewCreatureCardList_Target.transform, true, false);
        }
        //刷新进阶详情(进度内容/进阶详情的显隐随阶段切换)
        RefreshAscendData();
    }

    /// <summary>
    /// 刷新进度
    /// </summary>
    public void RefreshVatProgress()
    {
        //进度按「秒/总时长」归一化为0~1(总时长按源稀有度查表)
        float progressNormalized = 0;
        if (userAscendDetails != null)
        {
            progressNormalized = userAscendDetails.GetProgressNormalized();
            //刚好达成则刷新按钮状态(显示完成)
            if (userAscendDetails.IsComplete())
            {
                RefreshVatState();
            }
        }
        ui_ProgressText.text = $"{MathUtil.GetPercentage(progressNormalized, 2)}%";
        ui_Progress.fillAmount = progressNormalized;
    }

    /// <summary>
    /// 设置当前容器数据
    /// </summary>
    public void SetCurrentVat(int indexVat)
    {
        targetCreatureSelect = null;
        listMaterialCreatureSelect.Clear();

        currentIndexVat = indexVat;

        //检测是否有数据
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        UserAscendBean userAscend = userData.GetUserAscendData();
        userAscendDetails = userAscend.GetAscendData(currentIndexVat);
        //获取容器模型
        targetVat = scenePrefab.objBuildingVat.transform.GetChild(indexVat);

        vatCamera.Follow = targetVat;
        vatCamera.LookAt = targetVat;

        InitCreaturekDataForTarget();

        //设置展示vat
        scenePrefab.BuildingVatShow(currentIndexVat);
    }

    /// <summary>
    /// 初始化生物数据
    /// </summary>
    public void InitCreaturekDataForTarget()
    {
        listTargetCreatureShow.Clear();
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        userData.GetUserBackpackCreatureData().listBackpackCreature.ForEach((int index, CreatureBean creatureData) =>
        {
            if (creatureData.creatureState != CreatureStateEnum.Idle)
                return;
            //排除已满级(进阶耗时<=0,即 L)不可进阶的目标
            if (RarityInfoCfg.GetAscendTimeByRarity(creatureData.rarity) <= 0)
                return;
            listTargetCreatureShow.Add(creatureData);
        });
        ui_UIViewCreatureCardList_Target.SetData(listTargetCreatureShow, CardUseStateEnum.CreatureAscendTarget, OnCellChangeForBackpackCreatureTarget);
    }

    /// <summary>
    /// 初始化生物数据
    /// </summary>
    public void InitCreaturekDataForMaterial()
    {
        listMaterialCreatureShow.Clear();
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        //目标稀有度(rarity<=0 视为 N),素材只能选比目标更高稀有度的魔物
        int targetRarity = targetCreatureSelect.GetRarityValue();
        userData.GetUserBackpackCreatureData().listBackpackCreature.ForEach((int index, CreatureBean creatureData) =>
        {
            if (creatureData == targetCreatureSelect)
                return;
            if (creatureData.creatureState != CreatureStateEnum.Idle)
                return;
            //排除上阵魔物
            if (userData.CheckIsInAnyLineup(creatureData.creatureUUId))
                return;
            //仅保留比目标更高稀有度的魔物
            int materialRarity = creatureData.GetRarityValue();
            if (materialRarity <= targetRarity)
                return;
            listMaterialCreatureShow.Add(creatureData);
        });
        ui_UIViewCreatureCardList_Material.SetData(listMaterialCreatureShow, CardUseStateEnum.CreatureAscendMaterial, OnCellChangeForBackpackCreatureMaterial);
    }

    /// <summary>
    /// 背包生物列表变化
    /// </summary>
    public void OnCellChangeForBackpackCreatureTarget(int index, UIViewCreatureCardItem itemView, CreatureBean itemData)
    {
        if (targetCreatureSelect == itemData)
        {
            itemView.SetCardState(CardStateEnum.CreatureAscendSelect);
        }
        else
        {
            itemView.SetCardState(CardStateEnum.CreatureAscendNoSelect);
        }
    }


    /// <summary>
    /// 背包生物列表变化
    /// </summary>
    public void OnCellChangeForBackpackCreatureMaterial(int index, UIViewCreatureCardItem itemView, CreatureBean itemData)
    {
        if (listMaterialCreatureSelect.Contains(itemData))
        {
            itemView.SetCardState(CardStateEnum.CreatureAscendSelect);
        }
        else
        {
            itemView.SetCardState(CardStateEnum.CreatureAscendNoSelect);
        }
    }


    #region  点击相关
    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_ViewExit)
        {
            OnClickForExit();
        }
        else if (viewButton == ui_BtnLeft)
        {
            OnClickForChangeVat(-1);
        }
        else if (viewButton == ui_BtnRight)
        {
            OnClickForChangeVat(1);
        }
        else if (viewButton == ui_BtnStart)
        {
            OnClickForStart();
        }
        else if (viewButton == ui_BtnEnd)
        {
            OnClickForEnd();
        }
        else if (viewButton == ui_BtnComplete)
        {
            OnClickForComplete();
        }
        else if(viewButton == ui_BtnAddProgress_Button)
        {
            OnClickForAddProgress();
        }
    }

    /// <summary>
    /// 事件-长按增加进度
    /// </summary>
    public void OnClickLongForAddProgress()
    {
        OnClickForAddProgress(isHintEnoughCrystal: false);
    }

    /// <summary>
    /// 点击增加进度
    /// </summary>
    public void OnClickForAddProgress(bool isHintEnoughCrystal = true)
    {
        if (targetVat == null || userAscendDetails == null)
            return;
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        int payCrystal = 1;
        //检测水晶是否足够
        if (userData.CheckHasCrystal(payCrystal, isHintEnoughCrystal, true))
        {
            //每颗魔晶加速1秒(进度+1秒);培养过程不主动存档
            userAscendDetails.AddProgress(1f);
            EventHandler.Instance.TriggerEvent(EventsInfo.CreatureAscend_AddProgress);

            Vector3 startPosition = targetVat.transform.position + new Vector3(0, 2.5f, 0);
            Vector3 endPosition = targetVat.transform.position + new Vector3(0, 1.2f, 0);
            EffectHandler.Instance.ShowCreatureAscendAddProgressEffect(payCrystal, startPosition, endPosition);       
        }
    }

    /// <summary>
    /// 点击开始
    /// </summary>
    public void OnClickForStart()
    {
        if (targetCreatureSelect == null)
        {
            string hintStr = TextHandler.Instance.GetTextById(80008);
            UIHandler.Instance.ToastHintText(hintStr);
            return;
        }
        //未选择素材魔物则提示并拦截
        if (listMaterialCreatureSelect == null || listMaterialCreatureSelect.Count == 0)
        {
            UIHandler.Instance.ToastHintText(TextHandler.Instance.GetTextById(80012));
            return;
        }
        DialogBean dialogData = new DialogBean();
        string materialCreatureName = "";
        for (int i = 0; i < listMaterialCreatureSelect.Count; i++)
        {
            materialCreatureName += $"{listMaterialCreatureSelect[i].creatureName} ";
        }
        dialogData.content = string.Format(TextHandler.Instance.GetTextById(80010), materialCreatureName, targetCreatureSelect.creatureName);
        dialogData.actionSubmit = (view, data) =>
        {
            //先关闭UI
            UIHandler.Instance.ShowScreenLock();
            gameObject.SetActive(false);
            Action actionForAnimEnd = () =>
            {
                UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
                UserAscendBean userAscend = userData.GetUserAscendData();

                //升阶后稀有度复用 GetTargetNewRarity();耗时按源稀有度(=newRarity-1)查表;预定BUFF开始时即确定(含素材概率加成)
                int newRarity = GetTargetNewRarity();
                float timeMax = RarityInfoCfg.GetAscendTimeByRarity(newRarity - 1);
                BuffBean ascendBuff = BuffUtil.CreateAscendRarityBuff((RarityEnum)newRarity, listMaterialCreatureSelect);

                for (int i = 0; i < listMaterialCreatureSelect.Count; i++)
                {
                    var itemCreatureData = listMaterialCreatureSelect[i];
                    //脱掉所有素材生物的装备
                    itemCreatureData.RemoveAllEquipToBackpack();
                    //删除素材生物
                    userData.RemoveBackpackCreature(itemCreatureData);
                }
                //设置生物状态
                targetCreatureSelect.creatureState = CreatureStateEnum.Vat;
                //写入临时进阶数据(预定BUFF/目标稀有度/耗时上限)
                userAscendDetails = userAscend.AddAscendData(currentIndexVat, targetCreatureSelect, newRarity, timeMax, ascendBuff);
                //开始进阶保存一次
                GameDataHandler.Instance.manager.SaveUserData();
                //解锁UI锁定
                UIHandler.Instance.HideScreenLock();
                gameObject.SetActive(true);
                //刷新状态
                RefreshVatState();
                RefreshVatProgress();
            };
            scenePrefab.BuildingVatAnimForStart(targetVat, targetCreatureSelect, listMaterialCreatureSelect, actionForAnimEnd);
        };
        UIHandler.Instance.ShowDialogNormal(dialogData);
    }

    /// <summary>
    /// 点击结束
    /// </summary>
    public void OnClickForEnd()
    {
        DialogBean dialogData = new DialogBean();
        dialogData.content = TextHandler.Instance.GetTextById(80004);
        dialogData.actionSubmit = (voew, data) =>
        {
            //设置数据
            UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
            UserAscendBean userAscend = userData.GetUserAscendData();
            userAscend.RemoveAscendData(currentIndexVat);
            userAscendDetails = null;
            //设置状态
            scenePrefab.BuildingVatSetState(targetVat, 1, targetCreatureSelect);
            //刷新状态
            RefreshVatState();
            RefreshVatProgress();
        };
        UIHandler.Instance.ShowDialogNormal(dialogData);
    }

    /// <summary>
    /// 点击完成
    /// </summary>
    public void OnClickForComplete()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        UserAscendBean userAscend = userData.GetUserAscendData();
        //把临时进阶数据落地到生物正式数据:升稀有度 + 授予预定BUFF(按目标稀有度槽位覆盖)
        if (userAscendDetails != null)
        {
            var targetCreature = userData.GetBackpackCreature(userAscendDetails.creatureUUId);
            if (targetCreature != null)
            {
                targetCreature.rarity = userAscendDetails.targetRarity;
                if (userAscendDetails.ascendBuff != null)
                {
                    targetCreature.dicRarityBuff[(RarityEnum)userAscendDetails.targetRarity] = userAscendDetails.ascendBuff;
                }
            }
        }
        //移除临时数据(复位生物为Idle)
        userAscend.RemoveAscendData(currentIndexVat);
        userAscendDetails = null;
        //完成进阶保存一次(把落地后的正式数据写盘并清除临时数据)
        GameDataHandler.Instance.manager.SaveUserData();
        //设置状态
        scenePrefab.BuildingVatSetState(targetVat,0, null);
        RefreshVatState();
        RefreshVatProgress();
    }

    /// <summary>
    /// 点击离开
    /// </summary>
    public void OnClickForExit()
    {
        UIHandler.Instance.OpenUIAndCloseOther<UIBaseCore>();
    }

    /// <summary>
    /// 点击切换容器
    /// </summary>
    public void OnClickForChangeVat(int changeType)
    {
        Transform targetVatOld = scenePrefab.objBuildingVat.transform.GetChild(currentIndexVat);
        float distance = float.MaxValue;
        int targetIndex = -1;
        //查询最近的容器
        for (int i = 0; i < scenePrefab.objBuildingVat.transform.childCount; i++)
        {
            var itemVat = scenePrefab.objBuildingVat.transform.GetChild(i);
            //容器可以使用 并且不是自身
            if (itemVat.gameObject.activeSelf && itemVat != targetVatOld)
            {
                //判断左右
                //如果是左选 但是目标在右边 则不处理
                if (changeType == -1 && itemVat.position.x > targetVatOld.position.x)
                {
                    continue;
                }

                //如果是右选 但是目标在左边 则不处理
                if (changeType == 1 && itemVat.position.x < targetVatOld.position.x)
                {
                    continue;
                }

                float tempDis = Vector3.Distance(itemVat.position, targetVatOld.position);
                if (tempDis < distance)
                {
                    distance = tempDis;
                    targetIndex = i;
                }
            }
        }
        if (targetIndex != -1)
        {
            SetCurrentVat(targetIndex);
        }
        RefreshVatState();
        RefreshVatProgress();
    }
    #endregion

    #region 事件
    /// <summary>
    /// 刷新进度
    /// </summary>
    public void EventForRefreshVatProgress()
    {
        if (userAscendDetails != null)
        {
            RefreshVatProgress();
        }
    }

    /// <summary>
    /// 选择
    /// </summary>
    public void EventForCardClickSelect(UIViewCreatureCardItem selectItemView)
    {
        var selectCreatureData = selectItemView.cardData.creatureData;
        //目标选择
        if (selectItemView.cardData.cardUseState == CardUseStateEnum.CreatureAscendTarget)
        {
            listMaterialCreatureSelect.Clear();
            if (selectItemView.cardData.cardState == CardStateEnum.CreatureAscendSelect)
            {
                //取消选择
                if (targetCreatureSelect != null && targetCreatureSelect == selectCreatureData)
                {
                    targetCreatureSelect = null;
                    AnimForListShow(ui_UIViewCreatureCardList_Material.transform, false, true);
                    scenePrefab.BuildingVatSetState(targetVat, 1, null);
                }
            }
            else
            {
                //切换目标魔物
                AnimForListShow(ui_UIViewCreatureCardList_Material.transform, true, true);
                targetCreatureSelect = selectCreatureData;
                scenePrefab.BuildingVatSetState(targetVat, 2, selectCreatureData);
                //初始化材料
                InitCreaturekDataForMaterial();
            }
            ui_UIViewCreatureCardList_Target.RefreshAllCard();
            //目标变化:刷新进阶详情(显隐+前后卡牌+BUFF增益)
            RefreshAscendData();
        }
        //材料选择
        else if (selectItemView.cardData.cardUseState == CardUseStateEnum.CreatureAscendMaterial)
        {
            if (selectItemView.cardData.cardState == CardStateEnum.CreatureAscendSelect)
            {
                if (listMaterialCreatureSelect.Contains(selectCreatureData))
                {
                    listMaterialCreatureSelect.Remove(selectCreatureData);
                }
            }
            else
            {
                if (!listMaterialCreatureSelect.Contains(selectCreatureData))
                {
                    //素材数量达到上限则拒绝并提示
                    if (listMaterialCreatureSelect.Count >= MaterialMax)
                    {
                        UIHandler.Instance.ToastHintText(string.Format(TextHandler.Instance.GetTextById(80011), MaterialMax));
                    }
                    else
                    {
                        listMaterialCreatureSelect.Add(selectCreatureData);
                    }
                }
            }
            ui_UIViewCreatureCardList_Material.RefreshAllCard();
            //素材变化:仅刷新BUFF增益概率展示(前后卡牌不变)
            RefreshAscendBuffs();
        }
    }
    #endregion

    #region 进阶详情相关
    /// <summary>
    /// 刷新进阶详情:按阶段切换 ProgressContent/AscendData 显隐,并在素材选择阶段+已选目标时填充前后卡牌与BUFF增益。
    /// <para>素材选择阶段(userAscendDetails==null):隐藏 ProgressContent;仅当已选目标魔物时显示 AscendData。</para>
    /// <para>培养阶段(userAscendDetails!=null):显示 ProgressContent,隐藏 AscendData。</para>
    /// </summary>
    public void RefreshAscendData()
    {
        //素材选择阶段 = 当前容器没有进行中的进阶数据
        bool isSelectingPhase = userAscendDetails == null;
        //进阶详情仅在「素材选择阶段 且 已选目标魔物」时展示
        bool showAscend = isSelectingPhase && targetCreatureSelect != null;

        //进度内容只在培养阶段显示
        if (ui_ProgressContent != null)
            ui_ProgressContent.gameObject.SetActive(!isSelectingPhase);
        if (ui_AscendData != null)
            ui_AscendData.gameObject.SetActive(showAscend);

        if (!showAscend)
        {
            //不展示时清空BUFF增益item
            HideAllAscendBuffItems();
            return;
        }
        //填充升阶前后卡牌(带掉落动画)
        RefreshAscendCards();
        //填充BUFF增益概率
        RefreshAscendBuffs();
    }

    /// <summary>
    /// 获取目标魔物升阶后的稀有度(源稀有度+1,源 rarity<=0 视为 N)。
    /// </summary>
    /// <returns>升阶后稀有度值;无目标返回 0</returns>
    protected int GetTargetNewRarity()
    {
        if (targetCreatureSelect == null)
            return 0;
        return targetCreatureSelect.GetRarityValue() + 1;
    }

    /// <summary>
    /// 刷新升阶前/后两张生物卡牌(均关闭popup详情),并播放从上而下的掉落出现动画。
    /// </summary>
    public void RefreshAscendCards()
    {
        if (targetCreatureSelect == null)
            return;
        int newRarity = GetTargetNewRarity();
        //升阶前:目标魔物当前数据(关闭popup)
        if (ui_UIViewCreatureCardItem_BeforeAscend != null)
        {
            ui_UIViewCreatureCardItem_BeforeAscend.SetData(targetCreatureSelect, CardUseStateEnum.ShowNoPopup);
            PlayCardDropIn(ui_UIViewCreatureCardItem_BeforeAscend, advanceCardBeforePos, advanceCardBeforeScale, 0f);
        }
        //升阶后:目标魔物的升阶预览(稀有度+1,关闭popup)
        if (ui_UIViewCreatureCardItem_AfterAscend != null)
        {
            CreatureBean afterPreview = BuildAscendPreviewCreature(targetCreatureSelect, newRarity);
            ui_UIViewCreatureCardItem_AfterAscend.SetData(afterPreview, CardUseStateEnum.ShowNoPopup);
            PlayCardDropIn(ui_UIViewCreatureCardItem_AfterAscend, advanceCardAfterPos, advanceCardAfterScale, 0.08f);
        }
    }

    /// <summary>
    /// 刷新BUFF增益概率展示:按进阶规则计算各BUFF命中概率,池化生成/复用item并布局+动画。
    /// </summary>
    public void RefreshAscendBuffs()
    {
        if (targetCreatureSelect == null)
            return;
        int newRarity = GetTargetNewRarity();
        //按进阶BUFF生成规则计算概率(素材BUFF在前,随机增益兜底在后)
        List<CreatureAscendBuffChanceStruct> listChance = BuffUtil.GetCreatureAscendBuffChances((RarityEnum)newRarity, listMaterialCreatureSelect);
        //rarity 传升阶后稀有度:本批候选BUFF均在该稀有度槽位生成,用于子项稀有度配色
        SetAscendBuffItems(listChance, newRarity);
    }

    /// <summary>
    /// 按概率列表池化设置BUFF增益item:复用/克隆、实时布局(一排最多5个,后续y轴下移)、出现/消失/移动动画。
    /// </summary>
    /// <param name="listData">各BUFF命中概率</param>
    /// <param name="rarity">升阶后稀有度(子项名字/BG的稀有度配色)</param>
    protected void SetAscendBuffItems(List<CreatureAscendBuffChanceStruct> listData, int rarity)
    {
        int count = listData != null ? listData.Count : 0;
        //生成/复用展示项
        for (int i = 0; i < count; i++)
        {
            UIViewCreatureVatAscendBuffItem itemView;
            if (i < listAscendBuffItem.Count)
            {
                itemView = listAscendBuffItem[i];
            }
            else
            {
                //不足则克隆模板(模板作原型,本体保持隐藏)
                GameObject itemObj = Instantiate(ui_UIViewCreatureVatAscendBuffItem.gameObject, ui_AscendBuffs);
                itemView = itemObj.GetComponent<UIViewCreatureVatAscendBuffItem>();
                listAscendBuffItem.Add(itemView);
            }
            var itemData = listData[i];
            Vector2 targetPos = GetAscendBuffItemPosition(i, count);
            //已显示则平滑移动,新出现则定位后播放掉落出现动画
            bool wasActive = itemView.gameObject.activeSelf;
            itemView.SetData(itemData, rarity);
            if (wasActive)
            {
                itemView.MoveTo(targetPos);
            }
            else
            {
                itemView.rectTransform.anchoredPosition = targetPos;
                itemView.PlayAppear();
            }
        }
        //多余项播放消失动画
        for (int i = count; i < listAscendBuffItem.Count; i++)
        {
            listAscendBuffItem[i].PlayDisappear();
        }
    }

    /// <summary>
    /// 计算第 index 个BUFF增益item的锚点位置(一排最多5个并水平居中,超出则换行 y 轴下移)。
    /// </summary>
    /// <param name="index">item序号(0基)</param>
    /// <param name="total">item总数</param>
    /// <returns>相对 AscendBuffs 容器的锚点位置</returns>
    protected Vector2 GetAscendBuffItemPosition(int index, int total)
    {
        int row = index / AscendBuffMaxPerRow;
        int col = index % AscendBuffMaxPerRow;
        //本行实际item数(末行可能不足5个),用于水平居中
        int rowCount = Mathf.Min(AscendBuffMaxPerRow, total - row * AscendBuffMaxPerRow);
        float x = (col - (rowCount - 1) / 2f) * AscendBuffCellWidth;
        float y = -row * AscendBuffCellHeight;
        return new Vector2(x, y);
    }

    /// <summary>
    /// 立即隐藏所有BUFF增益item(无动画)
    /// </summary>
    protected void HideAllAscendBuffItems()
    {
        for (int i = 0; i < listAscendBuffItem.Count; i++)
        {
            listAscendBuffItem[i].HideImmediately();
        }
    }

    /// <summary>
    /// 构造升阶后预览生物:复制目标关键展示字段、稀有度替换为升阶后值,引用型字段共享(仅只读展示,不改原数据)。
    /// </summary>
    /// <param name="source">源目标魔物</param>
    /// <param name="newRarity">升阶后稀有度</param>
    /// <returns>仅用于卡牌展示的预览生物</returns>
    protected CreatureBean BuildAscendPreviewCreature(CreatureBean source, int newRarity)
    {
        //值字段复制 + 稀有度替换为升阶后值;引用型字段共享(只读展示:图标/CMP加成/职业等)
        CreatureBean preview = new CreatureBean
        {
            creatureId = source.creatureId,
            creatureUUId = source.creatureUUId,
            creatureName = source.creatureName,
            level = source.level,
            levelExp = source.levelExp,
            rarity = newRarity,
            relationship = source.relationship,
            creatureNpcData = source.creatureNpcData,
            creatureState = source.creatureState,
            bodySizeScale = source.bodySizeScale,
            creatureAttribute = source.creatureAttribute,
            dicSkinData = source.dicSkinData,
            dicEquipItemData = source.dicEquipItemData,
            dicRarityBuff = source.dicRarityBuff,
        };
        return preview;
    }

    /// <summary>
    /// 生物卡牌掉落出现动画:从落点上方落入 + BackOut 缩放弹出(灵动)。
    /// </summary>
    /// <param name="card">目标卡牌</param>
    /// <param name="endPos">落点锚点位置</param>
    /// <param name="endScale">落点缩放</param>
    /// <param name="delay">起始延迟(用于前后卡牌错位出现)</param>
    protected void PlayCardDropIn(UIViewCreatureCardItem card, Vector2 endPos, Vector3 endScale, float delay)
    {
        RectTransform rtf = card.rectTransform;
        rtf.DOKill();
        //起点:落点正上方 + 缩小
        rtf.anchoredPosition = endPos + new Vector2(0, 320);
        rtf.localScale = endScale * 0.6f;
        Sequence seq = DOTween.Sequence();
        seq.AppendInterval(delay);
        seq.Append(rtf.DOAnchorPos(endPos, 0.45f).SetEase(Ease.OutBack));
        seq.Join(rtf.DOScale(endScale, 0.45f).SetEase(Ease.OutBack));
    }
    #endregion

    #region 动画相关
    public void AnimForListShow(Transform targetList, bool isShow, bool isAnim,float animTime = 0.2f)
    {
        RectTransform tragetRTF = (RectTransform)targetList;
        tragetRTF.DOKill();
        Vector2 targetPos;
        if (isShow)
        {
            targetPos = Vector2.zero;
        }
        else
        {
            if (tragetRTF.pivot.x == 1)
            {
                targetPos = new Vector2(600, 0);
            }
            else
            {
                targetPos = new Vector2(-600, 0);
            }
        }
        if (isAnim)
        {
            tragetRTF.DOAnchorPos(targetPos, animTime);
        }
        else
        {
            tragetRTF.anchoredPosition = targetPos;
        }
    }
    #endregion
}