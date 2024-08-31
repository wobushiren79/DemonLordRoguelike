using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseLauncher : BaseMonoBehaviour
{
    private void Start()
    {
        Launch();
    }

    /// <summary>
    /// 启动
    /// </summary>
    public virtual void Launch()
    {
        //初始化图集
        IconHandler.Instance.InitData();
        //先清理一下内存
        SystemUtil.GCCollect();

        GameConfigBean gameConfig = GameDataHandler.Instance.manager.GetGameConfig();
        //设置全屏
        Screen.fullScreen = gameConfig.window == 1 ? true : false;
        //设置FPS
        FPSHandler.Instance.SetData(gameConfig.stateForFrames, gameConfig.frames);
        //修改抗锯齿
        //CameraHandler.Instance.ChangeAntialiasing(gameConfig.GetAntialiasingMode(), gameConfig.antialiasingQualityLevel);
    }
}
