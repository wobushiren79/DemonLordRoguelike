using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DoomCouncilLogic : BaseGameLogic
{
    public DoomCouncilBean doomCouncilData;
    public ScenePrefabForDoomCouncil scenePrefab;

    public override async void PreGame()
    {
        base.PreGame();
        //进入议会场景
        await WorldHandler.Instance.EnterDoomCouncilScene();
        var scenePrefabObj = WorldHandler.Instance.GetCurrentScene(GameSceneTypeEnum.DoomCouncil);
        scenePrefab = scenePrefabObj.GetComponent<ScenePrefabForDoomCouncil>();

        //生成议员
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        var listCouncilorInfo = NpcInfoCfg.GetNpcInfosByType(NpcTypeEnum.Councilor);
        List<CreatureBean> listCouncilor = new List<CreatureBean>(); 
        for (int i = 0; i < listCouncilorInfo.Count; i++)
        {
            var itemInfo = listCouncilorInfo[i];
            CreatureBean creatureData = new CreatureBean(itemInfo);
            listCouncilor.Add(creatureData);
        }
        await scenePrefab.InitCouncilor(listCouncilor);

        //设置基地场景视角
        await CameraHandler.Instance.InitBaseSceneControlCamera(userData.selfCreature, scenePrefab.podium.transform.position);
        //开始
        StartGame();
    }

    public override void StartGame()
    {
        base.StartGame();
        //打开主UI
        var uiBaseMain = UIHandler.Instance.OpenUIAndCloseOther<UIBaseMain>();
    }

    public override void EndGame()
    {
        base.EndGame();
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        WorldHandler.Instance.EnterGameForBaseScene(userData, true);
    }

    /// <summary>
    /// 开始投票
    /// </summary>
    public void StartVote()
    {
        UIDoomCouncilVote targetUI = UIHandler.Instance.OpenUIAndCloseOther<UIDoomCouncilVote>();
        targetUI.SetData(doomCouncilData);
    }

    /// <summary>
    /// 和讲台互动
    /// </summary>
    public void InteractPodium()
    {
        if (gameState != GameStateEnum.Gaming)
        {
            return;
        }
        DialogSelectBean dialogSelectData = new DialogSelectBean();
        //开始投票
        dialogSelectData.AddSelect(TextHandler.Instance.GetTextById(53004), () =>
        {
            StartVote();
        });
        //离开议会
        dialogSelectData.AddSelect(TextHandler.Instance.GetTextById(53005), () =>
        {
            EndGame();
        });
        UIDialogSelect targetUI = UIHandler.Instance.ShowDialogSelect(dialogSelectData);
    }

    /// <summary>
    /// 和议员互动
    /// </summary>
    public void InteractCouncilor(GameObject targetObj)
    {
        if (gameState != GameStateEnum.Gaming)
        {
            return;
        }
        //获取议员数据
        var councilorData = doomCouncilData.GetCouncilor(targetObj.name);
        if (councilorData == null)
        {
            LogUtil.LogError($"获取议员数据失败 没有找到议员数据 uuid:{targetObj.name}");
            return;
        }
         UIGameConversation targetUI = UIHandler.Instance.OpenUIAndCloseOther<UIGameConversation>();
    }
}