using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class VolumeHandler : BaseHandler<VolumeHandler, VolumeManager>
{
    /// <summary>
    /// ��ʼ������
    /// </summary>
    public void InitData()
    {
        GameConfigBean gameConfig = GameDataHandler.Instance.manager.GetGameConfig();
        SetDepthOfField(DepthOfFieldMode.Bokeh, 4, 140, 10);
    }


    /// <summary>
    /// ����Զ��ģ��
    /// </summary>
    /// <param name="mode">Off��ѡ���ѡ��ɽ��þ��Gaussian��ѡ���ѡ���ʹ�ø��쵫�����޵ľ���ģʽ��Bokeh��ѡ���ѡ���ʹ�û���ɢ���ľ���ģʽ��</param>
    /// <param name="focusDistance">���ô������������ľ���</param>
    /// <param name="focalLength">	������������������������ͷ֮��ľ��루�Ժ���Ϊ��λ����ֵԽ�󣬾���Խǳ��</param>
    /// <param name="aperture">���ÿ׾��ȣ�Ҳ��Ϊ f ֵ (f-stop) �� f �� (f-number)����ֵԽС������Խǳ��</param>
    public void SetDepthOfField(DepthOfFieldMode mode, float focusDistance, float focalLength, float aperture)
    {
        var depthOfField = manager.depthOfField;
        depthOfField.mode.overrideState = true;
        depthOfField.mode.value = mode;
        depthOfField.focusDistance.overrideState = true;
        depthOfField.focusDistance.value = focusDistance;
        depthOfField.focalLength.overrideState = true;
        depthOfField.focalLength.value = focalLength;
        depthOfField.aperture.overrideState = true;
        depthOfField.aperture.value = aperture;
    }
}