using DG.Tweening;
using DG.Tweening.Core;
using NUnit.Framework.Constraints;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public partial class EffectHandler
{
    protected string effectCreatureAscendAddProgressName = "EffectMove_1";
    protected string effectBloodName = "EffectBlood_1";
    protected string effectShieldHitName = "EffectShieldHit_1";

    /// <summary>
    /// 播放护盾打击粒子
    /// </summary>
    /// <param name="targetPos"></param>
    /// <param name="direction">护盾朝向 0左 1右</param>
    public void ShowShieldHitEffect(Vector3 targetPos, Vector3 attDirection)
    {
        //播放粒子
        Action<EffectBase> playEffect = (targetEffect) =>
        {
            if (targetEffect == null)
                return;
            var targetVisualEffect = targetEffect.GetVisualEffect();
            targetVisualEffect.SetVector3("Position", targetPos);
            targetVisualEffect.SetInt("Direction", attDirection.x < 0 ? 0 : 1);
            targetEffect.PlayEffect();
        };

        //获取粒子实例
        manager.GetEffectForEnduring(effectShieldHitName, (targetEffect) =>
        {
            playEffect?.Invoke(targetEffect);
        });
    }

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
    /// 展示生物进阶增加进度粒子
    /// </summary>
    public void ShowCreatureAscendAddProgressEffect(int addNum, Vector3 startPosition, Vector3 endPosition)
    {
        //播放粒子
        Action<EffectBase> playEffect = (targetEffect) =>
        {
            if (targetEffect == null)
                return;
            var targetVisualEffect = targetEffect.GetVisualEffect();
            float randomRange = 0.5f;
            targetVisualEffect.SetInt("EffectNum", addNum);
            targetVisualEffect.SetFloat("StartSize", 0.2f);
            targetVisualEffect.SetFloat("MoveSpeed", 0.02f); 
            targetVisualEffect.SetVector3("StartPositionRandomA", startPosition + new Vector3(-randomRange, -randomRange, -randomRange));
            targetVisualEffect.SetVector3("StartPositionRandomB", startPosition + new Vector3(randomRange, randomRange, randomRange));
            targetVisualEffect.SetVector3("EndPosition", endPosition);
            targetEffect.PlayEffect();
        };

        //获取粒子实例
        manager.GetEffectForEnduring(effectCreatureAscendAddProgressName, (targetEffect) =>
        {
            playEffect?.Invoke(targetEffect);
        });
    }

    /// <summary>
    /// 展示粒子
    /// </summary>
    public void ShowEffect(long effectId, Vector3 targetPos, Direction2DEnum direction, float size)
    {
        targetPos += new Vector3(0, 0.002f, 0f);
        var effectInfo = EffectInfoCfg.GetItemData(effectId);
        //播放粒子
        Action<EffectBase> playEffect = (targetEffect) =>
        {
            if (targetEffect == null)
                return;
            var targetVisualEffect = targetEffect.GetVisualEffect();
            var targetParticleSystem = targetEffect.GetParticleSystem();
            //粒子系统处理
            var dicEffectData = effectInfo.GetEffectItemData();
            dicEffectData.ForEach((index, value) =>
            {
                switch (value.dataType)
                {
                    case 1://float
                        float targetFloatData = value.dataFloat;
                        if (value.isSize)
                        {
                            targetFloatData = size * targetFloatData;
                            //设置PS系统的起始位置
                            if (targetParticleSystem != null) targetEffect.SetParticleSystemSize(targetFloatData);
                        }
                        targetVisualEffect?.SetFloat(value.dataName, targetFloatData);
                        break;
                    case 2://int
                        int targetIntData = value.dataInt;
                        if (value.isDirection)
                        {
                            targetIntData = (int)direction;
                        }
                        targetVisualEffect?.SetInt(value.dataName, targetIntData);
                        break;
                    case 5://vector3
                        Vector3 targetVector3Data = value.dataVector3;
                        if (value.isStartPosition)
                        {
                            targetVector3Data = targetPos + targetVector3Data;
                            //设置PS系统的起始位置
                            if (targetParticleSystem != null) targetEffect.SetParticleSystemStartPosition(targetVector3Data);
                        }
                        targetVisualEffect?.SetVector3(value.dataName, targetVector3Data);
                        break;
                    case 6://vector4
                        Vector4 targetVector4Data = value.dataVector4;
                        targetVisualEffect?.SetVector4(value.dataName, targetVector4Data);
                        break;
                }
            });
            targetEffect.PlayEffect();
        };
        EffectShowTypeEnum showType = effectInfo.GetShowType();
        //如果是持久
        if (showType == EffectShowTypeEnum.Enduring)
        {
            //获取粒子实例
            manager.GetEffectForEnduring(effectInfo.res_name, (targetEffect) =>
            {
                playEffect?.Invoke(targetEffect);
            });
        }
        //如果是一次性
        else if (showType == EffectShowTypeEnum.Once)
        {
            EffectBean effectData = effectInfo.GetEffectData();
            effectData.effectPosition = targetPos + new Vector3(0, 0f, -0.1f);
            ShowEffect(effectData, (targetEffect) =>
            {
                playEffect?.Invoke(targetEffect);
            });
        }
    }

    public Tween animForShowSacrficeEffect;
    public Tween animForShowSacrficeEffectComplete;
    /// <summary>
    /// 展示献祭粒子
    /// </summary>
    public void ShowSacrficeEffect(List<GameObject> listSacrficeTarget, Vector3 endPostion, float timeCenterDelay, float timeCenterLifetime, Action actionForComplete)
    {
        if (animForShowSacrficeEffect != null)
            animForShowSacrficeEffect.Kill();
        if (animForShowSacrficeEffectComplete != null)
            animForShowSacrficeEffectComplete.Kill();
        //播放粒子
        Action<EffectBase> playEffect = (targetEffect) =>
        {
            if (targetEffect == null)
                return;
            listSacrficeTarget.ForEach((int index, GameObject itemObj) =>
            {
                VisualEffect visualEffect = itemObj.GetComponentInChildren<VisualEffect>(true);
                visualEffect.gameObject.SetActive(true);

                visualEffect.SetVector3("StartPosition", visualEffect.transform.position);
                visualEffect.SetVector3("EndPosition", endPostion + new Vector3(0, 0.5f, 0));
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

        animForShowSacrficeEffect = DOVirtual.DelayedCall(timeCenterDelay + (timeCenterLifetime / 2f), () =>
        {
            listSacrficeTarget.ForEach((int index, GameObject itemObj) =>
            {
                VisualEffect visualEffect = itemObj.GetComponentInChildren<VisualEffect>(true);
                visualEffect.Stop();
            });
        });

        animForShowSacrficeEffectComplete = DOVirtual.DelayedCall(timeCenterDelay + timeCenterLifetime, () =>
        {
            actionForComplete?.Invoke();
        });
    }
}
