using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LauncherGame : BaseLauncher
{
    public override void Launch()
    {
        base.Launch();
        //故事演出系统初始化(事件注册;仅真实游戏入口注册,测试场景走 StoryHandler.PlayStory 强制播放)
        StoryHandler.Instance.InitData();
        WorldHandler.Instance.EnterMainForBaseScene();
    }
}
