using System;

[Serializable]
public class FightBeanForTest : FightBean
{
    //进攻数据
    public FightAttackBean fightAttackDataRemark;


    public FightBeanForTest() : base()
    {
        
    }

    public FightBeanForTest(GameWorldInfoRandomBean gameWorldInfoRandomData) : base(gameWorldInfoRandomData)
    {

    }
}