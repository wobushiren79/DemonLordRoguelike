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

    public List<GameObject> listCouncilorObj = new List<GameObject>();

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
            var targetCreatureObj = await CreatureHandler.Instance.CreateDoomCouncilCreature(listCouncilor[i], itemPosition.position);
            listCouncilorObj.Add(targetCreatureObj);
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
        for (int i = 0; i < listCouncilorObj.Count; i++)
        {
            var itemObj = listCouncilorObj[i];
            Destroy(itemObj);
        }
        listCouncilorObj.Clear();
    }

    /// <summary>
    /// 议员投票
    /// </summary>
    public void CouncilorVote(GameObject targetCouncilor, NpcVoteTypeEnum voteType)
    {
        var voteSign = targetCouncilor.transform.Find("VoteSign");
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