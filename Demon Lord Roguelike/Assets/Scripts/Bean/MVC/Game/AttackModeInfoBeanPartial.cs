using System;
using System.Collections.Generic;
using UnityEngine;
public partial class AttackModeInfoBean
{
    #region 攻击起始位置偏移
    protected bool isInitStartPosOffset = false;
    protected Vector3 startPosOffset;

    /// <summary>
    /// 获取攻击起始位置偏移（叠加在生物攻击起始位置之上，空=Vector3.zero，缓存解析结果）
    /// </summary>
    public Vector3 GetStartPosOffset()
    {
        if (!isInitStartPosOffset)
        {
            startPosOffset = start_pos_offset.SplitForVector3(',');
            isInitStartPosOffset = true;
        }
        return startPosOffset;
    }
    #endregion

    #region Buff
    protected List<BuffBean> listBuffData;
    /// <summary>
    /// 获取可能会触发的BUFF列表
    /// </summary>
    public List<BuffBean> GetListBuff()
    {
        if (buff.IsNull())
        {
            return null;
        }
        if (listBuffData.IsNull())
        {
            listBuffData = new List<BuffBean>();
            var dicBuffData = buff.SplitForDictionaryLongFloat();
            foreach (var item in dicBuffData)
            {
                BuffBean buffData = new BuffBean(item.Key, createRate: item.Value);
                listBuffData.Add(buffData);
            }
        }
        return listBuffData;
    }
    #endregion

    #region 碰撞检测
    protected float[] colliderAreaSize;

    /// <summary>
    /// 获取碰撞范围大小数组（逗号分隔，缓存解析结果）
    /// </summary>
    public float[] GetColliderAreaSize()
    {
        if (colliderAreaSize == null)
        {
            colliderAreaSize = collider_area_size.SplitForArrayFloat(',');
        }
        return colliderAreaSize;
    }

    /// <summary>
    /// 获取碰撞范围检测类型
    /// </summary>
    public CreatureSearchType GetColliderAreaSerachType()
    {
        return (CreatureSearchType)collider_area_type;
    }

    /// <summary>
    /// 获取攻击搜索检测类型
    /// </summary>
    public CreatureSearchType GetCreatureSerachType()
    {
        return (CreatureSearchType)attack_search_type;
    }
    #endregion

    #region 特效
    protected long[] effectHitIds;

    /// <summary>
    /// 获取击中特效ID（effect_hit 为 & 分隔的多个ID，缓存解析结果）
    /// </summary>
    /// <param name="index">特效索引，0=初始攻击，1=连锁攻击，以此类推</param>
    public long GetEffectHitId(int index = 0)
    {
        if (effect_hit.IsNull()) return 0;
        if (effectHitIds == null)
        {
            string[] parts = effect_hit.Split('&');
            effectHitIds = new long[parts.Length];
            for (int i = 0; i < parts.Length; i++)
                long.TryParse(parts[i].Trim(), out effectHitIds[i]);
        }
        if (index < 0 || index >= effectHitIds.Length) return 0;
        return effectHitIds[index];
    }
    #endregion

    #region 拖尾(Trail 方案B)
    protected bool isInitTrailConfig = false;
    protected AttackModeTrailConfig trailConfig;

    /// <summary>
    /// 获取拖尾配置（trail_data 为 & 分隔的 key:value 项：count/interval/startAlpha/endAlpha/color；缓存解析结果）。
    /// <para>需配合 visual_name 走 DSP 批量渲染方可生效（残影材质克隆弹体桶材质）；trail_data 空或 count/interval≤0 即不启用。</para>
    /// </summary>
    public AttackModeTrailConfig GetTrailConfig()
    {
        if (!isInitTrailConfig)
        {
            trailConfig = AttackModeTrailConfig.Parse(trail_data);
            isInitTrailConfig = true;
        }
        return trailConfig;
    }
    #endregion
}

/// <summary>
/// 攻击弹道拖尾渲染方式：
/// <para>1=Instanced：现有方案，DrawMeshInstanced 逐"年龄档"批量把弹体贴图画在若干历史位置上（默认）。</para>
/// <para>2=Vfx：单个 GPU VFX 特效（VFX_Trail_1），每帧记录子弹位置并在该位置喷射轨迹粒子；支持逐弹染色。</para>
/// </summary>
public enum AttackModeTrailType
{
    /// <summary>不启用拖尾（仅运行时状态：BaseAttackMode.trailMode 用；配置解析不会产出此值）。</summary>
    None = 0,
    /// <summary>方案1：DrawMeshInstanced 逐年龄档批量绘制历史位置（默认）。染色为桶级——同 visual_name 的弹道共用首个注册者的 color。</summary>
    Instanced = 1,
    /// <summary>方案2：单个 GPU VFX 特效，每帧记录子弹位置喷射轨迹粒子。染色为逐弹级——同 visual_name 下各攻击模式可各配各的 color。</summary>
    Vfx = 2,
}

