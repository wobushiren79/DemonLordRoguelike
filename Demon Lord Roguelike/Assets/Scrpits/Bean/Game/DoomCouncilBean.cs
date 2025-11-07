using System.Collections.Generic;

public class DoomCouncilBean
{
    //议会信息
    public DoomCouncilInfoBean doomCouncilInfo;
    //所有的议员
    public List<CreatureBean> listCouncilor;

    /// <summary>
    /// 获取议员数据
    /// </summary>
    public CreatureBean GetCouncilor(string creatureUUId)
    {
        if (listCouncilor.IsNull())
        {
            return null;
        }
        for (int i = 0; i < listCouncilor.Count; i++)
        {
            if (listCouncilor[i].creatureUUId == creatureUUId)
            {
                return listCouncilor[i];
            }
        }
        return null;
    }
}