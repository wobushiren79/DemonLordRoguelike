using System;
using System.Collections.Generic;

public class DialogSelectItemBean : DialogBean
{
    public Action<UIDialogSelectItem, ItemBean> actionForSelectGift;
    public Action<UIDialogSelectItem, ItemBean> actionForSelectDelete;
}