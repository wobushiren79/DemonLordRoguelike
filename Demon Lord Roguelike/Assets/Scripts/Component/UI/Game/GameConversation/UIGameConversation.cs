

using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public partial class UIGameConversation : BaseUIComponent
{
    public GameObject creatureObj;
    public CreatureBean creatureData;
    public Action acionForEnd;

    [Header("文本动画")]
    public float timeForTextAnim = 0.05f;//每个字符的显示间隔
    protected bool isTextAnimPlaying;
    protected string contentForTextAnim = "";//当前动画的完整文本
    //文本动画取消源：懒创建一次复用，开始 Reset 重建令牌、停止 Cancel（跳过/重开/关闭统一收口；链接 gameObject 销毁自动取消）
    protected GTaskCancel cancelForTextAnim;

    public override void OpenUI()
    {
        base.OpenUI();
        //每次打开先把对话框布局还原到预制体默认（故事演出可能改过对齐/偏移，不还原会残留到议会对话等其它打开方式）
        ResetContentLayout();
        //每次打开默认不高亮（故事演出开过目标高亮，不隐藏会残留到其它打开方式）
        HideStoryHighlight();
    }

    public override void CloseUI()
    {
        base.CloseUI();
        //终止高亮出现/位置动画(UI 隐藏后不渲染,停止防残留;透明度置满防下次打开残留半透明态)
        highlightFadeTween?.Kill();
        highlightFadeTween = null;
        highlightMoveTween?.Kill();
        highlightMoveTween = null;
        ui_MaskTarget.color = Color.white;
        //终止动画推进令牌（防在途异步访问已销毁控件）+ 截断说话音效
        StopTextAnim();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        //销毁取消源（链接令牌本也会自动取消，这里显式收口释放 CTS）
        cancelForTextAnim?.Dispose();
        //终止高亮动画(防 DOTween 继续访问已销毁的材质报错)
        highlightFadeTween?.Kill();
        highlightFadeTween = null;
        highlightMoveTween?.Kill();
        highlightMoveTween = null;
        //销毁克隆的高亮材质（防材质泄漏）
        if (maskHighlightMaterial != null)
            Destroy(maskHighlightMaterial);
    }

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(GameObject creatureObj, CreatureBean creatureData, string content, Action acionForEnd)
    {
        this.creatureObj = creatureObj;
        this.creatureData = creatureData;
        this.acionForEnd = acionForEnd;
        //NPC配置了头像图片（无spine资源）时走静态头像模式，否则走spine形象模式
        string npcIconRes = GetNpcIconRes(creatureData);
        bool isIconMode = !npcIconRes.IsNull();
        SetCardIcon(creatureData, npcIconRes);
        SetName(creatureData.creatureName);
        SetContent(content);
        if (isIconMode)
        {
            //静态头像无生物模型数据：清空详情气泡（防UI复用残留上一个生物的数据），并隐藏贿赂入口（无议会逻辑会白扣道具）
            ui_IconContent.SetData(null, PopupEnum.CreatureCardDetails);
            ui_Gift.gameObject.SetActive(false);
        }
        else
        {
            ui_IconContent.SetData(creatureData, PopupEnum.CreatureCardDetails);
            ui_Gift.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 设置故事演出对话数据（故事演出系统专用入口；支持旁白：npc_id=0 时无立绘/无名字/无贿赂按钮）
    /// </summary>
    /// <param name="creatureObj">说话生物的场景物体（无实体传 null）</param>
    /// <param name="talkData">故事对话配置</param>
    /// <param name="actionForEnd">点击结束回调</param>
    public void SetDataForStory(GameObject creatureObj, StoryTalkInfoBean talkData, Action actionForEnd)
    {
        if (talkData.IsNarration())
        {
            //旁白：双立绘入口与贿赂全隐藏，名字置空，直接以配置文本起打字机
            this.creatureObj = creatureObj;
            this.creatureData = null;
            this.acionForEnd = actionForEnd;
            ui_Icon.ShowObj(false);
            ui_IconImg.ShowObj(false);
            ui_Gift.gameObject.SetActive(false);
            ui_IconContent.SetData(null, PopupEnum.CreatureCardDetails);
            SetName("");
            SetContent(talkData.content_language);
            return;
        }
        //NPC模式：复用现有立绘/静态头像/名字/打字机管线，仅强制隐藏贿赂入口
        var npcInfo = NpcInfoCfg.GetItemData(talkData.npc_id);
        if (npcInfo == null)
        {
            LogUtil.LogError($"故事演出对话失败，找不到NPC配置 id:{talkData.npc_id}");
            actionForEnd?.Invoke();
            return;
        }
        SetData(creatureObj, new CreatureBean(npcInfo), talkData.content_language, actionForEnd);
        ui_Gift.gameObject.SetActive(false);
    }

    /// <summary>
    /// 设置名字
    /// </summary>
    /// <param name="name"></param>
    public void SetName(string name)
    {
        ui_Name.text = $"{name}";
    }

    /// <summary>
    /// 设置内容（开始逐字显示动画）
    /// </summary>
    public void SetContent(string content)
    {
        StartTextAnim(content);
    }

    #region 对话框布局（故事演出对齐/偏移）
    //ui_Content 预制体默认布局（首次打开时捕获，捕获后才允许还原）
    protected bool isCapturedContentLayout;
    protected Vector2 contentLayoutAnchorMin;
    protected Vector2 contentLayoutAnchorMax;
    protected Vector2 contentLayoutPivot;
    protected Vector2 contentLayoutPos;

    /// <summary>
    /// 还原 ui_Content 到预制体默认布局（首次打开时先捕获默认快照；此后每次打开都还原，防故事演出的自定义对齐/偏移残留）
    /// </summary>
    public void ResetContentLayout()
    {
        if (!isCapturedContentLayout)
        {
            contentLayoutAnchorMin = ui_Content.anchorMin;
            contentLayoutAnchorMax = ui_Content.anchorMax;
            contentLayoutPivot = ui_Content.pivot;
            contentLayoutPos = ui_Content.anchoredPosition;
            isCapturedContentLayout = true;
            return;
        }
        ui_Content.anchorMin = contentLayoutAnchorMin;
        ui_Content.anchorMax = contentLayoutAnchorMax;
        ui_Content.pivot = contentLayoutPivot;
        ui_Content.anchoredPosition = contentLayoutPos;
    }

    /// <summary>
    /// 设置故事演出的对话框布局（对齐锚点(0~1) + 偏移坐标；OpenUI 已先还原默认，这里覆盖为演出配置）
    /// </summary>
    /// <param name="anchor">对齐锚点（x: 0左/0.5中/1右，y: 0下/0.5中/1上）</param>
    /// <param name="offset">相对对齐点的 anchoredPosition 偏移</param>
    public void SetStoryContentLayout(Vector2 anchor, Vector2 offset)
    {
        ui_Content.anchorMin = anchor;
        ui_Content.anchorMax = anchor;
        ui_Content.pivot = anchor;
        ui_Content.anchoredPosition = offset;
    }
    #endregion

    #region 目标高亮（故事演出 MaskTarget，Shader_UI_GuideHighlight）
    //高亮材质实例(克隆自 ui_MaskTarget 材质,仅写本实例的 _Center/_Size,不污染共享材质)
    protected Material maskHighlightMaterial;
    //GetWorldCorners 复用缓冲(固定 4 角,避免每次高亮分配)
    protected readonly Vector3[] highlightCornerBuffer = new Vector3[4];
    //高亮出现淡入动画(仅首现/无亮→有亮时从 0 淡入;亮→亮连续切换保持透明度只更新位置,防切换闪一帧)
    protected Tween highlightFadeTween;
    //高亮位置/尺寸过渡动画(亮→亮切换时 _Center/_Size 从旧值平滑插值到新值:洞移动过程可见,压暗恒定不闪)
    protected Tween highlightMoveTween;

    /// <summary>
    /// 隐藏目标高亮（每次打开 UI 默认不高亮，防上次演出残留）
    /// </summary>
    public void HideStoryHighlight()
    {
        //停止在途出现/位置动画并复位画面透明度(防残留动画在下次淡入前把 alpha 慢慢拉回造成闪动)
        highlightFadeTween?.Kill();
        highlightFadeTween = null;
        highlightMoveTween?.Kill();
        highlightMoveTween = null;
        ui_MaskTarget.color = Color.white;
        ui_MaskTarget.gameObject.SetActive(false);
    }

    /// <summary>
    /// 高亮一个 UI 目标（世界四角→uiCamera 屏幕→Mask UV；目标区域透亮，其余压暗）
    /// </summary>
    /// <param name="targetRect">目标 UI（默认取目标自身大小为高亮范围）</param>
    /// <param name="shapeType">高亮形状（0=方形 1=圆形，对应 Shader_UI_GuideHighlight 的 _ShapeType）</param>
    /// <param name="sizeScale">尺寸倍率（以目标大小为基准放大缩小，默认 1）</param>
    public void SetStoryHighlight(RectTransform targetRect, int shapeType = 0, float sizeScale = 1f)
    {
        //UV 计算前先把遮罩拉伸满屏,保证 UV 相对全屏
        EnsureMaskFullStretch();
        //UI 矩形与 mask 同属 UI Canvas: Overlay 模式时 UGUI 约定相机传 null(世界点即像素平面,用主相机投影会得到天文数字般的屏幕坐标)
        Camera uvCam = GetMaskUVCamera();
        targetRect.GetWorldCorners(highlightCornerBuffer);
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        for (int i = 0; i < highlightCornerBuffer.Length; i++)
        {
            Vector2 uv = WorldPointToMaskUV(highlightCornerBuffer[i], uvCam, uvCam);
            min = Vector2.Min(min, uv);
            max = Vector2.Max(max, uv);
        }
        ApplyStoryHighlight((min + max) * 0.5f, max - min, shapeType, sizeScale);
    }

    /// <summary>
    /// 高亮一个世界空间目标（bounds 八顶点→mainCamera 屏幕→Mask UV；场景物体走此入口）
    /// </summary>
    /// <param name="worldBounds">目标世界包围盒（默认取包围盒大小为高亮范围）</param>
    /// <param name="shapeType">高亮形状（0=方形 1=圆形，对应 Shader_UI_GuideHighlight 的 _ShapeType）</param>
    /// <param name="sizeScale">尺寸倍率（以包围盒大小为基准放大缩小，默认 1）</param>
    public void SetStoryHighlight(Bounds worldBounds, int shapeType = 0, float sizeScale = 1f)
    {
        //UV 计算前先把遮罩拉伸满屏,保证 UV 相对全屏
        EnsureMaskFullStretch();
        var mainCam = CameraHandler.Instance.manager.mainCamera;
        //场景世界点投影到屏幕必须用主相机;屏幕点转 mask 矩形相机随 Canvas 模式(Overlay=null,ScreenSpaceCamera=worldCamera)
        Camera rectCam = GetMaskUVCamera();
        Vector3 center = worldBounds.center;
        Vector3 extents = worldBounds.extents;
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        //八顶点投影取屏幕 AABB(斜视角下世界 AABB 的面投影不单调,顶点全投最稳)
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = center + new Vector3((i & 1) == 0 ? -extents.x : extents.x, (i & 2) == 0 ? -extents.y : extents.y, (i & 4) == 0 ? -extents.z : extents.z);
            Vector2 uv = WorldPointToMaskUV(corner, mainCam, rectCam);
            min = Vector2.Min(min, uv);
            max = Vector2.Max(max, uv);
        }
        ApplyStoryHighlight((min + max) * 0.5f, max - min, shapeType, sizeScale);
    }

    /// <summary>
    /// 高亮遮罩拉伸铺满父节点（压暗整屏只留目标区域透亮；UV 计算前必须先调用，保证 UV 相对全屏）
    /// </summary>
    protected void EnsureMaskFullStretch()
    {
        RectTransform maskRect = ui_MaskTarget.rectTransform;
        maskRect.anchorMin = Vector2.zero;
        maskRect.anchorMax = Vector2.one;
        maskRect.offsetMin = Vector2.zero;
        maskRect.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// 应用高亮（按倍率缩放范围、圆形换算正圆；克隆材质写 _Center/_Size/_ShapeType 并显示）
    /// </summary>
    protected void ApplyStoryHighlight(Vector2 centerUV, Vector2 sizeUV, int shapeType, float sizeScale)
    {
        //尺寸倍率(以目标大小为基准放大缩小,下限防退化)
        sizeUV *= Mathf.Max(sizeScale, 0.01f);
        //圆形:shader 的圆=按 _Size 长短轴的椭圆,这里按屏幕像素取最大边为直径换算 UV,保证正圆
        if (shapeType == 1)
        {
            Rect maskArea = ui_MaskTarget.rectTransform.rect;
            float diameterPixel = Mathf.Max(sizeUV.x * maskArea.width, sizeUV.y * maskArea.height);
            sizeUV = new Vector2(diameterPixel / maskArea.width, diameterPixel / maskArea.height);
        }
        //尺寸下限保护:目标过小/投影退化时高亮洞不可见,整屏压暗会像死机
        sizeUV = Vector2.Max(sizeUV, new Vector2(0.03f, 0.03f));
        if (maskHighlightMaterial == null)
        {
            maskHighlightMaterial = new Material(ui_MaskTarget.material);
            ui_MaskTarget.material = maskHighlightMaterial;
        }
        //形状立即写入(无需过渡);位置的插值动画在分支内启动——注意必须先取旧值快照、再启动 tween,绝不能先 SetVector 再 DOVctor
        //(DOVctor/DOTween.To 的起点读材质当前值,先写目标值会让起点=终点,动画变成零位移,表现为"没有动画")
        maskHighlightMaterial.SetFloat("_ShapeType", shapeType);
        //首现判定在 SetActive 前取:false=上次未显示(首现/无亮→有亮),true=亮→亮连续切换
        bool wasActive = ui_MaskTarget.gameObject.activeSelf;
        if (wasActive)
        {
            //亮→亮连续切换:透明度保持不变(压暗恒定不闪),洞从旧位置/旧尺寸平滑过渡到新目标(可见的出现动画引导视线)
            highlightFadeTween?.Kill();
            highlightFadeTween = null;
            ui_MaskTarget.color = Color.white;
            highlightMoveTween?.Kill();
            //起点读旧值(尚未写入新值),setter 每帧写材质:材质从旧值插值到目标,洞移动过程可见
            highlightMoveTween = DOTween.Sequence().SetUpdate(true)
                .Join(DOTween.To(() => maskHighlightMaterial.GetVector("_Center"), v => maskHighlightMaterial.SetVector("_Center", v), new Vector4(centerUV.x, centerUV.y, 0f, 0f), 0.18f))
                .Join(DOTween.To(() => maskHighlightMaterial.GetVector("_Size"), v => maskHighlightMaterial.SetVector("_Size", v), new Vector4(sizeUV.x, sizeUV.y, 0f, 0f), 0.18f));
        }
        else
        {
            //首现/无亮→有亮:直接写入目标值,alpha 从 0 快速淡入,把玩家注意力引到高亮区
            //(快速下一句都在"亮→亮"路径不改 alpha,只有真正首现才重新淡入;unscaled,战斗 timeScale=0 演出下照常)
            highlightMoveTween?.Kill();
            highlightMoveTween = null;
            maskHighlightMaterial.SetVector("_Center", new Vector4(centerUV.x, centerUV.y, 0f, 0f));
            maskHighlightMaterial.SetVector("_Size", new Vector4(sizeUV.x, sizeUV.y, 0f, 0f));
            highlightFadeTween?.Kill();
            highlightFadeTween = null;
            ui_MaskTarget.color = new Color(1f, 1f, 1f, 0f);
            highlightFadeTween = ui_MaskTarget.DOFade(1f, 0.12f).SetUpdate(true);
        }
        ui_MaskTarget.gameObject.SetActive(true);
    }

    /// <summary>
    /// 世界点经投影相机转屏幕点，再转 ui_MaskTarget 本地 UV（0~1，原点=Mask 矩形左下）
    /// <para>viewCam: 世界点投影到屏幕的相机(UI 矩形传 mask Canvas 相机,Overlay Canvas 传 null;场景物体传主相机)</para>
    /// <para>rectCam: 屏幕点转 Mask 本地矩形的相机(与 mask 所属 Canvas 渲染模式匹配,Overlay 传 null;不匹配会算错)</para>
    /// </summary>
    protected Vector2 WorldPointToMaskUV(Vector3 worldPos, Camera viewCam, Camera rectCam)
    {
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(viewCam, worldPos);
        RectTransform maskRect = ui_MaskTarget.rectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(maskRect, screenPos, rectCam, out Vector2 localPos);
        Rect rect = maskRect.rect;
        return new Vector2((localPos.x - rect.xMin) / rect.width, (localPos.y - rect.yMin) / rect.height);
    }

    /// <summary>
    /// 取 mask 所属 Canvas 的渲染相机约定值(UV 换算用):
    /// ScreenSpaceOverlay 返回 null(UGUI 约定:Overlay Canvas 下世界点=像素平面,无投影相机);
    /// ScreenSpaceCamera 返回 Canvas 的 worldCamera(缺失时兜底 uiCamera)
    /// </summary>
    protected Camera GetMaskUVCamera()
    {
        var maskCanvas = ui_MaskTarget.canvas;
        if (maskCanvas == null)
            return null;
        if (maskCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;
        return maskCanvas.worldCamera != null ? maskCanvas.worldCamera : CameraHandler.Instance.manager.uiCamera;
    }
    #endregion

    /// <summary>
    /// 设置卡片图像
    /// </summary>
    /// <param name="creatureData">生物数据</param>
    /// <param name="iconRes">NPC头像图片配置（NpcInfo.icon_res）；非空时用静态图片展示（无spine资源的NPC），为空时用spine形象</param>
    public void SetCardIcon(CreatureBean creatureData, string iconRes)
    {
        if (!iconRes.IsNull())
        {
            //静态头像模式：隐藏spine，从UI图集加载头像图片
            ui_Icon.ShowObj(false);
            ui_IconImg.ShowObj(true);
            IconHandler.Instance.SetUIIcon(iconRes, ui_IconImg);
            return;
        }
        //spine形象模式：隐藏静态头像，比原始大小放大2倍
        ui_IconImg.ShowObj(false);
        GameUIUtil.SetCreatureUIForSimple(ui_Icon, creatureData, scale: 2);
    }

    /// <summary>
    /// 获取NPC头像图片配置（NpcInfo.icon_res，无spine资源NPC的静态头像）；非NPC或未配置返回 null
    /// </summary>
    protected string GetNpcIconRes(CreatureBean creatureData)
    {
        var npcInfo = creatureData?.GetCreatureNpcData()?.npcInfo;
        if (npcInfo == null || npcInfo.icon_res.IsNull())
            return null;
        return npcInfo.icon_res;
    }

    #region 文本动画
    /// <summary>
    /// 开始文本逐字显示动画（UniTask 驱动，等待/取消统一走框架层 GTask 封装）
    /// </summary>
    public void StartTextAnim(string content)
    {
        StopTextAnim();
        contentForTextAnim = content;
        ui_TalkText.text = content;
        ui_TalkText.maxVisibleCharacters = 0;
        //空文本直接结束（不播音效不进动画）
        if (content.IsNull())
            return;
        //说话音效整条只播一次（独立音源），动画结束/跳过时由收尾逻辑截断
        AudioHandler.Instance.PlaySoundOnce(AudioEnum.sound_talk_1);
        isTextAnimPlaying = true;
        //显式丢弃：UniTaskVoid 发射即忘（消除「未观察异步调用」警告），取消/异常由 UniTaskScheduler 兜底
        _ = TextAnimForContent();
    }

    /// <summary>
    /// 停止文本动画（isShowAll=true 时直接显示全部文本）
    /// </summary>
    public void StopTextAnim(bool isShowAll = false)
    {
        //取消在途动画推进（Cancel 后 await 点抛 OperationCanceledException，UniTask 静默退出）
        cancelForTextAnim?.Cancel();
        FinishTextAnim(isShowAll);
    }

    /// <summary>
    /// 动画收尾（显示全文/复位播放标记/截断音效），不触碰取消源；自然播完与主动停止共用
    /// </summary>
    protected void FinishTextAnim(bool isShowAll)
    {
        if (isShowAll)
            ui_TalkText.maxVisibleCharacters = int.MaxValue;
        isTextAnimPlaying = false;
        //动画比音效短时，动画一停就把还在播的说话音效直接截断（已自然播完则为空操作）
        AudioHandler.Instance.StopSoundOnce(AudioEnum.sound_talk_1);
    }

    /// <summary>
    /// 异步推进逐字显示（async UniTaskVoid 发射即忘直接调用；GTask.WaitReal 实时等待不受 timeScale 影响，故事演出暂停战斗时打字机照常；逐字递增 TMP maxVisibleCharacters）
    /// <para>取消时 await 点抛 OperationCanceledException，UniTaskVoid 默认静默（真异常由 UniTaskScheduler 记录），无需 try/catch</para>
    /// </summary>
    protected async UniTaskVoid TextAnimForContent()
    {
        //取消源懒创建一次（链接 gameObject 销毁自动取消），每次开始 Reset 重建令牌复用
        if (cancelForTextAnim == null)
            cancelForTextAnim = GTask.NewCancel(gameObject);
        cancelForTextAnim.Reset();
        for (int i = 1; i <= contentForTextAnim.Length; i++)
        {
            ui_TalkText.maxVisibleCharacters = i;
            await GTask.WaitReal(timeForTextAnim, cancelForTextAnim);
        }
        //自然播完只收尾不 Cancel（取消源留给下次 Start 的 Reset 复用）
        FinishTextAnim(true);
    }
    #endregion

    #region 点击事件
    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_BG)
        {
            OnClickForEnd();
        }
        else if (viewButton == ui_Gift)
        {
            OnClickForGift();
        }
    }

    /// <summary>
    /// 点击结束（文本动画播放中则跳过动画显示全文，不结束对话）
    /// </summary>
    public void OnClickForEnd()
    {
        if (isTextAnimPlaying)
        {
            StopTextAnim(true);
            return;
        }
        acionForEnd?.Invoke();
    }

    /// <summary>
    /// 点击贿赂
    /// </summary>
    public void OnClickForGift()
    {
        DialogSelectItemBean dialogData = new DialogSelectItemBean();
        dialogData.actionForSelectGift = ActionForItemSelectGift;
        UIHandler.Instance.ShowDialogItemSelect(dialogData);
    }
    #endregion
    
    #region 道具使用回调
    public void ActionForItemSelectGift(UIDialogSelectItem dialogView, ItemBean itemData)
    {
        dialogView.DestroyDialog();
        //从背包里删除这个道具
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        userData.RemoveBackpackItem(itemData);
        var doomCouncilLogic = GameHandler.Instance.manager.GetGameLogic<DoomCouncilLogic>();
        //贿赂: 提升该议员的投票态度(每次固定+10%; 态度只与本场议案绑定, 存于 DoomCouncilBean)
        if (doomCouncilLogic != null && doomCouncilLogic.doomCouncilData != null)
        {
            doomCouncilLogic.doomCouncilData.AddCouncilorAttitude(creatureData.creatureUUId, 10);
        }
        //议会固定NPC: 额外增加好感并持久化(按道具稀有度的好感加成)
        if (creatureData.IsFixedCouncilor())
        {
            var npcData = creatureData.GetCreatureNpcData();
            var rarityInfo = RarityInfoCfg.GetItemData(itemData.rarity);
            int addRelationship = rarityInfo != null ? rarityInfo.item_add_relationship : 0;
            int newRelationship = userData.GetUserRelationshipData().AddRelationship(npcData.npcId, addRelationship);
            creatureData.relationship = newRelationship;
            GameDataHandler.Instance.manager.SaveUserData();
        }
        //刷新该议员的态度颜色/好感图标显示
        if (doomCouncilLogic != null)
        {
            doomCouncilLogic.RefreshCouncilorView(creatureData.creatureUUId);
        }
        //播放增加好感的粒子
        EffectBean effectData = new EffectBean();
        effectData.effectName = "Effect_AddRelationship_1";
        effectData.timeForShow = 1f;
        effectData.effectPosition = creatureObj.transform.position;
        EffectHandler.Instance.ShowEffect(effectData);
    }
    #endregion
}