using Unity.Cinemachine;
using DG.Tweening;
using Spine.Unity;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using System.Threading.Tasks;

[Serializable]
public class CreatureSacrificeLogic : BaseGameLogic
{
    public CreatureSacrificeBean creatureSacrificeData;
    //蛋预制的资源路径
    public string pathForSacrificeCreature = "Assets/LoadResources/Creatures/SacrificeCreature_1.prefab";
    //子控件名字
    public string spineChildTFName = "Spine";
    //场景预制
    public ScenePrefabForBase scenePrefab;
    //目标生物模型
    public GameObject objTargetCreature;
    //目标被献祭的生物
    public List<GameObject> listObjFodderCreatures;

    //粒子效果
    public List<VisualEffect> listVFXLight;
    public VisualEffect VFXAltar;

    //献祭摄像头
    public CinemachineCamera sacrificeCamera;
    public override void PreGame()
    {
        base.PreGame();

        //注册事件
        this.RegisterEvent<List<CreatureBean>>(EventsInfo.CreatureSacrifice_SelectCreature, EventForSelectCreature);

        this.RegisterEvent(EventsInfo.CreatureSacrifice_SacrificeSuccess, EventForSacrificeSuccess);
        this.RegisterEvent(EventsInfo.CreatureSacrifice_SacrificeFail, EventForSacrificeFail);

        //初始化场景
        InitSceneData();

        //开始
        StartGame();
    }


    /// <summary>
    /// 处理场景数据
    /// </summary>
    public void InitSceneData()
    {
        //场景实例(赋值给字段，SetAltarEffect/EventForSelectCreature 等依赖该字段，不能用局部变量遮蔽)
        scenePrefab = WorldHandler.Instance.GetCurrentScenePrefab<ScenePrefabForBase>(GameSceneTypeEnum.BaseGaming);
        //设置祭坛粒子
        SetAltarEffect(true);
        //设置摄像头
        sacrificeCamera = CameraHandler.Instance.SetCreatureSacrificeCamera(int.MaxValue, true);
        //先暂时关闭所有UI
        UIHandler.Instance.CloseAllUI();
        //现在场景中心加载目标生物
        objTargetCreature = GameHandler.Instance.manager.GetGameObjectSync(pathForSacrificeCreature);
        objTargetCreature.transform.position = scenePrefab.objBuildingAltar.transform.position;
        objTargetCreature.transform.localScale = Vector3.one;
        listObjFodderCreatures = new List<GameObject>();

        //设置生物数据
        SetCreatureData(objTargetCreature, creatureSacrificeData.targetCreature);     
    }

    /// <summary>
    /// 开始
    /// </summary>
    public override void StartGame()
    {
        base.StartGame();
        //首先打开选择生物UI
        UIHandler.Instance.OpenUIAndCloseOther<UICreatureSacrifice>();
    }

    public override void EndGame()
    {
        base.EndGame();
        SetAltarEffect(false);
        UIHandler.Instance.OpenUIAndCloseOther<UICreatureManager>();
    }

    /// <summary>
    /// 开始献祭: 动画播放前先按成功率掷骰确定结果,动画结束后再做数据结算。
    /// </summary>
    public void StartSacrifice()
    {
        float timeCenterDelay = 2;
        float timeCenterLifetime = 3;
        float timeReset = 0.5f;

        //计算本次献祭最终成功率(保底+祭品)并掷骰判定
        //测试模式且开启手动成功率时, 直接使用手动指定的成功率, 否则按真实公式计算
        float successRate;
        if (creatureSacrificeData.isTestMode && creatureSacrificeData.useManualSuccessRate)
        {
            successRate = Mathf.Clamp01(creatureSacrificeData.manualSuccessRate);
        }
        else
        {
            successRate = CreatureUtil.GetSacrificeSuccessRate(creatureSacrificeData.targetCreature, creatureSacrificeData.fodderCreatures);
        }
        bool isSuccess = UnityEngine.Random.Range(0f, 1f) <= successRate;

        //献祭生物
        List<GameObject> listFodderCreatureObj = new List<GameObject>();
        listObjFodderCreatures.ForEach((int index, GameObject itemCreatureObj) =>
        {
            if (itemCreatureObj.activeSelf)
            {
                listFodderCreatureObj.Add(itemCreatureObj);
            }
        });
        //关闭所有UI
        UIHandler.Instance.CloseAllUI();

        //失败: 在播放动画前立即结算(消耗祭品+累积保底)并立即落盘,
        //防止玩家在献祭动画期间强退,规避祭品消耗与保底累积后重新献祭
        //(成功的结算延后到动画结束,以便在加点界面一并保存玩家手动分配的属性)
        if (!isSuccess)
        {
            SettleSacrificeData(false);
        }

        //播放生物动画
        AnimForCreatureObjSacrfice(listFodderCreatureObj, objTargetCreature, timeCenterDelay + timeCenterLifetime, timeReset, () =>
        {

        });
        //播放粒子动画
        AnimForSacrficeEffect(listFodderCreatureObj, timeCenterDelay, timeCenterLifetime, () =>
        {

        });
        //播放摄像头动画,动画结束后做表现与收尾
        AnimForSacrficeCamera(timeCenterDelay + timeCenterLifetime, timeReset, () =>
        {
            OnSacrificeAnimEnd(isSuccess);
        });
    }

