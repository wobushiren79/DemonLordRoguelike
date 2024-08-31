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
    /// ����
    /// </summary>
    public virtual void Launch()
    {
        //��ʼ��ͼ��
        IconHandler.Instance.InitData();
        //������һ���ڴ�
        SystemUtil.GCCollect();

        GameConfigBean gameConfig = GameDataHandler.Instance.manager.GetGameConfig();
        //����ȫ��
        Screen.fullScreen = gameConfig.window == 1 ? true : false;
        //����FPS
        FPSHandler.Instance.SetData(gameConfig.stateForFrames, gameConfig.frames);
        //�޸Ŀ����
        //CameraHandler.Instance.ChangeAntialiasing(gameConfig.GetAntialiasingMode(), gameConfig.antialiasingQualityLevel);
    }
}
