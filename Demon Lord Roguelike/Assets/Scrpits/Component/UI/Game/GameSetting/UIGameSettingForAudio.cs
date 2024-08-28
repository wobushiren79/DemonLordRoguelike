using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIGameSettingForAudio : UIGameSettingBase
{
    protected UIViewGameSettingRange rangeForMusic;
    protected UIViewGameSettingRange rangeForSound;

    public UIGameSettingForAudio(GameObject objListContainer) : base(objListContainer)
    {

    }

    public override void Open()
    {
        base.Open();
        string textMusicTitle = TextHandler.Instance.GetTextById(43001);
        rangeForMusic = CreatureItemForRange(textMusicTitle, 0f, 1f);
        rangeForMusic.SetProgress(gameConfig.musicVolume);

        string textSoundTitle = TextHandler.Instance.GetTextById(43002);
        rangeForSound = CreatureItemForRange(textSoundTitle, 0f, 1f);
        rangeForSound.SetProgress(gameConfig.soundVolume);
    }


    public override void ActionForRangeValueChange(UIViewGameSettingRange targetView, float progress)
    {
        base.ActionForRangeValueChange(targetView, progress);
        if (targetView == rangeForMusic)
        {
            targetView.SetProgressText($"{Mathf.RoundToInt(progress * 100)}");
            gameConfig.musicVolume = progress;
            AudioHandler.Instance.InitAudio();
        }
        else if (targetView == rangeForSound)
        {
            targetView.SetProgressText($"{Mathf.RoundToInt(progress * 100)}");
            gameConfig.soundVolume = progress;
            AudioHandler.Instance.InitAudio();
        }
    }
}
