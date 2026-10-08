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
        //初始化所有Mod(必须在 TextHandler/任何Cfg首次访问前: Mod的JsonText(含多语言)合并依赖扫描结果)
        ModHandler.Instance.InitializeAllModsSync();
        //设置多语言
        TextHandler.Instance.InitData();
        //初始化图集
        IconHandler.Instance.InitData();
        //先清理一下内存
        SystemUtil.GCCollect();

        GameConfigBean gameConfig = GameDataHandler.Instance.manager.GetGameConfig();
        //设置全屏
        Screen.fullScreen = gameConfig.window == 1 ? true : false;
        //初始化屏幕分辨率Handler（窗口自由拖动等比缩放）
        ScreenResolutionHandler.Instance.InitData();
        //设置垂直同步与FPS（两者互斥，锁帧优先：vSyncCount 非 0 时 targetFrameRate 会被忽略；旧配置可能两项同开，此处强制修正；必须先设 vSync 再设帧率）
        if (gameConfig.stateForFrames == 1)
            gameConfig.vsync = false;
        FPSHandler.Instance.SetSyncCount(gameConfig.vsync ? 1 : 0);
        FPSHandler.Instance.SetData(gameConfig.stateForFrames, gameConfig.frames);
        //修改抗锯齿
        //CameraHandler.Instance.ChangeAntialiasing(gameConfig.GetAntialiasingMode(), gameConfig.antialiasingQualityLevel);
        //音效初始化
        AudioHandler.Instance.InitAudio();
        //成就系统初始化(事件注册)
        AchievementHandler.Instance.InitData();
    }
}
