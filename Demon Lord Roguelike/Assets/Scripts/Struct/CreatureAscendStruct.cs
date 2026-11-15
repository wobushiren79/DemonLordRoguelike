/// <summary>
/// 一条进阶BUFF的命中概率(供孵化缸进阶详情实时展示用)。
/// </summary>
public struct CreatureAscendBuffChanceStruct
{
    /// <summary>BUFF配置id;-1表示"随机增益"兜底项(未命中素材BUFF时走通用随机)</summary>
    public long buffId;
    /// <summary>展示名(素材BUFF取 name_language,兜底项取 BuffUtil.AscendRandomBuffName)</summary>
    public string name;
    /// <summary>命中概率(0~100,百分比)</summary>
    public float rate;
    /// <summary>触发值下限(素材命中该 id 时抬高的下限,取各素材最大原值;兜底项为0)。用于进阶增益范围预览按抬高后的下限显示 min~max</summary>
    public float floorValue;
    /// <summary>同 id 素材中最强的百分比。减冷却、减耗魔取最小原值，其余取最大原值。用于进阶范围预览。</summary>
    public float floorValueRate;
}

/// <summary>
/// 同一 BUFF 在素材里出现的次数和最强数值，用于进阶生成和概率展示。
/// </summary>
public struct CreatureAscendMaterialBuffStruct
{
    /// <summary>同一 buff id 在素材里出现的数量</summary>
    public int count;
    /// <summary>触发值下限(取各素材该 id 的最大原值)</summary>
    public float floorValue;
    /// <summary>各素材该 id 的最强百分比。减冷却、减耗魔取最小原值，其余取最大原值。</summary>
    public float floorValueRate;
}