    /// <summary>
    /// 献祭数据结算: 消耗祭品(装备退回背包+从背包移除)、成功则升级清保底、失败则按研究等级累积保底。
    /// <para>落盘只在失败时立即执行(测试模式不落盘);成功时仅改内存,落盘延后到加点确认后的 <see cref="SaveAndEndGame"/>,避免玩家在加点界面强退时祭品已扣、加点未存造成亏损。</para>
    /// <para>不负责任何界面跳转与提示,表现/收尾交由 <see cref="OnSacrificeAnimEnd"/>。失败时在动画播放前调用以即时持久化,防止中途退出规避结算。</para>
    /// </summary>
    /// <param name="isSuccess">本次献祭是否成功</param>
    /// <returns>本次升级获得的可分配属性加点数(失败为 0)</returns>
    public int SettleSacrificeData(bool isSuccess)
    {
        var userData = GameDataHandler.Instance.manager.GetUserData();
        var targetCreature = creatureSacrificeData.targetCreature;
        var listFodder = creatureSacrificeData.fodderCreatures;

        //处理祭品: 无论成功失败都消耗祭品,身上装备先退回背包,再从背包(及阵容)移除
        if (!listFodder.IsNull())
        {
            for (int i = 0; i < listFodder.Count; i++)
            {
                var fodder = listFodder[i];
                if (fodder == null)
                    continue;
                //祭品装备退回背包
                fodder.RemoveAllEquipToBackpack();
                //从背包移除祭品
                userData.RemoveBackpackCreature(fodder);
            }
        }

        //本次升级获得的可分配属性加点数(仅成功时 > 0)
        int attributePoint = 0;
        if (isSuccess)
        {
            //升级一级(返回可分配加点数)并清空保底;属性加点改由玩家在加点界面手动分配
            attributePoint = targetCreature.UpLevelForSacrifice();
            targetCreature.sacrificePityRate = 0f;
        }
        else
        {
            //失败只扣祭品,按「献祭失败保底概率提升」研究等级累积保底(未解锁0级则不累积),下次献祭叠加,成功后清零
            float pityAddRate = userData.GetUserUnlockData().GetUnlockSacrificeFailPityAddRate();
            targetCreature.sacrificePityRate = Mathf.Clamp01(targetCreature.sacrificePityRate + pityAddRate);
        }

        //清空本次祭品选择
        creatureSacrificeData.fodderCreatures = null;

        //落盘策略:仅失败时立即落盘,确保失败结算(扣祭品+累积保底)即时持久化,
        //防止玩家在动画期间强退规避结算。
        //成功时不在此落盘:此时祭品已消耗、生物已升级,但本次升级获得的加点尚未由玩家在
        //加点界面手动分配,若现在落盘,玩家在加点界面强退会丢失祭品却保留不了加点,造成亏损。
        //成功的落盘统一延后到加点确认后的 SaveAndEndGame(或无加点时直接 SaveAndEndGame),
        //加点界面强退则本次成功结算全部不写盘,祭品自然恢复。
        if (!creatureSacrificeData.isTestMode && !isSuccess)
        {
            GameDataHandler.Instance.manager.SaveUserData();
        }
        return attributePoint;
    }

    /// <summary>
    /// 献祭动画结束后的表现与收尾。
    /// <para>成功: 此时才结算数据(升级)、提示成功,有可分配点数则弹出加点界面(加点确认后再存档返回),否则直接返回。</para>
    /// <para>失败: 数据已在动画播放前结算并落盘,此处仅提示失败并返回生物管理界面。</para>
    /// </summary>
    /// <param name="isSuccess">本次献祭是否成功</param>
    public void OnSacrificeAnimEnd(bool isSuccess)
    {
        if (isSuccess)
        {
            //成功结算: 升级并立即落盘,返回本次可分配加点数
            int attributePoint = SettleSacrificeData(true);
            TriggerEvent(EventsInfo.CreatureSacrifice_SacrificeSuccess);
            //有可分配点数: 弹出属性加点界面手动加点,加点确认后再存档并返回
            if (attributePoint > 0)
            {
                OpenAddAttributeUI(creatureSacrificeData.targetCreature, attributePoint);
                return;
            }
            //无可分配点数: 直接存档并返回
            SaveAndEndGame();
        }
        else
        {
            //失败数据已在动画前结算并落盘,这里仅提示并返回
            TriggerEvent(EventsInfo.CreatureSacrifice_SacrificeFail);
            EndGame();
        }
    }

