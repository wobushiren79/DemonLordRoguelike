using System;
using System.Collections.Generic;

[Serializable]
public class FightBeanForTest : FightBean
{
    //进攻数据
    public FightAttackBean fightAttackDataRemark;

    //测试深渊馈赠目标行ID列表(已按"族根+等级"解析好的具体馈赠行id；由 GameFightLogicTest 在防守核心创建后统一添加)
    public List<long> testAbyssalBlessingIds = new List<long>();

    //测试魔王蓝量(战斗开始时魔王当前魔力值,应用时同时把魔力上限提升到不低于该值;由 GameFightLogicTest 在防守核心创建后统一应用)
    public float testDemonLordMP = 9999;


    public FightBeanForTest() : base()
    {

    }
}