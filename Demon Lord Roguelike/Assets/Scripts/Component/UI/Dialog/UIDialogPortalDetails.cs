

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
        var userData = GameDataHandler.Instance.manager.GetUserData();
        var userUnlock = userData.GetUserUnlockData();
        //用户可以选择的最高难度
        int userDifficultyLevel = userUnlock.GetUnlockGameWorldConquerDifficultyLevel(gameWorldInfoRandom.worldId);

        gameWorldInfoRandom.difficultyLevel += changeLevel;
        if (gameWorldInfoRandom.difficultyLevel < 1)
        {
            gameWorldInfoRandom.difficultyLevel = 1;
        }
        else if (gameWorldInfoRandom.difficultyLevel > userDifficultyLevel)
        {
            gameWorldInfoRandom.difficultyLevel = userDifficultyLevel;
        }
        SetDifficultyLevel(gameWorldInfoRandom.difficultyLevel);
    }
}