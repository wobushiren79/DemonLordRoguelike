using System;
using System.Collections.Generic;
public partial class AttackModeInfoBean
{
    protected FightBuffStruct[] fightBuff;
    public FightBuffStruct[] GetBuff()
    {
        if (buff.IsNull())
        {
            return null;
        }
        if (fightBuff.IsNull())
        {
            fightBuff = FightBuffStruct.GetData(buff);
        }
        return fightBuff;
    }
}
public partial class AttackModeInfoCfg
{

}