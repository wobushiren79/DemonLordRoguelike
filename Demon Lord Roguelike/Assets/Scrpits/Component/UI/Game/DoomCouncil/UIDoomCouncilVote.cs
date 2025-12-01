

using UnityEngine;

public partial class UIDoomCouncilVote : BaseUIComponent
{
    protected DoomCouncilBean doomCouncilData;
    public int ayeVoteNum = 0;
    public int nayVoteNum = 0;

    public override void OpenUI()
    {
        base.OpenUI();
    }

    public override void RefreshUI(bool isOpenInit = false)
    {
        base.RefreshUI(isOpenInit);
        SetProgress();
    }

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(DoomCouncilBean doomCouncilData)
    {
        this.doomCouncilData = doomCouncilData;
        ayeVoteNum = 0;
        nayVoteNum = 0;
        SetTitle(doomCouncilData.doomCouncilInfo.name_language);
        RefreshUI();
    }

    /// <summary>
    /// 增加投票数据
    /// </summary>
    public void AddVoteData(NpcVoteTypeEnum npcVoteType, int voteNum)
    {
        if (npcVoteType == NpcVoteTypeEnum.Aye)
        {
            ayeVoteNum += voteNum;
        }
        else if (npcVoteType == NpcVoteTypeEnum.Nay)
        {
            nayVoteNum += voteNum;
        }
        RefreshUI();
    }

    /// <summary>
    /// 设置标题
    /// </summary>
    public void SetTitle(string title)
    {
        ui_TitleText.text = title;
    }

    /// <summary>
    /// 设置进度
    /// </summary>
    public void SetProgress()
    {        
        float leftProgress;
        float rightProgress;

        if (ayeVoteNum + nayVoteNum == 0)
        {
            leftProgress = 0.5f;
            rightProgress = 0.5f;
        }
        else
        {
            leftProgress = (float)ayeVoteNum / (ayeVoteNum + nayVoteNum);
            rightProgress = (float)nayVoteNum / (ayeVoteNum + nayVoteNum);
        }

        ui_ProgressLeft.fillAmount = leftProgress;
        ui_ProgressRight.fillAmount = rightProgress;

        ui_ProgressLeftTitleText.text = $"{TextHandler.Instance.GetTextById(53006)}\n{ayeVoteNum}";
        ui_ProgressRightTitleText.text = $"{TextHandler.Instance.GetTextById(53007)}\n{nayVoteNum}"; 
    }
}