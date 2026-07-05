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
            if (listTable.Count <= 0)
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

    #region 议员态度/好感显示
    /// <summary>
    /// 刷新某个议员的态度颜色(Success) + 好感图标(Relationship)显示
    /// </summary>
    /// <param name="creatureUUId">议员UUID</param>
    /// <param name="councilorData">议员数据</param>
    /// <param name="attitude">投票态度(0~100, 来自 DoomCouncilBean)</param>
    public void RefreshCouncilorView(string creatureUUId, CreatureBean councilorData, int attitude)
    {
        if (!dicCouncilorObj.TryGetValue(creatureUUId, out GameObject targetObj) || targetObj == null)
        {
            return;
        }
        SetCouncilorAttitudeView(targetObj, attitude);
        SetCouncilorRelationshipView(targetObj, councilorData);
    }

    /// <summary>
    /// 隐藏所有议员的态度(意愿)图标(Success SpriteRenderer)
    /// 投票开始时调用: 正式投票阶段不再向玩家展示议员的赞成意愿
    /// </summary>
    public void HideAllCouncilorAttitudeView()
    {
        foreach (var item in dicCouncilorObj)
        {
            var targetObj = item.Value;
            if (targetObj == null)
            {
                continue;
            }
            var successTF = targetObj.transform.Find("Success");
            if (successTF != null)
            {
                successTF.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 设置议员态度进度颜色(Success SpriteRenderer): 0红 50白 100绿
    /// </summary>
    /// <param name="targetCouncilor">议员对象</param>
    /// <param name="attitude">态度值(0~100)</param>
    public void SetCouncilorAttitudeView(GameObject targetCouncilor, int attitude)
    {
        var successTF = targetCouncilor.transform.Find("Success");
        if (successTF == null)
        {
            return;
        }
        var spriteRenderer = successTF.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            return;
        }
        spriteRenderer.color = GetAttitudeColor(attitude);
    }

    /// <summary>
    /// 态度颜色插值: 0=红, 50=白, 100=绿
    /// </summary>
    /// <param name="attitude">态度值(0~100)</param>
    /// <returns>过渡颜色</returns>
    public static Color GetAttitudeColor(int attitude)
    {
        attitude = Mathf.Clamp(attitude, 0, 100);
        ColorUtility.TryParseHtmlString("#D61515", out Color red);
        ColorUtility.TryParseHtmlString("#2ECC40", out Color green);
        Color white = Color.white;
        if (attitude <= 50)
        {
            return Color.Lerp(red, white, attitude / 50f);
        }
        return Color.Lerp(white, green, (attitude - 50) / 50f);
    }

    /// <summary>
    /// 设置议员好感图标显示(Relationship SpriteRenderer)
    /// 议会固定NPC: 显示好感图标, Relationship.x=-0.1, Success.x=0.1 (同排显示)
    /// 议会随机NPC: 隐藏Relationship, Success.x=0 (居中显示)
    /// </summary>
    /// <param name="targetCouncilor">议员对象</param>
    /// <param name="councilorData">议员数据</param>
    public void SetCouncilorRelationshipView(GameObject targetCouncilor, CreatureBean councilorData)
    {
        var successTF = targetCouncilor.transform.Find("Success");
        var relationshipTF = targetCouncilor.transform.Find("Relationship");
        bool isFixed = councilorData.IsFixedCouncilor();
        if (isFixed)
        {
            //固定NPC: Success与Relationship并排
            if (successTF != null)
            {
                var pos = successTF.localPosition;
                pos.x = 0.1f;
                successTF.localPosition = pos;
            }
            if (relationshipTF != null)
            {
                relationshipTF.gameObject.SetActive(true);
                var pos = relationshipTF.localPosition;
                pos.x = -0.1f;
                relationshipTF.localPosition = pos;
                var spriteRenderer = relationshipTF.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    var relationshipInfo = NpcRelationshipInfoCfg.GetNpcRelationship(councilorData.relationship);
                    IconHandler.Instance.GetIconSprite(SpriteAtlasTypeEnum.UI, relationshipInfo.icon_res, (sprite) =>
                    {
                        if (spriteRenderer != null)
                        {
                            spriteRenderer.sprite = sprite;
                        }
                    });
                }
            }
        }
        else
        {
            //随机NPC: 隐藏好感, Success居中
            if (successTF != null)
            {
                var pos = successTF.localPosition;
                pos.x = 0f;
                successTF.localPosition = pos;
            }
            if (relationshipTF != null)
            {
                relationshipTF.gameObject.SetActive(false);
            }
        }
    }
    #endregion
}