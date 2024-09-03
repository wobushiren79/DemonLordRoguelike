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
            //�򿪼���UI
            UIHandler.Instance.OpenUIAndCloseOther<UILoading>();
            //��ͷ��ʼ��
            CameraHandler.Instance.InitData();
            //����������ʼ��
            VolumeHandler.Instance.InitData();
            //���ػ��س���
            WorldHandler.Instance.LoadBaseScene((targetObj) =>
            {
                //���û��س����ӽ�
                CameraHandler.Instance.SetGameStartCamera(int.MaxValue, true);
                //�ر�LoadingUI �򿪿�ʼUI
                UIHandler.Instance.OpenUIAndCloseOther<UIMainStart>();
            });
        });
    }
}
