using System.Collections.Generic;
using DG.Tweening;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class UIDialogCreatureShow : DialogView
{
    protected DialogCreatureShowBean dialogCreatureShowData;
    // 当前生物数据
    protected CreatureBean currentCreatureData;
    // 动画名称列表
    protected List<string> animNameList = new List<string>();

    public override void SetData(DialogBean dialogData)
    {
        base.SetData(dialogData);
        dialogCreatureShowData = dialogData as DialogCreatureShowBean;
        InitCreature(dialogCreatureShowData.creatureData);

#if UNITY_EDITOR
        InitAnimList();
#endif
    }

    public void InitCreature(CreatureBean creatureData)
    {
        this.currentCreatureData = creatureData;
        GameUIUtil.SetCreatureUIForDetails(ui_CreatureSpine, null, creatureData, customUISize: 2);
    }

    /// <summary>
    /// 初始化动画列表
    /// </summary>
    protected void InitAnimList()
    {
        if (ui_CreatureSpine == null || ui_CreatureSpine.SkeletonData == null)
        {
            return;
        }
        // 获取所有动画名称
        animNameList.Clear();
        var animations = ui_CreatureSpine.SkeletonData.Animations;
        foreach (var anim in animations)
        {
            animNameList.Add(anim.Name);
        }
        // 设置列表数据
        ui_AnimList.SetData(animNameList.Count, (index, targetObj) =>
        {
            var animItem = animNameList[index];
            var targetText = targetObj.transform.Find("Text").GetComponent<TextMeshProUGUI>();
            var targetBtn = targetObj.transform.GetComponent<Button>();
            targetText.text = animItem;
            targetBtn.onClick.RemoveAllListeners();
            targetBtn.onClick.AddListener(() =>
            {
                OnClickAnimButton(animItem);
            });
        });
    }

    /// <summary>
    /// 点击动画按钮
    /// </summary>
    /// <param name="animName">动画名称</param>
    protected void OnClickAnimButton(string animName)
    {
        if (ui_CreatureSpine == null)
        {
            return;
        }
        // 播放指定动画
        SpineHandler.Instance.PlayAnim(ui_CreatureSpine, SpineAnimationStateEnum.None, true, animNameAppoint: animName);
    }
}
