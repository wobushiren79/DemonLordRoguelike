using Cinemachine;
using DG.Tweening;
using Spine.Unity;
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Burst.Intrinsics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.VFX;

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
    public CinemachineVirtualCamera sacrificeCamera;
    public override void PreGame()
    {
        base.PreGame();

        //注册事件
        this.RegisterEvent<List<CreatureBean>>(EventsInfo.CreatureSacrifice_SelectCreature, EventForSelectCreature);

        this.RegisterEvent(EventsInfo.CreatureSacrifice_SacrificeSuccess, EventForSacrificeSuccess);
        this.RegisterEvent(EventsInfo.CreatureSacrifice_SacrificeFail, EventForSacrificeFail);

        //初始化场景
        InitSceneData(() =>
        {
            //开始
            StartGame();
        });
    }


    /// <summary>
    /// 处理场景数据
    /// </summary>
    public void InitSceneData(Action actionForComplete)
    {
        //场景实例
        var baseSceneObj = WorldHandler.Instance.currentBaseScene;
        scenePrefab = baseSceneObj.GetComponent<ScenePrefabForBase>();
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
        actionForComplete?.Invoke();
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
    /// 开始献祭
    /// </summary>
    public void StartSacrifice()
    {
        float timeCenterDelay = 2;
        float timeCenterLifetime = 3;
        float timeReset = 0.5f;
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
        //播放生物动画
        AnimForCreatureObj(listFodderCreatureObj, objTargetCreature, timeCenterDelay + timeCenterLifetime, timeReset);
        //播放粒子动画
        AnimForSacrficeEffect(listFodderCreatureObj, timeCenterDelay, timeCenterLifetime);
        //播放摄像头动画
        AnimForSacrficeCamera(timeCenterDelay + timeCenterLifetime, timeReset, () =>
        {
            UIHandler.Instance.OpenUIAndCloseOther<UICreatureSacrifice>();
        });
    }


    /// <summary>
    /// 清理数据
    /// </summary>
    public override void ClearGame()
    {
        if (!listObjFodderCreatures.IsNull())
        {
            listObjFodderCreatures.ForEach((int index, GameObject itemData) =>
            {
                GameObject.DestroyImmediate(itemData);

            });
            listObjFodderCreatures.Clear();
        }
        GameObject.DestroyImmediate(objTargetCreature);
        base.ClearGame();
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
                if (itemTF.name.Contains("VFX_Altar"))
                {
                    VisualEffect visualEffect = itemTF.GetComponent<VisualEffect>();
                    VFXAltar = visualEffect;
                }
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
        if (targetObj.name.Equals(creatureData.creatureId))
        {
            return;
        }
        SkeletonAnimation creatureSpine = targetObj.transform.Find(spineChildTFName).GetComponent<SkeletonAnimation>();
        SpineHandler.Instance.SetSkeletonDataAsset(creatureSpine, creatureData.creatureModel.res_name);
        string[] skinArray = creatureData.GetSkinArray();
        //修改皮肤
        SpineHandler.Instance.ChangeSkeletonSkin(creatureSpine.skeleton, skinArray);
        //播放spine动画
        SpineHandler.Instance.PlayAnim(creatureSpine, SpineAnimationStateEnum.Idle, true);
        targetObj.name = creatureData.creatureId;
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
        Vector2[] arrayPosition = VectorUtil.GetListCirclePosition(listSelectCreature.Count, -90f, startPosition, 1.9f);
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
                    listObjFodderCreatures.Add(itemCreatureObj);
                }
                else
                {
                    itemCreatureObj = listObjFodderCreatures[i];
                }
                itemCreatureObj.SetActive(true);
                itemCreatureObj.transform.position = new Vector3(itemPosition.x, 0, itemPosition.y);
                itemCreatureObj.transform.localScale = Vector3.one;
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
                    itemCreatureObj.gameObject.SetActive(true);
                    itemCreatureObj.transform.position = new Vector3(itemPosition.x, 0, itemPosition.y);
                    itemCreatureObj.transform.localScale = Vector3.one;
                    SetCreatureData(itemCreatureObj, listSelectCreature[i]);
                }
            }
        }
    }

    /// <summary>
    /// 事件-献祭成功
    /// </summary>
    public void EventForSacrificeSuccess()
    {

    }

    /// <summary>
    /// 事件-献祭失败
    /// </summary>
    public void EventForSacrificeFail()
    {

    }

    #endregion

    #region 动画

    /// <summary>
    /// 动画-生物
    /// </summary>
    public void AnimForCreatureObj(List<GameObject> listFodderCreatureObj, GameObject targetCreature, float timeAnim, float timeReset)
    {
        listFodderCreatureObj.ForEach((int index,GameObject objItemCreature)=>
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
            .DOMoveY(0.2f,timeAnim));
        animForSacrificeCreature.Append(targetCreature.transform
            .DOMove(originPosition,timeReset));
    }

    /// <summary>
    /// 动画-粒子
    /// </summary>
    public void AnimForSacrficeEffect(List<GameObject> listFodderCreatureObj, float timeCenterDelay, float timeCenterLifetime)
    {
        EffectHandler.Instance.ShowSacrficeEffect(listFodderCreatureObj, objTargetCreature.transform.position, timeCenterDelay, timeCenterLifetime);
    }

    /// <summary>
    /// 动画-献祭摄像头
    /// </summary>
    public void AnimForSacrficeCamera(float timeAnim, float timeReset, Action actionForComplete)
    {
        //播放摄像头动画
        // 获取噪声组件
        var cameraNoise = sacrificeCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        // 创建动画序列
        float originalFOV = sacrificeCamera.m_Lens.FieldOfView;
        float originalFrequencyGain = cameraNoise.m_FrequencyGain;
        DG.Tweening.Sequence animForSacrificeCamera = DOTween.Sequence();
        //开始抖动
        animForSacrificeCamera.AppendCallback(() =>
        {
            cameraNoise.m_FrequencyGain = 1;
        });
        //拉近镜头 (改变FOV)
        animForSacrificeCamera.Append(DOTween.To(
            () => sacrificeCamera.m_Lens.FieldOfView,
            x => sacrificeCamera.m_Lens.FieldOfView = x,
            originalFOV - 20,
            timeAnim
            ).SetEase(Ease.InOutQuad));
        //延迟后恢复原始FOV
        animForSacrificeCamera.Append(DOTween.To(
            () => sacrificeCamera.m_Lens.FieldOfView,
            x => sacrificeCamera.m_Lens.FieldOfView = x,
            originalFOV,
            timeReset
        ).SetEase(Ease.OutQuad));
        //延迟后恢复原始抖动
        animForSacrificeCamera.AppendCallback(() =>
        {
            cameraNoise.m_FrequencyGain = originalFrequencyGain;
        });

        animForSacrificeCamera.onComplete = () =>
        {
            actionForComplete?.Invoke();
        };
    }
    #endregion
}