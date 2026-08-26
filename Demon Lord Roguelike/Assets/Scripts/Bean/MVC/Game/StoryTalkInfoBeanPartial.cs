using System;
using System.Collections.Generic;
public partial class StoryTalkInfoBean
{
    /// <summary>
    /// 是否旁白（npc_id=0：无立绘/无名字/无贿赂按钮）
    /// </summary>
    public bool IsNarration()
    {
        return npc_id == 0;
    }
}
public partial class StoryTalkInfoCfg
{
}
