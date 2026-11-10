using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

public class ScenePrefabForDoomCouncil : ScenePrefabBase
{
    //议员位置
    public GameObject councilorPosition;
    //讲台
    public GameObject podium;
    //自由活动时的移动边界，Transform 的 X/Z 缩放表示范围。
    public GameObject objBoard;
    //所有议员的预制体
    public Dictionary<string, GameObject> dicCouncilorObj = new Dictionary<string, GameObject>();

    #region 议会建筑

    [SerializeField] private Transform wallEast;
    [SerializeField] private GameObject eastBraziers;
    [SerializeField] private Transform wallWest;
    [SerializeField] private GameObject rearBraziers;
    [SerializeField] private Transform wallPodium;
    [SerializeField] private Transform wallGate;
    [SerializeField] private GameObject cutawayEast;
    [SerializeField] private GameObject cutawayPodium;
    [SerializeField] private Transform podiumEmblem;
    [SerializeField] private Transform gate;

    [System.Flags]
    private enum WallSide
    {
        None = 0,
        East = 1,
        West = 2,
        Podium = 4,
        Gate = 8
    }

    private (Renderer renderer, ShadowCastingMode shadowMode)[] eastRenderers;
    private (Renderer renderer, ShadowCastingMode shadowMode)[] westRenderers;
    private (Renderer renderer, ShadowCastingMode shadowMode)[] podiumRenderers;
    private (Renderer renderer, ShadowCastingMode shadowMode)[] gateWallRenderers;
    private (Renderer renderer, ShadowCastingMode shadowMode)[] emblemRenderers;
    private (Renderer renderer, ShadowCastingMode shadowMode)[] gateRenderers;
    private WallSide hiddenWalls;

    /// <summary>当前实例是否用于暴力说服战斗。</summary>
    internal bool IsForBattle { get; private set; }

    #region 初始化

    /// <summary>
    /// 缓存围墙和大门的显示组件及其投影设置。
    /// </summary>
    private void Awake()
    {
        eastRenderers = CacheRenderers(wallEast);
        westRenderers = CacheRenderers(wallWest);
        podiumRenderers = CacheRenderers(wallPodium);
        gateWallRenderers = CacheRenderers(wallGate);
        emblemRenderers = CacheRenderers(podiumEmblem);
        gateRenderers = CacheRenderers(gate);
    }

    /// <summary>
    /// 记录原始投影模式，便于退出剖视时还原。
    /// </summary>
    private static (Renderer renderer, ShadowCastingMode shadowMode)[] CacheRenderers(Transform root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        var states = new (Renderer renderer, ShadowCastingMode shadowMode)[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            states[i] = (renderer, renderer.shadowCastingMode);
        }
        return states;
    }

    /// <summary>
    /// 在主摄像机渲染前检查围墙遮挡。
    /// </summary>
    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += UpdateWallVisibility;
    }

    /// <summary>
    /// 解绑渲染回调并恢复完整围墙的可见性和投影设置。
    /// </summary>
    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= UpdateWallVisibility;
        SetHiddenWalls(WallSide.None);
    }

    #endregion

    #region 围墙与大门显示

    /// <summary>
    /// 根据是否进入战斗切换讲台墙的剖视规则。大门保持预制体的关闭姿态。
    /// </summary>
    public void SetForBattle(bool forBattle)
    {
        IsForBattle = forBattle;
        RefreshPodiumCutaway((hiddenWalls & WallSide.Podium) != 0);
    }

    /// <summary>
    /// 相机越过围墙时隐藏近侧墙面，普通议会等镜头离开讲台墙的厚度后再恢复高墙。
    /// </summary>
    private void UpdateWallVisibility(ScriptableRenderContext context, Camera camera)
    {
        if (camera.cameraType != CameraType.Game || camera != Camera.main)
            return;

        Vector3 point = transform.InverseTransformPoint(camera.transform.position);
        WallSide outside = WallSide.None;
        if (point.x > transform.InverseTransformPoint(wallEast.position).x)
            outside |= WallSide.East;
        if (point.x < transform.InverseTransformPoint(wallWest.position).x)
            outside |= WallSide.West;
        float podiumCutawayZ = transform.InverseTransformPoint(wallPodium.position).z;
        // 普通议会把切换线放在墙柱内沿，避免相机还在墙体中时恢复完整墙面。
        if (!IsForBattle)
            podiumCutawayZ += 0.25f;
        if (point.z < podiumCutawayZ)
            outside |= WallSide.Podium;
        if (point.z > transform.InverseTransformPoint(wallGate.position).z)
            outside |= WallSide.Gate;

        SetHiddenWalls(outside);
    }

    /// <summary>
    /// 只更新本次跨过边界的墙面，同步东墙和讲台的低墙剖视。
    /// </summary>
    private void SetHiddenWalls(WallSide outside)
    {
        WallSide changed = hiddenWalls ^ outside;
        if ((changed & WallSide.East) != 0)
        {
            bool hidden = (outside & WallSide.East) != 0;
            SetRenderersHidden(eastRenderers, hidden);
            cutawayEast.SetActive(hidden);
            if (eastBraziers != null)
                eastBraziers.SetActive(!hidden);
        }
        if ((changed & WallSide.West) != 0)
        {
            bool hidden = (outside & WallSide.West) != 0;
            SetRenderersHidden(westRenderers, hidden);
            rearBraziers.SetActive(!hidden);
        }
        if ((changed & WallSide.Podium) != 0)
        {
            bool hidden = (outside & WallSide.Podium) != 0;
            SetRenderersHidden(podiumRenderers, hidden);
            SetRenderersHidden(emblemRenderers, hidden);
            RefreshPodiumCutaway(hidden);
        }
        if ((changed & WallSide.Gate) != 0)
        {
            bool hidden = (outside & WallSide.Gate) != 0;
            SetRenderersHidden(gateWallRenderers, hidden);
            SetRenderersHidden(gateRenderers, hidden);
        }
        hiddenWalls = outside;
    }

    /// <summary>普通议会从讲台侧剖视时，显示低墙及两端收口。</summary>
    private void RefreshPodiumCutaway(bool podiumHidden)
    {
        if (cutawayPodium != null)
            cutawayPodium.SetActive(podiumHidden && !IsForBattle);
    }

    /// <summary>
    /// 隐藏模型时保留原有投影，恢复显示时还原原始投影模式。
    /// </summary>
    private static void SetRenderersHidden((Renderer renderer, ShadowCastingMode shadowMode)[] renderers, bool hidden)
    {
        foreach (var (renderer, shadowMode) in renderers)
        {
            if (shadowMode == ShadowCastingMode.Off)
                renderer.forceRenderingOff = hidden;
            else
                renderer.shadowCastingMode = hidden ? ShadowCastingMode.ShadowsOnly : shadowMode;
        }
    }

    #endregion

    #endregion

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
