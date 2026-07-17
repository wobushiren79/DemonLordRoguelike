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
    //攻击弹道拖尾粒子ID(方案2 VFX,Effect_Trail_1;非播放式——EffectHandler 按视觉桶各建一个常驻实例+每帧喂 GraphicsBuffer,不入池不 PlayEffect)
    public long effectAttackModeTrailId = 1600001;
    //飘字(伤害数字)预制体地址
    public string effectTextNumberName = "Assets/LoadResources/Effects/EffectTextNumber_2.prefab";

    //res_name 缓存：高频/常用粒子首次按 id 查表解析后缓存到字段，后续直接读字段避免重复查配置表
    public string resNameBlood;
    public string resNameShieldHit;
    public string resNameAscendAddProgress;
    public string resNameAscendComplete;
    public string resNameAttackModeTrail;
    #endregion

    #region 攻击弹道拖尾粒子(方案2 VFX)状态
    //拖尾 VFX 实例注册表：key=弹道视觉桶签名(visualKey)，value=该桶专属的 VFX 实例+上传缓冲+染色基准
    //由 EffectHandler 的「攻击弹道拖尾粒子」区独占读写；AttackModeInstanceRenderer 只经 Handler 接口报位置+染色，不直接访问
    public Dictionary<string, AttackModeTrailVfxBean> dicAttackModeTrailVfx = new Dictionary<string, AttackModeTrailVfxBean>();
    //拖尾 VFX 模板预制(本场解析一次后缓存)：与 tried 门控配合——tried=true 且此值为 null 即表示资源缺失，本场不再重试
    public GameObject objAttackModeTrailModel;
    //本场是否已尝试加载拖尾 VFX 模板：缺资源时 Addressables 会抛异常，用它保证每场至多试一次、避免逐桶注册刷屏；整场结束由 ClearAllAttackModeTrailVfx 复位
    public bool triedLoadAttackModeTrailModel = false;
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
