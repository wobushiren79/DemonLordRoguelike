

using System;
using Cysharp.Threading.Tasks;
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

    }

    public override void CloseUI()
    {
        base.CloseUI();
        //终止动画推进令牌（防在途异步访问已销毁控件）+ 截断说话音效
        StopTextAnim();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        //销毁取消源（链接令牌本也会自动取消，这里显式收口释放 CTS）
        cancelForTextAnim?.Dispose();
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