using UnityEngine;

/// <summary>
/// 战斗掉落水晶数据
/// </summary>
public class FightDropCrystalBean
{
    //掉落水晶的基础存在时长(秒)；研究加成在掉落时叠加到此基础值上
    public const float BASE_LIFE_TIME = 30;
    //掉落位置
    public Vector3 dropPos;
    //掉落数量
    public int crystalNum;
    //掉落持续时间
    public float lifeTime;
    //掉落者生物UUID 用于BUFF事件筛选 BUFF自身追加的水晶应保持为空 避免被其他BUFF再次触发或自反馈
    public string dropperCreatureUUId;
    public FightDropCrystalBean()
    {

    }

    public FightDropCrystalBean(int crystalNum, Vector3 dropPos)
    {
        this.crystalNum = crystalNum;
        this.dropPos = dropPos;
        this.lifeTime = BASE_LIFE_TIME;
    }
}