/// <summary>
/// 攻击弹道拖尾(残影 Ghost)配置：由配置表 trail_data 列解析而来（type:方式&count:残影数&interval:采样间隔秒&startAlpha:最新档透明度&endAlpha:最老档透明度&color:染色rgb）。
/// <para>残影 = 弹体贴图本身画在若干历史位置上、越老越透明(类似冲刺残影)。enable=count>0 且 interval>0 时成立；
/// startAlpha 是最靠近弹体一档的透明度、endAlpha 是最远一档的透明度(线性插值)；color 默认白(不改弹体贴图原色)。</para>
/// <para>type 选择渲染方式：1=Instanced(默认)，2=Vfx(单个 GPU VFX 特效)。</para>
/// <para>⚠️**两方式吃的参数不同**：
/// ①Instanced 用全套 `count/interval/startAlpha/endAlpha/color`；
/// ②**Vfx 只用 `type` + `color`**——段数/间隔/透明度是桶级的(注册时灌进 VFX 实例、同 visual_name 首个注册者赢，逐行配了也没用)，
/// 故已统一写死在 `EffectHandler` 的「攻击弹道拖尾粒子」区常量里，配置侧不再提供；写了会被忽略。要调这些表现请改那几个常量。</para>
/// <para>⚠️color 的作用域随 type 而异：Vfx 逐弹生效(同 visual_name 下各行可各配各色，同一 VFX 内并存)；
/// Instanced 为桶级——桶按 visual_name 建、首个注册者的 color 赢，同 visual_name 的其余行 color 会被静默忽略。需同图不同色时请用 type:2。</para>
/// </summary>
public struct AttackModeTrailConfig
{
    /// <summary>是否启用拖尾（方案1：count>0 且 interval>0；方案2：配了 type:2 即启用）</summary>
    public bool enable;
    /// <summary>拖尾渲染方式（1=Instanced 桶级染色；2=Vfx 逐弹染色）；未配 type 时默认 Instanced</summary>
    public AttackModeTrailType type;
    /// <summary>【仅方案1】残影数量（几个残影档；渲染器 clamp 到 TrailMaxPoints）。方案2 忽略——其粒子寿命写死在 EffectHandler</summary>
    public int count;
    /// <summary>【仅方案1】采样间隔（秒），相邻残影之间的时间间距 → 空间间距≈弹速×interval。方案2 忽略——其喷粒间隔写死在 EffectHandler</summary>
    public float interval;
    /// <summary>【仅方案1】最靠近弹体(最新)一档的透明度。方案2 忽略——写死在 EffectHandler</summary>
    public float startAlpha;
    /// <summary>【仅方案1】最远(最老)一档的透明度。方案2 忽略——写死在 EffectHandler</summary>
    public float endAlpha;
    /// <summary>残影叠加染色（默认白，即用弹体贴图原色；alpha 不用此值、由 start/endAlpha 逐档决定）</summary>
    public Color color;

    /// <summary>
    /// 解析 trail_data 字符串为拖尾配置；空串/无效返回未启用配置。未配透明度时默认 startAlpha=0.5、endAlpha=0.05。
    /// </summary>
    public static AttackModeTrailConfig Parse(string trailData)
    {
        AttackModeTrailConfig cfg = default;
        cfg.color = Color.white;
        cfg.startAlpha = 0.5f;
        cfg.endAlpha = 0.05f;
        cfg.type = AttackModeTrailType.Instanced;
        if (string.IsNullOrEmpty(trailData))
            return cfg;
        //按 & 拆项，每项以第一个 : 拆 key/value
        string[] items = trailData.Split('&');
        for (int i = 0; i < items.Length; i++)
        {
            string item = items[i];
            if (string.IsNullOrEmpty(item))
                continue;
            int sep = item.IndexOf(':');
            if (sep <= 0)
                continue;
            string key = item.Substring(0, sep).Trim();
            string val = item.Substring(sep + 1).Trim();
            switch (key)
            {
                case "type":
                    //仅显式配 2 才走 VFX 方案，其余(含非法值)一律回退现有 Instanced 方案
                    cfg.type = (int.TryParse(val, out int t) && t == (int)AttackModeTrailType.Vfx)
                        ? AttackModeTrailType.Vfx : AttackModeTrailType.Instanced;
                    break;
                case "count":
                    int.TryParse(val, out cfg.count);
                    break;
                case "interval":
                    float.TryParse(val, out cfg.interval);
                    break;
                case "startAlpha":
                    float.TryParse(val, out cfg.startAlpha);
                    break;
                case "endAlpha":
                    float.TryParse(val, out cfg.endAlpha);
                    break;
                case "color":
                    cfg.color = ParseColor(val, cfg.color);
                    break;
            }
        }
        //方案2(Vfx)：段数/间隔/透明度已在 EffectHandler 内写死(配置侧只需 type+color)，故只要显式配了 type:2 即启用，不看 count/interval
        //方案1(Instanced)：仍靠 count/interval 决定是否启用(两者>0 才有残影可画)
        cfg.enable = cfg.type == AttackModeTrailType.Vfx || (cfg.count > 0 && cfg.interval > 0f);
        return cfg;
    }

    /// <summary>逗号分隔的 rgba(a 可省略默认1)解析为 Color；失败返回回退色。</summary>
    private static Color ParseColor(string val, Color fallback)
    {
        if (string.IsNullOrEmpty(val))
            return fallback;
        string[] parts = val.Split(',');
        if (parts.Length < 3)
            return fallback;
        Color c = fallback;
        float.TryParse(parts[0], out c.r);
        float.TryParse(parts[1], out c.g);
        float.TryParse(parts[2], out c.b);
        c.a = 1f;
        if (parts.Length >= 4)
            float.TryParse(parts[3], out c.a);
        return c;
    }
}
public partial class AttackModeInfoCfg
{

    /// <summary>
    /// 初始化测试数据
    /// </summary>
    public static void InitTestData(string buffTestData)
    {
        var allData = GetAllData();
        allData.ForEach((key, value) =>
        {
            value.buff = buffTestData;
            value.GetListBuff();
        });
    }
    
}