using NUnit.Framework.Constraints;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class EffectHandler
{
    protected string effectBloodName = "EffectBlood_1";

    /// <summary>
    /// 播放血粒子
    /// </summary>
    /// <param name="targetPos"></param>
    public void ShowBloodEffect(Vector3 targetPos, Vector3 attDirection)
    {       
        //播放粒子
        Action<EffectBase> playEffect = (targetEffect) =>
        {
            if (targetEffect == null)
                return;
            var targetVisualEffect = targetEffect.GetVisualEffect();
            if (attDirection.x > 0)
            {
                //targetVisualEffect.SetVector3("BloodVelocityRandomStart", new Vector3(1, 3, 1));
                targetVisualEffect.SetVector3("BloodVelocityRandomEnd", new Vector3(3, -1, -1));
            }
            else
            {
                //targetVisualEffect.SetVector3("BloodVelocityRandomStart", new Vector3(1, 3, 1));
                targetVisualEffect.SetVector3("BloodVelocityRandomEnd", new Vector3(-3, -1, -1));
            }
            targetVisualEffect.SetVector3("PositionStart", targetPos);
            targetEffect.PlayEffect();
        };

        //获取粒子实例
        manager.GetEffectForEnduring(effectBloodName, (targetEffect) => 
        {
            if (targetEffect == null)
            {
                EffectBean effectData = new EffectBean();
                effectData.effectName = "EffectBlood_1";
                effectData.effectType = EffectTypeEnum.Visual;
                effectData.effectShowType = EffectShowTypeEnum.Enduring;
                effectData.isPlayInShow = false;
                effectData.timeForShow = -1;
                ShowEffect(gameObject, effectData, callBackShow: (targetEffect) =>
                {
                    playEffect?.Invoke(targetEffect);
                });
            }
            else
            {
                playEffect?.Invoke(targetEffect);
            }
        });
    }
}
