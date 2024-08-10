using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;

[Serializable]
public class GashaponMachineLogic : BaseGameLogic
{
    public GashaponMachineBean gashaponMachineData;

    //展示吐蛋的间隔
    public float timeForAnimShowEggInterval = 0.3f;

    //核心建筑
    public GameObject objBuildingCore;

    //蛋预制
    public List<GameObject> listEggPool = new List<GameObject>();

    public override void PreGameForRegisterEvent()
    {
        this.RegisterEvent<GameObject>(EventsInfo.GashaponMachine_ClickBreak, EventForEggBreak);
    }

    public override void PreGame()
    {
        base.PreGame();
        //设置摄像头
        CameraHandler.Instance.SetGashaponMachineCamera(int.MaxValue, true);
        //先暂时关闭所有UI
        UIHandler.Instance.CloseAllUI();
        //开始
        StartGame();
    }

    public override void StartGame()
    {
        base.StartGame();
        ProcessForShowEgg((listEgg) =>
        {
            ProcessForEggBreak(listEgg);
        });
    }

    /// <summary>
    /// 流程-展示egg
    /// </summary>
    public async void ProcessForShowEgg(Action<List<GameObject>> actionForComplete)
    {
        int gashaponNum = gashaponMachineData.gashaponNum;
        int showNum = 0;
        List<GameObject> listEggTarget = new List<GameObject>();
        Action<GameObject> actionForShowEnd = (targetEgg) =>
        {
            listEggTarget.Add(targetEgg);
            showNum++;
            //展示完成
            if (showNum == gashaponNum)
            {
                actionForComplete?.Invoke(listEggTarget);
            }
        };
        for (int i = 0; i < gashaponNum; i++)
        {
            AnimForShowEgg(i, actionForShowEnd);
            await new WaitForSeconds(timeForAnimShowEggInterval);
        }
    }

    /// <summary>
    /// 流程-蛋破碎
    /// </summary>
    public void ProcessForEggBreak(List<GameObject> listEggTarget)
    {
        //获取列表中第一个蛋
        var firstEggObj = listEggTarget[0];
        //设置摄像头
        var targetCamera = CameraHandler.Instance.SetGashaponBreakCamera(int.MaxValue, true);
        targetCamera.LookAt = firstEggObj.transform;
        targetCamera.Follow = firstEggObj.transform;
        //打开UI
        var targetUI = UIHandler.Instance.OpenUIAndCloseOther<UIGashaponBreak>();
        targetUI.InitForClick(firstEggObj);
        //晃动蛋
        AnimForEggPunch(firstEggObj);
    }

    public override void UpdateGame()
    {
        base.UpdateGame();
    }

    public override void EndGame()
    {
        base.EndGame();
    }

    public override void ClearGame()
    {
        base.ClearGame();

        CameraHandler.Instance.SetGashaponMachineCamera(0, false);
        if (!listEggPool.IsNull())
        {
            for (int i = 0; i < listEggPool.Count; i++)
            {
                var itemEgg = listEggPool[i];
                GameObject.DestroyImmediate(itemEgg);
            }
        }
    }

    #region 事件
    /// <summary>
    /// 事件
    /// </summary>
    /// <param name="targetEgg"></param>
    public void EventForEggBreak(GameObject targetEgg)
    {
        AnimForEggBreak(targetEgg);
    }
    #endregion

    #region 动画
    /// <summary>
    /// 动画-蛋破壳
    /// </summary>
    /// <param name="targetEgg"></param>
    public void AnimForEggBreak(GameObject targetEgg)
    {
        targetEgg.transform.DOKill();
        var eggTF = targetEgg.transform.Find("Other_Egg");
        var effectTF = targetEgg.transform.Find("Effect_MeshShow_1");
        var rendererTF = targetEgg.transform.Find("Renderer");

        var effectPS = effectTF.GetComponent<ParticleSystem>();
        ///设置蛋的大小
        var effectMain = effectPS.main;
        var effectShape = effectPS.shape;
        effectMain.startSizeMultiplier = 0.5f;
        effectShape.scale = targetEgg.transform.localScale;

        effectTF.ShowObj(true);
        eggTF.ShowObj(false);
    }

    /// <summary>
    /// 动画-蛋摇动
    /// </summary>
    public void AnimForEggPunch(GameObject targetEgg)
    {
        targetEgg.transform.DOKill();
        float randomX = UnityEngine.Random.Range(-10f, 10f);
        float randomZ = UnityEngine.Random.Range(-10f, 10f);
        targetEgg.transform.DOPunchRotation(new Vector3(randomX, 0, randomZ), 1, 4, 1).OnComplete(() =>
        {
            AnimForEggPunch(targetEgg);
        });
    }

    /// <summary>
    /// 吐蛋动画
    /// </summary>
    public void AnimForShowEgg(int index, Action<GameObject> actionForComplete)
    {
        //场景实例
        var baseSceneObj = WorldHandler.Instance.currentBaseScene;
        if (objBuildingCore == null)
        {
            objBuildingCore = baseSceneObj.transform.Find("Core/Building").gameObject;
        }
        GameObject objEgg;
        if (index < listEggPool.Count)
        {
            //使用老蛋
            objEgg = listEggPool[index];
        }
        else
        {
            //创建一个新蛋
            objEgg = GameHandler.Instance.manager.GetGameObjectSync("Assets/LoadResources/Common/Gashapon_1.prefab");
            listEggPool.Add(objEgg);
        }
        objEgg.gameObject.SetActive(true);
        objEgg.transform.position = objBuildingCore.transform.position + new Vector3(0, 0.5f, -0.2f);
        objEgg.transform.localScale = Vector3.zero;
        objEgg.transform.DOKill();

        float startPos;
        if (gashaponMachineData.gashaponNum == 1)
        {
            startPos = 0;
        }
        else if (gashaponMachineData.gashaponNum == 10)
        {
            startPos = 4.5f;
        }
        else
        {
            startPos = gashaponMachineData.gashaponNum / 2f;
        }
        float animTimeForEggJump = 1;
        objEgg.transform.DOScale(Vector3.one * 0.25f, animTimeForEggJump / 2f);
        objEgg.transform
            .DOJump(objBuildingCore.transform.position + new Vector3(-startPos + index, 0, -2), 1, 5, animTimeForEggJump)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                actionForComplete?.Invoke(objEgg);
            });

        objBuildingCore.transform.localScale = Vector3.one;
        objBuildingCore.transform.DOPunchScale(new Vector3(0.2f, -0.2f, 0.2f), timeForAnimShowEggInterval, 2, 0.5f);
    }
    #endregion
}