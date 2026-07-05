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
        //赋值给类字段(不要用 var 新建局部变量,否则会遮蔽 this.scenePrefab 导致后续 StartVote/InteractPodium 取到 null)
        scenePrefab = WorldHandler.Instance.GetCurrentScenePrefab<ScenePrefabForDoomCouncil>(GameSceneTypeEnum.DoomCouncil);
        //生成议员(议会人数随机 + 随机议员 + 固定NPC 10%概率)
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        List<CreatureBean> listCouncilor = GenerateCouncilors();
        //按议案通过率为议员生成投票态度
        GenerateCouncilorAttitudes(listCouncilor, doomCouncilData.doomCouncilInfo.success_rate);
        doomCouncilData.listCouncilor = listCouncilor;
        await scenePrefab.InitCouncilor(listCouncilor);
        //刷新所有议员的态度/好感显示
        RefreshAllCouncilorView();

        //初始化位置数据
        doomCouncilData.dicCouncilorPosition = new Dictionary<string, Vector3>();
        foreach (var itemCouncilor in scenePrefab.dicCouncilorObj)
        {
            doomCouncilData.dicCouncilorPosition.Add(itemCouncilor.Key, itemCouncilor.Value.transform.position);
        }

        //设置基地场景视角
        Vector3 startPosition = scenePrefab.podium.transform.position;
        //Vector3 startPosition = doomCouncilData.dicCouncilorPosition[doomCouncilData.listCouncilor[0].creatureUUId];
        await CameraHandler.Instance.InitBaseSceneControlCamera(userData.selfCreature, startPosition);
        //开始
        StartGame();
    }

    public override void StartGame()
    {
        base.StartGame();
        //打开议会主UI(替换原本的UIBaseMain)
        var uiDoomCouncilMain = UIHandler.Instance.OpenUIAndCloseOther<UIDoomCouncilMain>();
    }

    public override void EndGame()
    {
        base.EndGame();
        //展示mask
        UIHandler.Instance.ShowMask(1, null, () =>
        {
            //保存用户数据
            UserDataBean userData = GameDataHandler.Instance.manager.SaveUserData();
            WorldHandler.Instance.EnterGameForBaseScene(userData);
        }, false);
    }

    /// <summary>
    /// 开始投票
    /// </summary>
    public async void StartVote()
    {
        //关闭角色控制
        GameControlHandler.Instance.SetBaseControl(false, isHideControlTarget: false);
        //隐藏所有议员的意愿(态度)图标: 正式投票阶段不再展示赞成意愿
        scenePrefab.HideAllCouncilorAttitudeView();
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
            var creatureData = doomCouncilData.GetCouncilor(creatureUUId);
            if (creatureData == null)
            {
                continue;
            }
            //态度代表该议员投赞成的概率(0~100)
            int attitude = doomCouncilData.GetCouncilorAttitude(creatureUUId);
            NpcVoteTypeEnum npcVoteType = UnityEngine.Random.Range(0, 100) < attitude ? NpcVoteTypeEnum.Aye : NpcVoteTypeEnum.Nay;

            var creatureNpcData = creatureData.GetCreatureNpcData();
            int voteNum = 1;
            //获取该NPC的投票数(按评级)
            if (creatureNpcData != null && creatureNpcData.npcId != 0)
            {
                var npcInfo = NpcInfoCfg.GetItemData(creatureNpcData.npcId);
                int councilorRatings = npcInfo.GetCouncilorRatings();
                var ratingInfo = DoomCouncilRatingsInfoCfg.GetItemData(councilorRatings);
                voteNum = ratingInfo.vote;
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
            //二次确认：离开后消耗的资源不会返还
            DialogBean dialogConfirmData = new DialogBean();
            dialogConfirmData.content = TextHandler.Instance.GetTextById(53013);
            dialogConfirmData.actionSubmit = (dialogView, data) =>
            {
                EndGame();
            };
            //取消离开：选择"离开议会"时讲台选择弹窗已被销毁，这里重新打开讲台菜单返回上一个弹窗
            dialogConfirmData.actionCancel = (dialogView, data) =>
            {
                InteractPodium();
            };
            UIHandler.Instance.ShowDialogNormal(dialogConfirmData);
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
        targetUI.SetData(targetObj, councilorData, conversationContent, ActionForCouncilorConversationEnd);
        //停止控制
        GameControlHandler.Instance.SetBaseControl(false, isHideControlTarget: false);
    }

    /// <summary>
    /// 回调-结束议员谈话
    /// </summary>
    public void ActionForCouncilorConversationEnd()
    {
        //打开议会主UI 并且恢复移动(替换原本的UIBaseMain)
        var uiDoomCouncilMain = UIHandler.Instance.OpenUIAndCloseOther<UIDoomCouncilMain>();
    }

    #region 议员生成与态度
    /// <summary>
    /// 生成本场议会的议员列表
    /// 人数在议案 council_num 区间内随机; 每个席位随机一种生物 + 按权重随机评级的议会随机NPC;
    /// 整场有10%概率出现1名议会固定NPC(拥有持久化好感系统)
    /// </summary>
    /// <returns>议员列表</returns>
    private List<CreatureBean> GenerateCouncilors()
    {
        List<CreatureBean> listCouncilor = new List<CreatureBean>();
        int councilNum = doomCouncilData.doomCouncilInfo.GetRandomCouncilNum();
        //10%概率出现议会固定NPC
        bool hasFixed = UnityEngine.Random.Range(0f, 1f) <= 0.1f;
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        var userRelationship = userData.GetUserRelationshipData();
        for (int i = 0; i < councilNum; i++)
        {
            NpcInfoBean npcInfo = null;
            //首个席位尝试作为固定NPC
            if (hasFixed && i == 0)
            {
                npcInfo = NpcInfoCfg.GetRandomFixedCouncilorNpc();
            }
            //其余席位(或没有固定NPC时)生成随机议员
            if (npcInfo == null)
            {
                npcInfo = NpcInfoCfg.GetRandomCouncilorNpc();
            }
            if (npcInfo == null)
            {
                continue;
            }
            CreatureBean creatureData = new CreatureBean(npcInfo);
            //议员显示名使用评级名称(预备/列席/初级议员等)
            creatureData.SetCouncilorDisplayName();
            //固定NPC: 载入持久化好感(默认仇恨)
            if (creatureData.IsFixedCouncilor())
            {
                creatureData.relationship = userRelationship.GetRelationship(npcInfo.id);
            }
            listCouncilor.Add(creatureData);
        }
        return listCouncilor;
    }

    /// <summary>
    /// 按议案通过率为议员生成投票态度(0/25/50/75/100)
    /// 算法: 高态度(赞成)组人数 = 总数×通过率 → 分配到{75,100}; 其余低态度组 → 分配到{0,25};
    /// 通过率越高 → 越多议员倾向赞成; 最后从全体中随机取10%覆盖为50; 议会固定NPC再叠加其好感对应的态度修正
    /// </summary>
    /// <param name="listCouncilor">议员列表</param>
    /// <param name="successRate">议案通过率(0~1)</param>
    private void GenerateCouncilorAttitudes(List<CreatureBean> listCouncilor, float successRate)
    {
        int count = listCouncilor.Count;
        if (count == 0)
        {
            return;
        }
        successRate = Mathf.Clamp01(successRate);
        //打乱索引顺序
        List<int> indexList = new List<int>();
        for (int i = 0; i < count; i++)
        {
            indexList.Add(i);
        }
        ShuffleList(indexList);
        //高态度(赞成)组人数 = 总数 × 通过率
        int highCount = Mathf.RoundToInt(count * successRate);
        for (int k = 0; k < count; k++)
        {
            int targetIndex = indexList[k];
            int attitude;
            if (k < highCount)
            {
                //高态度组: 75 或 100
                attitude = UnityEngine.Random.Range(0, 2) == 0 ? 75 : 100;
            }
            else
            {
                //低态度组: 0 或 25
                attitude = UnityEngine.Random.Range(0, 2) == 0 ? 0 : 25;
            }
            doomCouncilData.SetCouncilorAttitude(listCouncilor[targetIndex].creatureUUId, attitude);
        }
        //从全体中随机取10%覆盖为50
        int fiftyCount = Mathf.RoundToInt(count * 0.1f);
        ShuffleList(indexList);
        for (int k = 0; k < fiftyCount && k < count; k++)
        {
            doomCouncilData.SetCouncilorAttitude(listCouncilor[indexList[k]].creatureUUId, 50);
        }
        //议会固定NPC: 叠加好感对应的态度修正
        for (int i = 0; i < count; i++)
        {
            var itemCouncilor = listCouncilor[i];
            if (itemCouncilor.IsFixedCouncilor())
            {
                int modifier = GetRelationshipAttitudeModifier(itemCouncilor.relationship);
                doomCouncilData.AddCouncilorAttitude(itemCouncilor.creatureUUId, modifier);
            }
        }
    }

    /// <summary>
    /// 好感对应的态度修正值(百分点): 仇恨-100 冷淡-50 中立0 友好+50 迷恋+100
    /// </summary>
    /// <param name="relationship">好感度数值</param>
    /// <returns>态度修正值</returns>
    public static int GetRelationshipAttitudeModifier(int relationship)
    {
        int relationshipType = (int)NpcRelationshipInfoCfg.GetNpcRelationshipEnum(relationship);
        return (relationshipType - 3) * 50;
    }

    /// <summary>
    /// 洗牌(Fisher-Yates)
    /// </summary>
    /// <param name="list">待洗牌的索引列表</param>
    private void ShuffleList(List<int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    /// <summary>
    /// 刷新所有议员的态度颜色/好感图标显示
    /// </summary>
    public void RefreshAllCouncilorView()
    {
        if (scenePrefab == null)
        {
            return;
        }
        foreach (var item in scenePrefab.dicCouncilorObj)
        {
            var councilorData = doomCouncilData.GetCouncilor(item.Key);
            if (councilorData == null)
            {
                continue;
            }
            scenePrefab.RefreshCouncilorView(item.Key, councilorData, doomCouncilData.GetCouncilorAttitude(item.Key));
        }
    }

    /// <summary>
    /// 刷新单个议员的态度颜色/好感图标显示
    /// </summary>
    /// <param name="creatureUUId">议员UUID</param>
    public void RefreshCouncilorView(string creatureUUId)
    {
        if (scenePrefab == null)
        {
            return;
        }
        var councilorData = doomCouncilData.GetCouncilor(creatureUUId);
        if (councilorData == null)
        {
            return;
        }
        scenePrefab.RefreshCouncilorView(creatureUUId, councilorData, doomCouncilData.GetCouncilorAttitude(creatureUUId));
    }
    #endregion
}