    /// <summary>
    /// 打开属性加点界面: 玩家手动把本次升级获得的点数分配到 HP/护甲/攻击,确认后存档并返回生物管理界面。
    /// </summary>
    /// <param name="targetCreature">升级的目标生物</param>
    /// <param name="attributePoint">本次可分配的属性加点数</param>
    public void OpenAddAttributeUI(CreatureBean targetCreature, int attributePoint)
    {
        UIHandler.Instance.OpenUIAndCloseOther<UICreatureAddAttribute>((ui) =>
        {
            ui.SetData(targetCreature, attributePoint, SaveAndEndGame);
        });
    }

    /// <summary>
    /// 存档(测试模式不落盘到真实存档,仅内存生效)并返回生物管理界面。
    /// </summary>
    public void SaveAndEndGame()
    {
        if (!creatureSacrificeData.isTestMode)
        {
            GameDataHandler.Instance.manager.SaveUserData();
        }
        EndGame();
    }


    /// <summary>
    /// 清理数据
    /// </summary>
    public override async Task ClearGame()
    {
        if (!listObjFodderCreatures.IsNull())
        {
            listObjFodderCreatures.ForEach((int index, GameObject itemData) =>
            {
                itemData.transform.DOKill();
                GameObject.DestroyImmediate(itemData);

            });
            listObjFodderCreatures.Clear();
        }
        objTargetCreature.transform.DOKill();
        GameObject.DestroyImmediate(objTargetCreature);
        await base.ClearGame();
    }

    #region  设置数据

    /// <summary>
    /// 设置祭坛粒子
    /// </summary>
    public void SetAltarEffect(bool isOpen)
    {
        if (VFXAltar == null || listVFXLight.IsNull())
        {
            listVFXLight = new List<VisualEffect>();
            for (int i = 0; i < scenePrefab.objBuildingAltar.transform.childCount; i++)
            {
                var itemTF = scenePrefab.objBuildingAltar.transform.GetChild(i);
                if (itemTF.name.Contains("VFX_LightFire"))
                {
                    VisualEffect visualEffect = itemTF.GetComponent<VisualEffect>();
                    listVFXLight.Add(visualEffect);
                }
            }
            var magicArray = scenePrefab.transform.Find("MagicArray");
            if (magicArray != null)
            {
                VisualEffect visualEffect = magicArray.GetComponent<VisualEffect>();
                VFXAltar = visualEffect;
            }
        }
        //设置灯光
        listVFXLight.ForEach((int index, VisualEffect targetVFX) =>
        {
            if (isOpen)
            {
                targetVFX.gameObject.SetActive(true);
            }
            else
            {
                targetVFX.gameObject.SetActive(false);
            }
        });
        //设置法阵
        if (isOpen)
        {
            VFXAltar.SetVector3("CenterAngleSpeed", new Vector3(10, -20, 20));
        }
        else
        {
            VFXAltar.SetVector3("CenterAngleSpeed", new Vector3(2, -4, 4));
        }
    }

    /// <summary>
    /// 设置生物数据
    /// </summary>
    public void SetCreatureData(GameObject targetObj, CreatureBean creatureData)
    {
        //不重复设置数据
        if (targetObj.name.Equals(creatureData.creatureUUId))
        {
            return;
        }
        SkeletonAnimation creatureSpine = targetObj.transform.Find(spineChildTFName).GetComponent<SkeletonAnimation>();

        //设置spine
        CreatureHandler.Instance.SetCreatureData(creatureSpine, creatureData);
        //播放spine动画
        SpineHandler.Instance.PlayAnim(creatureSpine, SpineAnimationStateEnum.Idle, creatureData, true);
        targetObj.name = creatureData.creatureUUId;
    }
    #endregion

