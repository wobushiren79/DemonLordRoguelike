using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public partial class EffectManager
{
    #region 粒子资源名 / ID 常量
    //受击溅血粒子ID(EffectBlood_1,高频调用)
    public long effectBloodId = 1200001;
    //护盾打击粒子ID(EffectShieldHit_1,高频调用)
    public long effectShieldHitId = 1300001;
    //进阶增加进度粒子ID(EffectMove_1,飞向容器的流动光点)
    public long effectCreatureAscendAddProgressId = 1400001;
    //进阶完成庆祝粒子ID(EffectAscendComplete_1,白模板运行时按新稀有度上色成"稀有度流光")
    public long effectCreatureAscendCompleteId = 1500001;
    //放置魔物-魔王(防守核心)处消耗魔力粒子ID
    public long effectManaId = 1000001;
    //放置魔物-生成位置魔物登场粒子ID
    public long effectCreatureShowId = 1100001;
    //飘字(伤害数字)预制体地址
    public string effectTextNumberName = "Assets/LoadResources/Effects/EffectTextNumber_2.prefab";

    //res_name 缓存：高频/常用粒子首次按 id 查表解析后缓存到字段，后续直接读字段避免重复查配置表
    public string resNameBlood;
    public string resNameShieldHit;
    public string resNameAscendAddProgress;
    public string resNameAscendComplete;
    #endregion

    #region 飘字(伤害数字)缓存与颜色
    // 飘字模型缓存
    public Dictionary<string, GameObject> dicTextNumModel = new Dictionary<string, GameObject>();
    // 飘字对象缓存池
    public Queue<GameObject> queueTextNumPool = new Queue<GameObject>();
    // 飘字对象总表（含使用中与缓存池），用于战斗结束时统一清理
    public List<GameObject> listTextNumAll = new List<GameObject>();

    //普通伤害颜色
    public Color colorDamage = new Color(1f, 0.647f, 0);
    //闪避伤害颜色
    public Color colorDamageEVA = new Color(1f, 1f, 1f);
    //暴击伤害颜色
    public Color colorDamageCRT = new Color(0.698f, 0.133f, 0.133f);
    //HP颜色
    public Color colorHPAdd = new Color(0.196f, 0.804f, 0.196f);
    //DR颜色
    public Color colorDRAdd = new Color(0.255f, 0.412f, 1f);
    #endregion

    #region 献祭动画句柄
    //献祭祭品飞向中心的动画句柄
    public Tween animForShowSacrficeEffect;
    //献祭完成回调的延时句柄
    public Tween animForShowSacrficeEffectComplete;
    #endregion
}
