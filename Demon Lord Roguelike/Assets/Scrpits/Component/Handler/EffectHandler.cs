using NUnit.Framework.Constraints;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

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
            playEffect?.Invoke(targetEffect);
        });
    }

    /// <summary>
    /// 设置爆炸粒子
    /// </summary>
    public void ShowBoomEffect(string effectName, Vector3 targetPos, float size)
    {
        //播放粒子
        Action<EffectBase> playEffect = (targetEffect) =>
        {
            if (targetEffect == null)
                return;
            var targetVisualEffect = targetEffect.GetVisualEffect();
            targetVisualEffect.SetVector3("StartPosition", targetPos);
            targetVisualEffect.SetFloat("LifeTime", 0.5f);
            targetVisualEffect.SetFloat("WaveSize", size * 2);
            targetVisualEffect.SetFloat("BoomSize", size);
            targetVisualEffect.SetFloat("SmokeSize", size);
            targetEffect.PlayEffect();
        };

        //获取粒子实例
        manager.GetEffectForEnduring(effectName, (targetEffect) =>
        {
            playEffect?.Invoke(targetEffect);
        });
    }

    public float timeCenterDelay = 2;
    public float timeCenterLifetime = 5;
    public float timeItemLifeTime = 3;
    public float attractorSpeed = 1;
    /// <summary>
    /// 展示献祭粒子
    /// </summary>
    public void ShowSacrficeEffect(List<GameObject> listSacrficeTarget, Vector3 endPostion)
    {
        //播放粒子
        Action<EffectBase> playEffect = (targetEffect) =>
        {
            if (targetEffect == null)
                return;
            listSacrficeTarget.ForEach((int index, GameObject itemObj) =>
            {
                VisualEffect visualEffect = itemObj.GetComponentInChildren<VisualEffect>(true);
                visualEffect.gameObject.SetActive(true);

                visualEffect.SetFloat("LifeTime", timeItemLifeTime);
                visualEffect.SetVector3("StartPosition", visualEffect.transform.position);
                visualEffect.SetVector3("EndPosition", endPostion + new Vector3(0, 0.5f, 0));
                visualEffect.SetFloat("AttractorSpeed", attractorSpeed);
                visualEffect.Play();
            });

            var targetVisualEffect = targetEffect.GetVisualEffect();
            targetVisualEffect.SetFloat("StartDelay", timeCenterDelay);
            targetVisualEffect.SetFloat("LifeTime", timeCenterLifetime);
            targetVisualEffect.SetVector3("EndPosition", endPostion + new Vector3(0, 0.5f, 0));
            targetEffect.PlayEffect();    
        };

        //获取粒子实例
        manager.GetEffectForEnduring("EffectSacrfice_1", (targetEffect) =>
        {
            playEffect?.Invoke(targetEffect);
        });
    }
}