    #region 事件
    /// <summary>
    /// 生物选择
    /// </summary>
    public void EventForSelectCreature(List<CreatureBean> listSelectCreature)
    {
        Transform altarTF = scenePrefab.objBuildingAltar.transform;
        Vector2 startPosition = new Vector2(altarTF.position.x, altarTF.position.z);
        Vector2[] arrayPosition = GetFodderPositions(listSelectCreature.Count, startPosition);

        //如果是添加生物
        if (listSelectCreature.Count >= listObjFodderCreatures.Count)
        {
            for (int i = 0; i < arrayPosition.Length; i++)
            {
                var itemPosition = arrayPosition[i];
                GameObject itemCreatureObj;
                if (i >= listObjFodderCreatures.Count)
                {
                    itemCreatureObj = GameHandler.Instance.manager.GetGameObjectSync(pathForSacrificeCreature);
                    itemCreatureObj.SetActive(false);
                    listObjFodderCreatures.Add(itemCreatureObj);
                }
                else
                {
                    itemCreatureObj = listObjFodderCreatures[i];
                }
                AnimForCreatureShow(itemCreatureObj, itemPosition);
                itemCreatureObj.SetActive(true);
                SetCreatureData(itemCreatureObj, listSelectCreature[i]);
            }
        }
        //如果是减少生物
        else
        {
            for (int i = 0; i < listObjFodderCreatures.Count; i++)
            {
                var itemCreatureObj = listObjFodderCreatures[i];
                //隐藏
                if (i >= listSelectCreature.Count)
                {
                    itemCreatureObj.gameObject.SetActive(false);
                }
                //设置数据
                else
                {
                    var itemPosition = arrayPosition[i];
                    AnimForCreatureShow(itemCreatureObj, itemPosition);
                    itemCreatureObj.gameObject.SetActive(true);
                    SetCreatureData(itemCreatureObj, listSelectCreature[i]);
                }
            }
        }
    }


    /// <summary>
    /// 计算祭品在祭坛周围的站位:沿整圈平均分布(相邻间隔 360°/count),
    /// 并以祭坛正前方(-90°,屏幕最下方)为中心对整圈做居中偏移。
    /// 这样祭品数量变化时整圈会重新居中旋转,而不是把第一个祭品固定钉在最下方,显示更生动。
    /// </summary>
    /// <param name="count">祭品数量</param>
    /// <param name="centerPosition">祭坛中心(XZ 平面)</param>
    /// <returns>各祭品的站位(XZ 平面),长度为 count</returns>
    public Vector2[] GetFodderPositions(int count, Vector2 centerPosition)
    {
        const float radius = 1.9f;
        //居中基准:祭坛正前方(屏幕最下方)
        const float centerAngle = -90f;

        Vector2[] listData = new Vector2[count];
        if (count <= 0)
        {
            return listData;
        }
        //整圈平均分配
        float stepAngle = 360f / count;
        //让整圈的角度中心落在 centerAngle 上(对称居中)
        float startAngle = centerAngle - stepAngle * (count - 1) / 2f;
        for (int i = 0; i < count; i++)
        {
            float itemAngle = startAngle + stepAngle * i;
            listData[i] = VectorUtil.GetCirclePosition(itemAngle, centerPosition, radius);
        }
        return listData;
    }

    /// <summary>
    /// 事件-献祭成功(展示升级反馈提示)
    /// </summary>
    public void EventForSacrificeSuccess()
    {
        //成功提示(多语言 textId 61007),state=1 显示成功(绿色)图标
        UIHandler.Instance.ToastHintText(TextHandler.Instance.GetTextById(61007), 1);
    }

    /// <summary>
    /// 事件-献祭失败(展示失败反馈提示)
    /// </summary>
    public void EventForSacrificeFail()
    {
        //献祭失败播放失败音效
        AudioHandler.Instance.PlaySound(AudioEnum.sound_lose_1);
        //失败提示(多语言 textId 61008),state=0 显示失败(红色)图标
        UIHandler.Instance.ToastHintText(TextHandler.Instance.GetTextById(61008), 0);
    }

    #endregion

    #region 动画
    /// <summary>
    /// 动画-生物出现
    /// </summary>
    public void AnimForCreatureShow(GameObject itemCreatureObj, Vector3 itemPosition)
    {
        float animMoveTime = 0.2f;
        float animScaleTime = 0.2f;
        //如果之前已经显示 则只播放移动动画
        if (itemCreatureObj.activeSelf)
        {
            itemCreatureObj.transform.DOMove(new Vector3(itemPosition.x, 0, itemPosition.y), animMoveTime);
            itemCreatureObj.transform.localScale = Vector3.one;
        }
        //如果之前未显示 则只播放缩放动画
        else
        {
            itemCreatureObj.transform.position = new Vector3(itemPosition.x, 0, itemPosition.y);
            itemCreatureObj.transform.localScale = Vector3.zero;
            itemCreatureObj.transform.DOScale(Vector3.one, animScaleTime).SetEase(Ease.OutBack);
        }
    }

