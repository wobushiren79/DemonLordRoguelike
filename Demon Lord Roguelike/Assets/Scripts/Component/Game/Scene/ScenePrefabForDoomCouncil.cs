using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using TMPro;
using UnityEngine;

public class ScenePrefabForDoomCouncil : ScenePrefabBase
{
    //议员位置
    public GameObject councilorPosition;
    //讲台
    public GameObject podium;
    //所有议员的预制体
    public Dictionary<string, GameObject> dicCouncilorObj = new Dictionary<string, GameObject>();

    /// <summary>
    /// 初始化所有议员
    /// </summary>
    public async Task InitCouncilor(List<CreatureBean> listCouncilor)
    {
        List<Transform> listTable = new List<Transform>();
        //获取所有席位
        for (int i = 0; i < councilorPosition.transform.childCount; i++)
        {
            var itemTable = councilorPosition.transform.GetChild(i);
            listTable.Add(itemTable);
        }
        //生成议会议员
        for (int i = 0; i < listCouncilor.Count; i++)
        {
            //如果席位已经没了 则不再生成议员
            if (listTable.Count < 0)
            {
                break;
            }   
            //随机一个席位
            var itemTable = listTable[Random.Range(0, listTable.Count)];
            var itemPosition = itemTable.Find("Position");
            var itemCreatureData = listCouncilor[i];
            var targetCreatureObj = await CreatureHandler.Instance.CreateDoomCouncilCreature(itemCreatureData, itemPosition.position);
            dicCouncilorObj.Add(itemCreatureData.creatureUUId, targetCreatureObj);
            //列表里移除席位
            listTable.Remove(itemTable);
        }
    }

    /// <summary>
    /// 场景删除
    /// </summary>
    /// <returns></returns>
    public override async Task DestoryScene()
    {
        await base.DestoryScene();
        DestoryAllCouncilor();
    }

    /// <summary>
    /// 删除所有议员
    /// </summary>
    public void DestoryAllCouncilor()
    {
        foreach (var item in dicCouncilorObj)
        {
            var itemObj = item.Value;
            Destroy(itemObj);
        }
        dicCouncilorObj.Clear();
    }

    /// <summary>
    /// 议员投票
    /// </summary>
    public void CouncilorVote(GameObject targetCouncilor, NpcVoteTypeEnum voteType)
    {
        var voteSign = targetCouncilor.transform.Find("VoteSign");
        var sleepState = targetCouncilor.transform.Find("SleepState");
        
        var voteText = voteSign.GetComponentInChildren<TextMeshPro>();
        switch (voteType)
        {
            case NpcVoteTypeEnum.Aye:
                voteSign.gameObject.SetActive(true);
                ColorUtility.TryParseHtmlString($"#1DA9D6", out Color targetColorAye);
                voteText.text = TextHandler.Instance.GetTextById(53006);
                voteText.color = targetColorAye;
                break;
            case NpcVoteTypeEnum.Nay:
                voteSign.gameObject.SetActive(true);
                ColorUtility.TryParseHtmlString($"#D61515", out Color targetColorNay);
                voteText.text = TextHandler.Instance.GetTextById(53007);
                voteText.color = targetColorNay;
                break;
            case NpcVoteTypeEnum.Sleep:
                sleepState.gameObject.SetActive(true);
                break;
        }
        //是否要跳上桌子
        if(NpcVoteTypeEnum.Aye == voteType || NpcVoteTypeEnum.Nay == voteType)
        {
            float randomJumpTable = UnityEngine.Random.Range(0f, 1f);
            if (randomJumpTable <= 0.3f)
            {
                var targetPosition = targetCouncilor.transform.position;
                targetCouncilor.transform.DOJump(targetPosition + new Vector3(0, 0.5f, -0.5f), 0.5f, 1, 0.2f);
            }
        }
    }
}