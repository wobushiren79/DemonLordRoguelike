using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LauncherGame : BaseLauncher
{
    public override void Launch()
    {
        base.Launch();
        WorldHandler.Instance.ClearWorldData(() =>
        {
            //打开加载UI
            UIHandler.Instance.OpenUIAndCloseOther<UILoading>();
            //镜头初始化
            CameraHandler.Instance.InitData();
            //环境参数初始化
            VolumeHandler.Instance.InitData();
            //加载基地场景
            WorldHandler.Instance.LoadBaseScene((targetObj) =>
            {
                //设置基地场景视角
                CameraHandler.Instance.SetGameStartCamera(int.MaxValue, true);
                //关闭LoadingUI 打开开始UI
                UIHandler.Instance.OpenUIAndCloseOther<UIMainStart>();
            });
        });
    }
}
