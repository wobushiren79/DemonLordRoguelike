

using UnityEngine.UI;

public partial class UIDialogPortalDetails : DialogView
{
    protected GameWorldInfoBean gameWorldInfo;
    protected GameWorldInfoRandomBean gameWorldInfoRandom;

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(GameWorldInfoBean gameWorldInfo, GameWorldInfoRandomBean gameWorldInfoRandom)
    {
        this.gameWorldInfo = gameWorldInfo;
        this.gameWorldInfoRandom = gameWorldInfoRandom;
        SetDifficultyLevel(gameWorldInfoRandom.difficultyLevel);
        LayoutRebuilder.ForceRebuildLayoutImmediate(ui_DialogContent);
    }

    /// <summary>
    /// 设置难度等级
    /// </summary>
    public void SetDifficultyLevel(int level)
    {
        ui_DifficultyContent.text = string.Format(TextHandler.Instance.GetTextById(403), level);
    }

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_DifficultySelectLeftBtn)
        {
            OnClickForChangeDifficultyLevel(-1);
        }
        else if (viewButton == ui_DifficultySelectRightBtn)
        {
            OnClickForChangeDifficultyLevel(1);
        }
    }

    /// <summary>
    /// 点击改变难度
    /// </summary>
    public void OnClickForChangeDifficultyLevel(int changeLevel)
    {
        UserUnlockWorldBean userUnlockWorldData = gameWorldInfoRandom.GetUserUnlockWorldData();

        gameWorldInfoRandom.difficultyLevel += changeLevel;
        if (gameWorldInfoRandom.difficultyLevel < 1)
        {
            gameWorldInfoRandom.difficultyLevel = 1;
        }
        else if (gameWorldInfoRandom.difficultyLevel > userUnlockWorldData.difficultyLevel)
        {
            gameWorldInfoRandom.difficultyLevel = userUnlockWorldData.difficultyLevel;
        }
        SetDifficultyLevel(gameWorldInfoRandom.difficultyLevel);
    }
}