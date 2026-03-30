using System;
using System.Collections.Generic;

public class DialogSelectBean : DialogBean
{
    public List<string> listSelectContent = new List<string>();
    public List<Action> listActions = new List<Action>();

    public void AddSelect(string selectContent, Action action)
    {
        listSelectContent.Add(selectContent);
        listActions.Add(action);
    }
}