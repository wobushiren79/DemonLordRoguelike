
using System.Collections.Generic;

public class GashaponMachineBean
{
    //抽取数量
    public int gashaponNum = 0;
    //单次孕育消耗的魔晶（用于再次孕育/重置时扣费）
    public int payCrystal = 0;
    //抽取的生物数据
    public List<GashaponMachineCreatureStruct> listCreatureRandomData;
}
public struct GashaponMachineCreatureStruct
{
    //生物ID
    public long creatureId;
    //随机生物模块ID
    public Dictionary<CreatureSkinTypeEnum,List<long>> randomCreatureMode;
}