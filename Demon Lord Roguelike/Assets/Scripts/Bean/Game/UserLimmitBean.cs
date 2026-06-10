using System;
[Serializable]
public class UserLimmitBean
{
    //阵容套数基础值（最终可用套数 = 该基础值 + LineupNum 研究等级）
    public int lineupMax = 1;
    //单个阵容生物上限基础值（最终上限 = 该基础值 + LineupCreatureAddNum 研究等级）
    public int lineupCreatureMax = 5;
    //传送门显示数量基础值（最终数量 = 该基础值 + PortalShowNum 研究等级）
    public int portalShowMax = 3;
    //征服模式难度等级基础值（最终难度 = 该基础值 + 对应世界征服难度研究等级）
    public int conquerDifficultyMax = 1;
    //生物升阶容器数量基础值（已解锁容器功能时，最终数量 = 该基础值 + CreatureVatAdd 研究等级）
    public int creatureVatMax = 1;
    //献祭最大数量
    public int sacrificeMax = 5;
    //孕育(扭蛋)创建生物时的随机属性总点数
    public int gashaponRandomAttributeNum = 5;
}
