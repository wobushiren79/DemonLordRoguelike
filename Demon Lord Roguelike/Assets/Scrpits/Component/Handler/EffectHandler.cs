using NUnit.Framework.Constraints;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class EffectHandler
{


    /// <summary>
    /// 播放血粒子
    /// </summary>
    /// <param name="targetPos"></param>
    public void ShowBloodEffect(Vector3 targetPos, Vector3 attDirection)
    {
        //播放粒子
        Action playEffect = () =>
        {
            if (manager.effectBlood == null)
                return;
            var targetVisualEffect = manager.effectBlood.GetVisualEffect();
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
            manager.effectBlood.PlayEffect();
        };

        if (manager.effectBlood == null)
        {
            EffectBean effectData = new EffectBean();
            effectData.effectName = "EffectBlood_1";
            effectData.effectType = EffectTypeEnum.Visual;
            effectData.isPlayInShow = false;
            effectData.timeForShow = -1;
            ShowEffect(gameObject, effectData, callBackShow: (targetEffect) =>
            {
                manager.effectBlood = targetEffect;
                playEffect?.Invoke();
            });
        }
        else
        {
            playEffect?.Invoke();
        }
    }
}
