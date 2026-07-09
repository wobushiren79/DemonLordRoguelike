using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.VFX;

public partial class EffectHandler
{
    #region 内部工具
    /// <summary>
    /// 按 id 从 EffectInfo 配置解析粒子资源名(res_name)并缓存到 manager 字段：首次查表，之后直接读缓存字段，供高频粒子避免重复查配置表
    /// </summary>
    /// <param name="effectId">EffectInfo 配置表 id</param>
    /// <param name="cache">manager 上对应的 res_name 缓存字段(ref)</param>
    private string GetEffectResName(long effectId, ref string cache)
    {
        if (cache == null)
            cache = EffectInfoCfg.GetItemData(effectId).res_name;
        return cache;
    }
    #endregion

    #region 打击 / 溅血粒子
    /// <summary>
    /// 播放护盾打击粒子
    /// </summary>
    /// <param name="targetPos">粒子播放的世界坐标</param>
    /// <param name="attDirection">攻击来向(x<0从左、否则从右)，决定护盾朝向</param>
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
        manager.GetEffectForEnduring(GetEffectResName(manager.effectShieldHitId, ref manager.resNameShieldHit), (targetEffect) =>
        {
            playEffect?.Invoke(targetEffect);
        });
    }

    /// <summary>
    /// 播放血粒子
    /// </summary>
    /// <param name="targetPos">粒子播放的世界坐标</param>
    /// <param name="attDirection">攻击来向(x>0血向右溅、否则向左溅)</param>
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
        manager.GetEffectForEnduring(GetEffectResName(manager.effectBloodId, ref manager.resNameBlood), (targetEffect) =>
        {
            playEffect?.Invoke(targetEffect);
        });
    }
    #endregion

    #region 飘字(伤害数字)粒子
    /// <summary>
    /// 播放数字粒子(伤害/闪避/HP/护甲飘字)：从缓存池取或实例化→上色→DOTween 上飘淡出→归还缓存池
    /// </summary>
    /// <param name="targetPos">飘字起始的世界坐标</param>
    /// <param name="number">显示的数值(闪避类型忽略)</param>
    /// <param name="type">类型 0普通伤害 1闪避 2暴击伤害 3HP增加 4护甲增加</param>
    /// <param name="randomPosOffset">起始位置随机偏移范围</param>
    public void ShowTextNumEffect(Vector3 targetPos, int number, int type, float randomPosOffset = 0.2f)
    {
        // 从缓存池获取或实例化
        GameObject textObj = null;
        if (manager.queueTextNumPool.Count > 0)
        {
            textObj = manager.queueTextNumPool.Dequeue();
            textObj.ShowObj(true);
        }
        else
        {
            GameObject objModel = null;
            if (!manager.dicTextNumModel.TryGetValue(manager.effectTextNumberName, out objModel))
            {
                objModel = LoadAddressablesUtil.LoadAssetSync<GameObject>(manager.effectTextNumberName);
                if (objModel != null)
                    manager.dicTextNumModel.Add(manager.effectTextNumberName, objModel);
            }
            if (objModel == null)
            {
                LogUtil.LogError($"飘字预制体加载失败：{manager.effectTextNumberName}");
                return;
            }
            textObj = Instantiate(objModel);
            //记录到总表，便于战斗结束时统一清理
            manager.listTextNumAll.Add(textObj);
        }

        // 清除可能残留的Tween
        textObj.transform.DOKill();
        textObj.transform.localScale = Vector3.one;

        TextMeshPro textMesh = textObj.GetComponent<TextMeshPro>();
        if (textMesh == null)
        {
            LogUtil.LogError($"飘字预制体缺少TextMeshPro组件：{manager.effectTextNumberName}");
            Destroy(textObj);
            return;
        }
        textMesh.DOKill();

        // 确定颜色、大小、文本
        Color targetColor = Color.white;
        float targetTextSize = 2f;
        string textContent = number.ToString();
        switch (type)
        {
            case 0: // 普通伤害
                targetColor = manager.colorDamage;
                break;
            case 1: // 闪避
                targetColor = manager.colorDamageEVA;
                textContent = "闪避";
                break;
            case 2: // 暴击伤害
                targetColor = manager.colorDamageCRT;
                targetTextSize = 3f;
                break;
            case 3: // HP增加
                targetColor = manager.colorHPAdd;
                break;
            case 4: // 护甲增加
                targetColor = manager.colorDRAdd;
                break;
        }

        // 设置随机起始位置
        Vector3 startPos = RandomUtil.GetRandomVector3(targetPos, randomPosOffset);
        textObj.transform.position = startPos;

        // 设置文本和样式
        textMesh.text = textContent;
        textMesh.color = targetColor;
        textMesh.fontSize = targetTextSize;
        textMesh.alpha = 1f;

        // DG.Tweening动画：向上飘动 + 淡出 + 缩放弹出
        Sequence sequence = DOTween.Sequence();
        sequence.Append(textObj.transform.DOMoveY(startPos.y + 0.5f, 1f).SetEase(Ease.OutQuad));
        sequence.Join(textMesh.DOFade(0f, 0.8f).SetEase(Ease.InQuad));
        sequence.Join(textObj.transform.DOScale(1.2f, 0.15f).SetLoops(2, LoopType.Yoyo));

        sequence.OnComplete(() =>
        {
            textMesh.DOKill();
            textObj.transform.DOKill();
            textMesh.alpha = 1f;
            textObj.transform.localScale = Vector3.one;
            textObj.ShowObj(false);
            manager.queueTextNumPool.Enqueue(textObj);
        });
    }

    /// <summary>
    /// 清理所有飘字粒子（战斗结束时调用）
    /// </summary>
    public void ClearTextNumEffect()
    {
        for (int i = 0; i < manager.listTextNumAll.Count; i++)
        {
            var textObj = manager.listTextNumAll[i];
            if (textObj == null)
                continue;
            //停止可能残留的Tween
            textObj.transform.DOKill();
            TextMeshPro textMesh = textObj.GetComponent<TextMeshPro>();
            if (textMesh != null)
                textMesh.DOKill();
            Destroy(textObj);
        }
        manager.listTextNumAll.Clear();
        manager.queueTextNumPool.Clear();
    }
    #endregion

    #region 生物进阶粒子
    /// <summary>
    /// 展示生物进阶增加进度粒子(从起点飞向终点容器的流动光点)
    /// </summary>
    /// <param name="addNum">发射的光点数量</param>
    /// <param name="startPosition">光点起始世界坐标(带随机散布)</param>
    /// <param name="endPosition">光点汇聚的终点世界坐标</param>
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
        manager.GetEffectForEnduring(GetEffectResName(manager.effectCreatureAscendAddProgressId, ref manager.resNameAscendAddProgress), (targetEffect) =>
        {
            playEffect?.Invoke(targetEffect);
        });
    }

    /// <summary>
    /// 展示生物进阶完成的庆祝粒子(专用粒子,进阶成功时在容器处播放,按升阶后的新稀有度上色成"稀有度流光")。
    /// </summary>
    /// <param name="targetPosition">容器世界坐标(粒子播放位置)</param>
    /// <param name="rarityColor">升阶后新稀有度的主色(粒子起始颜色)</param>
    public void ShowCreatureAscendCompleteEffect(Vector3 targetPosition, Color rarityColor)
    {
        //专用一次性粒子:先定位、按稀有度给所有粒子系统上色,再播放,2秒后自动销毁
        EffectBean effectData = new EffectBean();
        effectData.effectName = GetEffectResName(manager.effectCreatureAscendCompleteId, ref manager.resNameAscendComplete);
        effectData.effectPosition = targetPosition;
        effectData.timeForShow = 2f;
        effectData.isDestoryPlayEnd = true;
        effectData.isPlayInShow = false;
        ShowEffect(effectData, (effect) =>
        {
            if (effect == null)
                return;
            //白模板粒子按新稀有度上色(逐个粒子系统改 startColor)后再播放
            if (!effect.listPS.IsNull())
            {
                for (int i = 0; i < effect.listPS.Count; i++)
                {
                    var mainModule = effect.listPS[i].main;
                    mainModule.startColor = rarityColor;
                }
            }
            effect.PlayEffect();
        });
    }
    #endregion

    #region 放置魔物粒子
    /// <summary>
    /// 放置魔物时在魔王(防守核心)位置播放消耗魔力粒子
    /// </summary>
    /// <param name="targetPos">魔王世界坐标</param>
    public void ShowManaEffect(Vector3 targetPos)
    {
        ShowEffect(manager.effectManaId, targetPos);
    }

    /// <summary>
    /// 放置魔物时在生成的魔物位置播放登场粒子
    /// </summary>
    /// <param name="targetPos">魔物生成的世界坐标</param>
    public void ShowCreatureShowEffect(Vector3 targetPos)
    {
        ShowEffect(manager.effectCreatureShowId, targetPos);
    }
    #endregion

    #region 配置驱动通用粒子
    /// <summary>
    /// 展示粒子(配置驱动)：按 EffectInfo 配置解析 float/int/vector3/vector4 数据并写入 VFX/PS，按展示类型(持久/一次性)取实例播放
    /// </summary>
    /// <param name="effectId">EffectInfo 配置表 id</param>
    /// <param name="targetPos">粒子播放的世界坐标</param>
    /// <param name="direction">方向(供含 {Direction} 占位的 int 数据使用)</param>
    /// <param name="size">尺寸倍率(供含 {Size} 占位的 float 数据及 PS 起始大小使用)</param>
    public void ShowEffect(long effectId, Vector3 targetPos, Direction2DEnum direction = Direction2DEnum.None, float size = 1)
    {
        targetPos += new Vector3(0, 0.002f, -0.001f);
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
    #endregion

    #region 献祭粒子
    /// <summary>
    /// 展示献祭粒子：各祭品发射光流飞向中心点，中心汇聚粒子按延时/存活时长播放，半程停祭品光流，结束触发完成回调
    /// </summary>
    /// <param name="listSacrficeTarget">祭品对象列表(各自带 VisualEffect 光流)</param>
    /// <param name="endPostion">汇聚中心世界坐标</param>
    /// <param name="timeCenterDelay">中心粒子起始延时</param>
    /// <param name="timeCenterLifetime">中心粒子存活时长</param>
    /// <param name="actionForComplete">全部播放结束的回调</param>
    public void ShowSacrficeEffect(List<GameObject> listSacrficeTarget, Vector3 endPostion, float timeCenterDelay, float timeCenterLifetime, Action actionForComplete)
    {
        if (manager.animForShowSacrficeEffect != null)
            manager.animForShowSacrficeEffect.Kill();
        if (manager.animForShowSacrficeEffectComplete != null)
            manager.animForShowSacrficeEffectComplete.Kill();
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

        manager.animForShowSacrficeEffect = DOVirtual.DelayedCall(timeCenterDelay + (timeCenterLifetime / 2f), () =>
        {
            listSacrficeTarget.ForEach((int index, GameObject itemObj) =>
            {
                VisualEffect visualEffect = itemObj.GetComponentInChildren<VisualEffect>(true);
                visualEffect.Stop();
            });
        });

        manager.animForShowSacrficeEffectComplete = DOVirtual.DelayedCall(timeCenterDelay + timeCenterLifetime, () =>
        {
            actionForComplete?.Invoke();
        });
    }
    #endregion
}
