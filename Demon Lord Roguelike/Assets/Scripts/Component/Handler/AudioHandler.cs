using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public partial class AudioHandler
{
    public override void Awake()
    {
        base.Awake();
        UIHandler.Instance.AddOnClickAction(ActionForUIOnClick);
        //订阅 ESC：与点击音效对称，按 ESC 退出当前界面时也走通用退出音效
        InputAction escAction = InputHandler.Instance.manager.GetInputUIData(InputActionUIEnum.ESC);
        if (escAction != null)
            escAction.started += ActionForUIEscExit;
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
            //弹窗背景点击关闭：命中 DialogView 的 ui_Background 且该弹窗允许点背景关闭(isDestroyBG)时播退出音；放在 sprite 空判之前因背景按钮常无 sprite
            DialogView dialogView = targetObj.GetComponentInParent<DialogView>();
            if (dialogView != null && dialogView.ui_Background == tagetButton
                && dialogView.dialogData != null && dialogView.dialogData.isDestroyBG)
            {
                PlaySound(AudioEnum.sound_btn_31);
                return;
            }
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
                PlaySound(AudioEnum.sound_btn_31);
                return;
            }
            if (manager.listCommonUIClick.Contains(targetImage.sprite.name))
            {
                PlaySound(AudioEnum.sound_btn_1);
            }
        }
    }

    /// <summary>
    /// ESC 退出回调。鼠标点 ViewExit 按钮的退出音走 ActionForUIOnClick（射线命中），
    /// 但 ESC 退出不产生点击，故在此全局补播退出音；任意 ESC 按下均播放。
    /// </summary>
    /// <param name="callback">输入回调上下文</param>
    public void ActionForUIEscExit(InputAction.CallbackContext callback)
    {
        PlaySound(AudioEnum.sound_btn_31);
    }

    #region 音频枚举重载（统一用 AudioEnum 调用，内部委托给框架层 int 接口）
    /// <summary>
    /// 播放音效（枚举）
    /// </summary>
    /// <param name="audio">音频枚举</param>
    /// <param name="audioSource">指定播放源，null 时使用默认音效源</param>
    public void PlaySound(AudioEnum audio, AudioSource audioSource = null)
    {
        PlaySound((long)audio, audioSource);
    }

    /// <summary>
    /// 随机播放一个音效（从传入的音效枚举中等概率随机取一个播放）。
    /// 用于同类音效有多个备选、希望避免重复时随机切换（如清扫音效 sound_clean_1/sound_clean_2）。
    /// </summary>
    /// <param name="audios">候选音效枚举，至少传入一个；为空时不播放</param>
    public void PlaySoundRandom(params AudioEnum[] audios)
    {
        if (audios == null || audios.Length == 0)
            return;
        int randomIndex = Random.Range(0, audios.Length);
        PlaySound(audios[randomIndex]);
    }

    /// <summary>
    /// 播放音效（枚举，指定位置）
    /// </summary>
    /// <param name="audio">音频枚举</param>
    /// <param name="soundPosition">播放位置</param>
    /// <param name="audioSource">指定播放源</param>
    public void PlaySound(AudioEnum audio, Vector3 soundPosition, AudioSource audioSource = null)
    {
        PlaySound((long)audio, soundPosition, audioSource);
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
        PlaySound((long)audio, soundPosition, volumeScale, audioSource);
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
            PlaySound((long)audio, Camera.main.transform.position, gameConfig.soundVolume * volumeScale, audioSource);
        }
    }

    /// <summary>
    /// 循环播放音乐-单曲（枚举）
    /// </summary>
    /// <param name="audio">音频枚举</param>
    public void PlayMusicForLoop(AudioEnum audio)
    {
        PlayMusicForLoop((long)audio);
    }

    /// <summary>
    /// 循环播放音乐-单曲（枚举，指定音量）
    /// </summary>
    /// <param name="audio">音频枚举</param>
    /// <param name="volumeScale">音量大小</param>
    public void PlayMusicForLoop(AudioEnum audio, float volumeScale)
    {
        PlayMusicForLoop((long)audio, volumeScale);
    }

    /// <summary>
    /// 循环播放音乐-列表（枚举）
    /// </summary>
    /// <param name="audios">音频枚举列表</param>
    public void PlayMusicListForLoop(List<AudioEnum> audios)
    {
        if (audios == null)
            return;
        List<long> musicIds = new List<long>(audios.Count);
        for (int i = 0; i < audios.Count; i++)
        {
            musicIds.Add((long)audios[i]);
        }
        PlayMusicListForLoop(musicIds);
    }

    /// <summary>
    /// 播放环境音（枚举）
    /// </summary>
    /// <param name="audio">音频枚举</param>
    public void PlayEnvironment(AudioEnum audio)
    {
        PlayEnvironment((long)audio);
    }

    /// <summary>
    /// 播放环境音（枚举，指定音量）
    /// </summary>
    /// <param name="audio">音频枚举</param>
    /// <param name="volumeScale">音量大小</param>
    public void PlayEnvironment(AudioEnum audio, float volumeScale)
    {
        PlayEnvironment((long)audio, volumeScale);
    }

    /// <summary>
    /// 播放连续音效（枚举）。默认音量取 soundVolume；pitch 可加速/变调（2=加快一倍）。
    /// </summary>
    /// <param name="audio">音频枚举</param>
    /// <param name="volumeScale">基础音量；&lt;0 时取音效音量 soundVolume（默认行为）</param>
    /// <param name="pitch">播放速率/音调，1=原速，2=加快一倍（同时升调）</param>
    public void PlayLoopSound(AudioEnum audio, float volumeScale = -1f, float pitch = 1f)
    {
        PlayLoopSound((long)audio, volumeScale, pitch);
    }

    /// <summary>
    /// 停止连续音效（枚举）
    /// </summary>
    /// <param name="audio">音频枚举</param>
    public void StopLoopSound(AudioEnum audio)
    {
        StopLoopSound((long)audio);
    }

    /// <summary>
    /// 指定连续音效是否正在播放（枚举）
    /// </summary>
    /// <param name="audio">音频枚举</param>
    /// <returns>正在播放为 true</returns>
    public bool IsLoopSoundPlaying(AudioEnum audio)
    {
        return IsLoopSoundPlaying((long)audio);
    }
    #endregion
}
