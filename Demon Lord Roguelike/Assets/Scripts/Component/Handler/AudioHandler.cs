using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class AudioHandler
{
    public override void Awake()
    {
        base.Awake();
        UIHandler.Instance.AddOnClickAction(ActionForUIOnClick);
    }

    public void PlayMusicForMain()
    {
        List<AudioEnum> listMusic = new List<AudioEnum>
        {
            AudioEnum.music_main_1
        };
        PlayMusicListForLoop(listMusic);
    }

    public void PlayMusicForGaming()
    {
        // List<AudioEnum> listMusic = new List<AudioEnum>
        // {
        //     AudioEnum.music_gaming_1,AudioEnum.music_gaming_2,AudioEnum.music_gaming_3,AudioEnum.music_gaming_4,AudioEnum.music_gaming_5,AudioEnum.music_gaming_6,AudioEnum.music_gaming_7
        // };
        // PlayMusicListForLoop(listMusic);
    }

    public void PlayMusicForFight()
    {
        List<AudioEnum> listMusic = new List<AudioEnum>
        {
            AudioEnum.music_fight_1,AudioEnum.music_fight_2,AudioEnum.music_fight_3,AudioEnum.music_fight_4,AudioEnum.music_fight_5,AudioEnum.music_fight_6
        };
        PlayMusicListForLoop(listMusic);
    }

    /// <summary>
    /// UI点击回调
    /// </summary>
    public void ActionForUIOnClick(GameObject targetObj)
    {
        Button tagetButton = targetObj.GetComponent<Button>();
        if (tagetButton != null)
        {
            Image targetImage = tagetButton.image;
            if (targetImage == null || targetImage.sprite == null)
            {
                return;
            }
            LogUtil.Log($"ActionForUIOnClick {targetImage.sprite.name}");
            //通用点击
            //如果是退出点击
            if (targetImage.name.Equals("ViewExit"))
            {
                PlaySound(AudioEnum.sound_btn_4);
                return;
            }
            if (manager.listCommonUIClick.Contains(targetImage.sprite.name))
            {
                PlaySound(AudioEnum.sound_btn_3);
            }
        }
    }

    #region 音频枚举重载（统一用 AudioEnum 调用，内部委托给框架层 int 接口）
    /// <summary>
    /// 播放音效（枚举）
    /// </summary>
    /// <param name="audio">音频枚举</param>
    /// <param name="audioSource">指定播放源，null 时使用默认音效源</param>
    public void PlaySound(AudioEnum audio, AudioSource audioSource = null)
    {
        PlaySound((int)audio, audioSource);
    }

    /// <summary>
    /// 播放音效（枚举，指定位置）
    /// </summary>
    /// <param name="audio">音频枚举</param>
    /// <param name="soundPosition">播放位置</param>
    /// <param name="audioSource">指定播放源</param>
    public void PlaySound(AudioEnum audio, Vector3 soundPosition, AudioSource audioSource = null)
    {
        PlaySound((int)audio, soundPosition, audioSource);
    }

    /// <summary>
    /// 播放音效（枚举，指定位置与音量）
    /// </summary>
    /// <param name="audio">音频枚举</param>
    /// <param name="soundPosition">播放位置</param>
    /// <param name="volumeScale">音量大小</param>
    /// <param name="audioSource">指定播放源</param>
    public void PlaySound(AudioEnum audio, Vector3 soundPosition, float volumeScale, AudioSource audioSource = null)
    {
        PlaySound((int)audio, soundPosition, volumeScale, audioSource);
    }

    /// <summary>
    /// 播放音效（枚举，音量缩放）。
    /// 最终音量 = 配置表音效音量(soundVolume) × volumeScale，用于在配置音量基础上整体放大/缩小单个音效。
    /// </summary>
    /// <param name="audio">音频枚举</param>
    /// <param name="volumeScale">音量缩放倍率（叠加在配置音效音量上，1 为原始大小，1.5 为放大 1.5 倍）</param>
    /// <param name="audioSource">指定播放源，null 时使用默认音效源</param>
    public void PlaySoundForVolumeScale(AudioEnum audio, float volumeScale, AudioSource audioSource = null)
    {
        if (audioSource == null)
            audioSource = manager.audioSourceForSound;
        GameConfigBean gameConfig = GameDataHandler.Instance.manager.GetGameConfig();
        if (Camera.main != null)
        {
            PlaySound((int)audio, Camera.main.transform.position, gameConfig.soundVolume * volumeScale, audioSource);
        }
    }

    /// <summary>
    /// 循环播放音乐-单曲（枚举）
    /// </summary>
    /// <param name="audio">音频枚举</param>
    public void PlayMusicForLoop(AudioEnum audio)
    {
        PlayMusicForLoop((int)audio);
    }

    /// <summary>
    /// 循环播放音乐-单曲（枚举，指定音量）
    /// </summary>
    /// <param name="audio">音频枚举</param>
    /// <param name="volumeScale">音量大小</param>
    public void PlayMusicForLoop(AudioEnum audio, float volumeScale)
    {
        PlayMusicForLoop((int)audio, volumeScale);
    }

    /// <summary>
    /// 循环播放音乐-列表（枚举）
    /// </summary>
    /// <param name="audios">音频枚举列表</param>
    public void PlayMusicListForLoop(List<AudioEnum> audios)
    {
        if (audios == null)
            return;
        List<int> musicIds = new List<int>(audios.Count);
        for (int i = 0; i < audios.Count; i++)
        {
            musicIds.Add((int)audios[i]);
        }
        PlayMusicListForLoop(musicIds);
    }

    /// <summary>
    /// 播放环境音（枚举）
    /// </summary>
    /// <param name="audio">音频枚举</param>
    public void PlayEnvironment(AudioEnum audio)
    {
        PlayEnvironment((int)audio);
    }

    /// <summary>
    /// 播放环境音（枚举，指定音量）
    /// </summary>
    /// <param name="audio">音频枚举</param>
    /// <param name="volumeScale">音量大小</param>
    public void PlayEnvironment(AudioEnum audio, float volumeScale)
    {
        PlayEnvironment((int)audio, volumeScale);
    }
    #endregion
}
