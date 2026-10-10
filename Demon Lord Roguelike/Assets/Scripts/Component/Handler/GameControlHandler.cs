using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;

public class GameControlHandler : BaseHandler<GameControlHandler,GameControlManager>
{
    /// <summary>
    /// 设置战斗控制
    /// </summary>
    public void SetFightControl()
    {
        manager.EnableAllControl(false);
        manager.controlForGameFight.EnabledControl(true);
    }

    /// <summary>
    /// 设置基础移动控制
    /// </summary>
    public void SetBaseControl(bool isEnable = true, bool isHideControlTarget = true)
    {
        manager.EnableAllControl(false);
        manager.controlForGameBase.EnabledControl(isEnable, isHideControlTarget);
    }

    /// <summary>
    /// 动画--基础控制物体出现-跳跃
    /// </summary>
    /// <param name="startPos"></param>
    /// <param name="endPos"></param>
    /// <param name="animTimeForJump"></param>
    public void AnimForBaseControlShow(Vector3 endPos,float animTime)
    {
        var targetTF = manager.controlTargetForCreature.transform;
        targetTF.gameObject.ShowObj(true);
        var targetRenderer = targetTF.Find("Renderer");
        //保留 SetCreatureData 注入的幻化 world_data 偏移(Renderer 的 localPosition), 否则落地即被归零
        Vector3 localOffset = targetRenderer.localPosition;
        targetRenderer.position = endPos + localOffset + new Vector3(0,5,0);
        targetRenderer.eulerAngles = Vector3.zero;
        //从天而降起跳即播待机动画(此前要等落地后 UIBaseMain 打开经 EnabledControl 才播 Idle, 坠落全程是静止姿势)
        manager.controlForGameBase.PlayAnimForControlTarget(SpineAnimationStateEnum.Idle);
        targetRenderer
            .DOMove(endPos + localOffset, animTime)
            .SetEase(Ease.InCubic)
            .OnComplete(() =>
            {
                EffectBean effectData = new EffectBean();
                effectData.timeForShow = 1;
                effectData.effectPosition = endPos;
                effectData.effectName="Effect_BodySlam_1";
                effectData.isDestoryPlayEnd = true;
                EffectHandler.Instance.ShowEffect(effectData);
                //从天而降动画播放完毕 播放落地音效
                AudioHandler.Instance.PlaySound(AudioEnum.sound_hit_6);
            });
    }
}