    /// <summary>
    /// 动画-生物
    /// </summary>
    public void AnimForCreatureObjSacrfice(List<GameObject> listFodderCreatureObj, GameObject targetCreature, float timeAnim, float timeReset, Action actionForComplete)
    {
        listFodderCreatureObj.ForEach((int index, GameObject objItemCreature) =>
        {
            SkeletonAnimation creatureSpine = objItemCreature.transform.Find(spineChildTFName).GetComponent<SkeletonAnimation>();
            creatureSpine.skeleton.SetColor(Color.white);
            DOTween.
            To(() => creatureSpine.skeleton.GetColor(),
                x => creatureSpine.skeleton.SetColor(x),
                new Color(1, 1, 1, 0),
                timeAnim * 0.7f);
        });

        //被献祭生物漂浮再落下
        Vector3 originPosition = targetCreature.transform.position;
        DG.Tweening.Sequence animForSacrificeCreature = DOTween.Sequence();
        animForSacrificeCreature.Append(targetCreature.transform
            .DOMoveY(0.2f, timeAnim));
        animForSacrificeCreature.Append(targetCreature.transform
            .DOMove(originPosition, timeReset));
        animForSacrificeCreature.OnComplete(() =>
        {
            listFodderCreatureObj.ForEach((int index, GameObject objItemCreature) =>
            {
                SkeletonAnimation creatureSpine = objItemCreature.transform.Find(spineChildTFName).GetComponent<SkeletonAnimation>();
                creatureSpine.skeleton.SetColor(Color.white);
                objItemCreature.SetActive(false);
            });
            actionForComplete?.Invoke();
        });
    }

    /// <summary>
    /// 动画-粒子
    /// </summary>
    public void AnimForSacrficeEffect(List<GameObject> listFodderCreatureObj, float timeCenterDelay, float timeCenterLifetime, Action actionForComplete)
    {
        string CenterAngleSpeedName = "CenterAngleSpeed";
        Vector3 originCenterAngleSpeed = VFXAltar.GetVector3(CenterAngleSpeedName);
        VFXAltar.SetVector3(CenterAngleSpeedName, originCenterAngleSpeed * 10);
        EffectHandler.Instance.ShowSacrficeEffect(listFodderCreatureObj, objTargetCreature.transform.position, timeCenterDelay, timeCenterLifetime, () =>
        {
            VFXAltar.SetVector3(CenterAngleSpeedName, originCenterAngleSpeed);
            listObjFodderCreatures.ForEach((int index, GameObject itemObj) =>
            {
                VisualEffect visualEffect = itemObj.GetComponentInChildren<VisualEffect>(true);
                visualEffect.gameObject.SetActive(false);
            });
            actionForComplete?.Invoke();
        });
    }

    /// <summary>
    /// 动画-献祭摄像头
    /// </summary>
    public void AnimForSacrficeCamera(float timeAnim, float timeReset, Action actionForComplete)
    {
        //播放摄像头动画
        // 获取噪声组件
        var cameraNoise = sacrificeCamera.GetComponent<CinemachineBasicMultiChannelPerlin>();
        // 创建动画序列
        float originalFOV = sacrificeCamera.Lens.FieldOfView;
        float originalFrequencyGain = cameraNoise.FrequencyGain;
        DG.Tweening.Sequence animForSacrificeCamera = DOTween.Sequence();
        //开始抖动
        animForSacrificeCamera.AppendCallback(() =>
        {
            cameraNoise.FrequencyGain = 1;
        });
        //拉近镜头 (改变FOV)
        animForSacrificeCamera.Append(DOTween.To(
            () => sacrificeCamera.Lens.FieldOfView,
            x => sacrificeCamera.Lens.FieldOfView = x,
            originalFOV - 20,
            timeAnim
            ).SetEase(Ease.InOutQuad));
        //延迟后恢复原始FOV
        animForSacrificeCamera.Append(DOTween.To(
            () => sacrificeCamera.Lens.FieldOfView,
            x => sacrificeCamera.Lens.FieldOfView = x,
            originalFOV,
            timeReset
        ).SetEase(Ease.OutQuad));
        //延迟后恢复原始抖动
        animForSacrificeCamera.AppendCallback(() =>
        {
            cameraNoise.FrequencyGain = originalFrequencyGain;
        });

        animForSacrificeCamera.onComplete = () =>
        {
            actionForComplete?.Invoke();
        };
    }
    #endregion
}