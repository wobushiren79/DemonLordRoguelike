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
        doomCouncilData.listCouncilor = listCouncilor;
        await scenePrefab.InitCouncilor(listCouncilor);

        //初始化位置数据
        doomCouncilData.dicCouncilorPosition = new Dictionary<string, Vector3>();
        foreach (var itemCouncilor in scenePrefab.dicCouncilorObj)
        {
            doomCouncilData.dicCouncilorPosition.Add(itemCouncilor.Key, itemCouncilor.Value.transform.position);
        }

        //设置基地场景视角
        //Vector3 startPosition = scenePrefab.podium.transform.position;
        Vector3 startPosition = doomCouncilData.dicCouncilorPosition[doomCouncilData.listCouncilor[0].creatureUUId];
        await CameraHandler.Instance.InitBaseSceneControlCamera(userData.selfCreature, startPosition);
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
        //保存用户数据
        UserDataBean userData = GameDataHandler.Instance.manager.SaveUserData();
        WorldHandler.Instance.EnterGameForBaseScene(userData, true);
    }

    /// <summary>
    /// 开始投票
    /// </summary>
    public async void StartVote()
    {
        //关闭角色控制
        GameControlHandler.Instance.SetBaseControl(false, isHideControlTarget: false);
        //控制角色位移到讲台
        var controlTarget = GameControlHandler.Instance.manager.controlTargetForCreature;
        controlTarget.transform.position = scenePrefab.podium.transform.position;
        //设置到投票视角
        CameraHandler.Instance.SetCameraForDoomCouncilVote();
        //等待0.5秒镜头切换
        await new WaitForSeconds(0.5f);       
        //打开投票UI
        UIDoomCouncilVote voteUI = UIHandler.Instance.OpenUIAndCloseOther<UIDoomCouncilVote>();
        voteUI.SetData(doomCouncilData);
        //获取所有议员
        Dictionary<string, GameObject> dicCouncilorObj = scenePrefab.dicCouncilorObj;

        int ayeVoteNum = 0;
        int nayVoteNum = 0;

        foreach (var itemCouncilor in dicCouncilorObj)
        {
            string creatureUUId = itemCouncilor.Key;
            GameObject creatureObj = itemCouncilor.Value;

            await new WaitForSeconds(0.2f);
            //计算成功率
            NpcVoteTypeEnum npcVoteType = NpcVoteTypeEnum.None;
            float successRate = UnityEngine.Random.Range(0f, 1f);
            if (successRate <= doomCouncilData.doomCouncilInfo.success_rate)
            {
                npcVoteType = NpcVoteTypeEnum.Aye;
            }
            else
            {
                npcVoteType = NpcVoteTypeEnum.Nay;
            }
            //正常情况有30%的概率睡觉 (后续通过升级可以减小这个概率)
            float sleepRate = UnityEngine.Random.Range(0f, 1f);
            if (sleepRate <= 0.3f)
            {
                npcVoteType = NpcVoteTypeEnum.Sleep;
            }
            var creatureData = doomCouncilData.GetCouncilor(creatureUUId);
            var creatureNpcData = creatureData.GetCreatureNpcData();
            
            int voteNum = 1;
            //获取该NPC的投票数
            if (creatureNpcData!=null&&creatureNpcData.npcId!=0)
            {
                var npcInfo = NpcInfoCfg.GetItemData(creatureNpcData.npcId);
                int councilorRatings=npcInfo.GetCouncilorRatings();
                var rarityInfo =  DoomCouncilRatingsInfoCfg.GetItemData(councilorRatings);
                voteNum = rarityInfo.vote;
            }

            if (npcVoteType == NpcVoteTypeEnum.Aye)
            {
                ayeVoteNum += voteNum;
            }
            else if (npcVoteType == NpcVoteTypeEnum.Nay)
            {
                nayVoteNum += voteNum;
            }
            //播放议员投票动画
            scenePrefab.CouncilorVote(creatureObj, npcVoteType);
            //刷新UI
            voteUI.AddVoteData(npcVoteType, voteNum);
        }

        await new WaitForSeconds(0.5f);
        //计算是否通过
        bool isPass = ayeVoteNum >= nayVoteNum ? true : false;
        //展示投票结果
        var voteEndUI = UIHandler.Instance.OpenUI<UIDoomCouncilVoteEnd>();    
        voteEndUI.VoteEndShow(isPass);
        await new WaitForSeconds(2f);
        //弹出下一步提示
        DialogSelectBean dialogSelectData = new DialogSelectBean();
        dialogSelectData.isDestroyBG = false;
        //如果没有通过
        if (!isPass)
        {
            //暴力说服
            dialogSelectData.AddSelect(TextHandler.Instance.GetTextById(53010), () =>
            {
                FightBeanForDoomCouncil fightData = new FightBeanForDoomCouncil(doomCouncilData);
                WorldHandler.Instance.EnterGameForFightScene(fightData);
            });
        }
        //离开议会
        dialogSelectData.AddSelect(TextHandler.Instance.GetTextById(53005), () =>
        {
            EndGame();
        });
        UIDialogSelect dialogSelect = UIHandler.Instance.ShowDialogSelect(dialogSelectData);
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
        dialogSelectData.isDestroyBG = false;
        //开始投票
        dialogSelectData.AddSelect(TextHandler.Instance.GetTextById(53004), () =>
        {
            StartVote();
        });
        //自由活动
        dialogSelectData.AddSelect(TextHandler.Instance.GetTextById(53011), () =>
        {
           //恢复控制
            GameControlHandler.Instance.SetBaseControl(true);
        });
        //离开议会
        dialogSelectData.AddSelect(TextHandler.Instance.GetTextById(53005), () =>
        {
            EndGame();
        });
        UIDialogSelect targetUI = UIHandler.Instance.ShowDialogSelect(dialogSelectData);
        //停止控制
        GameControlHandler.Instance.SetBaseControl(false, isHideControlTarget: false);
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
        string creatureUUId = targetObj.name.Replace("Councilor_", "");
        //获取议员数据
        var councilorData = doomCouncilData.GetCouncilor(creatureUUId);
        if (councilorData == null)
        {
            LogUtil.LogError($"获取议员数据失败 没有找到议员数据 uuid:{targetObj.name}");
            return;
        }
        //获取和该议员的关系
        NpcRelationshipEnum npcRelationship = councilorData.GetRelationshipForNpc();
        //获取该关系下的所有对话
        var listCouncilorInfo = ConversationCouncilorInfoCfg.GetDataByRelationship(npcRelationship);
        //随机获取一条交谈内容
        var randomConversationInfo = listCouncilorInfo[UnityEngine.Random.Range(0, listCouncilorInfo.Count)];
        string conversationContent = randomConversationInfo.content_language;

        UIGameConversation targetUI = UIHandler.Instance.OpenUIAndCloseOther<UIGameConversation>();
        targetUI.SetData(councilorData, conversationContent, ActionForCouncilorConversationEnd);
        //停止控制
        GameControlHandler.Instance.SetBaseControl(false, isHideControlTarget: false);
    }

    /// <summary>
    /// 回调-结束议员谈话
    /// </summary>
    public void ActionForCouncilorConversationEnd()
    {
        //打开主UI 并且恢复移动
        var uiBaseMain = UIHandler.Instance.OpenUIAndCloseOther<UIBaseMain>();
    }
}