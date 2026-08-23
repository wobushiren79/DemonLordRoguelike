using UnityEngine;

/// <summary>
/// 全局单例粒子播放参数（<see cref="EffectHandler.ShowEnduringSingletonEffect"/> 入参）。
/// <para>除 targetPos 必填外，其余字段以默认值(0)为哨兵：**有参数才设置**——值为哨兵的字段不覆盖粒子 prefab 配置原值，
/// 用对象初始化器只赋需要的字段即可（struct 零堆分配，命中特效热路径友好）。</para>
/// </summary>
public struct SingletonEffectParam
{
    #region 字段
    /// <summary>播放位置（世界坐标，必填）</summary>
    public Vector3 targetPos;
    /// <summary>主粒子时长（秒，如地面火焰的燃烧时长）；0=不设置</summary>
    public float duration;
    /// <summary>主粒子起始尺寸倍率（如冲击波半径换算）；0=不设置</summary>
    public float startSizeMultiplier;
    /// <summary>主粒子寿命倍率（如冲击波扩张时长换算）；0=不设置</summary>
    public float startLifetimeMultiplier;
    /// <summary>攻击方向（供 EffectInfo 含 {Direction} 占位的 VFX int 数据使用，如刀光朝向）；None=不设置</summary>
    public Direction2DEnum direction;
    /// <summary>尺寸倍率（供 EffectInfo 含 {Size} 占位的 VFX float 数据使用，攻击方传 colliderAreaSize[0]）；0=不设置</summary>
    public float size;
    #endregion
}
