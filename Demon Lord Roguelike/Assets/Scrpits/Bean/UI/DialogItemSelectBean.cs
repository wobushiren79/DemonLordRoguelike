using System;
using System.Collections.Generic;

public class DialogItemSelectBean : DialogBean
{
    public Action<UIDialogItemSelect, ItemBean> actionForSelectGift;
    public Action<UIDialogItemSelect, ItemBean> actionForSelectDelete;
}