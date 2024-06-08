using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VolumeManager : BaseManager
{
    //��������
    protected Volume _volume;
    public Volume volume
    {
        get
        {
            if (_volume == null)
            {
                _volume = FindWithTag<Volume>(TagInfo.Tag_Volume);
                if (_volume == null)
                {
                    GameObject objVolumeModel = LoadAddressablesUtil.LoadAssetSync<GameObject>(PathInfo.RenderVolumePath);
                    GameObject objVolume = Instantiate(gameObject, objVolumeModel);
                    objVolume.transform.localPosition = Vector3.zero;
                    _volume = objVolume.GetComponent<Volume>();
                }
            }
            return _volume;
        }
    }

    //�����ļ�
    protected VolumeProfile _volumeProfile;
    public VolumeProfile volumeProfile
    {
        get
        {
            if (_volumeProfile == null)
            {
                _volumeProfile = volume.profile;
            }
            return _volumeProfile;
        }
    }

    //Զ��ģ��
    protected DepthOfField _depthOfField;
    public DepthOfField depthOfField
    {
        get
        {
            if (_depthOfField == null)
            {
                volumeProfile.TryGet(out _depthOfField);
            }
            return _depthOfField;
        }
    }
}
