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
        targetRenderer.position = endPos + new Vector3(0,5,0);
        targetRenderer.eulerAngles = Vector3.zero;
        targetRenderer
            .DOMove(endPos, animTime)
            .SetEase(Ease.InCubic)
            .OnComplete(() =>
            {
                EffectBean effectData = new EffectBean();
                effectData.timeForShow = 1;
                effectData.effectPosition = endPos;
                effectData.effectName="EffectBodySlam_1";
                effectData.isDestoryPlayEnd = true;
                EffectHandler.Instance.ShowEffect(effectData);
            });
    }
}
