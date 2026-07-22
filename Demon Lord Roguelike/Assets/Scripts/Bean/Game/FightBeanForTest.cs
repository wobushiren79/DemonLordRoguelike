using System;
using System.Collections.Generic;

[Serializable]
public class FightBeanForTest : FightBean
{
    //进攻数据
    public FightAttackBean fightAttackDataRemark;

    //测试深渊馈赠目标行ID列表(已按"族根+等级"解析好的具体馈赠行id；由 GameFightLogicTest 在防守核心创建后统一添加)
    public List<long> testAbyssalBlessingIds = new List<long>();


    public FightBeanForTest() : base()
    {

    }
}