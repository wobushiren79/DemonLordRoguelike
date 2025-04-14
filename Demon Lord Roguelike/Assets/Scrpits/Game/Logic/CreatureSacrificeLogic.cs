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

    //场景预制
    public ScenePrefabForBase scenePrefab;
    //目标生物模型
    public GameObject objTargetCreature;
    //目标被献祭的生物
    public List<GameObject> listObjFodderCreatures;

    //粒子效果
    public List<VisualEffect> listVFXLight;
    public VisualEffect VFXAltar;

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
        CameraHandler.Instance.SetCreatureSacrificeCamera(int.MaxValue, true);
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
        List<GameObject> listCreatureObj = new List<GameObject>();
        //播放粒子
        listObjFodderCreatures.ForEach((int index, GameObject itemCreatureObj) =>
        {
            if(itemCreatureObj.activeSelf)
            {
                listCreatureObj.Add(itemCreatureObj);
            }
        });
        EffectHandler.Instance.ShowSacrficeEffect(listCreatureObj, objTargetCreature.transform.position);
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
        SkeletonAnimation creatureSpine = targetObj.transform.Find("Spine").GetComponent<SkeletonAnimation>();
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

    #endregion
}