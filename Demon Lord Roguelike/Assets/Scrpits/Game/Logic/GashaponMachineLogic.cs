using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GashaponMachineLogic : BaseGameLogic
{
    public GashaponMachineBean gashaponMachineData;

    //展示吐蛋的间隔
    public float timeForAnimShowEggInterval = 0.3f;

    //核心建筑
    public GameObject objBuildingCore;

    public override void PreGameForRegisterEvent()
    {

    }

    public override void PreGame()
    {
        base.PreGame();
        //设置摄像头
        CameraHandler.Instance.SetGashaponMachineCamera(int.MaxValue, true);
        //先暂时关闭所有UI
        //UIHandler.Instance.CloseAllUI();
        //开始
        StartGame();
    }

    public async override void StartGame()
    {
        base.StartGame();
        int gashaponNum = gashaponMachineData.gashaponNum;
        for (int i = 0; i < gashaponNum; i++)
        {
            AnimForShowEgg(i);
            await new WaitForSeconds(timeForAnimShowEggInterval);
        }
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
        if (!listEggObj.IsNull())
        {
            for (int i = 0; i < listEggObj.Count; i++)
            {
                var itemEgg = listEggObj[i];
                GameObject.DestroyImmediate(itemEgg);
            }
        }
    }

    //蛋预制
    public List<GameObject> listEggObj = new List<GameObject>();

    /// <summary>
    /// 吐蛋动画
    /// </summary>
    public void AnimForShowEgg(int index)
    {
        //场景实例
        var baseSceneObj = WorldHandler.Instance.currentBaseScene;
        if (objBuildingCore == null)
        {
            objBuildingCore = baseSceneObj.transform.Find("Core/Building").gameObject;
        }
        GameObject objEgg;
        if (index < listEggObj.Count)
        {
            //使用老蛋
            objEgg = listEggObj[index];
        }
        else
        {
            //创建一个新蛋
            objEgg = GameHandler.Instance.manager.GetGameObjectSync("Assets/LoadResources/Common/Gashapon_1.prefab");
            listEggObj.Add(objEgg);
        }
        objEgg.gameObject.SetActive(true);
        objEgg.transform.position = objBuildingCore.transform.position + new Vector3(0, 0.5f, -0.2f);
        objEgg.transform.localScale = Vector3.zero;

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
        objEgg.transform.DOJump(objBuildingCore.transform.position + new Vector3(-startPos + index, 0, -2), 1, 5, animTimeForEggJump).SetEase(Ease.Linear);

        objBuildingCore.transform.localScale = Vector3.one;
        objBuildingCore.transform.DOPunchScale(new Vector3(0.2f, -0.2f, 0.2f), timeForAnimShowEggInterval, 2, 0.5f);
    }
}