/// <summary>
/// 一条进阶BUFF的命中概率(供孵化缸进阶详情实时展示用)。
/// </summary>
public struct CreatureAscendBuffChanceStruct
{
    /// <summary>可继承的 BUFF 配置 ID。</summary>
    public long buffId;
    /// <summary>素材 BUFF 的本地化名称。</summary>
    public string name;
    /// <summary>命中概率(0~100,百分比)</summary>
    public float rate;
    /// <summary>同 ID 素材中最大的触发值，用作进阶范围预览的下限。</summary>
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
