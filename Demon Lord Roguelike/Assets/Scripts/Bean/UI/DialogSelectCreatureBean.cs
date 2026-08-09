using System;
using System.Collections.Generic;

public class DialogSelectCreatureBean : DialogBean
{
    //最大选择数量
    public int selectNumMax;
    //魔物过滤条件(返回true=保留在可选列表中)；为空=不过滤
    public Func<CreatureBean, bool> filterCreature;
}