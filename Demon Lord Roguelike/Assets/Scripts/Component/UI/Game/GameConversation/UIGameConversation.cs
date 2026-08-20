

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public partial class UIGameConversation : BaseUIComponent
{
    public GameObject creatureObj;
    public CreatureBean creatureData;
    public Action acionForEnd;

    [Header("文本动画")]
    public float timeForTextAnim = 0.05f;//每个字符的显示间隔
    protected Coroutine coroutineForTextAnim;
    protected bool isTextAnimPlaying;

    public override void OpenUI()
    {
        base.OpenUI();

    }

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(GameObject creatureObj, CreatureBean creatureData, string content, Action acionForEnd)
    {
        this.creatureObj = creatureObj;
        this.creatureData = creatureData;
        this.acionForEnd = acionForEnd;
        SetCardIcon(creatureData);
        SetName(creatureData.creatureName);
        SetContent(content);
        ui_IconContent.SetData(creatureData, PopupEnum.CreatureCardDetails);
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
    public void SetCardIcon(CreatureBean creatureData)
    {
        //比原始大小放大2倍
        GameUIUtil.SetCreatureUIForSimple(ui_Icon, creatureData, scale: 2);
    }

    #region 文本动画
    /// <summary>
    /// 开始文本逐字显示动画
    /// </summary>
    public void StartTextAnim(string content)
    {
        StopTextAnim();
        ui_TalkText.text = content;
        coroutineForTextAnim = StartCoroutine(CoroutineForTextAnim(content));
    }

    /// <summary>
    /// 停止文本动画（isShowAll=true 时直接显示全部文本）
    /// </summary>
    public void StopTextAnim(bool isShowAll = false)
    {
        if (coroutineForTextAnim != null)
        {
            StopCoroutine(coroutineForTextAnim);
            coroutineForTextAnim = null;
        }
        if (isShowAll)
            ui_TalkText.maxVisibleCharacters = int.MaxValue;
        isTextAnimPlaying = false;
    }

    /// <summary>
    /// 协程-逐字显示文本并播放说话音效
    /// </summary>
    protected IEnumerator CoroutineForTextAnim(string content)
    {
        isTextAnimPlaying = true;
        ui_TalkText.maxVisibleCharacters = 0;
        for (int i = 1; i <= content.Length; i++)
        {
            ui_TalkText.maxVisibleCharacters = i;
            //非空白字符显示时播放说话音效（PlaySound 内置0.1s重复抑制，自动限流）
            if (!char.IsWhiteSpace(content[i - 1]))
            {
                AudioHandler.Instance.PlaySound(AudioEnum.sound_talk_1);
            }
            yield return new WaitForSeconds(timeForTextAnim);
        }
        StopTextAnim(true);
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