

using System;
using UnityEngine.UI;

public partial class UIRewardSelect : BaseUIComponent
{
    public Action actionForEnd = null;

    public override void Awake()
    {
        base.Awake();
    }

    /// <summary>
    /// 设置数据
    /// </summary>
    public async void SetData(Action actionForEnd = null)
    {
        gameObject.SetActive(false);
        this.actionForEnd = actionForEnd;
        ui_TitleTextNum.text =  string.Format(TextHandler.Instance.GetTextById(52003), 3, 3);
        await WorldHandler.Instance.EnterRewardSelectScene();
        gameObject.SetActive(true);
    }
    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_SkipBtn)
        {
            actionForEnd?.Invoke();
        }
    }
    
}