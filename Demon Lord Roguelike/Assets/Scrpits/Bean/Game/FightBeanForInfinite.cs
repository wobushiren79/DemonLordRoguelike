using System;

[Serializable]
public class FightBeanForInfinite : FightBean
{
    //游戏随机数据
    public GameWorldInfoRandomBean gameWorldInfoRandomData;

    public FightBeanForInfinite(GameWorldInfoRandomBean gameWorldInfoRandomData) : base()
    {
        this.gameWorldInfoRandomData = gameWorldInfoRandomData;
        gameFightType = gameWorldInfoRandomData.gameFightType;
        InitData();
    }
}